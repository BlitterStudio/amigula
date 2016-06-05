using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amigula.Domain.Classes;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Persistence
{
    public class MusicPlayerRepository : IMusicPlayerRepository
    {
        public MusicPlayerDto GetPlayerPath()
        {
            var playerPath = new MusicPlayerDto();
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            playerPath.PlayerPath = Directory.Exists(Path.Combine(programFilesPath, "Deliplayer2")) 
                ? Path.Combine(programFilesPath, "Deliplayer2\\DeliPlayer.exe") 
                : ".\\xmplay\\xmplay.exe";

            return playerPath;
        }

        public OperationResult PlayGameMusic(string gameTitle)
        {
            var musicPlayer = GetPlayerPath();
            var result = new OperationResult();
            if (string.IsNullOrEmpty(gameTitle))
            {
                result.Success = false;
                result.Information = "No music file found";
                return result;
            }

            if (!string.IsNullOrEmpty(musicPlayer.PlayerPath) && File.Exists(musicPlayer.PlayerPath))
            {
                try
                {
                    Process.Start(musicPlayer.PlayerPath,
                        "\"" + Path.Combine(musicPlayer.PlayerPath, gameTitle) + "\"");
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Information = ex.InnerException.ToString();
                }
            }
            else
            {
                result.Success = false; 
                result.Information = 
                    "Sorry, but you have no music player set-up in Preferences!\nWithout one, it's not possible to play the game's music...";
            }
            return result;
        }
    }
}
