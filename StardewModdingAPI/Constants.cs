﻿using System;
using System.IO;
using System.Reflection;
using StardewValley;

namespace StardewModdingAPI
{
    /// <summary>
    /// Static class containing readonly values.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Stardew Valley's roaming app data location.
        /// %AppData%//StardewValley
        /// </summary>
        public static string DataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");

        public static string SavesPath => Path.Combine(DataPath, "Saves");

        private static string saveFolderName => PlayerNull ? string.Empty : Game1.player.name.RemoveNumerics() + "_" + Game1.uniqueIDForThisGame;
        public static string SaveFolderName => CurrentSavePathExists ? saveFolderName : "";

        private static string currentSavePath => PlayerNull ? string.Empty : Path.Combine(SavesPath, saveFolderName);
        public static string CurrentSavePath => CurrentSavePathExists ? currentSavePath : "";

        public static bool CurrentSavePathExists => Directory.Exists(currentSavePath);

        public static bool PlayerNull => !Game1.hasLoadedGame || Game1.player == null || string.IsNullOrEmpty(Game1.player.name);

        /// <summary>
        /// Execution path to execute the code.
        /// </summary>
        public static string ExecutionPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Title for the API console
        /// </summary>
        public static string ConsoleTitle => string.Format("Stardew Modding API Console - Version {0} - Mods Loaded: {1}", VersionString, ModsLoaded);

        /// <summary>
        /// Path for log files to be output to.
        /// %LocalAppData%//StardewValley//ErrorLogs
        /// </summary>
        public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs");

        public const int MajorVersion = 0;

        public const int MinorVersion = 38;

        public const int PatchVersion = 8;

        public const string Build = "Alpha";

        public static string VersionString => string.Format("{0}.{1}.{2} {3}", MajorVersion, MinorVersion, PatchVersion, Build);

        /// <summary>
        /// Not quite "constant", but it makes more sense for it to be here, at least for now
        /// </summary>
        public static int ModsLoaded = 0;
    }
}