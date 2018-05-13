using System.Collections;
using System.IO;
using System.Windows;
using static AngelicaArchiveManager.Core.Events;

namespace AngelicaArchiveManager.Core
{
    public class FSWatcher
    {
        public string Dir { get; set; } = "";
        public string TmpFile { get; set; } = "";

        private Hashtable Watchers = new Hashtable();

        public event FileWatcherCreated FileWatcherCreated;

        public FSWatcher(string path)
        {
            int i = 0;
            FileSystemWatcher watcher;
            foreach (string driveName in Directory.GetLogicalDrives())
            {
                if (Directory.Exists(driveName))
                {
                    watcher = new FileSystemWatcher
                    {
                        Filter = Path.GetFileName(path),
                        NotifyFilter = NotifyFilters.FileName,
                        IncludeSubdirectories = true,
                        Path = driveName
                    };
                    watcher.Created += Created;
                    watcher.EnableRaisingEvents = true;
                    Watchers.Add($"file_watcher{++i}", watcher);
                }
            }
        }

        private void Created(object sender, FileSystemEventArgs e)
        {
            FileWatcherCreated?.Invoke(sender, e);
        }
    }
}
