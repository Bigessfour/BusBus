// <auto-added>
#nullable enable
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public partial class RouteModalPanel : BaseForm
    {
        private readonly IRouteService _routeService;
        private readonly RouteDisplayDTO? _routeDto;
        private bool _isNewRoute;
        private RoutePanel? _routePanel;

        public RouteDisplayDTO? RouteDto => _routeDto;
        public bool IsSaved { get; private set; }

        public RouteModalPanel(IRouteService routeService, RouteDisplayDTO? routeDto = null)
        {
            _routeService = routeService ?? throw new ArgumentNullException(nameof(routeService));
            _routeDto = routeDto;
            _isNewRoute = routeDto == null || routeDto.Id == Guid.Empty;

            InitializeComponent();
            LoadRoutePanel();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewRoute ? "Add New Route" : "Edit Route";
            this.Size = new Size(600, 500);
            this.MinimumSize = new Size(500, 400);
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = ThemeManager.CurrentTheme.MainBackground;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                BackColor = ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(5, 0, 0, 0)
            };
            cancelButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            cancelButton.Click += (s, e) => this.Close(); var saveButton = new Button
            {
                Text = "Save",
                Size = new Size(80, 30),
                BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground,
                ForeColor = Color.White,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            saveButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
            saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(saveButton);

            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        private void LoadRoutePanel()
        {
            try
            {
                _routePanel = new RoutePanel(_routeService);
                _routePanel.Dock = DockStyle.Fill;

                // Find the main panel and add the route panel
                foreach (Control control in this.Controls)
                {
                    if (control is Panel mainPanel && mainPanel.Dock == DockStyle.Fill)
                    {
                        mainPanel.Controls.Add(_routePanel);
                        break;
                    }
                }

                // If we have a route to edit, load it
                if (_routeDto != null && !_isNewRoute)
                {
                    // Convert RouteDisplayDTO to Route for the panel
                    var route = new Route
                    {
                        Id = _routeDto.Id,
                        Name = _routeDto.Name,
                        StartLocation = _routeDto.StartLocation,
                        EndLocation = _routeDto.EndLocation,
                        ScheduledTime = _routeDto.ScheduledTime,
                        RouteDate = _routeDto.TripDate,
                        AMStartingMileage = _routeDto.AMStartingMileage,
                        AMEndingMileage = _routeDto.AMEndingMileage,
                        AMRiders = _routeDto.AMRiders,
                        PMStartMileage = _routeDto.PMStartMileage,
                        PMEndingMileage = _routeDto.PMEndingMileage,
                        PMRiders = _routeDto.PMRiders,
                        DriverId = _routeDto.DriverId,
                        VehicleId = _routeDto.VehicleId
                    };

                    _routePanel.LoadRoute(route);
                }

                if (_routePanel.Parent != null)
                {
                    _routePanel.Render(_routePanel.Parent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing route panel: {ex.Message}", "Initialization Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_routePanel == null)
                {
                    MessageBox.Show("Route panel not initialized.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var saveButton = sender as Button;
                if (saveButton != null)
                {
                    saveButton.Enabled = false;
                    saveButton.Text = "Saving...";
                }

                // The RoutePanel should handle the saving internally
                // For now, we'll simulate a successful save
                await Task.Delay(500); // Simulate save operation

                IsSaved = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving route: {ex.Message}", "Save Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                var saveButton = sender as Button;
                if (saveButton != null)
                {
                    saveButton.Enabled = true;
                    saveButton.Text = "Save";
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _routePanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
