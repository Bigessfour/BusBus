using BusBus.Services;
using BusBus.UI;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Panel that displays real-time statistics for the dashboard
    /// </summary>
    public class StatisticsPanel : Panel
    {
        private readonly IStatisticsService _statisticsService;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private TableLayoutPanel _statisticsLayout;
        private Label _schoolYearMilesLabel;
        private Label _schoolYearStudentsLabel;
        private Label _thisMonthMilesLabel;
        private Label _thisMonthStudentsLabel;
        private Label _thisWeekMilesLabel;
        private Label _thisWeekStudentsLabel;
        private Label _lastUpdatedLabel;
        private CancellationTokenSource _cancellationTokenSource;        public StatisticsPanel(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _cancellationTokenSource = new CancellationTokenSource();
            
            Console.WriteLine("[StatisticsPanel] Constructor called");
            
            // Set up refresh timer (refresh every 30 seconds)
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 30000; // 30 seconds
            _refreshTimer.Tick += async (s, e) => await RefreshStatisticsAsync();
            
            InitializeComponent();
            ApplyTheme();
            
            Console.WriteLine("[StatisticsPanel] Component initialized");
            
            // Start timer after handle is created (control is ready)
            this.HandleCreated += async (s, e) =>
            {
                Console.WriteLine("[StatisticsPanel] Handle created, refreshing statistics");
                await RefreshStatisticsAsync();
                _refreshTimer.Start();
            };
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(10, 5, 10, 5);

            // Create main layout
            _statisticsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 4,
                AutoSize = false,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Configure columns - equal width for each statistic
            for (int i = 0; i < 4; i++)
            {
                _statisticsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }
            
            // Configure rows - top row for values, bottom row for labels
            _statisticsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // Values
            _statisticsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F)); // Labels

            // Create statistic display pairs
            CreateStatisticPair("0", "School Year Miles", 0, out _schoolYearMilesLabel);
            CreateStatisticPair("0", "School Year Students", 1, out _schoolYearStudentsLabel);
            CreateStatisticPair("0", "This Month Miles", 2, out _thisMonthMilesLabel);
            CreateStatisticPair("0", "This Month Students", 3, out _thisMonthStudentsLabel);

            // Create last updated label spanning all columns
            _lastUpdatedLabel = new Label
            {
                Text = "Last updated: Never",
                Font = new Font("Segoe UI", 7F, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 15,
                Padding = new Padding(0, 2, 0, 0)
            };

            this.Controls.Add(_statisticsLayout);
            this.Controls.Add(_lastUpdatedLabel);
        }

        private void CreateStatisticPair(string value, string label, int columnIndex, out Label valueLabel)
        {
            // Value label (top row)
            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Description label (bottom row)
            var descLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            _statisticsLayout.Controls.Add(valueLabel, columnIndex, 0);
            _statisticsLayout.Controls.Add(descLabel, columnIndex, 1);
        }        private void ApplyTheme()
        {
            // Temporarily use bright colors to make the panel visible for debugging
            this.BackColor = Color.Yellow;
            _statisticsLayout.BackColor = Color.LightGreen;

            // Apply theme to all labels
            foreach (Control control in _statisticsLayout.Controls)
            {
                if (control is Label label)
                {
                    label.BackColor = Color.LightGreen;
                    label.ForeColor = Color.Black; // Dark text for visibility
                }
            }

            _lastUpdatedLabel.BackColor = Color.Yellow;
            _lastUpdatedLabel.ForeColor = Color.Black;
        }

        public async Task RefreshStatisticsAsync()
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            try
            {
                var statistics = await _statisticsService.GetDashboardStatisticsAsync();

                // Update UI on the UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateStatisticsDisplay(statistics)));
                }
                else
                {
                    UpdateStatisticsDisplay(statistics);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StatisticsPanel] Error refreshing statistics: {ex.Message}");
                
                // Update UI on the UI thread to show error state
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => ShowErrorState()));
                }
                else
                {
                    ShowErrorState();
                }
            }
        }

        private void UpdateStatisticsDisplay(DashboardStatistics statistics)
        {
            _schoolYearMilesLabel.Text = statistics.TotalMilesSchoolYear.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _schoolYearStudentsLabel.Text = statistics.TotalStudentsSchoolYear.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _thisMonthMilesLabel.Text = statistics.TotalMilesThisMonth.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _thisMonthStudentsLabel.Text = statistics.TotalStudentsThisMonth.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            _lastUpdatedLabel.Text = $"Last updated: {statistics.LastUpdated:HH:mm:ss}";
        }

        private void ShowErrorState()
        {
            _schoolYearMilesLabel.Text = "Error";
            _schoolYearStudentsLabel.Text = "Error";
            _thisMonthMilesLabel.Text = "Error";
            _thisMonthStudentsLabel.Text = "Error";
            _lastUpdatedLabel.Text = "Last updated: Error";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
