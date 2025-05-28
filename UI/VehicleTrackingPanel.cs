#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace BusBus.UI
{
    public class VehicleTrackingPanel : UserControl
    {
        private DatabaseManager dbManager;
        private System.Windows.Forms.Timer locationUpdateTimer;

        public VehicleTrackingPanel()
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            SetupLocationTracking();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 300);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            var titleLabel = new Label
            {
                Text = "Vehicle GPS Tracking",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            var mapPanel = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(380, 200),
                BorderStyle = BorderStyle.Fixed3D,
                BackColor = Color.LightBlue
            };

            var statusLabel = new Label
            {
                Name = "statusLabel",
                Text = "Initializing GPS tracking...",
                Location = new Point(10, 250),
                Size = new Size(380, 20)
            };

            this.Controls.AddRange(new Control[] { titleLabel, mapPanel, statusLabel });
        }
        private void SetupLocationTracking()
        {
            locationUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000 // 30 seconds
            };
            locationUpdateTimer.Tick += async (s, e) => await UpdateVehicleLocationsAsync();
            locationUpdateTimer.Start();
        }

        private async Task UpdateVehicleLocationsAsync()
        {
            try
            {
                // Simulate GPS updates for demo
                var nearbyVehicles = DatabaseManager.GetVehiclesNearLocation(40.7128, -74.0060, 10); // NYC area

                var statusLabel = this.Controls["statusLabel"] as Label;
                if (statusLabel != null)
                {
                    statusLabel.Text = $"Tracking {nearbyVehicles.Count} vehicles";
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                var statusLabel = this.Controls["statusLabel"] as Label;
                if (statusLabel != null)
                {
                    statusLabel.Text = $"GPS Error: {ex.Message}";
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Stop and dispose the timer to prevent background updates after disposal
                if (locationUpdateTimer != null)
                {
                    locationUpdateTimer.Stop();
                    locationUpdateTimer.Dispose();
                    locationUpdateTimer = null!; // Using null-forgiving operator since we're cleaning up
                }

                // Dispose other resources
                if (dbManager is IDisposable disposableDb)
                {
                    disposableDb.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
