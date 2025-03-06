using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SmartCollections.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
            public PluginConfiguration()
            {
                Tags = new[]
                {
                    "christmas",
                    "halloween"
                };
            }

            public string[] Tags { get; set; }
    }
}
