using Microsoft.Win32;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Interop;

public static class StartupManager
{
    private const string AppRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "QuietoraFocus";

    public static bool IsLaunchAtStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppRunKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Failed to read startup registry key: {ex.Message}");
            return false;
        }
    }

    public static void SetLaunchAtStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppRunKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    AppLogger.LogInfo("Configured Quietora Focus to launch at Windows startup.");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                AppLogger.LogInfo("Removed Quietora Focus from Windows startup.");
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError($"Failed to toggle startup preference: {ex.Message}", ex);
        }
    }
}
