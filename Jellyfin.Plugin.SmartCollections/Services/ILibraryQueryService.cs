using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Service for querying the Jellyfin library for media items.
    /// </summary>
    public interface ILibraryQueryService
    {
        /// <summary>
        /// Gets movies matching a single term (tag/genre or person).
        /// </summary>
        /// <param name="term">The tag, genre, or person name to search.</param>
        /// <param name="specificPerson">Optional person to search by actor/director.</param>
        /// <returns>Matching movies.</returns>
        IEnumerable<Movie> GetMovies(string term, Person? specificPerson = null);

        /// <summary>
        /// Gets series matching a single term (tag/genre or person).
        /// </summary>
        /// <param name="term">The tag, genre, or person name to search.</param>
        /// <param name="specificPerson">Optional person to search by actor/director.</param>
        /// <returns>Matching series.</returns>
        IEnumerable<Series> GetSeries(string term, Person? specificPerson = null);

        /// <summary>
        /// Gets movies matching ALL given terms (AND logic).
        /// </summary>
        /// <param name="terms">Tags/genres that must all match.</param>
        /// <param name="specificPerson">Optional person filter.</param>
        /// <returns>Movies matching all terms.</returns>
        IEnumerable<Movie> GetMoviesWithAndMatching(string[] terms, Person? specificPerson = null);

        /// <summary>
        /// Gets series matching ALL given terms (AND logic).
        /// </summary>
        /// <param name="terms">Tags/genres that must all match.</param>
        /// <param name="specificPerson">Optional person filter.</param>
        /// <returns>Series matching all terms.</returns>
        IEnumerable<Series> GetSeriesWithAndMatching(string[] terms, Person? specificPerson = null);

        /// <summary>
        /// Finds a person by name that has a primary image.
        /// </summary>
        /// <param name="name">The person name to search.</param>
        /// <returns>The person if found, otherwise null.</returns>
        Person? FindPersonWithImage(string name);
    }
}
