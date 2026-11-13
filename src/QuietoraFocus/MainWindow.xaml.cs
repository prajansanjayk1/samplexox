using System.ComponentModel;
using System.Windows;
using QuietoraFocus.Services;
using QuietoraFocus.ViewModels;

namespace QuietoraFocus;

public partial class MainWindow : Window
{
    private readonly TrayService? _trayService;

    public MainWindow()
    {
        InitializeComponent();

        if (DataContext is MainViewModel vm)
        {
            try
            {
                _trayService = new TrayService(vm, this);
            }
            catch
            {
                // Tray service is non-fatal if not supported
            }
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && (vm.IsSessionActive || vm.IsStrictMode))
        {
            e.Cancel = true;
            Hide();
            _trayService?.ShowBalloon("Quietora Focus Active", "Your focus session is running in the system tray. Double-click the tray icon anytime to restore the app.");
            return;
        }

        _trayService?.Dispose();
    }
}
