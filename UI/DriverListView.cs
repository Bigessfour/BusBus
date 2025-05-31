// Suppress possible null reference warning for state argument
#pragma warning disable CS8604 // Possible null reference argument
// Suppress unused event warnings for this view
#pragma warning disable CS0067 // Event is never used
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.UI.Interfaces;
using BusBus.Services;
using BusBus.UI.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BusBus.UI
{
    public class DriverListView : IView, IStatefulView
    {
        private readonly IServiceProvider _serviceProvider;
        private DriverListPanel? _panel;

        public string ViewName => "drivers";
        public string Title => "Driver Management";
        public Control? Control => _panel;

        // Required by IView
        public event EventHandler<NavigationEventArgs>? NavigationChanged;
        public event EventHandler<StatusEventArgs>? StatusChanged;

        // Existing events (possibly used internally)
        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        public DriverListView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Debug logging for constructor
            var logger = serviceProvider.GetService<ILogger<DriverListView>>();
#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
            logger?.LogDebug("DriverListView constructor called");
#pragma warning restore CA1848
        }

        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            if (_panel == null)
            {
                var driverService = _serviceProvider.GetRequiredService<IDriverService>();
                _panel = new DriverListPanel(driverService);

                // Forward status events from panel to dashboard
                _panel.StatusUpdated += (sender, e) =>
                {
                    StatusUpdated?.Invoke(this, e);
                    StatusChanged?.Invoke(this, e);
                };
                _panel.DriverEditRequested += (sender, e) =>
                {
                    // Could navigate to driver edit view if implemented
                    NavigationRequested?.Invoke(this, new NavigationEventArgs("driver-edit", e.Entity));
                    NavigationChanged?.Invoke(this, new NavigationEventArgs("driver-edit", e.Entity));
                };
            }

            await _panel.LoadDriversAsync();
        }

        public Task DeactivateAsync()
        {
            return Task.CompletedTask;
        }

        public object? GetState()
        {
            return _panel?.GetState();
        }


        // Required by IStatefulView
        public void SetState(object? state)
        {
            _panel?.RestoreState(state);
        }

        // Optional: keep existing methods for compatibility

        public void RestoreState(object? state)
        {
            if (state != null && _panel != null)
            {
                _panel.RestoreState(state);
            }
        }

        public static void SaveState(object? state)
        {
            if (state != null)
            {
                DriverListPanel.SaveState(state);
            }
        }

        // Required by IView
        public void Show()
        {
            _panel?.Show();
        }

        public void Hide()
        {
            _panel?.Hide();
        }

        public void Dispose()
        {
            _panel?.Dispose();
        }
    }
}
