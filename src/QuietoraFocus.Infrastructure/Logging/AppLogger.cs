using System.Text;

namespace QuietoraFocus.Infrastructure.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class AppLogger
{
    private static readonly object LockObj = new();
    private static string? _logFilePath;

    public static void Initialize(string? directory = null)
    {
        try
        {
            directory ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuietoraLabs", "QuietoraFocus", "logs");

            Directory.CreateDirectory(directory);
            _logFilePath = Path.Combine(directory, $"quietora_{DateTime.UtcNow:yyyyMMdd}.log");
            Log(LogLevel.Info, "Quietora Logger initialized. Privacy-safe diagnostic logging enabled.");
        }
        catch
        {
            // Logging failure should never crash the app
        }
    }

    public static void Log(LogLevel level, string message, Exception? ex = null)
    {
        try
        {
            if (_logFilePath == null) return;

            var sb = new StringBuilder();
            sb.Append($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff UTC}] [{level}] {message}");
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append($"   Exception: {ex.GetType().FullName}: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append($"   Stack: {ex.StackTrace}");
                }
            }

            lock (LockObj)
            {
                File.AppendAllText(_logFilePath, sb.ToString() + Environment.NewLine);
            }
        }
        catch
        {
            // Never rethrow logging errors
        }
    }

    public static void LogInfo(string message) => Log(LogLevel.Info, message);
    public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    public static void LogError(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
}
