using System.Diagnostics;
using System.Reflection;

namespace singleinstanceservice;

public class SingleInstanceManager
{
    private Mutex serviceInstanceMutex;
    private Menelabs.FileSystemSafeWatcher watcher;

    public event EventHandler ShutdownRequested;

    public event EventHandler<string[]> NewInstanceRequested;


    public SingleInstanceManager(string name)
    {
        var id = (name ?? Assembly.GetEntryAssembly().GetName().Name) + ".singleinstancemanager";
        var fileName = "." + id;
        var monitoredFile = Path.Combine(Path.GetTempPath(), fileName);

        Console.WriteLine("Common File : " + monitoredFile);

        if (Mutex.TryOpenExisting("Global\\" + id, out serviceInstanceMutex))
        {
            AlreadyRunning = true;
            File.WriteAllLines(monitoredFile, Environment.GetCommandLineArgs().Skip(1));
        }
        else
        {
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
        watcher.Dispose();
        ShutdownRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnMonitorFiledCreated(object sender, FileSystemEventArgs e)
    {
        if (File.Exists(e.FullPath))
        {
            this.NewInstanceRequested?.Invoke(this, File.ReadAllLines(e.FullPath));
            File.Delete(e.FullPath);
        }
    }
}
