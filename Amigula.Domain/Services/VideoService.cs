using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class VideoService
    {
        private const string AmigaLongplayKeyword = @"Amiga Longplay ";
        private readonly IVideoRepository _videoRepository;

        public VideoService(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }

        public IEnumerable<VideoDto> GetVideos(string title)
        {
            if (string.IsNullOrEmpty(title)) return new List<VideoDto>();
            var searchKeyword = AmigaLongplayKeyword + title;
            var videos = _videoRepository.GetVideos(searchKeyword);
            return videos;
        }

        public string GetEmbeddedVideo(IEnumerable<VideoDto> videos)
        {
            var videoDtos = videos as IList<VideoDto> ?? videos.ToList();
            var firstVideo = videoDtos.FirstOrDefault();

            return firstVideo?.EmbedUrl;
        }

        /// <summary>
        ///     Checks if the specified URL exists.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public bool UrlExists(string url)
        {
            var urlIsValid = IsValidUri(url);

            if (!urlIsValid)
            {
                var urlWithHttp = BuildHttpUri(url);
                urlIsValid = IsValidUri(urlWithHttp);
                if (urlIsValid) url = urlWithHttp;
            }

            if (!urlIsValid) return false;

            try
            {
                //Creating the HttpWebRequest
                var request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                if (request != null)
                {
                    request.Method = "HEAD";
                    //Getting the Web Response.
                    var response = request.GetResponse() as HttpWebResponse;
                    //Returns TRUE if the Status code == 200
                    return response != null && (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch
            {
                //Any exception will return false.
                return false;
            }

            return false;
        }

        private static bool IsValidUri(string url)
        {
            Uri uriResult;
            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out uriResult);
            if (isValidUri)
            {
                isValidUri = uriResult.Scheme == Uri.UriSchemeHttp ||
                             uriResult.Scheme == Uri.UriSchemeHttps;
            }

            return isValidUri;
        }

        private static string BuildHttpUri(string uri)
        {
            return $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{uri}/";
        }
    }
}