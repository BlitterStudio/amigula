using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Amigula.Domain.Services
{
    public class YoutubeService
    {
        /// <summary>
        /// Checks if the specified URL exists.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public bool UrlExists(string url)
        {
            if (!IsValidUri(url)) return false;
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
            var result = Uri.TryCreate(url, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp ||
                         uriResult.Scheme == Uri.UriSchemeHttps;
            return result;
        }
    }
}
