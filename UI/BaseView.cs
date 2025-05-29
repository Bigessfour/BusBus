#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI
{
    public abstract class BaseView : UserControl, IView
    {
        protected CancellationTokenSource? _viewCancellationTokenSource;

        public abstract string ViewName { get; }
        public abstract string Title { get; }
        public Control? Control => this;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public event EventHandler<StatusEventArgs>? StatusUpdated;

        protected BaseView()
        {
            // Do not call InitializeView() here! Derived classes must call it after their fields are set.
        }

        protected virtual void InitializeView()
        {
            this.Dock = DockStyle.Fill;
        }

        public virtual async Task ActivateAsync(CancellationToken cancellationToken)
        {
            _viewCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await OnActivateAsync(_viewCancellationTokenSource.Token);
        }        public virtual async Task DeactivateAsync()
        {
            try
            {
                if (_viewCancellationTokenSource != null && !_viewCancellationTokenSource.IsCancellationRequested)
                {
                    _viewCancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }

            await OnDeactivateAsync();

            try
            {
                _viewCancellationTokenSource?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }
            finally
            {
                _viewCancellationTokenSource = null;
            }
        }

        protected abstract Task OnActivateAsync(CancellationToken cancellationToken);
        protected abstract Task OnDeactivateAsync();

        protected void NavigateTo(string viewName, object? parameter = null)
        {
            NavigationRequested?.Invoke(this, new NavigationEventArgs(viewName, parameter));
        }

        protected void UpdateStatus(string message, StatusType type = StatusType.Info)
        {
            StatusUpdated?.Invoke(this, new StatusEventArgs(message, type));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _viewCancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
