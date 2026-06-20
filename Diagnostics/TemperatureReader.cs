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

            if (!_client.TryRunJob(_settings.Ecu, _settings.TemperatureJob, out List<EdiabasResultSet> sets))
                return sample;

            foreach (EdiabasResultSet set in sets)
            {
                if (!sample.CoolantC.HasValue)
                    sample.CoolantC = set.GetDouble(_settings.CoolantResult);
                if (!sample.OilC.HasValue)
                    sample.OilC = set.GetDouble(_settings.OilResult);
            }

            return sample;
        }
    }
}
