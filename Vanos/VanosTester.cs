using System;
using System.Collections.Generic;
using System.Globalization;
using S54VanosTester.Ediabas;

namespace S54VanosTester.Vanos
{
    /// <summary>
    /// Runs the S54 (MSS54) VANOS function test the way ISTA does: raise idle to the fixed test
    /// speed, drive the intake (Einlass / EVANOS) and exhaust (Auslass / AVANOS) cams to their
    /// advanced (frueh) and retarded (spaet) end stops, run the leak (Dichtheit) test and the
    /// adjustment-time (Verstellzeit) test, then report the measured values and a pass/fail verdict.
    ///
    /// <para>
    /// The engine must be running and warm — these jobs physically actuate the VANOS units. The job
    /// and result names below were read directly from the mss54ds0 SGBD.
    /// </para>
    /// </summary>
    public sealed class VanosTester
    {
        private readonly EdiabasClient _client;
        private readonly AppSettings _settings;
        private readonly Action<string> _log;

        // The engine must actually be running for the cams to move.
        private const int MinRunningRpm = 400;

        public VanosTester(EdiabasClient client, AppSettings settings, Action<string> log = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log ?? (_ => { });
        }

        public VanosTestReport Run()
        {
            var report = new VanosTestReport();
            string ecu = _settings.Ecu;

            // Precondition: refuse to actuate the cams unless the engine is running.
            double? rpm = ReadEngineRpm(ecu);
            if (!rpm.HasValue || rpm.Value < MinRunningRpm)
            {
                throw new InvalidOperationException(
                    "Start the engine and let it reach operating temperature before running the VANOS " +
                    "test — it raises idle and actuates the cams. " +
                    (rpm.HasValue ? $"Engine speed is only {rpm.Value:0} rpm." : "The engine does not appear to be running."));
            }
            _log($"Engine running at {rpm.Value:0} rpm. Raising idle for the VANOS test...");

            // Raise idle to the fixed VANOS-test speed.
            _client.TryRunJob(ecu, "STEUERN_LLS_TESTDREHZAHL", out _);

            // 1. Position reached at the end stops.
            _log("Rocking intake cam (advanced / retarded)...");
            AddMeasure(report, "Position reached, advanced inlet", ecu, "STEUERN_EVANOS1_FRUEHANSCHLAG", "EVAN_ISTWERT", "EVAN_ISTWERT_EINH");
            AddMeasure(report, "Position reached, retarded inlet", ecu, "STEUERN_EVANOS1_SPAETANSCHLAG", "EVAN_ISTWERT", "EVAN_ISTWERT_EINH");
            _log("Rocking exhaust cam (advanced / retarded)...");
            AddMeasure(report, "Position reached, advanced exhaust", ecu, "STEUERN_AVANOS1_FRUEHANSCHLAG", "AVAN_ISTWERT", "AVAN_ISTWERT_EINH");
            AddMeasure(report, "Position reached, retarded exhaust", ecu, "STEUERN_AVANOS1_SPAETANSCHLAG", "AVAN_ISTWERT", "AVAN_ISTWERT_EINH");

            // 2. Leak (tightness) test — deviation plus the SGBD's own pass/fail status.
            _log("Running leak (tightness) test...");
            AddMeasure(report, "Deviation, leak test, inlet", ecu, "STEUERN_EVANOS1_DICHTHEIT", "EVAN_ISTWERT", "EVAN_ISTWERT_EINH", "EVAN_STATUS");
            AddMeasure(report, "Deviation, leak test, exhaust", ecu, "STEUERN_AVANOS1_DICHTHEIT", "AVAN_ISTWERT", "AVAN_ISTWERT_EINH", "AVAN_STATUS");

            // 3. Adjustment times (advanced + retarded come from one job each).
            _log("Measuring adjustment times...");
            AddTimes(report, "inlet", ecu, "STEUERN_EVANOS1_VERSTELLZEIT", "EVAN_VERSTELLZEIT_FRUEH", "EVAN_VERSTELLZEIT_SPAET", "EVAN_VERSTELLZEIT_FRUEH_EINH");
            AddTimes(report, "exhaust", ecu, "STEUERN_AVANOS1_VERSTELLZEIT", "AVAN_VERSTELLZEIT_FRUEH", "AVAN_VERSTELLZEIT_SPAET", "AVAN_VERSTELLZEIT_FRUEH_EINH");

            report.Completed = true;
            report.Summary = BuildSummary(report);
            _log("VANOS test complete. Idle returns to normal shortly (blip the throttle if needed).");
            return report;
        }

