using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Amigula.Domain.Interfaces;
using HtmlAgilityPack;

namespace Amigula.Domain.Services
{
    public class MetadataService
    {
        private readonly IMetadataRepository _metadataRepository;

        public MetadataService(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public static string GetFetchedGenre(HtmlDocument document)
        {
            // XPath for Genre: //table[@width='100%']/tr[12]/td/table/tr[2]/td[2]/a
            var fetchedGenre =
                document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[13]/td[1]/table/tr[2]/td[2]/a")
                    .InnerText;
            return fetchedGenre;
        }

        public static string GetFetchedPublisher(HtmlDocument document)
        {
            // XPath for Publisher: //table[@width='100%']/tr[2]/td[4]/a
            var fetchedPublisher = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a") != null)
                fetchedPublisher =
                    document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[2]/td[4]/a").InnerText;
            return fetchedPublisher;
        }

        public static string GetFetchedYear(HtmlDocument document)
        {
            // XPath for Year: //table[@width='100%']/tr[1]/td[2]/a
            var fetchedYear = "";
            if (document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a") != null)
                fetchedYear = document.DocumentNode.SelectSingleNode("//table[@width='100%']/tr[1]/td[2]/a").InnerText;
            //MessageBox.Show("The game's Year is: " + fetchedYear);
            return fetchedYear;
        }

        public static string GetGameUrl(string gamelink)
        {
            var gameurl = gamelink.Substring(gamelink.IndexOf("http", StringComparison.Ordinal),
                gamelink.IndexOf(",", StringComparison.Ordinal) - gamelink.IndexOf("http", StringComparison.Ordinal));
            return gameurl;
        }

        /// <summary>
        ///     Get the game Year from the filename if it exists
        /// </summary>
        /// <param name="selectedGamePath">The selected game filename</param>
        /// <returns></returns>
        public static int GetGameYear(string selectedGamePath)
        {
            // Try to get the game Year from the filename
            // e.g. gameTitle (1988) (Psygnosis).zip should return 1988 as gameYear
            var gameYear = 1900; // default year if no other is found

            if (Regex.IsMatch(selectedGamePath, @"\((\d{4})\)"))
                int.TryParse(Regex.Replace(Regex.Match(selectedGamePath, @"\((\d{4})\)").Value, @"\(|\)", ""),
                    out gameYear);
            return gameYear;
        }

        public void LookupUrl(string gameTitle, string website)
        {
            if (string.IsNullOrEmpty(gameTitle)) return;
            switch (website)
            {
                case "HOL":
                {
                    const string targetUrl = @"http://hol.abime.net/hol_search.php?find=";
                    Process.Start(targetUrl + gameTitle);
                    break;
                }
                case "LemonAmiga":
                {
                    const string targetUrl = @"http://www.lemonamiga.com/games/list.php?list_letter=";
                    Process.Start(targetUrl + gameTitle);
                    break;
                }
            }
        }
    }
}