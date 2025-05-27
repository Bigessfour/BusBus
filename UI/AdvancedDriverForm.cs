#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BusBus.Models;

namespace BusBus.UI
{
    public class AdvancedDriverForm : Form
    {
        private DatabaseManager dbManager;
        private int? driverId;
        private Driver currentDriver;

        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }
        private static readonly string[] items = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-", "Unknown" };

        public AdvancedDriverForm(int? id = null)
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            driverId = id;

            if (driverId.HasValue)
            {
                LoadDriverData();
            }
            else
            {
                currentDriver = new Driver();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Advanced Driver Information";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            // Create tabbed interface for advanced data
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Basic Info Tab
            var basicTab = new TabPage("Basic Information");
            CreateBasicInfoControls(basicTab);

            // Personal Details Tab (JSON data)
            var personalTab = new TabPage("Personal Details");
            CreatePersonalDetailsControls(personalTab);

            // Emergency Contact Tab (JSON data)
            var emergencyTab = new TabPage("Emergency Contact");
            CreateEmergencyContactControls(emergencyTab);

            tabControl.TabPages.AddRange(new TabPage[] { basicTab, personalTab, emergencyTab });
            this.Controls.Add(tabControl);
        }

        private static void CreateBasicInfoControls(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // Name
            var lblName = new Label { Text = "Name:", Location = new Point(20, 20), Size = new Size(100, 25) };
            var txtName = new TextBox { Name = "txtDriverName", Location = new Point(130, 20), Size = new Size(200, 25) };

            // License Number
            var lblLicenseNumber = new Label { Text = "License Number:", Location = new Point(20, 60), Size = new Size(100, 25) };
            var txtLicenseNumber = new TextBox { Name = "txtLicenseNumber", Location = new Point(130, 60), Size = new Size(200, 25) };

            // Contact Info
            var lblContactInfo = new Label { Text = "Contact Info:", Location = new Point(20, 100), Size = new Size(100, 25) };
            var txtContactInfo = new TextBox { Name = "txtContactInfo", Location = new Point(130, 100), Size = new Size(200, 25) };

            panel.Controls.AddRange(new Control[] {
                lblName, txtName, lblLicenseNumber, txtLicenseNumber,
                lblContactInfo, txtContactInfo
            });

            tab.Controls.Add(panel);
        }

        private static void CreatePersonalDetailsControls(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // Hair Color (the famous field!)
            var lblHairColor = new Label { Text = "Hair Color:", Location = new Point(20, 20), Size = new Size(100, 25) };
            var txtHairColor = new TextBox { Name = "txtHairColor", Location = new Point(130, 20), Size = new Size(200, 25) };

            // Eye Color
            var lblEyeColor = new Label { Text = "Eye Color:", Location = new Point(20, 60), Size = new Size(100, 25) };
            var txtEyeColor = new TextBox { Name = "txtEyeColor", Location = new Point(130, 60), Size = new Size(200, 25) };

            // Height
            var lblHeight = new Label { Text = "Height (cm):", Location = new Point(20, 100), Size = new Size(100, 25) };
            var txtHeight = new NumericUpDown { Name = "txtHeight", Location = new Point(130, 100), Size = new Size(200, 25), Minimum = 100, Maximum = 250 };

            // Blood Type
            var lblBloodType = new Label { Text = "Blood Type:", Location = new Point(20, 140), Size = new Size(100, 25) };
            var cmbBloodType = new ComboBox
            {
                Name = "cmbBloodType",
                Location = new Point(130, 140),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbBloodType.Items.AddRange(items);

            // Allergies
            var lblAllergies = new Label { Text = "Allergies:", Location = new Point(20, 180), Size = new Size(100, 25) };
            var txtAllergies = new TextBox
            {
                Name = "txtAllergies",
                Location = new Point(130, 180),
                Size = new Size(300, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Medical Notes
            var lblMedicalNotes = new Label { Text = "Medical Notes:", Location = new Point(20, 250), Size = new Size(100, 25) };
            var txtMedicalNotes = new TextBox
            {
                Name = "txtMedicalNotes",
                Location = new Point(130, 250),
                Size = new Size(300, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            panel.Controls.AddRange(new Control[] {
                lblHairColor, txtHairColor, lblEyeColor, txtEyeColor,
                lblHeight, txtHeight, lblBloodType, cmbBloodType,
                lblAllergies, txtAllergies, lblMedicalNotes, txtMedicalNotes
            });

            tab.Controls.Add(panel);
        }

        private static void CreateEmergencyContactControls(TabPage tab)
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // Emergency Contact Name
            var lblEcName = new Label { Text = "Emergency Contact Name:", Location = new Point(20, 20), Size = new Size(150, 25) };
            var txtEcName = new TextBox { Name = "txtEcName", Location = new Point(180, 20), Size = new Size(200, 25) };

            // Emergency Contact Phone
            var lblEcPhone = new Label { Text = "Emergency Contact Phone:", Location = new Point(20, 60), Size = new Size(150, 25) };
            var txtEcPhone = new TextBox { Name = "txtEcPhone", Location = new Point(180, 60), Size = new Size(200, 25) };

            panel.Controls.AddRange(new Control[] {
                lblEcName, txtEcName, lblEcPhone, txtEcPhone
            });

            tab.Controls.Add(panel);
        }        private void LoadDriverData()
        {
            if (driverId.HasValue)
            {
                var driver = DatabaseManager.GetDriverById(driverId.Value);
                if (driver != null)
                {
                    currentDriver = driver;
                    PopulateFormFromDriver();
                }
            }
        }

        private void PopulateFormFromDriver()
        {
            // Basic info
            if (Controls["txtDriverName"] is TextBox nameBox)
                nameBox.Text = currentDriver.DriverName;

            // Personal details from JSON
            if (Controls["txtHairColor"] is TextBox hairBox)
                hairBox.Text = currentDriver.PersonalDetails?.HairColor ?? "";

            // ... populate other fields from JSON data
        }

        private void SaveDriver()
        {
            // Update basic fields
            currentDriver.DriverName = (Controls["txtDriverName"] as TextBox)?.Text ?? "";

            // Update JSON fields
            if (currentDriver.PersonalDetails == null)
                currentDriver.PersonalDetails = new PersonalDetails();

            currentDriver.PersonalDetails.HairColor = (Controls["txtHairColor"] as TextBox)?.Text ?? "";
            // ... update other JSON fields

            // Save to database (the backend will handle JSON serialization)
            if (driverId.HasValue)
            {
                DatabaseManager.UpdateDriver(driverId.Value, currentDriver.DriverName,
                    currentDriver.LicenseNumber, currentDriver.ContactInfo);
                DatabaseManager.UpdateDriverPersonalDetails(driverId.Value,
                    new Dictionary<string, object> { ["personalDetails"] = currentDriver.PersonalDetails });
            }
            else
            {
                DatabaseManager.AddDriver(currentDriver.DriverName, currentDriver.LicenseNumber, currentDriver.ContactInfo);
                // TODO: Get new ID and update personal details
            }

            DataSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
