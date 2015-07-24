using System;
using System.Net;

namespace Amigula.Domain.Services
{
    public class YoutubeService
    {
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