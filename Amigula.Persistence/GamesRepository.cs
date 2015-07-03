using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class GamesRepository : IGamesRepository
    {
        public IEnumerable<GamesDto> GetGamesList()
        {
            throw new NotImplementedException();
        }

        public bool FilenameExists(string gameFullPath)
        {
            return File.Exists(gameFullPath);
        }

        public OperationResult CopyFileInPlace(string screenshot, string destination)
        {
            if (ScreenshotPathDoesNotExist(destination))
                CreateScreenshotPath(destination);

            throw new NotImplementedException();
        }

        private void CreateScreenshotPath(string destination)
        {
            throw new NotImplementedException();
        }

        private bool ScreenshotPathDoesNotExist(string destination)
        {
            throw new NotImplementedException();
        }
    }
}
