﻿using AngelicaArchiveManager.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using static AngelicaArchiveManager.Core.Events;

namespace AngelicaArchiveManager.Core.ArchiveEngine
{
    public class ArchiveManager : IArchiveManager
    {
        private string Path { get; set; } = "";
        public List<IArchiveEntry> Files { get; set; } = new List<IArchiveEntry>();
        private ArchiveStream Stream { get; set; }
        private ArchiveKey Key { get; set; }
        public ArchiveVersion Version { get; set; }

        public event SetProgress SetProgress;
        public event SetProgressMax SetProgressMax;
        public event SetProgressNext SetProgressNext;
        public event LoadData LoadData;

        public ArchiveManager(string path, ArchiveKey key, bool detect_version = true)
        {
            Path = path;
            Key = key;
            Stream = new ArchiveStream(path);
            if (detect_version)
            {
                Stream.Reopen(true);
                Stream.Seek(-4, SeekOrigin.End);
                short version = Stream.ReadInt16();
                switch (version)
                {
                    case 2:
                        Version = ArchiveVersion.V2;
                        break;
                    case 3:
                        Version = ArchiveVersion.V3;
                        break;
                    default:
                        MessageBox.Show("Unknown archive type");
                        break;
                }
                Stream.Close();
            }
        }

        public void ReadFileTable()
        {
            switch (Version)
            {
                case ArchiveVersion.V2:
                    ReadFileTableV2();
                    break;
                case ArchiveVersion.V3:
                    ReadFileTableV3();
                    break;
                default:
                    MessageBox.Show("Unknown archive type");
                    break;
            }
        }

        public void SaveFileTable(long filetable = -1)
        {
            switch (Version)
            {
                case ArchiveVersion.V2:
                    SaveFileTableV2(filetable);
                    break;
                case ArchiveVersion.V3:
                    SaveFileTableV3(filetable);
                    break;
                default:
                    MessageBox.Show("Unknown archive type");
                    break;
            }
        }

        public void AddFiles(List<string> files, string srcdir, string dstdir)
        {
            switch (Version)
            {
                case ArchiveVersion.V2:
                    AddFilesV2(files, srcdir, dstdir);
                    break;
                case ArchiveVersion.V3:
                    AddFilesV3(files, srcdir, dstdir);
                    break;
                default:
                    MessageBox.Show("Unknown archive type");
                    break;
            }
        }

        public void Defrag()
        {
            switch (Version)
            {
                case ArchiveVersion.V2:
                    DefragV2();
                    break;
                case ArchiveVersion.V3:
                    DefragV3();
                    break;
                default:
                    MessageBox.Show("Unknown archive type");
                    break;
            }
        }

        #region V2
        public void ReadFileTableV2()
        {
            Stream.Reopen(true);
            Stream.Seek(-8, SeekOrigin.End);
            int FilesCount = Stream.ReadInt32();
            SetProgressMax?.Invoke(FilesCount);
            Stream.Seek(-272, SeekOrigin.End);
            long FileTableOffset = (long)((ulong)((uint)(Stream.ReadUInt32() ^ (ulong)Key.KEY_1)));
            Stream.Seek(FileTableOffset, SeekOrigin.Begin);
            BinaryReader TableStream = new BinaryReader(new MemoryStream(Stream.ReadBytes((int)(Stream.GetLenght() - FileTableOffset - 280))));
            for (int i = 0; i < FilesCount; ++i)
            {
                SetProgressNext?.Invoke();
                int EntrySize = TableStream.ReadInt32() ^ Key.KEY_1;
                TableStream.ReadInt32();
                Files.Add(new ArchiveEntryV2(TableStream.ReadBytes(EntrySize)));
            }
            SetProgress?.Invoke(0);
            Stream.Close();
            LoadData?.Invoke(0);
        }

