namespace DeskPet.App;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        _mainWindow = new MainWindow();
        _mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _mainWindow?.DisposeTrayIcon();
        base.OnExit(e);
    }
}
