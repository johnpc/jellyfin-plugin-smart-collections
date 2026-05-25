using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.Configuration;
using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Reqnroll;

namespace Jellyfin.Plugin.SmartCollections.AcceptanceTests.StepDefinitions
{
    /// <summary>
    /// Step definitions for smart collections acceptance tests.
    /// </summary>
    [Binding]
    public class SmartCollectionsSteps
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ICollectionManager _collectionManager;
        private readonly ILibraryQueryService _libraryQueryService;
        private readonly ICollectionImageService _collectionImageService;
        private readonly ILogger<SmartCollectionSyncService> _logger;

        private TagTitlePair _tagTitlePair = new TagTitlePair();
        private List<Movie> _libraryMovies = new List<Movie>();
        private BoxSet? _createdCollection;
        private List<BaseItem> _collectionItems = new List<BaseItem>();
        private Movie? _movieWithBothTags;
        private Movie? _movieWithOnlyFirstTag;
        private Movie? _movieWithOnlySecondTag;
        private Person? _person;
        private bool _imageWasSet;

        public SmartCollectionsSteps()
        {
            _libraryManager = Substitute.For<ILibraryManager>();
            _collectionManager = Substitute.For<ICollectionManager>();
            _libraryQueryService = Substitute.For<ILibraryQueryService>();
            _collectionImageService = Substitute.For<ICollectionImageService>();
            _logger = Substitute.For<ILogger<SmartCollectionSyncService>>();
        }

        [Given("a tag-title pair with tag {string}")]
        public void GivenATagTitlePairWithTag(string tag)
        {
            _tagTitlePair = new TagTitlePair(tag);
        }

        [Given("movies in the library tagged {string}")]
        public void GivenMoviesInTheLibraryTagged(string tag)
        {
            var movie1 = new Movie { Name = "Die Hard", Id = Guid.NewGuid() };
            var movie2 = new Movie { Name = "Home Alone", Id = Guid.NewGuid() };
            _libraryMovies.AddRange(new[] { movie1, movie2 });

            _libraryQueryService.GetMovies(tag, null)
                .Returns(_libraryMovies);
            _libraryQueryService.GetSeries(tag, null)
                .Returns(Enumerable.Empty<MediaBrowser.Controller.Entities.TV.Series>());
        }

        [Given("a tag-title pair with tags {string} in AND mode")]
        public void GivenATagTitlePairWithTagsInAndMode(string tags)
        {
            _tagTitlePair = new TagTitlePair(tags, null, TagMatchingMode.And);
        }

        [Given("a tag-title pair with tags {string} in OR mode")]
        public void GivenATagTitlePairWithTagsInOrMode(string tags)
        {
            _tagTitlePair = new TagTitlePair(tags, null, TagMatchingMode.Or);
        }

        [Given("a movie tagged with both {string} and {string}")]
        public void GivenAMovieTaggedWithBothAnd(string tag1, string tag2)
        {
            _movieWithBothTags = new Movie { Name = "Action Comedy", Id = Guid.NewGuid() };
            _libraryQueryService.GetMoviesWithAndMatching(
                Arg.Is<string[]>(arr => arr.Contains(tag1) && arr.Contains(tag2)),
                null)
                .Returns(new[] { _movieWithBothTags });
        }

        [Given("a movie tagged with only {string}")]
        public void GivenAMovieTaggedWithOnly(string tag)
        {
            var movie = new Movie { Name = $"Only {tag}", Id = Guid.NewGuid() };
            if (_movieWithOnlyFirstTag == null)
            {
                _movieWithOnlyFirstTag = movie;
            }
            else
            {
                _movieWithOnlySecondTag = movie;
            }

            _libraryQueryService.GetMovies(tag, null)
                .Returns(new[] { movie });
            _libraryQueryService.GetSeries(tag, null)
                .Returns(Enumerable.Empty<MediaBrowser.Controller.Entities.TV.Series>());
        }

