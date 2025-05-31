using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BusBus.UI.Common;
using BusBus.Services;
using BusBus.UI.Core;

namespace BusBus.UI
{
    /// <summary>
    /// Overview panel that shows when "Dashboard" is selected in navigation
    /// Displays statistics and summary information without creating a nested dashboard
    /// </summary>
    public class DashboardOverviewView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardOverviewView> _logger;
        private readonly IRouteService _routeService;

        private Panel _statsPanel = null!;
        private Panel _summaryPanel = null!;
        private Label _welcomeLabel = null!;
        private Label _statsTitle = null!;

        // Logger message definitions for performance
        private static readonly Action<ILogger, Exception?> LogInitializing =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1, "Initializing"), "Initializing Dashboard Overview View");

        private static readonly Action<ILogger, Exception?> LogInitialized =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, "Initialized"), "Dashboard Overview View initialized successfully");

        private static readonly Action<ILogger, Exception?> LogStatsLoaded =
            LoggerMessage.Define(LogLevel.Debug, new EventId(3, "StatsLoaded"), "Dashboard overview stats loaded successfully"); private static readonly Action<ILogger, Exception?> LogStatsError =
            LoggerMessage.Define(LogLevel.Error, new EventId(4, "StatsError"), "Error loading dashboard overview stats");

        private static readonly Action<ILogger, Exception?> LogStatsCancelled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(5, "StatsCancelled"), "LoadStatsAsync was cancelled");

        public DashboardOverviewView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetRequiredService<ILogger<DashboardOverviewView>>();
            _routeService = serviceProvider.GetRequiredService<IRouteService>();

            InitializeOverviewPanel();
        }

        public override string ViewName => "dashboard";

        public override string Title => "Dashboard Overview";

        protected override void InitializeView()
        {
            base.InitializeView();

            LogInitializing(_logger, null);

            // Set the form properties
            Size = new Size(800, 600);
            BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground; CreateWelcomeSection();
            CreateStatsSection();
            CreateSummarySection();

            // Start loading stats in background (fire-and-forget for initialization)
            _ = LoadStatsAsync();

            LogInitialized(_logger, null);
        }

        private void InitializeOverviewPanel()
        {
            InitializeView();
        }

        private void CreateWelcomeSection()
        {
            _welcomeLabel = new Label
            {
                Text = $"Welcome to BusBus Dashboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                Location = new Point(30, 30),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = $"Today is {DateTime.Now.ToString("dddd, MMMM dd, yyyy")}",
                Font = new Font("Segoe UI", 12),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.SecondaryText,
                Location = new Point(30, 70),
                AutoSize = true
            };

            Controls.AddRange(new Control[] { _welcomeLabel, subtitleLabel });
        }

        private void CreateStatsSection()
        {
            _statsTitle = new Label
            {
                Text = "Today's Overview",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                Location = new Point(30, 120),
                AutoSize = true
            };

            _statsPanel = new Panel
            {
                Location = new Point(30, 160),
                Size = new Size(740, 120),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create stats cards
            CreateStatsCard("Active Routes", "Loading...", 20, 20);
            CreateStatsCard("Available Drivers", "Loading...", 190, 20);
            CreateStatsCard("Active Vehicles", "Loading...", 360, 20);
            CreateStatsCard("Total Capacity", "Loading...", 530, 20);

            Controls.AddRange(new Control[] { _statsTitle, _statsPanel });
        }

        private void CreateStatsCard(string title, string value, int x, int y)
        {
            var cardPanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(150, 80),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.SecondaryText,
                Location = new Point(10, 10),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.PrimaryText,
                Location = new Point(10, 35),
                Size = new Size(130, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = $"value_{title.Replace(" ", "").ToLower()}"
            };

            cardPanel.Controls.AddRange(new Control[] { titleLabel, valueLabel });
            _statsPanel.Controls.Add(cardPanel);
        }

        private void CreateSummarySection()
        {
            var summaryTitle = new Label
            {
                Text = "Quick Actions",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.HeadlineText,
                Location = new Point(30, 300),
                AutoSize = true
            };

            _summaryPanel = new Panel
            {
                Location = new Point(30, 340),
                Size = new Size(740, 200),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Create action buttons
            CreateActionButton("Add Route", "Create a new route assignment", 20, 20, (s, e) => NavigateToView?.Invoke("routes"));
            CreateActionButton("Manage Drivers", "View and edit driver information", 200, 20, (s, e) => NavigateToView?.Invoke("drivers"));
            CreateActionButton("Vehicle Status", "Check vehicle availability", 380, 20, (s, e) => NavigateToView?.Invoke("vehicles"));
            CreateActionButton("Generate Reports", "View system reports", 560, 20, (s, e) => NavigateToView?.Invoke("reports"));

            Controls.AddRange(new Control[] { summaryTitle, _summaryPanel });
        }

        private void CreateActionButton(string title, string description, int x, int y, EventHandler clickHandler)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(160, 80),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.PrimaryText,
                Location = new Point(10, 10),
                Size = new Size(140, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8),
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.SecondaryText,
                Location = new Point(10, 35),
                Size = new Size(140, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            buttonPanel.Controls.AddRange(new Control[] { titleLabel, descLabel });
            buttonPanel.Click += clickHandler;

            // Make child controls clickable too
            foreach (Control control in buttonPanel.Controls)
            {
                control.Click += clickHandler;
            }

            _summaryPanel.Controls.Add(buttonPanel);
        }
        private async Task LoadStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Load actual statistics                var totalRoutes = await _routeService.GetRoutesCountAsync(cancellationToken);
                var activeRoutes = (await _routeService.GetRoutesAsync(cancellationToken)).Count(r => r.IsActive);

                // Update stats cards
                UpdateStatsCard("activeroutes", activeRoutes.ToString());
                UpdateStatsCard("availabledrivers", "5"); // Placeholder
                UpdateStatsCard("activevehicles", "8"); // Placeholder
                UpdateStatsCard("totalcapacity", "540"); // Placeholder

                LogStatsLoaded(_logger, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Operation was cancelled, don't update UI
                LogStatsCancelled(_logger, null);
            }
            catch (Exception ex)
            {
                LogStatsError(_logger, ex);

                // Show error state
                UpdateStatsCard("activeroutes", "Error");
                UpdateStatsCard("availabledrivers", "Error");
                UpdateStatsCard("activevehicles", "Error");
                UpdateStatsCard("totalcapacity", "Error");
            }
        }

        private void UpdateStatsCard(string cardName, string value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatsCard(cardName, value)));
                return;
            }

            var valueControl = _statsPanel.Controls.Find($"value_{cardName}", true);
            if (valueControl.Length > 0 && valueControl[0] is Label label)
            {
                label.Text = value;
            }
        }

        public event Action<string>? NavigateToView; protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await LoadStatsAsync(cancellationToken);
        }

        protected override async Task OnDeactivateAsync()
        {
            await Task.CompletedTask;
        }
    }
}
