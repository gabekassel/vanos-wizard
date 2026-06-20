using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Management;

namespace S54VanosTester.Ediabas
{
    /// <summary>Describes a serial port candidate discovered on the machine.</summary>
    public sealed class ComPortInfo
    {
        public string Port { get; set; }        // e.g. "COM3"
        public string Description { get; set; }  // e.g. "USB Serial Port (COM3)"
        public bool LikelyDiagnostic { get; set; }

        public override string ToString()
            => string.IsNullOrEmpty(Description) ? Port : $"{Port} - {Description}";
    }

    /// <summary>
    /// Enumerates serial ports and ranks the ones that look like a BMW diagnostic cable
    /// (K+DCAN / FTDI / Prolific style USB-serial adapters) first, so the auto-probe tries
    /// the most likely candidates before anything else.
    /// </summary>
    public static class ComPortFinder
    {
        private static readonly string[] DiagnosticHints =
        {
            "FTDI", "FT232", "USB Serial", "USB-Serial", "OBD", "K+DCAN", "KDCAN",
            "D-CAN", "Prolific", "CH340", "Silicon Labs", "CP210"
        };

        /// <summary>Return all serial ports, diagnostic-looking ones first.</summary>
        public static List<ComPortInfo> Enumerate()
        {
            var byPort = new Dictionary<string, ComPortInfo>(StringComparer.OrdinalIgnoreCase);

            // Base list from the framework guarantees we never miss a port.
            foreach (string port in SerialPort.GetPortNames())
            {
                byPort[port] = new ComPortInfo { Port = port, Description = port };
            }

            // Enrich with friendly names + descriptions via WMI.
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                           "SELECT Name, Caption, Description FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string name = (device["Name"] as string) ?? (device["Caption"] as string);
                        if (string.IsNullOrEmpty(name))
                            continue;

                        string port = ExtractComPort(name);
                        if (port == null)
                            continue;

                        var info = new ComPortInfo
                        {
                            Port = port,
                            Description = name,
                            LikelyDiagnostic = LooksDiagnostic(name)
                        };
                        byPort[port] = info;
                    }
                }
            }
            catch
            {
                // WMI may be unavailable; the framework list above is the fallback.
            }

            var list = new List<ComPortInfo>(byPort.Values);
            list.Sort((a, b) =>
            {
                if (a.LikelyDiagnostic != b.LikelyDiagnostic)
                    return a.LikelyDiagnostic ? -1 : 1;
                return ComPortNumber(a.Port).CompareTo(ComPortNumber(b.Port));
            });
            return list;
        }

        private static bool LooksDiagnostic(string text)
        {
            foreach (string hint in DiagnosticHints)
            {
                if (text.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string ExtractComPort(string name)
        {
            int open = name.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
            if (open < 0)
                return null;
            int close = name.IndexOf(')', open);
            if (close < 0)
                return null;
            return name.Substring(open + 1, close - open - 1);
        }

        private static int ComPortNumber(string port)
        {
            return int.TryParse(port.Replace("COM", string.Empty), out int n) ? n : int.MaxValue;
        }
    }
}
