using System;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Deemix;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Deemix
{
    public class Deemix : HttpIndexerBase<DeemixIndexerSettings>
    {
        public override string Name => "Deemix";
        public override string Protocol => nameof(DeemixDownloadProtocol);
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 100;
        public override TimeSpan RateLimit => new TimeSpan(0);

        private readonly ICached<DeemixUser> _userCache;
        private readonly IDeemixProxy _deemixProxy;

        public Deemix(ICacheManager cacheManager,
            IDeemixProxy deemixProxy,
            IHttpClient httpClient,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _userCache = cacheManager.GetCache<DeemixUser>(typeof(DeemixProxy), "user");
            _deemixProxy = deemixProxy;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DeemixRequestGenerator()
            {
                Settings = Settings,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            _deemixProxy.Authenticate(Settings);

            return new DeemixParser()
            {
                User = _userCache.Find(Settings.BaseUrl)
            };
        }
    }
}
