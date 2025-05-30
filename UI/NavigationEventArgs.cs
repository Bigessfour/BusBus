#nullable enable
using System;

namespace BusBus.UI
{
    public class NavigationEventArgs : EventArgs
    {
        public string ViewName { get; }
        public object? Parameter { get; }

        public NavigationEventArgs(string viewName, object? parameter = null)
        {
            ViewName = viewName;
            Parameter = parameter;
        }
    }
}
