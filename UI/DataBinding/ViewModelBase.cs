#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BusBus.UI.DataBinding
{
    /// <summary>
    /// Base class for ViewModels supporting .NET 8 enhanced data binding
    /// Implements WPF-like MVVM patterns in Windows Forms
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Example ViewModel for Dashboard data
    /// Demonstrates .NET 8 Windows Forms MVVM patterns
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private string _welcomeMessage = string.Empty;
        private int _totalRoutes;
        private int _totalDrivers;
        private int _totalVehicles;
        private bool _isLoading;

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public int TotalRoutes
        {
            get => _totalRoutes;
            set => SetProperty(ref _totalRoutes, value);
        }

        public int TotalDrivers
        {
            get => _totalDrivers;
            set => SetProperty(ref _totalDrivers, value);
        }

        public int TotalVehicles
        {
            get => _totalVehicles;
            set => SetProperty(ref _totalVehicles, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusText => IsLoading ? "Loading..." : "Ready";
    }
}
