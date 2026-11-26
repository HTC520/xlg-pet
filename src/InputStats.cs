using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace StardewDesktopPet
{
    public class DailyInputEntry
    {
        public string DateKey;
        public long KeyPresses;
        public long MouseClicks;
    }

    public class InputStats
    {
        public long TotalKeyPresses { get; set; }
        public long TotalMouseClicks { get; set; }
        public long TodayKeyPresses { get; set; }
        public long TodayMouseClicks { get; set; }
        public long TodayLeftClicks { get; set; }
        public long TodayRightClicks { get; set; }
        public int BestMinuteOps { get; set; }
        public string TodayDate { get; private set; }
        public Dictionary<string, DailyInputEntry> History { get; private set; }

        private readonly Queue<DateTime> _recentMinuteEvents = new Queue<DateTime>();
        private bool _dirty;

        public InputStats()
        {
            TodayDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            History = new Dictionary<string, DailyInputEntry>();
        }

        private static string GetStatsPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StardewDesktopPet");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "stats.ini");
        }

        public void EnsureCurrentDay()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (TodayDate != today)
            {
                SyncTodayToHistory();
                TodayDate = today;
                TodayKeyPresses = 0;
                TodayMouseClicks = 0;
                TodayLeftClicks = 0;
                TodayRightClicks = 0;
                TrimOldHistory();
                _dirty = true;
            }
        }

        public void RecordActivity(int keyDelta, int leftClickDelta, int rightClickDelta, int middleClickDelta)
        {
            int totalMouseDelta = leftClickDelta + rightClickDelta + middleClickDelta;
            if (keyDelta <= 0 && totalMouseDelta <= 0) return;

            EnsureCurrentDay();

            TotalKeyPresses += keyDelta;
            TodayKeyPresses += keyDelta;

            TotalMouseClicks += totalMouseDelta;
            TodayMouseClicks += totalMouseDelta;
            TodayLeftClicks += leftClickDelta;
            TodayRightClicks += rightClickDelta;

            DateTime now = DateTime.UtcNow;
            int totalOps = keyDelta + totalMouseDelta;
            for (int i = 0; i < totalOps; i++)
            {
                _recentMinuteEvents.Enqueue(now);
            }
            PruneRecentMinute(now);
            if (_recentMinuteEvents.Count > BestMinuteOps)
            {
                BestMinuteOps = _recentMinuteEvents.Count;
            }

            SyncTodayToHistory();
            _dirty = true;
        }

        public int GetCurrentMinuteOps()
        {
            PruneRecentMinute(DateTime.UtcNow);
            return _recentMinuteEvents.Count;
        }

        private void PruneRecentMinute(DateTime now)
        {
            DateTime cutoff = now.AddSeconds(-60);
            while (_recentMinuteEvents.Count > 0 && _recentMinuteEvents.Peek() < cutoff)
            {
                _recentMinuteEvents.Dequeue();
            }
        }

        private void SyncTodayToHistory()
        {
            DailyInputEntry entry;
            if (!History.TryGetValue(TodayDate, out entry))
            {
                entry = new DailyInputEntry { DateKey = TodayDate };
                History[TodayDate] = entry;
            }
            entry.KeyPresses = TodayKeyPresses;
            entry.MouseClicks = TodayMouseClicks;
        }

        private void TrimOldHistory()
        {
            DateTime cutoff = DateTime.Today.AddDays(-13);
            List<string> toRemove = new List<string>();
            foreach (string key in History.Keys)
            {
                DateTime dt;
                if (DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    if (dt < cutoff) toRemove.Add(key);
                }
            }
            foreach (string k in toRemove)
            {
                History.Remove(k);
            }
        }

        public List<DailyInputEntry> GetLast7Days()
        {
            EnsureCurrentDay();
            SyncTodayToHistory();
            List<DailyInputEntry> list = new List<DailyInputEntry>();
            for (int i = 6; i >= 0; i--)
            {
                string key = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                DailyInputEntry entry;
                if (History.TryGetValue(key, out entry))
                {
                    list.Add(entry);
                }
                else
                {
                    list.Add(new DailyInputEntry { DateKey = key, KeyPresses = 0, MouseClicks = 0 });
                }
            }
            return list;
        }

        public string GetFarmerTitle()
        {
            long todayTotal = TodayKeyPresses + TodayMouseClicks;
            if (todayTotal >= 15000) return "★ 祝尼魔键盘舞王";
            if (todayTotal >= 8000) return "★ 社区中心速通大师";
            if (todayTotal >= 3500) return "★ 星露谷勤劳矿工";
            if (todayTotal >= 1000) return "★ 熟练的防风草农夫";
            if (todayTotal >= 200) return "★ 鹈鹕镇新居民";
            return "★ 刚下火车的见习农夫";
        }

        public void ResetAll()
        {
            TotalKeyPresses = 0;
            TotalMouseClicks = 0;
            TodayKeyPresses = 0;
            TodayMouseClicks = 0;
            TodayLeftClicks = 0;
            TodayRightClicks = 0;
            BestMinuteOps = 0;
            _recentMinuteEvents.Clear();
            History.Clear();
            TodayDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            SyncTodayToHistory();
            _dirty = true;
            Save(true);
        }

        public static InputStats Load()
        {
            InputStats stats = new InputStats();
            try
            {
                string path = GetStatsPath();
                if (!File.Exists(path)) return stats;

                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string line in lines)
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    long lv;
                    int iv;
                    switch (key)
                    {
                        case "TotalKeyPresses":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TotalKeyPresses = lv;
                            break;
                        case "TotalMouseClicks":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TotalMouseClicks = lv;
                            break;
                        case "TodayDate":
                            stats.TodayDate = val;
                            break;
                        case "TodayKeyPresses":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TodayKeyPresses = lv;
                            break;
                        case "TodayMouseClicks":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TodayMouseClicks = lv;
                            break;
                        case "TodayLeftClicks":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TodayLeftClicks = lv;
                            break;
                        case "TodayRightClicks":
                            if (long.TryParse(val, out lv) && lv >= 0) stats.TodayRightClicks = lv;
                            break;
                        case "BestMinuteOps":
                            if (int.TryParse(val, out iv) && iv >= 0) stats.BestMinuteOps = iv;
                            break;
                        default:
                            if (key.StartsWith("History_"))
                            {
                                string dateKey = key.Substring("History_".Length);
                                string[] parts = val.Split(',');
                                long hk = 0, hm = 0;
                                if (parts.Length >= 2 &&
                                    long.TryParse(parts[0], out hk) &&
                                    long.TryParse(parts[1], out hm))
                                {
                                    stats.History[dateKey] = new DailyInputEntry
                                    {
                                        DateKey = dateKey,
                                        KeyPresses = Math.Max(0, hk),
                                        MouseClicks = Math.Max(0, hm)
                                    };
                                }
                            }
                            break;
                    }
                }

                stats.EnsureCurrentDay();
                stats.SyncTodayToHistory();
                stats._dirty = false;
            }
            catch
            {
                // Return fresh stats if unreadable
            }
            return stats;
        }

        public void Save(bool force = false)
        {
            if (!force && !_dirty) return;
            try
            {
                EnsureCurrentDay();
                SyncTodayToHistory();
                TrimOldHistory();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("TotalKeyPresses=" + TotalKeyPresses);
                sb.AppendLine("TotalMouseClicks=" + TotalMouseClicks);
                sb.AppendLine("TodayDate=" + TodayDate);
                sb.AppendLine("TodayKeyPresses=" + TodayKeyPresses);
                sb.AppendLine("TodayMouseClicks=" + TodayMouseClicks);
                sb.AppendLine("TodayLeftClicks=" + TodayLeftClicks);
                sb.AppendLine("TodayRightClicks=" + TodayRightClicks);
                sb.AppendLine("BestMinuteOps=" + BestMinuteOps);

                foreach (var kv in History)
                {
                    sb.AppendLine(string.Format("History_{0}={1},{2}",
                        kv.Key, kv.Value.KeyPresses, kv.Value.MouseClicks));
                }

                File.WriteAllText(GetStatsPath(), sb.ToString(), Encoding.UTF8);
                _dirty = false;
            }
            catch
            {
                // Ignore IO errors
            }
        }
    }
}
