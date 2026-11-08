using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Recognises decade terms on a collection's tag line and turns them into production-year ranges.
    /// Accepted forms: <c>1980s</c>, <c>80s</c> (30s-90s are the 1900s, 00s-20s the 2000s) and <c>1980-1989</c>.
    /// </summary>
    public static partial class DecadeTerm
    {
        private const int TwoDigitCenturyCutoff = 30;

        /// <summary>
        /// Tries to interpret a term as a decade or year range.
        /// </summary>
        /// <param name="term">The term from the tag line, e.g. <c>1980s</c>, <c>80s</c> or <c>1980-1989</c>.</param>
        /// <param name="years">Every production year covered by the term, or empty when it is not a decade term.</param>
        /// <returns><c>true</c> when the term is a decade or year range.</returns>
        public static bool TryParse(string term, out int[] years)
        {
            years = System.Array.Empty<int>();
            if (string.IsNullOrWhiteSpace(term))
            {
                return false;
            }

            var trimmed = term.Trim();

            var decade = DecadeRegex().Match(trimmed);
            if (decade.Success)
            {
                var start = int.Parse(decade.Groups[1].Value, CultureInfo.InvariantCulture) * 10;
                if (start < 100)
                {
                    start += start < TwoDigitCenturyCutoff ? 2000 : 1900;
                }

                years = Enumerable.Range(start, 10).ToArray();
                return true;
            }

            var range = YearRangeRegex().Match(trimmed);
            if (range.Success)
            {
                var first = int.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture);
                var last = int.Parse(range.Groups[2].Value, CultureInfo.InvariantCulture);
                if (first <= last)
                {
                    years = Enumerable.Range(first, last - first + 1).ToArray();
                    return true;
                }
            }

            return false;
        }

        [GeneratedRegex(@"^'?(\d{1,3})0s$", RegexOptions.IgnoreCase)]
        private static partial Regex DecadeRegex();

        [GeneratedRegex(@"^(\d{4})\s*-\s*(\d{4})$")]
        private static partial Regex YearRangeRegex();
    }
}
