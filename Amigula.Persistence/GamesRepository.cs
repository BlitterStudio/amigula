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
            if (PathDoesNotExist(destination))
                CreatePath(destination);

            throw new NotImplementedException();
        }

        private void CreatePath(string destination)
        {
            throw new NotImplementedException();
        }

        private bool PathDoesNotExist(string destination)
        {
            throw new NotImplementedException();
        }
    }
}
