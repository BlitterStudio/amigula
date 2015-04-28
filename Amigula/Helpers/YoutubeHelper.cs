using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Amigula.Helpers
{
    public sealed class YoutubeHelper
    {
        public class YouTubeInfo
        {
            public string LinkUrl { get; set; }
            public string EmbedUrl { get; set; }
        }

        /// <summary>
        ///     Simple helper methods that turns a link string into a embed string
        ///     for a YouTube item.
        ///     turns
        ///     http://www.youtube.com/watch?v=hV6B7bGZ0_E
        ///     into
        ///     http://www.youtube.com/v/hV6B7bGZ0_E
        /// </summary>
        public static string GetEmbedUrlFromLink(string link)
        {
            try
            {
                string embedUrl = link.Replace("watch?v=", "embed/").Replace("&feature=youtube_gdata", "");
                return embedUrl;
            }
            catch
            {
                return link;
            }
        }

        /// <summary>
        ///     Returns a List<see cref="MainWindow.YouTubeInfo">YouTubeInfo</see> which represent
        ///     the YouTube videos that matched the keyWord input parameter
        /// </summary>
        public static List<YouTubeInfo> LoadVideosKey(string keyWord)
        {
            const string search = "http://gdata.youtube.com/feeds/api/videos?q={0}&alt=rss&&max-results=1&v=2";
            
            try
            {
                XElement xraw = XElement.Load(String.Format(search, keyWord));
                XElement xroot = XElement.Parse(xraw.ToString());
                var xElement = xroot.Element("channel");
                if (xElement != null)
                {
                    IEnumerable<YouTubeInfo> links = (from item in xElement.Descendants("item")
                        let element = item.Element("link")
                        where element != null
                        select new YouTubeInfo
                        {
                            LinkUrl = element.Value,
                            EmbedUrl = GetEmbedUrlFromLink(element.Value),
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
    }
}