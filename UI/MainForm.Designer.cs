using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace S54VanosTester.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Branding
        private BrandHeader brandHeader;
        private MenuStrip menuStrip;
        private ToolStripMenuItem helpMenu;
        private ToolStripMenuItem aboutMenuItem;

        // Connection bar
        private Panel connectionBar;
        private Label portLabel;
        private ComboBox portCombo;
        private Button refreshButton;
        private Button autoConnectButton;
        private Button connectButton;
        private Button disconnectButton;
        private Label statusLabel;

        // Main split
        private SplitContainer mainSplit;

        // VANOS panel
        private GroupBox vanosGroup;
        private Button runVanosButton;
        private Label vanosSummaryLabel;
        private DataGridView vanosGrid;

        // Live panel
        private GroupBox liveGroup;
        private Panel liveButtons;
        private Button startLiveButton;
        private Button stopLiveButton;
        private Panel liveReadouts;
        private Label coolantCaption;
        private Label coolantValue;
        private Label oilCaption;
        private Label oilValue;
        private Chart liveChart;

        // Log
        private TextBox logBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _session?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.brandHeader = new BrandHeader();
            this.menuStrip = new MenuStrip();
            this.helpMenu = new ToolStripMenuItem();
            this.aboutMenuItem = new ToolStripMenuItem();
            this.connectionBar = new Panel();
            this.portLabel = new Label();
            this.portCombo = new ComboBox();
            this.refreshButton = new Button();
            this.autoConnectButton = new Button();
            this.connectButton = new Button();
            this.disconnectButton = new Button();
            this.statusLabel = new Label();
            this.mainSplit = new SplitContainer();
            this.vanosGroup = new GroupBox();
            this.vanosGrid = new DataGridView();
            this.vanosSummaryLabel = new Label();
            this.runVanosButton = new Button();
            this.liveGroup = new GroupBox();
            this.liveChart = new Chart();
            this.liveReadouts = new Panel();
            this.coolantCaption = new Label();
            this.coolantValue = new Label();
            this.oilCaption = new Label();
            this.oilValue = new Label();
            this.liveButtons = new Panel();
            this.startLiveButton = new Button();
            this.stopLiveButton = new Button();
            this.logBox = new TextBox();

            // Suspend layout while the control tree is assembled. Several Fill-docked children
            // (notably liveChart) are briefly squeezed to a non-positive size as their Top-docked
            // siblings are added; the DataVisualization Chart throws ArgumentException on a
            // non-positive height, so all layout is deferred until the form has its real ClientSize.
            ((System.ComponentModel.ISupportInitialize)(this.vanosGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.liveChart)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.connectionBar.SuspendLayout();
            this.vanosGroup.SuspendLayout();
            this.liveGroup.SuspendLayout();
            this.liveReadouts.SuspendLayout();
            this.liveButtons.SuspendLayout();
            this.SuspendLayout();

            // menuStrip
            this.aboutMenuItem.Text = "&About";
            this.aboutMenuItem.Click += this.aboutMenuItem_Click;
            this.helpMenu.Text = "&Help";
            this.helpMenu.DropDownItems.Add(this.aboutMenuItem);
            this.menuStrip.Items.Add(this.helpMenu);
            this.menuStrip.BackColor = Branding.Ink;
            this.menuStrip.ForeColor = Branding.OnInk;
            this.helpMenu.ForeColor = Branding.OnInk;

            // connectionBar
            this.connectionBar.Dock = DockStyle.Top;
            this.connectionBar.Height = 48;
            this.connectionBar.Padding = new Padding(8);
            this.connectionBar.BackColor = System.Drawing.Color.FromArgb(0xF2, 0xF3, 0xF5);

            // portLabel
            this.portLabel.Text = "COM Port:";
            this.portLabel.AutoSize = true;
            this.portLabel.Location = new System.Drawing.Point(10, 16);

            // portCombo
            this.portCombo.Location = new System.Drawing.Point(75, 12);
            this.portCombo.Width = 220;
            this.portCombo.DropDownStyle = ComboBoxStyle.DropDownList;

            // refreshButton
            this.refreshButton.Text = "Refresh";
            this.refreshButton.Location = new System.Drawing.Point(305, 10);
            this.refreshButton.Width = 75;
            this.refreshButton.Height = 26;
            this.refreshButton.Click += this.refreshButton_Click;
            Branding.StyleSecondary(this.refreshButton);

            // autoConnectButton
            this.autoConnectButton.Text = "Auto-Connect";
            this.autoConnectButton.Location = new System.Drawing.Point(388, 10);
            this.autoConnectButton.Width = 110;
            this.autoConnectButton.Height = 26;
            this.autoConnectButton.Click += this.autoConnectButton_Click;
            Branding.StylePrimary(this.autoConnectButton);

            // connectButton
            this.connectButton.Text = "Connect";
            this.connectButton.Location = new System.Drawing.Point(506, 10);
            this.connectButton.Width = 90;
            this.connectButton.Height = 26;
            this.connectButton.Click += this.connectButton_Click;
            Branding.StyleSecondary(this.connectButton);

            // disconnectButton
            this.disconnectButton.Text = "Disconnect";
            this.disconnectButton.Location = new System.Drawing.Point(604, 10);
            this.disconnectButton.Width = 90;
            this.disconnectButton.Height = 26;
            this.disconnectButton.Enabled = false;
            this.disconnectButton.Click += this.disconnectButton_Click;
            Branding.StyleSecondary(this.disconnectButton);

            // statusLabel
            this.statusLabel.Text = "Disconnected";
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5f, System.Drawing.FontStyle.Bold);
            this.statusLabel.ForeColor = Branding.Fail;
            this.statusLabel.Location = new System.Drawing.Point(710, 16);

            this.connectionBar.Controls.Add(this.portLabel);
            this.connectionBar.Controls.Add(this.portCombo);
            this.connectionBar.Controls.Add(this.refreshButton);
            this.connectionBar.Controls.Add(this.autoConnectButton);
            this.connectionBar.Controls.Add(this.connectButton);
            this.connectionBar.Controls.Add(this.disconnectButton);
            this.connectionBar.Controls.Add(this.statusLabel);

            // mainSplit
            this.mainSplit.Dock = DockStyle.Fill;
            this.mainSplit.Orientation = Orientation.Vertical;
            this.mainSplit.SplitterDistance = 520;

            // vanosGroup
            this.vanosGroup.Text = "VANOS Test Results";
            this.vanosGroup.Dock = DockStyle.Fill;
            this.vanosGroup.Padding = new Padding(8);

            // runVanosButton
            this.runVanosButton.Text = "Run VANOS Test";
            this.runVanosButton.Dock = DockStyle.Top;
            this.runVanosButton.Height = 40;
            this.runVanosButton.Enabled = false;
            this.runVanosButton.Click += this.runVanosButton_Click;
            Branding.StylePrimary(this.runVanosButton);

            // vanosSummaryLabel
            this.vanosSummaryLabel.Dock = DockStyle.Top;
            this.vanosSummaryLabel.Height = 36;
            this.vanosSummaryLabel.Text = "No test run yet.";
            this.vanosSummaryLabel.Padding = new Padding(0, 8, 0, 0);

            // vanosGrid
            this.vanosGrid.Dock = DockStyle.Fill;
            this.vanosGrid.ReadOnly = true;
            this.vanosGrid.AllowUserToAddRows = false;
            this.vanosGrid.AllowUserToDeleteRows = false;
            this.vanosGrid.RowHeadersVisible = false;
            this.vanosGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.vanosGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            this.vanosGroup.Controls.Add(this.vanosGrid);
            this.vanosGroup.Controls.Add(this.vanosSummaryLabel);
            this.vanosGroup.Controls.Add(this.runVanosButton);

            // liveGroup
            this.liveGroup.Text = "Live Data - Temperatures";
            this.liveGroup.Dock = DockStyle.Fill;
            this.liveGroup.Padding = new Padding(8);

            // liveButtons
            this.liveButtons.Dock = DockStyle.Top;
            this.liveButtons.Height = 40;

            // startLiveButton
            this.startLiveButton.Text = "Start Live";
            this.startLiveButton.Location = new System.Drawing.Point(0, 4);
            this.startLiveButton.Width = 100;
            this.startLiveButton.Height = 28;
            this.startLiveButton.Enabled = false;
            this.startLiveButton.Click += this.startLiveButton_Click;
            Branding.StylePrimary(this.startLiveButton);

            // stopLiveButton
            this.stopLiveButton.Text = "Stop Live";
            this.stopLiveButton.Location = new System.Drawing.Point(108, 4);
            this.stopLiveButton.Width = 100;
            this.stopLiveButton.Height = 28;
            this.stopLiveButton.Enabled = false;
            this.stopLiveButton.Click += this.stopLiveButton_Click;
            Branding.StyleSecondary(this.stopLiveButton);

            this.liveButtons.Controls.Add(this.startLiveButton);
            this.liveButtons.Controls.Add(this.stopLiveButton);

            // liveReadouts
            this.liveReadouts.Dock = DockStyle.Top;
            this.liveReadouts.Height = 70;

            // coolantCaption
            this.coolantCaption.Text = "Coolant";
            this.coolantCaption.AutoSize = true;
            this.coolantCaption.Location = new System.Drawing.Point(4, 4);

            // coolantValue
            this.coolantValue.Text = "-- °C";
            this.coolantValue.AutoSize = true;
            this.coolantValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.coolantValue.ForeColor = Branding.Coolant;
            this.coolantValue.Location = new System.Drawing.Point(4, 22);

            // oilCaption
            this.oilCaption.Text = "Oil";
            this.oilCaption.AutoSize = true;
            this.oilCaption.Location = new System.Drawing.Point(220, 4);

            // oilValue
            this.oilValue.Text = "-- °C";
            this.oilValue.AutoSize = true;
            this.oilValue.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.oilValue.ForeColor = Branding.Oil;
            this.oilValue.Location = new System.Drawing.Point(220, 22);

            this.liveReadouts.Controls.Add(this.coolantCaption);
            this.liveReadouts.Controls.Add(this.coolantValue);
            this.liveReadouts.Controls.Add(this.oilCaption);
            this.liveReadouts.Controls.Add(this.oilValue);

            // liveChart
            this.liveChart.Dock = DockStyle.Fill;
            ChartArea area = new ChartArea("main");
            area.AxisX.Title = "Time (s)";
            area.AxisY.Title = "°C";
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.Gainsboro;
            this.liveChart.ChartAreas.Add(area);

            Series coolantSeries = new Series("Coolant")
            {
                ChartType = SeriesChartType.FastLine,
                XValueType = ChartValueType.Double,
                Color = Branding.Coolant,
                BorderWidth = 2
            };
            Series oilSeries = new Series("Oil")
            {
                ChartType = SeriesChartType.FastLine,
                XValueType = ChartValueType.Double,
                Color = Branding.Oil,
                BorderWidth = 2
            };
            this.liveChart.Series.Add(coolantSeries);
            this.liveChart.Series.Add(oilSeries);
            this.liveChart.Legends.Add(new Legend("legend") { Docking = Docking.Top });

            this.liveGroup.Controls.Add(this.liveChart);
            this.liveGroup.Controls.Add(this.liveReadouts);
            this.liveGroup.Controls.Add(this.liveButtons);

            this.mainSplit.Panel1.Controls.Add(this.vanosGroup);
            this.mainSplit.Panel2.Controls.Add(this.liveGroup);

            // logBox
            this.logBox.Dock = DockStyle.Bottom;
            this.logBox.Height = 110;
            this.logBox.Multiline = true;
            this.logBox.ReadOnly = true;
            this.logBox.ScrollBars = ScrollBars.Vertical;
            this.logBox.BackColor = System.Drawing.Color.Black;
            this.logBox.ForeColor = System.Drawing.Color.LightGreen;
            this.logBox.Font = new System.Drawing.Font("Consolas", 9F);

            // MainForm
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1060, 720);
            this.MinimumSize = new System.Drawing.Size(900, 620);
            // Docking is resolved from the highest z-order index down, so the control added LAST
            // ends up at the very top. Order: chart/split fills, log at bottom, then the connection
            // bar, brand header and menu stack downward from the top edge.
            this.Controls.Add(this.mainSplit);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.connectionBar);
            this.Controls.Add(this.brandHeader);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Text = Branding.WindowTitle;

            // Re-enable layout now that the tree is built and the form is sized. EndInit lets the
            // Chart and grid validate their dimensions once, against the final (positive) size.
            ((System.ComponentModel.ISupportInitialize)(this.liveChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vanosGrid)).EndInit();
            this.liveButtons.ResumeLayout(false);
            this.liveReadouts.ResumeLayout(false);
            this.liveGroup.ResumeLayout(false);
            this.vanosGroup.ResumeLayout(false);
            this.connectionBar.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
