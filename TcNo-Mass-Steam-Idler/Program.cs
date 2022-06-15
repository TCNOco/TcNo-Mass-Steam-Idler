// USAGE:
// Create appids.txt in the same folder as install, with comma and/or line seperated Steam App IDs.
// This program assumes everything is free, or a free demo.
// Running the program will then try to steam://install/<AppId> each one to make sure everything is activated.
// When this is done, the program asks to be restarted, and upon restarting, every AppId is emulated and 'started/run' for X seconds, as defined by user.

using Steamworks;
using System.Diagnostics;
using System.Globalization;
using System.Net;


// Check for updates.
const string version = "2022-06-15_01";

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




// Set Working Directory to same as self
Directory.SetCurrentDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "");


var appIds = new List<string>();

// Read appIds.txt lines into list, if it exists
if (File.Exists("appids.txt"))
{
    appIds = string.Join(",", File.ReadAllLines("appids.txt").ToList()).Replace("\r\n", ",").Replace("\n", ",").Replace(",,", ",").Split(',').ToList();
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

// Check if Steam is running
if (!SteamAPI.IsSteamRunning())
{
    Console.WriteLine("Steam not running. Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(0);
}


int waitTime = 0;
if (File.Exists("skipcheck"))
{
    //Console.WriteLine("\nEnter time to idle a game (seconds).\nIf too low, 32 games will idle and then SteamAPI will no longer respond for a while (timeout).");
    Console.WriteLine("\nEnter time to idle each game (seconds).");
    while (waitTime is 0)
    {
        Console.Write("Idle time (Any whole number): ");
        int.TryParse(Console.ReadLine(), out waitTime);
    }
}


if (!File.Exists("idle.exe"))
{
    Console.WriteLine("Can not find idle.exe. Please redownload the program.");
}


if (!File.Exists("skipcheck"))
{
    File.WriteAllText("steam_appid.txt", "480");
    SteamAPI.Init();
    Console.WriteLine($"Checking of all ({appIds.Count}) games are activated on account (assuming all are demos/free).");
    var invalidIds = new List<string>();
    var activatedIds = new List<string>();
    var anyPopups = false;
    var vCount = 0;
    foreach (var appId in appIds)
    {
        vCount++;
        uint aId = 0;
        uint.TryParse(appId, out aId);

        if (aId == 0)
        {
            Console.WriteLine("Invalid ID: " + appId);
            invalidIds.Add(appId);
            continue;
        }

        var activated = SteamApps.BIsSubscribedApp(new AppId_t(aId));
        if (!activated)
        {
            if (!anyPopups)
            {
                Console.WriteLine($"\nApps are not activated. Opening install dialogue for unowned apps. Close these if you want.\n");
                anyPopups = true;
            }

            Console.WriteLine($"(Checking {vCount}/{appIds.Count}) App {appId} is not activated.");

            var sActivate = new ProcessStartInfo();
            sActivate.FileName = "steam://install/" + uint.Parse(appId);
            sActivate.UseShellExecute = true;

            Process.Start(sActivate);
            Thread.Sleep(5000);

            if (SteamApps.BIsSubscribedApp(new AppId_t(aId))) activatedIds.Add(appId); // NEW: Checks if game activated after trying to. Will keep list up to date.
        }
        else
        {
            activatedIds.Add(appId);
        }
    }

    foreach (var iAd in invalidIds)
    {
        appIds.Remove(iAd);
    }

    File.Create("skipcheck");
    File.WriteAllText("appids_activated.txt", string.Join(",", activatedIds));

    Console.WriteLine("Please restart the program. Press Enter to close.");
    Console.ReadLine();
    Environment.Exit(0);
}
else
{
    File.Delete("skipcheck");
}


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

Console.WriteLine("Press any key to exit.");
Console.ReadKey();