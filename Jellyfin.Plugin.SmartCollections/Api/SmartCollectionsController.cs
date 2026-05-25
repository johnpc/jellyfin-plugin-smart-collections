using System;
using System.Net.Mime;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.Api
{
    /// <summary>
    /// The Smart Collections API controller.
    /// </summary>
    [ApiController]
    [Route("SmartCollections")]
    [Produces(MediaTypeNames.Application.Json)]
    public class SmartCollectionsController : ControllerBase, IDisposable
    {
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;
        private readonly ILogger<SmartCollectionsController> _logger;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionsController"/> class.
        /// </summary>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="applicationPaths">The application paths.</param>
        public SmartCollectionsController(
            IProviderManager providerManager,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory,
            IApplicationPaths applicationPaths)
        {
            _syncSmartCollectionsManager = new SmartCollectionsManager(
                providerManager,
                collectionManager,
                libraryManager,
                loggerFactory,
                applicationPaths);
            _logger = loggerFactory.CreateLogger<SmartCollectionsController>();
        }

        /// <summary>
        /// Creates smart collections.
        /// </summary>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("SmartCollections")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult SmartCollectionsRequest()
        {
            _logger.LogInformation("Generating Smart Collections");
            _syncSmartCollectionsManager.ExecuteSmartCollectionsNoProgress();
            _logger.LogInformation("Completed");
            return NoContent();
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
