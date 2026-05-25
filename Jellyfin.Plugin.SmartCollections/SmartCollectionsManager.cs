using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.SmartCollections
{
    /// <summary>
    /// Thin orchestrator that delegates smart collection sync to extracted services.
    /// Retained for backward compatibility with existing DI registrations.
    /// </summary>
    public class SmartCollectionsManager : IDisposable
    {
        private readonly ISmartCollectionSyncService _syncService;
        private readonly Timer _timer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionsManager"/> class.
        /// </summary>
        /// <param name="providerManager">The provider manager (unused, kept for DI compat).</param>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="applicationPaths">The application paths.</param>
        public SmartCollectionsManager(
            IProviderManager providerManager,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            ILogger<SmartCollectionsManager> logger,
            IApplicationPaths applicationPaths)
        {
            var libraryQueryService = new LibraryQueryService(libraryManager);
            var nullImageLogger = NullLogger<CollectionImageService>.Instance;
            var collectionImageService = new CollectionImageService(
                libraryManager,
                libraryQueryService,
                nullImageLogger);
            var nullSyncLogger = NullLogger<SmartCollectionSyncService>.Instance;
            _syncService = new SmartCollectionSyncService(
                collectionManager,
                libraryManager,
                libraryQueryService,
                collectionImageService,
                nullSyncLogger);
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionsManager"/> class
        /// using a pre-built sync service.
        /// </summary>
        /// <param name="syncService">The sync service.</param>
        public SmartCollectionsManager(ISmartCollectionSyncService syncService)
        {
            _syncService = syncService;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Executes smart collections without progress reporting.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        public Task ExecuteSmartCollectionsNoProgress()
        {
            return _syncService.ExecuteAsync();
        }

        /// <summary>
        /// Executes smart collections with progress reporting.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        public Task ExecuteSmartCollections(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return _syncService.ExecuteAsync(progress, cancellationToken);
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
                    _timer?.Dispose();
                }

                _disposed = true;
            }
        }

        private void OnTimerElapsed()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
