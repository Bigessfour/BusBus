#pragma warning disable CS8612 // Nullability of reference types in type of event doesn't match implicitly implemented member
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;

namespace BusBus.UI
{
    public class SettingsView : IView
    {
        private readonly IServiceProvider _serviceProvider;
        private Panel _panel; public string ViewName => "settings";
        public string Title => "Settings";
        public Control Control => _panel;

#pragma warning disable CS0414, CS0067 // Events are assigned but never used, required by interface
        public event EventHandler<NavigationEventArgs> NavigationRequested = null!;
        public event EventHandler<StatusEventArgs> StatusUpdated = null!;
#pragma warning restore CS0414, CS0067

        public SettingsView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _panel = new Panel
            {
                BackColor = ThemeManager.CurrentTheme.MainBackground,
                Dock = DockStyle.Fill
            };

            var label = new Label
            {
                Text = "Settings View - Coming Soon",
                Font = ThemeManager.CurrentTheme.HeadlineFont,
                ForeColor = ThemeManager.CurrentTheme.HeadlineText,
                AutoSize = true,
                Location = new Point(20, 20),
                BackColor = Color.Transparent
            };

            _panel.Controls.Add(label);

            // Apply theme when theme changes
            ThemeManager.ThemeChanged += (s, e) => ApplyTheme();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            _panel.BackColor = ThemeManager.CurrentTheme.MainBackground;

            foreach (Control control in _panel.Controls)
            {
                if (control is Label label)
                {
                    label.ForeColor = ThemeManager.CurrentTheme.HeadlineText;
                    label.Font = ThemeManager.CurrentTheme.HeadlineFont;
                }
            }
        }

        public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeactivateAsync() => Task.CompletedTask;
        public void Dispose() => _panel?.Dispose();
    }
}
