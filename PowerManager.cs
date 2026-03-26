using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PowerPlanController;

public static class PowerManager
{
    // powercfg değişiklik ayarları
    private static void RunPowercfg(params string[] args)
    {
        var psi = new ProcessStartInfo("powercfg", string.Join(" ", args))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            Verb = "runas" // elevate if needed
        };
        try
        {
            var p = Process.Start(psi)!;
            p.WaitForExit(3000);
        }
        catch { /* ignore */ }
    }

    private static string RunPowercfgOutput(params string[] args)
    {
        var psi = new ProcessStartInfo("powercfg", string.Join(" ", args))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };
        try
        {
            var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return output;
        }
        catch { return ""; }
    }

    public static void Apply(int batteryScreen, int batterySleep,
                             int plugScreen,    int plugSleep)
    {
        RunPowercfg("/change", "monitor-timeout-dc",  batteryScreen.ToString());
        RunPowercfg("/change", "standby-timeout-dc",  batterySleep.ToString());
        RunPowercfg("/change", "monitor-timeout-ac",  plugScreen.ToString());
        RunPowercfg("/change", "standby-timeout-ac",  plugSleep.ToString());
    }

    public record Settings(int BatteryScreen, int BatterySleep,
                           int PlugScreen,    int PlugSleep);

    public static Settings GetCurrent()
    {
        string output = RunPowercfgOutput("/query");

        // Monitor GUID: 3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e
        // Sleep GUID  : 29f6c1db-86da-48c5-9fdb-f2b67b1f44da
        int batScreen = ParseMinutes(output, "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e", dc: true);
        int acScreen  = ParseMinutes(output, "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e", dc: false);
        int batSleep  = ParseMinutes(output, "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", dc: true);
        int acSleep   = ParseMinutes(output, "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", dc: false);

        return new Settings(batScreen, batSleep, acScreen, acSleep);
    }

    private static int ParseMinutes(string output, string guidHint, bool dc)
    {
        // Split on blocks
        var blocks = output.Split(new[] { "Power Setting GUID" },
                                  StringSplitOptions.RemoveEmptyEntries);
        foreach (var block in blocks)
        {
            if (!block.Contains(guidHint, StringComparison.OrdinalIgnoreCase))
                continue;

            string key = dc ? "Current DC Power Setting Index"
                            : "Current AC Power Setting Index";
            var m = Regex.Match(block, $@"{Regex.Escape(key)}:\s+(0x[0-9a-fA-F]+)",
                                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                long seconds = Convert.ToInt64(m.Groups[1].Value, 16);
                return (int)(seconds / 60);
            }
        }
        return 0;
    }
}
