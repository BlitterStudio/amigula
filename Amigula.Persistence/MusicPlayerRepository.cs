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
    public class MusicPlayerRepository : IMusicPlayerRepository
    {
        public MusicPlayerDto GetMusicPlayerPath()
        {
            var playerPath = new MusicPlayerDto();
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            playerPath.MusicPlayerPath = Directory.Exists(Path.Combine(programFilesPath, "Deliplayer2")) 
                ? Path.Combine(programFilesPath, "Deliplayer2\\DeliPlayer.exe") 
                : ".\\xmplay\\xmplay.exe";

            return playerPath;
        }
    }
}
