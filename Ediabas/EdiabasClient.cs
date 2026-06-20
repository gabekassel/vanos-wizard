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

        public EdiabasException(string message, int errorCode)
            : base($"{message} (EDIABAS {errorCode}: {EdiabasApi.apiErrorText()})")
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
            bool ok = EdiabasApi.apiInitExt(ediabasInterface, "S54", applicationId, configuration);
            if (!ok)
            {
                // Fall back to plain init that relies entirely on EDIABAS.INI.
                ok = EdiabasApi.apiInit();
            }

            if (!ok)
                throw new EdiabasException("apiInit failed", EdiabasApi.apiErrorCode());

            _initialised = true;
        }

        /// <summary>
        /// Run a job and return every result set. Throws <see cref="EdiabasException"/> on failure.
        /// </summary>
        public List<EdiabasResultSet> RunJob(string ecu, string job, string parameters = "", string resultFilter = "")
        {
            EnsureInitialised();

            bool started = EdiabasApi.apiJob(ecu, job, parameters ?? string.Empty, resultFilter ?? string.Empty);
            if (!started)
                throw new EdiabasException($"apiJob '{job}' could not be started", EdiabasApi.apiErrorCode());

            // Wait for completion.
            int state;
            while ((state = EdiabasApi.apiState()) == EdiabasApi.APIBUSY)
                Thread.Sleep(2);

            if (state == EdiabasApi.APIERROR)
                throw new EdiabasException($"Job '{job}' failed", EdiabasApi.apiErrorCode());

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

            EdiabasApi.apiResultSets(out ushort setCount);

            // Set 0 is the EDIABAS "system" set (job status). Real data starts at set 1.
            for (ushort set = 0; set <= setCount; set++)
            {
                if (!EdiabasApi.apiResultNumber(out ushort count, set))
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

        private static string ReadName(ushort index, ushort set)
        {
            var buffer = new byte[256];
            return EdiabasApi.apiResultName(buffer, index, set) ? BufferToString(buffer) : null;
        }

        private static EdiabasResultValue ReadValue(string name, ushort set)
        {
            var value = new EdiabasResultValue { Name = name };

            if (!EdiabasApi.apiResultFormat(out int format, name, set))
            {
                value.Format = EdiabasApi.APIFORMAT_TEXT;
                value.Value = null;
                return value;
            }

            value.Format = format;

            switch (format)
            {
                case EdiabasApi.APIFORMAT_REAL:
                    value.Value = EdiabasApi.apiResultReal(out double real, name, set) ? real : (object)null;
                    break;

                case EdiabasApi.APIFORMAT_CHAR:
                case EdiabasApi.APIFORMAT_BYTE:
                case EdiabasApi.APIFORMAT_INTEGER:
                case EdiabasApi.APIFORMAT_WORD:
                case EdiabasApi.APIFORMAT_LONG:
                case EdiabasApi.APIFORMAT_DWORD:
                    value.Value = EdiabasApi.apiResultInt(out int integer, name, set) ? integer : (object)null;
                    break;

                default: // TEXT / BINARY
                    var buffer = new byte[1024];
                    value.Value = EdiabasApi.apiResultText(buffer, name, set, string.Empty)
                        ? BufferToString(buffer)
                        : null;
                    break;
            }

            return value;
        }

        private static string BufferToString(byte[] buffer)
        {
            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
                length = buffer.Length;
            return Encoding.ASCII.GetString(buffer, 0, length).Trim();
        }

        private void EnsureInitialised()
        {
            if (!_initialised)
                throw new InvalidOperationException("EDIABAS is not initialised. Call Initialise() first.");
        }

        public void Dispose()
        {
            if (_initialised)
            {
                EdiabasApi.apiEnd();
                _initialised = false;
            }
        }
    }
}
