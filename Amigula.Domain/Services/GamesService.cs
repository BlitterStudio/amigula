using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class GamesService
    {
        private readonly IGamesRepository _gamesRepository;

        public GamesService(IGamesRepository gamesRepository)
        {
            _gamesRepository = gamesRepository;
        }

        public IEnumerable<GamesDto> GetGamesList()
        {
            var gamesDto = _gamesRepository.GetGamesList();
            return gamesDto;
        }

        public GameScreenshotsDto PrepareGameTitleForScreenshot(string gameTitle)
        {
            var result = new GameScreenshotsDto();
            if (string.IsNullOrEmpty(gameTitle)) return result;
         
            result.GameFolder = DetermineTitleSubfolder(gameTitle);
            result.Title = CleanGameTitle(gameTitle);

            result.Screenshot1 = DetermineTitleScreenshot(result.Title, 1);
            result.Screenshot2 = DetermineTitleScreenshot(result.Title, 2);
            result.Screenshot3 = DetermineTitleScreenshot(result.Title, 3);

            return result;
        }

        /// <summary>
        /// Remove version information and anything with () or [] from title
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns>Cleaned up Title</returns>
        private static string CleanGameTitle(string gameTitle)
        {
            // Remove anything in the title containing () or []
            gameTitle = Regex.Replace(gameTitle, @"[\[(].+?[\])]", "");

            // if there's version information (e.g. v1.2) in the filename remove it as well
            if (Regex.IsMatch(gameTitle, @"\sv(\d{1})"))
            {
                gameTitle = gameTitle.Substring(0,
                    gameTitle.IndexOf(" v",
                        StringComparison
                            .OrdinalIgnoreCase));
            }
            return gameTitle;
        }

        /// <summary>
        /// Get the first letter of the game title, to get the subfolder from that.
        /// if the first letter is a number, the subfolder should be set to "0"
        /// in both scenarios we add 2 backslashes at the end, since this is a path.
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns>Game Screenshot Folder</returns>
        private static string DetermineTitleSubfolder(string gameTitle)
        {
            int n;
            if (int.TryParse(gameTitle.Substring(0, 1), out n))
                return "0\\";
            return gameTitle.Substring(0, 1) + "\\";
        }

        /// <summary>
        /// Replace spaces with underscores, adding ".png" at the end
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <param name="screenshotNumber"></param>
        /// <returns></returns>
        private static string DetermineTitleScreenshot(string gameTitle, int screenshotNumber)
        {
            // Screenshot 1 does not get any numbering,
            // Screenshot 2 gets the suffix _1.png,
            // Screenshot 3 gets the suffix _2.png

            var suffix = ".png";

            if (screenshotNumber == 2) suffix = "_1" + suffix;
            if (screenshotNumber == 3) suffix = "_2" + suffix;

            return Regex.Replace(gameTitle, " $", "")
                .Replace(" ", "_") + suffix;
        }
    }
}
