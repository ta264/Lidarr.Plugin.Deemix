using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Blacklisting
{
    public class DeemixBlacklist : IBlacklistForProtocol
    {
        private readonly IBlacklistRepository _blacklistRepository;

        public DeemixBlacklist(IBlacklistRepository blacklistRepository)
        {
            _blacklistRepository = blacklistRepository;
        }

        public string Protocol => nameof(DeemixDownloadProtocol);

        public bool IsBlacklisted(int artistId, ReleaseInfo release)
        {
            var blacklistedByTorrentInfohash = _blacklistRepository.BlacklistedByTorrentInfoHash(artistId, release.Guid);
            return blacklistedByTorrentInfohash.Any(b => SameRelease(b, release));
        }

        public Blacklist GetBlacklist(DownloadFailedEvent message)
        {
            return new Blacklist
            {
                ArtistId = message.ArtistId,
                AlbumIds = message.AlbumIds,
                SourceTitle = message.SourceTitle,
                Quality = message.Quality,
                Date = DateTime.UtcNow,
                PublishedDate = DateTime.Parse(message.Data.GetValueOrDefault("publishedDate")),
                Size = long.Parse(message.Data.GetValueOrDefault("size", "0")),
                Indexer = message.Data.GetValueOrDefault("indexer"),
                Protocol = message.Data.GetValueOrDefault("protocol"),
                Message = message.Message,
                TorrentInfoHash = message.Data.GetValueOrDefault("guid")
            };
        }

        private bool SameRelease(Blacklist item, ReleaseInfo release)
        {
            if (release.Guid.IsNotNullOrWhiteSpace())
            {
                return release.Guid.Equals(item.TorrentInfoHash);
            }

            return item.Indexer.Equals(release.Indexer, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
