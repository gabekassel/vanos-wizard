using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace S54VanosTester.Ediabas
{
    /// <summary>
    /// Locates and edits EDIABAS.INI so the requested interface and OBD COM port are used.
    /// Writing the port to the INI guarantees the correct port is used even on EDIABAS builds
    /// that ignore the configuration string passed to <c>apiInitExt</c>.
    /// </summary>
    public static class EdiabasConfig
    {
        /// <summary>
        /// Attempt to find EDIABAS.INI using (in order): the EDIABAS env var, the registry,
        /// and well-known install locations.
        /// </summary>
        public static string FindEdiabasIni()
        {
            var candidates = new List<string>();

            string env = Environment.GetEnvironmentVariable("EDIABAS");
            if (!string.IsNullOrEmpty(env))
                candidates.Add(Path.Combine(env, "BIN", "EDIABAS.INI"));

            try
            {
                using (RegistryKey key = RegistryKey
                           .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                           .OpenSubKey(@"SOFTWARE\EDIABAS"))
                {
                    string path = key?.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(path))
                        candidates.Add(Path.Combine(path, "BIN", "EDIABAS.INI"));
                }
            }
            catch
            {
                // Registry access is best-effort.
            }

            candidates.Add(@"C:\EDIABAS\BIN\EDIABAS.INI");
            candidates.Add(@"C:\Program Files (x86)\EDIABAS\BIN\EDIABAS.INI");
            candidates.Add(@"C:\Program Files\EDIABAS\BIN\EDIABAS.INI");

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Set the interface and OBD COM port in EDIABAS.INI. Existing keys are updated in place;
        /// missing keys are appended. Returns false if the INI could not be located.
        /// </summary>
        public static bool ApplyInterface(string comPort, string ediabasInterface = "STD:OBD")
        {
            string iniPath = FindEdiabasIni();
            if (iniPath == null)
                return false;

            string[] lines = File.ReadAllLines(iniPath);
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Interface"] = ediabasInterface,
                ["ObdComPort"] = comPort,
                ["Port"] = comPort,
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                int eq = trimmed.IndexOf('=');
                if (eq <= 0 || trimmed.StartsWith(";") || trimmed.StartsWith("["))
                    continue;

                string key = trimmed.Substring(0, eq).Trim();
                if (settings.TryGetValue(key, out string newValue))
                {
                    lines[i] = $"{key} = {newValue}";
                    seen.Add(key);
                }
            }

            var toAppend = new List<string>();
            foreach (var kvp in settings)
            {
                if (!seen.Contains(kvp.Key))
                    toAppend.Add($"{kvp.Key} = {kvp.Value}");
            }

            if (toAppend.Count > 0)
            {
                var combined = new List<string>(lines);
                combined.Add(string.Empty);
                combined.AddRange(toAppend);
                lines = combined.ToArray();
            }

            File.WriteAllLines(iniPath, lines);
            return true;
        }
    }
}
