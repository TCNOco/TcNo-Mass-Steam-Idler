// USAGE:
// Create appids.txt in the same folder as install, with comma and/or line seperated Steam App IDs.
// This program assumes everything is free, or a free demo.
// Running the program will then try to steam://install/<AppId> each one to make sure everything is activated.
// When this is done, the program asks to be restarted, and upon restarting, every AppId is emulated and 'started/run' for X seconds, as defined by user.

using Steamworks;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using static TcNo_Mass_Steam_Idler.Funcs;

namespace TcNo_Mass_Steam_Idler
{
    public class Program
    {
        public static string version = "2022-06-18_00";
        public static void Main(string[] args)
        {
            // Set Working Directory to same as self
            Directory.SetCurrentDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "");

            // Verify all files exist
            verifySystem();

            // Automation
            // This makes the activation use the slow method, with no input required.
            // This also requires the appids.txt file to exist and be populated.
            var automatic = AutomaticModeCheck();

            if (!automatic)
            {
                CheckForUpdates(version);
                Console.WriteLine("This program is NOT running in automatic mode, and will require user input. This is faster for fewer activations.");
                Console.WriteLine("See instructions for TcNo-Mass-Steam-Idler-Auto.exe.");
            }

            // Is this the second run? Idling games? Get idle time from user
            int waitTime = 0;
            if (!IsActivationRun())
            {
                waitTime = GetWaitTime(automatic);

                // Delete skipcheck file, so program checks keys/activations on next launch
                if (File.Exists("skipcheck"))
                    File.Delete("skipcheck");
                if (File.Exists("skipcheck.txt"))
                    File.Delete("skipcheck.txt");


                // Games to idle:
                // "idle_queue.txt"
            }

