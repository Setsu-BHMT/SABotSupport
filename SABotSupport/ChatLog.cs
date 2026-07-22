using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;

using MySettings = SABotSupport.Properties.Settings;
using ChannelType = SABotSupport.ChatLine.ChannelType;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Configuration;
using CsvHelper.Configuration;

namespace SABotSupport
{
    internal class ChatLine : IEquatable<ChatLine>
    {
        [Flags]
        internal enum ChannelType : uint
        {
            Unknown = 0,
            Local = 1 << 0,
            Team = 1 << 1,
            Private = 1 << 2,
            Family = 1 << 3,
            Announce = 1 << 4,
            Job = 1 << 5,
            World = 1 << 6,
            Interworld = 1 << 7,
            Broadcast = 1 << 8,
            System = 1 << 9,
            Custom = 1 << 10,   //stuff that we inject into the chat stream
            IgnoreIfMerging = Announce | World | Interworld | Broadcast | System,
            IgnoreIfParsingPlayerInfo = Team | Family | Announce | Job | World | Interworld | Broadcast,
        }

        public DateTime Timestamp { get; }
        public string Text { get; }
        public ChannelType Channel { get; }
        public string Server { get; }
        public string Name { get; }
        public Color Color { get; }

        [CsvHelper.Configuration.Attributes.Ignore]
        public int SourceProcessID { get; }

        /// <summary>
        /// The preferred method is to call GetChatLines to process the entire chat buffer at once.
        /// This allows correct parsing of announcement messages since it spans 2 lines.
        /// Call the constructor only if you know what you are doing.
        /// </summary>
        public ChatLine(string text, int color, DateTime timestamp, string server, string name, ChannelType channel, int sourceProcessID)
        {
            Text = text;
            Timestamp = timestamp;
            Server = server;
            Name = name;
            Channel = channel;
            SourceProcessID = sourceProcessID;
            Color = GetColorFromInt(color);

            //shorten ────────── which is too long to display
            if (text.Contains("──────────"))
            {
                Text = text.Remove(text.LastIndexOf("──────────"));
            }
        }

        internal static readonly List<Color> ChatColors = new() {
            Color.White,
            Color.DarkTurquoise,
            Color.BlueViolet,
            Color.Blue,
            Color.Goldenrod,
            Color.LimeGreen,
            Color.Red,
            Color.DarkGray,
            Color.SkyBlue,
            Color.PowderBlue,
            Color.Black,
        };

        internal static Color GetColorFromInt(int code)
        {
            if (code == -1)
                return Color.DarkOrange;        //custom message from Update(), not a valid value in actual game memory
            else if (code < ChatColors.Count)
                return ChatColors[code];
            else
                return ChatColors.Last();       //default black
        }

        private static readonly Regex PRIVATE_MESSAGE_REGEX = new(@"^\[.+\]告訴你");
        private static readonly Regex BROADCAST_MESSAGE_REGEX = new(@"^(玩家 .+ 的神兵問世了|[^：]+ 玩家召喚了一隻|第 .+ 屆亂舞格鬥準備開始|[^：]+擊倒龍族首領本次攻城活動結束|[^：]+征服了英雄戰場|\[[^：]+來吉卡\]|玩家 .+ 被怪物塔塔主邀請進入聖地)");
        private static readonly Regex SYSTEM_MESSAGE_REGEX = new(@"^([^：]+的(耐久力|氣力)回復|(恩惠|滋潤)精靈\.\.不得施予非玩家敵方)");
        private static readonly string[] ANNOUNCE_MESSAGES = new string[] {
            "輸入/guide 指令集查詢系統內建", "遊戲僅是娛樂，切勿沉迷。", "若發現遊戲BUG請聯繫管理團隊進行處理",
            "玩家可藉由Discord平台更快速",
        };
        private static readonly string[] BROADCAST_MESSAGES = new string[] {
            "[廣播頻道]", "[戰積寶箱]", "「抓豬活動」", "月光寶盒重新出現", "[通告]",
            "[緊急事件]", "[怪物攻城事件]", "龍族首領在薩姆吉爾村做最後掙扎", "薩姆吉爾村似乎出現了攻城怪物", 
            "距離截止參加PK比賽時間還剩", "由於參加比賽人數過少", "由於無家族挑戰莊園",
            "遠方來的嘶吼聲:", "[爭強好勝寵物榜]","歡迎玩家", 
            "你看見吉魯島上突然發出一到聖光", "天空中劃過一顆流星", "天空中閃過一道流星",
            "一陣強風吹來，", "[春節活動特派員]", "[端午活動特派員]", "[中秋活動特派員]",
        };
        private static readonly string[] SYSTEM_MESSAGES = new string[] {
            "[系統]", "得到經驗:", "耐久力回復", "獲得學生", "周圍的地面已經滿了。", 
            "加入團隊！", "脫離團隊！", "團隊已解散！", "無法加入團隊。",
            "取得寵物，請稍後！", "存放道具中，請稍後！", "取出道具中，請稍後！",
            "提升學習經驗", "戰鬥中不可騎寵！", "戰鬥中無法使用EO。", "系統自動為您回復體力！",
            "[嗜血補氣]", "[自動加點]",
            //"無法交換名片。", "無人可以對戰。", "那裡沒有任何人。",
            //"[每日簽到]", "請稍候，連絡", "交易ＯＫ！", "真是抱歉，對方不願意跟你交易！", "擺攤中不得", "店家東西賣完了",
            //"學習經驗的能力提升", "什麼也沒有發生。", "無法拾獲該寵物。",
        };

