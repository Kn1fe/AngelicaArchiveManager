using System.Collections.Generic;
using AngelicaArchiveManager.Interfaces;

namespace AngelicaArchiveManager.Core.ArchiveEngine
{
    public interface IArchiveManager
    {
        List<IArchiveEntry> Files { get; set; }

        event Events.LoadData LoadData;
        event Events.SetProgress SetProgress;
        event Events.SetProgressMax SetProgressMax;
        event Events.SetProgressNext SetProgressNext;

        void AddFiles(List<string> files, string srcdir, string dstdir);
        void Defrag();
        byte[] GetFile(IArchiveEntry entry, bool reload = true);
        List<byte[]> GetFiles(List<IArchiveEntry> files);
        void ReadFileTable();
        void SaveFileTable(long filetable = -1);
        void UnpackFiles(string srcdir, List<IArchiveEntry> files, string dstdir);
    }
}