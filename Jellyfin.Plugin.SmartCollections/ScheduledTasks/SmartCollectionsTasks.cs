using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;

namespace Jellyfin.Plugin.SmartCollections.ScheduledTasks
{
    public class ExecuteSmartCollectionsTask : IScheduledTask
    {
        private readonly ILogger<SmartCollectionsManager> _logger;
        private readonly SmartCollectionsManager _syncSmartCollectionsManager;

        public ExecuteSmartCollectionsTask(ICollectionManager collectionManager, ILibraryManager libraryManager, ILogger<SmartCollectionsManager> logger, IApplicationPaths applicationPaths)
        {
            _logger = logger;
            _syncSmartCollectionsManager = new SmartCollectionsManager(collectionManager, libraryManager, logger, applicationPaths);
        }
        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.LogInformation("Starting plugin, executing Smart Collections...");
            _syncSmartCollectionsManager.ExecuteSmartCollections();
            _logger.LogInformation("All smart collections generated");
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Execute(cancellationToken, progress);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Run this task every 24 hours
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            };
        }

        public string Name => "Smart Collections";
        public string Key => "SmartCollections";
        public string Description => "Enables creation of Smart Collections based on Tags";
        public string Category => "Smart Collections";
    }
}
