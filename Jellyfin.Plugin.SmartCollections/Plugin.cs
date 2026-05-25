using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SmartCollections.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartCollections
{
    /// <summary>
    /// The Smart Collections plugin entry point.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IDisposable
    {
        private readonly Guid _id = new Guid("09612e52-0f93-41ab-a6ab-5a19479f5315");
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="appPaths">Application paths.</param>
        /// <param name="xmlSerializer">XML serializer.</param>
        /// <param name="collectionManager">Collection manager.</param>
        /// <param name="providerManager">Provider manager.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="loggerFactory">Logger factory.</param>
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

            InitializeConfigurationIfNeeded();
        }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "Smart Collections";

        /// <inheritdoc />
        public override string Description
            => "Enables creation of Smart Collections based on Tags with custom titles";

        /// <inheritdoc />
        public override Guid Id => _id;

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Smart Collections",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configurationpage.html",
                },
            };
        }

        /// <summary>
        /// Disposes the plugin resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed resources.
        /// </summary>
        /// <param name="disposing">Whether called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _syncSmartCollectionsManager?.Dispose();
                }

                _disposed = true;
            }
        }

        private void InitializeConfigurationIfNeeded()
        {
            bool isInitialized = (Configuration.TagTitlePairs != null && Configuration.TagTitlePairs.Count > 0)
                || (Configuration.Tags != null && Configuration.Tags.Length > 0);

            if (!isInitialized)
            {
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
                    new TagTitlePair("mystery"),
                };

                Configuration.Tags = Configuration.TagTitlePairs.ConvertAll(pair => pair.Tag).ToArray();
                SaveConfiguration();
            }

            EnsureBackwardCompatibility();
        }

        private void EnsureBackwardCompatibility()
        {
            if ((Configuration.TagTitlePairs == null || Configuration.TagTitlePairs.Count == 0) &&
                Configuration.Tags != null && Configuration.Tags.Length > 0)
            {
                Configuration.TagTitlePairs = Configuration.Tags
                    .Select(tag => new TagTitlePair(tag))
                    .ToList();
                SaveConfiguration();
            }
        }
    }
}