        // --- Steps -----------------------------------------------------------------------

        /// <summary>Run one actuation job and add a single measured row (value + unit, optional status).</summary>
        private void AddMeasure(VanosTestReport report, string label, string ecu, string job,
                                string valueResult, string unitResult, string statusResult = null)
        {
            if (!_client.TryRunJob(ecu, job, out List<EdiabasResultSet> sets))
            {
                _log($"  {job}: no response.");
                report.Items.Add(new VanosResultItem(label, "—", string.Empty, "FAIL"));
                return;
            }

            string value = FormatNumber(sets, valueResult, "0.0");
            string unit = MapUnit(FindString(sets, unitResult));
            string status = statusResult != null ? MapStatus(FindString(sets, statusResult)) : string.Empty;
            report.Items.Add(new VanosResultItem(label, value ?? "—", unit, status));
        }

        /// <summary>Run an adjustment-time job which returns both advanced and retarded times.</summary>
        private void AddTimes(VanosTestReport report, string side, string ecu, string job,
                              string advResult, string retResult, string unitResult)
        {
            if (!_client.TryRunJob(ecu, job, out List<EdiabasResultSet> sets))
            {
                _log($"  {job}: no response.");
                report.Items.Add(new VanosResultItem($"Advanced adjustment time, {side}", "—", "ms", "FAIL"));
                report.Items.Add(new VanosResultItem($"Retarded adjustment time, {side}", "—", "ms", "FAIL"));
                return;
            }

            string unit = MapUnit(FindString(sets, unitResult)) ?? "ms";
            report.Items.Add(new VanosResultItem($"Advanced adjustment time, {side}", FormatNumber(sets, advResult, "0") ?? "—", unit, string.Empty));
            report.Items.Add(new VanosResultItem($"Retarded adjustment time, {side}", FormatNumber(sets, retResult, "0") ?? "—", unit, string.Empty));
        }

        private double? ReadEngineRpm(string ecu)
        {
            if (!_client.TryRunJob(ecu, "STATUS_MOTORDREHZAHL", out List<EdiabasResultSet> sets))
                return null;

            // Result name varies; take the first numeric result that looks like an engine-speed value.
            foreach (EdiabasResultSet set in sets)
            {
                foreach (var kvp in set.Values)
                {
                    if (kvp.Key.IndexOf("DREHZAHL", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        kvp.Value.TryGetDouble(out double d))
                        return d;
                }
            }
            return null;
        }

        // --- Helpers ---------------------------------------------------------------------

        private static string FormatNumber(List<EdiabasResultSet> sets, string name, string format)
        {
            foreach (EdiabasResultSet set in sets)
            {
                double? d = set.GetDouble(name);
                if (d.HasValue)
                    return d.Value.ToString(format, CultureInfo.InvariantCulture);
            }
            return null;
        }

        private static string FindString(List<EdiabasResultSet> sets, string name)
        {
            if (name == null)
                return null;
            foreach (EdiabasResultSet set in sets)
            {
                string s = set.GetString(name);
                if (!string.IsNullOrEmpty(s))
                    return s;
            }
            return null;
        }

        // ISTA shows crankshaft degrees as "°cr"; the SGBD reports them as "Grad KW" (Kurbelwelle).
        private static string MapUnit(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            string u = raw.ToUpperInvariant();
            if (u.Contains("KW") || u.Contains("KURBEL"))
                return "°cr";
            if (u.Contains("GRAD C") || u == "C")
                return "°C";
            if (u.Contains("MS"))
                return "ms";
            return raw;
        }

        private static string MapStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            string u = raw.ToUpperInvariant();
            if (u.Contains("N.I.O") || u.Contains("NIO") || u.Contains("NICHT") || u.Contains("FEHLER") || u.Contains("FAIL"))
                return "FAIL";
            if (u.Contains("I.O") || u == "IO" || u.Contains("OK") || u.Contains("BESTANDEN"))
                return "OK";
            return raw;
        }

        private static string BuildSummary(VanosTestReport report)
        {
            int fails = 0;
            bool anyData = false;
            foreach (var item in report.Items)
            {
                if (string.Equals(item.Status, "FAIL", StringComparison.OrdinalIgnoreCase))
                    fails++;
                if (item.Value != "—")
                    anyData = true;
            }

            if (!anyData)
                return "VANOS test returned no data. Confirm the engine is running and warm, then retry.";

            return fails == 0
                ? "All actual values conform to specified values. VANOS system O.K."
                : $"VANOS test complete — {fails} measurement(s) reported a fault. Review the highlighted rows.";
        }
    }
}
