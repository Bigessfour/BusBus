#nullable enable
using BusBus.UI.Commands;
using BusBus.UI.DataBinding;
using System;
using System.Windows.Input;

namespace BusBus.UI.ViewModels
{
    /// <summary>
    /// Dashboard ViewModel implementing .NET 8 MVVM patterns
    /// Uses ICommand for button actions and enhanced data binding
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly Func<string, System.Threading.Tasks.Task> _navigationHandler;
        private readonly Action _themeToggleHandler;
        private string _currentTheme = "Dark";
        private string _welcomeMessage = string.Empty;
        private bool _isNavigating;

        public DashboardViewModel(
            Func<string, System.Threading.Tasks.Task> navigationHandler,
            Action themeToggleHandler)
        {
            _navigationHandler = navigationHandler ?? throw new ArgumentNullException(nameof(navigationHandler));
            _themeToggleHandler = themeToggleHandler ?? throw new ArgumentNullException(nameof(themeToggleHandler));

            InitializeCommands();
            InitializeData();
        }

        // Properties
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        public bool IsNavigating
        {
            get => _isNavigating;
            set => SetProperty(ref _isNavigating, value);
        }

        public string ThemeButtonText => CurrentTheme == "Dark" ? "🌙" : "☀️";

        // Commands
        public ICommand? ToggleThemeCommand { get; private set; }
        public ICommand? NavigateToRoutesCommand { get; private set; }
        public ICommand? NavigateToDriversCommand { get; private set; }
        public ICommand? NavigateToVehiclesCommand { get; private set; }
        public ICommand? NavigateToReportsCommand { get; private set; }
        public ICommand? NavigateToSettingsCommand { get; private set; }

        private void InitializeCommands()
        {
            ToggleThemeCommand = new DelegateCommand(_themeToggleHandler);
            NavigateToRoutesCommand = new AsyncDelegateCommand(() => NavigateToAsync("routes"), () => !IsNavigating);
            NavigateToDriversCommand = new AsyncDelegateCommand(() => NavigateToAsync("drivers"), () => !IsNavigating);
            NavigateToVehiclesCommand = new AsyncDelegateCommand(() => NavigateToAsync("vehicles"), () => !IsNavigating);
            NavigateToReportsCommand = new AsyncDelegateCommand(() => NavigateToAsync("reports"), () => !IsNavigating);
            NavigateToSettingsCommand = new AsyncDelegateCommand(() => NavigateToAsync("settings"), () => !IsNavigating);
        }

        private void InitializeData()
        {
            WelcomeMessage = $"Welcome, {Environment.UserName}";
        }

        private async System.Threading.Tasks.Task NavigateToAsync(string viewName)
        {
            IsNavigating = true;
            try
            {
                await _navigationHandler(viewName);
            }
            finally
            {
                IsNavigating = false;
            }
        }

        public void UpdateTheme(string themeName)
        {
            CurrentTheme = themeName;
            OnPropertyChanged(nameof(ThemeButtonText));
        }

        public void RefreshCommands()
        {
            if (NavigateToRoutesCommand is DelegateCommand routesCmd) routesCmd.RaiseCanExecuteChanged();
            if (NavigateToDriversCommand is AsyncDelegateCommand driversCmd) driversCmd.RaiseCanExecuteChanged();
            if (NavigateToVehiclesCommand is AsyncDelegateCommand vehiclesCmd) vehiclesCmd.RaiseCanExecuteChanged();
            if (NavigateToReportsCommand is AsyncDelegateCommand reportsCmd) reportsCmd.RaiseCanExecuteChanged();
            if (NavigateToSettingsCommand is AsyncDelegateCommand settingsCmd) settingsCmd.RaiseCanExecuteChanged();
        }
    }
}
