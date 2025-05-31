#nullable enable
#pragma warning disable CS0169 // Field is never used
using BusBus.Models;
using BusBus.Services;
using BusBus.UI.Common;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public partial class DriverPanel : BaseForm
    {
        private readonly IDriverService _driverService;
        private Driver? _driver;
        private bool _isNewDriver; private TextBox _firstNameTextBox = null!;
        private TextBox _lastNameTextBox = null!;
        private TextBox _licenseNumberTextBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public Driver? Driver => _driver;
        public bool IsSaved { get; private set; }
        public static bool SuppressDialogsForTests { get; set; }

        public DriverPanel(IDriverService driverService, Driver? driver = null)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            _driver = driver;
            _isNewDriver = driver == null || driver.Id == Guid.Empty;

            InitializeComponent();
            LoadDriverData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNewDriver ? "Add New Driver" : "Edit Driver";
            this.Size = new Size(400, 250);
            this.MinimumSize = new Size(350, 200);
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 2,
                Padding = new Padding(15),
                BackColor = ThemeManager.CurrentTheme.MainBackground
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // First Name
            var firstNameLabel = new Label
            {
                Text = "First Name:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(firstNameLabel, 0, 0); _firstNameTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_firstNameTextBox, 1, 0);

            // Last Name
            var lastNameLabel = new Label
            {
                Text = "Last Name:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(lastNameLabel, 0, 1); _lastNameTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_lastNameTextBox, 1, 1);

            // License Number
            var licenseLabel = new Label
            {
                Text = "License #:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont
            };
            mainPanel.Controls.Add(licenseLabel, 0, 2); _licenseNumberTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeManager.CurrentTheme.TextBoxBackground,
                ForeColor = ThemeManager.CurrentTheme.CardText,
                Font = ThemeManager.CurrentTheme.SmallButtonFont,
                Margin = new Padding(5, 3, 0, 3)
            };
            mainPanel.Controls.Add(_licenseNumberTextBox, 1, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            }; _cancelButton = new Button
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
            _cancelButton.Click += (s, e) => this.Close(); _saveButton = new Button
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

            mainPanel.Controls.Add(buttonPanel, 0, 3);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            this.Controls.Add(mainPanel);
            this.BackColor = ThemeManager.CurrentTheme.MainBackground;
            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadDriverData()
        {
            if (_driver != null && !_isNewDriver)
            {
                _firstNameTextBox.Text = _driver.FirstName;
                _lastNameTextBox.Text = _driver.LastName;
                _licenseNumberTextBox.Text = _driver.LicenseNumber;
            }
        }

        public void LoadDriver(Driver driver)
        {
            _driver = driver;
            LoadDriverData();
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

                if (_isNewDriver)
                {
                    _driver = new Driver
                    {
                        Id = Guid.NewGuid(),
                        FirstName = _firstNameTextBox.Text.Trim(),
                        LastName = _lastNameTextBox.Text.Trim(),
                        LicenseNumber = _licenseNumberTextBox.Text.Trim()
                    };

                    await _driverService.CreateAsync(_driver);
                }
                else if (_driver != null)
                {
                    _driver.FirstName = _firstNameTextBox.Text.Trim();
                    _driver.LastName = _lastNameTextBox.Text.Trim();
                    _driver.LicenseNumber = _licenseNumberTextBox.Text.Trim();

                    await _driverService.UpdateAsync(_driver);
                }

                IsSaved = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show($"Error saving driver: {ex.Message}", "Save Error",
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
            if (string.IsNullOrWhiteSpace(_firstNameTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("First name is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _firstNameTextBox.Focus();
                }
                return false;
            }
            if (string.IsNullOrWhiteSpace(_lastNameTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("Last name is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _lastNameTextBox.Focus();
                }
                return false;
            }
            if (string.IsNullOrWhiteSpace(_licenseNumberTextBox.Text))
            {
                if (!SuppressDialogsForTests)
                {
                    MessageBox.Show("License number is required.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _licenseNumberTextBox.Focus();
                }
                return false;
            }

            return true;
        }
    }
}
#pragma warning restore CS0169 // Field is never used
