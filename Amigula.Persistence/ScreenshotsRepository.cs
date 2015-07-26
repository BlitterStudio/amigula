using System;
using System.IO;
using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class ScreenshotsRepository : IScreenshotsRepository
    {
        private readonly IFileOperations _fileOperations;

        // TODO The below must be stored in the Settings
        private readonly string _screenshotsPath = @"C:\GameBase\Screenshots";

        public ScreenshotsRepository(IFileOperations fileOperations)
        {
            _fileOperations = fileOperations;
        }

        public BitmapImage LoadImage(string filename)
        {
            var gameScreenshot = new BitmapImage();
            try
            {
                gameScreenshot.BeginInit();
                gameScreenshot.CacheOption = BitmapCacheOption.OnLoad;
                gameScreenshot.UriSource = new Uri(filename);
                gameScreenshot.EndInit();
            }
            catch (Exception ex)
            {
                // something went wrong! Handle accordingly here
            }
            return gameScreenshot;
        }

        public string GetTitleSubfolder(string gameTitle)
        {
            int n;
            if (int.TryParse(gameTitle.Substring(0, 1), out n))
                return "0\\";
            return gameTitle.Substring(0, 1) + "\\";
        }

        public OperationResult Add(string screenshot, string renamedScreenshot)
        {
            _fileOperations.CopyFileInPlace(screenshot, GetFullPath(screenshot));
            _fileOperations.RenameFile(screenshot, renamedScreenshot);
            return new OperationResult { Success = true, Information = renamedScreenshot};
        }

        public OperationResult Delete(string screenshot)
        {
            throw new NotImplementedException();
        }

        private string GetFullPath(string filename)
        {
            var titleSubFolder = GetTitleSubfolder(filename);
            var fullpath = Path.Combine(_screenshotsPath, titleSubFolder, filename);
            return fullpath;
        }

        public bool ScreenshotFileExists(string filename)
        {
            var fullpath = GetFullPath(filename);
            return _fileOperations.FilenameExists(fullpath);
        }
    }
}