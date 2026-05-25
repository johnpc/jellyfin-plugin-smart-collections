using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.ScheduledTasks
{
    /// <summary>
    /// Scheduled task that triggers smart collection synchronization.
    /// </summary>
    public class ExecuteSmartCollectionsTask : IScheduledTask, IDisposable
    {
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteSmartCollectionsTask"/> class.
        /// </summary>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="applicationPaths">The application paths.</param>
        public ExecuteSmartCollectionsTask(
            IProviderManager providerManager,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            ILogger<SmartCollectionsManager> logger,
            IApplicationPaths applicationPaths)
        {
            _syncSmartCollectionsManager = new SmartCollectionsManager(
                providerManager,
                collectionManager,
                libraryManager,
                logger,
                applicationPaths);
        }

        /// <inheritdoc />
        public string Name => "Smart Collections";

        /// <inheritdoc />
        public string Key => "SmartCollections";

        /// <inheritdoc />
        public string Description => "Enables creation of Smart Collections based on Tags";

        /// <inheritdoc />
        public string Category => "Smart Collections";

        /// <inheritdoc />
        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
            => _syncSmartCollectionsManager.ExecuteSmartCollections(progress, cancellationToken);

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(24).Ticks,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">Whether called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _syncSmartCollectionsManager?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
