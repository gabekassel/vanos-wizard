using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using S54VanosTester.Diagnostics;
using S54VanosTester.Vanos;

namespace S54VanosTester.UI
{
    public partial class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly DiagnosticsSession _session;

        private Stopwatch _liveClock;
        private const int MaxChartPoints = 600;

        public MainForm(AppSettings settings)
        {
            _settings = settings ?? AppSettings.Load();
            InitializeComponent();
            TryLoadBrandIcon();

            _session = new DiagnosticsSession(_settings);
            _session.Log += OnSessionLog;
            _session.SampleReceived += OnSampleReceived;

            SetupGridColumns();
            Load += (s, e) => PopulatePorts();
        }

        private void TryLoadBrandIcon()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kp.ico");
                if (System.IO.File.Exists(path))
                    Icon = new System.Drawing.Icon(path);
            }
            catch
            {
                // Non-fatal: fall back to the default window icon.
            }
        }

        // --- Menu -----------------------------------------------------------------------

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutForm())
                about.ShowDialog(this);
        }

        // --- Connection -----------------------------------------------------------------

        private void refreshButton_Click(object sender, EventArgs e) => PopulatePorts();

        private void PopulatePorts()
        {
            portCombo.Items.Clear();
            foreach (var info in Ediabas.ComPortFinder.Enumerate())
                portCombo.Items.Add(info);

            if (portCombo.Items.Count > 0)
                portCombo.SelectedIndex = 0; // diagnostic-looking ports are sorted first

            Log($"Found {portCombo.Items.Count} COM port(s).");
        }

        private async void autoConnectButton_Click(object sender, EventArgs e)
        {
            SetConnectingState(true);
            try
            {
                string port = await _session.ConnectAutoAsync();
                OnConnected(port);
            }
            catch (Exception ex)
            {
                OnConnectFailed(ex);
            }
            finally
            {
                SetConnectingState(false);
            }
        }

        private async void connectButton_Click(object sender, EventArgs e)
        {
            if (!(portCombo.SelectedItem is Ediabas.ComPortInfo info))
            {
                MessageBox.Show(this, "Select a COM port first, or use Auto-Connect.", "No port selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetConnectingState(true);
            try
            {
                await _session.ConnectAsync(info.Port);
                OnConnected(info.Port);
            }
            catch (Exception ex)
            {
                OnConnectFailed(ex);
            }
            finally
            {
                SetConnectingState(false);
            }
        }

        private async void disconnectButton_Click(object sender, EventArgs e)
        {
            disconnectButton.Enabled = false;
            await _session.DisconnectAsync();
            statusLabel.Text = "Disconnected";
            statusLabel.ForeColor = Branding.Fail;
            SetConnectedControls(false);
        }

        private void OnConnected(string port)
        {
            statusLabel.Text = $"Connected ({port})";
            statusLabel.ForeColor = Branding.Ok;
            SetConnectedControls(true);
        }

        private void OnConnectFailed(Exception ex)
        {
            statusLabel.Text = "Disconnected";
            statusLabel.ForeColor = Branding.Fail;
            SetConnectedControls(false);
            Log($"Connect failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Connection failed",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetConnectingState(bool connecting)
        {
            autoConnectButton.Enabled = !connecting;
            connectButton.Enabled = !connecting;
            refreshButton.Enabled = !connecting;
            portCombo.Enabled = !connecting;
            if (connecting)
                statusLabel.Text = "Connecting...";
        }

        private void SetConnectedControls(bool connected)
        {
            disconnectButton.Enabled = connected;
            runVanosButton.Enabled = connected;
            startLiveButton.Enabled = connected;
            stopLiveButton.Enabled = false;
        }

        // --- VANOS test -----------------------------------------------------------------

        private async void runVanosButton_Click(object sender, EventArgs e)
        {
            runVanosButton.Enabled = false;
            vanosSummaryLabel.Text = "Running VANOS test...";
            try
            {
                VanosTestReport report = await _session.RunVanosTestAsync();
                ShowVanosReport(report);
            }
            catch (Exception ex)
            {
                vanosSummaryLabel.Text = "VANOS test failed.";
                Log($"VANOS test error: {ex.Message}");
                MessageBox.Show(this, ex.Message, "VANOS test failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                runVanosButton.Enabled = _session.IsConnected;
            }
        }

        private void SetupGridColumns()
        {
            vanosGrid.AutoGenerateColumns = false;
            vanosGrid.Columns.Clear();
            vanosGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Result", DataPropertyName = "Name", FillWeight = 50 });
            vanosGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", DataPropertyName = "Value", FillWeight = 25 });
            vanosGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit", DataPropertyName = "Unit", FillWeight = 10 });
            vanosGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status", FillWeight = 15 });
        }

        private void ShowVanosReport(VanosTestReport report)
        {
            vanosSummaryLabel.Text = $"{report.Timestamp:HH:mm:ss}  -  {report.Summary}";
            var binding = new BindingSource { DataSource = report.Items };
            vanosGrid.DataSource = binding;

            foreach (DataGridViewRow row in vanosGrid.Rows)
            {
                string status = row.Cells[3].Value as string;
                if (string.Equals(status, "FAIL", StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.BackColor = Branding.FailRow;
                    row.Cells[3].Style.ForeColor = Branding.Fail;
                    row.Cells[3].Style.Font = new System.Drawing.Font(vanosGrid.Font, System.Drawing.FontStyle.Bold);
                }
                else if (string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.BackColor = Branding.OkRow;
                    row.Cells[3].Style.ForeColor = Branding.Ok;
                    row.Cells[3].Style.Font = new System.Drawing.Font(vanosGrid.Font, System.Drawing.FontStyle.Bold);
                }
            }
        }

        // --- Live data ------------------------------------------------------------------

        private void startLiveButton_Click(object sender, EventArgs e)
        {
            _liveClock = Stopwatch.StartNew();
            liveChart.Series["Coolant"].Points.Clear();
            liveChart.Series["Oil"].Points.Clear();
            _session.StartLive();
            startLiveButton.Enabled = false;
            stopLiveButton.Enabled = true;
        }

        private void stopLiveButton_Click(object sender, EventArgs e)
        {
            _session.StopLive();
            startLiveButton.Enabled = _session.IsConnected;
            stopLiveButton.Enabled = false;
        }

        private void OnSampleReceived(TemperatureSample sample)
        {
            if (IsDisposed)
                return;
            BeginInvoke((Action)(() => UpdateLiveUi(sample)));
        }

        private void UpdateLiveUi(TemperatureSample sample)
        {
            double t = _liveClock?.Elapsed.TotalSeconds ?? 0;

            if (sample.CoolantC.HasValue)
            {
                coolantValue.Text = $"{sample.CoolantC.Value:0.0} °C";
                AddPoint(liveChart.Series["Coolant"], t, sample.CoolantC.Value);
            }

            if (sample.OilC.HasValue)
            {
                oilValue.Text = $"{sample.OilC.Value:0.0} °C";
                AddPoint(liveChart.Series["Oil"], t, sample.OilC.Value);
            }
        }

        private static void AddPoint(System.Windows.Forms.DataVisualization.Charting.Series series, double x, double y)
        {
            series.Points.AddXY(x, y);
            while (series.Points.Count > MaxChartPoints)
                series.Points.RemoveAt(0);
        }

        // --- Logging --------------------------------------------------------------------

        private void OnSessionLog(string message)
        {
            if (IsDisposed)
                return;
            BeginInvoke((Action)(() => Log(message)));
        }

        private void Log(string message)
        {
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
