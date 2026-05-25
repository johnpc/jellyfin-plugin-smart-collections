using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SmartCollections.Services;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="SmartCollectionsManager"/>.
    /// </summary>
    public class SmartCollectionsManagerTests : IDisposable
    {
        private readonly ISmartCollectionSyncService _syncService;
        private readonly SmartCollectionsManager _sut;

        public SmartCollectionsManagerTests()
        {
            _syncService = Substitute.For<ISmartCollectionSyncService>();
            _sut = new SmartCollectionsManager(_syncService);
        }

        [Fact]
        public async Task ExecuteSmartCollectionsNoProgress_DelegatesToSyncService()
        {
            // Arrange
            _syncService.ExecuteAsync().Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteSmartCollectionsNoProgress();

            // Assert
            await _syncService.Received(1).ExecuteAsync();
        }

        [Fact]
        public async Task ExecuteSmartCollections_DelegatesToSyncServiceWithProgress()
        {
            // Arrange
            var progress = Substitute.For<IProgress<double>>();
            _syncService.ExecuteAsync(progress, CancellationToken.None).Returns(Task.CompletedTask);

            // Act
            await _sut.ExecuteSmartCollections(progress, CancellationToken.None);

            // Assert
            await _syncService.Received(1).ExecuteAsync(progress, CancellationToken.None);
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
