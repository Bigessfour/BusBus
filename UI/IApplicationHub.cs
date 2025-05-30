#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.UI
{
    /// <summary>
    /// Defines the contract for the main application shell/hub that manages navigation and views
    /// </summary>
    public interface IApplicationHub
    {
        /// <summary>
        /// Navigates to a specific view by name
        /// </summary>
        Task NavigateToAsync(string viewName, object? parameter = null);

        /// <summary>
        /// Shows a status message to the user
        /// </summary>
        void ShowStatus(string message, StatusType type = StatusType.Info);

        /// <summary>
        /// Shows a notification to the user
        /// </summary>
        void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);
        void ShowNotification(string title, string message, Dashboard.NotificationType type);

        /// <summary>
        /// Gets the current active view
        /// </summary>
        IView? CurrentView { get; }

        /// <summary>
        /// Event fired when navigation occurs
        /// </summary>
        event EventHandler<NavigationEventArgs>? NavigationChanged;
    }
}
