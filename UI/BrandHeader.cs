using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace S54VanosTester.UI
{
    /// <summary>
    /// The Kassel Performance header banner. Owner-drawn so no external image asset is required:
    /// a dark chrome bar, a racing-red "KP" badge, the wordmark, and the product subtitle, with a
    /// thin accent stripe along the bottom edge.
    /// </summary>
    internal sealed class BrandHeader : Panel
    {
        public BrandHeader()
        {
            Dock = DockStyle.Top;
            Height = 64;
            DoubleBuffered = true;
            BackColor = Branding.Ink;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Subtle vertical sheen on the chrome bar.
            using (var sheen = new LinearGradientBrush(
                       new Rectangle(0, 0, Width, Height), Branding.InkLight, Branding.Ink, 90f))
            {
                g.FillRectangle(sheen, 0, 0, Width, Height);
            }

            const int pad = 14;
            int badge = Height - pad * 2;
            var badgeRect = new Rectangle(pad, pad, badge, badge);

            // Red "KP" badge.
            using (var badgeBrush = new SolidBrush(Branding.Accent))
                g.FillRectangle(badgeBrush, badgeRect);

            using (var kpFont = new Font("Segoe UI Black", badge * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var kpBrush = new SolidBrush(Branding.OnInk))
            using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("KP", kpFont, kpBrush, badgeRect, center);
            }

            int textX = badgeRect.Right + 14;

            // Wordmark: "KASSEL" in white, "PERFORMANCE" in red, tracked out.
            using (var wordFont = new Font("Segoe UI", 17f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var white = new SolidBrush(Branding.OnInk))
            using (var red = new SolidBrush(Branding.Accent))
            {
                const string a = "KASSEL ";
                string b = "PERFORMANCE";
                float y = pad + 1;
                g.DrawString(a, wordFont, white, textX, y);
                float aw = g.MeasureString(a, wordFont).Width;
                g.DrawString(b, wordFont, red, textX + aw - 4, y);
            }

            // Product subtitle.
            using (var subFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var muted = new SolidBrush(Branding.OnInkMuted))
            {
                g.DrawString("BMW S54 — VANOS Test & Live Diagnostics", subFont, muted, textX, pad + 26);
            }

            // Bottom accent stripe.
            using (var stripe = new SolidBrush(Branding.Accent))
                g.FillRectangle(stripe, 0, Height - 3, Width, 3);
        }
    }
}
