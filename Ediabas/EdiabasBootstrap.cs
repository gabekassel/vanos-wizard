using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace S54VanosTester.Ediabas
{
    /// <summary>
    /// Makes the application run against a <b>bundled, portable</b> EDIABAS runtime that lives in an
    /// "EDIABAS" folder next to the executable, so no separate EDIABAS installation is required on
    /// the target machine.
    ///
    /// <para>Expected layout (you supply these proprietary BMW files from your own licensed copy):</para>
    /// <code>
    ///   &lt;appdir&gt;\EDIABAS\BIN\api32.dll   (+ engine and interface DLLs, EDIABAS.INI)
    ///   &lt;appdir&gt;\EDIABAS\ECU\MSS54.PRG   (+ group/shared SGBD files)
    /// </code>
    ///
    /// <para>
    /// <see cref="Prepare"/> must run once at process start, before any EDIABAS API call. If no
    /// bundle is present it does nothing and the app falls back to an installed EDIABAS.
    /// </para>
    /// </summary>
    public static class EdiabasBootstrap
    {
        // Adds a directory to the native DLL search path used by LoadLibrary, so api32.dll (and the
        // sibling engine/interface DLLs it loads by name) resolve from the bundled BIN folder.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static string BundledRoot =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EDIABAS");

        public static string BundledBin => Path.Combine(BundledRoot, "BIN");
        public static string BundledEcu => Path.Combine(BundledRoot, "ECU");

        /// <summary>True when a portable api32.dll is bundled next to the executable.</summary>
        public static bool IsBundlePresent =>
            File.Exists(Path.Combine(BundledBin, "api32.dll"));

        /// <summary>True after <see cref="Prepare"/> activated a bundled runtime.</summary>
        public static bool IsPortable { get; private set; }

        /// <summary>
        /// Point the process at the bundled EDIABAS runtime: set the EDIABAS environment variable,
        /// add BIN to the native DLL search path, and make sure EDIABAS.INI uses the bundled ECU
        /// folder. Safe to call when no bundle exists. Returns true if a bundle was activated.
        /// </summary>
        public static bool Prepare()
        {
            if (!IsBundlePresent)
                return false;

            // 1. EDIABAS resolves its config/ECU folders from this environment variable.
            Environment.SetEnvironmentVariable("EDIABAS", BundledRoot, EnvironmentVariableTarget.Process);

            // 2. Make api32.dll (and any sibling DLLs it loads by name) resolvable by the OS loader.
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (path.IndexOf(BundledBin, StringComparison.OrdinalIgnoreCase) < 0)
            {
                Environment.SetEnvironmentVariable(
                    "PATH", BundledBin + Path.PathSeparator + path, EnvironmentVariableTarget.Process);
            }

            try { SetDllDirectory(BundledBin); } catch { /* best effort */ }

            // 3. Ensure EDIABAS.INI exists and points its EcuPath at the bundled ECU folder. EcuPath
            //    is written as an absolute path at runtime so the folder stays portable across
            //    machines and drive letters.
            EnsureBundledIni();

            IsPortable = true;
            return true;
        }

        private static void EnsureBundledIni()
        {
            try
            {
                string ini = Path.Combine(BundledBin, "EDIABAS.INI");
                if (!File.Exists(ini))
                    File.WriteAllText(ini, DefaultIni());

                EdiabasConfig.SetValues(ini, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EcuPath"] = BundledEcu,
                });
            }
            catch
            {
                // Non-fatal; apiInit will surface a clear error if the paths are wrong.
            }
        }

        private static string DefaultIni()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "[Configuration]",
                "Interface = STD:OBD",
                "EcuPath = " + BundledEcu,
                "Simulation = 0",
                "TracePath = " + Path.Combine(BundledRoot, "Trace"),
                "ApiTrace = 0",
                "IfhTrace = 0",
                string.Empty,
            });
        }
    }
}
