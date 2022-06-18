using System.Diagnostics;
using System.Globalization;

const string version = "2022-06-18_00-AUTO";
// Set Working Directory to same as self
Directory.SetCurrentDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "");

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



Console.WriteLine("Welcome to the TcNo Mass Idler - Automatic mode");
Console.WriteLine("This mode requires no user input. Activation will be slower but should be more than done over an hour.");
Console.WriteLine("Assuming appids.txt has valid IDs, everything will work well.");
Console.WriteLine();


var sActivate = new ProcessStartInfo();
sActivate.FileName = "TcNo-Mass-Steam-Idler.exe";
sActivate.UseShellExecute = false;
sActivate.Arguments = "--auto";
var maxTime = 9999999;


while (true)
{
    // Ask Steam to open the install dialog, activating the game
    var proc = Process.Start(sActivate);

    while (!proc.WaitForExit(maxTime));

    if (proc.ExitCode == 0)
    {
        Console.WriteLine("A fatal error occurred, and the program couldn't continue.");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }
    if (proc.ExitCode == 50)
    {
        Console.WriteLine("AUTO MODE MANAGER: Restarting now activations are complete.");
        Console.WriteLine("AUTO MODE MANAGER: Starting idle system.");
        maxTime = 6 * 50 * 1000; // 50 x 6 (seconds)
    }
    if (proc.ExitCode == 12)
    {
        Console.WriteLine("AUTO MODE MANAGER: Idling complete.");
        Console.WriteLine("AUTO MODE MANAGER: Waiting an hour before starting key activations again.");
        Console.WriteLine($"Time now: {DateTime.Now.ToString("MM/dd/yyyy h:mm tt")}. Will resume at: {DateTime.Now.AddHours(1).ToString("MM/dd/yyyy h:mm tt")}");
        maxTime = 9999999;
        Thread.Sleep(60 * 60 * 1000);
    }
}
