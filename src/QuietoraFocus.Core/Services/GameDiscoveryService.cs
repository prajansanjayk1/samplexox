using System.IO;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public static class GameDiscoveryService
{
    private static readonly HashSet<string> KnownGameProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cs2", "csgo", "valorant", "dota2", "leagueclient", "leagueclientux", "league of legends",
        "gta5", "minecraft", "javaw", "bedrock_server", "fortniteclient-win64-shipping", "epicgameslauncher",
        "r5apex", "overwatch", "genshinimpact", "honkaistarrail", "robloxplayerbeta", "rocketleague",
        "witcher3", "cyberpunk2077", "eldenring", "fifa23", "fc24", "forzahorizon5", "destiny2",
        "rainbowsix", "pubg", "tslgame", "hl2", "portal2", "terraria", "stardew valley", "among us"
    };

    public static bool IsKnownGameProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var clean = processName.Trim().ToLowerInvariant();
        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) clean = clean[..^4];
        return KnownGameProcessNames.Contains(clean);
    }

    public static List<DetectedGame> ScanInstalledGames()
    {
        var games = new List<DetectedGame>();

        // 1. Steam Common Directory
        var steamPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Steam\steamapps\common",
            @"D:\SteamLibrary\steamapps\common",
            @"E:\SteamLibrary\steamapps\common"
        };

        foreach (var steamDir in steamPaths)
        {
            try
            {
                if (Directory.Exists(steamDir))
                {
                    foreach (var gameFolder in Directory.GetDirectories(steamDir))
                    {
                        var name = Path.GetFileName(gameFolder);
                        var exes = Directory.GetFiles(gameFolder, "*.exe", SearchOption.TopDirectoryOnly);
                        var primaryExe = exes.FirstOrDefault() ?? Path.Combine(gameFolder, $"{name}.exe");

                        games.Add(new DetectedGame
                        {
                            GameName = name,
                            ProcessName = Path.GetFileNameWithoutExtension(primaryExe).ToLowerInvariant(),
                            ExecutablePath = primaryExe,
                            Launcher = "Steam",
                            IsBlocked = true
                        });
                    }
                }
            }
            catch { }
        }

        // 2. Riot Games Directory
        var riotPath = @"C:\Riot Games";
        try
        {
            if (Directory.Exists(riotPath))
            {
                foreach (var folder in Directory.GetDirectories(riotPath))
                {
                    var name = Path.GetFileName(folder);
                    games.Add(new DetectedGame
                    {
                        GameName = name,
                        ProcessName = name.ToLowerInvariant().Replace(" ", ""),
                        ExecutablePath = Path.Combine(folder, $"{name}.exe"),
                        Launcher = "Riot Games",
                        IsBlocked = true
                    });
                }
            }
        }
        catch { }

        // 3. Fallback popular standalone presets if none found
        if (games.Count == 0)
        {
            games.AddRange(new[]
            {
                new DetectedGame { GameName = "Counter-Strike 2", ProcessName = "cs2", Launcher = "Steam", IsBlocked = true },
                new DetectedGame { GameName = "Valorant", ProcessName = "valorant", Launcher = "Riot Games", IsBlocked = true },
                new DetectedGame { GameName = "League of Legends", ProcessName = "leagueclient", Launcher = "Riot Games", IsBlocked = true },
                new DetectedGame { GameName = "Dota 2", ProcessName = "dota2", Launcher = "Steam", IsBlocked = true },
                new DetectedGame { GameName = "Minecraft", ProcessName = "javaw", Launcher = "Standalone", IsBlocked = true },
                new DetectedGame { GameName = "Grand Theft Auto V", ProcessName = "gta5", Launcher = "Epic / Steam", IsBlocked = true }
            });
        }

        return games;
    }
}
