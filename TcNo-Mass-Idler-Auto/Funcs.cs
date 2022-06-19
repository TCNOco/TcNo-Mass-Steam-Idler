using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TcNo_Mass_Idler_Auto
{
    internal class Funcs
    {
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
    }
}
