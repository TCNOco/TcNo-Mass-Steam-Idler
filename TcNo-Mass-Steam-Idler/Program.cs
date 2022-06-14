using Steamworks;
using System.Diagnostics;

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
}

// Check if Steam is running
if (!SteamAPI.IsSteamRunning())
{
    Console.WriteLine("Steam not running. Press any key to exit...");
    Console.ReadKey();
    Environment.Exit(0);
}

int waitTime = 0;
while (waitTime is 0)
{
    Console.Write("Idle each game for (1-99) seconds: ");
    int.TryParse(Console.ReadLine(), out waitTime);
    waitTime = waitTime * 1000 + 200;
}


if (!File.Exists("idle.exe"))
{
    Console.WriteLine("Can not find idle.exe. Please redownload the program.");
}

Console.WriteLine("Starting idling!");
var i = 0;
foreach (var appId in appIds)
{
    i++;
    // Cleanup - Remove spaces from each appId
    var a = appId.Trim();
    File.WriteAllText("steam_appid.txt", a);

    // Start game.exe
    string sysFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
    ProcessStartInfo pInfo = new ProcessStartInfo();
    pInfo.FileName = "idle.exe";
    pInfo.Arguments = waitTime + " " + appId;

    Console.WriteLine($"[{i}/{appIds.Count}] Idling: {a}, for 5 seconds...");

    Process p = Process.Start(pInfo);
    p.WaitForExit();

    Console.WriteLine();
}

Console.ReadKey();