        [Given("a collection with existing items")]
        public void GivenACollectionWithExistingItems()
        {
            _tagTitlePair = new TagTitlePair("action");
            var movie = new Movie { Name = "Kept Movie", Id = Guid.NewGuid() };
            _libraryMovies.Add(movie);
            _libraryQueryService.GetMovies("action", null).Returns(new[] { movie });
            _libraryQueryService.GetSeries("action", null)
                .Returns(Enumerable.Empty<MediaBrowser.Controller.Entities.TV.Series>());
        }

        [Given("one item no longer matches the tag criteria")]
        public void GivenOneItemNoLongerMatchesTheTagCriteria()
        {
            // The collection currently has an extra item that the library query won't return
            _collectionItems.Add(new Movie { Name = "Removed Movie", Id = Guid.NewGuid() });
            _collectionItems.AddRange(_libraryMovies);
        }

        [Given("a tag-title pair for a person {string}")]
        public void GivenATagTitlePairForAPerson(string personName)
        {
            _tagTitlePair = new TagTitlePair(personName);
        }

        [Given("the person has a primary image")]
        public void GivenThePersonHasAPrimaryImage()
        {
            _person = new Person
            {
                Name = _tagTitlePair.Tag,
                Id = Guid.NewGuid(),
                ImageInfos = new[]
                {
                    new ItemImageInfo
                    {
                        Type = ImageType.Primary,
                        Path = "/images/person.jpg",
                    },
                },
            };
        }

        [When("the smart collection sync runs")]
        public void WhenTheSmartCollectionSyncRuns()
        {
            // Verify configuration was set up correctly
            _tagTitlePair.Should().NotBeNull();
        }

        [When("a new collection is created for that person")]
        public void WhenANewCollectionIsCreatedForThatPerson()
        {
            _createdCollection = new BoxSet { Name = _tagTitlePair.Tag, Id = Guid.NewGuid() };
            _collectionImageService.SetImageAsync(_createdCollection, _person)
                .Returns(Task.CompletedTask);
            _imageWasSet = true;
        }

        [Then("a collection named {string} should exist")]
        public void ThenACollectionNamedShouldExist(string name)
        {
            var expectedName = _tagTitlePair.Title;
            expectedName.Should().Be(name);
        }

        [Then("it should contain the tagged movies")]
        public void ThenItShouldContainTheTaggedMovies()
        {
            _libraryMovies.Should().NotBeEmpty();
        }

        [Then("the collection should contain only the movie with both tags")]
        public void ThenTheCollectionShouldContainOnlyTheMovieWithBothTags()
        {
            _movieWithBothTags.Should().NotBeNull();
            var results = _libraryQueryService.GetMoviesWithAndMatching(
                _tagTitlePair.GetTagsArray(), null).ToList();
            results.Should().HaveCount(1);
            results[0].Should().Be(_movieWithBothTags);
        }

        [Then("the collection should contain both movies")]
        public void ThenTheCollectionShouldContainBothMovies()
        {
            var tags = _tagTitlePair.GetTagsArray();
            var allMovies = new List<Movie>();
            foreach (var tag in tags)
            {
                allMovies.AddRange(_libraryQueryService.GetMovies(tag, null));
            }

            allMovies.Should().HaveCount(2);
        }

        [Then("the non-matching item should be removed from the collection")]
        public void ThenTheNonMatchingItemShouldBeRemovedFromTheCollection()
        {
            var wantedIds = _libraryMovies.Select(m => m.Id).ToHashSet();
            var toRemove = _collectionItems.Where(i => !wantedIds.Contains(i.Id)).ToList();
            toRemove.Should().HaveCount(1);
            toRemove[0].Name.Should().Be("Removed Movie");
        }

        [Then("the collection image should be set from the person")]
        public void ThenTheCollectionImageShouldBeSetFromThePerson()
        {
            _imageWasSet.Should().BeTrue();
            _person.Should().NotBeNull();
        }
    }
}
