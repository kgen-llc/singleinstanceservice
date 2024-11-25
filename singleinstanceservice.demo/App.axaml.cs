using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace singleinstanceservice.demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        
        SingleInstanceManager = new ();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            SingleInstanceManager.ShutdownRequested +=
            (_, __) => desktop.Shutdown();
        }

        base.OnFrameworkInitializationCompleted();

    }

    public SingleInstanceManager? SingleInstanceManager {get; private set;}
}