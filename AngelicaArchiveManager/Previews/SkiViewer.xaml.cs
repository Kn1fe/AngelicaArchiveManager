using AngelicaArchiveManager.Core.ArchiveEngine;
using AngelicaArchiveManager.Interfaces;
using AngelicaArchiveManager.Previews.Models;
using System.Windows;

namespace AngelicaArchiveManager.Previews
{
    public partial class SkiViewer : Window, IPreviewWin
    {
        public IArchiveManager Manager { get; set; }
        public string Path { get; set; }
        public IArchiveEntry File { get; set; }

        public SkiViewer()
        {
            InitializeComponent();
        }

        public void Prepare()
        {
            SkiReader Ski = new SkiReader(Manager.GetFile(File))
            {
                Manager = Manager,
                ModelFilePath = Path
            };
            Model.Content = Ski.GetModel();
        }
    }
}
