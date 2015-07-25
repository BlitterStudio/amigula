using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;

namespace Amigula.Domain.Interfaces
{
    public interface IScreenshotsRepository
    {
        bool FilenameExists(string filenameFullPath);
        OperationResult CopyFileInPlace(string filename, string destination);
        OperationResult RenameFile(string oldFilename, string newFilename);
        BitmapImage LoadImage(string filename);
        OperationResult Delete(string filename);
    }
}