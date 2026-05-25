using System;
using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.Configuration;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="PluginConfiguration"/> and <see cref="TagTitlePair"/>.
    /// </summary>
    public class PluginConfigurationTests
    {
        [Fact]
        public void TagTitlePair_DefaultConstructor_HasDefaults()
        {
            // Act
            var pair = new TagTitlePair();

            // Assert
            pair.Tag.Should().Be(string.Empty);
            pair.Title.Should().Be("Smart Collection");
            pair.MatchingMode.Should().Be(TagMatchingMode.Or);
        }

        [Fact]
        public void TagTitlePair_WithTag_GeneratesDefaultTitle()
        {
            // Act
            var pair = new TagTitlePair("christmas");

            // Assert
            pair.Tag.Should().Be("christmas");
            pair.Title.Should().Be("Christmas Smart Collection");
            pair.MatchingMode.Should().Be(TagMatchingMode.Or);
        }

        [Fact]
        public void TagTitlePair_WithCustomTitle_UsesCustomTitle()
        {
            // Act
            var pair = new TagTitlePair("holiday", "My Holiday Collection");

            // Assert
            pair.Title.Should().Be("My Holiday Collection");
        }

        [Fact]
        public void TagTitlePair_WithAndMode_SetsCorrectMode()
        {
            // Act
            var pair = new TagTitlePair("action", null, TagMatchingMode.And);

            // Assert
            pair.MatchingMode.Should().Be(TagMatchingMode.And);
        }

        [Fact]
        public void GetTagsArray_EmptyTag_ReturnsEmptyArray()
        {
            // Arrange
            var pair = new TagTitlePair();

            // Act
            var result = pair.GetTagsArray();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetTagsArray_SingleTag_ReturnsSingleElement()
        {
            // Arrange
            var pair = new TagTitlePair("action");

            // Act
            var result = pair.GetTagsArray();

            // Assert
            result.Should().HaveCount(1);
            result[0].Should().Be("action");
        }

        [Fact]
        public void GetTagsArray_MultipleTags_ReturnsAllTrimmed()
        {
            // Arrange
            var pair = new TagTitlePair("action, comedy , drama");

            // Act
            var result = pair.GetTagsArray();

            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainInOrder("action", "comedy", "drama");
        }

        [Fact]
        public void GetTagsArray_EmptySegments_AreFiltered()
        {
            // Arrange
            var pair = new TagTitlePair("action,,comedy");

            // Act
            var result = pair.GetTagsArray();

            // Assert
            result.Should().HaveCount(2);
            result.Should().ContainInOrder("action", "comedy");
        }

        [Fact]
        public void PluginConfiguration_DefaultConstructor_InitializesEmptyCollections()
        {
            // Act
            var config = new PluginConfiguration();

            // Assert
            config.TagTitlePairs.Should().NotBeNull();
            config.TagTitlePairs.Should().BeEmpty();
            config.Tags.Should().NotBeNull();
            config.Tags.Should().BeEmpty();
        }

        [Fact]
        public void TagMatchingMode_Or_IsZero()
        {
            ((int)TagMatchingMode.Or).Should().Be(0);
        }

        [Fact]
        public void TagMatchingMode_And_IsOne()
        {
            ((int)TagMatchingMode.And).Should().Be(1);
        }
    }
}
