using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.SmartCollections.Services
{
    /// <summary>
    /// Queries the Jellyfin library for movies and series by tag, genre, or person.
    /// </summary>
    public class LibraryQueryService : ILibraryQueryService
    {
        private static readonly string[] ActorPersonType = new[] { "Actor" };
        private static readonly string[] DirectorPersonType = new[] { "Director" };

        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryQueryService"/> class.
        /// </summary>
        /// <param name="libraryManager">The Jellyfin library manager.</param>
        public LibraryQueryService(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public IEnumerable<Movie> GetMovies(string term, Person? specificPerson = null)
        {
            if (specificPerson != null)
            {
                return GetItemsByPerson<Movie>(BaseItemKind.Movie, specificPerson.Name);
            }

            return GetItemsByTagOrGenre<Movie>(BaseItemKind.Movie, term);
        }

        /// <inheritdoc />
        public IEnumerable<Series> GetSeries(string term, Person? specificPerson = null)
        {
            if (specificPerson != null)
            {
                return GetItemsByPerson<Series>(BaseItemKind.Series, specificPerson.Name);
            }

            return GetItemsByTagOrGenre<Series>(BaseItemKind.Series, term);
        }

        /// <inheritdoc />
        public IEnumerable<Movie> GetMoviesWithAndMatching(string[] terms, Person? specificPerson = null)
        {
            return GetItemsWithAndMatching<Movie>(terms, specificPerson, GetMovies);
        }

        /// <inheritdoc />
        public IEnumerable<Series> GetSeriesWithAndMatching(string[] terms, Person? specificPerson = null)
        {
            return GetItemsWithAndMatching<Series>(terms, specificPerson, GetSeries);
        }

        /// <inheritdoc />
        public Person? FindPersonWithImage(string name)
        {
            var personQuery = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Person },
                Name = name,
            };

            return _libraryManager.GetItemList(personQuery)
                .FirstOrDefault(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    p.ImageInfos != null &&
                    p.ImageInfos.Any(i => i.Type == ImageType.Primary)) as Person;
        }

        private static IEnumerable<T> GetItemsWithAndMatching<T>(
            string[] terms,
            Person? specificPerson,
            Func<string, Person?, IEnumerable<T>> getter)
            where T : BaseItem
        {
            if (terms.Length == 0)
            {
                return Enumerable.Empty<T>();
            }

            var results = getter(terms[0], specificPerson).ToList();

            for (int i = 1; i < terms.Length && results.Count > 0; i++)
            {
                var matchingIds = getter(terms[i], specificPerson)
                    .Select(m => m.Id)
                    .ToHashSet();
                results = results.Where(item => matchingIds.Contains(item.Id)).ToList();
            }

            return results;
        }

        private IEnumerable<T> GetItemsByTagOrGenre<T>(BaseItemKind kind, string term)
            where T : BaseItem
        {
            var byTags = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Tags = new[] { term },
            }).OfType<T>();

            var byGenres = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Genres = new[] { term },
            }).OfType<T>();

            return byTags.Union(byGenres);
        }

        private IEnumerable<T> GetItemsByPerson<T>(BaseItemKind kind, string personName)
            where T : BaseItem
        {
            var byActors = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Person = personName,
                PersonTypes = ActorPersonType,
            }).OfType<T>();

            var byDirectors = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Person = personName,
                PersonTypes = DirectorPersonType,
            }).OfType<T>();

            return byActors.Union(byDirectors);
        }
    }
}
