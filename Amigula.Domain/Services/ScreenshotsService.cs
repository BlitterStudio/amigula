using System.IO;
using System.Text.RegularExpressions;
using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class ScreenshotsService
    {
        // TODO The below must be stored in the Settings
        private readonly string _screenshotsPath = @"C:\GameBase\Screenshots";
        private readonly IScreenshotsRepository _screenshotsRepository;
        private readonly IFileOperations _fileOperations;

        public ScreenshotsService(IScreenshotsRepository screenshotsRepository, IFileOperations fileOperations)
        {
            _screenshotsRepository = screenshotsRepository;
            _fileOperations = fileOperations;
        }

        public GameScreenshotsDto PrepareTitleScreenshot(string gameTitle)
        {
            var result = new GameScreenshotsDto();
            if (string.IsNullOrEmpty(gameTitle)) return result;

            result.GameFolder = DetermineTitleSubfolder(gameTitle);
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
        private static string DetermineTitleSubfolder(string gameTitle)
        {
            int n;
            if (int.TryParse(gameTitle.Substring(0, 1), out n))
                return "0\\";
            return gameTitle.Substring(0, 1) + "\\";
        }

        /// <summary>
        ///     Replace spaces with underscores, adding ".png" at the end
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

        public OperationResult AddGameScreenshot(string gameTitle, string screenshot)
        {
            var renamedScreenshotFile = CreateScreenshotFilename(gameTitle, screenshot);
            if (renamedScreenshotFile != screenshot)
            {
                _fileOperations.CopyFileInPlace(screenshot, GetFullPath(screenshot));
                _fileOperations.RenameFile(screenshot, renamedScreenshotFile);
                return new OperationResult {Success = true};
            }

            return new OperationResult {Success = false, Information = "Could not add new Screenshot for game!"};
        }

        public OperationResult Delete(string screenshot)
        {
            var result = _fileOperations.Delete(screenshot);
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

            if (ScreenshotFileExists(renamedScreenshot))
            {
                for (var i = 1; i < 3; i++)
                {
                    renamedScreenshot = $"{gameTitle}_{i}.png";
                    if (!ScreenshotFileExists(renamedScreenshot)) break;
                }
            }

            return ScreenshotFileExists(renamedScreenshot) ? screenshot : renamedScreenshot;
        }

        private bool ScreenshotFileExists(string filename)
        {
            var fullpath = GetFullPath(filename);
            return _fileOperations.FilenameExists(fullpath);
        }

        private string GetFullPath(string filename)
        {
            var titleSubFolder = DetermineTitleSubfolder(filename);
            var fullpath = Path.Combine(_screenshotsPath, titleSubFolder, filename);
            return fullpath;
        }
    }
}