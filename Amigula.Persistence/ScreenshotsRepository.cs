using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
    }
}