        public static IEnumerable<ChatLine> GetChatLines(IEnumerable<(string Text, int Color)> lineTuples, string server, string name, int sourceProcessID)
        {
            bool processLineAsAnnouncement = false;
            int count = 0;

            foreach (var line in lineTuples)
            {
                var text = line.Text;
                ChannelType channel;

                //parse text to determine channel
                if (processLineAsAnnouncement)
                {
                    channel = ChannelType.Announce;
                    processLineAsAnnouncement = false;
                }
                else if (text.StartsWith("[隊]"))
                {
                    channel = ChannelType.Team;
                }
                else if (text.StartsWith("你告訴") || PRIVATE_MESSAGE_REGEX.IsMatch(text))
                {
                    channel = ChannelType.Private;
                }
                else if (text.StartsWith("[族]"))
                {
                    channel = ChannelType.Family;
                }
                else if (text.StartsWith("[職]"))
                {
                    channel = ChannelType.Job;
                }
                else if (text.StartsWith("[世]"))
                {
                    channel = ChannelType.World;
                }
                else if (text.StartsWith("[互動]") || text.StartsWith("[掛網]"))
                {
                    channel = ChannelType.Interworld;
                }
                else if (text.EndsWith("公告。"))
                {
                    channel = ChannelType.Announce;
                    processLineAsAnnouncement = true;   //for the next line
                }
                else if (ANNOUNCE_MESSAGES.Any(x => text.StartsWith(x)))
                {   //this is for when the chat buffer contains only the second line of the announcement
                    channel = ChannelType.Announce;
                }
                else if (BROADCAST_MESSAGES.Any(x => text.StartsWith(x)) || BROADCAST_MESSAGE_REGEX.IsMatch(text))
                {
                    channel = ChannelType.Broadcast;
                }
                else if (SYSTEM_MESSAGES.Any(x => text.StartsWith(x)) || SYSTEM_MESSAGE_REGEX.IsMatch(text))
                {
                    channel = ChannelType.System;
                }
                else
                {
                    channel = ChannelType.Local;
                }

                //we slightly nudge the timestamps of each line so that the order can be preserved when sorted by time
                //this is necessary because this loop is so fast it completes within 1 tick
                //each tick is 100ns which is much smaller than the update frequency so it shouldn't cause problems
                DateTime timestamp = new(DateTime.Now.Ticks + count++);

                yield return new ChatLine(text, line.Color, timestamp, server, name, channel, sourceProcessID);
            }
        }

        public override int GetHashCode()
        {
            int hash = 1009;
            int factor = 9176;

            unchecked
            {
                hash = hash * factor + Text.GetHashCode();
                //hash = hash * factor + Timestamp.Year.GetHashCode();
                //hash = hash * factor + Timestamp.Month.GetHashCode();
                hash = hash * factor + Timestamp.Day.GetHashCode();
                hash = hash * factor + Timestamp.Hour.GetHashCode();
                hash = hash * factor + Timestamp.Minute.GetHashCode();
                //hash = hash * factor + Timestamp.Second.GetHashCode();
            }

            return hash;
        }

        public bool Equals(ChatLine other)
            => Text.Equals(other?.Text) && (Timestamp - other.Timestamp).Duration().TotalSeconds < 1;
        
        public override bool Equals(object other)
            => (other is ChatLine line) && this.Equals(line);

        public static bool operator ==(ChatLine first, ChatLine second)
            => first.Equals(second);

        public static bool operator !=(ChatLine first, ChatLine second)
            => !(first == second);
    }

    internal static class ChatLog
    {
        private class ChatRecord
        {
            public List<ChatLine> History { get; } = new();
            public string[] LastChatBuffer { get; set; } = new string[20];
            public int LastChatIndex { get; set; } = 0;
            public bool IsOnline { get; set; } = true;
        }

        private static class ChatLogWriter
        {
            private sealed class ChatLineMap : ClassMap<ChatLine>
            {
                public ChatLineMap()
                {
                    Map(m => m.Timestamp);
                    Map(m => m.Text);
                    Map(m => m.Color).ConvertUsing(x => x.Color.Name);
                    Map(m => m.Channel);
                    Map(m => m.Server);
                    Map(m => m.Name);
                }
            }

