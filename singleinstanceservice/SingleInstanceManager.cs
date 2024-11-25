using System.Diagnostics;
using System.Reflection;

namespace singleinstanceservice;

public class SingleInstanceManager
{
    public event EventHandler ShutdownRequested;

    public void Restart() {

        var restartProcess = new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule.FileName,
        };

        var arguments = Environment.GetCommandLineArgs();
        if(arguments.Length >1) 
        {
            restartProcess.Arguments = string.Join(" ", arguments.Skip(1).Select(_ => $"\"{_}\""));
        }

        ShutdownRequested?.Invoke(this, EventArgs.Empty);
        
        Process.Start(restartProcess);
        Environment.Exit(0);
    }
}
