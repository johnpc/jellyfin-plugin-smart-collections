using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SmartCollections.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Orchestrates the creation and synchronization of smart collections.
    /// </summary>
    public class SmartCollectionSyncService : ISmartCollectionSyncService
    {
        private static readonly string[] SmartCollectionTag = new[] { "smartcollection" };

        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILibraryQueryService _libraryQueryService;
        private readonly ICollectionImageService _collectionImageService;
        private readonly IPluginConfigurationProvider _configurationProvider;
        private readonly ILogger<SmartCollectionSyncService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionSyncService"/> class.
        /// </summary>
        /// <param name="collectionManager">The collection manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="libraryQueryService">The library query service.</param>
        /// <param name="collectionImageService">The collection image service.</param>
        /// <param name="configurationProvider">The configuration provider.</param>
        /// <param name="logger">The logger.</param>
        public SmartCollectionSyncService(
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            ILibraryQueryService libraryQueryService,
            ICollectionImageService collectionImageService,
            IPluginConfigurationProvider configurationProvider,
            ILogger<SmartCollectionSyncService> logger)
        {
            _collectionManager = collectionManager;
            _libraryManager = libraryManager;
            _libraryQueryService = libraryQueryService;
            _collectionImageService = collectionImageService;
            _configurationProvider = configurationProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            await ExecuteAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Plugin must continue processing remaining collections on failure")]
        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Performing smart collection sync");
            var tagTitlePairs = _configurationProvider.GetTagTitlePairs();
            if (tagTitlePairs == null || tagTitlePairs.Count == 0)
            {
                _logger.LogInformation("No tag-title pairs configured, skipping sync");
                return;
            }

            foreach (var pair in tagTitlePairs)
            {
                try
                {
                    await SyncCollectionAsync(pair).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing smart collection for tag: {Tag}", pair.Tag);
                }
            }

            _logger.LogInformation("Completed smart collection sync");
        }

        private static string GetCollectionName(TagTitlePair tagTitlePair)
        {
            if (!string.IsNullOrWhiteSpace(tagTitlePair.Title))
            {
                return tagTitlePair.Title;
            }

            var tags = tagTitlePair.GetTagsArray();
            if (tags.Length == 0)
            {
                return "Smart Collection";
            }

            string firstTag = tags[0];
            string capitalized = firstTag.Length > 0
                ? char.ToUpper(firstTag[0], CultureInfo.InvariantCulture) + firstTag[1..]
                : firstTag;

            if (tagTitlePair.MatchingMode == TagMatchingMode.And && tags.Length > 1)
            {
                return $"{capitalized} + {tags.Length - 1} more tags";
            }

            return $"{capitalized} Smart Collection";
        }

        private async Task SyncCollectionAsync(TagTitlePair tagTitlePair)
        {
            var collectionName = GetCollectionName(tagTitlePair);
            var collection = GetBoxSetByName(collectionName);
            bool isNewCollection = collection == null;

            if (collection == null)
            {
                _logger.LogInformation("{Name} not found, creating.", collectionName);
                collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = collectionName,
                    IsLocked = true,
                }).ConfigureAwait(false);
                collection.Tags = SmartCollectionTag;
                await _libraryManager.UpdateItemAsync(
                    collection,
                    collection.GetParent(),
                    ItemUpdateType.MetadataEdit,
                    CancellationToken.None).ConfigureAwait(false);
            }

            var tags = tagTitlePair.GetTagsArray();
            if (tags.Length == 0)
            {
                _logger.LogWarning("No tags found for collection {CollectionName}", collectionName);
                return;
            }

            var (mediaItems, specificPerson) = ResolveMediaItems(tagTitlePair, tags);

            await RemoveUnwantedItemsAsync(collection, mediaItems).ConfigureAwait(false);
            await AddWantedItemsAsync(collection, mediaItems).ConfigureAwait(false);

            if (isNewCollection)
            {
                await _collectionImageService.SetImageAsync(collection, specificPerson).ConfigureAwait(false);
            }
        }

        private (List<BaseItem> MediaItems, Person? SpecificPerson) ResolveMediaItems(
            TagTitlePair tagTitlePair,
            string[] tags)
        {
            var allMovies = new List<Movie>();
            var allSeries = new List<Series>();

            if (tagTitlePair.MatchingMode == TagMatchingMode.And)
            {
                allMovies = _libraryQueryService.GetMoviesWithAndMatching(tags, null).ToList();
                allSeries = _libraryQueryService.GetSeriesWithAndMatching(tags, null).ToList();
            }
            else
            {
                foreach (var tag in tags)
                {
                    allMovies.AddRange(_libraryQueryService.GetMovies(tag, null));
                    allSeries.AddRange(_libraryQueryService.GetSeries(tag, null));
                }

                allMovies = allMovies.DistinctBy(m => m.Id).ToList();
                allSeries = allSeries.DistinctBy(s => s.Id).ToList();
            }

            Person? specificPerson = null;
            if (allMovies.Count == 0 && allSeries.Count == 0)
            {
                specificPerson = FindPersonForTags(tags);
                if (specificPerson != null)
                {
                    (allMovies, allSeries) = QueryByPerson(tagTitlePair, tags, specificPerson);
                }
            }

            var mediaItems = allMovies.Cast<BaseItem>().Concat(allSeries.Cast<BaseItem>()).ToList();
            return (mediaItems, specificPerson);
        }

        private Person? FindPersonForTags(string[] tags)
        {
            foreach (var tag in tags)
            {
                var person = _libraryQueryService.FindPersonWithImage(tag);
                if (person != null)
                {
                    return person;
                }
            }

            return null;
        }

        private (List<Movie>, List<Series>) QueryByPerson(
            TagTitlePair tagTitlePair,
            string[] tags,
            Person person)
        {
            List<Movie> movies;
            List<Series> series;

            if (tagTitlePair.MatchingMode == TagMatchingMode.And)
            {
                movies = _libraryQueryService.GetMoviesWithAndMatching(tags, person).ToList();
                series = _libraryQueryService.GetSeriesWithAndMatching(tags, person).ToList();
            }
            else
            {
                movies = new List<Movie>();
                series = new List<Series>();
                foreach (var tag in tags)
                {
                    movies.AddRange(_libraryQueryService.GetMovies(tag, person));
                    series.AddRange(_libraryQueryService.GetSeries(tag, person));
                }

                movies = movies.DistinctBy(m => m.Id).ToList();
                series = series.DistinctBy(s => s.Id).ToList();
            }

            return (movies, series);
        }

        private async Task RemoveUnwantedItemsAsync(BoxSet collection, List<BaseItem> wantedItems)
        {
            var wantedIds = wantedItems.Select(item => item.Id).ToHashSet();
            var toRemove = collection.GetLinkedChildren()
                .Where(item => !wantedIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToArray();

            if (toRemove.Length > 0)
            {
                _logger.LogInformation("Removing {Count} items from {Collection}", toRemove.Length, collection.Name);
                await _collectionManager.RemoveFromCollectionAsync(collection.Id, toRemove).ConfigureAwait(false);
            }
        }

        private async Task AddWantedItemsAsync(BoxSet collection, List<BaseItem> wantedItems)
        {
            var existingIds = collection.GetLinkedChildren().Select(item => item.Id).ToHashSet();
            var toAdd = wantedItems
                .Where(item => !existingIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToArray();

            if (toAdd.Length > 0)
            {
                _logger.LogInformation("Adding {Count} items to {Collection}", toAdd.Length, collection.Name);
                await _collectionManager.AddToCollectionAsync(collection.Id, toAdd).ConfigureAwait(false);
            }
        }

        private BoxSet? GetBoxSetByName(string name)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.BoxSet },
                CollapseBoxSetItems = false,
                Recursive = true,
                Tags = SmartCollectionTag,
                Name = name,
            }).OfType<BoxSet>().FirstOrDefault();
        }
    }
}
