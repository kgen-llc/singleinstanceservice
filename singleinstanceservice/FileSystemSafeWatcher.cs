

namespace Menelabs
{

    using System.Timers;
    using System.Collections;
    using System.ComponentModel;
    using System.Collections.ObjectModel;

    /// <summary>
    /// This class wraps a FileSystemWatcher object. The class is not derived
    /// from FileSystemWatcher because most of the FileSystemWatcher methods
    /// are not virtual. The class was designed to resemble FileSystemWatcher class
    /// as much as possible so that you can use FileSystemSafeWatcher instead
    /// of FileSystemWatcher objects.
    /// FileSystemSafeWatcher will capture all events from the FileSystemWatcher object.
    /// The captured events will be delayed by at least ConsolidationInterval milliseconds in order
    /// to be able to eliminate duplicate events. When duplicate events are found, the last event
    /// is droped and the first event is fired (the reverse is not recomended because it could
    /// cause some events not be fired at all since the last event will become the first event and
    /// it won't fire a if a new similar event arrives imediately afterwards).
    /// </summary>
    public sealed class FileSystemSafeWatcher : IDisposable
    {
        private readonly FileSystemWatcher _fileSystemWatcher;

        /// <summary>
        /// Lock order is _enterThread, _events.SyncRoot
        /// </summary>
        private readonly object _enterThread = new object(); // Only one timer event is processed at any given moment
        private ArrayList _events;

        private System.Timers.Timer _serverTimer;
        private int _consolidationInterval = 100; // milliseconds

        #region Delegate to FileSystemWatcher

        public FileSystemSafeWatcher()
        {
            _fileSystemWatcher = new FileSystemWatcher();
            Initialize();
        }

        public FileSystemSafeWatcher(string path)
        {
            _fileSystemWatcher = new FileSystemWatcher(path);
            Initialize();
        }

