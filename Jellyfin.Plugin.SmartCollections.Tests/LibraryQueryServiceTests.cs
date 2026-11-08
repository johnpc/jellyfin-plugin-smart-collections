using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SmartCollections.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="LibraryQueryService"/>.
    /// </summary>
    public class LibraryQueryServiceTests
    {
        private readonly ILibraryManager _libraryManager;
        private readonly LibraryQueryService _sut;

        public LibraryQueryServiceTests()
        {
            _libraryManager = Substitute.For<ILibraryManager>();
            _sut = new LibraryQueryService(_libraryManager);
        }

        [Fact]
        public void GetMovies_ByTag_ReturnsMatchingMovies()
        {
            // Arrange
            var movie = CreateMovie("Test Movie");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { movie });

            // Act
            var results = _sut.GetMovies("action").ToList();

            // Assert
            results.Should().Contain(movie);
        }

        [Fact]
        public void GetMovies_ByPerson_ReturnsMatchingMovies()
        {
            // Arrange
            var movie = CreateMovie("Test Movie");
            var person = CreatePerson("Tom Hanks");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { movie });

            // Act
            var results = _sut.GetMovies("Tom Hanks", person).ToList();

            // Assert
            results.Should().Contain(movie);
        }

        [Fact]
        public void GetSeries_ByTag_ReturnsMatchingSeries()
        {
            // Arrange
            var series = CreateSeries("Test Series");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { series });

            // Act
            var results = _sut.GetSeries("drama").ToList();

            // Assert
            results.Should().Contain(series);
        }

        [Fact]
        public void GetSeries_ByPerson_ReturnsMatchingSeries()
        {
            // Arrange
            var series = CreateSeries("Test Series");
            var person = CreatePerson("Bryan Cranston");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { series });

            // Act
            var results = _sut.GetSeries("Bryan Cranston", person).ToList();

            // Assert
            results.Should().Contain(series);
        }

        [Fact]
        public void GetMovies_ByStudio_ReturnsMoviesFromThatStudio()
        {
            // Arrange
            var netflix = CreateStudio("Netflix");
            var netflixMovie = CreateMovie("Netflix Original");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            ArrangeStudioLookup(netflix);
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.StudioIds.Contains(netflix.Id)))
                .Returns(new List<BaseItem> { netflixMovie });

            // Act
            var results = _sut.GetMovies("Netflix").ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(netflixMovie);
            _libraryManager.Received(1).GetItemList(Arg.Is<InternalItemsQuery>(q =>
                q.IncludeItemTypes.Single() == BaseItemKind.Movie &&
                q.StudioIds.Contains(netflix.Id) &&
                q.Tags.Length == 0 &&
                q.Genres.Count == 0));
        }

        [Fact]
        public void GetSeries_ByStudio_ReturnsSeriesFromThatStudio()
        {
            // Arrange
            var hbo = CreateStudio("HBO");
            var hboSeries = CreateSeries("HBO Drama");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            ArrangeStudioLookup(hbo);
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.StudioIds.Contains(hbo.Id)))
                .Returns(new List<BaseItem> { hboSeries });

            // Act
            var results = _sut.GetSeries("hbo").ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(hboSeries);
        }

        [Fact]
        public void GetMovies_StudioLookupReturnsDifferentName_DoesNotQueryByStudio()
        {
            // Arrange
            var otherStudio = CreateStudio("Netflix Animation");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            ArrangeStudioLookup(otherStudio);

            // Act
            var results = _sut.GetMovies("Netflix").ToList();

            // Assert
            results.Should().BeEmpty();
            _libraryManager.DidNotReceive().GetItemList(Arg.Is<InternalItemsQuery>(q => q.StudioIds.Any()));
        }

        [Fact]
        public void GetMovies_TagAndStudioBothMatch_ReturnsDistinctUnion()
        {
            // Arrange
            var netflix = CreateStudio("Netflix");
            var taggedMovie = CreateMovie("Tagged Movie");
            var studioMovie = CreateMovie("Studio Movie");
            var bothMovie = CreateMovie("Tagged Studio Movie");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Tags.Contains("Netflix")))
                .Returns(new List<BaseItem> { taggedMovie, bothMovie });
            ArrangeStudioLookup(netflix);
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.StudioIds.Contains(netflix.Id)))
                .Returns(new List<BaseItem> { studioMovie, bothMovie });

            // Act
            var results = _sut.GetMovies("Netflix").ToList();

            // Assert
            results.Should().BeEquivalentTo(new[] { taggedMovie, bothMovie, studioMovie });
        }

        [Fact]
        public void GetMoviesWithAndMatching_StudioAndGenre_ReturnsIntersection()
        {
            // Arrange
            var netflix = CreateStudio("Netflix");
            var netflixComedy = CreateMovie("Netflix Comedy");
            var netflixDrama = CreateMovie("Netflix Drama");
            var otherComedy = CreateMovie("Other Comedy");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            ArrangeStudioLookup(netflix);
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.StudioIds.Contains(netflix.Id)))
                .Returns(new List<BaseItem> { netflixComedy, netflixDrama });
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Genres.Contains("comedy")))
                .Returns(new List<BaseItem> { netflixComedy, otherComedy });

            // Act
            var results = _sut.GetMoviesWithAndMatching(new[] { "Netflix", "comedy" }).ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(netflixComedy);
        }

        [Fact]
        public void GetMoviesWithAndMatching_EmptyTerms_ReturnsEmpty()
        {
            // Act
            var results = _sut.GetMoviesWithAndMatching(Array.Empty<string>()).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void GetMoviesWithAndMatching_MultipleTerms_ReturnsIntersection()
        {
            // Arrange
            var movieBoth = CreateMovie("Movie Both");
            var movieFirst = CreateMovie("Movie First Only");

            // Each term issues a tag query, a genre query, then a studio lookup (no studio => no further query).
            // First term's tag query returns both movies, second term's returns only movieBoth
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(
                    new List<BaseItem> { movieBoth, movieFirst },
                    new List<BaseItem>(), // empty for genre query of first term
                    new List<BaseItem>(), // no studio named after first term
                    new List<BaseItem> { movieBoth },
                    new List<BaseItem>(), // empty for genre query of second term
                    new List<BaseItem>()); // no studio named after second term

            // Act
            var results = _sut.GetMoviesWithAndMatching(new[] { "action", "comedy" }).ToList();

            // Assert
            results.Should().Contain(movieBoth);
            results.Should().NotContain(movieFirst);
        }

        [Fact]
        public void GetMoviesWithAndMatching_WithPerson_StillRequiresOtherTerms()
        {
            // Arrange
            var person = CreatePerson("Tom Hanks");
            var comedy = CreateMovie("The Money Pit");
            var drama = CreateMovie("Philadelphia");

            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Person == "Tom Hanks"))
                .Returns(new List<BaseItem> { comedy, drama });
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Tags.Contains("comedy")))
                .Returns(new List<BaseItem> { comedy });

            // Act
            var results = _sut.GetMoviesWithAndMatching(new[] { "tom hanks", "comedy" }, person).ToList();

            // Assert
            results.Should().Contain(comedy);
            results.Should().NotContain(drama);
        }

        [Fact]
        public void GetSeriesWithAndMatching_EmptyTerms_ReturnsEmpty()
        {
            // Act
            var results = _sut.GetSeriesWithAndMatching(Array.Empty<string>()).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void GetEpisodes_ByTag_QueriesEpisodesByTagOnly()
        {
            // Arrange
            var episode = CreateEpisode("A Very Special Christmas");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { episode });

            // Act
            var results = _sut.GetEpisodes("christmas").ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(episode);
            _libraryManager.Received(1).GetItemList(Arg.Any<InternalItemsQuery>());
            _libraryManager.Received(1).GetItemList(Arg.Is<InternalItemsQuery>(q =>
                q.IncludeItemTypes.Single() == BaseItemKind.Episode &&
                q.Tags.Single() == "christmas" &&
                q.Genres.Count == 0 &&
                !q.StudioIds.Any()));
        }

        [Fact]
        public void GetEpisodesWithAndMatching_EmptyTerms_ReturnsEmpty()
        {
            // Act
            var results = _sut.GetEpisodesWithAndMatching(Array.Empty<string>()).ToList();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public void GetEpisodesWithAndMatching_MultipleTerms_ReturnsIntersection()
        {
            // Arrange
            var episodeBoth = CreateEpisode("Episode Both");
            var episodeFirst = CreateEpisode("Episode First Only");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(
                    new List<BaseItem> { episodeBoth, episodeFirst },
                    new List<BaseItem> { episodeBoth });

            // Act
            var results = _sut.GetEpisodesWithAndMatching(new[] { "christmas", "comedy" }).ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(episodeBoth);
        }

        [Fact]
        public void FindPersonWithImage_PersonExists_ReturnsPerson()
        {
            // Arrange
            var person = CreatePerson("Tom Hanks");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { person });

            // Act
            var result = _sut.FindPersonWithImage("Tom Hanks");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Tom Hanks");
        }

        [Fact]
        public void FindPersonWithImage_PersonNotFound_ReturnsNull()
        {
            // Arrange
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            // Act
            var result = _sut.FindPersonWithImage("Nobody");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetMovies_NoResults_ReturnsEmpty()
        {
            // Arrange
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            // Act
            var results = _sut.GetMovies("nonexistent").ToList();

            // Assert
            results.Should().BeEmpty();
        }

        private static Movie CreateMovie(string name)
        {
            return new Movie
            {
                Name = name,
                Id = Guid.NewGuid(),
            };
        }

        private static Series CreateSeries(string name)
        {
            return new Series
            {
                Name = name,
                Id = Guid.NewGuid(),
            };
        }

        private static Episode CreateEpisode(string name)
        {
            return new Episode
            {
                Name = name,
                Id = Guid.NewGuid(),
            };
        }

        private void ArrangeStudioLookup(Studio studio)
        {
            // Jellyfin's Name filter is case-insensitive, so the lookup mock is too.
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q =>
                    q.IncludeItemTypes.Contains(BaseItemKind.Studio) &&
                    string.Equals(q.Name, studio.Name, StringComparison.OrdinalIgnoreCase)))
                .Returns(new List<BaseItem> { studio });
        }

        private static Studio CreateStudio(string name)
        {
            return new Studio
            {
                Name = name,
                Id = Guid.NewGuid(),
            };
        }

        private static Person CreatePerson(string name)
        {
            return new Person
            {
                Name = name,
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
    }
}
