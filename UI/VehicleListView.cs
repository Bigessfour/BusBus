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
    public class VehicleListView : IView
    {
        private readonly IServiceProvider _serviceProvider;
        private VehicleListPanel? _panel;

        public string ViewName => "vehicles";
        public string Title => "Vehicle Management";
        public Control? Control => _panel;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        
#pragma warning disable CS0067 // The event is never used - required by IView interface
        public event EventHandler<StatusEventArgs>? StatusUpdated;
#pragma warning restore CS0067

        public VehicleListView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Debug logging for constructor
            var logger = serviceProvider.GetService<ILogger<VehicleListView>>();
#pragma warning disable CA1848 // Use LoggerMessage delegates for logging performance
            logger?.LogDebug("VehicleListView constructor called");
#pragma warning restore CA1848
        }

        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            if (_panel == null)
            {
                var vehicleService = _serviceProvider.GetRequiredService<IVehicleService>();
                _panel = new VehicleListPanel(vehicleService);

                // Forward events from panel to dashboard
                _panel.VehicleEditRequested += (sender, e) =>
                {
                    // Could navigate to vehicle edit view if implemented
                    NavigationRequested?.Invoke(this, new NavigationEventArgs("vehicle-edit", e.Entity));
                };
            }

            await _panel.LoadVehiclesAsync();
        }

        public Task DeactivateAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _panel?.Dispose();
        }
    }
}
