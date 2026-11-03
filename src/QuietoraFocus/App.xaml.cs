using System.Windows;
using System.Windows.Threading;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Logging;
using QuietoraFocus.Infrastructure.Repositories;
using QuietoraFocus.Services;

namespace QuietoraFocus;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        AppLogger.Initialize();
        AppLogger.LogInfo("Quietora Focus v2.0 starting up...");

        // Centralized exception handling
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            var db = new QuietoraDatabase();
            var settings = new SettingsRepository(db);
            var isDark = settings.GetBool("dark_mode", true);
            ThemeManager.ApplyTheme(isDark);
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error applying initial theme or connecting database", ex);
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.LogError("Dispatcher unhandled exception caught", e.Exception);
        MessageBox.Show(
            "An unexpected error occurred in Quietora Focus.\n\n" + e.Exception.Message +
            "\n\nDetails have been recorded to your local diagnostic log file.",
            "Quietora Focus Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogger.LogError("Fatal AppDomain unhandled exception caught", ex);
        }
    }
}
