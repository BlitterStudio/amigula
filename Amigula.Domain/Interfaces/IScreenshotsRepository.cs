using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;

namespace Amigula.Domain.Interfaces
{
    public interface IScreenshotsRepository
    {
        BitmapImage LoadImage(string filename);
        string GetGameSubfolder(string gameTitle);
        OperationResult Add(string filename, string newFilename);
        OperationResult Delete(string filename);
        bool IsFileExists(string filename);
    }
}