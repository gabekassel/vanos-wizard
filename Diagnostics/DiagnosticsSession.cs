using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using S54VanosTester.Ediabas;
using S54VanosTester.Vanos;

namespace S54VanosTester.Diagnostics
{
    /// <summary>
    /// Owns the EDIABAS client and a single worker thread. Every EDIABAS call is marshalled onto
    /// that thread because the api32.dll is thread-affine. Public methods return Tasks so the UI
    /// can await them without blocking the message pump.
    /// </summary>
    public sealed class DiagnosticsSession : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly Thread _worker;

        private EdiabasClient _client;
        private TemperatureReader _temperatureReader;
        private VanosTester _vanosTester;

        private Timer _liveTimer;
        private volatile bool _livePolling;

        /// <summary>Raised on a worker callback whenever a fresh temperature sample is read.</summary>
        public event Action<TemperatureSample> SampleReceived;

        /// <summary>Raised for human-readable status/log lines.</summary>
        public event Action<string> Log;

        public bool IsConnected { get; private set; }
        public string ConnectedPort { get; private set; }

        public DiagnosticsSession(AppSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "EDIABAS-Worker" };
            _worker.Start();
        }

        private void WorkerLoop()
        {
            foreach (Action work in _queue.GetConsumingEnumerable())
            {
                try { work(); }
                catch (Exception ex) { OnLog($"Worker error: {ex.Message}"); }
            }
        }

        private Task<T> Enqueue<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            _queue.Add(() =>
            {
                try { tcs.SetResult(func()); }
                catch (Exception ex) { tcs.SetException(ex); }
            });
            return tcs.Task;
        }

        private Task Enqueue(Action action) => Enqueue<bool>(() => { action(); return true; });

        /// <summary>
        /// Auto-detect the COM port and connect. Tries the most diagnostic-looking ports first and
        /// confirms each by running the identification job against the DME.
        /// </summary>
        public Task<string> ConnectAutoAsync()
        {
            return Enqueue(() =>
            {
                var ports = ComPortFinder.Enumerate();
                if (ports.Count == 0)
                    throw new InvalidOperationException("No serial (COM) ports were found on this machine.");

                foreach (ComPortInfo port in ports)
                {
                    OnLog($"Trying {port}...");
                    if (TryConnect(port.Port))
                    {
                        IsConnected = true;
                        ConnectedPort = port.Port;
                        OnLog($"Connected to MSS54 on {port.Port}.");
                        return port.Port;
                    }

                    // Reset before trying the next candidate.
                    DisposeClient();
                }

                throw new InvalidOperationException(
                    "Could not reach the MSS54 DME on any COM port. Check the cable, ignition (KL15 on) " +
                    "and that the job/ECU names in appsettings.json match your SGBD.");
            });
        }

        /// <summary>Connect to a specific, user-chosen COM port.</summary>
        public Task ConnectAsync(string comPort)
        {
            return Enqueue(() =>
            {
                if (!TryConnect(comPort))
                {
                    DisposeClient();
                    throw new InvalidOperationException($"Failed to reach the MSS54 DME on {comPort}.");
                }

                IsConnected = true;
                ConnectedPort = comPort;
                OnLog($"Connected to MSS54 on {comPort}.");
            });
        }

        private bool TryConnect(string comPort)
        {
            // Persist the port to EDIABAS.INI so it is honoured by every EDIABAS build.
            if (!EdiabasConfig.ApplyInterface(comPort, _settings.EdiabasInterface))
                OnLog("Warning: EDIABAS.INI not found; relying on the apiInitExt configuration string.");

            _client = new EdiabasClient();
            try
            {
                _client.Initialise(comPort, _settings.EdiabasInterface);
            }
            catch (EdiabasException ex)
            {
                OnLog($"  init failed on {comPort}: {ex.Message}");
                return false;
            }

            // Verify the link actually reaches the DME by running the identification job.
            bool reachable = _client.TryRunJob(_settings.Ecu, _settings.IdentJob, out _);
            if (!reachable)
                OnLog($"  no response from {_settings.Ecu} on {comPort}.");

            if (reachable)
            {
                _temperatureReader = new TemperatureReader(_client, _settings);
                _vanosTester = new VanosTester(_client, _settings);
            }

            return reachable;
        }

        /// <summary>Stop live polling and tear down the EDIABAS link, keeping the worker alive for reconnection.</summary>
        public Task DisconnectAsync()
        {
            StopLive();
            return Enqueue(() =>
            {
                DisposeClient();
                ConnectedPort = null;
                OnLog("Disconnected.");
            });
        }

        /// <summary>Run the VANOS test and return its report.</summary>
        public Task<VanosTestReport> RunVanosTestAsync()
        {
            return Enqueue(() =>
            {
                EnsureConnected();
                bool wasPolling = _livePolling;
                if (wasPolling) PauseLive();

                try
                {
                    OnLog("Running VANOS test...");
                    VanosTestReport report = _vanosTester.Run();
                    OnLog(report.Summary);
                    return report;
                }
                finally
                {
                    if (wasPolling) ResumeLive();
                }
            });
        }

        // --- Live temperature polling ----------------------------------------------------

        public void StartLive()
        {
            _livePolling = true;
            int interval = Math.Max(100, _settings.LivePollIntervalMs);
            _liveTimer = new Timer(_ => PollOnce(), null, 0, interval);
            OnLog("Live data acquisition started.");
        }

        public void StopLive()
        {
            _livePolling = false;
            _liveTimer?.Dispose();
            _liveTimer = null;
            OnLog("Live data acquisition stopped.");
        }

        private void PauseLive()
        {
            _liveTimer?.Dispose();
            _liveTimer = null;
        }

        private void ResumeLive()
        {
            int interval = Math.Max(100, _settings.LivePollIntervalMs);
            _liveTimer = new Timer(_ => PollOnce(), null, 0, interval);
        }

        private void PollOnce()
        {
            if (!_livePolling)
                return;

            // Marshal onto the worker thread so we never call EDIABAS concurrently with a test.
            _queue.Add(() =>
            {
                if (!_livePolling || !IsConnected)
                    return;
                try
                {
                    TemperatureSample sample = _temperatureReader.Read();
                    SampleReceived?.Invoke(sample);
                }
                catch (Exception ex)
                {
                    OnLog($"Live read error: {ex.Message}");
                }
            });
        }

        private void EnsureConnected()
        {
            if (!IsConnected || _client == null)
                throw new InvalidOperationException("Not connected. Connect to the vehicle first.");
        }

        private void DisposeClient()
        {
            try { _client?.Dispose(); } catch { /* ignore */ }
            _client = null;
            _temperatureReader = null;
            _vanosTester = null;
            IsConnected = false;
        }

        private void OnLog(string message) => Log?.Invoke(message);

        public void Dispose()
        {
            StopLive();
            // Tear down the client on the worker thread, then stop the worker.
            try
            {
                Enqueue(() => DisposeClient()).Wait(2000);
            }
            catch { /* ignore */ }

            _queue.CompleteAdding();
        }
    }
}
