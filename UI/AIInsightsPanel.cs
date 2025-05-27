#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using BusBus.AI;

namespace BusBus.UI
{
    public class AIInsightsPanel : UserControl
    {
        private GrokService grokService;
        private RichTextBox insightsTextBox;
        private ComboBox analysisTypeCombo;
        private Button generateButton;
        private ProgressBar progressBar;
        private static readonly string[] items = new[] {
                "Maintenance Optimization",
                "Route Efficiency",
                "Driver Performance",
                "Cost Analysis",
                "Safety Predictions",
                "Ridership Trends"
            };

        public AIInsightsPanel()
        {
            InitializeComponent();
            grokService = new GrokService();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(600, 400);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            var titleLabel = new Label
            {
                Text = "🤖 AI-Powered Insights",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 122, 204)
            };

            analysisTypeCombo = new ComboBox
            {
                Location = new Point(10, 50),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            analysisTypeCombo.Items.AddRange(items);

            generateButton = new Button
            {
                Text = "Generate AI Insights",
                Location = new Point(220, 50),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            generateButton.Click += GenerateButton_Click;

            progressBar = new ProgressBar
            {
                Location = new Point(10, 85),
                Size = new Size(580, 10),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            insightsTextBox = new RichTextBox
            {
                Location = new Point(10, 105),
                Size = new Size(580, 285),
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(248, 248, 248)
            };

            this.Controls.AddRange(new Control[] {
                titleLabel, analysisTypeCombo, generateButton, progressBar, insightsTextBox
            });
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            if (analysisTypeCombo.SelectedItem == null) return;

            progressBar.Visible = true;
            generateButton.Enabled = false;
            insightsTextBox.Text = "Generating AI insights...";

            try
            {
                string insights = await GenerateInsightsAsync(analysisTypeCombo.SelectedItem.ToString());
                insightsTextBox.Text = insights;
            }
            catch (Exception ex)
            {
                insightsTextBox.Text = $"Error generating insights: {ex.Message}";
            }
            finally
            {
                progressBar.Visible = false;
                generateButton.Enabled = true;
            }
        }

        private async Task<string> GenerateInsightsAsync(string analysisType)
        {
            var dbManager = new DatabaseManager();

            return analysisType switch
            {
                "Maintenance Optimization" => await AnalyzeMaintenanceAsync(dbManager),
                "Route Efficiency" => await AnalyzeRoutesAsync(dbManager),
                "Driver Performance" => await AnalyzeDriversAsync(dbManager),
                "Cost Analysis" => await AnalyzeCostsAsync(dbManager),
                _ => "Analysis type not implemented yet."
            };
        }

        private async Task<string> AnalyzeMaintenanceAsync(DatabaseManager dbManager)
        {
            var maintenance = DatabaseManager.GetAllMaintenance();
            var vehicles = DatabaseManager.GetAllVehicles();

            var data = $"Vehicles: {vehicles.Count}, Maintenance Records: {maintenance.Count}";
            // Add more detailed data preparation

            return await grokService.AnalyzeMaintenancePatternAsync(data);
        }

        private async Task<string> AnalyzeRoutesAsync(DatabaseManager dbManager)
        {
            var routes = DatabaseManager.GetAllRoutes();
            var data = $"Total Routes: {routes.Count}";
            // Add ridership and performance data

            return await grokService.OptimizeRouteAsync(data, "Sample ridership data");
        }

        private async Task<string> AnalyzeDriversAsync(DatabaseManager dbManager)
        {
            var drivers = DatabaseManager.GetAllDrivers();
            var data = $"Total Drivers: {drivers.Count}";
            // Add performance metrics

            return await grokService.GenerateDriverInsightsAsync(data);
        }

        private async Task<string> AnalyzeCostsAsync(DatabaseManager dbManager)
        {
            var maintenance = DatabaseManager.GetAllMaintenance();
            var totalCost = 0m;
            foreach (var m in maintenance)
            {
                totalCost += m.Cost;
            }

            var prompt = $"Analyze bus fleet costs: Total maintenance cost: ${totalCost:N2}";
            return await grokService.AnalyzeMaintenancePatternAsync(prompt);
        }
    }
}
