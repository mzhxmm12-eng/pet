using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DeskPet.App;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _mainWindow = new MainWindow();
        _mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _mainWindow?.DisposeTrayIcon();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.Exception);
        System.Windows.MessageBox.Show(
            $"DeskPet 启动或运行时遇到问题，错误日志已保存。\n\n{e.Exception.Message}",
            "DeskPet",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogUnhandledException(exception);
        }
    }

    private static void LogUnhandledException(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            var logPath = Path.Combine(logDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(logPath, exception.ToString());
        }
        catch
        {
            // Avoid recursive crashes while handling startup failures.
        }
    }
}
