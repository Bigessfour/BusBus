#pragma warning disable CS8618 // Non-nullable field/property must contain a non-null value when exiting constructor
using BusBus.Services;
using BusBus.UI;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

#pragma warning disable CS0169 // Field is never used
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
        private CancellationTokenSource _cancellationTokenSource; public StatisticsPanel(IStatisticsService statisticsService)
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
            this.Padding = new Padding(20, 15, 20, 15); // Enhanced padding for better text spacing
            this.MinimumSize = new Size(800, 100); // Minimum size to prevent truncation

            // Create main layout
            _statisticsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 4,
                AutoSize = true, // Enable auto-sizing for dynamic content
                AutoSizeMode = AutoSizeMode.GrowAndShrink, // Allow shrinking and growing
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(5) // Add margin for better spacing
            };

            // Configure columns - auto size for flexible text display
            for (int i = 0; i < 4; i++)
            {
                _statisticsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Changed from Percent to AutoSize
            }

            // Configure rows - flexible sizing for better text display
            _statisticsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize, 50F)); // Values row with minimum height
            _statisticsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize, 30F)); // Labels row with minimum height

            // Create statistic display pairs
            CreateStatisticPair("0", "School Year Miles", 0, out _schoolYearMilesLabel);
            CreateStatisticPair("0", "School Year Students", 1, out _schoolYearStudentsLabel);
            CreateStatisticPair("0", "This Month Miles", 2, out _thisMonthMilesLabel);
            CreateStatisticPair("0", "This Month Students", 3, out _thisMonthStudentsLabel);            // Create last updated label spanning all columns
            _lastUpdatedLabel = new Label
            {
                Text = "Last updated: Never",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic), // Increased from 7F to 8F for better readability
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 20, // Increased from 15 to 20 for better text display
                Padding = new Padding(5, 3, 5, 3), // Enhanced padding
                AutoSize = false // Set to false to use explicit height
            };

#if DEBUG
            // Add debug truncation detection in development builds
            this.HandleCreated += (s, e) =>
            {
                var truncatedControls = Utils.LayoutDebugger.DetectTextTruncation(this);
                if (truncatedControls.Count > 0)
                {
                    Console.WriteLine($"[StatisticsPanel] DEBUG: Found {truncatedControls.Count} potentially truncated controls");
                    foreach (var controlInfo in truncatedControls)
                    {
                        Console.WriteLine($"  - {controlInfo}");
                    }
                }
            };
#endif

            this.Controls.Add(_statisticsLayout);
            this.Controls.Add(_lastUpdatedLabel);
        }
        private void CreateStatisticPair(string value, string label, int columnIndex, out Label valueLabel)
        {
            // Value label (top row) - enhanced sizing for better text display
            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false, // Use fixed sizing with proper dimensions
                MinimumSize = new Size(120, 40), // Minimum size to prevent truncation
                Padding = new Padding(5, 2, 5, 2), // Add padding for better text display
                AutoEllipsis = true // Enable ellipsis for very long numbers
            };

            // Description label (bottom row) - enhanced sizing for better text display
            var descLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular), // Increased from 8F to 9F
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                AutoSize = false, // Use fixed sizing with proper dimensions
                MinimumSize = new Size(120, 25), // Minimum size to prevent truncation
                Padding = new Padding(3, 0, 3, 2), // Add padding for better text display
                AutoEllipsis = true // Enable ellipsis for long descriptions
            };

            _statisticsLayout.Controls.Add(valueLabel, columnIndex, 0);
            _statisticsLayout.Controls.Add(descLabel, columnIndex, 1);
        }
        private void ApplyTheme()
        {
            // Apply modern theme colors for better integration
            this.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _statisticsLayout.BackColor = ThemeManager.CurrentTheme.CardBackground;

            // Apply theme to all labels
            foreach (Control control in _statisticsLayout.Controls)
            {
                if (control is Label label)
                {
                    label.BackColor = ThemeManager.CurrentTheme.CardBackground;
                    label.ForeColor = ThemeManager.CurrentTheme.CardText;
                }
            }

            _lastUpdatedLabel.BackColor = ThemeManager.CurrentTheme.CardBackground;
            _lastUpdatedLabel.ForeColor = ThemeManager.CurrentTheme.SecondaryText;
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
                _refreshTimer?.Stop(); _refreshTimer?.Dispose();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
#pragma warning restore CS0169 // Field is never used
