using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartCollections.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.SmartCollections
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;

        public Plugin(
            IServerApplicationPaths appPaths,
            IXmlSerializer xmlSerializer,
            ICollectionManager collectionManager,
            IProviderManager providerManager,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
            _syncSmartCollectionsManager = new SmartCollectionsManager(
                providerManager,
                collectionManager,
                libraryManager,
                loggerFactory.CreateLogger<SmartCollectionsManager>(),
                appPaths);

            // Initialize configuration with defaults only on first run
            InitializeConfigurationIfNeeded();
        }

        private void InitializeConfigurationIfNeeded()
        {
            // Check if this is the first time the plugin is being loaded
            bool isInitialized = false;
            if (Configuration.TagTitlePairs != null && Configuration.TagTitlePairs.Count > 0)
            {
                isInitialized = true;
            }
            else if (Configuration.Tags != null && Configuration.Tags.Length > 0)
            {
                isInitialized = true;
            }

            // Only add default collections if this is the first time loading
            if (!isInitialized)
            {
                // Add default collections for first-time users
                Configuration.TagTitlePairs = new List<TagTitlePair>
                {
                    new TagTitlePair("christmas"),
                    new TagTitlePair("halloween"),
                    new TagTitlePair("japan"),
                    new TagTitlePair("based on novel or book"),
                    new TagTitlePair("revenge"),
                    new TagTitlePair("parody"),
                    new TagTitlePair("based on comic"),
                    new TagTitlePair("adult animation"),
                    new TagTitlePair("heist"),
                    new TagTitlePair("post-apocalyptic future"),
                    new TagTitlePair("reality"),
                    new TagTitlePair("mystery")
                };

                // For backward compatibility
                Configuration.Tags = Configuration.TagTitlePairs.ConvertAll(pair => pair.Tag).ToArray();

                // Save the configuration with defaults
                SaveConfiguration();
            }

            // Ensure backward compatibility when loading configuration
            EnsureBackwardCompatibility();
        }

        private void EnsureBackwardCompatibility()
        {
            // If we have Tags but no TagTitlePairs, convert Tags to TagTitlePairs
            if ((Configuration.TagTitlePairs == null || Configuration.TagTitlePairs.Count == 0) &&
                Configuration.Tags != null && Configuration.Tags.Length > 0)
            {
                Configuration.TagTitlePairs = Configuration.Tags
                    .Select(tag => new TagTitlePair(tag))
                    .ToList();

                // Save the updated configuration
                SaveConfiguration();
            }
        }

        public override string Name => "Smart Collections";

        public static Plugin Instance { get; private set; }

        public override string Description
            => "Enables creation of Smart Collections based on Tags with custom titles";

        private readonly Guid _id = new Guid("09612e52-0f93-41ab-a6ab-5a19479f5315");
        public override Guid Id => _id;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Smart Collections",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configurationpage.html"
                }
            };
        }
    }
}
