using System;
using System.Collections.Generic;
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

        public bool IsGameExists(string filename)
        {
            throw new NotImplementedException();
        }
    }
}