        public void SaveFileTableV2(long filetable = -1)
        {
            try
            {
                Stream.Reopen(false);
                long FileTableOffset = filetable;
                if (FileTableOffset == -1)
                {
                    Stream.Seek(-272, SeekOrigin.End);
                    FileTableOffset = (long)((ulong)((uint)(Stream.ReadUInt32() ^ (ulong)Key.KEY_1)));
                    Stream.Cut(FileTableOffset);
                }
                Stream.Seek(FileTableOffset, SeekOrigin.Begin);
                SetProgressMax?.Invoke(Files.Count);
                int cl = Settings.CompressionLevel;
                foreach (IArchiveEntry entry in Files)
                {
                    SetProgressNext?.Invoke();
                    byte[] data = entry.Write(cl);
                    Stream.WriteInt32(data.Length ^ Key.KEY_1);
                    Stream.WriteInt32(data.Length ^ Key.KEY_2);
                    Stream.WriteBytes(data);
                }
                Stream.WriteInt32(Key.ASIG_1);
                Stream.WriteInt16(2);
                Stream.WriteInt16(2);
                Stream.WriteUInt32((uint)(FileTableOffset ^ Key.KEY_1));
                Stream.WriteInt32(0);
                Stream.WriteBytes(Encoding.Default.GetBytes("Angelica File Package, Perfect World."));
                Stream.WriteBytes(new byte[215]);
                Stream.WriteInt32(Key.ASIG_2);
                Stream.WriteInt32(Files.Count);
                Stream.WriteInt16(2);
                Stream.WriteInt16(2);
                Stream.Seek(4, SeekOrigin.Begin);
                Stream.WriteUInt32((uint)Stream.GetLenght());
                SetProgress?.Invoke(0);
                Stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\n{e.Source}\n{e.StackTrace}");
            }
        }

        public void AddFilesV2(List<string> files, string srcdir, string dstdir)
        {
            Stream.Reopen(false);
            SetProgressMax?.Invoke(files.Count);
            int cl = Settings.CompressionLevel;
            Stream.Seek(-272, SeekOrigin.End);
            long current_end = (long)((ulong)((uint)(Stream.ReadUInt32() ^ (ulong)Key.KEY_1)));
            foreach (string file in files)
            {
                SetProgressNext?.Invoke();
                byte[] data = File.ReadAllBytes(file);
                int size = data.Length;
                byte[] compressed = Zlib.Compress(data, cl);
                if (compressed.Length < size)
                    data = compressed;
                string path = (dstdir + file.RemoveFirst(srcdir).RemoveFirstSeparator()).RemoveFirstSeparator();
                var entry = Files.Where(x => x.Path == path).ToList();
                if (entry.Count > 0)
                {
                    if (data.Length <= entry[0].CSize)
                    {
                        entry[0].Size = size;
                        entry[0].CSize = data.Length;
                        Stream.Seek(entry[0].Offset, SeekOrigin.Begin);
                        Stream.WriteBytes(data);
                    }
                    else
                    {
                        entry[0].Size = size;
                        entry[0].CSize = data.Length;
                        entry[0].Offset = current_end;
                        Stream.Seek(current_end, SeekOrigin.Begin);
                        current_end += data.Length;
                        Stream.WriteBytes(data);
                    }
                }
                else
                {
                    Files.Add(new ArchiveEntryV2()
                    {
                        Path = path,
                        Size = size,
                        CSize = data.Length,
                        Offset = current_end
                    });
                    Stream.Seek(current_end, SeekOrigin.Begin);
                    current_end += data.Length;
                    Stream.WriteBytes(data);
                }
            }
            SaveFileTable(current_end);
            SetProgress?.Invoke(0);
            LoadData?.Invoke(0);
            LoadData?.Invoke(1);
        }

        public void DefragV2()
        {
            Stream.Reopen(true);
            long oldsize = Stream.GetLenght();
            ArchiveManager am = new ArchiveManager(Path + ".defrag", Key, false);
            am.Stream.Reopen(false);
            am.Stream.WriteInt32(Key.FSIG_1);
            am.Stream.WriteInt32(0);
            am.Stream.WriteInt32(Key.FSIG_2);
            int cl = Settings.CompressionLevel;
            SetProgressMax?.Invoke(Files.Count);
            foreach (IArchiveEntry file in Files)
            {
                SetProgressNext?.Invoke();
                byte[] data = GetFile(file, false);
                byte[] compressed = Zlib.Compress(data, cl);
                if (compressed.Length >= data.Length)
                    compressed = data;
                file.Offset = am.Stream.Position;
                file.Size = data.Length;
                file.CSize = compressed.Length;
                am.Stream.WriteBytes(compressed);
            }
            am.Files = Files;
            am.SaveFileTable(am.Stream.Position);
            am.Stream.Close();
            File.Delete(Path);
            File.Move(Path + ".defrag", Path);
            long newsize = Stream.GetLenght();
            MessageBox.Show($"Old size: {oldsize}\nNew size: {newsize}");
            Stream.Close();
            ReadFileTable();
        }
        #endregion

