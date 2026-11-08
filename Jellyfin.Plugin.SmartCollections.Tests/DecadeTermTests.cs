using System.Linq;
using FluentAssertions;
using Jellyfin.Plugin.SmartCollections.Services;
using Xunit;

namespace Jellyfin.Plugin.SmartCollections.Tests
{
    /// <summary>
    /// Tests for <see cref="DecadeTerm"/>.
    /// </summary>
    public class DecadeTermTests
    {
        [Theory]
        [InlineData("1980s", 1980, 1989)]
        [InlineData("2000s", 2000, 2009)]
        [InlineData(" 1990S ", 1990, 1999)]
        [InlineData("'80s", 1980, 1989)]
        [InlineData("80s", 1980, 1989)]
        [InlineData("30s", 1930, 1939)]
        [InlineData("20s", 2020, 2029)]
        [InlineData("10s", 2010, 2019)]
        [InlineData("00s", 2000, 2009)]
        [InlineData("1980-1989", 1980, 1989)]
        [InlineData("1995 - 2005", 1995, 2005)]
        [InlineData("2001-2001", 2001, 2001)]
        public void TryParse_DecadeTerm_ReturnsEveryYearInRange(string term, int first, int last)
        {
            // Act
            var parsed = DecadeTerm.TryParse(term, out var years);

            // Assert
            parsed.Should().BeTrue();
            years.Should().Equal(Enumerable.Range(first, last - first + 1));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("christmas")]
        [InlineData("1980")]
        [InlineData("1985s")]
        [InlineData("80")]
        [InlineData("8s")]
        [InlineData("1990-1980")]
        [InlineData("80-89")]
        [InlineData("1980s, 1990s")]
        [InlineData("the 80s")]
        public void TryParse_NonDecadeTerm_ReturnsFalseAndNoYears(string term)
        {
            // Act
            var parsed = DecadeTerm.TryParse(term, out var years);

            // Assert
            parsed.Should().BeFalse();
            years.Should().BeEmpty();
        }
    }
}
