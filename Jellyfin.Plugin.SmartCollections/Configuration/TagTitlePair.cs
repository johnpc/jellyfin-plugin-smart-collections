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
        }

        /// <summary>
        /// Gets or sets the tag or comma-separated tags.
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
        /// Splits the comma-separated tag string into individual tags.
        /// </summary>
        /// <returns>An array of trimmed, non-empty tag strings.</returns>
        public string[] GetTagsArray()
        {
            if (string.IsNullOrEmpty(Tag))
            {
                return Array.Empty<string>();
            }

            return Tag.Split(',')
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

            string firstTag = tag.Split(',')[0].Trim();
            return firstTag.Length > 0
                ? char.ToUpper(firstTag[0], CultureInfo.InvariantCulture) + firstTag[1..] + " Smart Collection"
                : "Smart Collection";
        }
    }
}