            internal static void WriteRecords(IEnumerable<ChatLine> allAccountLines)
            {
                foreach (var lines in allAccountLines.GroupBy(x => new { x.Name, x.Timestamp.Day }).Where(x => !String.IsNullOrEmpty(x.Key.Name)))
                {
                    string filename = $"{lines.First().Timestamp:yyyy-MM-dd}_{lines.First().Name}.csv";

                    //build directory path
                    string path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                    path = Path.GetDirectoryName(path);
                    path = Path.GetDirectoryName(path);
                    path = Path.Combine(path, "ChatLog");
                    Directory.CreateDirectory(path);
                    path = Path.Combine(path, filename);

                    bool isNewFile = !File.Exists(path);

                    using var writer = new StreamWriter(path, true);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                    csv.Configuration.RegisterClassMap<ChatLineMap>();
                    csv.Configuration.HasHeaderRecord = isNewFile;

                    csv.WriteRecords(lines);
                }
            }
        }

        private static readonly ConcurrentDictionary<int, ChatRecord> allChatRecords = new();
        private static DateTime lastSaveTime = DateTime.Now;
        private static volatile bool isSaving = false;

        public delegate void NewChatAvailableEventHandler(object sender, NewChatAvailableEventArgs e);
        /// <summary>
        ///  always subscribe to the event before calling update, to make sure you always get the first update
        /// </summary>
        public static event NewChatAvailableEventHandler NewChatAvailableEvent = delegate { };  //avoids null check when firing events

        internal static void Update(int processID, DataPackage instanceData)
        {
            Debug.Assert(processID > 0, "processID cannot be 0");

            var gameData = instanceData.GameClientData;

            if (!gameData.IsInitialized)
                return;

            var record = allChatRecords.GetOrAdd(processID, new ChatRecord());
            var chatIndex = gameData.CurrentChatIndex;
            var chatBuffer = gameData.ChatBuffer;

            //check if we have new messages to process
            (var newLineTuples, var rotatedBuffer) = CompareChatBuffers(record.LastChatBuffer, record.LastChatIndex, chatBuffer, chatIndex);
            var newLines = ChatLine.GetChatLines(newLineTuples, gameData.Server, instanceData.Player.Name, processID).ToList();

            //check if we need to inject our own messages
            if (record.IsOnline && !instanceData.IsOnline && !String.IsNullOrEmpty(instanceData.Player.Name))
            {
                newLines.Add(new ChatLine($"{instanceData.Player.Name} 已離線", -1, new(DateTime.Now.Ticks + 21), 
                    gameData.Server, instanceData.Player.Name, ChannelType.Custom, processID));
            }
            else if (!record.IsOnline && instanceData.IsOnline)
            {
                newLines.Add(new ChatLine($"{instanceData.Player.Name} 已上線", -1, new(DateTime.Now.Ticks + 21),
                    gameData.Server, instanceData.Player.Name, ChannelType.Custom, processID));
            }

            //update the record
            record.History.AddRange(newLines);
            record.LastChatBuffer = rotatedBuffer;
            record.LastChatIndex = chatIndex;
            record.IsOnline = instanceData.IsOnline;

            //check if we need to save
            if (!isSaving && (DateTime.Now - lastSaveTime).Hours >= 2)
            {
                SaveAllChatLogs(2);
            }

            NewChatAvailableEvent(null, new NewChatAvailableEventArgs(processID, newLines, gameData.CurrentAccount, gameData.CurrentCharacter));
        }

        /// <remarks>
        /// firstBuffer is assumed to be already rotated into the order oldest -> newest.
        /// </remarks>
        internal static (List<(string Text, int Color)> newLineTuples, string[] rotatedSecondBuffer) CompareChatBuffers(string[] firstBuffer, int firstIndex, (string Text, int Color)[] secondBuffer, int secondIndex)
        {
            //re-order second buffer to have the latest index in the last position
            var newBuffer = secondBuffer.Rotate(secondIndex + 1);
            var rotatedBuffer = newBuffer.Select(x => x.Text).ToArray();

            //compare the buffers and return if there are no new chat messages
            if (Enumerable.SequenceEqual(firstBuffer, rotatedBuffer))
                return (new List<(string Text, int Color)>(), rotatedBuffer);

            //rotate the old buffer to match index with the new one and delete the oldest message
            var rotations = secondIndex - firstIndex;
            rotations = (rotations >= 0) ? rotations : rotations + firstBuffer.Length;
            var oldBuffer = firstBuffer.Rotate(rotations).ToArray();
            oldBuffer[oldBuffer.Length - 1] = String.Empty;

            //return only new messages
            return (newBuffer.SkipWhile((x, i) => x.Text == oldBuffer[i])
                             .Where(x => !String.IsNullOrEmpty(x.Text))
                             .ToList(), rotatedBuffer);
        }

