using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Service for managing collection images.
    /// </summary>
    public interface ICollectionImageService
    {
        /// <summary>
        /// Sets the primary image for a collection.
        /// </summary>
        /// <param name="collection">The collection to set the image on.</param>
        /// <param name="specificPerson">Optional person whose image to use.</param>
        /// <returns>A task representing the async operation.</returns>
        Task SetImageAsync(BoxSet collection, Person? specificPerson = null);
    }
}
