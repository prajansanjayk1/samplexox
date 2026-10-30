using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public sealed class DnsProviderInfo
{
    public FamilyDnsProvider Provider { get; set; }
    public string DisplayName { get; set; } = "";
    public string PrimaryIpv4 { get; set; } = "";
    public string SecondaryIpv4 { get; set; } = "";
    public string PrimaryIpv6 { get; set; } = "";
    public string SecondaryIpv6 { get; set; } = "";
    public string Description { get; set; } = "";
    public string Limitations { get; set; } = "";
}

public static class DnsGuidanceService
{
    public static readonly Dictionary<FamilyDnsProvider, DnsProviderInfo> Providers = new()
    {
        [FamilyDnsProvider.CloudflareFamily] = new DnsProviderInfo
        {
            Provider = FamilyDnsProvider.CloudflareFamily,
            DisplayName = "Cloudflare 1.1.1.3 (Malware + Adult Filter)",
            PrimaryIpv4 = "1.1.1.3",
            SecondaryIpv4 = "1.0.0.3",
            PrimaryIpv6 = "2606:4700:4700::1113",
            SecondaryIpv6 = "2606:4700:4700::1003",
            Description = "Fastest global privacy resolver with automated adult-domain and phishing blocking.",
            Limitations = "Cannot inspect encrypted TLS payloads; browser DoH or VPN can bypass if enabled."
        },
        [FamilyDnsProvider.AdGuardFamily] = new DnsProviderInfo
        {
            Provider = FamilyDnsProvider.AdGuardFamily,
            DisplayName = "AdGuard DNS (Family Protection)",
            PrimaryIpv4 = "94.140.14.15",
            SecondaryIpv4 = "94.140.15.16",
            PrimaryIpv6 = "2a10:50c0::bad1:ff",
            SecondaryIpv6 = "2a10:50c0::bad2:ff",
            Description = "Filters explicit adult websites, enforces search-engine SafeSearch, and blocks ads.",
            Limitations = "May add slight resolution latency outside primary server regions."
        },
        [FamilyDnsProvider.CleanBrowsingFamily] = new DnsProviderInfo
        {
            Provider = FamilyDnsProvider.CleanBrowsingFamily,
            DisplayName = "CleanBrowsing (Family Filter)",
            PrimaryIpv4 = "185.228.168.168",
            SecondaryIpv4 = "185.228.169.168",
            PrimaryIpv6 = "2a0d:2a00:1::",
            SecondaryIpv6 = "2a0d:2a00:2::",
            Description = "Strict family filter designed for schools and households. Enforces Google/Bing SafeSearch.",
            Limitations = "Does not filter non-DNS traffic or direct IP connections."
        }
    };

    public static string GeneratePowerShellApplyCommand(FamilyDnsProvider provider, string adapterName = "Wi-Fi")
    {
        if (!Providers.TryGetValue(provider, out var info)) return "";

        return $"""
# Run as Administrator in PowerShell to configure Family-Safe DNS on '{adapterName}':
Set-DnsClientServerAddress -InterfaceAlias "{adapterName}" -ServerAddresses ("{info.PrimaryIpv4}", "{info.SecondaryIpv4}")
Clear-DnsClientCache
Write-Host "Family-Safe DNS active with {info.DisplayName}"
""";
    }

    public static string GeneratePowerShellResetCommand(string adapterName = "Wi-Fi")
    {
        return $"""
# Run as Administrator in PowerShell to restore automatic DHCP DNS:
Set-DnsClientServerAddress -InterfaceAlias "{adapterName}" -ResetServerAddresses
Clear-DnsClientCache
Write-Host "DNS restored to automatic DHCP default."
""";
    }
}
