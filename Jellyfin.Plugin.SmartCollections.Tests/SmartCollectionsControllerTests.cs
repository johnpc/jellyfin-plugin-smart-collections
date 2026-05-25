using System;
using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="SmartCollectionsController"/>.
    /// </summary>
    public class SmartCollectionsControllerTests : IDisposable
    {
        private readonly SmartCollectionsController _sut;

        public SmartCollectionsControllerTests()
        {
            var providerManager = Substitute.For<IProviderManager>();
            var collectionManager = Substitute.For<ICollectionManager>();
            var libraryManager = Substitute.For<ILibraryManager>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            loggerFactory.CreateLogger(Arg.Any<string>())
                .Returns(Substitute.For<ILogger>());
            var applicationPaths = Substitute.For<IApplicationPaths>();

            _sut = new SmartCollectionsController(
                providerManager,
                collectionManager,
                libraryManager,
                loggerFactory,
                applicationPaths);
        }

        [Fact]
        public void SmartCollectionsRequest_Returns204NoContent()
        {
            // Act
            var result = _sut.SmartCollectionsRequest();

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act & Assert - no exception
            _sut.Dispose();
            _sut.Dispose();
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}
