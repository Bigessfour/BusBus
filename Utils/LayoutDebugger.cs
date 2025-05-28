#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Layout debugging utility for visualizing control layouts and positioning
    /// </summary>
    public static class LayoutDebugger
    {
        private static readonly Dictionary<Control, DebugOverlay> _activeOverlays = new();
        private static bool _debugEnabled = false;
        private static ILogger? _logger;
        private static readonly Random _random = new();        // LoggerMessage delegates for performance
        private static readonly Action<ILogger, Exception?> _debugModeEnabled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(1), "Layout debug mode enabled");
        private static readonly Action<ILogger, Exception?> _debugModeDisabled =
            LoggerMessage.Define(LogLevel.Debug, new EventId(2), "Layout debug mode disabled"); private static readonly Action<ILogger, string, string, string, Rectangle, bool, Exception?> _logControlHierarchy =
            LoggerMessage.Define<string, string, string, Rectangle, bool>(LogLevel.Debug, new EventId(3),
                "{Indent}Control: {ControlType} '{ControlName}' Bounds: {Bounds} Visible: {Visible}");
        private static readonly Action<ILogger, string, string, Exception?> _truncatedTextDetected =
            LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(4),
                "Truncated text detected in {ControlName}: '{TruncatedText}...'");
        private static readonly Action<ILogger, string, Exception?> _fixingTruncatedLabel =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(5),
                "Fixing truncated label: {LabelName}");
        private static readonly Action<ILogger, string, Exception?> _fixingTruncatedButton =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(6),
                "Fixing truncated button: {ButtonName}");
        private static readonly Action<ILogger, int, Exception?> _truncationIssuesRemain =
            LoggerMessage.Define<int>(LogLevel.Warning, new EventId(7),
                "{RemainingCount} truncation issues remain after fix attempt");
        private static readonly Action<ILogger, int, Exception?> _successfullyFixedIssues =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(8),
                "Successfully fixed {FixedCount} truncation issues");
        private static readonly Action<ILogger, int, Exception?> _detectedTruncationAfterResize =
            LoggerMessage.Define<int>(LogLevel.Warning, new EventId(9),
                "Detected {IssueCount} truncation issues after resize");

        public static bool IsDebugEnabled => _debugEnabled;
        public static bool DebugMode => _debugEnabled;

        public static void Initialize(ILogger? logger = null)
        {
            _logger = logger;
        }

        public static void EnableDebugMode()
        {
            _debugEnabled = true;
            if (_logger != null)
                _debugModeEnabled(_logger, null);
        }

        public static void DisableDebugMode()
        {
            _debugEnabled = false;
            ClearAllOverlays();
            if (_logger != null)
                _debugModeDisabled(_logger, null);
        }

        public static void ToggleDebugMode()
        {
            if (_debugEnabled)
                DisableDebugMode();
            else
                EnableDebugMode();
        }

        public static void ShowDebugOverlay(Control control, string? label = null)
        {
            if (!_debugEnabled || control == null) return;

            if (_activeOverlays.ContainsKey(control))
                RemoveDebugOverlay(control);

            var overlay = new DebugOverlay(control, label ?? control.Name);
            _activeOverlays[control] = overlay;
            overlay.Show();
        }

        public static void RemoveDebugOverlay(Control control)
        {
            if (_activeOverlays.TryGetValue(control, out var overlay))
            {
                overlay.Hide();
                overlay.Dispose();
                _activeOverlays.Remove(control);
            }
        }

        public static void ClearAllOverlays()
        {
            foreach (var overlay in _activeOverlays.Values)
            {
                overlay.Hide();
                overlay.Dispose();
            }
            _activeOverlays.Clear();
        }

        public static void LogControlHierarchy(Control control, int depth = 0)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control)); if (_logger == null) return;

            var indent = new string(' ', depth * 2);
            _logControlHierarchy(_logger, indent, control.GetType().Name, control.Name, control.Bounds, control.Visible, null);

            foreach (Control child in control.Controls)
            {
                LogControlHierarchy(child, depth + 1);
            }
        }

        public static List<Control> DetectTextTruncation(Control control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var result = new List<Control>();
            DetectTextTruncationRecursive(control, result);
            return result;
        }

        private static void DetectTextTruncationRecursive(Control control, List<Control> result)
        {
            if (control is Button button && !string.IsNullOrEmpty(button.Text))
            {
                using (var g = button.CreateGraphics())
                {
                    SizeF textSize = g.MeasureString(button.Text, button.Font);
                    if (textSize.Width > button.Width - 10)
                    {
                        result.Add(button); if (DebugMode)
                        {
                            button.BackColor = Color.FromArgb(255, 200, 200);
                            if (_logger != null)
                                _truncatedTextDetected(_logger, button.Name, TruncateText(button.Text, 30), null);
                        }
                    }
                }
            }

            foreach (Control child in control.Controls)
            {
                DetectTextTruncationRecursive(child, result);
            }
        }

        public static void FixTextTruncation(Control rootControl)
        {
            var truncatedControls = DetectTextTruncation(rootControl);

            foreach (var control in truncatedControls)
            {
                if (control is Label label)
                {
                    if (_logger != null)
                        _fixingTruncatedLabel(_logger, label.Name, null);

                    if (label.Parent is TableLayoutPanel || label.Parent is FlowLayoutPanel)
                    {
                        if (label.Parent != null)
                        {
                            label.AutoSize = true;
                            label.MaximumSize = new Size(label.Parent.Width - 10, 0);
                        }
                    }
                    else
                    {
                        label.AutoEllipsis = true;

                        if (label.Font.Size > 11 || label.Font.Bold)
                        {
                            if (label.Parent != null)
                            {
                                var originalSize = label.Size;
                                label.AutoSize = true;
                                label.MaximumSize = new Size(Math.Max(label.Parent.Width, 300), 0);

                                if (label.Width > label.Parent.Width)
                                {
                                    label.AutoSize = false;
                                    label.Size = originalSize;
                                }
                            }
                        }
                    }
                }
                else if (control is Button button)
                {
                    if (_logger != null)
                        _fixingTruncatedButton(_logger, button.Name, null);

                    button.AutoEllipsis = true;

                    if (button.Parent != null && button.Width < 150 && button.Parent.Width > button.Width * 1.5)
                    {
                        button.Width = Math.Min(button.Width * 3 / 2, button.Parent.Width - 20);
                    }
                }
            }
            var remainingIssues = DetectTextTruncation(rootControl);
            if (remainingIssues.Count > 0)
            {
                if (_logger != null)
                    _truncationIssuesRemain(_logger, remainingIssues.Count, null);
            }
            else if (truncatedControls.Count > 0)
            {
                if (_logger != null)
                    _successfullyFixedIssues(_logger, truncatedControls.Count, null);
            }
        }

        public static void MonitorForTruncationOnResize(Form form)
        {
            if (form == null) return;

            form.Resize += (sender, e) =>
            {
                if (!DebugMode) return; var issues = DetectTextTruncation(form);
                if (issues.Count > 0)
                {
                    if (_logger != null)
                        _detectedTruncationAfterResize(_logger, issues.Count, null);
                }
            };
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength);
        }

        private static void BuildControlTree(Control control, StringBuilder sb, int indent)
        {
            foreach (Control child in control.Controls)
            {
                var indentStr = new string(' ', indent * 2);
                sb.AppendLine($"{indentStr}- {child.GetType().Name} ({child.Name})");
                sb.AppendLine($"{indentStr}  Size: {child.Width} x {child.Height}");
                sb.AppendLine($"{indentStr}  Location: {child.Location}");
                sb.AppendLine($"{indentStr}  Dock: {child.Dock}");
                sb.AppendLine($"{indentStr}  Anchor: {child.Anchor}");
                sb.AppendLine($"{indentStr}  Visible: {child.Visible}");

                BuildControlTree(child, sb, indent + 1);
            }
        }

        private static Color GenerateRandomColor()
        {
            return Color.FromArgb(
                _random.Next(100, 240),
                _random.Next(100, 240),
                _random.Next(100, 240)
            );
        }

        /// <summary>
        /// Generates a comprehensive layout report for debugging
        /// </summary>
        public static string GenerateLayoutReport(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            var report = new StringBuilder();
            report.AppendLine("=== BUSBUS LAYOUT DEBUG REPORT ===");
            report.AppendLine($"Generated: {DateTime.Now}");
            report.AppendLine($"Root Control: {control.GetType().Name} ({control.Name})");
            report.AppendLine();

            // Truncation issues
            var truncationIssues = DetectTextTruncation(control);
            report.AppendLine($"TEXT TRUNCATION ISSUES FOUND: {truncationIssues.Count}");
            foreach (var issue in truncationIssues)
            {
                report.AppendLine($"  ⚠️ {issue}");
            }
            report.AppendLine();

            // Control hierarchy
            report.AppendLine("CONTROL HIERARCHY:");
            BuildControlTree(control, report, 0);

            return report.ToString();
        }

        /// <summary>
        /// Exports layout debug information to a file
        /// </summary>
        public static void ExportLayoutReport(Control control, string? filePath = null)
        {
            var report = GenerateLayoutReport(control);
            var fileName = filePath ?? $"BusBus_Layout_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            try
            {
                System.IO.File.WriteAllText(fileName, report);
                MessageBox.Show($"Layout debug report exported to:\n{fileName}", "Debug Report Exported",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export layout report:\n{ex.Message}", "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class DebugOverlay : IDisposable
        {
            private readonly Control _targetControl;
            private readonly string _label;
            private readonly System.Windows.Forms.Timer _updateTimer;
            private Form? _overlayForm;

            public DebugOverlay(Control targetControl, string label)
            {
                _targetControl = targetControl; _label = label;
                _updateTimer = new System.Windows.Forms.Timer { Interval = 100 };
                _updateTimer.Tick += UpdateOverlay;
            }

            public void Show()
            {
                CreateOverlayForm();
                _updateTimer.Start();
            }

            public void Hide()
            {
                _updateTimer.Stop();
                _overlayForm?.Hide();
            }

            private void CreateOverlayForm()
            {
                _overlayForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.Red,
                    Opacity = 0.3,
                    TopMost = true,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual
                };

                var label = new Label
                {
                    Text = _label,
                    ForeColor = Color.White,
                    BackColor = Color.Black,
                    AutoSize = true,
                    Location = new Point(2, 2)
                };
                _overlayForm.Controls.Add(label);

                UpdateOverlayPosition();
                _overlayForm.Show();
            }

            private void UpdateOverlay(object? sender, EventArgs e)
            {
                UpdateOverlayPosition();
            }

            private void UpdateOverlayPosition()
            {
                if (_overlayForm == null || _targetControl == null) return;

                var screenBounds = _targetControl.RectangleToScreen(_targetControl.ClientRectangle);
                _overlayForm.Bounds = screenBounds;
            }
            public void Dispose()
            {
                _updateTimer?.Dispose();
                _overlayForm?.Dispose();
            }
        }
    }
}

