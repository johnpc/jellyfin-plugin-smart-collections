using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.SmartCollections

{


    public class SmartCollectionsManager : IDisposable
    {
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IProviderManager _providerManager;
        private readonly Timer _timer;
        private readonly ILogger<SmartCollectionsManager> _logger;
        private readonly string _pluginDirectory;

        public SmartCollectionsManager(IProviderManager providerManager, ICollectionManager collectionManager, ILibraryManager libraryManager, ILogger<SmartCollectionsManager> logger, IApplicationPaths applicationPaths)
        {
            _providerManager = providerManager;
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
            _pluginDirectory = Path.Combine(applicationPaths.DataPath, "smartcollections");
            Directory.CreateDirectory(_pluginDirectory);
        }

        private IEnumerable<Series> GetSeriesFromLibrary(string term)
        {
            var byTags = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = true,
                Tags = [term]
            }).Select(m => m as Series);

            var byGenres = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = true,
                Genres = [term]
            }).Select(m => m as Series);

            // Combine results and remove duplicates
            return byTags.Union(byGenres);
        }

        private IEnumerable<Movie> GetMoviesFromLibrary(string term)
        {
            var byTags = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = false,
                Tags = [term]
            }).Select(m => m as Movie);

            var byGenres = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = false,
                Genres = [term]
            }).Select(m => m as Movie);

            // Combine results and remove duplicates
            return byTags.Union(byGenres);
        }

        private async Task RemoveUnwantedMediaItems(BoxSet collection, IEnumerable<BaseItem> wantedMediaItems)
        {
            // Get the set of IDs for media items we want to keep
            var wantedItemIds = wantedMediaItems.Select(item => item.Id).ToHashSet();

            // Get current items and filter for unwanted ones
            var childrenToRemove = collection.GetLinkedChildren()
                .Where(item => !wantedItemIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToArray();

            if (childrenToRemove.Length > 0)
            {
                _logger.LogInformation($"Removing {childrenToRemove.Length} items from collection {collection.Name}");
                await _collectionManager.RemoveFromCollectionAsync(collection.Id, childrenToRemove).ConfigureAwait(true);
            }
        }

        private async Task AddWantedMediaItems(BoxSet collection, IEnumerable<BaseItem> wantedMediaItems)
        {
            // Get the set of IDs for items currently in the collection
            var existingItemIds = collection.GetLinkedChildren()
                .Select(item => item.Id)
                .ToHashSet();

            // Create LinkedChild objects for items that aren't already in the collection
            var childrenToAdd = wantedMediaItems
                .Where(item => !existingItemIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToArray();

            if (childrenToAdd.Length > 0)
            {
                _logger.LogInformation($"Adding {childrenToAdd.Length} items to collection {collection.Name}");
                await _collectionManager.AddToCollectionAsync(collection.Id, childrenToAdd).ConfigureAwait(true);
            }
        }

        private BoxSet? GetBoxSetByName(string name)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                CollapseBoxSetItems = false,
                Recursive = true,
                Tags = new[] { "smartcollection" },
                Name = name,
            }).Select(b => b as BoxSet).FirstOrDefault();
        }

        public async Task ExecuteSmartCollectionsNoProgress()
        {
            _logger.LogInformation("Performing ExecuteSmartCollections");
            // Define the list of tags to create smart collections for
            var tags = Plugin.Instance!.Configuration.Tags;

            _logger.LogInformation($"Starting execution of smart collections for {tags.Length} tags");

            foreach (var tag in tags)
            {
                try
                {
                    _logger.LogInformation($"Processing smart collection for tag: {tag}");
                    await ExecuteSmartCollectionsForTag(tag);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing smart collection for tag: {tag}");
                    // Continue with next tag even if one fails
                    continue;
                }
            }

            _logger.LogInformation("Completed execution of all smart collections");
        }

        public async Task ExecuteSmartCollections(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await ExecuteSmartCollectionsNoProgress();
        }

        private string GetCollectionName(string tag)
        {
            string capitalizedTag = tag.Length > 0
                ? char.ToUpper(tag[0]) + tag[1..]
                : tag;

            return $"{capitalizedTag} Smart Collection";
        }

        private async Task SetPhotoForCollection(BoxSet collection)
        {
            try
            {
                var query = new InternalItemsQuery
                {
                    Recursive = true
                };

                var items = collection.GetItems(query)
                    .Items
                    .Where(item => item is Movie || item is Series)
                    .ToList();

                _logger.LogDebug("Found {Count} items in collection {CollectionName}",
                    items.Count, collection.Name);

                var firstItemWithImage = items
                    .FirstOrDefault(item =>
                        item.ImageInfos != null &&
                        item.ImageInfos.Any(i => i.Type == ImageType.Primary));

                if (firstItemWithImage != null)
                {
                    var imageInfo = firstItemWithImage.ImageInfos
                        .First(i => i.Type == ImageType.Primary);

                    // Simply set the image path directly
                    collection.SetImage(new ItemImageInfo
                    {
                        Path = imageInfo.Path,
                        Type = ImageType.Primary
                    }, 0);

                    await _libraryManager.UpdateItemAsync(
                        collection,
                        collection.GetParent(),
                        ItemUpdateType.ImageUpdate,
                        CancellationToken.None);
                    _logger.LogInformation("Successfully set image for collection {CollectionName} from {ItemName}",
                        collection.Name, firstItemWithImage.Name);
                }
                else
                {
                    _logger.LogWarning("No items with images found in collection {CollectionName}. Items: {Items}",
                        collection.Name,
                        string.Join(", ", items.Select(i => i.Name)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting image for collection {CollectionName}",
                    collection.Name);
            }
        }

        private async Task ExecuteSmartCollectionsForTag(string tag)
        {
            _logger.LogInformation($"Performing ExecuteSmartCollections for tag {tag}");
            var Name = GetCollectionName(tag);
            var collection = GetBoxSetByName(Name);
            if (collection is null)
            {
                _logger.LogInformation("{Name} not found, creating.", Name);
                collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = Name,
                    IsLocked = true
                });
                collection.Tags = new[] { "smartcollection" };
            }
            collection.DisplayOrder = "Default";

            var movies = GetMoviesFromLibrary(tag).ToList();
            var series = GetSeriesFromLibrary(tag).ToList();
            _logger.LogInformation($"Found {movies.Count} movies and {series.Count} series in library");
            var mediaItems = movies.Cast<BaseItem>().Concat(series.Cast<BaseItem>())
                .ToList();
            _logger.LogInformation($"Processing {mediaItems.Count} total media items");

            await RemoveUnwantedMediaItems(collection, mediaItems);
            await AddWantedMediaItems(collection, mediaItems);
            await SetPhotoForCollection(collection);
        }

        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public Task RunAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
