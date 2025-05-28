#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Modern view model base class supporting .NET 8 data binding improvements
    /// </summary>
    public abstract class ModernViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _isLoading;
        private string? _statusMessage;
        private bool _disposed;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Indicates if the view model is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Current status message for the view
        /// </summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Modern property setter with automatic change notification
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>        /// Thread-safe property change notification for UI updates
        /// </summary>
        protected void NotifyPropertyChangedSafe(string propertyName)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm?.InvokeRequired == true)
                {
                    mainForm.Invoke(() => OnPropertyChanged(propertyName));
                }
                else
                {
                    OnPropertyChanged(propertyName);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
