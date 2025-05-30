#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BusBus.UI.Common;

namespace BusBus
{
    public partial class DynamicForm : BaseForm
    {
        private DatabaseManager dbManager;
        private string tableName;
        private int? recordId;
        private Dictionary<string, Control> fieldControls;
        public event EventHandler DataSaved;
        public bool IsEmbedded { get; set; }
        private static readonly string[] items = new[] { "Active", "Inactive", "Available", "Unavailable" };

        public DynamicForm(string tableName, int? recordId = null)
        {
            this.tableName = tableName;
            this.recordId = recordId;
            this.dbManager = new DatabaseManager();
            this.fieldControls = new Dictionary<string, Control>();

            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = $"{tableName} Information";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.AutoScroll = true;

            var columns = dbManager.GetTableColumns(tableName);
            var yPos = 20;

            foreach (var column in columns)
            {
                // Skip ID columns
                if (column.EndsWith("ID") && column == tableName + "ID")
                    continue;

                var label = new Label
                {
                    Text = FormatColumnName(column) + ":",
                    Location = new Point(20, yPos),
                    Size = new Size(120, 25)
                };

                Control inputControl;

                // Create appropriate control based on column name/type
                if (column.Contains("date", StringComparison.OrdinalIgnoreCase))
                {
                    inputControl = new DateTimePicker
                    {
                        Location = new Point(150, yPos),
                        Size = new Size(300, 25)
                    };
                }
                else if (column.Contains("status", StringComparison.OrdinalIgnoreCase))
                {
                    inputControl = new ComboBox
                    {
                        Location = new Point(150, yPos),
                        Size = new Size(300, 25),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };
                    // Add common status options
                    if (inputControl is ComboBox comboBox)
                        comboBox.Items.AddRange(items);
                }
                else
                {
                    inputControl = new TextBox
                    {
                        Location = new Point(150, yPos),
                        Size = new Size(300, 25)
                    };
                }

                this.Controls.Add(label);
                this.Controls.Add(inputControl);
                fieldControls[column] = inputControl;

                yPos += 35;
            }

            // Add buttons
            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(150, yPos + 20),
                Size = new Size(100, 30)
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(260, yPos + 20),
                Size = new Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            // Load data if editing
            if (recordId.HasValue)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            var data = dbManager.GetTableDataDynamic(tableName);
            if (data.Rows.Count > 0)
            {
                var row = data.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r[$"{tableName}ID"]) == recordId);
                if (row != null)
                {
                    foreach (var kvp in fieldControls)
                    {
                        if (data.Columns.Contains(kvp.Key))
                        {
                            SetControlValue(kvp.Value, row[kvp.Key]);
                        }
                    }
                }
            }
        }

        private static void SetControlValue(Control control, object value)
        {
            if (control is TextBox textBox)
                textBox.Text = value?.ToString() ?? "";
            else if (control is ComboBox comboBox)
                comboBox.SelectedItem = value?.ToString() ?? "";
            else if (control is DateTimePicker dateTimePicker && value is DateTime dateValue)
                dateTimePicker.Value = dateValue;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var values = new Dictionary<string, object>();

            foreach (var kvp in fieldControls)
            {
                values[kvp.Key] = GetControlValue(kvp.Value);
            }

            if (recordId.HasValue)
            {
                dbManager.UpdateDynamicRecord(tableName, recordId.Value, values);
            }
            else
            {
                dbManager.SaveDynamicRecord(tableName, values);
            }

            DataSaved?.Invoke(this, EventArgs.Empty);

            if (!IsEmbedded)
            {
                this.Close();
            }
        }

        private static object GetControlValue(Control control)
        {
            if (control is TextBox textBox)
                return string.IsNullOrEmpty(textBox.Text) ? null : textBox.Text;
            else if (control is ComboBox comboBox)
                return comboBox.SelectedItem;
            else if (control is DateTimePicker dateTimePicker)
                return dateTimePicker.Value;
            return null;
        }

        private static string FormatColumnName(string columnName)
        {
            // Convert "HairColor" to "Hair Color"
            // Use a source-generated Regex for performance and to avoid SYSLIB1045 warning
            return FormatColumnNameRegex().Replace(columnName, "$1 $2");
        }

        [System.Text.RegularExpressions.GeneratedRegex("([a-z])([A-Z])", System.Text.RegularExpressions.RegexOptions.Compiled)]
        private static partial System.Text.RegularExpressions.Regex FormatColumnNameRegex();
    }
}
