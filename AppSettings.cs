using System;
using System.IO;
using System.Web.Script.Serialization;

namespace S54VanosTester
{
    /// <summary>
    /// Names of the ECU, jobs and results used to talk to the MSS54 DME. These default to the
    /// commonly referenced MSS54 identifiers but are externalised to <c>appsettings.json</c> so
    /// they can be matched to your exact SGBD (inspect MSS54.PRG with EDIABAS Tool32 to confirm).
    /// </summary>
    public sealed class AppSettings
    {
        // The SGBD / ECU name. "MSS54" for E46 M3 / Z3M / Z4M (S54). Some cars use "MSS54HP".
        public string Ecu { get; set; } = "MSS54";

        public string EdiabasInterface { get; set; } = "STD:OBD";

        // --- Identification (used to verify a COM port actually reaches the DME) ---
        public string IdentJob { get; set; } = "IDENT";

        // --- VANOS test ---
        // The VANOS function test is a fixed actuation sequence (raise idle, drive the intake/exhaust
        // cams to their end stops, run the leak and adjustment-time tests) implemented in VanosTester
        // against the mss54ds0 SGBD, so the job/result names live in code rather than here.

        // --- Live temperatures ---
        // Some SGBDs return coolant + oil from a single job; others (e.g. mss54ds0) expose a
        // separate status job per sensor. CoolantJob/OilJob take priority when set; if left empty
        // the reader falls back to running TemperatureJob once and reading both results from it.
        public string TemperatureJob { get; set; } = "STATUS_TEMPERATUR";
        public string CoolantJob { get; set; } = "";
        public string OilJob { get; set; } = "";
        public string CoolantResult { get; set; } = "STAT_KUEHLMITTELTEMPERATUR_WERT";
        public string OilResult { get; set; } = "STAT_OELTEMPERATUR_WERT";

        // Live polling interval, milliseconds.
        public int LivePollIntervalMs { get; set; } = 500;

        public static AppSettings Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = new JavaScriptSerializer().Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // Fall back to defaults if the file is missing or malformed.
            }

            return new AppSettings();
        }
    }
}
