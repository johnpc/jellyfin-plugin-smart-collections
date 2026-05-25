using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Service responsible for synchronizing smart collections.
    /// </summary>
    public interface ISmartCollectionSyncService
    {
        /// <summary>
        /// Executes the smart collection sync process.
        /// </summary>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task ExecuteAsync(System.IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the smart collection sync process without progress reporting.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        Task ExecuteAsync();
    }
}
