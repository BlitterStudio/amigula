using System;
using System.IO;
using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class ScreenshotsRepository : IScreenshotsRepository
    {
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

        /// <summary>
        ///     Delete the game's specified Screenshot
        /// </summary>
        /// <param name="filename">The filename to delete</param>
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

        public OperationResult CopyFileInPlace(string filename, string destination)
        {
            if (PathDoesNotExist(destination))
                CreatePath(destination);

            throw new NotImplementedException();
        }

        public OperationResult RenameFile(string oldFilename, string newFilename)
        {
            throw new NotImplementedException();
        }

        public bool FilenameExists(string filenameFullPath)
        {
            return File.Exists(filenameFullPath);
        }

        private bool PathDoesNotExist(string destination)
        {
            throw new NotImplementedException();
        }

        private void CreatePath(string destination)
        {
            //if (!Directory.Exists(Path.Combine(Settings.Default.ScreenshotsPath, gameSubFolder)))
            //    Directory.CreateDirectory(Path.Combine(Settings.Default.ScreenshotsPath, gameSubFolder));
            throw new NotImplementedException();
        }
    }
}