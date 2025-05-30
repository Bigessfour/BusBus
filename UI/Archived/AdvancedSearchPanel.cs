#pragma warning disable CS8618 // Non-nullable field/property/event must contain a non-null value when exiting constructor
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate
// Static readonly arrays moved inside the class below
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace BusBus.UI
{
    public class AdvancedSearchPanel : UserControl
    {
        private static readonly string[] OperatorOptions = { "Equals", "Contains", "Starts With", "Greater Than", "Less Than", "Between" };
        private static readonly string[] DriverFields = { "DriverName", "LicenseNumber", "Status", "HairColor", "YearsOfService" };
        private static readonly string[] VehicleFields = { "Number", "Status", "Capacity", "MakeModel", "Year", "FuelType" };
        private static readonly string[] RouteFields = { "RouteName", "StartLocation", "EndLocation", "IsActive" };
        private static readonly string[] MaintenanceFields = { "Number", "MaintenanceType", "Cost", "MaintenanceDate" };
        private ComboBox searchTypeCombo;
        private ComboBox fieldCombo;
        private ComboBox operatorCombo;
        private TextBox valueTextBox;
        private ListBox activeFiltersListBox;
        private Button addFilterButton;
        private Button clearFiltersButton;
        private static readonly string[] items = new[] { "Drivers", "Vehicles", "Routes", "Maintenance" };

        public event EventHandler<SearchCriteria> SearchExecuted;

        public AdvancedSearchPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(300, 400);
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.BorderStyle = BorderStyle.FixedSingle;

            var titleLabel = new Label
            {
                Text = "Advanced Search",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };

            // Search Type
            var searchTypeLabel = new Label { Text = "Search Type:", Location = new Point(10, 45), Size = new Size(80, 20) };
            searchTypeCombo = new ComboBox
            {
                Location = new Point(10, 65),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            searchTypeCombo.Items.AddRange(items);
            searchTypeCombo.SelectedIndexChanged += SearchTypeCombo_SelectedIndexChanged;

            // Field Selection
            var fieldLabel = new Label { Text = "Field:", Location = new Point(10, 95), Size = new Size(80, 20) };
            fieldCombo = new ComboBox
            {
                Location = new Point(10, 115),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Operator
            var operatorLabel = new Label { Text = "Operator:", Location = new Point(10, 145), Size = new Size(80, 20) };
            operatorCombo = new ComboBox
            {
                Location = new Point(10, 165),
                Size = new Size(280, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            operatorCombo.Items.AddRange(OperatorOptions);

            // Value
            var valueLabel = new Label { Text = "Value:", Location = new Point(10, 195), Size = new Size(80, 20) };
            valueTextBox = new TextBox
            {
                Location = new Point(10, 215),
                Size = new Size(280, 25)
            };

            // Buttons
            addFilterButton = new Button
            {
                Text = "Add Filter",
                Location = new Point(10, 250),
                Size = new Size(80, 30)
            };
            addFilterButton.Click += AddFilterButton_Click;

            clearFiltersButton = new Button
            {
                Text = "Clear All",
                Location = new Point(100, 250),
                Size = new Size(80, 30)
            };
            clearFiltersButton.Click += ClearFiltersButton_Click;

            // Active Filters
            var filtersLabel = new Label { Text = "Active Filters:", Location = new Point(10, 290), Size = new Size(100, 20) };
            activeFiltersListBox = new ListBox
            {
                Location = new Point(10, 310),
                Size = new Size(280, 80)
            };

            this.Controls.AddRange(new Control[] {
                titleLabel, searchTypeLabel, searchTypeCombo, fieldLabel, fieldCombo,
                operatorLabel, operatorCombo, valueLabel, valueTextBox,
                addFilterButton, clearFiltersButton, filtersLabel, activeFiltersListBox
            });
        }

        private void SearchTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            fieldCombo.Items.Clear();

            switch (searchTypeCombo.SelectedItem?.ToString())
            {
                case "Drivers":
                    fieldCombo.Items.AddRange(DriverFields);
                    break;
                case "Vehicles":
                    fieldCombo.Items.AddRange(VehicleFields);
                    break;
                case "Routes":
                    fieldCombo.Items.AddRange(RouteFields);
                    break;
                case "Maintenance":
                    fieldCombo.Items.AddRange(MaintenanceFields);
                    break;
            }
        }

        private void AddFilterButton_Click(object sender, EventArgs e)
        {
            if (ValidateFilter())
            {
                var filter = $"{fieldCombo.SelectedItem} {operatorCombo.SelectedItem} '{valueTextBox.Text}'";
                activeFiltersListBox.Items.Add(filter);

                // Execute search
                var criteria = new SearchCriteria
                {
                    EntityType = searchTypeCombo.SelectedItem?.ToString(),
                    Filters = GetActiveFilters()
                };
                SearchExecuted?.Invoke(this, criteria);
            }
        }

        private bool ValidateFilter()
        {
            return searchTypeCombo.SelectedItem != null &&
                   fieldCombo.SelectedItem != null &&
                   operatorCombo.SelectedItem != null &&
                   !string.IsNullOrWhiteSpace(valueTextBox.Text);
        }

        private List<SearchFilter> GetActiveFilters()
        {
            var filters = new List<SearchFilter>();
            foreach (string item in activeFiltersListBox.Items)
            {
                // Parse the filter string back to SearchFilter object
                filters.Add(ParseFilterString(item));
            }
            return filters;
        }

        private static SearchFilter ParseFilterString(string filterString)
        {
            // Implementation to parse filter string
            return new SearchFilter(); // Simplified
        }

        private void ClearFiltersButton_Click(object sender, EventArgs e)
        {
            activeFiltersListBox.Items.Clear();
            var criteria = new SearchCriteria
            {
                EntityType = searchTypeCombo.SelectedItem?.ToString(),
                Filters = new List<SearchFilter>()
            };
            SearchExecuted?.Invoke(this, criteria);
        }
    }

    public class SearchCriteria
    {
        public string EntityType { get; set; }
        public List<SearchFilter> Filters { get; set; } = new List<SearchFilter>();
    }

    public class SearchFilter
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
    }
}
