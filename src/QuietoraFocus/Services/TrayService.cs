using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using QuietoraFocus.Infrastructure.Logging;
using QuietoraFocus.ViewModels;

namespace QuietoraFocus.Services;

public sealed class TrayService : IDisposable
{
    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_INFO = 0x00000010;
    private const int NIIF_INFO = 0x00000001;

    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    private readonly Window _window;
    private readonly MainViewModel _vm;
    private IntPtr _hwnd;
    private bool _isCreated;
    private HwndSource? _hwndSource;

    public TrayService(MainViewModel vm, Window window)
    {
        _vm = vm;
        _window = window;

        if (_window.IsLoaded)
        {
            InitializeTray();
        }
        else
        {
            _window.Loaded += (_, _) => InitializeTray();
        }
    }

    private void InitializeTray()
    {
        var helper = new WindowInteropHelper(_window);
        _hwnd = helper.Handle;
        if (_hwnd == IntPtr.Zero) return;

        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);

        var data = CreateData();
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        data.uCallbackMessage = WM_TRAYICON;
        data.szTip = "Quietora Focus v2.0";
        data.hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION

        _isCreated = Shell_NotifyIcon(NIM_ADD, ref data);
        AppLogger.LogInfo($"Native Windows System Tray initialized (success={_isCreated})");
    }

    private NOTIFYICONDATA CreateData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
            hWnd = _hwnd,
            uID = 1,
            szTip = "",
            szInfo = "",
            szInfoTitle = ""
        };
    }

    public void ShowBalloon(string title, string message)
    {
        if (!_isCreated || _hwnd == IntPtr.Zero) return;

        var data = CreateData();
        data.uFlags = NIF_INFO;
        data.szInfoTitle = title;
        data.szInfo = message;
        data.dwInfoFlags = NIIF_INFO;

        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    public void UpdateTooltip(string text)
    {
        if (!_isCreated || _hwnd == IntPtr.Zero) return;

        var data = CreateData();
        data.uFlags = NIF_TIP;
        data.szTip = text.Length > 120 ? text[..120] : text;

        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            var mouseMsg = (int)lParam;
            if (mouseMsg == WM_LBUTTONDBLCLK)
            {
                RestoreWindow();
                handled = true;
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                // Right click: quick restore
                RestoreWindow();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void RestoreWindow()
    {
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }
        _window.Activate();
    }

    public void Dispose()
    {
        if (_isCreated && _hwnd != IntPtr.Zero)
        {
            var data = CreateData();
            Shell_NotifyIcon(NIM_DELETE, ref data);
            _isCreated = false;
        }
        _hwndSource?.RemoveHook(WndProc);
    }
}
