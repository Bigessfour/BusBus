#pragma warning disable CS8612 // Nullability of reference types in type of event doesn't match implicitly implemented member
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Services;

namespace BusBus.UI
{
    public class RouteListView : IView
    {
        private readonly IRouteService _routeService;
        private Panel _panel;

        public string ViewName => "routes";
        public string Title => "Route Management"; public Control Control => _panel;

#pragma warning disable CS0414, CS0067 // Events are assigned but never used, required by interface
        public event EventHandler<NavigationEventArgs> NavigationRequested = null!;
        public event EventHandler<StatusEventArgs> StatusUpdated = null!;
#pragma warning restore CS0414, CS0067

        public RouteListView(IRouteService routeService)
        {
            _routeService = routeService;
            _panel = new Panel();
            var label = new Label
            {
                Text = "Routes View - Coming Soon",
                Font = new Font("Segoe UI", 12F),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            _panel.Controls.Add(label);
        }

        public Task ActivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeactivateAsync() => Task.CompletedTask;
        public void Dispose() => _panel?.Dispose();
    }
}
