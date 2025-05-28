#pragma warning disable CS8618 // Non-nullable field/property/event must contain a non-null value when exiting constructor
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using BusBus.Models;

namespace BusBus.UI
{
    public class JsonDataEditor : UserControl
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private TextBox jsonTextBox;
        private Button validateButton;
        private Label statusLabel;

        public string JsonData { get; set; }
        public event EventHandler JsonChanged;

        public JsonDataEditor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(400, 300);

            var titleLabel = new Label
            {
                Text = "JSON Data Editor",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(5, 5),
                AutoSize = true
            };

            jsonTextBox = new TextBox
            {
                Location = new Point(5, 30),
                Size = new Size(390, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };
            jsonTextBox.TextChanged += (s, e) => JsonData = jsonTextBox.Text;

            validateButton = new Button
            {
                Text = "Validate JSON",
                Location = new Point(5, 240),
                Size = new Size(100, 25)
            };
            validateButton.Click += ValidateButton_Click;

            statusLabel = new Label
            {
                Location = new Point(115, 245),
                Size = new Size(280, 20),
                ForeColor = Color.Green,
                Text = "Ready"
            };

            this.Controls.AddRange(new Control[] { titleLabel, jsonTextBox, validateButton, statusLabel });
        }

        private void ValidateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonTextBox.Text))
                {
                    statusLabel.Text = "Empty JSON";
                    statusLabel.ForeColor = Color.Orange;
                    return;
                }

                JsonDocument.Parse(jsonTextBox.Text);
                statusLabel.Text = "✓ Valid JSON";
                statusLabel.ForeColor = Color.Green;
                JsonChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (JsonException ex)
            {
                statusLabel.Text = $"✗ Invalid: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        public void LoadPersonalDetails(PersonalDetails details)
        {
            if (details != null)
            {
                jsonTextBox.Text = JsonSerializer.Serialize(details, JsonOptions);
            }
        }
    }
}
