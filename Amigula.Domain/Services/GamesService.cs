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
            int n;

            // Get the first letter of the game, to get the subfolder from that.
            // if the first letter is a number, the subfolder should be set to "0"
            if (!string.IsNullOrEmpty(gameTitle) 
                && int.TryParse(gameTitle.Substring(0, 1), out n))
                result.GameFolder = "0\\";
            else if (!string.IsNullOrEmpty(gameTitle))
                result.GameFolder = gameTitle.Substring(0, 1) + "\\";

            // Use RegEx to clean up anything in () or []
            gameTitle = Regex.Replace(gameTitle, @"[\[(].+?[\])]", "");

            // if there's version information (e.g. v1.2) in the filename exclude it as well
            if (Regex.IsMatch(gameTitle, @"\sv(\d{1})"))
            {
                gameTitle = gameTitle.Substring(0,
                    gameTitle.IndexOf(" v",
                        StringComparison
                            .OrdinalIgnoreCase));
            }

            result.Title = gameTitle;

            // now try to match the filename to the title selected, adding ".png" at the end
            // this is far from perfect, needs improvement!
            if (gameTitle.Length > 0)
            {
                result.Screenshot1 = Regex.Replace(gameTitle, " $", "")
                    .Replace(" ", "_") + ".png";
                result.Screenshot2 = Regex.Replace(gameTitle, " $", "")
                    .Replace(" ", "_") + "_1.png";
                result.Screenshot3 = Regex.Replace(gameTitle, " $", "")
                    .Replace(" ", "_") + "_2.png";
            }
            return result;
        }
    }
}
