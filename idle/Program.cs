using Steamworks;
using System.Diagnostics;

int waitTime = 0;
int appId = 0;

// Get arguments
string[] arguments = Environment.GetCommandLineArgs();
foreach (var a in arguments)
{
    int value = 0;
    int.TryParse(a, out value);
    if (value == 0)
        continue;

    if (waitTime == 0)
    {
        waitTime = value;
        continue;
    }

    if (appId == 0)
    {
        appId = value;
        continue;
    }
}

Console.WriteLine("Idling game: " + appId);

File.WriteAllText("steam_appid.txt", appId.ToString());

var isInit = SteamAPI.Init();
var announced = 0;
while (!isInit)
{
    if (announced == 0)
    {
        Console.WriteLine("SteamAPI could not connect.\nWaiting 10 seconds, and trying again.");
        Thread.Sleep(10000);
    }
    else if (announced >= 1)
    {
        using (StreamWriter w = File.AppendText("appids_failed.txt"))
        {
            w.Write($"{appId},");
        }
        Console.WriteLine("SteamAPI could not connect again.\nSkipping this AppID. Writing to 'appids_failed.txt'.");
        break;
    }

    announced += 1;
    isInit = SteamAPI.Init();
}

Thread.Sleep(waitTime);