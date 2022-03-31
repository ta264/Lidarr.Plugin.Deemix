using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class DeemixResult<T>
        where T : new()
    {
        public bool Result { get; set; }

        public string Errid { get; set; }

        public T Data { get; set; }
    }

    public class DeemixConfigResult
    {
        public DeemixConfig Settings { get; set; }
    }

    public class DeemixConfig
    {
        public string DownloadLocation { get; set; }
        public bool CreateAlbumFolder { get; set; }
        public bool CreateSingleFolder { get; set; }
    }

    public class DeemixConnect
    {
        public DeemixUser CurrentUser { get; set; }
    }

    public class DeemixUser
    {
        public long Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("can_stream_hq")]
        public bool CanStreamHq { get; set; }
        [JsonProperty("can_stream_lossless")]
        public bool CanStreamLossless { get; set; }
    }

    public class DeemixAddResult
    {
        public List<string> Url { get; set; }
        public string Bitrate { get; set; }
        public List<DeemixQueueItem> Obj { get; set; }
    }

    public class DeemixQueue
    {
        public Dictionary<string, DeemixQueueItem> Queue { get; set; }
        public List<string> QueueOrder { get; set; }
        public DeemixQueueItem Current { get; set; }
    }

    public class DeemixQueueItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Cover { get; set; }
        public bool Explicit { get; set; }
        public int Size { get; set; }
        public string ExtrasPath { get; set; }
        public int Downloaded { get; set; }
        public int Failed { get; set; }
        public List<object> Errors { get; set; }
        public int Progress { get; set; }
        public List<DeemixFile> Files { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Bitrate { get; set; }
        public string Uuid { get; set; }
        public string Status { get; set; }
    }

    public class DeemixSearchResponse
    {
        public IList<DeemixGwAlbum> Data { get; set; }
        public int Total { get; set; }
    }

    public class ExplicitAlbumContent
    {
        [JsonProperty("EXPLICIT_LYRICS_STATUS")]
        public int ExplicitLyrics { get; set; }

        [JsonProperty("EXPLICIT_COVER_STATUS")]
        public int ExplicitCover { get; set; }
    }

    public class DeemixGwAlbum
    {
        [JsonProperty("ALB_ID")]
        public string AlbumId { get; set; }
        [JsonProperty("ALB_TITLE")]
        public string AlbumTitle { get; set; }
        [JsonProperty("ALB_PICTURE")]
        public string AlbumPicture { get; set; }
        public bool Available { get; set; }
        [JsonProperty("ART_ID")]
        public string ArtistId { get; set; }
        [JsonProperty("ART_NAME")]
        public string ArtistName { get; set; }
        [JsonProperty("EXPLICIT_ALBUM_CONTENT")]
        public ExplicitAlbumContent ExplicitAlbumContent { get; set; }

        // These two are string not DateTime since sometimes Deemix provides invalid values (like 0000-00-00)
        [JsonProperty("PHYSICAL_RELEASE_DATE")]
        public string PhysicalReleaseDate { get; set; }
        [JsonProperty("DIGITAL_RELEASE_DATE")]
        public string DigitalReleaseDate { get; set; }

        public string Type { get; set; }
        [JsonProperty("ARTIST_IS_DUMMY")]
        public bool ArtistIsDummy { get; set; }
        [JsonProperty("NUMBER_TRACK")]
        public string TrackCount { get; set; }
        [JsonProperty("DURATION")]
        public int DurationInSeconds { get; set; }

        public string Version { get; set; }
        public string Link { get; set; }

        public bool Explicit => ExplicitAlbumContent?.ExplicitLyrics == 1 ||
                                ExplicitAlbumContent?.ExplicitLyrics == 4 ||
                                ExplicitAlbumContent?.ExplicitCover == 1;
    }

    public class DeemixAlbumUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }
    }

    public class DeemixFileData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }
    }

    public class DeemixFile
    {
        [JsonProperty("albumURLs")]
        public List<DeemixAlbumUrl> AlbumUrls { get; set; }

        [JsonProperty("albumPath")]
        public string AlbumPath { get; set; }

        [JsonProperty("albumFilename")]
        public string AlbumFilename { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("data")]
        public DeemixFileData Data { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
