#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Windows.Forms;
using BusBus.UI.Common;

namespace BusBus
{
    public partial class RouteForm : BaseForm
    {
        private DatabaseManager dbManager;
        private int? routeId;
        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }

        private TextBox txtRouteName;
        private TextBox txtStartLocation;
        private TextBox txtEndLocation;

        public RouteForm() : this(null) { }

        public RouteForm(int? id)
        {
            // InitializeComponent(); // Comment out if not using designer
            InitializeForm(); // Use custom initialization
            LoadRoutes();

            dbManager = new DatabaseManager();
            routeId = id;

            if (routeId.HasValue)
            {
                LoadRouteData();
            }
        }

        private void InitializeForm()
        {
            // Initialize form properties
            this.Text = "Route Management";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeControls();
        }

        private void InitializeControls()
        {
            this.txtRouteName = new TextBox();
            this.txtStartLocation = new TextBox();
            this.txtEndLocation = new TextBox();

            // Configure txtRouteName
            this.txtRouteName.Location = new System.Drawing.Point(120, 20);
            this.txtRouteName.Name = "txtRouteName";
            this.txtRouteName.Size = new System.Drawing.Size(200, 23);

            // Configure txtStartLocation
            this.txtStartLocation.Location = new System.Drawing.Point(120, 50);
            this.txtStartLocation.Name = "txtStartLocation";
            this.txtStartLocation.Size = new System.Drawing.Size(200, 23);

            // Configure txtEndLocation
            this.txtEndLocation.Location = new System.Drawing.Point(120, 80);
            this.txtEndLocation.Name = "txtEndLocation";
            this.txtEndLocation.Size = new System.Drawing.Size(200, 23);

            // Add controls to the form
            this.Controls.Add(this.txtRouteName);
            this.Controls.Add(this.txtStartLocation);
            this.Controls.Add(this.txtEndLocation);

            // Add labels
            var lblRouteName = new Label { Text = "Route Name:", Location = new System.Drawing.Point(20, 23) };
            var lblStartLocation = new Label { Text = "Start:", Location = new System.Drawing.Point(20, 53) };
            var lblEndLocation = new Label { Text = "End:", Location = new System.Drawing.Point(20, 83) };

            this.Controls.Add(lblRouteName);
            this.Controls.Add(lblStartLocation);
            this.Controls.Add(lblEndLocation);

            // Add buttons
            var btnSave = new Button { Text = "Save", Location = new System.Drawing.Point(120, 120) };
            var btnCancel = new Button { Text = "Cancel", Location = new System.Drawing.Point(220, 120) };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            // Configure form
            this.Text = "Route Form";
            this.Size = new System.Drawing.Size(400, 200);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (routeId.HasValue)
                {
                    DatabaseManager.UpdateRoute(routeId.Value, txtRouteName.Text, txtStartLocation.Text, txtEndLocation.Text);
                }
                else
                {
                    DatabaseManager.AddRoute(txtRouteName.Text, txtStartLocation.Text, txtEndLocation.Text);
                }

                DataSaved?.Invoke(this, EventArgs.Empty);

                if (IsEmbedded)
                {
                    ClearForm();
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving route: {ex.Message}", "Error");
            }
        }

        private void ClearForm()
        {
            txtRouteName.Text = "";
            txtStartLocation.Text = "";
            txtEndLocation.Text = "";
        }

        private static void LoadRoutes()
        {
            // Logic to refresh the list of routes
        }

        private static void LoadRouteData()
        {
            // Logic to load route data by routeId
        }
    }
}
