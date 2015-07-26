using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class MusicPlayerService
    {
        private readonly IMusicPlayerRepository _playerRepository;

        public MusicPlayerService(IMusicPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public MusicPlayerDto GetPlayerPath()
        {
            var musicPlayerPath = _playerRepository.GetPlayerPath();
            return musicPlayerPath;
        }

        public OperationResult PlayGameMusic(string gameTitle)
        {
            var playMusicResult = _playerRepository.PlayGameMusic(gameTitle);
            return playMusicResult;
        }
    }
}
