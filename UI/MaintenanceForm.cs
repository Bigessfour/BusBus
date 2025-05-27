#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Windows.Forms;

namespace BusBus
{
    public partial class MaintenanceForm : Form
    {
        private int? maintenanceId;
        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }
        private static readonly string[] items = new[] { "Routine Service", "Repair", "Inspection", "Emergency Repair", "Tire Service", "Oil Change" };

        public MaintenanceForm(int? id = null)
        {
            InitializeComponent();
            maintenanceId = id;

            LoadVehicles();

            if (maintenanceId.HasValue)
            {
                LoadMaintenanceData();
            }
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Maintenance Record";
            this.Size = new System.Drawing.Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            // Vehicle
            var lblVehicle = new Label { Text = "Vehicle:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(100, 25) };
            var cmbVehicle = new ComboBox
            {
                Name = "cmbVehicle",
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(290, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Maintenance Type
            var lblType = new Label { Text = "Type:", Location = new System.Drawing.Point(20, 60), Size = new System.Drawing.Size(100, 25) };
            var cmbType = new ComboBox
            {
                Name = "cmbType",
                Location = new System.Drawing.Point(130, 60),
                Size = new System.Drawing.Size(290, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(items);

            // Date
            var lblDate = new Label { Text = "Date:", Location = new System.Drawing.Point(20, 100), Size = new System.Drawing.Size(100, 25) };
            var dtpDate = new DateTimePicker
            {
                Name = "dtpDate",
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(290, 25)
            };

            // Cost
            var lblCost = new Label { Text = "Cost:", Location = new System.Drawing.Point(20, 140), Size = new System.Drawing.Size(100, 25) };
            var txtCost = new TextBox { Name = "txtCost", Location = new System.Drawing.Point(130, 140), Size = new System.Drawing.Size(290, 25) };

            // Description
            var lblDescription = new Label { Text = "Description:", Location = new System.Drawing.Point(20, 180), Size = new System.Drawing.Size(100, 25) };
            var txtDescription = new TextBox
            {
                Name = "txtDescription",
                Location = new System.Drawing.Point(130, 180),
                Size = new System.Drawing.Size(290, 100),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Buttons
            var btnSave = new Button
            {
                Name = "btnSave",
                Text = "Save",
                Location = new System.Drawing.Point(130, 300),
                Size = new System.Drawing.Size(100, 30)
            };
            btnSave.Click += btnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(240, 300),
                Size = new System.Drawing.Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            // Add controls
            this.Controls.AddRange(new Control[] {
                lblVehicle, cmbVehicle, lblType, cmbType,
                lblDate, dtpDate, lblCost, txtCost,
                lblDescription, txtDescription, btnSave, btnCancel
            });
        }

        private void LoadVehicles()
        {
            var vehicles = DatabaseManager.GetAllVehicles();
            if (Controls["cmbVehicle"] is ComboBox cmbVehicle)
            {
                cmbVehicle.DisplayMember = "Number";
                cmbVehicle.ValueMember = "VehicleId";
                cmbVehicle.DataSource = vehicles;
            }
            else
            {
                throw new InvalidOperationException("cmbVehicle control not found.");
            }
        }

        private void LoadMaintenanceData()
        {
            if (!maintenanceId.HasValue)
                throw new InvalidOperationException("maintenanceId must have a value to load maintenance data.");

            var maintenance = DatabaseManager.GetMaintenanceById(maintenanceId.Value);
            if (maintenance != null)
            {
                if (Controls["cmbVehicle"] is ComboBox cmbVehicle)
                    cmbVehicle.SelectedValue = maintenance.VehicleId;
                if (Controls["cmbType"] is ComboBox cmbType)
                    cmbType.SelectedItem = maintenance.MaintenanceType;
                if (Controls["dtpDate"] is DateTimePicker dtpDate)
                    dtpDate.Value = maintenance.MaintenanceDate;
                if (Controls["txtCost"] is TextBox txtCost)
                    txtCost.Text = maintenance.Cost.ToString("F2");
                if (Controls["txtDescription"] is TextBox txtDescription)
                    txtDescription.Text = maintenance.Description;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            int? vehicleId = null;
            string? type = null;
            DateTime maintenanceDate = DateTime.MinValue;
            string? costText = null;
            string? description = null;

            if (Controls["cmbVehicle"] is ComboBox cmbVehicle)
                vehicleId = cmbVehicle.SelectedValue as int?;
            if (Controls["cmbType"] is ComboBox cmbType)
                type = cmbType.SelectedItem?.ToString();
            if (Controls["dtpDate"] is DateTimePicker dtpDate)
                maintenanceDate = dtpDate.Value;
            if (Controls["txtCost"] is TextBox txtCost)
                costText = txtCost.Text;
            if (Controls["txtDescription"] is TextBox txtDescription)
                description = txtDescription.Text;

            // Validation
            if (!vehicleId.HasValue)
            {
                MessageBox.Show("Please select a vehicle.", "Validation Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Please select a maintenance type.", "Validation Error");
                return;
            }

            decimal cost = 0;
            if (!string.IsNullOrWhiteSpace(costText) && !decimal.TryParse(costText, out cost))
            {
                MessageBox.Show("Please enter a valid cost.", "Validation Error");
                return;
            }

            // Save
            if (maintenanceId.HasValue)
            {
                DatabaseManager.UpdateMaintenance(maintenanceId.Value, vehicleId.Value, type, maintenanceDate, cost, description);
            }
            else
            {
                DatabaseManager.AddMaintenance(vehicleId.Value, type, maintenanceDate, cost, description);
            }

            DataSaved?.Invoke(this, EventArgs.Empty);

            if (!IsEmbedded)
            {
                this.Close();
            }
        }
    }
}
