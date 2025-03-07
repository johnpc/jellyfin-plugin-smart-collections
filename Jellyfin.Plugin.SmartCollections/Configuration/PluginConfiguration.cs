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
                    "halloween",
                    "japan",
                    "based on novel or book",
                    "revenge",
                    "parody",
                    "based on comic",
                    "adult animation",
                    "heist",
                    "post-apocalyptic future",
                    "reality",
                    "mystery"
                };
            }

            public string[] Tags { get; set; }
    }
}
