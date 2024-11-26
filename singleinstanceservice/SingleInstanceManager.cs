using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace singleinstanceservice;

public class SingleInstanceManager
{
    private Mutex serviceInstanceMutex;
    private Menelabs.FileSystemSafeWatcher watcher;
    private readonly ILogger logger;

    public event EventHandler ShutdownRequested;

    public event EventHandler<NewInstanceRequestEventArgs> NewInstanceRequested;


    public SingleInstanceManager(ILogger logger, string name)
    {
        this.logger = logger;
        var id = (name ?? Assembly.GetEntryAssembly().GetName().Name) + ".singleinstancemanager";
        var fileName = "." + id;
        var monitoredFile = Path.Combine(Path.GetTempPath(), fileName);

        if (Mutex.TryOpenExisting("Global\\" + id, out serviceInstanceMutex))
        {
#pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger?.LogInformation("Application running, raising restart");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
            AlreadyRunning = true;
            File.WriteAllLines(monitoredFile, Environment.GetCommandLineArgs().Skip(1));
        }
        else
        {
            #pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger?.LogInformation("First instance, will monitor");
            #pragma warning restore CA1848 // Use the LoggerMessage delegates
            File.Delete(monitoredFile);
            watcher = new Menelabs.FileSystemSafeWatcher(Path.GetTempPath())
            {
                Filter = fileName
            };
            watcher.Created += OnMonitorFiledCreated;
            watcher.EnableRaisingEvents = true;


            AlreadyRunning = false;
            serviceInstanceMutex = new Mutex(true, "Global\\" + id);
        }
    }

    public bool AlreadyRunning { get; private set; }

    public void Restart()
    {
        #pragma warning disable CA1848 // Use the LoggerMessage delegates
        logger?.LogInformation("Restart requested");
        #pragma warning restore CA1848 

        var restartProcess = new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule.FileName,
        };

        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Length > 1)
        {
            restartProcess.Arguments = string.Join(" ", arguments.Skip(1).Select(_ => $"\"{_}\""));
        }

        Shutdown();

        Process.Start(restartProcess);
        Environment.Exit(0);
    }
    public void Shutdown()
    {
        #pragma warning disable CA1848 // Use the LoggerMessage delegates
        logger?.LogInformation("Shutdown requested");
        #pragma warning restore CA1848 
        watcher.Dispose();
        ShutdownRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMonitorFiledCreated(object sender, FileSystemEventArgs e)
    {
        if (File.Exists(e.FullPath))
        {
            #pragma warning disable CA1848 // Use the LoggerMessage delegates
            logger?.LogInformation("new instance requested");
            #pragma warning restore CA1848 
            this.NewInstanceRequested?.Invoke(
                this, 
                new NewInstanceRequestEventArgs(File.ReadAllLines(e.FullPath)));
            File.Delete(e.FullPath);
        }
    }
}
