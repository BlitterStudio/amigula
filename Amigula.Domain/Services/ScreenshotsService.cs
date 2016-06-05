using System.IO;
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

        public ScreenshotsDto GetGameScreenshots(string gameTitle)
        {
            var result = new ScreenshotsDto();
            if (string.IsNullOrEmpty(gameTitle)) return result;

            result.GameFolder = _screenshotsRepository.GetGameSubfolder(gameTitle);
            result.Title = GamesService.CleanGameTitle(gameTitle);

            result.Screenshot1 = BuildScreenshotFilename(result.Title, 1);
            result.Screenshot2 = BuildScreenshotFilename(result.Title, 2);
            result.Screenshot3 = BuildScreenshotFilename(result.Title, 3);

            return result;
        }

        /// <summary>
        ///     Replace spaces with underscores, adding ".png" at the end
        /// </summary>
        /// <param name="gameTitle"></param>
        /// <param name="screenshotNumber"></param>
        /// <returns></returns>
        private static string BuildScreenshotFilename(string gameTitle, int screenshotNumber)
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

        public OperationResult Add(string gameTitle, string filename)
        {
            // Get the filename from the full path
            var screenshot = Path.GetFileName(filename);
            // Rename filename according to game title
            var renamedScreenshot = RenameScreenshot(gameTitle, screenshot);
            // if the filename was changed we can proceed. Otherwise, the 3 available screenshots are already occupied
            if (renamedScreenshot == screenshot)
                return new OperationResult
                {
                    Success = false,
                    Information = "Could not add new Screenshot for game! Try deleting one of the existing ones first."
                };
            var result = _screenshotsRepository.Add(filename, renamedScreenshot);
            return result;
        }

        public OperationResult Delete(string screenshot)
        {
            var result = _screenshotsRepository.Delete(screenshot);
            return result;
        }

        private string RenameScreenshot(string gameTitle, string screenshot)
        {
            // use gametitle + .png as the screenshot name
            // test if that filename already exists
            // if it does, change the screenshot name to gametitle + _1.png
            // test if that filename already exists
            // if it does, change the screenshot name to gametitle + _2.png
            // test if that filename already exists
            // if it still does, then report an error

            var renamedScreenshot = $"{gameTitle}.png";

            if (_screenshotsRepository.IsFileExists(renamedScreenshot))
            {
                for (var i = 1; i < 3; i++)
                {
                    renamedScreenshot = $"{gameTitle}_{i}.png";
                    if (!_screenshotsRepository.IsFileExists(renamedScreenshot)) break;
                }
            }

            return _screenshotsRepository.IsFileExists(renamedScreenshot) ? screenshot : renamedScreenshot;
        }
    }
}