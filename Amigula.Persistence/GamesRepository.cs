using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
