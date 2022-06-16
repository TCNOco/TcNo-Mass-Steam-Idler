// USAGE:
// Create appids.txt in the same folder as install, with comma and/or line seperated Steam App IDs.
// This program assumes everything is free, or a free demo.
// Running the program will then try to steam://install/<AppId> each one to make sure everything is activated.
// When this is done, the program asks to be restarted, and upon restarting, every AppId is emulated and 'started/run' for X seconds, as defined by user.

using Steamworks;
using System.Diagnostics;
using System.Globalization;
using System.Net;


const string version = "2022-06-16_01";
// Set Working Directory to same as self
Directory.SetCurrentDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "");



// Automation
// This makes the activation use the slow method, with no input required.
// This also requires the appids.txt file to exist and be populated.
var automatic = false;
string[] arguments = Environment.GetCommandLineArgs();
if (arguments.Contains("--auto") || arguments.Contains("-a"))
{
    if (!File.Exists("appids.txt"))
    {
        Console.WriteLine("Can not run in automatic mode. The appids.txt file does not exist.");
        Environment.Exit(0);
    }
    automatic = true;
    Console.WriteLine("The manager is running in automatic mode.");
}


if (!automatic)
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

    Console.WriteLine("This program is NOT running in automatic mode, and will require user input. This is faster for fewer activations.");
    Console.WriteLine("See instructions for TcNo-Mass-Steam-Idler-Auto.exe.");
}



// Get list of AppIds
var appIds = new List<string>();

// Read appIds.txt lines into list, if it exists
if (File.Exists("appids.txt"))
{
    appIds = string.Join(",", File.ReadAllLines("appids.txt").ToList()).Replace("\r\n", ",").Replace("\n", ",").Replace(",,", ",").Split(',').ToList();
}

// Throw error if no appids and in automatic mode.
if (appIds.Count == 0 && automatic)
{
    Console.WriteLine("AUTO MODE: No app ids are in appids.txt. Operation will now pause. Press any key to continue...");
    Console.ReadKey();
    Environment.Exit(0);
}

