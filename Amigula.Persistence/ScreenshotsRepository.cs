using System;
using System.IO;
using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class ScreenshotsRepository : IScreenshotsRepository
    {
        // TODO The below must be stored in the Settings
        private readonly string _screenshotsPath = @"C:\GameBase\Screenshots";

        public ScreenshotsRepository()
        {
        }

        public ScreenshotsRepository(string screenshotsPath)
        {
            _screenshotsPath = screenshotsPath;
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

        public string GetGameSubfolder(string gameTitle)
        {
            int n;
            if (int.TryParse(gameTitle.Substring(0, 1), out n))
                return "0\\";
            return gameTitle.Substring(0, 1) + "\\";
        }

        public OperationResult Add(string filename, string newFilename)
        {
            var destinationPath = GetDestinationPath(newFilename);
            var destinationFilename = Path.Combine(destinationPath, newFilename);
            return CopyFileInPlace(filename, destinationFilename);
        }

        public bool IsFileExists(string filename)
        {
            var fullpath = GetDestinationPathWithFilename(filename);
            return File.Exists(fullpath);
        }

        /// <summary>
        ///     Delete the game's specified Screenshot
        /// </summary>
        /// <param name="filename">The Filename to delete</param>
        public OperationResult Delete(string filename)
        {
            var result = new OperationResult();
            try
            {
                File.Delete(filename);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Information = ex.InnerException.ToString();
                return result;
            }
            result.Success = true;
            return result;
        }

        private string GetDestinationPathWithFilename(string filename)
        {
            var gameSubfolder = GetGameSubfolder(filename);
            var fullpath = Path.Combine(_screenshotsPath, gameSubfolder, filename);
            return fullpath;
        }

        private string GetDestinationPath(string filename)
        {
            var titleSubFolder = GetGameSubfolder(filename);
            var fullpath = Path.Combine(_screenshotsPath, titleSubFolder);
            return fullpath;
        }

        private static OperationResult CopyFileInPlace(string sourceFilename, string destinationFilename)
        {
            var destinationFolder = Path.GetDirectoryName(destinationFilename);
            // check if the destination folder exists, create it if necessary
            if (destinationFolder != null && !Directory.Exists(destinationFolder))
            {
                try
                {
                    Directory.CreateDirectory(destinationFilename);
                }
                catch (Exception ex)
                {
                    return new OperationResult {Success = false, Information = ex.Message};
                }
            }

            // now copy the file
            try
            {
                File.Copy(sourceFilename, destinationFilename);
            }
            catch (Exception ex)
            {
                return new OperationResult {Success = false, Information = ex.Message};
            }

            return new OperationResult {Success = true};
        }
    }
}