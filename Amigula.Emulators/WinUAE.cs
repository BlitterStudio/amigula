using System;
using System.IO;
using Amigula.Domain.DTO;
using Amigula.Domain.Interfaces;

namespace Amigula.Emulators
{
    // ReSharper disable once InconsistentNaming
    public class WinUAE : IEmulatorRepository
    {

        public EmulatorDto GetEmulatorPaths()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var commonDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            var emulatorPathValues = new EmulatorDto();

            if (Directory.Exists(Path.Combine(programFilesPath, "WinUAE")))
            {
                // WinUAE was found in Program Files, check if Configurations exists under Public Documents or the WinUAE folder
                emulatorPathValues.EmulatorPath = Path.Combine(programFilesPath, "WinUAE\\WinUAE.exe");

                if (Directory.Exists(Path.Combine(commonDocumentsPath, "Amiga Files\\WinUAE\\Configurations")))
                    emulatorPathValues.ConfigurationFilesPath = Path.Combine(commonDocumentsPath, "Amiga Files\\WinUAE\\Configurations");
                else if (Directory.Exists(Path.Combine(programFilesPath, "WinUAE\\Configurations")))
                    emulatorPathValues.ConfigurationFilesPath = Path.Combine(programFilesPath, "WinUAE\\Configurations");
            }
            else
            // Do a secondary check in case our operating system is Windows XP 32-bit (and WinUAE is under Program Files)
            {
                var programFilesPath32 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if (!Directory.Exists(Path.Combine(programFilesPath32, "WinUAE"))) return emulatorPathValues;
                
                // WinUAE was found in Program Files, check if Configurations exists under Public Documents or the WinUAE folder
                emulatorPathValues.EmulatorPath = Path.Combine(programFilesPath32, "WinUAE\\WinUAE.exe");

                if (Directory.Exists(Path.Combine(commonDocumentsPath, "Amiga Files\\WinUAE\\Configurations")))
                    emulatorPathValues.ConfigurationFilesPath = Path.Combine(commonDocumentsPath, "Amiga Files\\WinUAE\\Configurations");
                else if (Directory.Exists(Path.Combine(programFilesPath32, "WinUAE\\Configurations")))
                    emulatorPathValues.ConfigurationFilesPath = Path.Combine(programFilesPath32, "WinUAE\\Configurations");
            }

            return emulatorPathValues;
        }
    }
}