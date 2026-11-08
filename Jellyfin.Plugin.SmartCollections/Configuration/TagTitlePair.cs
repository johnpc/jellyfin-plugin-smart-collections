using System;
using System.Globalization;
using System.Linq;

namespace Jellyfin.Plugin.SmartCollections.Configuration
{
    /// <summary>
    /// Represents a tag-title pair configuration for a smart collection.
    /// </summary>
    public class TagTitlePair
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagTitlePair"/> class.
        /// Parameterless constructor required for XML serialization.
        /// </summary>
        public TagTitlePair()
        {
            Tag = string.Empty;
            Title = "Smart Collection";
            MatchingMode = TagMatchingMode.Or;
            FilterTag = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagTitlePair"/> class.
        /// </summary>
        /// <param name="tag">The tag or comma-separated tags.</param>
        /// <param name="title">Optional custom title.</param>
        /// <param name="matchingMode">The matching mode for multiple tags.</param>
        public TagTitlePair(string tag, string? title = null, TagMatchingMode matchingMode = TagMatchingMode.Or)
        {
            Tag = tag;
            Title = title ?? GetDefaultTitle(tag);
            MatchingMode = matchingMode;
            FilterTag = string.Empty;
        }

        /// <summary>
        /// Gets or sets the tag or comma-separated tags.
        /// A tag prefixed with <c>!</c> (e.g. <c>!anime</c>) excludes items carrying it.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the custom title for the collection.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the matching mode for multiple tags.
        /// </summary>
        public TagMatchingMode MatchingMode { get; set; }

        /// <summary>
        /// Gets or sets optional comma-separated filter tags.
        /// When set, items must also match at least one of these tags.
        /// </summary>
        public string FilterTag { get; set; }

        /// <summary>
        /// Splits the comma-separated tag string into individual tags to include.
        /// Tags prefixed with <c>!</c> are exclusions and are not returned here.
        /// </summary>
        /// <returns>An array of trimmed, non-empty tag strings.</returns>
        public string[] GetTagsArray()
        {
            return SplitTags(Tag).Where(t => !IsExclusion(t)).ToArray();
        }

        /// <summary>
        /// Splits the comma-separated filter tag string into individual tags to include.
        /// Tags prefixed with <c>!</c> are exclusions and are not returned here.
        /// </summary>
        /// <returns>An array of trimmed, non-empty filter tag strings.</returns>
        public string[] GetFilterTagsArray()
        {
            return SplitTags(FilterTag).Where(t => !IsExclusion(t)).ToArray();
        }

        /// <summary>
        /// Gets the tags prefixed with <c>!</c> on either the tag line or the filter line.
        /// Items carrying any of these tags are removed from the collection.
        /// </summary>
        /// <returns>An array of trimmed, non-empty excluded tag strings without the <c>!</c> prefix.</returns>
        public string[] GetExcludedTagsArray()
        {
            return SplitTags(Tag)
                .Concat(SplitTags(FilterTag))
                .Where(IsExclusion)
                .Select(t => t[1..].Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        private static bool IsExclusion(string tag)
        {
            return tag.StartsWith('!');
        }

        /// <summary>
        /// Splits a comma-separated tag string into individual tags.
        /// </summary>
        /// <param name="tag">The comma-separated tag string.</param>
        /// <returns>An array of trimmed, non-empty tag strings.</returns>
        private static string[] SplitTags(string? tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return Array.Empty<string>();
            }

            return tag.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        private static string GetDefaultTitle(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return "Smart Collection";
            }

            // Title after the first included tag, skipping any !excluded ones
            string firstTag = tag.Split(',')
                .Select(t => t.Trim())
                .FirstOrDefault(t => !IsExclusion(t)) ?? string.Empty;
            return firstTag.Length > 0
                ? char.ToUpper(firstTag[0], CultureInfo.InvariantCulture) + firstTag[1..] + " Smart Collection"
                : "Smart Collection";
        }
    }
}
