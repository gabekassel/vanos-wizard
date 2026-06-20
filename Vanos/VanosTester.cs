using System;
using System.Collections.Generic;
using S54VanosTester.Ediabas;

namespace S54VanosTester.Vanos
{
    /// <summary>
    /// Runs the S54 (MSS54) VANOS test through EDIABAS and flattens every returned result into a
    /// report that the UI can display. The test job activates/adjusts the intake and exhaust VANOS
    /// units; the optional status job reads back the measured adjustment values.
    /// </summary>
    public sealed class VanosTester
    {
        private readonly EdiabasClient _client;
        private readonly AppSettings _settings;

        public VanosTester(EdiabasClient client, AppSettings settings)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Execute the VANOS test. Runs the test/actuation job and then, if configured, the status
        /// job, collecting all results. Throws <see cref="EdiabasException"/> if the test job fails.
        /// </summary>
        public VanosTestReport Run()
        {
            var report = new VanosTestReport();

            List<EdiabasResultSet> testSets = _client.RunJob(
                _settings.Ecu,
                _settings.VanosTestJob,
                _settings.VanosTestParameters ?? string.Empty);

            AddSets(report, testSets);

            // Read back the measured VANOS adjustment values, if a status job is configured.
            if (!string.IsNullOrWhiteSpace(_settings.VanosStatusJob))
            {
                if (_client.TryRunJob(_settings.Ecu, _settings.VanosStatusJob, out var statusSets))
                    AddSets(report, statusSets);
            }

            report.Completed = true;
            report.Summary = BuildSummary(report);
            return report;
        }

        private static void AddSets(VanosTestReport report, List<EdiabasResultSet> sets)
        {
            if (sets == null)
                return;

            foreach (EdiabasResultSet set in sets)
            {
                foreach (var kvp in set.Values)
                {
                    EdiabasResultValue value = kvp.Value;

                    // Skip EDIABAS housekeeping results from the system set.
                    if (IsHousekeeping(value.Name))
                        continue;

                    report.Items.Add(new VanosResultItem(
                        name: value.Name,
                        value: value.AsString(),
                        unit: GuessUnit(value.Name),
                        status: GuessStatus(value)));
                }
            }
        }

        private static bool IsHousekeeping(string name)
        {
            if (string.IsNullOrEmpty(name))
                return true;
            // Common EDIABAS system-set entries that aren't measurement data.
            return name.StartsWith("OBJECT", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("SAETZE", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("JOBNAME", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("VARIANTE", StringComparison.OrdinalIgnoreCase)
                   || name.StartsWith("JOB_STATUS", StringComparison.OrdinalIgnoreCase);
        }

        private static string GuessUnit(string name)
        {
            string n = name.ToUpperInvariant();
            if (n.Contains("WINKEL") || n.Contains("ANGLE")) return "°";
            if (n.Contains("TEMP")) return "°C";
            if (n.Contains("DREHZAHL") || n.Contains("RPM")) return "1/min";
            if (n.Contains("SPANNUNG") || n.Contains("VOLT")) return "V";
            if (n.Contains("STROM") || n.Contains("CURRENT")) return "A";
            if (n.Contains("DRUCK") || n.Contains("PRESS")) return "bar";
            return string.Empty;
        }

        private static string GuessStatus(EdiabasResultValue value)
        {
            string text = value.AsString()?.ToUpperInvariant() ?? string.Empty;
            if (text.Contains("OK") || text.Contains("IO") || text.Contains("BESTANDEN") || text.Contains("PASS"))
                return "OK";
            if (text.Contains("NIO") || text.Contains("FEHLER") || text.Contains("FAIL") || text.Contains("NOK"))
                return "FAIL";
            return string.Empty;
        }

        private static string BuildSummary(VanosTestReport report)
        {
            int failures = 0;
            foreach (var item in report.Items)
            {
                if (string.Equals(item.Status, "FAIL", StringComparison.OrdinalIgnoreCase))
                    failures++;
            }

            if (report.Items.Count == 0)
                return "VANOS test completed but returned no measurement results. Verify the job/result names in appsettings.json against your MSS54 SGBD.";

            return failures == 0
                ? $"VANOS test completed. {report.Items.Count} result(s) read, no failures reported."
                : $"VANOS test completed with {failures} failure(s) out of {report.Items.Count} result(s).";
        }
    }
}
