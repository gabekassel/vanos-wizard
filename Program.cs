using System;
using System.Windows.Forms;
using S54VanosTester.Ediabas;
using S54VanosTester.UI;

namespace S54VanosTester
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Activate the bundled, portable EDIABAS runtime (if shipped next to the .exe) before
            // any EDIABAS API call. No-op when no bundle is present, so an installed EDIABAS still
            // works as a fallback.
            EdiabasBootstrap.Prepare();

            AppSettings settings = AppSettings.Load();
            Application.Run(new MainForm(settings));
        }
    }
}
