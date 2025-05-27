#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace BusBus.UI
{
    public class DriverListView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;

        public override string ViewName => "drivers";
        public override string Title => "Driver Management";

        public DriverListView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            var label = new Label
            {
                Text = "Driver Management - Coming Soon",
                Font = new Font("Segoe UI", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(label);
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Driver view loaded", StatusType.Info);
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync() => Task.CompletedTask;
    }

    public class VehicleListView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;

        public override string ViewName => "vehicles";
        public override string Title => "Vehicle Management";

        public VehicleListView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            var label = new Label
            {
                Text = "Vehicle Management - Coming Soon",
                Font = new Font("Segoe UI", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(label);
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Vehicle view loaded", StatusType.Info);
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync() => Task.CompletedTask;
    }

    public class ReportsView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;

        public override string ViewName => "reports";
        public override string Title => "Reports & Analytics";

        public ReportsView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            var label = new Label
            {
                Text = "Reports & Analytics - Coming Soon",
                Font = new Font("Segoe UI", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(label);
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Reports view loaded", StatusType.Info);
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync() => Task.CompletedTask;
    }

    public class SettingsView : BaseView
    {
        private readonly IServiceProvider _serviceProvider;

        public override string ViewName => "settings";
        public override string Title => "Settings";

        public SettingsView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void InitializeView()
        {
            base.InitializeView();

            var label = new Label
            {
                Text = "Settings - Coming Soon",
                Font = new Font("Segoe UI", 24F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(label);
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            UpdateStatus("Settings view loaded", StatusType.Info);
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync() => Task.CompletedTask;
    }
}
