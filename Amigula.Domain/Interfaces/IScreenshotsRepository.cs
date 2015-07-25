using System.Windows.Media.Imaging;
using Amigula.Domain.Classes;

namespace Amigula.Domain.Interfaces
{
    public interface IScreenshotsRepository
    {
        BitmapImage LoadImage(string filename);

    }
}