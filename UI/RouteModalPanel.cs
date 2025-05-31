// <auto-added>
#nullable enable
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Core;
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
            Text = _isNewRoute ? "Add New Route" : "Edit Route";
            Size = new Size(600, 500);
            MinimumSize = new Size(500, 400);
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground,
                ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText,
                Font = BusBus.UI.Core.ThemeManager.CurrentTheme.SmallButtonFont,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(5, 0, 0, 0)
            };
            cancelButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;
            cancelButton.Click += (s, e) => Close(); var saveButton = new Button
            {
                Text = "Save",
                Size = new Size(80, 30),
                BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground,
                ForeColor = Color.White,
                Font = BusBus.UI.Core.ThemeManager.CurrentTheme.SmallButtonFont,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            saveButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
            saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(saveButton);

            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }

        private void LoadRoutePanel()
        {
            try
            {
                _routePanel = new RoutePanel(_routeService);
                _routePanel.Dock = DockStyle.Fill;

                // Find the main panel and add the route panel
                foreach (Control control in Controls)
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
                        // ScheduledTime = _routeDto.ScheduledTime, // Schedule scrubbed
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
                DialogResult = DialogResult.OK;
                Close();
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

        protected override void ApplyTheme()
        {
            BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;
            ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText;

            // Apply theme to controls
            foreach (Control control in Controls)
            {
                if (control is Panel panel)
                {
                    panel.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;

                    foreach (Control panelControl in panel.Controls)
                    {
                        if (panelControl is Button button)
                        {
                            // Default button styling
                            button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                            button.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText;
                            button.FlatStyle = FlatStyle.Flat;
                            button.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;

                            // Special styling for save button
                            if (button.Text == "Save")
                            {
                                button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
                                button.ForeColor = Color.White;
                                button.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
                            }
                        }
                        else if (panelControl is FlowLayoutPanel flowPanel)
                        {
                            flowPanel.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.MainBackground;

                            foreach (Control flowControl in flowPanel.Controls)
                            {
                                if (flowControl is Button nestedButton)
                                {
                                    // Default button styling
                                    nestedButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonBackground;
                                    nestedButton.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText;
                                    nestedButton.FlatStyle = FlatStyle.Flat;
                                    nestedButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.BorderColor;

                                    // Special styling for save button
                                    if (nestedButton.Text == "Save")
                                    {
                                        nestedButton.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
                                        nestedButton.ForeColor = Color.White;
                                        nestedButton.FlatAppearance.BorderColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
                                    }
                                }
                            }
                        }
                    }
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
