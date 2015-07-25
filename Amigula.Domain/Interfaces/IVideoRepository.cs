using System.Collections.Generic;
using Amigula.Domain.DTO;

namespace Amigula.Domain.Interfaces
{
    public interface IVideoRepository
    {
        IEnumerable<VideoDto> GetVideos(string title);
    }
}