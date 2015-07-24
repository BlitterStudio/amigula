using System;
using System.Text.RegularExpressions;

namespace Amigula.Domain.Services
{
    public class GameTitleService
    {
        /// <summary>
        ///     Remove version information and anything with () or [] from title
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns>Cleaned up Title</returns>
        public static string CleanGameTitle(string gameTitle)
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
        ///     Prepare the title for using it as a parameter in a URL, replace spaces with "%20".
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns></returns>
        public string PrepareTitleUrl(string gameTitle)
        {
            if (string.IsNullOrEmpty(gameTitle)) return "";

            var cleanedGameTitle = CleanGameTitle(gameTitle);

            if (cleanedGameTitle.Length > 0)
                cleanedGameTitle = cleanedGameTitle
                    .TrimEnd(' ')
                    .Replace(" ", "%20");

            return cleanedGameTitle;
        }
    }
}