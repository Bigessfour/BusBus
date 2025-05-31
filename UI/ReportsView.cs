#pragma warning disable CS8612 // Nullability of reference types in type of event doesn't match implicitly implemented member
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Interfaces;
using BusBus.Services;

namespace BusBus.UI
{
    public class ReportsView : IView
    {
        private readonly IServiceProvider _serviceProvider;
        private Panel _panel; public string ViewName => "reports";
        public string Title => "Reports";
        public Control Control => _panel;

#pragma warning disable CS0414, CS0067 // Events are assigned but never used, required by interface
        // Required by IView
        public event EventHandler<NavigationEventArgs>? NavigationChanged;
        public event EventHandler<StatusEventArgs>? StatusChanged;

        // Existing events (possibly used internally)
        public event EventHandler<NavigationEventArgs> NavigationRequested = null!;
        public event EventHandler<StatusEventArgs> StatusUpdated = null!;
#pragma warning restore CS0414, CS0067

        public ReportsView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _panel = new Panel();
            var label = new Label
            {
                Text = "Reports View - Coming Soon",
                Font = new Font("Segoe UI", 12F),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _panel.Controls.Add(label);
        }

        public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeactivateAsync() => Task.CompletedTask;
        public void Dispose() => _panel?.Dispose();

        // Required by IView
        public void Show()
        {
            _panel?.Show();
        }

        public void Hide()
        {
            _panel?.Hide();
        }
    }
}
