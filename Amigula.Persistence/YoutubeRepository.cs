using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class YoutubeRepository : IVideoRepository
    {
        public IEnumerable<VideoDto> GetVideos(string title)
        {
            const string search = "http://gdata.youtube.com/feeds/api/videos?q={0}&alt=rss&&max-results=1&v=2";

            try
            {
                var xraw = XElement.Load(string.Format(search, title));
                var xroot = XElement.Parse(xraw.ToString());
                var xElement = xroot.Element("channel");
                if (xElement != null)
                {
                    var links = (from item in xElement.Descendants("item")
                        let element = item.Element("link")
                        where element != null
                        select new VideoDto
                        {
                            LinkUrl = element.Value,
                            EmbedUrl = GetEmbedUrl(element.Value)
                        }).Take(1);

                    return links.ToList();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "ERROR");
            }
            return null;
        }

        /// <summary>
        ///     Simple helper methods that turns a link string into a embed string
        ///     for a YouTube item.
        ///     turns
        ///     http://www.youtube.com/watch?v=hV6B7bGZ0_E
        ///     into
        ///     http://www.youtube.com/v/hV6B7bGZ0_E
        /// </summary>
        public static string GetEmbedUrl(string link)
        {
            try
            {
                var embedUrl = link.Replace("watch?v=", "embed/").Replace("&feature=youtube_gdata", "");
                return embedUrl;
            }
            catch
            {
                return link;
            }
        }
    }
}