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
    public class DriverListView : IStatefulView // IStatefulView now inherits IView
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

        public async Task ActivateAsync(CancellationToken cancellationToken) // Added CancellationToken
        {
            if (_panel == null)
            {
                var driverService = _serviceProvider.GetRequiredService<IDriverService>();
                _panel = new DriverListPanel(driverService);

                _panel.StatusUpdated += (sender, e) => StatusUpdated?.Invoke(this, e);
                _panel.DriverEditRequested += (sender, e) =>
                {
                    NavigationRequested?.Invoke(this, new NavigationEventArgs("driver-edit", e.Entity));
                };
            }

            await _panel.LoadDriversAsync(cancellationToken); // Pass CancellationToken
        }

        public Task DeactivateAsync() // Matches IView
        {
            return Task.CompletedTask;
        }

        // IStatefulView specific members (already implemented)
        public void SaveState(object state) { /* ... */ }
        public void RestoreState(object state) { /* ... */ }
        public object? GetState() { return null; /* ... */ }

        // Explicit IView ActivateAsync to satisfy IStatefulView's base if there's ambiguity, though not strictly needed now
        async Task IView.ActivateAsync(CancellationToken cancellationToken)
        {
            await ActivateAsync(cancellationToken);
        }

        public void Dispose() { /* ... */ }
    }
}
