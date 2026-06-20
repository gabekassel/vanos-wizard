using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace S54VanosTester.Ediabas
{
    /// <summary>
    /// Thrown when an EDIABAS API call fails. Carries the EDIABAS error code/text.
    /// </summary>
    public sealed class EdiabasException : Exception
    {
        public int ErrorCode { get; }

        public EdiabasException(string message, int errorCode, string errorText)
            : base($"{message} (EDIABAS {errorCode}: {errorText})")
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Managed, thread-affine wrapper around the EDIABAS API. All calls into api32.dll must
    /// happen on the same thread; callers should drive this from a dedicated worker thread.
    /// </summary>
    public sealed class EdiabasClient : IDisposable
    {
        private bool _initialised;

        // Instance handle returned by apiInit/apiInitExt. This api32.dll uses the handle-based API:
        // every call takes this handle as its first argument (see EdiabasApi).
        private uint _handle;

        // Upper bound on how long we wait for a single job to finish. VANOS actuation jobs take a
        // few seconds; this just guards against a job that never leaves the BUSY state.
        private static readonly TimeSpan JobTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Initialise EDIABAS for the given interface and COM port. The COM port is also written
        /// to EDIABAS.INI by <see cref="EdiabasConfig"/> before this is called so it works on every
        /// EDIABAS build, regardless of whether the configuration string is honoured.
        /// </summary>
        /// <param name="comPort">e.g. "COM3"</param>
        /// <param name="ediabasInterface">EDIABAS interface name, e.g. "STD:OBD".</param>
        /// <param name="applicationId">Free-form application identifier reported to EDIABAS.</param>
        public void Initialise(string comPort, string ediabasInterface = "STD:OBD", string applicationId = "S54VANOS")
        {
            if (_initialised)
                return;

            string configuration = $"Interface={ediabasInterface};ObdComPort={comPort}";
            bool ok = EdiabasApi.apiInitExt(out _handle, ediabasInterface, "S54", applicationId, configuration);
            if (!ok)
            {
                // Fall back to plain init that relies entirely on EDIABAS.INI.
                ok = EdiabasApi.apiInit(out _handle);
            }

            if (!ok)
                throw Error("apiInit failed");

            _initialised = true;
        }

        /// <summary>
        /// Run a job and return every result set. Throws <see cref="EdiabasException"/> on failure.
        /// </summary>
        public List<EdiabasResultSet> RunJob(string ecu, string job, string parameters = "", string resultFilter = "")
        {
            EnsureInitialised();

            // apiJob returns the job id (non-zero when the job started).
            uint jobId = EdiabasApi.apiJob(_handle, ecu, job, parameters ?? string.Empty, resultFilter ?? string.Empty);
            if (jobId == 0)
                throw Error($"apiJob '{job}' could not be started");

            // Wait for completion, with a timeout so a stuck job can't hang the worker thread.
            int state;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while ((state = EdiabasApi.apiState(_handle)) == EdiabasApi.APIBUSY)
            {
                if (sw.Elapsed > JobTimeout)
                    throw Error($"Job '{job}' timed out");
                Thread.Sleep(2);
            }

            if (state == EdiabasApi.APIERROR)
                throw Error($"Job '{job}' failed");

            return ReadAllSets();
        }

        /// <summary>
        /// Tries to run a job, returning false (instead of throwing) when it fails. Used by the
        /// COM-port probe where a failure is an expected outcome.
        /// </summary>
        public bool TryRunJob(string ecu, string job, out List<EdiabasResultSet> sets, string parameters = "", string resultFilter = "")
        {
            sets = null;
            try
            {
                sets = RunJob(ecu, job, parameters, resultFilter);
                return true;
            }
            catch (EdiabasException)
            {
                return false;
            }
        }

        private List<EdiabasResultSet> ReadAllSets()
        {
            var output = new List<EdiabasResultSet>();

            EdiabasApi.apiResultSets(_handle, out ushort setCount);

            // Set 0 is the EDIABAS "system" set (job status). Real data starts at set 1.
            for (ushort set = 0; set <= setCount; set++)
            {
                if (!EdiabasApi.apiResultNumber(_handle, out ushort count, set))
                    continue;

                var resultSet = new EdiabasResultSet();
                for (ushort index = 1; index <= count; index++)
                {
                    string name = ReadName(index, set);
                    if (string.IsNullOrEmpty(name))
                        continue;

                    resultSet.Add(ReadValue(name, set));
                }

                output.Add(resultSet);
            }

            return output;
        }

        private string ReadName(ushort index, ushort set)
        {
            var buffer = new byte[256];
            return EdiabasApi.apiResultName(_handle, buffer, index, set) ? BufferToString(buffer) : null;
        }

        private EdiabasResultValue ReadValue(string name, ushort set)
        {
            // Read every result as its EDIABAS-formatted text. This api32.dll reports unusual format
            // codes (reals come back as 8, not the documented 6), and the formatted text already
            // carries the SGBD's scaling and decimals, so the text form is both simpler and more
            // reliable than typed reads. EdiabasResultValue.TryGetDouble parses it back to a number
            // when a caller needs one.
            var value = new EdiabasResultValue { Name = name };
            EdiabasApi.apiResultFormat(_handle, out int format, name, set);
            value.Format = format;

            var buffer = new byte[1024];
            value.Value = EdiabasApi.apiResultText(_handle, buffer, name, set, string.Empty)
                ? BufferToString(buffer)
                : null;

            return value;
        }

        private static string BufferToString(byte[] buffer)
        {
            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
                length = buffer.Length;
            return Encoding.ASCII.GetString(buffer, 0, length).Trim();
        }

        // Build an exception carrying the current EDIABAS error code/text for this instance.
        private EdiabasException Error(string message)
            => new EdiabasException(message, EdiabasApi.apiErrorCode(_handle), EdiabasApi.apiErrorText(_handle));

        private void EnsureInitialised()
        {
            if (!_initialised)
                throw new InvalidOperationException("EDIABAS is not initialised. Call Initialise() first.");
        }

        public void Dispose()
        {
            if (_initialised)
            {
                EdiabasApi.apiEnd(_handle);
                _initialised = false;
            }
        }
    }
}
