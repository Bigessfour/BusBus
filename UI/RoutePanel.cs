using BusBus.UI.Common;
using BusBus.UI.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.Services;
using Microsoft.EntityFrameworkCore;

namespace BusBus.UI
{
    public partial class RoutePanel : ThemeableControl, IDisplayable
    {
        private readonly IRouteService _routeService;
        private Route? _currentRoute;
        public RoutePanel(IRouteService routeService)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            InitializeComponent();
        }

        private static void InitializeComponent()
        {
            // TODO: Add UI initialization logic here if needed
        }

        public override void RefreshTheme()
        {
            ApplyTheme();
        }

        protected override void ApplyTheme()
        {
            // Example: update colors, fonts, etc. according to the current theme
            BackColor = ThemeManager.CurrentTheme.CardBackground;
            // Add more theme application logic as needed
        }
        public void Render(Control container)
        {
            if (container == null) return;
            container.Controls.Clear();
            container.Controls.Add(this);
            Dock = DockStyle.Fill;
        }
        void IDisposable.Dispose()
        {
            Dispose();
        }
        public void LoadRoute(Route route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));

            _currentRoute = route;

            // TODO: In a real implementation, this would update UI controls
            // with the route data. For now, we'll just store the route.
        }
        // ... rest of the class ...
    }
}
