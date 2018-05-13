using AngelicaArchiveManager.Controls.CustomFileDialog;
using AngelicaArchiveManager.Core.ArchiveEngine;
using System.Windows.Controls;

namespace AngelicaArchiveManager.Controls
{
    public partial class ArchiveDialog : ControlAddOnBase
    {
        public ArchiveKey Key
        {
            get => Settings.Keys[ArchiveType.SelectedIndex];
        }

        public ArchiveDialog()
        {
            InitializeComponent();
            foreach (var key in Settings.Keys)
                ArchiveType.Items.Add(key.Name);
            ArchiveType.SelectedIndex = 0;
        }
    }
}
