#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
#pragma warning disable CS0169 // The field is never used
#pragma warning disable CA1416 // Platform compatibility (Windows-only)
#pragma warning disable CS1998 // Async method lacks 'await' operators
#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using BusBus.Models;
using BusBus.UI.Common;

namespace BusBus
{
    public partial class VehicleForm : BaseForm
    {
        public static bool SuppressDialogsForTests { get; set; } = false;

        private DatabaseManager dbManager;
        private int? vehicleId;
        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }
        private static readonly string[] items = new[] { "Available", "In Service", "Maintenance", "Out of Service" };

        public VehicleForm(int? id = null)
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            vehicleId = id;

            if (vehicleId.HasValue)
            {
                LoadVehicleData();
            }
        }

        private void InitializeComponent()
        {
            // Form setup
            this.Text = "Vehicle Information";
            this.Size = new System.Drawing.Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;

            // Vehicle Number
            var lblVehicleNumber = new Label { Text = "Vehicle Number:", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(100, 25) };
            var txtNumber = new TextBox { Name = "txtNumber", Location = new System.Drawing.Point(130, 20), Size = new System.Drawing.Size(240, 25) };

            // Capacity
            var lblCapacity = new Label { Text = "Capacity:", Location = new System.Drawing.Point(20, 60), Size = new System.Drawing.Size(100, 25) };
            var txtCapacity = new TextBox { Name = "txtCapacity", Location = new System.Drawing.Point(130, 60), Size = new System.Drawing.Size(240, 25) };

            // Status
            var lblStatus = new Label { Text = "Status:", Location = new System.Drawing.Point(20, 100), Size = new System.Drawing.Size(100, 25) };
            var cmbStatus = new ComboBox
            {
                Name = "cmbStatus",
                Location = new System.Drawing.Point(130, 100),
                Size = new System.Drawing.Size(240, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(items);

            // Make/Model
            var lblMakeModel = new Label { Text = "Make/Model:", Location = new System.Drawing.Point(20, 140), Size = new System.Drawing.Size(100, 25) };
            var txtMakeModel = new TextBox { Name = "txtMakeModel", Location = new System.Drawing.Point(130, 140), Size = new System.Drawing.Size(240, 25) };

            // Year
            var lblYear = new Label { Text = "Year:", Location = new System.Drawing.Point(20, 180), Size = new System.Drawing.Size(100, 25) };
            var txtYear = new TextBox { Name = "txtYear", Location = new System.Drawing.Point(130, 180), Size = new System.Drawing.Size(240, 25) };

            // Buttons
            var btnSave = new Button
            {
                Name = "btnSave",
                Text = "Save",
                Location = new System.Drawing.Point(130, 230),
                Size = new System.Drawing.Size(100, 30)
            };
            btnSave.Click += btnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(240, 230),
                Size = new System.Drawing.Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            // Add controls
            this.Controls.AddRange(new Control[] {
                lblVehicleNumber, txtNumber, lblCapacity, txtCapacity,
                lblStatus, cmbStatus, lblMakeModel, txtMakeModel,
                lblYear, txtYear, btnSave, btnCancel
            });
        }

        private void LoadVehicleData()
        {
            if (!vehicleId.HasValue)
                throw new InvalidOperationException("vehicleId must have a value to load vehicle data.");

            var vehicle = DatabaseManager.GetVehicleById(vehicleId.Value);
            if (vehicle != null)
            {
                if (Controls["txtNumber"] is TextBox txtNumber)
                    txtNumber.Text = vehicle.Number;
                if (Controls["txtCapacity"] is TextBox txtCapacity)
                    txtCapacity.Text = vehicle.Capacity.ToString();
                if (Controls["cmbStatus"] is ComboBox cmbStatus)
                    cmbStatus.SelectedItem = vehicle.Status;
                if (Controls["txtMakeModel"] is TextBox txtMakeModel)
                    txtMakeModel.Text = vehicle.MakeModel;
                if (Controls["txtYear"] is TextBox txtYear)
                    txtYear.Text = vehicle.Year?.ToString() ?? string.Empty;
            }
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            string? number = null;
            string? capacityText = null;
            string? status = null;
            string? makeModel = null;
            string? yearText = null;

            if (Controls["txtNumber"] is TextBox txtNumber)
                number = txtNumber.Text;
            if (Controls["txtCapacity"] is TextBox txtCapacity)
                capacityText = txtCapacity.Text;
            if (Controls["cmbStatus"] is ComboBox cmbStatus)
                status = cmbStatus.SelectedItem?.ToString();
            if (Controls["txtMakeModel"] is TextBox txtMakeModel)
                makeModel = txtMakeModel.Text;
            if (Controls["txtYear"] is TextBox txtYear)
                yearText = txtYear.Text;            // Validation
            if (string.IsNullOrWhiteSpace(number))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Please enter a vehicle number.", "Validation Error");
                }
                return;
            }

            if (!int.TryParse(capacityText, out int capacity) || capacity <= 0)
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Please enter a valid capacity.", "Validation Error");
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(status))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Please select a status.", "Validation Error");
                }
                return;
            }

            int year = 0;
            if (!string.IsNullOrWhiteSpace(yearText) && !int.TryParse(yearText, out year))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Please enter a valid year.", "Validation Error");
                }
                return;
            }            // Save - match DatabaseManager method signatures
            bool isActive = status == "Available" || status == "In Service";
            if (vehicleId.HasValue)
            {
                DatabaseManager.UpdateVehicle(vehicleId.Value, number, capacity, makeModel ?? "", "", isActive);
            }
            else
            {
                DatabaseManager.AddVehicle(number, capacity, makeModel ?? "", "", isActive);
            }

            DataSaved?.Invoke(this, EventArgs.Empty);

            if (!IsEmbedded)
            {
                this.Close();
            }
        }
        private void SaveVehicleButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (ValidateChildren())
                {
                    // Get form control values
                    var txtNumber = Controls["txtNumber"] as TextBox;
                    var txtCapacity = Controls["txtCapacity"] as TextBox;
                    var cmbStatus = Controls["cmbStatus"] as ComboBox;
                    var txtMakeModel = Controls["txtMakeModel"] as TextBox;
                    var txtYear = Controls["txtYear"] as TextBox;

                    if (txtNumber == null || txtCapacity == null || cmbStatus == null || txtMakeModel == null || txtYear == null)
                    {
                        MessageBox.Show("Error accessing form controls.", "Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var number = txtNumber.Text;
                    var capacity = int.TryParse(txtCapacity.Text, out var cap) ? cap : 0;
                    var status = cmbStatus.SelectedItem?.ToString() ?? "Available";
                    var makeModel = txtMakeModel.Text;
                    var year = int.TryParse(txtYear.Text, out var yr) ? yr : (int?)null;                    // Create or update vehicle using proper Vehicle model - match DatabaseManager signatures
                    bool isActive = status == "Available" || status == "In Service";
                    if (vehicleId.HasValue)
                    {
                        // Update existing vehicle - use proper parameters
                        DatabaseManager.UpdateVehicle(vehicleId.Value, number, capacity, makeModel, "", isActive);
                    }
                    else
                    {
                        // Add new vehicle - use proper parameters
                        DatabaseManager.AddVehicle(number, capacity, makeModel, "", isActive);
                    }

                    DataSaved?.Invoke(this, EventArgs.Empty);

                    if (!IsEmbedded)
                    {
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving vehicle: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
