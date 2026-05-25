using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.Services;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="PluginConfigurationProvider"/>.
    /// </summary>
    public class PluginConfigurationProviderTests
    {
        [Fact]
        public void GetTagTitlePairs_WhenPluginInstanceNull_ReturnsNull()
        {
            // Arrange
            var provider = new PluginConfigurationProvider();

            // Act
            var result = provider.GetTagTitlePairs();

            // Assert - Plugin.Instance is null in test context
            result.Should().BeNull();
        }
    }
}
