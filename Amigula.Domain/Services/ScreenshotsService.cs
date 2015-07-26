using System.Text.RegularExpressions;
using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class ScreenshotsService
    {
        private readonly IScreenshotsRepository _screenshotsRepository;

        public ScreenshotsService(IScreenshotsRepository screenshotsRepository)
        {
            _screenshotsRepository = screenshotsRepository;
        }

        public ScreenshotsDto PrepareTitleScreenshot(string gameTitle)
        {
            var result = new ScreenshotsDto();
            if (string.IsNullOrEmpty(gameTitle)) return result;

            result.GameFolder = GetTitleSubfolder(gameTitle);
            result.Title = GameTitleService.CleanGameTitle(gameTitle);

            result.Screenshot1 = DetermineTitleScreenshot(result.Title, 1);
            result.Screenshot2 = DetermineTitleScreenshot(result.Title, 2);
            result.Screenshot3 = DetermineTitleScreenshot(result.Title, 3);

            return result;
        }

        /// <summary>
        ///     Get the first letter of the game title, to get the subfolder from that.
        ///     if the first letter is a number, the subfolder should be set to "0"
        ///     in both scenarios we add 2 backslashes at the end, since this is a path.
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <returns>Game Screenshot Folder</returns>
        private string GetTitleSubfolder(string gameTitle)
        {
            var result = _screenshotsRepository.GetTitleSubfolder(gameTitle);
            return result;
        }

        /// <summary>
        ///     Replace spaces with underscores, adding ".png" at the end
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <param name="screenshotNumber"></param>
        /// <returns></returns>
        private string DetermineTitleScreenshot(string gameTitle, int screenshotNumber)
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

        public OperationResult Add(string gameTitle, string screenshot)
        {
            var renamedScreenshot = CreateScreenshotFilename(gameTitle, screenshot);
            if (renamedScreenshot != screenshot)
            {
                var result = _screenshotsRepository.Add(gameTitle, renamedScreenshot);
                return result;
            }

            return new OperationResult {Success = false, Information = "Could not add new Screenshot for game!"};
        }

        public OperationResult Delete(string screenshot)
        {
            var result = _screenshotsRepository.Delete(screenshot);
            return result;
        }

        private string CreateScreenshotFilename(string gameTitle, string screenshot)
        {
            // use gametitle + .png as the screenshot name
            // test if that filename already exists
            // if it does, change the screenshot name to gametitle + _1.png
            // test if that filename already exists
            // if it does, change the screenshot name to gametitle + _2.png
            // test if that filename already exists
            // if it still does, then report an error

            var renamedScreenshot = $"{gameTitle}.png";

            if (_screenshotsRepository.ScreenshotFileExists(renamedScreenshot))
            {
                for (var i = 1; i < 3; i++)
                {
                    renamedScreenshot = $"{gameTitle}_{i}.png";
                    if (!_screenshotsRepository.ScreenshotFileExists(renamedScreenshot)) break;
                }
            }

            return _screenshotsRepository.ScreenshotFileExists(renamedScreenshot) ? screenshot : renamedScreenshot;
        }
    }
}