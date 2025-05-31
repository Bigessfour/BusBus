namespace BusBus.UI.Interfaces;

public interface IView
{
    string ViewName { get; }
    string Title { get; }
    System.Windows.Forms.Control? Control { get; }
    void Show();
    void Hide();
    Task ActivateAsync(System.Threading.CancellationToken cancellationToken);
    Task DeactivateAsync();
    event EventHandler<NavigationEventArgs>? NavigationChanged;
    event EventHandler<StatusEventArgs>? StatusChanged;
    event EventHandler<NavigationEventArgs>? NavigationRequested;
    event EventHandler<StatusEventArgs>? StatusUpdated;
}
