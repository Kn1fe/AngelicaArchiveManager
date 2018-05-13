using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AngelicaArchiveManager.Core.ArchiveEngine;
using MadMilkman.Ini;

namespace AngelicaArchiveManager
{
    public class Settings
    {
        private static IniFile Ini = new IniFile();
        public static List<ArchiveKey> Keys = new List<ArchiveKey>();
        public static int Language = 0;
        public static int CompressionLevel = 1;
        public static string LastDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static void Load()
        {
            Ini.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini"));
            CompressionLevel = Ini.Sections["GENERAL"].Keys["CompressionLevel"].Value.ToInt32();
            LastDirectory = Ini.Sections["GENERAL"].Keys["LastDirectory"].Value;
            foreach (IniSection section in Ini.Sections.Where(x => x.Name.Contains("KEYS")))
            {
                Keys.Add(new ArchiveKey
                {
                    Name = section.Keys["Name"].Value,
                    KEY_1 = section.Keys["KEY1"].Value.ToInt32(),
                    KEY_2 = section.Keys["KEY2"].Value.ToInt32(),
                    ASIG_1 = section.Keys["ASIG1"].Value.ToInt32(),
                    ASIG_2 = section.Keys["ASIG2"].Value.ToInt32(),
                    FSIG_1 = section.Keys["FSIG1"].Value.ToInt32(),
                    FSIG_2 = section.Keys["FSIG2"].Value.ToInt32()
                });
            }
        }

        public static void Save()
        {
            Ini.Sections["GENERAL"].Keys["Language"].Value = Language.ToString();
            Ini.Sections["GENERAL"].Keys["CompressionLevel"].Value = CompressionLevel.ToString();
            Ini.Sections["GENERAL"].Keys["LastDirectory"].Value = LastDirectory;
            var keys = Ini.Sections.Where(x => x.Name.StartsWith("KEYS")).ToList();
            foreach (var key in keys)
            {
                Ini.Sections.Remove(key.Name);
            }
            for (int i = 0; i < Keys.Count; ++i)
            {
                var section = new IniSection(Ini, $"KEYS{i}");
                section.Keys.Add("Name", Keys[i].Name);
                section.Keys.Add("KEY1", Keys[i].KEY_1.ToString());
                section.Keys.Add("KEY2", Keys[i].KEY_2.ToString());
                section.Keys.Add("ASIG1", Keys[i].ASIG_1.ToString());
                section.Keys.Add("ASIG2", Keys[i].ASIG_2.ToString());
                section.Keys.Add("FSIG1", Keys[i].FSIG_1.ToString());
                section.Keys.Add("FSIG2", Keys[i].FSIG_2.ToString());
                Ini.Sections.Add(section);
            }
            Ini.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini"));
        }
    }
}
