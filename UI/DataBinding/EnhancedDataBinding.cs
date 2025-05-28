#nullable enable
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace BusBus.UI.DataBinding
{
    /// <summary>
    /// Enhanced data binding helpers for .NET 8 Windows Forms
    /// Provides WPF-like binding capabilities using the new binding engine
    /// </summary>
    public static class EnhancedDataBinding
    {
        /// <summary>
        /// Binds a control property to a ViewModel property with automatic updates
        /// Uses .NET 8 enhanced data binding engine
        /// </summary>
        public static void BindProperty<TControl, TProperty>(
            TControl control,
            string controlPropertyName,
            INotifyPropertyChanged source,
            string sourcePropertyName,
            DataSourceUpdateMode updateMode = DataSourceUpdateMode.OnPropertyChanged)
            where TControl : Control
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(source);

            // Use .NET 8 enhanced binding with IBindableComponent
            if (control is IBindableComponent bindableComponent)
            {
                var binding = new Binding(controlPropertyName, source, sourcePropertyName)
                {
                    DataSourceUpdateMode = updateMode,
                    ControlUpdateMode = ControlUpdateMode.OnPropertyChanged
                };

                bindableComponent.DataBindings.Add(binding);
            }
            else
            {
                // Fallback to manual property change handling
                source.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == sourcePropertyName || string.IsNullOrEmpty(e.PropertyName))
                    {
                        control.BeginInvoke(() => UpdateControlProperty(control, controlPropertyName, source, sourcePropertyName));
                    }
                };
            }
        }

        /// <summary>
        /// Creates a two-way binding between control and ViewModel properties
        /// </summary>
        public static void BindTwoWay<TControl>(
            TControl control,
            string controlPropertyName,
            INotifyPropertyChanged source,
            string sourcePropertyName)
            where TControl : Control
        {
            BindProperty<TControl, object?>(control, controlPropertyName, source, sourcePropertyName, DataSourceUpdateMode.OnPropertyChanged);
        }

        /// <summary>
        /// Binds a control's enabled state to a ViewModel property
        /// Useful for command pattern implementations
        /// </summary>
        public static void BindEnabled<TControl>(
            TControl control,
            INotifyPropertyChanged source,
            string sourcePropertyName)
            where TControl : Control
        {
            BindProperty<TControl, bool>(control, nameof(Control.Enabled), source, sourcePropertyName);
        }

        /// <summary>
        /// Binds a control's visibility to a ViewModel property
        /// </summary>
        public static void BindVisible<TControl>(
            TControl control,
            INotifyPropertyChanged source,
            string sourcePropertyName)
            where TControl : Control
        {
            BindProperty<TControl, bool>(control, nameof(Control.Visible), source, sourcePropertyName);
        }

        private static void UpdateControlProperty(Control control, string propertyName, object source, string sourcePropertyName)
        {
            try
            {
                var sourceProperty = source.GetType().GetProperty(sourcePropertyName);
                var controlProperty = control.GetType().GetProperty(propertyName);

                if (sourceProperty != null && controlProperty != null && controlProperty.CanWrite)
                {
                    var value = sourceProperty.GetValue(source);
                    controlProperty.SetValue(control, value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating control property: {ex.Message}");
            }
        }
    }
}
