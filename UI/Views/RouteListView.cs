#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using BusBus.Services;

namespace BusBus.UI
{
    public class RouteListView : BaseView
    {
        private readonly IRouteService _routeService;
        private RouteListPanel _routeListPanel = null!;

        public override string ViewName => "routes";
        public override string Title => "Route Management";

        public RouteListView(IRouteService routeService)
        {
            if (routeService == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] routeService is null in RouteListView constructor!");
                System.Diagnostics.Debugger.Break();
                throw new ArgumentNullException(nameof(routeService));
            }
            _routeService = routeService;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] RouteListView constructor: routeService = {_routeService}");
            InitializeView(); // Call after field is set
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            System.Diagnostics.Debug.WriteLine($"[DEBUG] RouteListView.InitializeView: _routeService = {_routeService}");
            if (_routeService == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] _routeService is null in RouteListView.InitializeView!");
                System.Diagnostics.Debugger.Break();
                throw new ArgumentNullException(nameof(_routeService));
            }
            _routeListPanel = new RouteListPanel(_routeService)
            {
                Dock = System.Windows.Forms.DockStyle.Fill
            };

            this.Controls.Add(_routeListPanel);
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Loading routes...");
            await _routeListPanel.LoadRoutesAsync(1, 10, cancellationToken);
            UpdateStatus("Routes loaded", StatusType.Success);
        }

        protected override Task OnDeactivateAsync()
        {
            return Task.CompletedTask;
        }
    }
}
