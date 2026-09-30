using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartCollections.Configuration;
using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="SmartCollectionSyncService"/>.
    /// </summary>
    /// <remarks>
    /// Shares the "BaseItemStaticState" collection with other tests that assign the
    /// static <see cref="BaseItem.LibraryManager"/> property, so they never run in parallel.
    /// </remarks>
    [Collection("BaseItemStaticState")]
    public class SmartCollectionSyncServiceTests
    {
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILibraryQueryService _libraryQueryService;
        private readonly ICollectionImageService _collectionImageService;
        private readonly IPluginConfigurationProvider _configurationProvider;
        private readonly ILogger<SmartCollectionSyncService> _logger;
        private readonly SmartCollectionSyncService _sut;

        public SmartCollectionSyncServiceTests()
        {
            _collectionManager = Substitute.For<ICollectionManager>();
            _libraryManager = Substitute.For<ILibraryManager>();
            _libraryQueryService = Substitute.For<ILibraryQueryService>();
            _collectionImageService = Substitute.For<ICollectionImageService>();
            _configurationProvider = Substitute.For<IPluginConfigurationProvider>();
            _logger = Substitute.For<ILogger<SmartCollectionSyncService>>();
            _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

            // Folder.GetLinkedChildren resolves linked child ids through the static library manager.
            BaseItem.LibraryManager = _libraryManager;
            _sut = new SmartCollectionSyncService(
                _collectionManager,
                _libraryManager,
                _libraryQueryService,
                _collectionImageService,
                _configurationProvider,
                _logger);
        }

        [Fact]
        public async Task ExecuteAsync_WithProgress_Completes()
        {
            // Arrange
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair>());
            var progress = Substitute.For<IProgress<double>>();

            // Act
            await _sut.ExecuteAsync(progress, CancellationToken.None);

            // Assert - no exception thrown
        }

        [Fact]
        public async Task ExecuteAsync_NullConfig_SkipsSync()
        {
            // Arrange
            _configurationProvider.GetTagTitlePairs().Returns((List<TagTitlePair>?)null);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.DidNotReceive()
                .CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
        }

        [Fact]
        public async Task ExecuteAsync_EmptyConfig_SkipsSync()
        {
            // Arrange
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair>());

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.DidNotReceive()
                .CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
        }

        [Fact]
        public async Task ExecuteAsync_WithTag_OrMode_CreatesCollectionWithMatchingItems()
        {
            // Arrange
            var pair = new TagTitlePair("action", "Action Movies");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var movie = new Movie { Name = "Die Hard", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());

            // No existing collection found
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Action Movies");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
            await _collectionManager.Received(1).AddToCollectionAsync(boxSet.Id, Arg.Any<IReadOnlyList<Guid>>());
        }

        [Fact]
        public async Task ExecuteAsync_WithTag_AndMode_CreatesCollectionWithIntersection()
        {
            // Arrange
            var pair = new TagTitlePair("action,comedy", "Action Comedy", TagMatchingMode.And);
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var movie = new Movie { Name = "Rush Hour", Id = Guid.NewGuid() };
            _libraryQueryService.GetMoviesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2 && a[0] == "action" && a[1] == "comedy"),
                null)
                .Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeriesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2 && a[0] == "action" && a[1] == "comedy"),
                null)
                .Returns(new List<Series>());

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Action Comedy");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).AddToCollectionAsync(boxSet.Id, Arg.Any<IReadOnlyList<Guid>>());
        }

        [Fact]
        public async Task ExecuteAsync_ExistingCollection_DoesNotCreateNew()
        {
            // Arrange
            var pair = new TagTitlePair("action", "Action Movies");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var movie = new Movie { Name = "Die Hard", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());

            var existingBoxSet = CreateBoxSet("Action Movies");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { existingBoxSet });

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.DidNotReceive().CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
            await _collectionImageService.DidNotReceive().SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>());
        }

        [Fact]
        public async Task ExecuteAsync_ExistingCollectionFound_DoesNotSetImage()
        {
            // Arrange - existing collection means isNewCollection=false so no image set
            var pair = new TagTitlePair("action", "Action Movies");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());
            _libraryQueryService.FindPersonWithImage("action").Returns((Person?)null);

            var existingBoxSet = CreateBoxSet("Action Movies");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { existingBoxSet });

            // Act
            await _sut.ExecuteAsync();

            // Assert - no image set for existing collections
            await _collectionImageService.DidNotReceive().SetImageAsync(
                Arg.Any<BoxSet>(), Arg.Any<Person?>());
        }

        [Fact]
        public async Task ExecuteAsync_PersonFallback_OrMode_QueriesByPerson()
        {
            // Arrange
            var pair = new TagTitlePair("Tom Hanks", "Tom Hanks Collection");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            // No tag/genre matches
            _libraryQueryService.GetMovies("Tom Hanks", null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries("Tom Hanks", null).Returns(new List<Series>());

            // Person found
            var person = new Person
            {
                Name = "Tom Hanks",
                Id = Guid.NewGuid(),
                ImageInfos = new[] { new ItemImageInfo { Type = ImageType.Primary, Path = "/img.jpg" } },
            };
            _libraryQueryService.FindPersonWithImage("Tom Hanks").Returns(person);

            var movie = new Movie { Name = "Forrest Gump", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("Tom Hanks", person).Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeries("Tom Hanks", person).Returns(new List<Series>());

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Tom Hanks Collection");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).AddToCollectionAsync(boxSet.Id, Arg.Any<IReadOnlyList<Guid>>());
            await _collectionImageService.Received(1).SetImageAsync(boxSet, person);
        }

        [Fact]
        public async Task ExecuteAsync_PersonFallback_AndMode_QueriesByPerson()
        {
            // Arrange
            var pair = new TagTitlePair("Tom Hanks,comedy", "Tom Hanks Comedies", TagMatchingMode.And);
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            // No tag/genre matches with AND
            _libraryQueryService.GetMoviesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2), (Person?)null)
                .Returns(new List<Movie>());
            _libraryQueryService.GetSeriesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2), (Person?)null)
                .Returns(new List<Series>());

            // Person found for first tag
            var person = new Person
            {
                Name = "Tom Hanks",
                Id = Guid.NewGuid(),
                ImageInfos = new[] { new ItemImageInfo { Type = ImageType.Primary, Path = "/img.jpg" } },
            };
            _libraryQueryService.FindPersonWithImage("Tom Hanks").Returns(person);

            var movie = new Movie { Name = "The Money Pit", Id = Guid.NewGuid() };
            _libraryQueryService.GetMoviesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2), person)
                .Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeriesWithAndMatching(
                Arg.Is<string[]>(a => a.Length == 2), person)
                .Returns(new List<Series>());

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Tom Hanks Comedies");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).AddToCollectionAsync(boxSet.Id, Arg.Any<IReadOnlyList<Guid>>());
            await _collectionImageService.Received(1).SetImageAsync(boxSet, person);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyTag_SkipsCollection()
        {
            // Arrange
            var pair = new TagTitlePair { Tag = string.Empty, Title = "Empty" };
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Empty");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert - collection created but no items added because tags are empty
            await _collectionManager.DidNotReceive().AddToCollectionAsync(
                Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>());
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionInOneCollection_ContinuesProcessing()
        {
            // Arrange
            var pair1 = new TagTitlePair("bad-tag", "Bad");
            var pair2 = new TagTitlePair("good-tag", "Good");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair1, pair2 });

            // First collection lookup throws
            var callCount = 0;
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(callInfo =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new InvalidOperationException("Simulated failure");
                    }

                    return new List<BaseItem>();
                });

            _libraryQueryService.GetMovies("good-tag", null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries("good-tag", null).Returns(new List<Series>());
            _libraryQueryService.FindPersonWithImage("good-tag").Returns((Person?)null);

            var boxSet = CreateBoxSet("Good");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act - should not throw
            await _sut.ExecuteAsync();

            // Assert - second collection still processed (created)
            await _collectionManager.Received(1).CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
        }

        [Fact]
        public async Task ExecuteAsync_NoTitle_GeneratesDefaultName()
        {
            // Arrange
            var pair = new TagTitlePair("action") { Title = string.Empty };
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());
            _libraryQueryService.FindPersonWithImage("action").Returns((Person?)null);

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Action Smart Collection");
            _collectionManager.CreateCollectionAsync(Arg.Is<CollectionCreationOptions>(
                o => o.Name == "Action Smart Collection"))
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).CreateCollectionAsync(
                Arg.Is<CollectionCreationOptions>(o => o.Name == "Action Smart Collection"));
        }

        [Fact]
        public async Task ExecuteAsync_AndMode_MultiTag_GeneratesNameWithCount()
        {
            // Arrange
            var pair = new TagTitlePair("action,comedy", matchingMode: TagMatchingMode.And) { Title = string.Empty };
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            _libraryQueryService.GetMoviesWithAndMatching(Arg.Any<string[]>(), null)
                .Returns(new List<Movie>());
            _libraryQueryService.GetSeriesWithAndMatching(Arg.Any<string[]>(), null)
                .Returns(new List<Series>());
            _libraryQueryService.FindPersonWithImage("action").Returns((Person?)null);
            _libraryQueryService.FindPersonWithImage("comedy").Returns((Person?)null);

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Action + 1 more tags");
            _collectionManager.CreateCollectionAsync(Arg.Is<CollectionCreationOptions>(
                o => o.Name == "Action + 1 more tags"))
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).CreateCollectionAsync(
                Arg.Is<CollectionCreationOptions>(o => o.Name == "Action + 1 more tags"));
        }

        [Fact]
        public async Task ExecuteAsync_OrMode_MultipleTags_DeduplicatesItems()
        {
            // Arrange
            var pair = new TagTitlePair("action,thriller", "Action Thriller");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var sharedMovie = new Movie { Name = "Die Hard", Id = Guid.NewGuid() };
            var actionOnly = new Movie { Name = "Rocky", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie> { sharedMovie, actionOnly });
            _libraryQueryService.GetMovies("thriller", null).Returns(new List<Movie> { sharedMovie });
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());
            _libraryQueryService.GetSeries("thriller", null).Returns(new List<Series>());

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Action Thriller");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert - should add 2 unique items, not 3
            await _collectionManager.Received(1).AddToCollectionAsync(
                boxSet.Id,
                Arg.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2));
        }

        [Fact]
        public async Task ExecuteAsync_NoPersonFound_NoItemsAdded()
        {
            // Arrange
            var pair = new TagTitlePair("nonexistent", "Nothing Here");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            _libraryQueryService.GetMovies("nonexistent", null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries("nonexistent", null).Returns(new List<Series>());
            _libraryQueryService.FindPersonWithImage("nonexistent").Returns((Person?)null);

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Nothing Here");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.DidNotReceive().AddToCollectionAsync(
                Arg.Any<Guid>(), Arg.Any<IReadOnlyList<Guid>>());
        }

        [Fact]
        public async Task ExecuteAsync_WithSeriesItems_AddsSeriesAndMovies()
        {
            // Arrange
            var pair = new TagTitlePair("scifi", "Sci-Fi Collection");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var movie = new Movie { Name = "Interstellar", Id = Guid.NewGuid() };
            var series = new Series { Name = "The Expanse", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("scifi", null).Returns(new List<Movie> { movie });
            _libraryQueryService.GetSeries("scifi", null).Returns(new List<Series> { series });

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Sci-Fi Collection");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert - both movie and series should be added
            await _collectionManager.Received(1).AddToCollectionAsync(
                boxSet.Id,
                Arg.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2));
        }

        [Fact]
        public async Task ExecuteAsync_ExistingCollectionWithStaleItem_RemovesStaleItem()
        {
            // Arrange
            var pair = new TagTitlePair("action", "Action Movies");
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var wantedMovie = new Movie { Name = "Die Hard", Id = Guid.NewGuid() };
            var staleMovie = new Movie { Name = "Old Yeller", Id = Guid.NewGuid() };
            _libraryQueryService.GetMovies("action", null).Returns(new List<Movie> { wantedMovie });
            _libraryQueryService.GetSeries("action", null).Returns(new List<Series>());

            var boxSet = CreateBoxSet("Action Movies");
            boxSet.LinkedChildren = new[] { new LinkedChild { ItemId = staleMovie.Id } };

            // The by-name lookup finds the existing collection; the linked-child
            // resolution (by item ids) returns the stale member.
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Name != null))
                .Returns(new List<BaseItem> { boxSet });
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Name == null && q.ItemIds.Length > 0))
                .Returns(new List<BaseItem> { staleMovie });

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.DidNotReceive().CreateCollectionAsync(Arg.Any<CollectionCreationOptions>());
            await _collectionManager.Received(1).RemoveFromCollectionAsync(
                boxSet.Id,
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(staleMovie.Id)));
            await _collectionManager.Received(1).AddToCollectionAsync(
                boxSet.Id,
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(wantedMovie.Id)));
        }

        [Fact]
        public async Task ExecuteAsync_WithFilterTag_KeepsOnlyItemsMatchingAFilterTag()
        {
            // Arrange: documentary, filtered to music or band
            var pair = new TagTitlePair("documentary", "Music Docs") { FilterTag = "music, band" };
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var musicDoc = new Movie { Name = "Music Doc", Id = Guid.NewGuid() };
            var bandDoc = new Movie { Name = "Band Doc", Id = Guid.NewGuid() };
            var natureDoc = new Movie { Name = "Nature Doc", Id = Guid.NewGuid() };
            var concertFilm = new Movie { Name = "Concert Film", Id = Guid.NewGuid() };

            _libraryQueryService.GetMovies(Arg.Any<string>(), null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries(Arg.Any<string>(), null).Returns(new List<Series>());
            _libraryQueryService.GetMovies("documentary", null).Returns(new List<Movie> { musicDoc, bandDoc, natureDoc });
            _libraryQueryService.GetMovies("music", null).Returns(new List<Movie> { musicDoc, concertFilm });
            _libraryQueryService.GetMovies("band", null).Returns(new List<Movie> { bandDoc });

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Music Docs");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).AddToCollectionAsync(
                boxSet.Id,
                Arg.Is<IEnumerable<Guid>>(ids => ids.OrderBy(i => i).SequenceEqual(
                    new[] { musicDoc.Id, bandDoc.Id }.OrderBy(i => i))));
        }

        [Fact]
        public async Task ExecuteAsync_WithBlankFilterTag_IsIgnored()
        {
            // Arrange
            var pair = new TagTitlePair("sports, football", "Sports") { FilterTag = " , " };
            _configurationProvider.GetTagTitlePairs().Returns(new List<TagTitlePair> { pair });

            var sportsDoc = new Movie { Name = "Sports Doc", Id = Guid.NewGuid() };
            var footballMovie = new Movie { Name = "Football Movie", Id = Guid.NewGuid() };

            _libraryQueryService.GetMovies(Arg.Any<string>(), null).Returns(new List<Movie>());
            _libraryQueryService.GetSeries(Arg.Any<string>(), null).Returns(new List<Series>());
            _libraryQueryService.GetMovies("sports", null).Returns(new List<Movie> { sportsDoc });
            _libraryQueryService.GetMovies("football", null).Returns(new List<Movie> { footballMovie });

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            var boxSet = CreateBoxSet("Sports");
            _collectionManager.CreateCollectionAsync(Arg.Any<CollectionCreationOptions>())
                .Returns(boxSet);
            _libraryManager.UpdateItemAsync(
                Arg.Any<BaseItem>(), Arg.Any<BaseItem>(), Arg.Any<ItemUpdateType>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _collectionImageService.SetImageAsync(Arg.Any<BoxSet>(), Arg.Any<Person?>())
                .Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteAsync();

            // Assert
            await _collectionManager.Received(1).AddToCollectionAsync(
                boxSet.Id,
                Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 2));
        }

        private static BoxSet CreateBoxSet(string name)
        {
            return new BoxSet
            {
                Name = name,
                Id = Guid.NewGuid(),
                Tags = new[] { "smartcollection" },
            };
        }

    }
}
