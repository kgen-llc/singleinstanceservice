using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace singleinstanceservice.demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            if(SingleInstanceManager != null)
            {
                SingleInstanceManager.ShutdownRequested +=
                (_, __) => desktop.Shutdown();
                SingleInstanceManager.NewInstanceRequested +=
                (_, __) => {
                    
                    Dispatcher.UIThread.Invoke(() => 
                    {
                        var newWindow = new MainWindow();
                        newWindow.Title += $":{desktop.Windows.Count}";
                        newWindow.Show();
                    });
                };
            }
        }
        base.OnFrameworkInitializationCompleted();

    }

    public static SingleInstanceManager? SingleInstanceManager {get; set;}
}