        private static IEnumerable<T> Rotate<T>(this IEnumerable<T> sequence, int index)
            => sequence.Skip(index).Concat(sequence.Take(index));

        internal static void RequestChatRecord(int processID)
        {
            if (!allChatRecords.TryGetValue(processID, out ChatRecord activeRecord))
                return;

            if (MySettings.Default.EnableMergedChatMode)
            {
                List<ChatLine> chatLines = new(activeRecord.History);

                //merge non-active records
                foreach (var record in allChatRecords.Values)
                {
                    if (record != activeRecord)
                    {
                        chatLines.AddRange(record.History.Where(x => !ChannelType.IgnoreIfMerging.HasFlag(x.Channel)));
                    }
                }

                //do a groupby to keep only unique elements but biased to elements from the active processID
                //then order by timestamp
                var combinedLines = chatLines.GroupBy(x => x, (key, chatLineGroup) => chatLineGroup.Any(x => x.SourceProcessID == processID) ? chatLineGroup.Where(x => x.SourceProcessID == processID) : new[] { key })
                                             .SelectMany(x => x)
                                             .OrderBy(x => x.Timestamp)
                                             .ToList();

                NewChatAvailableEvent(null, new NewChatAvailableEventArgs(processID, combinedLines));
            }
            else
            {
                NewChatAvailableEvent(null, new NewChatAvailableEventArgs(processID, activeRecord.History));
            }
        }

        /// <summary>
        /// Save old entries in chat log to file and remove from memory.
        /// </summary>
        /// <param name="hours">The number of hours of data to keep in memory. Will also be excluded from backup.</param>
        internal static void SaveAllChatLogs(double hours, bool waitForCompletion = false)
        {
            Debug.Assert(hours >= 0, "hours parameter cannot be negative");

            isSaving = true;
            lastSaveTime = DateTime.Now;

            List<ChatLine> allAccountLines = new();

            //backup everything older than 2 hours
            foreach (var record in allChatRecords.Values)
            {
                allAccountLines.AddRange(record.History.Where(x => x.Timestamp <= lastSaveTime.AddHours(hours * -1)));
                record.History.RemoveAll(x => x.Timestamp <= lastSaveTime.AddHours(hours * -1));
            }

            if (allAccountLines.Count > 0)
            {
                if (waitForCompletion)
                {
                    Task.Run(() => ChatLogWriter.WriteRecords(allAccountLines)).Wait();
                }
                else
                {
                    Task.Run(() => ChatLogWriter.WriteRecords(allAccountLines));
                }
            }

            isSaving = false;
        }
        /// <summary>
        /// Save all chat log for this processID to file and remove from memory.
        /// </summary>
        internal static void SaveChatLog(int processID, bool waitForCompletion = false)
        {
            Debug.Assert(processID > 0, "processID cannot be 0");

            isSaving = true;
            lastSaveTime = DateTime.Now;

            var record = allChatRecords[processID];
            var currentTime = DateTime.Now;
            var allAccountLines = record.History.Where(x => x.Timestamp <= currentTime).ToList();
            record.History.RemoveAll(x => x.Timestamp <= currentTime);

            if (allAccountLines.Count > 0)
            {
                if (waitForCompletion)
                {
                    Task.Run(() => ChatLogWriter.WriteRecords(allAccountLines)).Wait();
                }
                else
                {
                    Task.Run(() => ChatLogWriter.WriteRecords(allAccountLines));
                }
            }

            isSaving = false;
        }

        /// <summary>
        /// Deletes backup chat log files older than specified number of days.
        /// </summary>
        internal static void PurgeOldRecords(double days)
        {
            Debug.Assert(days >= 0, "days cannot be negative");

            //build directory path
            string path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            path = Path.GetDirectoryName(path);
            path = Path.GetDirectoryName(path);
            path = Path.Combine(path, "ChatLog");

            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.EnumerateFiles(path, "*.csv"))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                filename = filename.Remove(filename.IndexOf('_'));

                if (DateTime.TryParse(filename, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) &&
                    date < DateTime.Now.AddDays(days * -1))
                {
                    File.Delete(file);
                }
            }
        }
    }

    internal class NewChatAvailableEventArgs : EventArgs
    {
        public int ProcessID { get; }
        public string Account { get; }
        public int Character { get; }
        public ReadOnlyCollection<ChatLine> ChatLines { get; }

        public NewChatAvailableEventArgs(int processID, List<ChatLine> chatLines, string account = "", int character = 0)
        {
            ProcessID = processID;
            ChatLines = chatLines.AsReadOnly();
            Account = account;                  //if this is an empty string, it means that the event was triggered by a request to get all records
            Character = character;
        }
    }
}
