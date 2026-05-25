using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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
        public void GetSeriesWithAndMatching_EmptyTerms_ReturnsEmpty()
        {
            // Act
            var results = _sut.GetSeriesWithAndMatching(Array.Empty<string>()).ToList();

            // Assert
            results.Should().BeEmpty();
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
