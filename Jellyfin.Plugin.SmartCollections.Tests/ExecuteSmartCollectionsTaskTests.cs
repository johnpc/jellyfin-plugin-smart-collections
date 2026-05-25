using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.ScheduledTasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="ExecuteSmartCollectionsTask"/>.
    /// </summary>
    public class ExecuteSmartCollectionsTaskTests : IDisposable
    {
        private readonly ExecuteSmartCollectionsTask _sut;

        public ExecuteSmartCollectionsTaskTests()
        {
            var providerManager = Substitute.For<IProviderManager>();
            var collectionManager = Substitute.For<ICollectionManager>();
            var libraryManager = Substitute.For<ILibraryManager>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            loggerFactory.CreateLogger(Arg.Any<string>())
                .Returns(Substitute.For<ILogger>());
            var applicationPaths = Substitute.For<IApplicationPaths>();

            _sut = new ExecuteSmartCollectionsTask(
                providerManager,
                collectionManager,
                libraryManager,
                loggerFactory,
                applicationPaths);
        }

        [Fact]
        public void Name_ReturnsExpectedValue()
        {
            _sut.Name.Should().Be("Smart Collections");
        }

        [Fact]
        public void Key_ReturnsExpectedValue()
        {
            _sut.Key.Should().Be("SmartCollections");
        }

        [Fact]
        public void Description_ReturnsExpectedValue()
        {
            _sut.Description.Should().Be("Enables creation of Smart Collections based on Tags");
        }

        [Fact]
        public void Category_ReturnsExpectedValue()
        {
            _sut.Category.Should().Be("Smart Collections");
        }

        [Fact]
        public async Task ExecuteAsync_CompletesWithoutError()
        {
            // Arrange
            var progress = Substitute.For<IProgress<double>>();

            // Act - will complete quickly since there's no Plugin.Instance configured
            await _sut.ExecuteAsync(progress, CancellationToken.None);

            // Assert - no exception
        }

        [Fact]
        public void GetDefaultTriggers_Returns24HourInterval()
        {
            // Act
            var triggers = _sut.GetDefaultTriggers().ToList();

            // Assert
            triggers.Should().HaveCount(1);
            triggers[0].IntervalTicks.Should().Be(TimeSpan.FromHours(24).Ticks);
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
