using System.Threading;
using System.Windows;

namespace CM.Gefaehrdungsbeurteilung;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstance = new Mutex(true, @"Local\CM-Gefaehrdungsbeurteilung", out _ownsMutex);
        if (!_ownsMutex)
        {
            MessageBox.Show("CM Gefährdungsbeurteilung ist bereits geöffnet.", "Bereits gestartet", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
