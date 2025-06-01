#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusBus.Utils
{
    /// <summary>
    /// Represents a tracked disposable resource.
    /// </summary>
    public class TrackedResource
    {
        public Guid Id { get; set; }
        public IDisposable Resource { get; set; } = null!; // Initialized in TrackResource
        public string ResourceType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DisposedAt { get; set; }
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// Tracks and manages disposable resources to ensure proper cleanup
    /// </summary>
    public static class ResourceTracker
    {
        /// <summary>
        /// Resets the ResourceTracker static state (for test isolation)
        /// </summary>
        public static void Reset()
        {
            ReleaseAllResources();
            _resources.Clear();
            _logger = null;
            _isEnabled = false;
        }
        private static readonly ConcurrentDictionary<Guid, TrackedResource> _resources =
            new ConcurrentDictionary<Guid, TrackedResource>();

        private static ILogger? _logger;
        private static bool _isEnabled;

        // LoggerMessage delegates
        private static readonly Action<ILogger, Exception?> _logResourceTrackerInitialized =
            LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(Initialize)), "[RESOURCE-TRACKER] Initialized");

        private static readonly Action<ILogger, string, string, Guid, Exception?> _logResourceTracked =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Debug, new EventId(2, nameof(TrackResource)), "[RESOURCE-TRACKER] Resource tracked: {ResourceType} | {Description} | ID: {Id}");

        private static readonly Action<ILogger, string, string, Guid, Exception?> _logResourceReleased =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Debug, new EventId(3, nameof(ReleaseResource)), "[RESOURCE-TRACKER] Resource released: {ResourceType} | {Description} | ID: {Id}");

        private static readonly Action<ILogger, string, string, Guid, Exception?> _logErrorDisposingResource =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Error, new EventId(4, nameof(ReleaseResource)), "[RESOURCE-TRACKER] Error disposing resource: {ResourceType} | {Description} | ID: {Id}");

        private static readonly Action<ILogger, int, Exception?> _logReleasingAllResources =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(5, nameof(ReleaseAllResources)), "[RESOURCE-TRACKER] Releasing all tracked resources ({Count})");

        private static readonly Action<ILogger, int, Exception?> _logTrackedResourcesFinalized =
            LoggerMessage.Define<int>(LogLevel.Warning, new EventId(6, nameof(CheckForUndisposedResources)), "[RESOURCE-TRACKER] {Count} tracked resources were not explicitly released and are being finalized.");

        private static readonly Action<ILogger, string, string, Guid, Exception?> _logUndisposedResource =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Warning, new EventId(7, nameof(CheckForUndisposedResources)), "[RESOURCE-TRACKER] Undisposed resource: {ResourceType} | {Description} | ID: {Id}");

        private static readonly Action<ILogger, string, string, Guid, Exception?> _logResourceTrackedDebug =
            LoggerMessage.Define<string, string, Guid>(LogLevel.Debug, new EventId(8, nameof(TrackResource)), "[RESOURCE-TRACKER] Resource tracked: {ResourceType} | {Description} | ID: {Id}");


        /// <summary>
        /// Initialize the resource tracker
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            Initialize(logger, true);
        }

        /// <summary>
        /// Initialize the resource tracker with optional verbosity control
        /// </summary>
        /// <param name="logger">The logger to use</param>
        /// <param name="verbose">Whether to log verbose initialization and tracking messages</param>
        public static void Initialize(ILogger logger, bool verbose)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
            _isEnabled = true;
            _verbose = verbose;

            if (_verbose)
            {
                _logResourceTrackerInitialized(_logger, null);
            }
        }

        // Track verbosity setting
        private static bool _verbose = true;

        /// <summary>
        /// Track a disposable resource
        /// </summary>
        public static Guid TrackResource(IDisposable resource, string resourceType, string description)
        {

            if (resource == null)
            {
                System.Diagnostics.Debug.WriteLine("[ResourceTracker] TrackResource called with null resource");
                return Guid.Empty;
            }
            if (!_isEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[ResourceTracker] TrackResource called when _isEnabled is false. Did you forget to call Initialize()?");
                return Guid.Empty;
            }

            var id = Guid.NewGuid();
            var stack = new System.Diagnostics.StackTrace(1, true);

            var trackedResource = new TrackedResource
            {
                Id = id,
                Resource = resource,
                ResourceType = resourceType,
                Description = description,
                CreatedAt = DateTime.Now,
                StackTrace = stack.ToString()
            }; _resources.TryAdd(id, trackedResource);

            // Only log resource tracking if verbose mode is enabled
            if (_logger != null && _verbose)
            {
                _logResourceTrackedDebug(_logger, resourceType, description, id, null);
            }

            return id;
        }

        /// <summary>
        /// Release a tracked resource
        /// </summary>
        public static void ReleaseResource(Guid id)
        {
            if (!_isEnabled || id == Guid.Empty)
            {
                System.Diagnostics.Debug.WriteLine($"[ResourceTracker] ReleaseResource called with _isEnabled={_isEnabled}, id={id}");
                return;
            }

            if (_resources.TryRemove(id, out var resource))
            {
                try
                {
                    resource.Resource.Dispose();
                    resource.DisposedAt = DateTime.Now;

                    _logResourceReleased(_logger!, resource.ResourceType, resource.Description, id, null);
                }
                catch (Exception ex)
                {
                    _logErrorDisposingResource(_logger!, resource.ResourceType, resource.Description, id, ex);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ResourceTracker] ReleaseResource: id {id} not found in _resources");
            }
        }

        /// <summary>
        /// Get a list of all tracked resources
        /// </summary>
        public static TrackedResource[] GetTrackedResources()
        {
            return _resources.Values.ToArray();
        }

        /// <summary>
        /// Get a list of all tracked resources of a specific type
        /// </summary>
        public static TrackedResource[] GetTrackedResources(string resourceType)
        {
            return _resources.Values
                .Where(r => r.ResourceType == resourceType)
                .ToArray();
        }

        /// <summary>
        /// Get a specific tracked resource by its ID
        /// </summary>
        public static TrackedResource? GetTrackedResource(Guid id)
        {
            _resources.TryGetValue(id, out var resource);
            return resource; // This can return null if the key is not found, resolving CS8603
        }

        /// <summary>
        /// Release all tracked resources
        /// </summary>
        public static void ReleaseAllResources()
        {
            if (!_isEnabled) return;

            _logReleasingAllResources(_logger!, _resources.Count, null);

            foreach (var id in _resources.Keys.ToArray())
            {
                ReleaseResource(id);
            }
        }        /// <summary>
                 /// Creates a disposable wrapper that automatically releases the resource when disposed
                 /// </summary>
        public static IDisposable? CreateAutoTracker(IDisposable? resource, string resourceType, string description)
        {
            if (!_isEnabled || resource == null)
            {
                return resource;
            }

            var id = TrackResource(resource, resourceType, description);
            return new ResourceReleaser(id);
        }

        /// <summary>
        /// Log information about any resources that haven't been properly disposed
        /// </summary>
        public static void CheckForUndisposedResources()
        {
            if (!_isEnabled || _resources.IsEmpty) return;

            var undisposedCount = _resources.Count;
            if (undisposedCount > 0)
            {
                _logTrackedResourcesFinalized(_logger!, undisposedCount, null);
                foreach (var resource in _resources.Values)
                {
                    _logUndisposedResource(_logger!, resource.ResourceType, resource.Description, resource.Id, null);
                }
            }
        }

        // Test-friendly static API
        public static Guid Track(IDisposable resource, string? tag = null)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            if (!_isEnabled)
                throw new InvalidOperationException("ResourceTracker is not enabled. Did you forget to call Initialize()?");
            return TrackResource(resource, resource.GetType().Name, tag ?? string.Empty);
        }
        public static void Dispose(Guid id)
        {
            ReleaseResource(id);
        }
        public static void DisposeAll()
        {
            ReleaseAllResources();
        }
        public static Guid TrackUsing(IDisposable resource, string? tag = null)
        {
            return Track(resource, tag);
        }
        public static List<Guid> TrackMultiple(IEnumerable<IDisposable> resources, string? tag = null)
        {
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            if (!_isEnabled)
                throw new InvalidOperationException("ResourceTracker is not enabled. Did you forget to call Initialize()?");
            var ids = new List<Guid>();
            foreach (var r in resources)
                ids.Add(Track(r, tag));
            return ids;
        }
        public static void DisposeByTag(string tag)
        {
            var toDispose = _resources.Values.Where(r => r.Description == tag).Select(r => r.Id).ToList();
            foreach (var id in toDispose)
                Dispose(id);
        }

        /// <summary>
        /// Helper class to automatically release a tracked resource when disposed
        /// </summary>
        private class ResourceReleaser : IDisposable
        {
            private readonly Guid _resourceId;
            private bool _disposed;

            public ResourceReleaser(Guid resourceId)
            {
                _resourceId = resourceId;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    ReleaseResource(_resourceId);
                    _disposed = true;
                }
            }
        }
    }
}
