using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Infrastructure.Network;

[SupportedOSPlatform("windows")]
public static class HostsFileService
{
    private static readonly string HostsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "drivers", "etc", "hosts");

    private const string BlockStartMarker = "# --- QUIETORA FOCUS ACTIVE BLOCK START ---";
    private const string BlockEndMarker = "# --- QUIETORA FOCUS ACTIVE BLOCK END ---";

    public static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows()) return false;
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryApplyHostsBlock(IEnumerable<BlockedWebsite> websites)
    {
        if (!IsAdministrator())
        {
            AppLogger.LogInfo("Hosts file blocking skipped: Application not running as Administrator.");
            return false;
        }

        try
        {
            if (!File.Exists(HostsFilePath)) return false;

            var activeSites = websites.Where(w => w.IsActive).ToList();
            if (activeSites.Count == 0) return true;

            // Remove any previous block first
            RemoveHostsBlock();

            var content = File.ReadAllText(HostsFilePath);
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(BlockStartMarker);
            sb.AppendLine("# Automatically managed during active focus session. Do not edit manually.");

            foreach (var site in activeSites)
            {
                var clean = site.Domain.Trim().ToLowerInvariant()
                    .Replace("https://", "")
                    .Replace("http://", "");

                if (string.IsNullOrWhiteSpace(clean)) continue;

                sb.AppendLine($"127.0.0.1 {clean}");
                if (!clean.StartsWith("www."))
                {
                    sb.AppendLine($"127.0.0.1 www.{clean}");
                }
                sb.AppendLine($"127.0.0.1 m.{clean}");
            }

            sb.AppendLine(BlockEndMarker);
            File.AppendAllText(HostsFilePath, sb.ToString());

            FlushDnsCache();
            AppLogger.LogInfo($"Successfully applied hosts file DNS block for {activeSites.Count} websites.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to apply hosts file block", ex);
            return false;
        }
    }

    public static bool RemoveHostsBlock()
    {
        if (!IsAdministrator()) return false;

        try
        {
            if (!File.Exists(HostsFilePath)) return false;

            var lines = File.ReadAllLines(HostsFilePath);
            var newLines = new List<string>();
            var inBlock = false;

            foreach (var line in lines)
            {
                if (line.Contains(BlockStartMarker))
                {
                    inBlock = true;
                    continue;
                }
                if (line.Contains(BlockEndMarker))
                {
                    inBlock = false;
                    continue;
                }
                if (!inBlock)
                {
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(HostsFilePath, newLines);
            FlushDnsCache();
            AppLogger.LogInfo("Cleanly removed hosts file DNS block.");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to clean up hosts file block", ex);
            return false;
        }
    }

    private static void FlushDnsCache()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit(3000);
        }
        catch { }
    }
}