        public FileSystemSafeWatcher(string path, string filter)
        {
            _fileSystemWatcher = new FileSystemWatcher(path, filter);
            Initialize();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the component is enabled.
        /// </summary>
        /// <value>true if the component is enabled; otherwise, false. The default is false. If you are using the component on a designer in Visual Studio 2005, the default is true.</value>
        public bool EnableRaisingEvents
        {
            get
            {
                return _fileSystemWatcher.EnableRaisingEvents;
            }
            set
            {
                _fileSystemWatcher.EnableRaisingEvents = value;
                if (value)
                {
                    _serverTimer.Start();
                }
                else
                {
                    _serverTimer.Stop();
                    _events.Clear();
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the filter string, used to determine what files are monitored in a directory.
        /// </summary>
        /// <value>The filter string. The default is "*.*" (Watches all files.)</value>
        public string Filter
        {
            get
            {
                return _fileSystemWatcher.Filter;
            }
            set
            {
                _fileSystemWatcher.Filter = value;
            }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether subdirectories within the specified path should be monitored.
        /// </summary>
        /// <value>true if you want to monitor subdirectories; otherwise, false. The default is false.</value>
        public bool IncludeSubdirectories
        {
            get
            {
                return _fileSystemWatcher.IncludeSubdirectories;
            }
            set
            {
                _fileSystemWatcher.IncludeSubdirectories = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the size of the internal buffer.
        /// </summary>
        /// <value>The internal buffer size. The default is 8192 (8K).</value>
        public int InternalBufferSize
        {
            get
            {
                return _fileSystemWatcher.InternalBufferSize;
            }
            set
            {
                _fileSystemWatcher.InternalBufferSize = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the type of changes to watch for.
        /// </summary>
        /// <value>One of the System.IO.NotifyFilters values. The default is the bitwise OR combination of LastWrite, FileName, and DirectoryName.</value>
        /// <exception cref="System.ArgumentException">The value is not a valid bitwise OR combination of the System.IO.NotifyFilters values.</exception>
        public NotifyFilters NotifyFilter
        {
            get
            {
                return _fileSystemWatcher.NotifyFilter;
            }
            set
            {
                _fileSystemWatcher.NotifyFilter = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the path of the directory to watch.
        /// </summary>
        /// <value>The path to monitor. The default is an empty string ("").</value>
        /// <exception cref="System.ArgumentException">The specified path contains wildcard characters.-or- The specified path contains invalid path characters.</exception>
        public string Path
        {
            get
            {
                return _fileSystemWatcher.Path;
            }
            set
            {
                _fileSystemWatcher.Path = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the object used to marshal the event handler calls issued as a result of a directory change.
        /// </summary>
        /// <value>The System.ComponentModel.ISynchronizeInvoke that represents the object used to marshal the event handler calls issued as a result of a directory change. The default is null.</value>
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                return _fileSystemWatcher.SynchronizingObject;
            }
            set
            {
                _fileSystemWatcher.SynchronizingObject = value;
            }
        }
        
        /// <summary>
        /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is changed.
        /// </summary>
        public event FileSystemEventHandler Changed;
        
        /// <summary>
        /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is created.
        /// </summary>
        public event FileSystemEventHandler Created;
        
        /// <summary>
        /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is deleted.
        /// </summary>
        public event FileSystemEventHandler Deleted;
        
        /// <summary>
        /// Occurs when the internal buffer overflows.
        /// </summary>
        public event ErrorEventHandler Error;
        
        /// <summary>
        /// Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path is renamed.
        /// </summary>
        public event RenamedEventHandler Renamed;

        /// <summary>
        /// Begins the initialization of a System.IO.FileSystemWatcher used on a form or used by another component. The initialization occurs at run time.
        /// </summary>
        public void BeginInit()
        {
            _fileSystemWatcher.BeginInit();
        }
        
        /// <summary>
        /// Releases the unmanaged resources used by the System.IO.FileSystemWatcher and optionally releases the managed resources.
        /// </summary>
        public void Dispose()
        {
            Uninitialize();
        }
        
        /// <summary>
        /// Ends the initialization of a System.IO.FileSystemWatcher used on a form or used by another component. The initialization occurs at run time.
        /// </summary>
        public void EndInit()
        {
            _fileSystemWatcher.EndInit();
        }
        
        /// <summary>
        /// Raises the System.IO.FileSystemWatcher.Changed event.
        /// </summary>
        /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
        private void OnChanged(FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the System.IO.FileSystemWatcher.Created event.
        /// </summary>
        /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
        private void OnCreated(FileSystemEventArgs e)
        {
            Created?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the System.IO.FileSystemWatcher.Deleted event.
        /// </summary>
        /// <param name="e">A System.IO.FileSystemEventArgs that contains the event data.</param>
        private void OnDeleted(FileSystemEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the System.IO.FileSystemWatcher.Error event.
        /// </summary>
        /// <param name="e">An System.IO.ErrorEventArgs that contains the event data.</param>
        private void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }
        
        /// <summary>
        /// Raises the System.IO.FileSystemWatcher.Renamed event.
        /// </summary>
        /// <param name="e">A System.IO.RenamedEventArgs that contains the event data.</param>
        private void OnRenamed(RenamedEventArgs e)
        {
            Renamed?.Invoke(this, e);
        }
        
        /// <summary>
        /// A synchronous method that returns a structure that contains specific information on the change that occurred, given the type of change you want to monitor.
        /// </summary>
        /// <param name="changeType">The System.IO.WatcherChangeTypes to watch for.</param>
        /// <returns>A System.IO.WaitForChangedResult that contains specific information on the change that occurred.</returns>
        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType)
        {
            //TODO
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// A synchronous method that returns a structure that contains specific information on the change that occurred, given the type of change you want to monitor
        /// and the time (in milliseconds) to wait before timing out.
        /// </summary>
        /// <param name="changeType">The System.IO.WatcherChangeTypes to watch for.</param>
        /// <param name="timeout">The time (in milliseconds) to wait before timing out.</param>
        /// <returns>A System.IO.WaitForChangedResult that contains specific information on the change that occurred.</returns>
        public WaitForChangedResult WaitForChanged(WatcherChangeTypes changeType, int timeout)
        {
            //TODO
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation

        private void Initialize()
        {
            _events = ArrayList.Synchronized(new ArrayList(32));
            _fileSystemWatcher.Changed += new FileSystemEventHandler(this.FileSystemEventHandler);
            _fileSystemWatcher.Created += new FileSystemEventHandler(this.FileSystemEventHandler);
            _fileSystemWatcher.Deleted += new FileSystemEventHandler(this.FileSystemEventHandler);
            _fileSystemWatcher.Error += new ErrorEventHandler(this.ErrorEventHandler);
            _fileSystemWatcher.Renamed += new RenamedEventHandler(this.RenamedEventHandler);
            _serverTimer = new System.Timers.Timer(_consolidationInterval);
            _serverTimer.Elapsed += new ElapsedEventHandler(this.ElapsedEventHandler);
            _serverTimer.AutoReset = true;
            _serverTimer.Enabled = _fileSystemWatcher.EnableRaisingEvents;
        }

        private void Uninitialize()
        {
            if (_fileSystemWatcher != null)
                _fileSystemWatcher.Dispose();
            if (_serverTimer != null)
                _serverTimer.Dispose();
        }

        private void FileSystemEventHandler(object sender, FileSystemEventArgs e)
        {
            _events.Add(new DelayedEvent(e));
        }

        private void ErrorEventHandler(object sender, ErrorEventArgs e)
        {
            OnError(e);
        }

        private void RenamedEventHandler(object sender, RenamedEventArgs e)
        {
            _events.Add(new DelayedEvent(e));
        }

        private void ElapsedEventHandler(Object sender, ElapsedEventArgs e)
        {
            // We don't fire the events inside the lock. We will queue them here until
            // the code exits the locks.
            Queue eventsToBeFired = null;
            if (System.Threading.Monitor.TryEnter(_enterThread))
            {
                // Only one thread at a time is processing the events                
                try
                {
                    eventsToBeFired = new Queue(32);
                    // Lock the collection while processing the events
                    lock (_events.SyncRoot)
                    {
                        DelayedEvent current;
                        for (int i = 0; i < _events.Count; i++)
                        {
                            current = _events[i] as DelayedEvent;
                            if (current.Delayed)
                            {
                                // This event has been delayed already so we can fire it
                                // We just need to remove any duplicates
                                for (int j = i + 1; j < _events.Count; j++)
                                {
                                    if (current.IsDuplicate(_events[j]))
                                    {
                                        // Removing later duplicates
                                        _events.RemoveAt(j);
                                        j--; // Don't skip next event
                                    }
                                }

                                bool raiseEvent = true;
                                if ((current.Args.ChangeType == WatcherChangeTypes.Created || current.Args.ChangeType == WatcherChangeTypes.Changed) 
                                    && File.Exists(current.Args.FullPath))
                                {
                                    //check if the file has been completely copied (can be opened for read)
                                    FileStream stream = null;
                                    try
                                    {
                                        stream = File.Open(current.Args.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                                        // If this succeeds, the file is finished
                                    }
                                    catch (IOException)
                                    {
                                        raiseEvent = false;
                                    }
                                    finally
                                    {
                                        if (stream != null) stream.Close();
                                    }
                                }

                                if (raiseEvent)
                                {
                                    // Add the event to the list of events to be fired
                                    eventsToBeFired.Enqueue(current);
                                    // Remove it from the current list
                                    _events.RemoveAt(i);
                                    i--; // Don't skip next event
                                }
                            }
                            else
                            {
                                // This event was not delayed yet, so we will delay processing
                                // this event for at least one timer interval
                                current.Delayed = true;
                            }
                        }
                    }
                }
                finally
                {
                    System.Threading.Monitor.Exit(_enterThread);
                }
            }
            // else - this timer event was skipped, processing will happen during the next timer event

            // Now fire all the events if any events are in eventsToBeFired
            RaiseEvents(eventsToBeFired);
        }

        public int ConsolidationInterval
        {
            get
            {
                return _consolidationInterval;
            }
            set
            {
                _consolidationInterval = value;
                _serverTimer.Interval = value;
            }
        }

        private void RaiseEvents(Queue deQueue)
        {
            if ((deQueue != null) && (deQueue.Count > 0))
            {
                DelayedEvent de;
                while (deQueue.Count > 0)
                {
                    de = deQueue.Dequeue() as DelayedEvent;
                    switch (de.Args.ChangeType)
                    {
                        case WatcherChangeTypes.Changed:
                            OnChanged(de.Args);
                            break;
                        case WatcherChangeTypes.Created:
                            OnCreated(de.Args);
                            break;
                        case WatcherChangeTypes.Deleted:
                            OnDeleted(de.Args);
                            break;
                        case WatcherChangeTypes.Renamed:
                            OnRenamed(de.Args as RenamedEventArgs);
                            break;
                    }
                }
            }
        }
        #endregion
    }

}
