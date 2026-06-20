using System.Drawing;

namespace S54VanosTester.UI
{
    /// <summary>
    /// Centralised Kassel Performance brand assets (colours, names, fonts) so the look and feel of
    /// the application is defined in one place and stays consistent across every form and control.
    /// </summary>
    internal static class Branding
    {
        // --- Names ---------------------------------------------------------------------
        public const string Company = "Kassel Performance";
        public const string Product = "S54 VANOS Tester";
        public const string WindowTitle = Company + " — " + Product; // em dash

        // --- Palette -------------------------------------------------------------------
        // A dark motorsport palette: near-black chrome with a single racing-red accent.
        public static readonly Color Ink = Color.FromArgb(0x16, 0x16, 0x18);   // header / chrome
        public static readonly Color InkLight = Color.FromArgb(0x26, 0x26, 0x2A);
        public static readonly Color Accent = Color.FromArgb(0xC8, 0x10, 0x2E); // racing red
        public static readonly Color OnInk = Color.FromArgb(0xF4, 0xF4, 0xF6);  // text on dark
        public static readonly Color OnInkMuted = Color.FromArgb(0xA8, 0xA8, 0xB0);

        // Data colours kept distinct and readable on a light chart background.
        public static readonly Color Coolant = Color.FromArgb(0x2E, 0x6F, 0xF2); // blue
        public static readonly Color Oil = Color.FromArgb(0xE8, 0x7A, 0x00);     // amber

        // --- Status colours ------------------------------------------------------------
        public static readonly Color Ok = Color.FromArgb(0x1B, 0x8A, 0x3A);
        public static readonly Color Fail = Color.FromArgb(0xC0, 0x1A, 0x2B);
        public static readonly Color OkRow = Color.FromArgb(0xE9, 0xF7, 0xEC);
        public static readonly Color FailRow = Color.FromArgb(0xFB, 0xE7, 0xE9);

        /// <summary>Style a button as a primary (filled, racing-red) brand action.</summary>
        public static void StylePrimary(System.Windows.Forms.Button button)
        {
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = OnInk;
            button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            button.UseVisualStyleBackColor = false;
        }

        /// <summary>Style a button as a secondary (outlined) brand action.</summary>
        public static void StyleSecondary(System.Windows.Forms.Button button)
        {
            button.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(0xC8, 0xCC, 0xD4);
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Color.White;
            button.ForeColor = Ink;
            button.UseVisualStyleBackColor = false;
        }
    }
}
