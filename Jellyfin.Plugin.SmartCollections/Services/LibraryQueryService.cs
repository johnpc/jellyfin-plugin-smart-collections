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
    /// Queries the Jellyfin library for movies, series, and episodes by tag, genre, studio, decade, or person.
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

            return GetItemsByTerm<Movie>(BaseItemKind.Movie, term);
        }

        /// <inheritdoc />
        public IEnumerable<Series> GetSeries(string term, Person? specificPerson = null)
        {
            if (specificPerson != null)
            {
                return GetItemsByPerson<Series>(BaseItemKind.Series, specificPerson.Name);
            }

            return GetItemsByTerm<Series>(BaseItemKind.Series, term);
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
        public IEnumerable<Episode> GetEpisodes(string term)
        {
            return GetItemsByTag<Episode>(BaseItemKind.Episode, term);
        }

        /// <inheritdoc />
        public IEnumerable<Episode> GetEpisodesWithAndMatching(string[] terms)
        {
            return GetItemsWithAndMatching<Episode>(terms, null, (term, _) => GetEpisodes(term));
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

            // Only the term naming the person is resolved by person; every other
            // term must still match by tag/genre/studio, otherwise AND degrades to "all of this person's items".
            Person? PersonFor(string term) =>
                specificPerson != null && term.Equals(specificPerson.Name, StringComparison.OrdinalIgnoreCase)
                    ? specificPerson
                    : null;

            var results = getter(terms[0], PersonFor(terms[0])).ToList();

            for (int i = 1; i < terms.Length && results.Count > 0; i++)
            {
                var matchingIds = getter(terms[i], PersonFor(terms[i]))
                    .Select(m => m.Id)
                    .ToHashSet();
                results = results.Where(item => matchingIds.Contains(item.Id)).ToList();
            }

            return results;
        }

        private IEnumerable<T> GetItemsByTag<T>(BaseItemKind kind, string term)
            where T : BaseItem
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Tags = new[] { term },
            }).OfType<T>();
        }

        private IEnumerable<T> GetItemsByTerm<T>(BaseItemKind kind, string term)
            where T : BaseItem
        {
            // A decade term (e.g. "1980s") matches by production year instead of tag/genre/studio.
            // Episodes deliberately skip this and keep matching on tags only.
            if (DecadeTerm.TryParse(term, out var years))
            {
                return _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { kind },
                    IsVirtualItem = false,
                    Recursive = true,
                    Years = years,
                }).OfType<T>();
            }

            var byTags = GetItemsByTag<T>(kind, term);

            var byGenres = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                Genres = new[] { term },
            }).OfType<T>();

            // A term such as "Netflix" or "HBO" also matches items whose Studios contain it,
            // so users don't have to hand-tag every show by its network.
            var byStudios = GetItemsByStudio<T>(kind, term);

            return byTags.Union(byGenres).Union(byStudios);
        }

        private IEnumerable<T> GetItemsByStudio<T>(BaseItemKind kind, string studioName)
            where T : BaseItem
        {
            var studio = FindStudio(studioName);
            if (studio == null)
            {
                return Enumerable.Empty<T>();
            }

            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { kind },
                IsVirtualItem = false,
                Recursive = true,
                StudioIds = new[] { studio.Id },
            }).OfType<T>();
        }

        private Studio? FindStudio(string name)
        {
            // Resolve the studio through a query (not ILibraryManager.GetStudio, which creates the
            // studio when it is missing) so an unknown term never leaves stray studio items behind.
            var studioQuery = new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Studio },
                Name = name,
            };

            return _libraryManager.GetItemList(studioQuery)
                .OfType<Studio>()
                .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
