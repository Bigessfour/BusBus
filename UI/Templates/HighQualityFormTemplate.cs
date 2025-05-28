#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BusBus.UI;
using BusBus.Utils;

namespace BusBus.UI.Templates
{
    /// <summary>
    /// High-quality form template with optimized text rendering and layout capabilities.
    /// Use this as a base for new forms that require excellent display quality and text readability.
    /// </summary>
    public abstract class HighQualityFormTemplate : BaseView
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger<HighQualityFormTemplate> _logger;
        protected TableLayoutPanel _mainLayout;
        protected bool _useGlassmorphism = true;
        protected bool _highAccessibilityMode = false;

        /// <summary>
        /// Creates a new high-quality form with optimized text rendering
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        protected HighQualityFormTemplate(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = serviceProvider.GetRequiredService<ILogger<HighQualityFormTemplate>>();
            _mainLayout = new TableLayoutPanel();

            // Enable high DPI support
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            this.AutoScaleMode = AutoScaleMode.Dpi;

            // Don't call InitializeView here - derived classes will call it
        }

        /// <summary>
        /// Initializes the view with high-quality text rendering and layout
        /// </summary>
        protected override void InitializeView()
        {
            base.InitializeView();

            // Fill the parent container
            this.Dock = DockStyle.Fill;

            // Apply theme
            ThemeManager.ApplyThemeToControl(this);

            // Set up main layout with proper padding and spacing
            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UiConstants.DefaultPadding),
                BackColor = ThemeManager.CurrentTheme.MainBackground,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                AutoSize = false
            };

            // Add to controls
            this.Controls.Add(_mainLayout);

            // Apply high-quality text rendering
            TextRenderingManager.RegisterForHighQualityTextRendering(this);

            // Set up theme change listener
            ThemeManager.ThemeChanged += (s, e) => RefreshTheme();

            // Check for text truncation
            LayoutDebugger.EnableDebugMode();
            LayoutDebugger.MonitorForTruncationOnResize(this.FindForm());
        }

        /// <summary>
        /// Creates a standard header section with proper text rendering
        /// </summary>
        protected Panel CreateHeaderSection(string title, string subtitle = null)
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Margin = new Padding(UiConstants.SmallPadding),
                Padding = new Padding(UiConstants.DefaultPadding),
                Tag = _useGlassmorphism ? "ModernCard" : "Header"
            };

            // Apply glassmorphic styling if enabled
            if (_useGlassmorphism)
            {
                ThemeManager.CurrentTheme.StyleModernCard(headerPanel);
            }

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = subtitle != null ? 2 : 1,
                BackColor = Color.Transparent,
                AutoSize = true
            };

            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (subtitle != null)
            {
                headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            // Title label with high-quality rendering
            var titleLabel = new Label
            {
                Text = title,
                Font = ThemeManager.CurrentTheme.HeadlineFont,
                ForeColor = _useGlassmorphism
                    ? ThemeManager.CurrentTheme.GlassmorphicTextColor
                    : ThemeManager.CurrentTheme.HeadlineText,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, subtitle != null ? UiConstants.SmallPadding : 0),
                Tag = "important"
            };

            headerLayout.Controls.Add(titleLabel, 0, 0);

            // Optional subtitle
            if (subtitle != null)
            {
                var subtitleLabel = new Label
                {
                    Text = subtitle,
                    Font = ThemeManager.CurrentTheme.CardFont,
                    ForeColor = _useGlassmorphism
                        ? ThemeManager.CurrentTheme.GlassmorphicSecondaryTextColor
                        : ThemeManager.CurrentTheme.SecondaryText,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                headerLayout.Controls.Add(subtitleLabel, 0, 1);
            }

            headerPanel.Controls.Add(headerLayout);
            return headerPanel;
        }

        /// <summary>
        /// Creates a standard content card with high-quality text rendering
        /// </summary>
        protected Panel CreateContentCard(string title = null, bool useGlass = true)
        {
            var cardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.CardBackground,
                Margin = new Padding(UiConstants.SmallPadding),
                Padding = new Padding(UiConstants.DefaultPadding),
                MinimumSize = new Size(UiConstants.CardPanelWidth / 2, 0),
                Tag = useGlass ? "ModernCard" : "ContentCard"
            };

            // Apply glassmorphic styling if enabled
            if (useGlass && _useGlassmorphism)
            {
                ThemeManager.CurrentTheme.StyleModernCard(cardPanel);
            }

            if (!string.IsNullOrEmpty(title))
            {
                var cardLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    BackColor = Color.Transparent
                };

                cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var titleLabel = new Label
                {
                    Text = title,
                    Font = new Font(ThemeManager.CurrentTheme.CardFont.FontFamily,
                                   ThemeManager.CurrentTheme.CardFont.Size,
                                   FontStyle.Bold),
                    ForeColor = useGlass && _useGlassmorphism
                        ? ThemeManager.CurrentTheme.GlassmorphicTextColor
                        : ThemeManager.CurrentTheme.CardText,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 0, UiConstants.SmallPadding),
                    Tag = "important"
                };

                var contentPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent
                };

                cardLayout.Controls.Add(titleLabel, 0, 0);
                cardLayout.Controls.Add(contentPanel, 0, 1);

                cardPanel.Controls.Add(cardLayout);
                return cardPanel;
            }
            else
            {
                return cardPanel;
            }
        }

        /// <summary>
        /// Creates a high-quality button with proper text rendering
        /// </summary>
        protected static Button CreateStyledButton(string text, EventHandler clickHandler = null)
        {
            var button = new Button
            {
                Text = text,
                Font = ThemeManager.CurrentTheme.ButtonFont,
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                MinimumSize = new Size(100, UiConstants.DefaultButtonHeight),
                Padding = new Padding(UiConstants.SmallPadding),
                Margin = new Padding(UiConstants.SmallPadding),
                AutoEllipsis = true,
                UseVisualStyleBackColor = false
            };

            // Add hover effect
            button.MouseEnter += (s, e) =>
            {
                button.BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
                button.ForeColor = ThemeManager.CurrentTheme.ButtonHoverText;
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = ThemeManager.CurrentTheme.ButtonBackground;
                button.ForeColor = ThemeManager.CurrentTheme.ButtonText;
            };

            // Add click handler if provided
            if (clickHandler != null)
            {
                button.Click += clickHandler;
            }

            return button;
        }

        /// <summary>
        /// Creates a high-quality label with proper text rendering
        /// </summary>
        protected Label CreateStyledLabel(string text, bool isTitle = false, bool isImportant = false)
        {
            var label = new Label
            {
                Text = text,
                Font = isTitle
                    ? ThemeManager.CurrentTheme.HeadlineFont
                    : ThemeManager.CurrentTheme.CardFont,
                ForeColor = _useGlassmorphism
                    ? ThemeManager.CurrentTheme.GlassmorphicTextColor
                    : (isTitle
                        ? ThemeManager.CurrentTheme.HeadlineText
                        : ThemeManager.CurrentTheme.CardText),
                AutoSize = true,
                Margin = new Padding(UiConstants.SmallPadding),
                AutoEllipsis = true,
                Tag = isImportant ? "important" : null
            };

            if (isTitle)
            {
                label.Font = new Font(label.Font, FontStyle.Bold);
            }

            return label;
        }

        /// <summary>
        /// Refreshes the theme on the form and all controls
        /// </summary>
        protected void RefreshTheme()
        {
            ThemeManager.ApplyThemeToControl(this);

            // Apply glassmorphic text color to all glassmorphic panels
            if (_useGlassmorphism)
            {
                ThemeManager.EnforceGlassmorphicTextColor(this);
            }

            // Check for truncation issues after theme change (font may have changed)
            var truncationIssues = LayoutDebugger.DetectTextTruncation(this);
            if (truncationIssues.Count > 0)
            {
#pragma warning disable CA1848
                _logger.LogWarning($"Theme change caused {truncationIssues.Count} truncation issues");
#pragma warning restore CA1848
                LayoutDebugger.FixTextTruncation(this);
            }
        }

        /// <summary>
        /// Sets high accessibility mode which enhances contrast and reduces glass effects
        /// </summary>
        public void SetHighAccessibilityMode(bool enabled)
        {
            _highAccessibilityMode = enabled;
            _useGlassmorphism = !enabled; // Disable glassmorphism in high accessibility mode

            RefreshTheme();

            // Set color contrast mode in the theme if supported
            if (ThemeManager.CurrentTheme is IAccessibleTheme accessibleTheme)
            {
                accessibleTheme.SetHighContrastMode(enabled);
                RefreshTheme();
            }
        }

        /// <summary>
        /// Sets glassmorphism mode for visual styling
        /// </summary>
        public void SetGlassmorphismMode(bool enabled)
        {
            if (_highAccessibilityMode && enabled)
            {
#pragma warning disable CA1848
                _logger.LogWarning("Cannot enable glassmorphism in high accessibility mode");
#pragma warning restore CA1848
                return;
            }

            _useGlassmorphism = enabled;
            RefreshTheme();
        }

        /// <summary>
        /// Creates a grid with proper text rendering settings
        /// </summary>
        protected static DataGridView CreateStyledDataGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeManager.CurrentTheme.GridBackground,
                BorderStyle = BorderStyle.None,
                GridColor = ThemeManager.CurrentTheme.BorderColor,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };

            // Apply high-quality text rendering
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = ThemeManager.CurrentTheme.HeadlineBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(
                ThemeManager.CurrentTheme.CardFont.FontFamily,
                ThemeManager.CurrentTheme.CardFont.Size,
                FontStyle.Bold);
            grid.ColumnHeadersHeight = UiConstants.GridHeaderHeight;

            grid.DefaultCellStyle.BackColor = ThemeManager.CurrentTheme.MainBackground;
            grid.DefaultCellStyle.ForeColor = ThemeManager.CurrentTheme.CardText;
            grid.DefaultCellStyle.Font = ThemeManager.CurrentTheme.CardFont;
            grid.RowTemplate.Height = UiConstants.GridRowHeight;

            // Selection colors
            grid.DefaultCellStyle.SelectionBackColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
            grid.DefaultCellStyle.SelectionForeColor = ThemeManager.CurrentTheme.ButtonHoverText;

            return grid;
        }

        /// <summary>
        /// Ensures proper control sizing to prevent truncation in a TableLayoutPanel
        /// </summary>
        protected static void OptimizeTableLayoutForText(TableLayoutPanel tableLayout)
#pragma warning disable CA1848
#pragma warning disable CA1848
#pragma warning disable CA1848
        {
            if (tableLayout == null) return;

            // Ensure at least one row is AutoSize
            bool hasAutoSizeRow = false;
            for (int i = 0; i < tableLayout.RowStyles.Count; i++)
            {
                if (tableLayout.RowStyles[i].SizeType == SizeType.AutoSize)
                {
                    hasAutoSizeRow = true;
                    break;
                }
            }

            if (!hasAutoSizeRow && tableLayout.RowStyles.Count > 0)
            {
                tableLayout.RowStyles[tableLayout.RowStyles.Count - 1] = new RowStyle(SizeType.AutoSize);
            }

            // Check all child controls for potential truncation
            foreach (Control control in tableLayout.Controls)
            {
                if (control is Label label && !label.AutoSize && !label.AutoEllipsis)
                {
                    label.AutoEllipsis = true;
                }
                else if (control is Button button && !button.AutoEllipsis)
                {
                    button.AutoEllipsis = true;
                }
            }
        }
    }
}
