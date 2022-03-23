using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Deemix;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public interface IDeemixProxy
    {
        DeemixConfig GetSettings(DeemixSettings settings);
        List<DownloadClientItem> GetQueue(DeemixSettings settings);
        string Download(string url, int bitrate, DeemixSettings settings);
        void RemoveFromQueue(string downloadId, DeemixSettings settings);
        public void Authenticate(DeemixSettings settings);
        public void Authenticate(DeemixIndexerSettings settings);
    }

    public class DeemixProxy : IDeemixProxy
    {
        private static readonly Dictionary<string, long> Bitrates = new Dictionary<string, long>
        {
            { "1", 128 },
            { "3", 320 },
            { "9", 1000 }
        };
        private static readonly Dictionary<string, string> Formats = new Dictionary<string, string>
        {
            { "1", "MP3 128" },
            { "3", "MP3 320" },
            { "9", "FLAC" }
        };

        private readonly ICached<string> _sessionCookieCache;
        private readonly ICached<DeemixUser> _userCache;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public DeemixProxy(ICacheManager cacheManager,
            IHttpClient httpClient,
            Logger logger)
        {
            _sessionCookieCache = cacheManager.GetCache<string>(GetType(), "sessionCookies");
            _userCache = cacheManager.GetCache<DeemixUser>(GetType(), "user");
            _httpClient = httpClient;
            _logger = logger;
        }

        public DeemixConfig GetSettings(DeemixSettings settings)
        {
            var request = BuildRequest(settings).Resource("/api/getSettings");
            var response = ProcessRequest<DeemixConfigResult>(request);

            return response.Settings;
        }

        public List<DownloadClientItem> GetQueue(DeemixSettings settings)
        {
            var request = BuildRequest(settings).Resource("/api/getQueue");
            var response = ProcessRequest<DeemixQueue>(request);

            return response.Queue.Values.Select(ToDownloadClientItem).ToList();
        }

        public void RemoveFromQueue(string downloadId, DeemixSettings settings)
        {
            var request = BuildRequest(settings)
                .Resource("/api/removeFromQueue")
                .Post()
                .AddFormParameter("uuid", downloadId);

            ProcessRequest(request);
        }

        public string Download(string url, int bitrate, DeemixSettings settings)
        {
            Authenticate(settings);

            var request = BuildRequest(settings)
                .Resource("/api/addToQueue")
                .Post()
                .AddFormParameter("url", url)
                .AddFormParameter("bitrate", bitrate);

            var response = ProcessRequest<DeemixResult<DeemixAddResult>>(request);

            if (response.Result)
            {
                if (response.Data.Obj.Count != 1)
                {
                    throw new DownloadClientException("Expected Deemix to add 1 item, got {0}", response.Data.Obj.Count);
                }

                _logger.Trace("Downloading item {0}", response.Data.Obj[0].Uuid);
                return response.Data.Obj[0].Uuid;
            }

            throw new DownloadClientException("Error adding item to Deemix: {0}", response.Errid);
        }

        private static DownloadClientItem ToDownloadClientItem(DeemixQueueItem x)
        {
            var title = $"{x.Artist} - {x.Title} [WEB] {Formats[x.Bitrate]}";
            if (x.Explicit)
            {
                title += " [Explicit]";
            }

            // assume 3 mins per track, bitrates in kbps
            var size = x.Size * 180L * Bitrates[x.Bitrate] * 128L;

            var item = new DownloadClientItem
            {
                DownloadId = x.Uuid,
                Title = title,
                TotalSize = size,
                RemainingSize = (long)(1 - (x.Progress / 100.0)) * size,
                Status = GetItemStatus(x),
                CanMoveFiles = true,
                CanBeRemoved = true
            };

            if (x.ExtrasPath.IsNotNullOrWhiteSpace())
            {
                item.OutputPath = new OsPath(x.ExtrasPath);
            }

            return item;
        }

        private static DownloadItemStatus GetItemStatus(DeemixQueueItem item)
        {
            if (item.Failed)
            {
                return DownloadItemStatus.Failed;
            }

            if (item.Progress == 0)
            {
                return DownloadItemStatus.Queued;
            }

            if (item.Progress < 100)
            {
                return DownloadItemStatus.Downloading;
            }

            return DownloadItemStatus.Completed;
        }

        private HttpRequestBuilder BuildRequest(DeemixSettings settings)
        {
            return new HttpRequestBuilder(settings.UseSsl, settings.Host, settings.Port, settings.UrlBase)
            {
                LogResponseContent = true
            };
        }

        private HttpRequestBuilder BuildRequest(string baseUrl)
        {
            return new HttpRequestBuilder(baseUrl)
            {
                LogResponseContent = true
            };
        }

        private TResult ProcessRequest<TResult>(HttpRequestBuilder requestBuilder)
            where TResult : new()
        {
            var responseContent = ProcessRequest(requestBuilder);

            return Json.Deserialize<TResult>(responseContent);
        }

        private string ProcessRequest(HttpRequestBuilder requestBuilder)
        {
            var request = requestBuilder.Build();
            request.LogResponseContent = true;
            request.SuppressHttpErrorStatusCodes = new[] { HttpStatusCode.Forbidden };

            var cookie = _sessionCookieCache.Find(requestBuilder.BaseUrl.FullUri);
            if (cookie != null)
            {
                _logger.Trace("Adding cookie {0}", cookie);
                request.Cookies.Add("connect.sid", cookie);
            }

            HttpResponse response;
            try
            {
                response = _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                throw new DownloadClientException("Failed to connect to Deemix, check your settings.", ex);
            }
            catch (WebException ex)
            {
                throw new DownloadClientException("Failed to connect to Deemix, please check your settings.", ex);
            }

            return response.Content;
        }

        public void Authenticate(DeemixSettings settings)
        {
            var requestBuilder = BuildRequest(settings);
            var baseUrl = requestBuilder.BaseUrl.FullUri;

            Authenticate(baseUrl, settings.Arl);
        }

        public void Authenticate(DeemixIndexerSettings settings)
        {
            Authenticate(settings.BaseUrl, settings.Arl);
        }

        private void Authenticate(string baseUrl, string arl)
        {
            var requestBuilder = BuildRequest(baseUrl);

            var user = Connect(baseUrl);
            if (user?.CurrentUser?.Name != null)
            {
                _userCache.Set(baseUrl, user.CurrentUser);
                _logger.Debug("Already logged in to Deemix.");
                return;
            }

            var cookie = _sessionCookieCache.Find(baseUrl);

            if (cookie == null)
            {
                _sessionCookieCache.Remove(baseUrl);
                _userCache.Remove(baseUrl);
            }

            var authLoginRequest = requestBuilder
                .Resource("api/loginArl")
                .Post()
                .AddFormParameter("arl", arl)
                .Accept(HttpAccept.Json)
                .Build();

            var response = _httpClient.Execute(authLoginRequest);
            var cookies = response.GetCookies();

            if (cookies.ContainsKey("connect.sid"))
            {
                cookie = cookies["connect.sid"];
                _sessionCookieCache.Set(baseUrl, cookie);

                _logger.Debug("Got cookie {0}", cookie);

                user = Connect(baseUrl);
                if (user?.CurrentUser?.Name != null)
                {
                    _userCache.Set(baseUrl, user.CurrentUser);
                    _logger.Debug("Deemix authentication succeeded");
                    return;
                }

                _sessionCookieCache.Remove(baseUrl);
                _userCache.Remove(baseUrl);
            }

            throw new DownloadClientException("Failed to authenticate with Deemix");
        }

        private DeemixConnect Connect(string baseUrl)
        {
            var requestBuilder = BuildRequest(baseUrl);
            requestBuilder.Resource("connect");

            var response = ProcessRequest<DeemixConnect>(requestBuilder);

            return response;
        }
    }
}
