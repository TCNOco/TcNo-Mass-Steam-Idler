using Steamworks;

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

SteamAPI.Init();
Thread.Sleep(waitTime);