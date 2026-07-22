using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SABotSupport
{
    internal class PlayerInfo
    {
        public int Encounter { get; private set; } = 0;
        public int Turn { get; private set; } = 0;
        public int Fame { get; private set; } = -1;
        public int Momentum { get; private set; } = -1;
        public int Vigour { get; private set; } = -1;
        public int CombatPoints { get; private set; } = -1;
        public int Silver { get; private set; } = -1;
        public int Appreciation { get; private set; } = -1;
        public string MemberLabel { get; private set; } = String.Empty;
        public DateTime MemberExpiration { get; private set; } = DateTime.MinValue;
        public int AdventureLevel { get; private set; } = -1;
        public int AdventureExp { get; private set; } = -1;
        public int AdventureNextExp { get; private set; } = -1;
        public int AdventurePoints { get; private set; } = -1;
        public int AdventureQuests { get; private set; } = -1;
        public DateTime AdventureResetTime { get; private set; } = DateTime.MinValue;
        public int SmithLevel { get; private set; } = -1;
        public int SmithExp { get; private set; } = -1;
        public int SmithNextExp { get; private set; } = -1;
        public DateTime ExpBoostExpiration { get; private set; } = DateTime.MinValue;
        public DateTime LastLoginTime { get; private set; } = DateTime.MinValue;
        public int ExpDifference { get; private set; } = 0;

        private (uint Level, int Exp, int NextExp) expCache = new();

        private static readonly Regex fameLineRegex = new(@"聲望:\s+(?<Fame>\d+).+氣勢:\s+(?<Momentum>\d+).+活力:\s+(?<Vigour>\d+).+戰鬥:\s+(?<CombatPoints>\d+)");
        private static readonly Regex silverLineRegex = new(@"銀幣:\s+(?<Silver>\d+).+回饋:\s+(?<Appreciation>\d+)");
        private static readonly Regex memberLineRegex1 = new(@"會員資料\s*:\s+(?<MemberLabel>\w+)\s+(?<MemberExpiration>.+)");
        private static readonly Regex memberLineRegex2 = new(@"會員資料\s*:\s+(?<MemberLabel>\w+)\s*$");
        private static readonly Regex adventureLevelLineRegex = new(@"冒險等級：\s+(?<AdventureLevel>\d+).+經\s*驗\s*：\s+(?<AdventureExp>\d+)\D+(?<AdventureNextExp>\d+)");
        private static readonly Regex adventurePointsLineRegex = new(@"冒險點數：\s+(?<AdventurePoints>\d+).+可委託次數：\s+(?<AdventureQuests>\d+).+下次重置：\s+(?<AdventureResetRemainingHours>\d+)");
        private static readonly Regex smithLevelLineRegex = new(@"鐵匠等級：\s+(?<SmithLevel>\d+).+經\s*驗\s*：\s+(?<SmithExp>\d+)\D+(?<SmithNextExp>\d+)");
        private static readonly Regex expBoostIncreaseLineRegex = new(@"^學習經驗的能力提升了150％，時效剩餘\s*(?<ExpBoostRemainMinutes>\d+)\s*分鐘。");
        private static readonly Regex expBoostDecreaseLineRegex = new(@"^提升學習經驗的能力剩大約\s*(?<ExpBoostRemainHours>\d+)\s*小時。");
        private static readonly Regex gainAdventurePointsRegex = new(@"^(獲得\s*(?<AdventurePoints>\d+)\s*冒險點數。|辛苦了！給你獎勵\s*(?<AdventurePoints>\d+)\s*點冒險點數！)");
        private static readonly Regex gainAdventureExpRegex = new(@"^獲得\s*\d+\s*冒險經驗，目前經驗:\s*(?<AdventureExp>\d+)\D+(?<AdventureNextExp>\d+)");
        private static readonly Regex gainSmithExpRegex = new(@"^獲得\s*\d+\s*鐵匠經驗，目前經驗:\s*(?<SmithExp>\d+)\D+(?<SmithNextExp>\d+)");

        public void ResetLastLoginTime()
            => LastLoginTime = DateTime.MinValue;
        
        public void UpdateFromDataPackage(DataPackage data)
        {
            //login time
            if (LastLoginTime == DateTime.MinValue && data.IsOnline)
            {
                LastLoginTime = DateTime.Now;
            }
            else if (LastLoginTime != DateTime.MinValue && !data.IsOnline)
            {
                ResetLastLoginTime();
            }

            //encounter and turn
            if (data.Encounter != Encounter || data.Turn > 0)
            {
                Encounter = data.Encounter;
                Turn = data.Turn;
            }

            //experience tracking
            ExpDifference = CalculateExpDifference(expCache, ExpDifference, data.Player.Level, data.Player.EXP, 152);
            expCache = new(data.Player.Level, data.Player.EXP, data.Player.NextEXP);
        }

        public void UpdateFromChat(IEnumerable<ChatLine> chatLines)
        {
            foreach (var line in chatLines)
            {
                if (ChatLine.ChannelType.IgnoreIfParsingPlayerInfo.HasFlag(line.Channel))
                    continue;

                string text = line.Text;
                Match match;

                if (line.Channel == ChatLine.ChannelType.Custom && text.Contains("已上線"))
                {
                    LastLoginTime = line.Timestamp;
                }
                else if (line.Channel == ChatLine.ChannelType.Custom && text.Contains("已離線"))
                {
                    ResetLastLoginTime();
                }
                else if (line.Channel == ChatLine.ChannelType.System && text.Contains("提升學習經驗的能力消失了!"))
                {
                    ExpBoostExpiration = DateTime.MinValue;
                }
                else if((match = fameLineRegex.Match(text)).Success)
                {
                    Fame = Int32.Parse(match.Groups["Fame"].Value);
                    Momentum = Int32.Parse(match.Groups["Momentum"].Value);
                    Vigour = Int32.Parse(match.Groups["Vigour"].Value);
                    CombatPoints = Int32.Parse(match.Groups["CombatPoints"].Value);
                }
                else if ((match = silverLineRegex.Match(text)).Success)
                {
                    Silver = Int32.Parse(match.Groups["Silver"].Value);
                    Appreciation = Int32.Parse(match.Groups["Appreciation"].Value);
                }
                else if ((match = memberLineRegex1.Match(text)).Success)
                {
                    MemberLabel = match.Groups["MemberLabel"].Value;
                    if (DateTime.TryParse(match.Groups["MemberExpiration"].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    {
                        MemberExpiration = result;
                    }
                }
                else if ((match = memberLineRegex2.Match(text)).Success)
                {
                    MemberLabel = match.Groups["MemberLabel"].Value;
                    MemberExpiration = DateTime.MinValue;
                }
                else if ((match = adventureLevelLineRegex.Match(text)).Success)
                {
                    AdventureLevel = Int32.Parse(match.Groups["AdventureLevel"].Value);
                    AdventureExp = Int32.Parse(match.Groups["AdventureExp"].Value);
                    AdventureNextExp = Int32.Parse(match.Groups["AdventureNextExp"].Value);
                }
                else if ((match = adventurePointsLineRegex.Match(text)).Success)
                {
                    AdventurePoints = Int32.Parse(match.Groups["AdventurePoints"].Value);
                    AdventureQuests = Int32.Parse(match.Groups["AdventureQuests"].Value);
                    int hours = Int32.Parse(match.Groups["AdventureResetRemainingHours"].Value);
                    AdventureResetTime = line.Timestamp + TimeSpan.FromHours(hours);
                }
                else if ((match = smithLevelLineRegex.Match(text)).Success)
                {
                    SmithLevel = Int32.Parse(match.Groups["SmithLevel"].Value);
                    SmithExp = Int32.Parse(match.Groups["SmithExp"].Value);
                    SmithNextExp = Int32.Parse(match.Groups["SmithNextExp"].Value);
                }
                else if ((match = expBoostIncreaseLineRegex.Match(text)).Success)
                {
                    var minutes = Int32.Parse(match.Groups["ExpBoostRemainMinutes"].Value);
                    ExpBoostExpiration = line.Timestamp + TimeSpan.FromMinutes(minutes);
                }
                else if ((match = expBoostDecreaseLineRegex.Match(text)).Success)
                {
                    var hours = Int32.Parse(match.Groups["ExpBoostRemainHours"].Value);
                    ExpBoostExpiration = line.Timestamp + TimeSpan.FromHours(hours);
                }
                else if (AdventurePoints > 0 && (match = gainAdventurePointsRegex.Match(text)).Success)
                {
                    AdventurePoints += Int32.Parse(match.Groups["AdventurePoints"].Value);
                }
                else if ((match = gainAdventureExpRegex.Match(text)).Success)
                {
                    AdventureExp = Int32.Parse(match.Groups["AdventureExp"].Value);
                    AdventureNextExp = Int32.Parse(match.Groups["AdventureNextExp"].Value);
                }
                else if ((match = gainSmithExpRegex.Match(text)).Success)
                {
                    SmithExp = Int32.Parse(match.Groups["SmithExp"].Value);
                    SmithNextExp = Int32.Parse(match.Groups["SmithNextExp"].Value);
                }
            }
        }

        /// <summary>
        /// Calculates a new exp difference value given the current state and previous state.
        /// </summary>
        /// <param name="expCache">Previous exp state.</param>
        /// <param name="expDifference">Previous exp difference value.</param>
        /// <param name="maxLevel">The maximum achievable level through combat. For players it's 152 and pets it's 140</param>
        /// <remarks>
        /// This is used by both PlayerJournal and PetJournal.
        /// </remarks>
        public static int CalculateExpDifference((uint Level, int Exp, int NextExp) expCache, int expDifference, uint level, int exp, uint maxLevel)
        {
            if (expCache.Level > level)
            {
                expDifference = 0;
            }
            else if (expCache.Level != 0 && level <= maxLevel)
            {
                var diff = exp - expCache.Exp +                                              //basic difference
                           ((level != expCache.Level) ? expCache.NextExp : 0) +              //accounting for getting to next level
                           Math.Max(0, (int)level - (int)expCache.Level - 1) * expCache.NextExp;  //approximation for when we jump many levels

                if (diff > 0)
                {
                    expDifference = diff;
                }
            }

            return expDifference;
        }
    }

    internal static class PlayerJournal
    {
        private static readonly ConcurrentDictionary<string, PlayerInfo> players = new();

        internal static PlayerInfo GetPlayerInfo(string account, int character)
            => players.GetOrAdd(GetKey(account, character), new PlayerInfo());

        internal static void Update(DataPackage data)
        {
            GetPlayerInfo(data.GameClientData.CurrentAccount, data.GameClientData.CurrentCharacter).UpdateFromDataPackage(data);
        }

        internal static void OnNewChatAvailable(object sender, NewChatAvailableEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Account))
            {
                GetPlayerInfo(e.Account, e.Character).UpdateFromChat(e.ChatLines);
            }
        }

        private static string GetKey(string account, int character)
            => $"{account}{character}";
    }
}
