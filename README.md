# singleinstanceservice
single instance pattern for a .net application.

It allows you to control the filetime:
- restart an application if required
- ensure only one instance of the application.

Note: does NOT use in very secure development

## For Avalonia

When used with an Avalonia application,

Consider the modification of the program as such :
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace singleinstanceservice.demo;

public partial class App : Application
{
    public static SingleInstanceManager? SingleInstanceManager {get; set;}

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

}
```

Notice the subscription on the ShutdownRequested and NewInstanceRequested.

Modify the program as such :
```csharp
[STAThread] 
public static void Main(string[] args)  {
    App.SingleInstanceManager = new (null, null);
    if(!App.SingleInstanceManager.AlreadyRunning)
    {
        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }
}
```
you can then for a button add the following handler :
```csharp
 public void RestartHandler(object sender, RoutedEventArgs args)
{
    App.SingleInstanceManager!.Restart();
}
```

Et voilà !

