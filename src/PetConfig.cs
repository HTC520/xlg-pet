using System;
using System.IO;
using System.Text;

namespace StardewDesktopPet
{
    public class PetConfig
    {
        public PetType Pet { get; set; }
        public int Scale { get; set; }
        public bool TopMost { get; set; }
        public bool AutoWalk { get; set; }
        public bool FreeRoam { get; set; }
        public bool ClickThrough { get; set; }
        public int MoveSpeed { get; set; }
        public int ActivityLevel { get; set; }
        public bool SoundEnabled { get; set; }
        public bool KeyboardDanceEnabled { get; set; }
        public int FriendshipPoints { get; set; }
        public int BreakReminderMinutes { get; set; }
        public int LastX { get; set; }
        public int LastY { get; set; }

        public PetConfig()
        {
            Pet = PetType.JunimoGreen;
            Scale = 4; // 16x16 * 4 = 64x64 px crisp pixel art
            TopMost = true;
            AutoWalk = true;
            FreeRoam = true;
            ClickThrough = false;
            MoveSpeed = 3;
            ActivityLevel = 5;
            SoundEnabled = true;
            KeyboardDanceEnabled = true;
            FriendshipPoints = 50;
            BreakReminderMinutes = 45;
            LastX = -1;
            LastY = -1;
        }

        private static string GetConfigPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StardewDesktopPet");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "config.ini");
        }

        public static PetConfig Load()
        {
            PetConfig cfg = new PetConfig();
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return cfg;

                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string line in lines)
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        case "Pet":
                            PetType pt;
                            if (Enum.TryParse(val, out pt)) cfg.Pet = pt;
                            break;
                        case "Scale":
                            int s;
                            if (int.TryParse(val, out s) && s >= 2 && s <= 6) cfg.Scale = s;
                            break;
                        case "TopMost":
                            bool tm;
                            if (bool.TryParse(val, out tm)) cfg.TopMost = tm;
                            break;
                        case "AutoWalk":
                            bool aw;
                            if (bool.TryParse(val, out aw)) cfg.AutoWalk = aw;
                            break;
                        case "FreeRoam":
                            bool fr;
                            if (bool.TryParse(val, out fr)) cfg.FreeRoam = fr;
                            break;
                        case "ClickThrough":
                            bool ct;
                            if (bool.TryParse(val, out ct)) cfg.ClickThrough = ct;
                            break;
                        case "MoveSpeed":
                            int ms;
                            if (int.TryParse(val, out ms) && ms >= 2 && ms <= 8) cfg.MoveSpeed = ms;
                            break;
                        case "ActivityLevel":
                            int al;
                            if (int.TryParse(val, out al) && al >= 1 && al <= 10) cfg.ActivityLevel = al;
                            break;
                        case "SoundEnabled":
                            bool se;
                            if (bool.TryParse(val, out se)) cfg.SoundEnabled = se;
                            break;
                        case "KeyboardDanceEnabled":
                            bool kd;
                            if (bool.TryParse(val, out kd)) cfg.KeyboardDanceEnabled = kd;
                            break;
                        case "FriendshipPoints":
                            int fp;
                            if (int.TryParse(val, out fp) && fp >= 0) cfg.FriendshipPoints = fp;
                            break;
                        case "BreakReminderMinutes":
                            int br;
                            if (int.TryParse(val, out br) && br >= 0) cfg.BreakReminderMinutes = br;
                            break;
                        case "LastX":
                            int lx;
                            if (int.TryParse(val, out lx)) cfg.LastX = lx;
                            break;
                        case "LastY":
                            int ly;
                            if (int.TryParse(val, out ly)) cfg.LastY = ly;
                            break;
                    }
                }
            }
            catch
            {
                // Return defaults if config is unreadable
            }
            return cfg;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Pet=" + Pet);
                sb.AppendLine("Scale=" + Scale);
                sb.AppendLine("TopMost=" + TopMost);
                sb.AppendLine("AutoWalk=" + AutoWalk);
                sb.AppendLine("FreeRoam=" + FreeRoam);
                sb.AppendLine("ClickThrough=" + ClickThrough);
                sb.AppendLine("MoveSpeed=" + MoveSpeed);
                sb.AppendLine("ActivityLevel=" + ActivityLevel);
                sb.AppendLine("SoundEnabled=" + SoundEnabled);
                sb.AppendLine("KeyboardDanceEnabled=" + KeyboardDanceEnabled);
                sb.AppendLine("FriendshipPoints=" + FriendshipPoints);
                sb.AppendLine("BreakReminderMinutes=" + BreakReminderMinutes);
                sb.AppendLine("LastX=" + LastX);
                sb.AppendLine("LastY=" + LastY);
                File.WriteAllText(GetConfigPath(), sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
