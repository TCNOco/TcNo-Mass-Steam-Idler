using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Steamworks;
using System.Diagnostics;

namespace TcNo_Mass_Steam_Idler
{
    public class Funcs
    {
        /// <summary>
        /// Pauses, and on key press: Closes the app.
        /// </summary>
        public static void PressAnyKeyToClose()
        {
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        public static void CheckForUpdates(string version)
        {
            // Check for Updates
            try
            {
                HttpClient HClient = new();
#if DEBUG
var latestVersion = HClient.GetStringAsync("https://tcno.co/Projects/MassIdler/api?debug&v=" + version).Result;
#else
                var latestVersion = HClient.GetStringAsync("https://tcno.co/Projects/MassIdler/api?v=" + version).Result;
#endif

                latestVersion = latestVersion.Replace("\r", "").Replace("\n", "");
                if (DateTime.TryParseExact(latestVersion, "yyyy-MM-dd_mm", null, DateTimeStyles.None, out var latestDate))
                {
                    if (DateTime.TryParseExact(version, "yyyy-MM-dd_mm", null, DateTimeStyles.None, out var currentDate))
                    {
                        if (!(latestDate.Equals(currentDate) || currentDate.Subtract(latestDate) > TimeSpan.Zero))
                            Console.WriteLine("An update is available! Check GitHub: https://github.com/TcNobo/TcNo-Mass-Steam-Idler/releases/latest.");
                    }
                    else
                        Console.WriteLine("Failed to check for update.");
                }
                else
                    Console.WriteLine("Failed to check for update.");
            }
            catch (Exception)
            {
                // Do nothing
            }
        }

        /// <summary>
        /// Checks if auto mode requested (in arguments)
        /// </summary>
        public static bool AutomaticModeCheck()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Contains("--auto") || arguments.Contains("-a"))
            {
                if (!File.Exists("appids.txt"))
                {
                    Console.WriteLine("Can not run in automatic mode. The appids.txt file does not exist.");
                    PressAnyKeyToClose();
                }
                Console.WriteLine("The manager is running in automatic mode.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets list of unique AppIds from file, or from user input.
        /// </summary>
        public static List<string> GetAppIds(string file = "appids.txt")
        {
            // Get list of AppIds
            var appIds = new List<string>();

            // Read appIds.txt lines into list, if it exists
            if (File.Exists(file))
            {
                appIds = string.Join(",", File.ReadAllLines(file).ToList()).Replace("\r\n", ",").Replace("\n", ",").Replace(",,", ",").Split(',').ToList();
            }

            // Manual entry
            if (file == "appids.txt" && appIds.Count == 0)
            {
                Console.Write("Please enter a list of AppIDs (Comma seperated): ");
                var response = Console.ReadLine();
                if (response is null || response.Trim().Length < 1)
                {
                    Console.WriteLine("No AppIDs entered.");
                    PressAnyKeyToClose();
                }
                appIds = response!.Split(',').ToList();
                File.WriteAllText("appids.txt", string.Join(",", appIds));
            }

            return appIds.Distinct().ToList();
        }

        /// <summary>
        /// Read idle completed file, and remove ids from appIds
        /// </summary>
        public static List<string> RemoveIdledGames(List<string> appIds)
        {
            if (!File.Exists("appids_idled.txt"))
                return appIds;

            // If exists: Read everything into array
            var originalCount = appIds.Count;
            Console.WriteLine("Removing already idled games...");
            var appIdsCompleted = string.Join(",", File.ReadAllLines("appids_idled.txt").ToList()).Replace("\r\n", ",").Replace("\n", ",").Replace(",,", ",").Split(',').Distinct().ToList();

            // Save list with duplicates removed
            File.WriteAllText("appids_idled.txt", string.Join(",", appIdsCompleted));

            // Remove already idled games from todo list
            foreach (var c in appIdsCompleted)
                if (appIds.Contains(c))
                    appIds.Remove(c);

            // Alert user of change
            if (originalCount != appIds.Count)
                Console.WriteLine($"Some apps already idled. Old queue length: {originalCount}. New queue length: {appIds.Count}");
            else
                Console.WriteLine("No apps already idled are included in this list.");

            return appIds;
        }

        public static int GetWaitTime(bool automatic)
        {
            var waitTime = 0;
            Console.WriteLine("\nEnter time to idle each game (seconds).");
            if (automatic)
            {
                Console.WriteLine("AUTO MODE: Idle time set to 5 seconds.");
                waitTime = 5; // If in auto mode, set time to 3.
            }
            else
            {
                while (waitTime is 0)
                {
                    Console.Write("Idle time (Any whole number): ");
                    int.TryParse(Console.ReadLine(), out waitTime);
                }
            }
            return waitTime;
        }

        /// <summary>
        /// Checks files, and if any are missing, prompts user to redownload software to repair.
        /// </summary>
        public static void verifySystem()
        {
            var missingFile = "";

            var requiredFiles = new List<string>() { "idle.exe", "idle.dll",
                "steam_api64.dll", "Steamworks.NET.dll",
                "TcNo-Mass-Steam-Idler.exe", "TcNo-Mass-Steam-Idler.dll" };

            foreach (var f in requiredFiles)
            {
                if (!File.Exists(f))
                {
                    missingFile = f;
                    break;
                }
            }

            if (missingFile == "")
            {
                // Check if Steam is running
                if (!SteamAPI.IsSteamRunning())
                {
                    Console.WriteLine("Steam not running. Press any key to exit...");
                    PressAnyKeyToClose();
                }

                // Otherwise, return if everything is working fine
                return;
            }

            Console.WriteLine($"Can not find {missingFile} Please redownload the program.");
            PressAnyKeyToClose();
        }

        public static bool IsActivationRun() => !(File.Exists("skipcheck") || File.Exists("skipcheck.txt"));

        /// <summary>
        /// Writes AppId to steam_appid.txt, for Steamworks to use.
        /// </summary>
        /// <param name="appId"></param>
        public static void WriteSteamAppId(string appId) =>
                File.WriteAllText("steam_appid.txt", appId);

        /// <summary>
        /// Checks if game is activated
        /// </summary>
        /// <returns>-1 if invalid, 1 if activated, - if not activated</returns>
        public static int IsGameActivated(string appId)
        {
            uint aId = 0;
            uint.TryParse(appId, out aId);

            // Check if AppID is valid
            if (aId == 0)
            {
                Console.WriteLine("Invalid ID: " + appId);
                return -1;
            }

            return SteamApps.BIsSubscribedApp(new AppId_t(aId)) ? 1 : 0;
        }

        public static void CreateSkipcheck() => File.Create("skipcheck");

        public static void StartProcess(string processPath)
        {
            var sActivate = new ProcessStartInfo();
            sActivate.FileName = processPath;
            sActivate.UseShellExecute = true;

            Process.Start(sActivate);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appIds"></param>
        /// <returns>List of invalidIds, activatedIds, and notAcitvatedIds</returns>
        public static (List<string>, List<string>, List<string>) CheckAppList(List<string> appIds)
        {
            var invalidIds = new List<string>(); // To remove later.
            var activatedIds = new List<string>();
            var notActivatedIds = new List<string>();
            // Check activation status of each appId
            foreach (var a in appIds)
            {
                var result = IsGameActivated(a);
                switch (result)
                {
                    case -1:
                        // Invalid
                        invalidIds.Add(a);
                        continue;
                    case 0:
                        notActivatedIds.Add(a);
                        continue;
                    case 1:
                        activatedIds.Add(a);
                        continue;
                }
            }

            return (invalidIds, activatedIds, notActivatedIds);
        }

        /// <summary>
        /// Save list of Ids into file.
        /// </summary>
        public static void SaveList(List<string> list, string fileName)
        {
            File.WriteAllText(fileName, string.Join(",", list));
        }
    }
}
