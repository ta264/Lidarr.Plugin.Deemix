using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class DeemixRequestGenerator : IIndexerRequestGenerator
    {
        private const int PageSize = 100;
        private const int MaxPages = 30;
        public DeemixIndexerSettings Settings { get; set; }
        public Logger Logger { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            // The /api/newReleases endpoint in deemix crashes the deemix process
            // when any album in the feed is unavailable on Deezer (GWAPIError:
            // "Track unavailable on Deezer"). This unhandled Promise rejection
            // kills the deemix Node process entirely, causing it to restart.
            //
            // Since Lidarr discovers releases via album search rather than the
            // recent releases feed, returning an empty chain here has no impact
            // on normal operation and prevents the crash during indexer testing.
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests($"artist:\"{searchCriteria.ArtistQuery}\" album:\"{searchCriteria.AlbumQuery}\""));
            chain.AddTier(GetRequests($"{searchCriteria.ArtistQuery} {searchCriteria.AlbumQuery}"));

            return chain;
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            var chain = new IndexerPageableRequestChain();

            chain.AddTier(GetRequests($"artist:\"{searchCriteria.ArtistQuery}\""));
            chain.AddTier(GetRequests(searchCriteria.ArtistQuery));

            return chain;
        }

        private IEnumerable<IndexerRequest> GetRequests(string searchParameters)
        {
            for (var page = 0; page < MaxPages; page++)
            {
                var url = $"{Settings.BaseUrl.TrimEnd('/')}/api/album-search?term={searchParameters}&nb={PageSize}&start={page * PageSize}";
                yield return new IndexerRequest(url, HttpAccept.Json);
            }
        }
    }
}
