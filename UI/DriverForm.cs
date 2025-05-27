#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Drawing; // Add this for Size
using System.Windows.Forms;

namespace BusBus
{
    public partial class DriverForm : Form
    {
        private DatabaseManager dbManager;
        private int? driverId;
        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }

        // Add these controls as fields if they don't exist
        private TextBox txtDriverName = new TextBox();
        private TextBox txtLicenseNumber = new TextBox();
        private TextBox txtContactInfo = new TextBox();

        public DriverForm() : this(null) { }

        public DriverForm(int? id)
        {
            InitializeComponent();
            dbManager = new DatabaseManager();
            driverId = id;

            if (driverId.HasValue)
            {
                LoadDriverData();
            }
        }

        private void InitializeComponent()
        {
            // Basic form initialization
            this.Size = new Size(400, 300);
            this.Text = "Driver Form";

            // Add basic layout
            this.Controls.Add(txtDriverName);
            this.Controls.Add(txtLicenseNumber);
            this.Controls.Add(txtContactInfo);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // ...existing code...

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

        private void ClearForm()
        {
            txtDriverName.Clear();
            txtLicenseNumber.Clear();
            txtContactInfo.Clear();
        }

        private static void LoadDriverData()
        {
            // Method to load driver data into the form fields
        }
    }
}
