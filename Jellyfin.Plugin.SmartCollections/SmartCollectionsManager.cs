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
using Jellyfin.Plugin.SmartCollections.Configuration;

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

        private IEnumerable<Series> GetSeriesFromLibrary(string term, Person? specificPerson = null)
        {
            IEnumerable<Series> results = Enumerable.Empty<Series>();
            
            if (specificPerson == null)
            {
                // When no specific person is provided, search by tags and genres
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
                
                results = byTags.Union(byGenres);
            }
            else
            {
                // When a specific person is provided, search by actor and director
                var personName = specificPerson.Name;
                
                var byActors = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Series },
                    IsVirtualItem = false,
                    Recursive = true,
                    HasTvdbId = true,
                    Person = personName,
                    PersonTypes = new[] { "Actor" }
                }).Select(m => m as Series);

                var byDirectors = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Series },
                    IsVirtualItem = false,
                    Recursive = true,
                    HasTvdbId = true,
                    Person = personName,
                    PersonTypes = new[] { "Director" }
                }).Select(m => m as Series);
                
                results = byActors.Union(byDirectors);
            }

            return results;
        }
        
        private IEnumerable<Series> GetSeriesFromLibraryWithAndMatching(string[] terms, Person? specificPerson = null)
        {
            if (terms.Length == 0)
                return Enumerable.Empty<Series>();
                
            // Start with all series matching the first tag
            var results = GetSeriesFromLibrary(terms[0], specificPerson).ToList();
            
            // For each additional tag, filter the results to only include series that also match that tag
            for (int i = 1; i < terms.Length && results.Any(); i++)
            {
                var matchingItems = GetSeriesFromLibrary(terms[i], specificPerson).ToList();
                results = results.Where(item => matchingItems.Any(m => m.Id == item.Id)).ToList();
            }
            
            return results;
        }

        private IEnumerable<Movie> GetMoviesFromLibrary(string term, Person? specificPerson = null)
        {
            IEnumerable<Movie> results = Enumerable.Empty<Movie>();
            
            if (specificPerson == null)
            {
                // When no specific person is provided, search by tags and genres
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
                
                results = byTags.Union(byGenres);
            }
            else
            {
                // When a specific person is provided, search by actor and director
                var personName = specificPerson.Name;
                
                var byActors = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    IsVirtualItem = false,
                    Recursive = true,
                    HasTvdbId = false,
                    Person = personName,
                    PersonTypes = new[] { "Actor" }
                }).Select(m => m as Movie);

                var byDirectors = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie },
                    IsVirtualItem = false,
                    Recursive = true,
                    HasTvdbId = false,
                    Person = personName,
                    PersonTypes = new[] { "Director" }
                }).Select(m => m as Movie);
                
                results = byActors.Union(byDirectors);
            }

            return results;
        }
        
        private IEnumerable<Movie> GetMoviesFromLibraryWithAndMatching(string[] terms, Person? specificPerson = null)
        {
            if (terms.Length == 0)
                return Enumerable.Empty<Movie>();
                
            // Start with all movies matching the first tag
            var results = GetMoviesFromLibrary(terms[0], specificPerson).ToList();
            
            // For each additional tag, filter the results to only include movies that also match that tag
            for (int i = 1; i < terms.Length && results.Any(); i++)
            {
                var matchingItems = GetMoviesFromLibrary(terms[i], specificPerson).ToList();
                results = results.Where(item => matchingItems.Any(m => m.Id == item.Id)).ToList();
            }
            
            return results;
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
            // Get tag-title pairs from configuration
            var tagTitlePairs = Plugin.Instance!.Configuration.TagTitlePairs;

            _logger.LogInformation($"Starting execution of smart collections for {tagTitlePairs.Count} tag-title pairs");

            foreach (var tagTitlePair in tagTitlePairs)
            {
                try
                {
                    _logger.LogInformation($"Processing smart collection for tag: {tagTitlePair.Tag}");
                    await ExecuteSmartCollectionsForTagTitlePair(tagTitlePair);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing smart collection for tag: {tagTitlePair.Tag}");
                    // Continue with next tag-title pair even if one fails
                    continue;
                }
            }

            _logger.LogInformation("Completed execution of all smart collections");
        }

        public async Task ExecuteSmartCollections(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await ExecuteSmartCollectionsNoProgress();
        }

        private string GetCollectionName(TagTitlePair tagTitlePair)
        {
            // If a custom title is set, use it
            if (!string.IsNullOrWhiteSpace(tagTitlePair.Title))
            {
                return tagTitlePair.Title;
            }
            
            // Otherwise use the default format based on the first tag
            string[] tags = tagTitlePair.GetTagsArray();
            if (tags.Length == 0)
                return "Smart Collection";
                
            string firstTag = tags[0];
            string capitalizedTag = firstTag.Length > 0
                ? char.ToUpper(firstTag[0]) + firstTag[1..]
                : firstTag;

            // For AND matching, use a different format to indicate the intersection
            if (tagTitlePair.MatchingMode == TagMatchingMode.And && tags.Length > 1)
            {
                return $"{capitalizedTag} + {tags.Length - 1} more tags";
            }

            return $"{capitalizedTag} Smart Collection";
        }

        private async Task SetPhotoForCollection(BoxSet collection, Person? specificPerson = null)
        {
            try
            {
                // First attempt: Use the specific person if provided
                if (specificPerson != null && specificPerson.ImageInfos != null)
                {
                    var personImageInfo = specificPerson.ImageInfos
                        .FirstOrDefault(i => i.Type == ImageType.Primary);

                    if (personImageInfo != null)
                    {
                        // Set the image path directly
                        collection.SetImage(new ItemImageInfo
                        {
                            Path = personImageInfo.Path,
                            Type = ImageType.Primary
                        }, 0);

                        await _libraryManager.UpdateItemAsync(
                            collection,
                            collection.GetParent(),
                            ItemUpdateType.ImageUpdate,
                            CancellationToken.None);
                        _logger.LogInformation("Successfully set image for collection {CollectionName} from specified person {PersonName}",
                            collection.Name, specificPerson.Name);

                        return; // We're done if we used the specified person's image
                    }
                }

                // Second attempt: Try to determine the collection type and set appropriate image

                // Get the collection's items to determine its nature
                var query = new InternalItemsQuery
                {
                    Recursive = true
                };

                var items = collection.GetItems(query)
                    .Items
                    .ToList();

                _logger.LogDebug("Found {Count} items in collection {CollectionName}",
                    items.Count, collection.Name);

                // If no specific person was provided, but collection name suggests it's for a person,
                // try to find that person
                if (specificPerson == null)
                {
                    string term = collection.Name;

                    // Check if this collection might be for a person (actor or director)
                    var personQuery = new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Person },
                        Name = term
                    };

                    var person = _libraryManager.GetItemList(personQuery)
                        .FirstOrDefault(p =>
                            p.Name.Equals(term, StringComparison.OrdinalIgnoreCase) &&
                            p.ImageInfos != null &&
                            p.ImageInfos.Any(i => i.Type == ImageType.Primary)) as Person;

                    // If we found a person with an image, use their image
                    if (person != null && person.ImageInfos != null)
                    {
                        var personImageInfo = person.ImageInfos
                            .FirstOrDefault(i => i.Type == ImageType.Primary);

                        if (personImageInfo != null)
                        {
                            // Set the image path directly
                            collection.SetImage(new ItemImageInfo
                            {
                                Path = personImageInfo.Path,
                                Type = ImageType.Primary
                            }, 0);

                            await _libraryManager.UpdateItemAsync(
                                collection,
                                collection.GetParent(),
                                ItemUpdateType.ImageUpdate,
                                CancellationToken.None);
                            _logger.LogInformation("Successfully set image for collection {CollectionName} from detected person {PersonName}",
                                collection.Name, person.Name);

                            return; // We're done if we found a person image
                        }
                    }
                }

                // Last fallback: Use an image from a movie/series in the collection
                var mediaItemWithImage = items
                    .Where(item => item is Movie || item is Series)
                    .FirstOrDefault(item =>
                        item.ImageInfos != null &&
                        item.ImageInfos.Any(i => i.Type == ImageType.Primary));

                if (mediaItemWithImage != null)
                {
                    var imageInfo = mediaItemWithImage.ImageInfos
                        .First(i => i.Type == ImageType.Primary);

                    // Set the image path directly
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
                        collection.Name, mediaItemWithImage.Name);
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

        private async Task ExecuteSmartCollectionsForTagTitlePair(TagTitlePair tagTitlePair)
        {
            _logger.LogInformation($"Performing ExecuteSmartCollections for tag: {tagTitlePair.Tag}");
            
            // Get the collection name from the tag-title pair
            var collectionName = GetCollectionName(tagTitlePair);
            
            // Get or create the collection
            var collection = GetBoxSetByName(collectionName);
            if (collection is null)
            {
                _logger.LogInformation("{Name} not found, creating.", collectionName);
                collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = collectionName,
                    IsLocked = true
                });
                collection.Tags = new[] { "smartcollection" };
            }
            collection.DisplayOrder = "Default";

            // Get all tags from the tag-title pair
            string[] tags = tagTitlePair.GetTagsArray();
            if (tags.Length == 0)
            {
                _logger.LogWarning("No tags found in tag-title pair for collection {CollectionName}", collectionName);
                return;
            }

            // Check if any tag might correspond to a person
            Person? specificPerson = null;
            foreach (var tag in tags)
            {
                var personQuery = new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Person },
                    Name = tag
                };

                specificPerson = _libraryManager.GetItemList(personQuery)
                    .FirstOrDefault(p =>
                        p.Name.Equals(tag, StringComparison.OrdinalIgnoreCase) &&
                        p.ImageInfos != null &&
                        p.ImageInfos.Any(i => i.Type == ImageType.Primary)) as Person;
                
                if (specificPerson != null)
                {
                    _logger.LogInformation("Found specific person {PersonName} matching tag {Tag}",
                        specificPerson.Name, tag);
                    break;
                }
            }

            // Collect all media items based on the matching mode
            var allMovies = new List<Movie>();
            var allSeries = new List<Series>();
            
            if (tagTitlePair.MatchingMode == TagMatchingMode.And)
            {
                // AND matching - items must match all tags
                _logger.LogInformation("Using AND matching mode for tags: {Tags}", string.Join(", ", tags));
                allMovies = GetMoviesFromLibraryWithAndMatching(tags, specificPerson).ToList();
                allSeries = GetSeriesFromLibraryWithAndMatching(tags, specificPerson).ToList();
            }
            else
            {
                // OR matching (default) - items can match any tag
                _logger.LogInformation("Using OR matching mode for tags: {Tags}", string.Join(", ", tags));
                foreach (var tag in tags)
                {
                    var movies = GetMoviesFromLibrary(tag, specificPerson).ToList();
                    var series = GetSeriesFromLibrary(tag, specificPerson).ToList();
                    
                    _logger.LogInformation($"Found {movies.Count} movies and {series.Count} series for tag: {tag}");
                    
                    allMovies.AddRange(movies);
                    allSeries.AddRange(series);
                }
                
                // Remove duplicates
                allMovies = allMovies.Distinct().ToList();
                allSeries = allSeries.Distinct().ToList();
            }
            
            _logger.LogInformation($"Processing {allMovies.Count} movies and {allSeries.Count} series total for collection: {collectionName}");
            
            var mediaItems = allMovies.Cast<BaseItem>().Concat(allSeries.Cast<BaseItem>())
                .ToList();

            await RemoveUnwantedMediaItems(collection, mediaItems);
            await AddWantedMediaItems(collection, mediaItems);
            await SetPhotoForCollection(collection, specificPerson);
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
