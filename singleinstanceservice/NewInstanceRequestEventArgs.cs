namespace singleinstanceservice;

public class NewInstanceRequestEventArgs : EventArgs
{
    public NewInstanceRequestEventArgs(string[] args)
    {
        Args = args;
    }

    public IReadOnlyCollection<string> Args { get; }
}
