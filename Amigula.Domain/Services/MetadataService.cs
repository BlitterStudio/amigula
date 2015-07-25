using System.Diagnostics;
using System.Text.RegularExpressions;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class MetadataService
    {
        private readonly IMetadataRepository _metadataRepository;

        public MetadataService(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public string GetGenre(string gameTitle)
        {
            var result = _metadataRepository.GetGenre(gameTitle);
            return result;
        }

        public string GetPublisher(string gameTitle)
        {
            var result = _metadataRepository.GetPublisher(gameTitle);
            return result;
        }

        public string GetYear(string gameTitle)
        {
            var result = _metadataRepository.GetYear(gameTitle);
            return result;
        }

        /// <summary>
        ///     Get the game Year from the filename if it exists
        /// </summary>
        /// <param name="selectedGamePath">The selected game filename</param>
        /// <returns></returns>
        public int GetGameYear(string selectedGamePath)
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