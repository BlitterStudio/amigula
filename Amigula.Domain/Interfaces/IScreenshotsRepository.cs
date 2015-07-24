using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Amigula.Domain.Interfaces
{
    public interface IScreenshotsRepository
    {
        BitmapImage LoadImage(string filename);
    }
}
