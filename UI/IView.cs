#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public interface IView : IDisposable
    {
        string ViewName { get; }
        string Title { get; }
        // For Form-based views, this is redundant, but kept for compatibility
        Control? Control { get; }
        event EventHandler<NavigationEventArgs>? NavigationRequested;
        event EventHandler<StatusEventArgs>? StatusUpdated;
        Task ActivateAsync(CancellationToken cancellationToken);
        Task DeactivateAsync();
    }
}
