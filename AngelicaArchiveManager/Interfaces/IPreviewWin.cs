using AngelicaArchiveManager.Core.ArchiveEngine;

namespace AngelicaArchiveManager.Interfaces
{
    public interface IPreviewWin
    {
        IArchiveManager Manager { get; set; }
        IArchiveEntry File { get; set; }
        string Path { get;set;}

        void Prepare();
    }
}
