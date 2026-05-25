using System.Collections.Generic;
using Jellyfin.Plugin.SmartCollections.Configuration;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Provides configuration from the Plugin singleton instance.
    /// </summary>
    public class PluginConfigurationProvider : IPluginConfigurationProvider
    {
        /// <inheritdoc />
        public List<TagTitlePair>? GetTagTitlePairs()
        {
            return Plugin.Instance?.Configuration.TagTitlePairs;
        }
    }
}
