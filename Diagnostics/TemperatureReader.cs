using System;
using System.Collections.Generic;
using S54VanosTester.Ediabas;

namespace S54VanosTester.Diagnostics
{
    /// <summary>A single coolant/oil temperature sample.</summary>
    public struct TemperatureSample
    {
        public DateTime Time;
        public double? CoolantC;
        public double? OilC;
    }

    /// <summary>
    /// Polls the MSS54 temperature status job and extracts coolant and oil temperatures for the
    /// live data view.
    /// </summary>
    public sealed class TemperatureReader
    {
        private readonly EdiabasClient _client;
        private readonly AppSettings _settings;

        public TemperatureReader(EdiabasClient client, AppSettings settings)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Read one sample. Returns the sample with null fields if a value is unavailable.</summary>
        public TemperatureSample Read()
        {
            var sample = new TemperatureSample { Time = DateTime.Now };

            // Per-sensor jobs take priority; fall back to the combined job for each that is unset.
            string coolantJob = FirstNonEmpty(_settings.CoolantJob, _settings.TemperatureJob);
            string oilJob = FirstNonEmpty(_settings.OilJob, _settings.TemperatureJob);

            if (!string.IsNullOrEmpty(coolantJob) &&
                string.Equals(coolantJob, oilJob, StringComparison.OrdinalIgnoreCase))
            {
                // A single job returns both temperatures.
                if (_client.TryRunJob(_settings.Ecu, coolantJob, out List<EdiabasResultSet> sets))
                {
                    foreach (EdiabasResultSet set in sets)
                    {
                        if (!sample.CoolantC.HasValue)
                            sample.CoolantC = set.GetDouble(_settings.CoolantResult);
                        if (!sample.OilC.HasValue)
                            sample.OilC = set.GetDouble(_settings.OilResult);
                    }
                }
                return sample;
            }

            // Separate job per sensor (e.g. mss54ds0).
            sample.CoolantC = ReadValue(coolantJob, _settings.CoolantResult);
            sample.OilC = ReadValue(oilJob, _settings.OilResult);
            return sample;
        }

        /// <summary>Run a single job and pull one numeric result from any of its result sets.</summary>
        private double? ReadValue(string job, string resultName)
        {
            if (string.IsNullOrEmpty(job) || string.IsNullOrEmpty(resultName))
                return null;
            if (!_client.TryRunJob(_settings.Ecu, job, out List<EdiabasResultSet> sets))
                return null;

            foreach (EdiabasResultSet set in sets)
            {
                double? value = set.GetDouble(resultName);
                if (value.HasValue)
                    return value;
            }
            return null;
        }

        private static string FirstNonEmpty(string a, string b)
            => !string.IsNullOrEmpty(a) ? a : b;
    }
}
