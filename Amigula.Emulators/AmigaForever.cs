using System;
using System.Collections.Generic;
using System.IO;
using Amigula.Domain.DTO;
using Amigula.Domain.Helpers;
using Amigula.Domain.Interfaces;
using Microsoft.Win32;

namespace Amigula.Emulators
{
    public class AmigaForever : IEmulatorRepository
    {
        private static HashSet<string> ValidFilenameExtensions
        {
            get
            {
                var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".zip",
                    ".adz",
                    ".adf",
                    ".dms",
                    ".ipf"
                };
                return extensions;
            }
        }

        private static string[] AmigaForeverKeys
        {
            // These are the keys we're interested in, if Amiga Forever is installed
            // AmigaFiles: where the WinUAE configuration files will be
            // Path: where AmigaForever binaries (WinUAE) is installed
            get
            {
                var afKeys = new[] {"AmigaFiles", "Path"};
                return afKeys;
            }
        }

        private static string RegistryRootKey
        {
            get
            {
                // Detect whether we're running on a 64-bit OS, change the registry scope accordingly
                var rootKey = OsBitCheck.Is64BitOperatingSystem()
                    ? "SOFTWARE\\Wow6432Node\\CLoanto\\Amiga Forever"
                    : "SOFTWARE\\CLoanto\\Amiga Forever";
                return rootKey;
            }
        }

        public EmulatorDto GetEmulatorPaths()
        {
            var rootKey = RegistryRootKey;
            var afKeys = AmigaForeverKeys;

            return ReadKeysFromRegistry(rootKey, afKeys);
        }

        private static EmulatorDto ReadKeysFromRegistry(string rootKey, string[] afKeys)
        {
            var emulatorPathValues = new EmulatorDto();
            var patRegistry = Registry.LocalMachine.OpenSubKey(rootKey);
            if (patRegistry != null)
                foreach (var subKeyName in patRegistry.GetSubKeyNames())
                {
                    patRegistry = Registry.LocalMachine.OpenSubKey(rootKey + "\\" + subKeyName);
                    foreach (var afKey in afKeys)
                    {
                        if (patRegistry != null && (afKey == "AmigaFiles" && patRegistry.GetValue(afKey) != null))
                        {
                            emulatorPathValues.ConfigurationFilesPath =
                                Path.Combine(patRegistry.GetValue(afKey).ToString(),
                                    "WinUAE\\Configurations");
                        }
                        if (patRegistry == null || (afKey != "Path" || patRegistry.GetValue(afKey) == null)) continue;
                        emulatorPathValues.EmulatorPath = Path.Combine(patRegistry.GetValue(afKey).ToString(),
                            "WinUAE\\winuae.exe");
                    }
                }
            patRegistry?.Close();
            return emulatorPathValues;
        }
    }
}