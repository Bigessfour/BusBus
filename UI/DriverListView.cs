#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated; public DriverListView(IServiceProvider serviceProvider)
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
                _panel.StatusUpdated += (sender, e) => StatusUpdated?.Invoke(this, e);
                _panel.DriverEditRequested += (sender, e) =>
                {
                    // Could navigate to driver edit view if implemented
                    NavigationRequested?.Invoke(this, new NavigationEventArgs("driver-edit", e.Entity));
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

        public void RestoreState(object state)
        {
            _panel?.RestoreState(state);
        }

        public void SaveState(object state)
        {
            _panel?.SaveState(state);
        }

        public void Dispose()
        {
            _panel?.Dispose();
        }
    }
}
