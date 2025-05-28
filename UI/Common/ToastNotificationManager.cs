#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Manages non-blocking toast notifications
    /// </summary>
    public class ToastNotificationManager
    {
        private readonly Form _parentForm;
        private readonly Queue<ToastNotification> _notificationQueue = new();
        private readonly List<ToastNotification> _activeNotifications = new();
        private const int MAX_VISIBLE_TOASTS = 3;
        private const int TOAST_MARGIN = 10;

        public ToastNotificationManager(Form parentForm)
        {
            _parentForm = parentForm;
        }

        public void ShowToast(string message, NotificationType type)
        {
            var toast = new ToastNotification(message, type);
            toast.ToastClosed += Toast_Closed;

            if (_activeNotifications.Count < MAX_VISIBLE_TOASTS)
            {
                ShowToastInternal(toast);
            }
            else
            {
                _notificationQueue.Enqueue(toast);
            }
        }
        private void ShowToastInternal(ToastNotification toast)
        {
            _activeNotifications.Add(toast);

            // Calculate position with dynamic spacing based on actual toast height
            var x = _parentForm.Right - toast.Width - TOAST_MARGIN;
            var y = _parentForm.Top + 120; // Start position to avoid content overlap

            // Calculate Y position based on previous toasts
            for (int i = 0; i < _activeNotifications.Count - 1; i++)
            {
                y += _activeNotifications[i].Height + TOAST_MARGIN;
            }

            toast.Location = new Point(x, y);
            toast.Show(_parentForm);
        }
        private void Toast_Closed(object? sender, EventArgs e)
        {
            if (sender is ToastNotification toast)
            {
                _activeNotifications.Remove(toast);

                // Reposition remaining toasts with dynamic spacing
                var currentY = _parentForm.Top + 120; // Start position
                for (int i = 0; i < _activeNotifications.Count; i++)
                {
                    var activeToast = _activeNotifications[i];
                    var x = _parentForm.Right - activeToast.Width - TOAST_MARGIN;
                    activeToast.Location = new Point(x, currentY);
                    currentY += activeToast.Height + TOAST_MARGIN; // Add actual height for next position
                }

                if (_notificationQueue.Count > 0 && _activeNotifications.Count < MAX_VISIBLE_TOASTS)
                {
                    ShowToastInternal(_notificationQueue.Dequeue());
                }
            }
        }
    }

    public class ToastNotification : Form
    {
        private readonly System.Windows.Forms.Timer _autoCloseTimer;
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private double _opacity = 0.9;        // Renamed to avoid conflict with Form.Closed
        public event EventHandler? ToastClosed;

        public ToastNotification(string message, NotificationType type)
        {
            InitializeComponent(message, type);

            _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _autoCloseTimer.Tick += (s, e) => FadeOut();
            _autoCloseTimer.Start();

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _fadeTimer.Tick += FadeTimer_Tick;
        }
        private void InitializeComponent(string message, NotificationType type)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Size = new Size(350, 90); // Increased from 300x80 to accommodate longer messages
            this.StartPosition = FormStartPosition.Manual;
            this.Opacity = _opacity;
            this.MinimumSize = new Size(300, 80); // Set minimum size to prevent excessive shrinking

            this.BackColor = type switch
            {
                NotificationType.Success => ThemeManager.CurrentTheme.ButtonBackground,
                NotificationType.Warning => ThemeManager.CurrentTheme.ButtonHoverBackground,
                NotificationType.Error => ThemeManager.CurrentTheme.ButtonPressedBackground,
                _ => ThemeManager.CurrentTheme.CardBackground
            };

            // Enhanced message label with better text handling
            var messageLabel = new Label
            {
                Text = message,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = new Font("Segoe UI", 10F),
                Location = new Point(15, 20), // Adjusted Y position for better centering
                MaximumSize = new Size(300, 60), // Maximum size to constrain text area
                AutoSize = true, // Enable auto-sizing to prevent text truncation
                AutoEllipsis = true, // Enable ellipsis for very long messages
                UseMnemonic = false // Prevent & characters from being interpreted as mnemonics
            };

            // Adjust toast size based on message label size if needed
            if (messageLabel.PreferredSize.Width > 300)
            {
                this.Width = Math.Min(messageLabel.PreferredSize.Width + 60, 450); // Cap maximum width
            }
            if (messageLabel.PreferredSize.Height > 40)
            {
                this.Height = Math.Min(messageLabel.PreferredSize.Height + 50, 120); // Cap maximum height
            }

            var closeButton = new Button
            {
                Text = "✕",
                ForeColor = ThemeManager.CurrentTheme.ButtonText,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(25, 25),
                Location = new Point(this.Width - 35, 5), // Position relative to actual width
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TabStop = false // Prevent tab navigation to close button
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => FadeOut();

            this.Controls.AddRange(new Control[] { messageLabel, closeButton });

#if DEBUG
            // Add debug truncation detection in development builds
            this.Load += (s, e) =>
            {
                var truncatedControls = Utils.LayoutDebugger.DetectTextTruncation(this);
                if (truncatedControls.Count > 0)
                {
                    Console.WriteLine($"[ToastNotification] DEBUG: Found {truncatedControls.Count} potentially truncated controls");
                    foreach (var controlInfo in truncatedControls)
                    {
                        Console.WriteLine($"  - {controlInfo}");
                    }
                }
            };
#endif
        }

        private void FadeOut()
        {
            _autoCloseTimer.Stop();
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            _opacity -= 0.05;
            if (_opacity <= 0)
            {
                _fadeTimer.Stop();
                ToastClosed?.Invoke(this, EventArgs.Empty);
                this.Close();
            }
            else
            {
                this.Opacity = _opacity;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoCloseTimer?.Dispose();
                _fadeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
