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
        // The job that performs the VANOS function/adjustment test.
        public string VanosTestJob { get; set; } = "STEUERN_VANOS_TEST";
        public string VanosTestParameters { get; set; } = "";
        // Optional status job to read the VANOS adjustment values after the test.
        public string VanosStatusJob { get; set; } = "STATUS_VANOS";

        // --- Live temperatures ---
        public string TemperatureJob { get; set; } = "STATUS_TEMPERATUR";
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
