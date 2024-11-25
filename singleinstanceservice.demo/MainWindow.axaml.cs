using Avalonia.Controls;
using Avalonia.Interactivity;

namespace singleinstanceservice.demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void RestartHandler(object sender, RoutedEventArgs args)
    {
        ((App)Avalonia.Application.Current!).SingleInstanceManager!.Restart();
    }
}