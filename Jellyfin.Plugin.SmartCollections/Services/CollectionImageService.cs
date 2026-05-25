using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Sets primary images on box-set collections from person or media sources.
    /// </summary>
    public class CollectionImageService : ICollectionImageService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILibraryQueryService _libraryQueryService;
        private readonly ILogger<CollectionImageService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionImageService"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="libraryQueryService">The library query service.</param>
        /// <param name="logger">The logger.</param>
        public CollectionImageService(
            ILibraryManager libraryManager,
            ILibraryQueryService libraryQueryService,
            ILogger<CollectionImageService> logger)
        {
            _libraryManager = libraryManager;
            _libraryQueryService = libraryQueryService;
            _logger = logger;
        }

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Plugin must not crash the server on image failures")]
        public async Task SetImageAsync(BoxSet collection, Person? specificPerson = null)
        {
            try
            {
                if (TrySetFromPerson(collection, specificPerson))
                {
                    await UpdateItemImageAsync(collection).ConfigureAwait(false);
                    return;
                }

                if (specificPerson == null && TrySetFromDetectedPerson(collection))
                {
                    await UpdateItemImageAsync(collection).ConfigureAwait(false);
                    return;
                }

                if (TrySetFromMediaItem(collection))
                {
                    await UpdateItemImageAsync(collection).ConfigureAwait(false);
                    return;
                }

                _logger.LogWarning(
                    "No items with images found in collection {CollectionName}",
                    collection.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting image for collection {CollectionName}", collection.Name);
            }
        }

        private bool TrySetFromPerson(BoxSet collection, Person? person)
        {
            if (person?.ImageInfos == null)
            {
                return false;
            }

            var imageInfo = person.ImageInfos.FirstOrDefault(i => i.Type == ImageType.Primary);
            if (imageInfo == null)
            {
                return false;
            }

            collection.SetImage(new ItemImageInfo { Path = imageInfo.Path, Type = ImageType.Primary }, 0);
            _logger.LogInformation(
                "Set image for collection {CollectionName} from person {PersonName}",
                collection.Name,
                person.Name);
            return true;
        }

        private bool TrySetFromDetectedPerson(BoxSet collection)
        {
            var person = _libraryQueryService.FindPersonWithImage(collection.Name);
            if (person == null)
            {
                return false;
            }

            return TrySetFromPerson(collection, person);
        }

        private bool TrySetFromMediaItem(BoxSet collection)
        {
            var query = new InternalItemsQuery { Recursive = true };
            var items = collection.GetItems(query).Items;

            var mediaItem = items
                .Where(item => item is Movie || item is Series)
                .FirstOrDefault(item =>
                    item.ImageInfos != null &&
                    item.ImageInfos.Any(i => i.Type == ImageType.Primary));

            if (mediaItem == null)
            {
                return false;
            }

            var imageInfo = mediaItem.ImageInfos.First(i => i.Type == ImageType.Primary);
            collection.SetImage(new ItemImageInfo { Path = imageInfo.Path, Type = ImageType.Primary }, 0);
            _logger.LogInformation(
                "Set image for collection {CollectionName} from {ItemName}",
                collection.Name,
                mediaItem.Name);
            return true;
        }

        private async Task UpdateItemImageAsync(BoxSet collection)
        {
            await _libraryManager.UpdateItemAsync(
                collection,
                collection.GetParent(),
                ItemUpdateType.ImageUpdate,
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
