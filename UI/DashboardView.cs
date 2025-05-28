#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI.Common;
using BusBus.Services;

namespace BusBus.UI
{
    /// <summary>
    /// The DashboardView is the home/main view that appears in the Dashboard's content panel.
    /// This is loaded when the user clicks on the "Dashboard" navigation button in the side panel.
    /// This view contains its own sections like today's routes, action items, quick stats, etc.
    /// NOTE: This is NOT the main container - it's a view that loads INSIDE the Dashboard container.
    /// </summary>
    public class DashboardView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DashboardView> _logger;
        private readonly Stopwatch _loadingStopwatch = new Stopwatch();
        private TableLayoutPanel _mainLayout = null!;
        private Panel _headerPanel = null!;
        private Panel _todaysRoutesPanel = null!;
        private Panel _actionItemsPanel = null!;
        private Panel _quickStatsPanel = null!;
        private Panel _quickActionsPanel = null!;

        // Timing tracking for performance analysis
        private Dictionary<string, long> _timingMetrics = new Dictionary<string, long>();
        private int _uiComponentsCreated = 0;
        private int _uiComponentsLoaded = 0;
        // Logger message definitions for performance
        private static readonly Action<ILogger, string, Exception?> _logDebug =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "UIDebug"),
                "{Message}");

        private static readonly Action<ILogger, string, long, int, Exception?> _logPerformance =
            LoggerMessage.Define<string, long, int>(
                LogLevel.Information,
                new EventId(2, "UIPerformance"),
                "{Operation} completed in {ElapsedMs}ms with {ComponentCount} components");
        private static readonly Action<ILogger, string, long, Exception?> _logMetric =
  LoggerMessage.Define<string, long>(
      LogLevel.Debug,
      new EventId(3, "UIMetric"),
      "Timing: {MetricName} = {MetricValue}ms");

        private static readonly Action<ILogger, string, Exception?> _logWarning =
            LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(4, "UIWarning"),
                "{Message}");

        private static readonly Action<ILogger, string, Exception?> _logError =
            LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(5, "UIError"),
                "{Message}");

        public override string ViewName => "dashboard";
        public override string Title => "What's Happening Today"; public DashboardView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Get the logger from the service provider
            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<DashboardView>() ??
                     Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger<DashboardView>();

            // Start performance tracking
            _loadingStopwatch.Start();
            LogDebug("DashboardView constructor called");

            // Start tracking creation time
            _timingMetrics["Constructor_Start"] = _loadingStopwatch.ElapsedMilliseconds;

            InitializeView();

            // Record initialization completion time
            _timingMetrics["InitializeView_Complete"] = _loadingStopwatch.ElapsedMilliseconds;
            LogPerformance("Initialization", _loadingStopwatch.ElapsedMilliseconds, _uiComponentsCreated);
        }
        // Helper logging methods that use the high-performance LoggerMessage pattern
        private void LogDebug(string message) => _logDebug(_logger, message, null);

        private void LogPerformance(string operation, long elapsedMs, int componentCount) =>
            _logPerformance(_logger, operation, elapsedMs, componentCount, null);

        private void LogMetric(string name, long value) => _logMetric(_logger, name, value, null);

        private void LogWarning(string message) => _logWarning(_logger, message, null);

        private void LogError(string message, Exception? ex = null) => _logError(_logger, message, ex);

        // Helper for tracking UI component creation
        private void TrackComponentCreation(Control control)
        {
            _uiComponentsCreated++;
            if (_uiComponentsCreated % 10 == 0)
            {
                _timingMetrics[$"Components_{_uiComponentsCreated}"] = _loadingStopwatch.ElapsedMilliseconds;
            }
        }
        protected override void InitializeView()
        {
            LogDebug("InitializeView: Starting initialization");
            _timingMetrics["InitializeView_Start"] = _loadingStopwatch.ElapsedMilliseconds;

            base.InitializeView();
            LogDebug("InitializeView: Base initialization complete");

            // Apply theme to this control
            ThemeManager.ApplyThemeToControl(this);
            LogDebug("InitializeView: Theme applied");

            // Main dashboard layout - 2x3 grid for dashboard widgets
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                Padding = new Padding(15), // Reduced padding to give more room to content
                BackColor = ThemeManager.CurrentTheme.MainBackground,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single // Border for debugging layout
            };

            // Equal column distribution
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Row distribution with better content scaling
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Header fixed height
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));  // Main content gets 70% of remaining space
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));  // Bottom row gets 30% of remaining space

            CreateHeaderSection();
            CreateTodaysRoutesSection();
            CreateActionItemsSection();
            CreateQuickStatsSection();
            CreateQuickActionsSection();

            this.Controls.Add(_mainLayout);

            // Register for resize events to dynamically adjust layouts
            this.Resize += OnDashboardResize;

            // Apply high-quality text rendering to all controls
            TextRenderingManager.RegisterForHighQualityTextRendering(this);

            // Detect and report any potential layout issues
            DetectLayoutIssues();
        }

        private void OnDashboardResize(object? sender, EventArgs e)
        {
            // This method will be called whenever the dashboard is resized
            // Refresh layout calculations for each section
            RefreshSectionLayouts();
        }

        private void RefreshSectionLayouts()
        {
            // Force all sections to recalculate their layouts
            foreach (Control control in _mainLayout.Controls)
            {
                control.PerformLayout();

                // If this is a container, refresh its child controls as well
                foreach (Control child in control.Controls)
                {
                    child.PerformLayout();
                }
            }
        }
        private void DetectLayoutIssues()
        {
            // Apply layout debugging to detect and fix truncation issues
            BusBus.Utils.LayoutDebugger.EnableDebugMode();
            var truncationIssues = BusBus.Utils.LayoutDebugger.DetectTextTruncation(this);
            if (truncationIssues.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardView] Found {truncationIssues.Count} text truncation issues");
                BusBus.Utils.LayoutDebugger.FixTextTruncation(this); foreach (var issue in truncationIssues)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Text truncation detected");
                }
            }
        }

        private void CreateHeaderSection()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Margin = new Padding(8),
                Padding = new Padding(20, 16, 20, 16)
            };

            // Apply glassmorphism styling to header panel
            ThemeManager.CurrentTheme.StyleModernCard(_headerPanel);

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };

            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));            // Welcome section
            var welcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 20, 10, 20) // Add padding around welcome content
            }; var titleLabel = new Label
            {
                Text = "🌅 Good Morning!",
                Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                AutoSize = true,
                AutoEllipsis = true,
                MaximumSize = new Size(700, 0),
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(4, 2, 4, 2),
                BackColor = Color.Transparent
            };

            var dateLabel = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy"),
                Font = new Font("Segoe UI", 14F),
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Location = new Point(15, 55),
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0),
                MaximumSize = Size.Empty // Remove any size constraints to prevent truncation
            };

            welcomePanel.Controls.AddRange(new Control[] { titleLabel, dateLabel });

            // Weather/Quick info section
            var infoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 20, 10, 20) // Add padding for weather section
            }; var weatherLabel = new Label
            {
                Text = "🌤️ 72°F\nPartly Cloudy",
                Font = new Font("Segoe UI", 12F),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                TextAlign = ContentAlignment.TopRight,
                AutoSize = true,
                Location = new Point(10, 10),
                MaximumSize = Size.Empty, // Remove size constraints to prevent truncation
                MinimumSize = new Size(150, 80) // Ensure minimum space for multi-line text
            };

            infoPanel.Controls.Add(weatherLabel);

            headerLayout.Controls.Add(welcomePanel, 0, 0);
            headerLayout.Controls.Add(infoPanel, 1, 0);            _headerPanel.Controls.Add(headerLayout);

            _mainLayout.Controls.Add(_headerPanel, 0, 0);
            _mainLayout.SetColumnSpan(_headerPanel, 2); // Span across both columns
        }

        private void CreateTodaysRoutesSection()
        {
            LogDebug("CreateTodaysRoutesSection: Starting");
            _todaysRoutesPanel = CreateDashboardCard("📍 Today's Routes", ThemeManager.CurrentTheme.ButtonBackground);
            LogDebug($"CreateTodaysRoutesSection: Created dashboard card with {_todaysRoutesPanel.Controls.Count} controls");

            // Use a more structured layout for routes list
            var routesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                AutoScroll = false // We'll use a nested panel with scrolling
            };            var routesList = new TableLayoutPanel
            {
                Name = "routesList",
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                ColumnCount = 1,
                RowCount = 0, // Will be incremented as items are added
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            LogDebug("CreateTodaysRoutesSection: Created routesList TableLayoutPanel");

            routesList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Full width            // Create a scrollable container for the routes list
            var scrollPanel = new Panel
            {
                Name = "routesScrollPanel",
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };// Load route data from service
            try
            {
                var routeService = _serviceProvider.GetService<BusBus.Services.IRouteService>();
                if (routeService != null)
                {                    // Get routes from the service - will be loaded during activation
                    var placeholderLabel = new Label
                    {
                        Text = "Loading routes...",
                        Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                        ForeColor = ThemeManager.CurrentTheme.SecondaryText,
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0, 20, 0, 20)
                    };
                    routesList.RowCount++;
                    routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    routesList.Controls.Add(placeholderLabel, 0, 0);
                    // Store reference to routes list for later updates
                    routesList.Tag = "routes-list";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Route service not available");
                    // Fallback to static content
                    var fallbackLabel = new Label
                    {
                        Text = "Route service unavailable",
                        Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                        ForeColor = Color.Red, // Use standard color for error
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0, 20, 0, 20)
                    };
                    routesList.RowCount++;
                    routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize)); routesList.Controls.Add(fallbackLabel, 0, 0);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors loading routes
                System.Diagnostics.Debug.WriteLine($"Error loading routes: {ex.Message}");
                var errorLabel = new Label
                {
                    Text = "Error loading routes",
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.Red,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 20, 0, 20)
                };
                routesList.RowCount++;
                routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                routesList.Controls.Add(errorLabel, 0, 0);
            }            // Assemble the layout hierarchy
            scrollPanel.Controls.Add(routesList);
            routesContainer.Controls.Add(scrollPanel);
            LogDebug($"CreateTodaysRoutesSection: Assembled container hierarchy - routesList in scrollPanel, scrollPanel in routesContainer");
              // Extract the content area from the card using the Tag (more reliable than hierarchy navigation)
            Panel? contentArea = null;
            var cardInfo = _todaysRoutesPanel.Tag;
            if (cardInfo?.GetType().GetProperty("ContentArea")?.GetValue(cardInfo) is Panel area)
            {
                contentArea = area;
                contentArea.Controls.Add(routesContainer);
                LogDebug("CreateTodaysRoutesSection: Added routesContainer to contentArea successfully using Tag reference");
            }
            else
            {
                // Fallback to hierarchy navigation
                if (_todaysRoutesPanel.Controls.Count > 0 && 
                    _todaysRoutesPanel.Controls[0] is TableLayoutPanel cardLayout &&
                    cardLayout.Controls.Count > 1)
                {
                    contentArea = cardLayout.Controls[1] as Panel;
                    if (contentArea != null)
                    {
                        contentArea.Controls.Add(routesContainer);
                        LogDebug("CreateTodaysRoutesSection: Added routesContainer to contentArea successfully using hierarchy fallback");
                    }
                }
                
                if (contentArea == null)
                {
                    LogError("Failed to find content area in _todaysRoutesPanel for routes container - adding directly as fallback");
                    _todaysRoutesPanel.Controls.Add(routesContainer); // Last resort fallback
                }
            }

            _mainLayout.Controls.Add(_todaysRoutesPanel, 0, 1);

            // Register for data updates
            RegisterForRouteDataUpdates();
        }

        private void RegisterForRouteDataUpdates()
        {
            // This would hook into a data service to update the routes when data changes
            try
            {
                var routeService = _serviceProvider.GetService<BusBus.Services.IRouteService>();
                if (routeService != null)
                {
                    // Listen for route data changes and refresh the UI
                    // This would be implemented based on your data service's event model
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to register for route updates: {ex.Message}");
            }
        }

        private void CreateActionItemsSection()
        {
            _actionItemsPanel = CreateDashboardCard("⚠️ Action Items", ThemeManager.CurrentTheme.ButtonHoverBackground);

            // Extract the content area from the card
            var contentArea = _actionItemsPanel.Controls[0].Controls[1] as Panel;

            // Create a scrollable container for action items
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(5)
            };

            // Use a TableLayoutPanel for better control over item layout
            var actionsList = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(10),
                ColumnCount = 1,
                RowCount = 0, // Will be incremented as items are added
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            actionsList.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Full width column

            // Sample action items
            var actions = new[]
            {
                new { Task = "Bus #204 Oil Change", Due = "Today", Type = "Maintenance" },
                new { Task = "Fuel Bus #105", Due = "Before 6 AM", Type = "Fuel" },
                new { Task = "Driver Schedule Gap", Due = "Route B2", Type = "Staffing" },
                new { Task = "Bus #301 Inspection", Due = "This Week", Type = "Inspection" }
            };

            // Add action items to the layout
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                var actionItem = CreateActionItem(action.Task, action.Due, action.Type);

                // Add a new row for this item
                actionsList.RowCount++;
                actionsList.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                // Add the action item to the layout
                actionsList.Controls.Add(actionItem, 0, i);

                // Apply theme to ensure consistency
                ThemeManager.ApplyThemeToControl(actionItem);
            }

            // Add layout to scrollable panel
            scrollPanel.Controls.Add(actionsList);

            // Add scrollable panel to content area
            contentArea?.Controls.Add(scrollPanel);

            // Add to main layout
            _mainLayout.Controls.Add(_actionItemsPanel, 1, 1);
        }
        private void CreateQuickStatsSection()
        {
            _quickStatsPanel = CreateDashboardCard("📊 Quick Stats", Color.FromArgb(155, 89, 182));

            // Create StatisticsPanel for real-time dashboard statistics
            try
            {
                var statisticsService = _serviceProvider.GetService<BusBus.Services.IStatisticsService>();
                if (statisticsService != null)
                {
                    var statisticsPanel = new StatisticsPanel(statisticsService)
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Color.Transparent,
                        Padding = new Padding(20, 18, 20, 15) // Enhanced padding for better spacing and text display
                    };

                    // Apply glassmorphism styling to the statistics panel
                    ThemeManager.CurrentTheme.StyleGlassPanel(statisticsPanel);

                    _quickStatsPanel.Controls.Add(statisticsPanel);
                }
                else
                {
                    // Fallback to manual stats layout if statistics service is not available
                    CreateFallbackStatsLayout();
                }
            }
            catch (Exception ex)
            {
                // Log error and fall back to manual stats layout
                System.Diagnostics.Debug.WriteLine($"Failed to create StatisticsPanel: {ex.Message}");
                CreateFallbackStatsLayout();
            }

            _mainLayout.Controls.Add(_quickStatsPanel, 0, 2);
        }

        private void CreateFallbackStatsLayout()
        {
            var statsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(20, 18, 20, 15), // Enhanced padding for better spacing and text display
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Use AutoSize for columns and rows to prevent text truncation
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var stats = new[]
            {
                new { Label = "Active Routes Today", Value = "12", Icon = "🚌", Description = "routes scheduled" },
                new { Label = "Fleet Status", Value = "15/18", Icon = "✅", Description = "buses available" },
                new { Label = "Drivers On Duty", Value = "14", Icon = "👨‍💼", Description = "ready to drive" },
                new { Label = "Average Fuel", Value = "78%", Icon = "⛽", Description = "fleet fuel level" }
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var statCard = CreateStatCard(stats[i].Label, stats[i].Value, stats[i].Icon, stats[i].Description);
                statsLayout.Controls.Add(statCard, i % 2, i / 2);
            }

            _quickStatsPanel.Controls.Add(statsLayout);
        }

        private void CreateQuickActionsSection()
        {
            _quickActionsPanel = CreateDashboardCard("⚡ Quick Actions", Color.FromArgb(46, 204, 113)); var actionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(20, 25, 20, 20), // Enhanced padding for better text display
                AutoSize = true
            };

            // Use AutoSize for rows to prevent text truncation
            for (int i = 0; i < 4; i++)
            {
                actionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Auto-size for dynamic text content
            }

            var quickActions = new[]
            {
                new { Text = "🚌 Manage Routes", Action = "routes", Description = "View and edit route information" },
                new { Text = "👨‍💼 View Drivers", Action = "drivers", Description = "Schedule and manage drivers" },
                new { Text = "🔧 Maintenance Log", Action = "maintenance", Description = "Vehicle maintenance records" },
                new { Text = "📱 Emergency Contacts", Action = "emergency", Description = "Important contact information" }
            };

            for (int i = 0; i < quickActions.Length; i++)
            {
                var action = quickActions[i];
                var actionPanel = CreateActionButton(action.Text, action.Description, action.Action);
                actionsLayout.Controls.Add(actionPanel, 0, i);
            }

            _quickActionsPanel.Controls.Add(actionsLayout);
            _mainLayout.Controls.Add(_quickActionsPanel, 1, 2);
        }
        private static Panel CreateActionButton(string text, string description, string action)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                Margin = new Padding(4, 8, 4, 8), // Enhanced margins for better spacing
                Padding = new Padding(20, 16, 20, 16), // Enhanced padding for better text display
                Cursor = Cursors.Hand,
                MinimumSize = new Size(220, 70), // Increased minimum size for better text display
                AutoSize = true // Enable auto-sizing to accommodate content
            };

            // Apply glassmorphism styling for modern action button appearance
            ThemeManager.CurrentTheme.StyleModernCard(panel);

            // Add interactive gradient overlay for glassmorphism effect
            panel.Paint += (s, e) =>
            {
                if (s is Panel p)
                {
                    // Smooth rendering
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Add interactive glassmorphism gradient overlay
                    using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, p.Width, p.Height),
                        Color.FromArgb(15, Color.White),
                        Color.Transparent,
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(gradientBrush, 1, 1, p.Width - 2, p.Height - 2);
                    }
                }
            };// Main button text
            var textLabel = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                Location = new Point(12, 8), // Better positioning
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };

            // Description text
            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = ThemeManager.CurrentTheme.SecondaryText,
                Location = new Point(12, 30), // Better positioning with more space
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };            // Arrow indicator
            var arrowLabel = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                AutoSize = true,
                Location = new Point(175, 20) // Fixed position for better alignment
            };

            // Position arrow at right edge
            panel.Resize += (s, e) =>
            {
                if (s is Panel p)
                {
                    arrowLabel.Location = new Point(p.Width - arrowLabel.Width - 15, 14);
                }
            };

            panel.Controls.AddRange(new Control[] { textLabel, descLabel, arrowLabel });

            // Add hover effects
            panel.MouseEnter += (s, e) =>
            {
                panel.BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
                textLabel.ForeColor = Color.White;
                arrowLabel.ForeColor = Color.White;
            };
            panel.MouseLeave += (s, e) =>
            {
                panel.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                textLabel.ForeColor = ThemeManager.CurrentTheme.ButtonText;
                arrowLabel.ForeColor = ThemeManager.CurrentTheme.ButtonText;
            };
            // Add click handler
            panel.Click += (s, e) => QuickActionPanel_Click(action);

            // Also wire up the child controls to pass clicks to the panel
            foreach (Control c in panel.Controls)
            {
                c.Click += (s, e) => QuickActionPanel_Click(action);
                c.Cursor = Cursors.Hand;
            }

            return panel;
        }

        private static void QuickActionPanel_Click(string action)
        {
            MessageBox.Show($"Quick action: {action}\n\nThis would navigate to the {action} section.",
                "Dashboard Action", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }        /// <summary>
                 /// Creates a themed dashboard card with support for dynamic content resizing
                 /// </summary>
                 /// <param name="title">Card title text</param>
                 /// <param name="accentColor">Accent color for card header</param>
                 /// <returns>A themed panel ready to host content</returns>        private static Panel CreateDashboardCard(string title, Color accentColor)
        {
            System.Diagnostics.Debug.WriteLine($"CreateDashboardCard: Creating card for '{title}'");
            
            // Create the main card container
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Margin = new Padding(5),
                Padding = new Padding(0),
                Tag = "ModernCard" // For theme handling
            };

            // Apply the theme's card styling
            ThemeManager.CurrentTheme.StyleModernCard(card);

            // Use a TableLayoutPanel for better content organization
            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2, // Header and content areas
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            
            System.Diagnostics.Debug.WriteLine($"CreateDashboardCard: Created cardLayout with {cardLayout.RowCount} rows, {cardLayout.ColumnCount} columns");

            // Set up row styles
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Fixed header height
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content fills remaining space

            // Set up column style
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Create the header panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 5, 15, 5)
            };

            // Add title with shadow effect for depth
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                AutoSize = true,
                Location = new Point(5, 10),
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            // Add accent line for visual distinction
            headerPanel.Paint += (s, e) =>
            {
                if (s is Panel panel)
                {
                    // Add accent color at top
                    using (var accentBrush = new SolidBrush(accentColor))
                    {
                        e.Graphics.FillRectangle(accentBrush, 0, 0, panel.Width, 4);
                    }
                }
            };

            // Add title to header
            headerPanel.Controls.Add(titleLabel);

            // Add header to the card layout
            cardLayout.Controls.Add(headerPanel, 0, 0);

            // Create content area that will host the actual content
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(1) // Minimal padding to allow content to control its own spacing
            };

            // Add content area to the card layout
            cardLayout.Controls.Add(contentArea, 0, 1);

            // Add the layout to the card
            card.Controls.Add(cardLayout);

            // Ensure the content area is accessible for adding actual content
            card.Tag = new { Title = title, ContentArea = contentArea, AccentColor = accentColor };

            return card;
        }

        private static Panel CreateRouteItem(string routeName, string time, string status, string priority)
        {
            var item = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1), // Slightly elevated
                Margin = new Padding(0, 6, 0, 6), // Consistent margin between items
                Padding = new Padding(20, 16, 20, 16), // Increased padding for text readability
                Cursor = Cursors.Hand // Indicate it's clickable
            };

            // Apply glassmorphism styling for modern route item appearance
            ThemeManager.CurrentTheme.StyleGlassPanel(item);

            // Add custom paint for status indicator and route-specific effects
            item.Paint += (s, e) =>
            {
                if (s is Panel panel)
                {
                    // Smooth rendering for crisp text and borders
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Status indicator dot - floating in top right with glassmorphism glow
                    var statusColor = status == "Ready" ?
                        Color.FromArgb(39, 174, 96) : // Green for ready
                        Color.FromArgb(230, 126, 34); // Orange for pending

                    // Add subtle glow around status dot
                    using (var glowBrush = new SolidBrush(Color.FromArgb(40, statusColor)))
                    {
                        e.Graphics.FillEllipse(glowBrush, panel.Width - 22, 8, 12, 12);
                    }

                    using (var brush = new SolidBrush(statusColor))
                    {
                        e.Graphics.FillEllipse(brush, panel.Width - 20, 10, 8, 8);
                    }
                }
            };

            // Create a layout panel for the text content
            var textLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 30, 0), // Right padding to avoid status dot overlap
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Create directly positioned labels for best rendering quality and minimal layout issues
            var routeLabel = new Label
            {
                Text = routeName,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Theme.EnsureAccessibleContrast(ThemeManager.CurrentTheme.HeadlineText,
                                                         ThemeManager.CurrentTheme.GetElevatedBackground(1)),
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 5)
            };

            var timeLabel = new Label
            {
                Text = time,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Theme.EnsureAccessibleContrast(ThemeManager.CurrentTheme.CardText,
                                                         ThemeManager.CurrentTheme.GetElevatedBackground(1)),
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 5)
            };

            var priorityLabel = new Label
            {
                Text = priority,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Theme.EnsureAccessibleContrast(ThemeManager.CurrentTheme.CardText,
                                                         ThemeManager.CurrentTheme.GetElevatedBackground(1)),
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            // Add labels to the layout in correct order
            textLayoutPanel.Controls.Add(routeLabel, 0, 0);
            textLayoutPanel.Controls.Add(timeLabel, 0, 1);
            textLayoutPanel.Controls.Add(priorityLabel, 0, 2);

            // Add the layout panel to the item
            item.Controls.Add(textLayoutPanel);

            // Add hover effects
            item.MouseEnter += (s, e) =>
            {
                item.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(2);
            };

            item.MouseLeave += (s, e) =>
            {
                item.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1);
            };

            // Make text labels clickable for better interaction
            Action itemClickAction = () =>
            {
                MessageBox.Show($"Route details for {routeName}\nTime: {time}\nStatus: {status}\nPriority: {priority}",
                    "Route Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            routeLabel.Click += (s, e) => itemClickAction();
            timeLabel.Click += (s, e) => itemClickAction();
            priorityLabel.Click += (s, e) => itemClickAction();
            item.Click += (s, e) => itemClickAction();

            routeLabel.Cursor = Cursors.Hand;
            timeLabel.Cursor = Cursors.Hand;
            priorityLabel.Cursor = Cursors.Hand;

            return item;
        }
        private static Panel CreateActionItem(string task, string due, string type)
        {
            var item = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1), // Slightly elevated
                Margin = new Padding(0, 6, 0, 6), // Consistent margin between items
                Padding = new Padding(20, 16, 20, 16), // Increased padding for text readability
                Cursor = Cursors.Hand // Indicate it's clickable
            };

            // Apply glassmorphism styling for modern action item appearance
            ThemeManager.CurrentTheme.StyleGlassPanel(item);

            // Add custom paint for priority indicators and action-specific effects
            item.Paint += (s, e) =>
            {
                if (s is Panel panel)
                {
                    // Smooth rendering
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Left accent line with glassmorphism gradient for urgency
                    using (var accentBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, 3, panel.Height),
                        Color.FromArgb(231, 76, 60), // Start with red
                        Color.FromArgb(192, 57, 43), // End with darker red
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillRectangle(accentBrush, 0, 0, 3, panel.Height);
                    }

                    // Glassmorphism checkbox indicator with glow
                    using (var glowBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                    {
                        e.Graphics.FillRectangle(glowBrush, panel.Width - 22, panel.Height / 2 - 10, 18, 18);
                    }
                    using (var borderPen = new Pen(ThemeManager.CurrentTheme.GlowColor, 1))
                    {
                        e.Graphics.DrawRectangle(borderPen, panel.Width - 20, panel.Height / 2 - 8, 14, 14);
                    }
                }
            };

            // Create a layout panel to position the text elements properly
            var textLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(5, 0, 30, 0), // Right padding to avoid checkbox overlap
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Glass-like text labels with proper sizing
            var taskLabel = new Label
            {
                Text = task,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 57, 43),
                AutoSize = true,
                AutoEllipsis = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 5)
            };

            var dueLabel = new Label
            {
                Text = $"Due: {due}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(231, 76, 60),
                AutoSize = true,
                AutoEllipsis = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 5)
            };

            var typeLabel = new Label
            {
                Text = type,
                Font = new Font("Segoe UI", 9F),
                ForeColor = ThemeManager.CurrentTheme.CardText,
                AutoSize = true,
                AutoEllipsis = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                UseMnemonic = false
            };

            // Add labels to the layout in correct order
            textLayoutPanel.Controls.Add(taskLabel, 0, 0);
            textLayoutPanel.Controls.Add(dueLabel, 0, 1);
            textLayoutPanel.Controls.Add(typeLabel, 0, 2);

            // Add the layout panel to the item
            item.Controls.Add(textLayoutPanel);

            // Add hover effects
            item.MouseEnter += (s, e) =>
            {
                item.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(2);
                taskLabel.ForeColor = Color.FromArgb(231, 76, 60); // Brighter on hover
            };
            item.MouseLeave += (s, e) =>
            {
                item.BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1);
                taskLabel.ForeColor = Color.FromArgb(192, 57, 43); // Back to normal
            };            // Make the panel interactive
            item.Click += (s, e) =>
            {
                var result = MessageBox.Show($"Action required: {task}\nDue: {due}\nType: {type}\n\nMark as complete?",
                    "Action Item", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Visual feedback for completion
                    item.Enabled = false;
                    item.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    taskLabel.Font = new Font(taskLabel.Font, FontStyle.Strikeout);
                    taskLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    dueLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    typeLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    item.Cursor = Cursors.Default;

                    // Disable all text labels
                    taskLabel.Enabled = false;
                    dueLabel.Enabled = false;
                    typeLabel.Enabled = false;
                    taskLabel.Cursor = Cursors.Default;
                    dueLabel.Cursor = Cursors.Default;
                    typeLabel.Cursor = Cursors.Default;
                }
            };            // Shared click handler for all interactive elements
            EventHandler sharedClickHandler = (s, e) =>
            {
                var result = MessageBox.Show($"Action required: {task}\nDue: {due}\nType: {type}\n\nMark as complete?",
                    "Action Item", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Visual feedback for completion
                    item.Enabled = false;
                    item.BackColor = ThemeManager.CurrentTheme.ButtonDisabledBackground;
                    taskLabel.Font = new Font(taskLabel.Font, FontStyle.Strikeout);
                    taskLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    dueLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    typeLabel.ForeColor = ThemeManager.CurrentTheme.ButtonDisabledText;
                    item.Cursor = Cursors.Default;

                    // Disable all text labels
                    taskLabel.Enabled = false;
                    dueLabel.Enabled = false;
                    typeLabel.Enabled = false;
                    taskLabel.Cursor = Cursors.Default;
                    dueLabel.Cursor = Cursors.Default;
                    typeLabel.Cursor = Cursors.Default;
                }
            };

            // Make text labels clickable for better interaction
            taskLabel.Click += sharedClickHandler;
            dueLabel.Click += sharedClickHandler;
            typeLabel.Click += sharedClickHandler;

            taskLabel.Cursor = Cursors.Hand;
            dueLabel.Cursor = Cursors.Hand;
            typeLabel.Cursor = Cursors.Hand;

            return item;
        }
        private static Panel CreateStatCard(string label, string value, string icon, string description)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.GetElevatedBackground(1), // Slightly elevated background
                Margin = new Padding(6, 6, 6, 6), // Good margin between stat cards
                Padding = new Padding(10, 8, 10, 8), // Reduced padding slightly
                MinimumSize = new Size(150, 90) // Increased minimum size for better readability
            };

            // Apply glassmorphism styling for modern stat card appearance
            ThemeManager.CurrentTheme.StyleGlassPanel(card);

            // Use TableLayoutPanel for more precise control over layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            // Configure rows and columns
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38)); // Icon column
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Content column

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // Value row
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label row
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Description row

            // Icon with glow effect
            var iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 18F), // Larger icon for visibility
                ForeColor = Color.FromArgb(155, 89, 182), // Use the stats panel accent color
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            // Value display
            var valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold), // Larger, bolder value for emphasis
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            // Label text
            var labelLabel = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular), // Readable label text
                ForeColor = ThemeManager.CurrentTheme.CardText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoEllipsis = false // Don't truncate with ellipsis
            };

            // Description text
            var descriptionLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = ThemeManager.CurrentTheme.CardText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoEllipsis = false // Don't truncate with ellipsis
            };

            // Add controls to layout
            layout.Controls.Add(iconLabel, 0, 0);
            layout.Controls.Add(valueLabel, 1, 0);
            layout.Controls.Add(labelLabel, 0, 1);
            layout.SetColumnSpan(labelLabel, 2); // Span across both columns
            layout.Controls.Add(descriptionLabel, 0, 2);
            layout.SetColumnSpan(descriptionLabel, 2); // Span across both columns

            // Add the layout to the card
            card.Controls.Add(layout);

            return card;
        }
        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Reset stopwatch for activation timing
            _loadingStopwatch.Restart();
            _timingMetrics.Clear();
            _timingMetrics["OnActivateAsync_Start"] = _loadingStopwatch.ElapsedMilliseconds;

            LogDebug($"OnActivateAsync: Dashboard view activation started");
            UpdateStatus("Loading dashboard data...", StatusType.Info);

            try
            {
                // Track each phase of loading
                _timingMetrics["BeforeDataRefresh"] = _loadingStopwatch.ElapsedMilliseconds;
                LogDebug($"OnActivateAsync: Starting data refresh");

                // Update the UI with latest data
                await RefreshDashboardDataAsync(cancellationToken);

                _timingMetrics["AfterDataRefresh"] = _loadingStopwatch.ElapsedMilliseconds;
                LogDebug($"OnActivateAsync: Data refresh completed in {_timingMetrics["AfterDataRefresh"] - _timingMetrics["BeforeDataRefresh"]}ms");
                // Log detailed timing metrics
                foreach (var metric in _timingMetrics)
                {
                    LogMetric(metric.Key, metric.Value);
                }

                UpdateStatus("Dashboard loaded", StatusType.Success);
                LogPerformance("Dashboard Activation", _loadingStopwatch.ElapsedMilliseconds, _uiComponentsLoaded);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading dashboard: {ex.Message}", StatusType.Error);
                LogError($"Error activating dashboard view: {ex.Message}", ex);

                // Even though there was an error, log the timing data we have
                LogWarning($"Dashboard activation failed after {_loadingStopwatch.ElapsedMilliseconds}ms");
            }
        }
        private async Task RefreshDashboardDataAsync(CancellationToken cancellationToken)
        {
            // Ensure we're on the UI thread for rendering operations
            if (InvokeRequired)
            {
                await Task.Run(() => Invoke(new Func<Task>(async () => await RefreshDashboardDataAsync(cancellationToken))));
                return;
            }

            _timingMetrics["RefreshData_Start"] = _loadingStopwatch.ElapsedMilliseconds;
            LogDebug("RefreshDashboardDataAsync: Starting data refresh");

            // Reset the UI loading counter
            _uiComponentsLoaded = 0;

            try
            {
                // This is where we'd load data from various services
                var tasks = new List<Task>();

                // Add tasks to refresh each part of the dashboard
                tasks.Add(RefreshRoutesDataAsync(cancellationToken));
                tasks.Add(RefreshStatsDataAsync(cancellationToken));
                tasks.Add(RefreshActionsDataAsync(cancellationToken));

                // Wait for all refresh tasks to complete
                await Task.WhenAll(tasks);

                _timingMetrics["RefreshData_Complete"] = _loadingStopwatch.ElapsedMilliseconds;
                var elapsed = _timingMetrics["RefreshData_Complete"] - _timingMetrics["RefreshData_Start"];
                LogDebug($"RefreshDashboardDataAsync: All data refreshed in {elapsed}ms");
            }
            catch (Exception ex)
            {
                LogError($"Error during dashboard data refresh: {ex.Message}", ex);
                throw; // Rethrow to let the caller handle it
            }
        }
        private async Task RefreshRoutesDataAsync(CancellationToken cancellationToken)
        {
            _timingMetrics["RefreshRoutes_Start"] = _loadingStopwatch.ElapsedMilliseconds; LogDebug("Starting routes data refresh");

            // Get the routes list control from the panel (declare outside try block for exception handler)
            TableLayoutPanel? routesList = _todaysRoutesPanel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (routesList == null)
            {
                LogWarning($"Routes list control not found in panel with {_todaysRoutesPanel.Controls.Count} controls");
                // Log details about existing controls for debugging
                foreach (Control control in _todaysRoutesPanel.Controls)
                {
                    LogDebug($"Found control: {control.GetType().Name}, Name: {control.Name}");
                }
                return;
            }

            try
            {
                // Access the route service to get routes data
                var routeService = _serviceProvider.GetService<BusBus.Services.IRouteService>();
                if (routeService == null)
                {
                    LogWarning("Route service not available");
                    DisplayLoadingError(_todaysRoutesPanel, "Route service unavailable");
                    return;
                }

                // Variable to hold the routes data
                object? routesObj = null;

                // Show loading indicator while we fetch data
                var loadingPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = ThemeManager.CurrentTheme.CardBackground
                };

                var loadingLabel = new Label
                {
                    Text = "Loading route data...",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = ThemeManager.CurrentTheme.CardText,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };

                loadingPanel.Controls.Add(loadingLabel);

                // Store reference to original content
                Control? originalContent = null;
                if (_todaysRoutesPanel.Controls.Count > 0)
                {
                    originalContent = _todaysRoutesPanel.Controls[0];
                    _todaysRoutesPanel.Controls.Clear();
                }

                // Show loading indicator
                _todaysRoutesPanel.Controls.Add(loadingPanel);

                // Start performance tracking for data loading
                var dataLoadStartTime = _loadingStopwatch.ElapsedMilliseconds;                // Load routes using the correct async method
                try
                {
                    var routes = await routeService.GetRoutesAsync(cancellationToken);
                    routesObj = routes;
                }
                catch (Exception ex)
                {
                    LogError("Failed to load routes", ex);
                    DisplayRouteLoadError(routesList, "Failed to load route data");
                    return;
                }

                // Record data loading time
                var dataLoadTime = _loadingStopwatch.ElapsedMilliseconds;
                LogMetric("RouteDataLoad", dataLoadTime);

                // Process the routes
                int routesAdded = 0;

                if (routesObj != null)
                {
                    // Convert to a list we can iterate through
                    var routesList2 = routesObj as System.Collections.IEnumerable;
                    if (routesList2 != null)
                    {
                        int rowIndex = 1; // Start after header

                        foreach (var route in routesList2)
                        {
                            // Check for cancellation periodically
                            if (routesAdded % 5 == 0)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }

                            // Extract route properties using reflection to handle different route models
                            string routeName = GetPropertyValue<string>(route, "Name") ??
                                            GetPropertyValue<string>(route, "RouteName") ??
                                            $"Route {GetPropertyValue<object>(route, "RouteID") ?? routesAdded}";

                            DateTime scheduleTime = GetPropertyValue<DateTime>(route, "ScheduledTime");
                            bool isActive = GetPropertyValue<bool>(route, "IsActive");

                            // Skip inactive routes
                            if (!isActive)
                            {
                                continue;
                            }

                            // Determine status and priority
                            string status = "Scheduled";
                            string priority = "Normal";

                            // Higher priority for today's routes
                            if (scheduleTime.Date == DateTime.Today)
                            {
                                priority = "High";
                            }

                            // Create the route UI element
                            var routeItem = CreateRouteItem(
                                routeName,
                                scheduleTime.ToString("h:mm tt"),
                                status,
                                priority);

                            // Add to UI
                            routesList.RowCount++;
                            routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                            routesList.Controls.Add(routeItem, 0, rowIndex);

                            ThemeManager.ApplyThemeToControl(routeItem);

                            routesAdded++;
                            rowIndex++;
                            _uiComponentsLoaded++;
                        }
                    }
                }

                // If no routes were added, show a message
                if (routesAdded == 0)
                {
                    var noRoutesLabel = new Label
                    {
                        Text = "No routes scheduled for today",
                        Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                        ForeColor = ThemeManager.CurrentTheme.CardText,
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(0, 20, 0, 20)
                    };

                    routesList.RowCount++;
                    routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    routesList.Controls.Add(noRoutesLabel, 0, 1);
                    _uiComponentsLoaded++;
                }

                // Refresh the layout
                routesList.PerformLayout();
            }
            catch (OperationCanceledException)
            {
                // Task was canceled - this is expected behavior when navigating away
                LogDebug("Route data refresh canceled");
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing routes data: {ex.Message}", ex);
                DisplayRouteLoadError(routesList, "Error loading routes");
            }
        }

        // Helper to display a route loading error message
        private void DisplayRouteLoadError(TableLayoutPanel routesList, string message)
        {
            var errorLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 20, 0, 20)
            }; routesList.RowCount = Math.Max(2, routesList.RowCount);
            routesList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            routesList.Controls.Add(errorLabel, 0, 1);
            _uiComponentsLoaded++;
        }        // Helper to display a loading error message in a panel
        private static void DisplayLoadingError(Panel container, string message)
        {
            container.Controls.Clear();

            var errorLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 20, 0, 20)
            };

            container.Controls.Add(errorLabel);
        }

        private async Task RefreshStatsDataAsync(CancellationToken cancellationToken)
        {
            _timingMetrics["RefreshStats_Start"] = _loadingStopwatch.ElapsedMilliseconds;
            LogDebug("Starting statistics data refresh");
            try
            {                // Access the stats service
                var statsService = _serviceProvider.GetService<IStatisticsService>();
                if (statsService == null)
                {
                    LogWarning("Stats service not available");
                    return;
                }

                // Clear existing stats before repopulating
                if (_quickStatsPanel?.Controls.Count > 0 && _quickStatsPanel.Controls[0] is TableLayoutPanel statsLayout)
                {                    // Store the header if there is one
                    Control? headerControl = null;
                    if (statsLayout.RowCount > 0)
                    {
                        var headerCandidate = statsLayout.GetControlFromPosition(0, 0);
                        if (headerCandidate != null && headerCandidate.Tag as string == "header")
                        {
                            headerControl = headerCandidate;
                            statsLayout.Controls.Remove(headerControl);
                        }
                    }

                    // Clear existing stats
                    statsLayout.Controls.Clear();

                    // Re-add header if it existed
                    if (headerControl != null)
                    {
                        statsLayout.Controls.Add(headerControl, 0, 0);
                    }

                    // Try to get stats
                    try
                    {
                        // Check for cancellation
                        cancellationToken.ThrowIfCancellationRequested();

                        // Get stats data
                        var todayRouteCount = await GetStatsValueAsync(statsService, "TodayRouteCount", 0);
                        var weeklyCompletion = await GetStatsValueAsync(statsService, "WeeklyCompletionRate", 94.5);
                        var pendingIssues = await GetStatsValueAsync(statsService, "PendingIssueCount", 2);
                        var fuelEfficiency = await GetStatsValueAsync(statsService, "FuelEfficiency", 11.3);

                        // Update UI with the stats
                        var statCard1 = CreateStatCard("Today's Routes", todayRouteCount.ToString(), "📅", "Routes scheduled for today");
                        var statCard2 = CreateStatCard("Completion Rate", $"{weeklyCompletion}%", "✓", "Last 7 days completion rate");
                        var statCard3 = CreateStatCard("Pending Issues", pendingIssues.ToString(), "⚠️", "Issues needing attention");
                        var statCard4 = CreateStatCard("Fuel Efficiency", $"{fuelEfficiency} mpg", "⛽", "Fleet average this week");

                        // Add stats to the layout
                        statsLayout.Controls.Add(statCard1, 0, 0);
                        statsLayout.Controls.Add(statCard2, 1, 0);
                        statsLayout.Controls.Add(statCard3, 0, 1);
                        statsLayout.Controls.Add(statCard4, 1, 1);

                        // Register controls for theme changes
                        ThemeManager.ApplyThemeToControl(statCard1);
                        ThemeManager.ApplyThemeToControl(statCard2);
                        ThemeManager.ApplyThemeToControl(statCard3);
                        ThemeManager.ApplyThemeToControl(statCard4);

                        _uiComponentsLoaded += 4;
                    }
                    catch (OperationCanceledException)
                    {
                        LogDebug("Stats data refresh canceled");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error loading stats data: {ex.Message}", ex);

                        // Show error in UI
                        var errorLabel = new Label
                        {
                            Text = "Error loading statistics",
                            Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                            ForeColor = Color.Red,
                            BackColor = Color.Transparent,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill,
                            Padding = new Padding(10)
                        };

                        statsLayout.Controls.Add(errorLabel, 0, 0);
                        statsLayout.SetColumnSpan(errorLabel, 2);
                        _uiComponentsLoaded++;
                    }

                    // Refresh layout
                    statsLayout.PerformLayout();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing stats data: {ex.Message}", ex);
            }

            _timingMetrics["RefreshStats_End"] = _loadingStopwatch.ElapsedMilliseconds;
            LogDebug($"Statistics data refreshed in {_timingMetrics["RefreshStats_End"] - _timingMetrics["RefreshStats_Start"]}ms");
        }

        private async Task<T> GetStatsValueAsync<T>(object statsService, string statName, T defaultValue)
        {
            try
            {
                // Try to find a method for getting this specific stat
                var method = statsService.GetType().GetMethod($"Get{statName}");
                if (method != null)
                {
                    // Method exists, invoke it
                    if (method.ReturnType.IsAssignableFrom(typeof(Task<T>)))
                    {
                        // Async method
                        var task = (Task<T>)method.Invoke(statsService, null)!;
                        return await task;
                    }
                    else
                    {
                        // Sync method
                        return (T)method.Invoke(statsService, null)!;
                    }
                }

                // Try to find a general GetStat method
                method = statsService.GetType().GetMethod("GetStat") ??
                         statsService.GetType().GetMethod("GetStatistic");

                if (method != null)
                {
                    // We found a method, try to invoke it with the stat name
                    if (method.ReturnType.IsAssignableFrom(typeof(Task<T>)))
                    {
                        // Async method
                        var task = (Task<T>)method.Invoke(statsService, new object[] { statName })!;
                        return await task;
                    }
                    else
                    {
                        // Sync method
                        return (T)method.Invoke(statsService, new object[] { statName })!;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error getting stat {statName}: {ex.Message}");
            }

            return defaultValue;
        }

        private Task RefreshActionsDataAsync(CancellationToken cancellationToken)
        {
            _timingMetrics["RefreshActions_Start"] = _loadingStopwatch.ElapsedMilliseconds;
            LogDebug("Starting actions data refresh");
            try
            {                // Action item service is not currently implemented
                LogDebug("Action item service not implemented - skipping action items refresh");

                // Show placeholder message in action items panel
                if (_actionItemsPanel?.Controls.Count > 0 && _actionItemsPanel.Controls[0] is TableLayoutPanel actionItemsContainer)
                {
                    var actionsList = actionItemsContainer.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                    if (actionsList != null)
                    {
                        actionsList.Controls.Clear();
                        var placeholderLabel = new Label
                        {
                            Text = "Action items feature coming soon...",
                            Font = new Font("Segoe UI", 10, FontStyle.Italic),
                            ForeColor = ThemeManager.CurrentTheme.CardText, // Use CardText instead of TextSecondary
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill,
                            AutoSize = false,
                            Height = 40
                        };
                        actionsList.Controls.Add(placeholderLabel);
                    }
                }
                LogDebug($"Action items refresh completed - placeholder shown");
                _timingMetrics["RefreshActions_End"] = _loadingStopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                LogError($"Error refreshing action items: {ex.Message}", ex);
            }

            LogDebug($"Actions data refreshed in {_timingMetrics["RefreshActions_End"] - _timingMetrics["RefreshActions_Start"]}ms");
            return Task.CompletedTask;
        }

        // Helper to display action item load error
        private void DisplayActionLoadError(Control container, string message)
        {
            var errorLabel = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                ForeColor = ThemeManager.CurrentTheme.CardText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 20),
                AutoSize = true
            };

            container.Controls.Add(errorLabel);
            _uiComponentsLoaded++;
        }

        private static T? GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null)
                return default;

            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
                return default;

            try
            {
                var value = property.GetValue(obj);
                if (value == null)
                    return default;

                return (T)value;
            }
            catch
            {
                return default;
            }
        }

        protected override Task OnDeactivateAsync()
        {
            // Called when the dashboard view becomes inactive
            // Here we could stop timers, save state, etc.
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of dashboard-specific resources if needed
            }
            base.Dispose(disposing);
        }
#pragma warning restore CA1822 // Mark members as static
    }
}
