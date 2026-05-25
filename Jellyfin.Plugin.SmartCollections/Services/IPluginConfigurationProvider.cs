using System.Collections.Generic;
using Jellyfin.Plugin.SmartCollections.Configuration;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Provides access to plugin configuration for smart collections.
    /// </summary>
    public interface IPluginConfigurationProvider
    {
        /// <summary>
        /// Gets the configured tag-title pairs.
        /// </summary>
        /// <returns>The list of tag-title pairs, or null if not configured.</returns>
        List<TagTitlePair>? GetTagTitlePairs();
    }
}
