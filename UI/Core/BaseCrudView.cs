



#nullable enable
using System;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Base class for CRUD views, provides shared DataGrid and CRUD event hooks.
    /// </summary>
    public abstract class BaseCrudView : UserControl
    {
        public string HeaderText { get; set; } = string.Empty;
        public DataGridView CrudDataGrid { get; private set; }

        public event EventHandler? CrudAddClicked;
        public event EventHandler? CrudEditClicked;
        public event EventHandler? CrudDeleteClicked;

        protected BaseCrudView()
        {
            CrudDataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            Controls.Add(CrudDataGrid);
        }

        protected void OnCrudAddClicked() => CrudAddClicked?.Invoke(this, EventArgs.Empty);
        protected void OnCrudEditClicked() => CrudEditClicked?.Invoke(this, EventArgs.Empty);
        protected void OnCrudDeleteClicked() => CrudDeleteClicked?.Invoke(this, EventArgs.Empty);
    }
}


