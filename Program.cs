using System;
using System.Windows.Forms;
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

            AppSettings settings = AppSettings.Load();
            Application.Run(new MainForm(settings));
        }
    }
}
