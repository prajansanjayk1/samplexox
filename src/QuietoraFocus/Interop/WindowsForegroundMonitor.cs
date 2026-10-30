using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using QuietoraFocus.Infrastructure.Logging;

namespace QuietoraFocus.Interop;

public static class WindowsForegroundMonitor
{
    private const int SwMinimize = 6;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_W = 0x57;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint GA_ROOT = 2;
    private const uint GA_ROOTOWNER = 3;
    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public static readonly HashSet<string> ProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "quietorafocus", "explorer", "taskmgr", "dwm", "winlogon", "csrss", "lsass",
        "services", "system", "svchost", "smss", "wininit", "sihost", "msmpeng",
        "securityhealthsystray", "searchhost", "startmenuexperiencehost", "fontdrvhost",
        "applicationframehost", "shellhost", "textinputhost", "systemsettings", "cmd", "powershell"
    };

    public static bool TryGetForegroundProcess(out string processName, out string windowTitle, out IntPtr windowHandle)
    {
        processName = string.Empty;
        windowTitle = string.Empty;
        windowHandle = GetForegroundWindow();
        if (windowHandle == IntPtr.Zero) return false;

        // Ensure we operate on the top-level root window, not internal child render widgets
        var root = GetAncestor(windowHandle, GA_ROOT);
        if (root != IntPtr.Zero)
        {
            windowHandle = root;
        }

        GetWindowThreadProcessId(windowHandle, out var pid);
        if (pid == 0 || pid == Environment.ProcessId) return false;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            processName = process.ProcessName.ToLowerInvariant();

            var sb = new StringBuilder(1024);
            GetWindowText(windowHandle, sb, 1024);
            windowTitle = sb.ToString();

            // Fallback 1: Root owner window text if empty
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                var owner = GetAncestor(windowHandle, GA_ROOTOWNER);
                if (owner != IntPtr.Zero && owner != windowHandle)
                {
                    GetWindowText(owner, sb, 1024);
                    windowTitle = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(windowTitle))
                    {
                        windowHandle = owner;
                    }
                }
            }

            // Fallback 2: Process main window handle
            if (string.IsNullOrWhiteSpace(windowTitle) && process.MainWindowHandle != IntPtr.Zero)
            {
                GetWindowText(process.MainWindowHandle, sb, 1024);
                windowTitle = sb.ToString();
                if (string.IsNullOrWhiteSpace(windowTitle))
                {
                    windowTitle = process.MainWindowTitle;
                }
                if (!string.IsNullOrWhiteSpace(windowTitle))
                {
                    windowHandle = process.MainWindowHandle;
                }
            }

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public static void MinimizeWindow(IntPtr handle)
    {
        try
        {
            if (handle != IntPtr.Zero)
            {
                var root = GetAncestor(handle, GA_ROOT);
                if (root != IntPtr.Zero) handle = root;

                ShowWindowAsync(handle, SwMinimize);
                AppLogger.LogInfo($"Minimized window {handle}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Could not minimize window handle {handle}: {ex.Message}");
        }
    }

    public static void CloseActiveTab(IntPtr windowHandle)
    {
        try
        {
            if (windowHandle == IntPtr.Zero) return;

            var root = GetAncestor(windowHandle, GA_ROOT);
            if (root != IntPtr.Zero) windowHandle = root;

            // Ensure window is in foreground to receive keyboard shortcut
            SetForegroundWindow(windowHandle);
            Thread.Sleep(35);

            // Send Ctrl+W (with virtual keys and hardware scan codes for Chromium compatibility)
            keybd_event(VK_CONTROL, 0x1D, 0, UIntPtr.Zero);
            keybd_event(VK_W, 0x11, 0, UIntPtr.Zero);
            keybd_event(VK_W, 0x11, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0x1D, KEYEVENTF_KEYUP, UIntPtr.Zero);

            AppLogger.LogInfo($"Dispatched Ctrl+W to close distracting tab on window {windowHandle}.");
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Could not close tab on handle {windowHandle}: {ex.Message}");
        }
    }
}
