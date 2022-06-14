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

var isInit = SteamAPI.Init();
var announced = 0;
while (!isInit)
{
    if (announced == 0)
    {
        Console.WriteLine("SteamAPI could not connect. After 32, it's usually a timeout.\nWaiting 10 seconds, and trying again.");
        Thread.Sleep(10000);
    }
    else if (announced == 1)
    {
        Console.WriteLine("SteamAPI could not connect. After 32, it's usually a timeout.\nWaiting 30 seconds, and trying again.");
        Thread.Sleep(30000);
    }
    else if (announced == 2)
    {
        Console.WriteLine("SteamAPI could not connect. After 32, it's usually a timeout.\nWaiting 1 minutes, and trying again.");
        Thread.Sleep(60000);
    }
    else if (announced >= 3)
    {
        Console.WriteLine("SteamAPI could not connect. After 32, it's usually a timeout.\nWaiting 1 minutes, and trying again.");
        Thread.Sleep(120000);
    }

    announced += 1;
    isInit = SteamAPI.Init();
}

Thread.Sleep(waitTime);