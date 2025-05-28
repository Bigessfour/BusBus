#nullable enable
using System;
using System.Collections.Generic;

namespace BusBus.UI
{
    /// <summary>
    /// Manages the state of the Dashboard shell, including navigation history and current view state
    /// </summary>
    public class DashboardState
    {
        public string? CurrentTheme { get; set; }
        public string? LastView { get; set; }
        public Dictionary<string, object> ViewStates { get; } = new();
        public string? CurrentViewName { get; set; }
        public object? CurrentViewParameter { get; set; }
        public List<string> NavigationHistory { get; set; } = new();
        public DateTime LastNavigationTime { get; set; }
        public bool IsLoading { get; set; }
        public string? LastStatusMessage { get; set; }
        public StatusType LastStatusType { get; set; }

        /// <summary>
        /// Records a navigation event
        /// </summary>
        public void RecordNavigation(string viewName, object? parameter = null)
        {
            CurrentViewName = viewName;
            CurrentViewParameter = parameter;
            LastNavigationTime = DateTime.Now;
            LastView = viewName;

            if (NavigationHistory.Count == 0 || NavigationHistory[^1] != viewName)
            {
                NavigationHistory.Add(viewName);
            }
        }

        /// <summary>
        /// Saves the state of a view
        /// </summary>
        public void SaveViewState(string viewName, object state)
        {
            ViewStates[viewName] = state;
        }

        /// <summary>
        /// Retrieves the saved state of a view
        /// </summary>
        public T? GetViewState<T>(string viewName) where T : class
        {
            return ViewStates.TryGetValue(viewName, out var state) ? state as T : null;
        }
    }
}
