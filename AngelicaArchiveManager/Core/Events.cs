using System.IO;

namespace AngelicaArchiveManager.Core
{
   public class Events
    {
        public delegate void SetProgress(int val);
        public delegate void SetProgressMax(int val);
        public delegate void SetProgressNext();
        public delegate void LoadData(byte type);
        public delegate void CloseTab(object tab);
        public delegate void FileWatcherCreated(object sender, FileSystemEventArgs e);
    }
}
