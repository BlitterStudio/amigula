using System.Collections.Generic;
using Amigula.Domain.DTO;

namespace Amigula.Domain.Interfaces
{
    public interface IGamesRepository
    {
        IEnumerable<GamesDto> GetGamesList();
    }
}