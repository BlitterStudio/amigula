using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;

namespace Amigula.Domain.Interfaces
{
    public interface IScreenshotsRepository
    {
        BitmapImage LoadImage(string filename);
        string GetTitleSubfolder(string gameTitle);
        OperationResult Add(string gameTitle, string screenshot);
        OperationResult Delete(string screenshot);
        bool ScreenshotFileExists(string screenshot);
    }
}