        #region V3
        public void ReadFileTableV3()
        {
            Stream.Reopen(true);
            Stream.Seek(-8, SeekOrigin.End);
            int FilesCount = Stream.ReadInt32();
            SetProgressMax?.Invoke(FilesCount);
            Stream.Seek(-280, SeekOrigin.End);
            long FileTableOffset = Stream.ReadInt64() ^ Key.KEY_1;
            Stream.Seek(FileTableOffset, SeekOrigin.Begin);
            BinaryReader TableStream = new BinaryReader(new MemoryStream(Stream.ReadBytes((int)(Stream.GetLenght() - FileTableOffset - 288))));
            for (int i = 0; i < FilesCount; ++i)
            {
                SetProgressNext?.Invoke();
                int EntrySize = TableStream.ReadInt32() ^ Key.KEY_1;
                TableStream.ReadInt32();
                Files.Add(new ArchiveEntryV3(TableStream.ReadBytes(EntrySize)));
            }
            SetProgress?.Invoke(0);
            Stream.Close();
            LoadData?.Invoke(0);
        }

        public void SaveFileTableV3(long filetable = -1)
        {
            try
            {
                Stream.Reopen(false);
                long FileTableOffset = filetable;
                if (FileTableOffset == -1)
                {
                    Stream.Seek(-280, SeekOrigin.End);
                    FileTableOffset = Stream.ReadInt64() ^ Key.KEY_1;
                    Stream.Cut(FileTableOffset);
                }
                Stream.Seek(FileTableOffset, SeekOrigin.Begin);
                SetProgressMax?.Invoke(Files.Count);
                int cl = Settings.CompressionLevel;
                foreach (IArchiveEntry entry in Files)
                {
                    SetProgressNext?.Invoke();
                    byte[] data = entry.Write(cl);
                    Stream.WriteInt32(data.Length ^ Key.KEY_1);
                    Stream.WriteInt32(data.Length ^ Key.KEY_2);
                    Stream.WriteBytes(data);
                }
                Stream.WriteInt32(Key.ASIG_1);
                Stream.WriteInt16(3);
                Stream.WriteInt16(2);
                Stream.WriteInt64(FileTableOffset ^ Key.KEY_1);
                Stream.WriteInt32(0);
                Stream.WriteBytes(Encoding.Default.GetBytes("Angelica File Package, Perfect World."));
                Stream.WriteBytes(new byte[215]);
                Stream.WriteInt32(Key.ASIG_2);
                Stream.WriteInt32(0);
                Stream.WriteInt32(Files.Count);
                Stream.WriteInt16(3);
                Stream.WriteInt16(2);
                Stream.Seek(4, SeekOrigin.Begin);
                Stream.WriteInt64(Stream.GetLenght());
                Stream.Close();
                SetProgress?.Invoke(0);
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\n{e.Source}\n{e.StackTrace}");
            }
        }

