// Enable nullable reference types for this file
#nullable enable
#pragma warning disable CS0169 // Field is never used
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public partial class VehiclePanel : BaseForm
    {
        private readonly IVehicleService _vehicleService;
        private Vehicle? _vehicle;
        private bool _isNewVehicle;

        private TextBox _numberTextBox = null!;
        private NumericUpDown _capacityNumericUpDown = null!;
        private TextBox _modelTextBox = null!;
        private TextBox _licensePlateTextBox = null!;
        private CheckBox _isActiveCheckBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public Vehicle? Vehicle => _vehicle;
        public bool IsSaved { get; private set; }
        public static bool SuppressDialogsForTests { get; set; }

        public VehiclePanel(IVehicleService vehicleService, Vehicle? vehicle = null)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _vehicle = vehicle;
            _isNewVehicle = vehicle == null || vehicle.Id == Guid.Empty;

            InitializeComponent();
            LoadVehicleData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewVehicle ? "Add New Vehicle" : "Edit Vehicle";
            this.Size = new Size(450, 300);
            this.MinimumSize = new Size(400, 250);
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 2,
                Padding = new Padding(15),
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Vehicle Number
            var numberLabel = new Label
            {
                Text = "Number:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(numberLabel, 0, 0); _numberTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_numberTextBox, 1, 0);

            // Capacity
            var capacityLabel = new Label
            {
                Text = "Capacity:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(capacityLabel, 0, 1); _capacityNumericUpDown = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3),
                Minimum = 1,
                Maximum = 200,
                Value = 40
            };
            mainPanel.Controls.Add(_capacityNumericUpDown, 1, 1);

            // Model
            var modelLabel = new Label
            {
                Text = "Model:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(modelLabel, 0, 2); _modelTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_modelTextBox, 1, 2);

            // License Plate
            var licensePlateLabel = new Label
            {
                Text = "License Plate:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(licensePlateLabel, 0, 3); _licensePlateTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_licensePlateTextBox, 1, 3);

            // Is Active
            var isActiveLabel = new Label
            {
                Text = "Active:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(isActiveLabel, 0, 4);

            _isActiveCheckBox = new CheckBox
            {
                Dock = DockStyle.Fill,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3),
                Checked = true
            };
            mainPanel.Controls.Add(_isActiveCheckBox, 1, 4);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };

            _cancelButton = new Button
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
            _cancelButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.BorderColor;
            _cancelButton.Click += (s, e) => this.Close();

            _saveButton = new Button
            {
                Text = "Save",
                Size = new Size(80, 30),
                BackColor = ThemeManager.CurrentTheme.ButtonHoverBackground,
                ForeColor = Color.White,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(5, 0, 0, 0)
            };
            _saveButton.FlatAppearance.BorderColor = ThemeManager.CurrentTheme.ButtonHoverBackground;
            _saveButton.Click += SaveButton_Click;

            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(_saveButton);

            mainPanel.Controls.Add(buttonPanel, 0, 5);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            this.Controls.Add(mainPanel);
            this.BackColor = ThemeManager.CurrentTheme.MainBackground;
            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadVehicleData()
        {
            if (_vehicle != null && !_isNewVehicle)
            {
                _numberTextBox.Text = _vehicle.Number;
                _capacityNumericUpDown.Value = _vehicle.Capacity;
                _modelTextBox.Text = _vehicle.Model;
                _licensePlateTextBox.Text = _vehicle.LicensePlate;
                _isActiveCheckBox.Checked = _vehicle.IsActive;
            }
        }

        public void LoadVehicle(Vehicle vehicle)
        {
            _vehicle = vehicle;
            LoadVehicleData();
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                _saveButton.Enabled = false;
                _saveButton.Text = "Saving...";

                if (_isNewVehicle)
                {
                    _vehicle = new Vehicle
                    {
                        Id = Guid.NewGuid(),
                        Number = _numberTextBox.Text.Trim(),
                        Capacity = (int)_capacityNumericUpDown.Value,
                        Model = _modelTextBox.Text.Trim(),
                        LicensePlate = _licensePlateTextBox.Text.Trim(),
                        IsActive = _isActiveCheckBox.Checked
                    };

                    await _vehicleService.CreateAsync(_vehicle);
                }
                else if (_vehicle != null)
                {
                    _vehicle.Number = _numberTextBox.Text.Trim();
                    _vehicle.Capacity = (int)_capacityNumericUpDown.Value;
                    _vehicle.Model = _modelTextBox.Text.Trim();
                    _vehicle.LicensePlate = _licensePlateTextBox.Text.Trim();
                    _vehicle.IsActive = _isActiveCheckBox.Checked;

                    await _vehicleService.UpdateAsync(_vehicle);
                }

                IsSaved = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show($"Error saving vehicle: {ex.Message}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _saveButton.Enabled = true;
                _saveButton.Text = "Save";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_numberTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Vehicle number is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _numberTextBox.Focus();
                }
                return false;
            }
            if (string.IsNullOrWhiteSpace(_modelTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Vehicle model is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _modelTextBox.Focus();
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(_licensePlateTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("License plate is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _licensePlateTextBox.Focus();
                }
                return false;
            }

            return true;
        }
    }
}
#pragma warning restore CS0169 // Field is never used
