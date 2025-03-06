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

namespace Jellyfin.Plugin.SmartCollections

{


    public class SmartCollectionsManager : IDisposable
    {
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly Timer _timer;
        private readonly ILogger<SmartCollectionsManager> _logger;
        private readonly string _pluginDirectory;

        public SmartCollectionsManager(ICollectionManager collectionManager, ILibraryManager libraryManager, ILogger<SmartCollectionsManager> logger, IApplicationPaths applicationPaths)
        {
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
            _pluginDirectory = Path.Combine(applicationPaths.DataPath, "smartcollections");
            Directory.CreateDirectory(_pluginDirectory);
        }

        private IEnumerable<Series> GetSeriesFromLibrary(string tag)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = true,
                Tags = [tag]
            }).Select(m => m as Series);
        }

        private IEnumerable<Movie> GetMoviesFromLibrary(string tag)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
                IsVirtualItem = false,
                Recursive = true,
                HasTvdbId = false,
                Tags = [tag]
            }).Select(m => m as Movie);
        }

        private void RemoveUnwantedMediaItems(BoxSet collection, IEnumerable<BaseItem> wantedMediaItems)
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
                _collectionManager.RemoveFromCollectionAsync(collection.Id, childrenToRemove).ConfigureAwait(true);
            }
        }

        private void AddWantedMediaItems(BoxSet collection, IEnumerable<BaseItem> wantedMediaItems)
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
                _collectionManager.AddToCollectionAsync(collection.Id, childrenToAdd).ConfigureAwait(true);
            }
        }

        private BoxSet GetOrCreateCollection(string tag)
        {
            // Create the collection name by appending "Smart Collection"
            string collectionName = $"{tag} Smart Collection";

            // Search for existing collection
            var existingCollection = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                Name = collectionName
            }).FirstOrDefault() as BoxSet;

            // If collection exists, return it
            if (existingCollection != null)
            {
                _logger.LogInformation($"Found existing collection: {collectionName}");
                return existingCollection;
            }

            // Collection doesn't exist, create new one
            _logger.LogInformation($"Creating new collection: {collectionName}");
            var collection = new BoxSet
            {
                Name = collectionName,
                // Set the collection's path within the plugin's directory
                Path = Path.Combine(_pluginDirectory, collectionName),
            };

            // Save the new collection to the library
            _libraryManager.CreateItem(collection, null);

            return collection;
        }

        public void ExecuteSmartCollections()
        {
            _logger.LogInformation("Performing ExecuteSmartCollections");
            // Define the list of tags to create smart collections for
            var tags = new List<string>
            {
                "christmas",
                // Add more tags here as needed
                // "action",
                // "comedy",
                // "drama"
            };

            _logger.LogInformation($"Starting execution of smart collections for {tags.Count} tags");

            foreach (var tag in tags)
            {
                try
                {
                    _logger.LogInformation($"Processing smart collection for tag: {tag}");
                    ExecuteSmartCollectionsForTag(tag);
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

        private void ExecuteSmartCollectionsForTag(string tag)
        {
            _logger.LogInformation($"Performing ExecuteSmartCollections for tag {tag}");
            var collection = GetOrCreateCollection(tag);
            var movies = GetMoviesFromLibrary(tag).ToList();
            var series = GetSeriesFromLibrary(tag).ToList();
            _logger.LogInformation($"Found {movies.Count} movies and {series.Count} series in library");
            var mediaItems = movies.Cast<BaseItem>().Concat(series.Cast<BaseItem>())
                .Take(1)
                .ToList();
            _logger.LogInformation($"Processing {mediaItems.Count} total media items");

            RemoveUnwantedMediaItems(collection, mediaItems);
            AddWantedMediaItems(collection, mediaItems);
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