        public void AddFilesV3(List<string> files, string srcdir, string dstdir)
        {
            Stream.Reopen(false);
            SetProgressMax?.Invoke(files.Count);
            int cl = Settings.CompressionLevel;
            Stream.Seek(-280, SeekOrigin.End);
            long current_end = Stream.ReadInt64() ^ Key.KEY_1;
            foreach (string file in files)
            {
                SetProgressNext?.Invoke();
                byte[] data = File.ReadAllBytes(file);
                int size = data.Length;
                byte[] compressed = Zlib.Compress(data, cl);
                if (compressed.Length < size)
                    data = compressed;
                string path = (dstdir + file.RemoveFirst(srcdir).RemoveFirstSeparator()).RemoveFirstSeparator();
                var entry = Files.Where(x => x.Path == path).ToList();
                if (entry.Count > 0)
                {
                    if (data.Length <= entry[0].CSize)
                    {
                        entry[0].Size = size;
                        entry[0].CSize = data.Length;
                        Stream.Seek(entry[0].Offset, SeekOrigin.Begin);
                        Stream.WriteBytes(data);
                    }
                    else
                    {
                        entry[0].Size = size;
                        entry[0].CSize = data.Length;
                        entry[0].Offset = current_end;
                        Stream.Seek(current_end, SeekOrigin.Begin);
                        current_end += data.Length;
                        Stream.WriteBytes(data);
                    }
                }
                else
                {
                    Files.Add(new ArchiveEntryV3()
                    {
                        Path = path,
                        Size = size,
                        CSize = data.Length,
                        Offset = current_end
                    });
                    Stream.Seek(current_end, SeekOrigin.Begin);
                    current_end += data.Length;
                    Stream.WriteBytes(data);
                }
            }
            SaveFileTable(current_end);
            SetProgress?.Invoke(0);
            LoadData?.Invoke(0);
            LoadData?.Invoke(1);
        }

        public void DefragV3()
        {
            Stream.Reopen(true);
            long oldsize = Stream.GetLenght();
            ArchiveManager am = new ArchiveManager(Path + ".defrag", Key, false)
            {
                Version = Version
            };
            am.Stream.Reopen(false);
            am.Stream.WriteInt32(Key.FSIG_1);
            am.Stream.WriteInt64(0);
            am.Stream.WriteInt32(Key.FSIG_2);
            int cl = Settings.CompressionLevel;
            SetProgressMax?.Invoke(Files.Count);
            foreach (IArchiveEntry file in Files)
            {
                SetProgressNext?.Invoke();
                byte[] data = GetFile(file, false);
                byte[] compressed = Zlib.Compress(data, cl);
                if (data.Length < compressed.Length)
                    compressed = data;
                file.Offset = am.Stream.Position;
                file.Size = data.Length;
                file.CSize = compressed.Length;
                am.Stream.WriteBytes(compressed);
            }
            am.Files = Files;
            am.SaveFileTable(am.Stream.Position);
            am.Stream.Close();
            Stream.Close();
            File.Delete(Path);
            File.Move(Path + ".defrag", Path);
            string pkx = Path.Replace(".pck", ".pkx");
            if (File.Exists(pkx))
            {
                File.Delete(pkx);
                File.Move(pkx + ".defrag", pkx);
            }
            ReadFileTable();
            Stream.Reopen(true);
            long newsize = Stream.GetLenght();
            MessageBox.Show($"Old size: {oldsize}\nNew size: {newsize}");
        }
        #endregion

        public void UnpackFiles(string srcdir, List<IArchiveEntry> files, string dstdir)
        {
            try
            {
                Stream.Reopen(true);
                SetProgressMax?.Invoke(files.Count);
                foreach (IArchiveEntry entry in files)
                {
                    SetProgressNext?.Invoke();
                    byte[] file = GetFile(entry, false);
                    string path = System.IO.Path.Combine(dstdir,
                        srcdir.Length > 2 ? entry.Path.RemoveFirst(srcdir.RemoveFirstSeparator()) : entry.Path);
                    string dir = System.IO.Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllBytes(path, file);
                }
                SetProgress?.Invoke(0);
                Stream.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message}\n{e.Source}\n{e.StackTrace}");
            }
        }

        public byte[] GetFile(IArchiveEntry entry, bool reload = true)
        {
            if (reload)
                Stream.Reopen(true);
            Stream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] file = Stream.ReadBytes(entry.CSize);
            if (entry.CSize < entry.Size)
                return Zlib.Decompress(file, entry.Size);
            else
                return file;
        }

        public List<byte[]> GetFiles(List<IArchiveEntry> files)
        {
            Stream.Reopen(true);
            SetProgressMax?.Invoke(files.Count);
            List<byte[]> fs = new List<byte[]>();
            foreach (IArchiveEntry entry in files)
            {
                SetProgressNext?.Invoke();
                fs.Add(GetFile(entry, false));
            }
            SetProgress?.Invoke(0);
            Stream.Close();
            return fs;
        }
    }
}