using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.Utils
{
    /// <summary>
    /// Monitors application shutdown and provides graceful cleanup
    /// </summary>
    public sealed class ShutdownMonitor : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ManualResetEventSlim _shutdownEvent;
        private bool _disposed;
        private static readonly ConcurrentBag<Task> _trackedTasks = new ConcurrentBag<Task>();
        private static readonly ConcurrentBag<IDisposable> _trackedDisposables = new ConcurrentBag<IDisposable>();
#pragma warning disable CA1805 // Do not initialize unnecessarily
        private static volatile bool _shutdownInitiated = false;
#pragma warning restore CA1805 // Do not initialize unnecessarily

        public ShutdownMonitor()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _shutdownEvent = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Gets the cancellation token that is triggered on shutdown
        /// </summary>
        public CancellationToken Token => _cancellationTokenSource.Token;

        /// <summary>
        /// Signals the application to shutdown
        /// </summary>
        public void SignalShutdown()
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
                _shutdownEvent.Set();
            }
        }

        /// <summary>
        /// Waits for shutdown to be signaled
        /// </summary>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>True if shutdown was signaled within timeout</returns>
        public bool WaitForShutdown(TimeSpan timeout)
        {
            return _shutdownEvent.Wait(timeout);
        }

        /// <summary>
        /// Waits for shutdown to be signaled asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task WaitForShutdownAsync(CancellationToken cancellationToken = default)        {
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            
            _ = Task.Run(() =>
            {
                try
                {
                    _shutdownEvent.Wait(cancellationToken);
                    tcs.TrySetResult(true);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
            }, cancellationToken);

            await tcs.Task;}

#pragma warning disable CA1822 // Member 'TrackTask' does not access instance data and can be marked as static - keeping instance method for API consistency
        public void TrackTask(Task task, string description = "")
#pragma warning restore CA1822
        {
            if (!_shutdownInitiated)
            {
                _trackedTasks.Add(task);
                Console.WriteLine($"[ShutdownMonitor] Tracking task: {description}");
            }        }

#pragma warning disable CA1822 // Member 'TrackDisposable' does not access instance data and can be marked as static - keeping instance method for API consistency
        public void TrackDisposable(IDisposable disposable, string description = "")
#pragma warning restore CA1822
        {
            if (!_shutdownInitiated)
            {
                _trackedDisposables.Add(disposable);
                Console.WriteLine($"[ShutdownMonitor] Tracking disposable: {description}");
            }        }

#pragma warning disable CA1822 // Member 'ShutdownAsync' does not access instance data and can be marked as static - keeping instance method for API consistency
        public async Task ShutdownAsync(TimeSpan timeout = default)
#pragma warning restore CA1822
        {
            if (_shutdownInitiated)
                return;

            _shutdownInitiated = true;
            Console.WriteLine("[ShutdownMonitor] Initiating shutdown...");

            if (timeout == default)
                timeout = TimeSpan.FromSeconds(10);

            // Wait for tracked tasks
            var tasks = _trackedTasks.ToArray();
            if (tasks.Length > 0)
            {
                Console.WriteLine($"[ShutdownMonitor] Waiting for {tasks.Length} tracked tasks...");
                try
                {
                    using var cts = new CancellationTokenSource(timeout);
                    await Task.WhenAll(tasks).WaitAsync(cts.Token);
                    Console.WriteLine("[ShutdownMonitor] All tracked tasks completed");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[ShutdownMonitor] Timeout waiting for tasks");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    Console.WriteLine($"[ShutdownMonitor] Error waiting for tasks: {ex.Message}");
                }
#pragma warning restore CA1031
            }

            // Dispose tracked resources
            var disposables = _trackedDisposables.ToArray();
            if (disposables.Length > 0)
            {
                Console.WriteLine($"[ShutdownMonitor] Disposing {disposables.Length} tracked resources...");
                foreach (var disposable in disposables)
                {
#pragma warning disable CA1031 // Do not catch general exception types
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ShutdownMonitor] Error disposing resource: {ex.Message}");
                    }
#pragma warning restore CA1031
                }
                Console.WriteLine("[ShutdownMonitor] All tracked resources disposed");
            }

            Console.WriteLine("[ShutdownMonitor] Shutdown complete");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationTokenSource?.Dispose();
                _shutdownEvent?.Dispose();
                _disposed = true;
            }
        }
    }
}
