using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace S54VanosTester.UI
{
    /// <summary>Kassel Performance "About" dialog.</summary>
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About " + Branding.Product;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 250);
            BackColor = Color.White;

            var header = new BrandHeader { Dock = DockStyle.Top };

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

            var body = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 16, 16, 16),
                Font = new Font("Segoe UI", 9.5f),
                Text =
                    Branding.Company + "\r\n" +
                    Branding.Product + "  v" + version + "\r\n\r\n" +
                    "VANOS function test and live oil/coolant temperature acquisition for the " +
                    "BMW S54 (MSS54 DME) over EDIABAS.\r\n\r\n" +
                    "Connect a K+DCAN / OBD diagnostic cable, switch the ignition to KL15, and the " +
                    "application will locate the diagnostic COM port automatically.\r\n\r\n" +
                    "© " + Branding.Company + ". For workshop diagnostic use."
            };

            var ok = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 40,
                DialogResult = DialogResult.OK
            };
            Branding.StylePrimary(ok);

            Controls.Add(body);
            Controls.Add(ok);
            Controls.Add(header);
            AcceptButton = ok;
        }
    }
}