if (appIds.Count == 0)
{
    Console.Write("Please enter a list of AppIDs (Comma seperated): ");
    var response = Console.ReadLine();
    if (response is null || response.Trim().Length < 1)
    {
        Console.WriteLine("No AppIDs entered. Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }
    appIds = response.Split(',').ToList();
    File.WriteAllText("appids.txt", string.Join(",", appIds));
}



// Skip already idled games
if (File.Exists("appids_idle_complete.txt"))
{
    // If exists: Read everything into array
    var originalCount = appIds.Count;
    Console.WriteLine("Removing already idled games...");
    var complete_appIds = string.Join(",", File.ReadAllLines("appids_idle_complete.txt").ToList()).Replace("\r\n", ",").Replace("\n", ",").Replace(",,", ",").Split(',').ToList();

    // Save list with duplicates removed
    File.WriteAllText("appids_idle_complete.txt", string.Join(",", complete_appIds));

    // Remove already idled games from todo list
    foreach (var completedApp in complete_appIds)
        if (appIds.Contains(completedApp))
            appIds.Remove(completedApp);

    // Alert user of change
    if (originalCount != appIds.Count)
        Console.WriteLine($"Some apps already idled. Old queue length: {originalCount}. New queue length: {appIds.Count}");
    else
        Console.WriteLine("No apps already idled are included in this list.");
}



// Check if Steam is running
if (!SteamAPI.IsSteamRunning())
{
    Console.WriteLine("Steam not running. Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(0);
}



// Get idle time from user, if skipcheck doesn't exist
int waitTime = 0;
if (File.Exists("skipcheck") || File.Exists("skipcheck.txt"))
{
    //Console.WriteLine("\nEnter time to idle a game (seconds).\nIf too low, 32 games will idle and then SteamAPI will no longer respond for a while (timeout).");
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
}



// Verify files
if (!File.Exists("idle.exe"))
{
    Console.WriteLine("Can not find idle.exe. Please redownload the program.");
}



// Activate a ton of games
if (!(File.Exists("skipcheck") || File.Exists("skipcheck.txt")))
{
    File.WriteAllText("steam_appid.txt", "480");
    SteamAPI.Init();
    Console.WriteLine($"Checking of all ({appIds.Count}) games are activated on account (assuming all are demos/free).");
    Console.WriteLine();
    Console.WriteLine("If the Steam Store opens, instead of the install window (which you don't click anything on):");
    Console.WriteLine("You've activated more than 50 in the hour, or the appId is incorrect (not a free demo).");
    Console.WriteLine("Create 'skipcheck.txt' in the install folder to skip activating games, and wait +- an hour.");
    Console.WriteLine();

    var invalidIds = new List<string>();
    var appIdsTodo = new List<string>();
    var activatedIds = new List<string>();
    var anyPopups = false;
    var vCount = 0; // Number of items checked
    var aCount = 0; // Number activated
    string copyCommands = "n";

    if (!automatic)
    {
        Console.WriteLine();
        Console.WriteLine("You can copy commands into Steam's console to activate a lot of games at once, instead of waiting for each one to finish.");
        Console.WriteLine("Are you comfortable copy/pasting commands?");
        Console.Write("Y / N: ");
        copyCommands = Console.ReadLine();
    }


    // COPY COMMANDS for user
    if (!automatic && copyCommands is not null && copyCommands.ToLower() == "y")
    {
        // User is comfortable copy/pasting commands into Steam's console.

        int todoCount = 0;
        // Clean list of AppIds
        foreach (var appId in appIds)
        {
            vCount++;
            aCount++;
            uint aId = 0;
            uint.TryParse(appId, out aId);

            // Check if AppID is valid
            if (aId == 0)
            {
                Console.WriteLine("Invalid ID: " + appId);
                invalidIds.Add(appId);
                appIds.Remove(appId);
                continue;
            }

            // Check if AppID is already activated
            var activated = SteamApps.BIsSubscribedApp(new AppId_t(aId));
            if (!activated)
            {
                todoCount++;
                appIdsTodo.Add(appId);
            }

            if (todoCount == 50)
            {
                Console.WriteLine("50 AppIds added to list.");
                break;
            }
        }

        var commands = new List<string>();
        var currentCount = 0;
        var currentCommand = "";
        // Add items to list to activate
        foreach (var a in appIdsTodo)
        {
            currentCount++;
            if (currentCount == 1)
                currentCommand = "app_license_request ";

            currentCommand += a + " ";

            if (currentCount == 30) // 30th entry
            {
                commands.Add(currentCommand);
                currentCommand = "app_license_request ";
            }
        }
        if (currentCount > 30)
            commands.Add(currentCommand);



        Console.WriteLine();
        Console.WriteLine("The Steam Console will now open. Copy/paste the commands below to activate up to the first 50 games (50 per hour limit)");
        Console.WriteLine();

        foreach (var c in commands)
        {
            Console.WriteLine(c);
            Console.WriteLine();
        }

        // Run steam://open/console
        var sActivate = new ProcessStartInfo();
        sActivate.FileName = "steam://open/console";
        sActivate.UseShellExecute = true;

        Process.Start(sActivate);

        Console.WriteLine("");
        Console.WriteLine("Once commands are run in Steam Console, please restart the program. Press Enter to close.");
        Console.ReadLine();


        File.Create("skipcheck");
        var newlyActivated = new List<string>();
        // Check which have been activated, and add to list.
        foreach (var appId in appIds)
        {
            uint aId = 0;
            uint.TryParse(appId, out aId);

            // Check if AppID is valid
            if (aId == 0)
            {
                Console.WriteLine("Invalid ID: " + appId);
                invalidIds.Add(appId);
                appIds.Remove(appId);
                continue;
            }

            if (SteamApps.BIsSubscribedApp(new AppId_t(aId)))
            {
                newlyActivated.Add(appId);
            }
        }

        File.WriteAllText("appids_activated.txt", string.Join(",", newlyActivated));
        Environment.Exit(0);
    }


    var activatedThisLoop = 0;
    // ELSE: Activate one by one automatically
    // This is default for automatic mode.
    foreach (var appId in appIds)
    {
        vCount++;
        aCount++;
        uint aId = 0;
        uint.TryParse(appId, out aId);

        // Check if AppID is already activated
        var activated = SteamApps.BIsSubscribedApp(new AppId_t(aId));
        if (!activated)
        {
            // If not, and first activation: Let user know what the new window popping up is
            if (!anyPopups)
            {
                Console.WriteLine($"\nApps are not activated. Opening install dialogue for unowned apps. Close these if you want.\n");
                anyPopups = true;
            }

            // If 50 area already activated: There could be a limit, stopping new activations
            // When this limit is active, the Steam Store opens, instead of the install dialog.
            if (aCount == 50)
            {
                Console.WriteLine("");
                Console.WriteLine("Steam has a limit of activating 50 games per hour. Please wait a bit before activating more.");
                Console.WriteLine("It's time to idle these games now. Please restart this program.");

                File.Create("skipcheck");
                File.WriteAllText("appids_activated.txt", string.Join(",", activatedIds));

                Console.WriteLine("");

                if (automatic)
                {
                    Console.WriteLine("AUTO MODE: Manager is restarting app...");
                    Environment.Exit(50);
                }

                Console.WriteLine("Please restart the program. Press Enter to close.");
                Console.ReadLine();
                Environment.Exit(1);
            }

            Console.WriteLine($"(Checking {vCount}/{appIds.Count}) App {appId} is not activated.");

            // Ask Steam to open the install dialog, activating the game
            var sActivate = new ProcessStartInfo();
            sActivate.FileName = "steam://install/" + uint.Parse(appId);
            sActivate.UseShellExecute = true;

            Process.Start(sActivate);
            Thread.Sleep(5000);

            if (SteamApps.BIsSubscribedApp(new AppId_t(aId)))
            {
                activatedIds.Add(appId); // NEW: Checks if game activated after trying to. Will keep list up to date.
                activatedThisLoop++;
            }

        }
        else
        {
            // Else, add to list of already activated
            activatedIds.Add(appId);
        }
    }

    if (activatedThisLoop < 50)
    {
        Console.WriteLine($"{activatedThisLoop} of 50 activated. Trying to activate {activatedThisLoop} x 2 random AppIDs from list, just to make sure.");
        var unused = appIds;
        foreach (var used in activatedIds)
        {
            unused.Remove(used);
        }

        var rnd = new Random();
        var todoCount = (50 - activatedThisLoop) * 2;
        for (int x = 0; x < todoCount; x++)
        {
            var sActivate = new ProcessStartInfo();
            var currentID = "";
            if (unused.Count > 50 - todoCount)
                currentID = unused[rnd.Next(0, unused.Count - 1)];
            else
                currentID = unused[unused.Count - x];

            activatedIds.Add(currentID);
            sActivate.FileName = "steam://install/" + uint.Parse(currentID);
            sActivate.UseShellExecute = true;

            Process.Start(sActivate);
            Thread.Sleep(5000);
        }
    }

    // Remove invalid AppIDs from master AppID list.
    foreach (var iAd in invalidIds)
    {
        appIds.Remove(iAd);
    }

    // Write file to tell program to skip activation/check
    // Prompt user to reoopen
    File.Create("skipcheck");
    File.WriteAllText("appids_activated.txt", string.Join(",", activatedIds));

    if (automatic)
    {
        Console.WriteLine("AUTO MODE: Manager is restarting app...");
        Environment.Exit(50);
    }

    Console.WriteLine("Please restart the program. Press Enter to close.");
    Console.ReadLine();
    Environment.Exit(0);
}
else
{
    // Delete skipcheck file, so program checks keys/activations on next launch
    if (File.Exists("skipcheck"))
        File.Delete("skipcheck");
    if (File.Exists("skipcheck.txt"))
        File.Delete("skipcheck.txt");
}



// Start idling games
Console.WriteLine($"Starting idling! ({appIds.Count})");
var i = 0;
foreach (var appId in appIds)
{
    i++;

    var a = appId.Trim();
    Console.WriteLine($"[{i}/{appIds.Count}] Idling: {a}, for {waitTime} seconds...");

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