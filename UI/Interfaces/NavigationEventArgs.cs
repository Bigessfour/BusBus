namespace BusBus.UI.Interfaces;

public class NavigationEventArgs : EventArgs
{
    public string? TargetView { get; set; }
    public object? Parameter { get; set; }
    public NavigationEventArgs(string? targetView = null, object? parameter = null)
    {
        TargetView = targetView;
        Parameter = parameter;
    }
}
