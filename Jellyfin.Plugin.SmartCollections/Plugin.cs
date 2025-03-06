using System;
using System.Collections.Generic;
using Jellyfin.Plugin.SmartCollections.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Collections;

namespace Jellyfin.Plugin.SmartCollections
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;

        public Plugin(
            IServerApplicationPaths appPaths,
            IXmlSerializer xmlSerializer,
            ICollectionManager collectionManager,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
            _syncSmartCollectionsManager = new SmartCollectionsManager(
                collectionManager,
                libraryManager,
                loggerFactory.CreateLogger<SmartCollectionsManager>(),
                appPaths);
        }

        public override string Name => "Smart Collections";

        public static Plugin Instance { get; private set; }

        public override string Description
            => "Enables creation of Smart Collections based on Tag";

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