            // Activate a ton of games
            if (IsActivationRun())
            {
                // Get list of AppIds
                // Either from appids.txt, or from user
                var appIds = GetAppIds();

                // Skip already idled games
                appIds = RemoveIdledGames(appIds);


                WriteSteamAppId("480"); // This is so Steam SteamAPI can connect and check existing activations.

                SteamAPI.Init();
                Console.WriteLine($"Checking of all ({appIds.Count}) games are activated on account (assuming all are demos/free).");
                Console.WriteLine();
                Console.WriteLine("If the Steam Store opens, instead of the install window (which you don't click anything on):");
                Console.WriteLine("- You've activated more than 50 in the hour, or\n- The appId is incorrect (not a free app/demo).");
                Console.WriteLine("Create 'skipcheck.txt' in the install folder to skip activating games, and idle instead.");
                Console.WriteLine();

                // Check if user can copy/paste codes to activate 50 games.
                // Because some may be invalid, instead copy 60, of which most will hopefully be activated.
                string copyCommands = "n";
                if (!automatic)
                {
                    Console.WriteLine();
                    Console.WriteLine("You can copy commands into Steam's console to activate a lot of games at once, instead of waiting for each one to finish.");
                    Console.WriteLine("Are you comfortable copy/pasting commands?");
                    Console.Write("Y / N: ");
                    copyCommands = Console.ReadLine();
                }

                var (invalidIds, activatedIds, notActivatedIds) = CheckAppList(appIds);
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

                // Remove invalid IDs
                foreach (var a in invalidIds)
                    appIds.Remove(a);

                //// Nothing left to activate? Ask user to restart.
                //if (notActivatedIds.Count == 0)
                //{
                //    Console.WriteLine("All games activated. Restart to idle.");
                //    CreateSkipcheck();
                //    PressAnyKeyToClose();
                //}

                var commands = new List<string>();
                if (!automatic && copyCommands is not null && copyCommands.ToLower() == "y")
                {
                    var command = "app_license_request ";
                    // Get up to first 60 not activated games
                    for (int i = 0; i < Math.Min(60, notActivatedIds.Count); i++)
                    {
                        command += notActivatedIds[i] + " ";

                        if (i == 29 || i == Math.Min(60, notActivatedIds.Count))
                        {
                            // 30th game (or last in list), new command.
                            commands.Add(command);
                            command = "app_license_request ";
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("The Steam Console will now open. Copy/paste the commands below to activate up to the first 50 games (50 per hour limit)");
                    Console.WriteLine();

                    foreach (var c in commands) Console.WriteLine($"{c}\n");

                    StartProcess("steam://open/console");

                    Console.WriteLine("");
                    Console.WriteLine("Once commands are run in Steam Console, please restart the program. Press Enter to close.");
                    Console.ReadLine();

                    CreateSkipcheck();

                    var (_, _, gamesToIdle) = CheckAppList(notActivatedIds); // Get newly activated games

                    SaveList(gamesToIdle, "idle_queue.txt");
                    Console.ReadLine();
                    Environment.Exit(0);
                }


                // ELSE: Activate one by one automatically
                // This is default for automatic mode.
                var loopIterations = 0;
                var newlyActivatedApps = new List<string>();
                var notifiedAboutInstall = false;
                var newlyFailedAppIds = new List<string>();
                foreach (var a in notActivatedIds)
                {
                    loopIterations++;
                    // If not, and first activation: Let user know what the new window popping up is
                    if (!notifiedAboutInstall)
                    {
                        Console.WriteLine($"\nApps are not activated. Opening install dialogue for unowned apps. Close these if you want. You do NOT need to install games to idle them.\n");
                        notifiedAboutInstall = true;
                    }

                    // If 50 area already activated: There could be a limit, stopping new activations
                    // When this limit is active, the Steam Store opens, instead of the install dialog.
                    if (newlyActivatedApps.Count == 50)
                    {
                        Console.WriteLine("\nActivated 50 games. Stopping. Steam has a limit of activating 50 games per hour.");
                        break;
                    }

                    Console.WriteLine($"(Checking {loopIterations}/{notActivatedIds.Count}) if app {a} is not activated.");

                    // Ask Steam to open the install dialog, activating the game
                    StartProcess("steam://install/" + uint.Parse(a));
                    Thread.Sleep(5000);

                    if (IsGameActivated(a) == 1)
                        newlyActivatedApps.Add(a);
                    else
                        newlyFailedAppIds.Add(a);

                    if (newlyFailedAppIds.Count == 10)
                    {
                        Console.WriteLine("10 activations failed. You may have reached the 50 activation/hour limit. If not, remove some of these AppIDs:");
                        foreach (var f in newlyFailedAppIds)
                            Console.Write($"{f}, ");
                        break;
                    }
                }

                if (newlyActivatedApps.Count == 0)
                {
                    Console.WriteLine("No new games were activated. Wait a while for the 50 game activations/hour limit to refresh.");
                    if (!automatic) PressAnyKeyToClose();
                    Environment.Exit(12);
                }

                // Write games to idle to list, and restart.
                CreateSkipcheck();
                SaveList(newlyActivatedApps, "idle_queue.txt");

                Console.WriteLine("");

                if (automatic)
                {
                    Console.WriteLine("AUTO MODE: Manager is restarting app...");
                    Environment.Exit(50);
                }

                Console.WriteLine("Please restart the program to start idling.");
                PressAnyKeyToClose();
            }



            // Start idling games
            List<string> appsToIdle;
            if (File.Exists("idle_queue.txt"))
                appsToIdle = GetAppIds("idle_queue.txt");
            else
            {
                Console.WriteLine("No specific idle queue exists ('idle_queue.txt') attempting to idle all app ids.");
                appsToIdle = GetAppIds();
            }

            Console.WriteLine($"Starting idling! ({appsToIdle.Count})");
            var current = 0;
            foreach (var appId in appsToIdle)
            {
                current++;

                var a = appId.Trim();
                Console.WriteLine($"[{current}/{appsToIdle.Count}] Idling: {a}, for {waitTime} seconds...");

                // Start game.exe
                string sysFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = "idle.exe";
                pInfo.Arguments = (waitTime * 1000) + " " + appId;

                Process p = Process.Start(pInfo);
                p.WaitForExit();

                Console.WriteLine();
            }

            if (automatic)
            {
                Console.WriteLine("AUTO MODE: Manager is restarting app...");
                Environment.Exit(12);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}