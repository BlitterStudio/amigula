using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Domain.Services
{
    public class GamesService
    {
        private readonly IGamesRepository _gamesRepository;

        public GamesService(IGamesRepository gamesRepository)
        {
            _gamesRepository = gamesRepository;
        }

        public IEnumerable<GamesDto> GetGamesList()
        {
            var gamesDto = _gamesRepository.GetGamesList();
            return gamesDto;
        }

        /// <summary>
        ///     Determine if a game is multi-disk from the filename, using certain scenarios
        /// </summary>
        /// <param name="gameFullPath"></param>
        /// <returns>A list of the filenames for the game, multi-disk or single disk</returns>
        public IEnumerable<string> GetGameDisks(string gameFullPath)
        {
            // If the game consists of more than 1 Disk, then the first disk should be passed to WinUAE as usual,
            // but the rest of them should go in the DiskSwapper feature of WinUAE. To do that, the config file must be
            // edited and lines diskimage0-19=<path to filename> must be appended/edited.

            // Checks to be done for possible versions of multi-disk games:
            // 1. <game> Disk1.zip, <game> Disk2.zip etc.
            // 2. <game> Disk01.zip, <game> Disk02.zip etc.
            // 3. <game> (Disk 1 of 2).zip, <game> (Disk 2 of 2).zip etc.
            // 4. <game> (Disk 01 of 11).zip, <game> (Disk 02 of 11).zip etc.
            // 5. <game>-1.zip, <game>-2.zip etc.

            var gameDisksFullPath = new List<string>();

            if (IsMultiDiskPattern1(gameFullPath))
            {
                // case 1. <game> Disk1.zip, <game> Disk2.zip etc.
                gameDisksFullPath = GetDisksFullPath(gameFullPath, 1);
                return gameDisksFullPath;
            }

            if (IsMultiDiskPattern2(gameFullPath))
            {
                // case 2. <game> Disk01.zip, <game> Disk02.zip etc.
                gameDisksFullPath = GetDisksFullPath(gameFullPath, 2);
                return gameDisksFullPath;
            }
            if (IsMultiDiskPattern3(gameFullPath))
            {
                // case 3. <game> (Disk 1 of 2).zip, <game> (Disk 2 of 2).zip etc.
                gameDisksFullPath = GetDisksFullPath(gameFullPath, 3);
                return gameDisksFullPath;
            }
            if (IsMultiDiskPattern4(gameFullPath))
            {
                // case 4. <game> (Disk 01 of 11).zip, <game> (Disk 02 of 11).zip etc.
                gameDisksFullPath = GetDisksFullPath(gameFullPath, 4);
                return gameDisksFullPath;
            }
            if (IsMultiDiskPattern5(gameFullPath))
            {
                // case 5. <game>-1.zip, <game>-2.zip etc.
                gameDisksFullPath = GetDisksFullPath(gameFullPath, 5);
                return gameDisksFullPath;
            }
            // if none of the above matches, assume the game has only one disk
            gameDisksFullPath.Add(gameFullPath);
            return gameDisksFullPath;
        }

        private List<string> GetDisksFullPath(string gameFullPath, int method)
        {
            var diskNumber = 1;
            var gameDisksFullPath = new List<string>();

            if (method == 1)
                do
                {
                    gameDisksFullPath.Add(Regex.Replace(gameFullPath, @"Disk(\d{1})\.", "Disk" + diskNumber + "."));
                    diskNumber++;
                } while (
                    _gamesRepository.FilenameExists(Regex.Replace(gameFullPath, @"Disk(\d{1})\.",
                        "Disk" + diskNumber + ".")));

            if (method == 2)
                do
                {
                    gameDisksFullPath.Add(Regex.Replace(gameFullPath, @"Disk(\d{2})\.",
                        "Disk" + diskNumber.ToString("00") + "."));
                    diskNumber++;
                } while (
                    _gamesRepository.FilenameExists(Regex.Replace(gameFullPath, @"Disk(\d{2})\.",
                        "Disk" + diskNumber.ToString("00") + ".")));
            if (method == 3)
                do
                {
                    gameDisksFullPath.Add(Regex.Replace(gameFullPath, @"Disk\s(\d{1})\sof",
                        "Disk " + diskNumber + " of"));
                    diskNumber++;
                } while (
                    _gamesRepository.FilenameExists(Regex.Replace(gameFullPath, @"Disk\s(\d{1})\sof",
                        "Disk " + diskNumber + " of")));

            if (method == 4)
                do
                {
                    gameDisksFullPath.Add(Regex.Replace(gameFullPath, @"Disk\s(\d{2})\sof",
                        "Disk " + diskNumber.ToString("00") + " of"));
                    diskNumber++;
                } while (
                    _gamesRepository.FilenameExists(Regex.Replace(gameFullPath, @"Disk\s(\d{2})\sof",
                        "Disk " + diskNumber.ToString("00") + " of")));

            if (method == 5)
                do
                {
                    gameDisksFullPath.Add(Regex.Replace(gameFullPath, @"-(\d{1})\.", "-" + diskNumber + "."));
                    diskNumber++;
                } while (
                    _gamesRepository.FilenameExists(Regex.Replace(gameFullPath, @"-(\d{1})\.", "-" + diskNumber + ".")));

            return gameDisksFullPath;
        }

        private static bool IsMultiDiskPattern5(string gameFullPath)
        {
            return Regex.IsMatch(gameFullPath, @"-(\d{1})\....$");
        }

        private static bool IsMultiDiskPattern4(string gameFullPath)
        {
            int n;
            return Regex.IsMatch(gameFullPath, @"Disk\s(\d{2})\sof\s(\d{2})") &&
                   int.TryParse(
                       gameFullPath.Substring(
                           gameFullPath.IndexOf("Disk ", StringComparison.OrdinalIgnoreCase) + 5, 2), out n);
        }

        private static bool IsMultiDiskPattern3(string gameFullPath)
        {
            int n;
            return Regex.IsMatch(gameFullPath, @"Disk\s(\d{1})\sof") &&
                   int.TryParse(
                       gameFullPath.Substring(
                           gameFullPath.IndexOf("Disk ", StringComparison.OrdinalIgnoreCase) + 5, 1), out n);
        }

        private static bool IsMultiDiskPattern2(string gameFullPath)
        {
            int n;
            return Regex.IsMatch(gameFullPath, @"Disk(\d{2})\....$") &&
                   int.TryParse(
                       gameFullPath.Substring(
                           gameFullPath.IndexOf("Disk", StringComparison.OrdinalIgnoreCase) + 4, 2), out n);
        }

        private static bool IsMultiDiskPattern1(string gameFullPath)
        {
            int n;
            return Regex.IsMatch(gameFullPath, @"Disk(\d{1})\....$") &&
                   int.TryParse(
                       gameFullPath.Substring(
                           gameFullPath.IndexOf("Disk", StringComparison.OrdinalIgnoreCase) + 4, 1), out n);
        }
    }
}