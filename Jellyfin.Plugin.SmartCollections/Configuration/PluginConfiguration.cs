using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SmartCollections.Configuration
{
    /// <summary>
    /// Plugin configuration for Smart Collections.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            TagTitlePairs = new List<TagTitlePair>();
            Tags = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the list of tag-title pairs defining smart collections.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Change collection properties to read only", Justification = "Required for XML serialization")]
        public List<TagTitlePair> TagTitlePairs { get; set; }

        /// <summary>
        /// Gets or sets the legacy tags array (kept for backward compatibility).
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Required for XML serialization backward compatibility")]
        public string[] Tags { get; set; }
    }
}
