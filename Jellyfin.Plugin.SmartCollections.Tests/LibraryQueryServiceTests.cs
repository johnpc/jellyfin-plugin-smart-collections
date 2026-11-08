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

            // First call returns both movies, second call returns only movieBoth
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(
                    new List<BaseItem> { movieBoth, movieFirst },
                    new List<BaseItem>(), // empty for genre query of first term
                    new List<BaseItem> { movieBoth },
                    new List<BaseItem>()); // empty for genre query of second term

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
                q.Genres.Count == 0));
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
        public void GetMovies_DecadeTerm_QueriesByProductionYearsOnly()
        {
            // Arrange
            var movie = CreateMovie("Back to the Future");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { movie });

            // Act
            var results = _sut.GetMovies("1980s").ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(movie);
            _libraryManager.Received(1).GetItemList(Arg.Any<InternalItemsQuery>());
            _libraryManager.Received(1).GetItemList(Arg.Is<InternalItemsQuery>(q =>
                q.IncludeItemTypes.Single() == BaseItemKind.Movie &&
                q.Years.SequenceEqual(Enumerable.Range(1980, 10)) &&
                q.Tags.Length == 0 &&
                q.Genres.Count == 0));
        }

        [Fact]
        public void GetSeries_ShortDecadeTerm_QueriesByProductionYears()
        {
            // Arrange
            var series = CreateSeries("Cheers");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { series });

            // Act
            var results = _sut.GetSeries("80s").ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(series);
            _libraryManager.Received(1).GetItemList(Arg.Is<InternalItemsQuery>(q =>
                q.IncludeItemTypes.Single() == BaseItemKind.Series &&
                q.Years.SequenceEqual(Enumerable.Range(1980, 10))));
        }

        [Fact]
        public void GetMovies_DecadeTerm_WithPerson_StillQueriesByPerson()
        {
            // Arrange
            var movie = CreateMovie("Big");
            var person = CreatePerson("Tom Hanks");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem> { movie });

            // Act
            var results = _sut.GetMovies("1980s", person).ToList();

            // Assert
            results.Should().Contain(movie);
            _libraryManager.DidNotReceive().GetItemList(Arg.Is<InternalItemsQuery>(q => q.Years.Length > 0));
        }

        [Fact]
        public void GetMoviesWithAndMatching_DecadeAndTag_ReturnsIntersection()
        {
            // Arrange
            var eightiesHorror = CreateMovie("The Thing");
            var eightiesComedy = CreateMovie("Ghostbusters");
            var modernHorror = CreateMovie("Hereditary");
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Years.Length > 0))
                .Returns(new List<BaseItem> { eightiesHorror, eightiesComedy });
            _libraryManager.GetItemList(Arg.Is<InternalItemsQuery>(q => q.Genres.Contains("horror")))
                .Returns(new List<BaseItem> { eightiesHorror, modernHorror });

            // Act
            var results = _sut.GetMoviesWithAndMatching(new[] { "1980s", "horror" }).ToList();

            // Assert
            results.Should().ContainSingle().Which.Should().Be(eightiesHorror);
        }

        [Fact]
        public void GetEpisodes_DecadeTerm_StillQueriesByTagOnly()
        {
            // Arrange
            _libraryManager.GetItemList(Arg.Any<InternalItemsQuery>())
                .Returns(new List<BaseItem>());

            // Act
            var results = _sut.GetEpisodes("1980s").ToList();

            // Assert
            results.Should().BeEmpty();
            _libraryManager.Received(1).GetItemList(Arg.Is<InternalItemsQuery>(q =>
                q.Tags.Single() == "1980s" &&
                q.Years.Length == 0));
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
