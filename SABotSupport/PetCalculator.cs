using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MathNet.Numerics.LinearAlgebra;

using SABotSupport.ASSAStructures;

namespace SABotSupport
{
    internal static class PetCalculator
    {
        private static readonly Regex NICKNAME_REGEX = new(@"\d+\.\d+\.\d+\.\d+");
        private static readonly Regex NICKNAME_SHORT_REGEX = new(@"(((?<=^[^\d])*|[\.a-i=_])\d+)");
        
        internal static int[] ParseNicknameStats(Pet pet)
        {
            var results = NICKNAME_SHORT_REGEX.Matches(pet.Nickname);
            if (results.Count >= 4)
            {   //shortened nickname format
                var output = new List<int>();

                foreach (Match match in results)
                {
                    switch (match.Value.First())
                    {
                        case '.': case '_':
                            output.Add(int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case '=': case 'a':
                            output.Add(100 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'b':
                            output.Add(200 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'c':
                            output.Add(300 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'd':
                            output.Add(400 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'e':
                            output.Add(500 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'f':
                            output.Add(600 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'g':
                            output.Add(700 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'h':
                            output.Add(800 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        case 'i':
                            output.Add(900 + int.Parse(match.Value.Substring(1).ToString()));
                            break;
                        default:
                            output.Add(int.Parse(match.Value));
                            break;
                    }
                }

                return [.. output];
            }
            else
            {   //long nickname format
                var result = NICKNAME_REGEX.Match(pet.Nickname);
                if (!result.Success)
                    return [];

                MessageBox.Show($"請回報錯誤給作者, 並附上寵物暱稱: {pet.Nickname}");
                
                return [];
            }
        }

        /// <summary>
        /// Automatically tries all calculation methods until a valid result is returned.
        /// Calculation by nickname has priority unless optional parameter is set.
        /// </summary>
        internal static decimal[] CalculateGrowthRate(Pet pet, bool useLinearRegression = false)
        {
            if (useLinearRegression)
            {
                var rates = CalculateGrowthRateByLinearRegression(PetJournal.GetAllStatsRecords(pet));

                return rates.Length > 0 ?
                    rates : CalculateGrowthRateByParsingNickname(pet);
            }
            else
            {
                var rates = CalculateGrowthRateByParsingNickname(pet);

                return rates.Length > 0 ?
                    rates : CalculateGrowthRateByLinearRegression(PetJournal.GetAllStatsRecords(pet));
            }
        }

        internal static decimal[] CalculateGrowthRateByParsingNickname(Pet pet)
        {
            int[] lv1Stats = ParseNicknameStats(pet);
            if (lv1Stats.Count() == 0)
                return [];

            uint denominator = (lv1Stats.Count() == 4) ? pet.Level - 1 : pet.Level - (uint)lv1Stats[4];
            if (denominator == 0)
                return [];

            decimal hpRate = (pet.MaxHP - lv1Stats[0]) * 1.0m / denominator;
            decimal atkRate = (pet.Attack - lv1Stats[1]) * 1.0m / denominator;
            decimal defRate = (pet.Armor - lv1Stats[2]) * 1.0m / denominator;
            decimal spdRate = (pet.Speed - lv1Stats[3]) * 1.0m / denominator;

            return [hpRate, atkRate, defRate, spdRate, atkRate + defRate + spdRate];
        }

        internal static decimal[] CalculateGrowthRateByLinearRegression(IReadOnlyList<PetStats> growthLog)
        {
            if (growthLog == default || growthLog.Count <= 1)
                return [];

            int Sx = growthLog.Select(x => (int)x.Level).Sum();
            int Sxx = growthLog.Select(x => (int)x.Level * (int)x.Level).Sum();
            int Sxy_hp = growthLog.Select(x => (int)x.Level * (int)x.HP).Sum();
            int Sxy_atk = growthLog.Select(x => (int)x.Level * (int)x.Attack).Sum();
            int Sxy_def = growthLog.Select(x => (int)x.Level * (int)x.Armor).Sum();
            int Sxy_spd = growthLog.Select(x => (int)x.Level * (int)x.Speed).Sum();
            int Sy_hp = growthLog.Select(x => (int)x.HP).Sum();
            int Sy_atk = growthLog.Select(x => (int)x.Attack).Sum();
            int Sy_def = growthLog.Select(x => (int)x.Armor).Sum();
            int Sy_spd = growthLog.Select(x => (int)x.Speed).Sum();
            int n = growthLog.Count;

            int denominator = n * Sxx - Sx * Sx;
            if (denominator == 0)
                return [];

            decimal hpRate = (n * Sxy_hp - Sx * Sy_hp) * 1.0m / denominator;
            decimal atkRate = (n * Sxy_atk - Sx * Sy_atk) * 1.0m / denominator;
            decimal defRate = (n * Sxy_def - Sx * Sy_def) * 1.0m / denominator;
            decimal spdRate = (n * Sxy_spd - Sx * Sy_spd) * 1.0m / denominator;

            return [hpRate, atkRate, defRate, spdRate, atkRate + defRate + spdRate];
        }

        private static int CustomRound(decimal value)
        {   //rounds on 0.6 instead of 0.5
            var integer = (int)decimal.Truncate(value);

            return (value - integer) >= (decimal)0.6 ? integer + 1 : integer;
        }

        internal static int[] PredictStats(Pet pet, decimal[] rates, uint predictLevel, bool useCustomRounding = false)
        {
            if (rates.Count() == 0 || pet.Level >= predictLevel)
                return [];

            uint multiplier = predictLevel - pet.Level;

            int hp, atk, def, spd;
            if (useCustomRounding)
            {
                hp = CustomRound(pet.MaxHP + rates[0] * multiplier);
                atk = CustomRound(pet.Attack + rates[1] * multiplier);
                def = CustomRound(pet.Armor + rates[2] * multiplier);
                spd = CustomRound(pet.Speed + rates[3] * multiplier);
            }
            else
            {
                hp = (int)decimal.Round(pet.MaxHP + rates[0] * multiplier, MidpointRounding.AwayFromZero);
                atk = (int)decimal.Round(pet.Attack + rates[1] * multiplier, MidpointRounding.AwayFromZero);
                def = (int)decimal.Round(pet.Armor + rates[2] * multiplier, MidpointRounding.AwayFromZero);
                spd = (int)decimal.Round(pet.Speed + rates[3] * multiplier, MidpointRounding.AwayFromZero);
            }

            return [hp, atk, def, spd];
        }

        internal static int[] PredictBoost(Pet pet, uint boostLevel)
        {
            if (pet.Level >= boostLevel || pet.Reincarnation < 2)
                return [];

            //predict 140 stats if necessary
            Vector<double> baseStats;
            uint baseLevel;
            if (pet.Level >= 140)
            {
                baseStats = EstimateBaseStats(pet);
                baseLevel = pet.Level;
            }
            else
            {
                var rates = CalculateGrowthRate(pet);
                if (rates.Length == 0)
                    return [];

                baseStats = EstimateBaseStats(PredictStats(pet, rates, 140));
                baseLevel = 140;
            }

            double sumStats = baseStats[1] + baseStats[2] + baseStats[3];
            double[] newBaseStats = new double[4];

            //determine if we should assume king pet in calculations
            bool isKingPet = Properties.Settings.Default.AutoDetermineBoostKingPet && IsKingPet(pet.Name) || 
                             Properties.Settings.Default.ForceBoostKingPet;

            //calculate vitality independently
            newBaseStats[0] = baseStats[0] + (boostLevel - baseLevel) * 3;

            if (boostLevel == 150 || boostLevel - baseLevel == 5)
            {   //use direct calculation, which is slightly more accurate
                EstimateNextBoostLevel(boostLevel, isKingPet, sumStats, baseStats, newBaseStats);

                //estimate stats from base stats and round
                var newStats = EstimateStatsFromBaseStats(newBaseStats);

                return Array.ConvertAll(newStats.PointwiseRound().ToArray(), Convert.ToInt32);
            }
            else
            {   //use geometric series calculation

                //if we are starting from 140, use direct calculation to estimate 150 first
                //this is because of the extra adjustments made to str only for 150
                if (baseLevel == 140)
                {
                    EstimateNextBoostLevel(150, isKingPet, sumStats, baseStats, newBaseStats);

                    baseLevel = 150;
                    baseStats = Vector<double>.Build.Dense(newBaseStats);
                    sumStats = baseStats[1] + baseStats[2] + baseStats[3];

                }

                double n = (boostLevel - baseLevel) / 5;

                //calculate the new points to be divided amonst the stats
                double newPoints = 1400 - (1400 - sumStats) * Math.Pow(0.9, n);

                //porportionally divide the points into str/tgh/dex
                newBaseStats[1] = newPoints * (baseStats[1] / sumStats);
                newBaseStats[2] = newPoints * (baseStats[2] / sumStats);
                newBaseStats[3] = newPoints * (baseStats[3] / sumStats);

                //estimate stats from base stats and round
                //note: floor hp and attack but round armor and speed, this was determined from test data to have smallest error
                var newStats = EstimateStatsFromBaseStats(newBaseStats);
                double[] output = [.. newStats];
                output[0] = Math.Floor(output[0]);
                output[1] = Math.Floor(output[1]);
                output[2] = Math.Round(output[2], MidpointRounding.AwayFromZero);
                output[3] = Math.Round(output[3], MidpointRounding.AwayFromZero);

                return Array.ConvertAll(output, Convert.ToInt32);
            }

            static bool IsKingPet(string name)
            {
                HashSet<string> kingPetList =
                [
                    "烏力",
                    "烏力烏力",
                    "烏力斯坦",
                    "烏力布魯",
                    "黑烏力",
                    "撲滿烏力",
                    "烏力萊德",
                    "烏力固力",
                    "布比",
                    "金布伊",
                    "布伊",
                    "布伊比",
                    "卡布伊",
                    "布依布魯",
                    "布依布依",
                    "布依胖",
                    "加美",
                    "加比",
                    "加比奧",
                    "加斯",
                    "加加",
                    "加斯奧",
                    "加格雷依",
                    "比比加",
                    "烏寶寶",
                    "威威",
                    "烏卡魯",
                    "威伯",
                    "烏寶依",
                    "威斯",
                    "威比",
                    "烏拉拉",
                    "貝洛恩",
                    "貝洛洛克",
                    "貝洛寶克爾",
                    "貝洛寶利",
                    "貝洛金",
                    "貝洛格",
                    "貝洛貝",
                    "貝洛波波",
                    "龜之盾",
                    "綠龜",
                    "卡梅蘭恩",
                    "石龜",
                    "藍龜",
                    "卡拉格爾",
                    "龜之鋼",
                    "卡拉龜",
                    "阿哥亞",
                    "尼可斯",
                    "特洛昆",
                    "達克爾",
                    "柏克爾",
                    "尼加斯",
                    "尼基斯",
                    "特洛可斯",
                    "拉奇魯哥",
                    "呼拔拔",
                    "多薩金格",
                    "魯尼帖斯",
                    "呼波波",
                    "呼魯魯",
                    "魯拉其斯",
                    "拉奇斯斯",
                    "卡達魯卡斯",
                    "柯伊達",
                    "柯洛加斯",
                    "洛奇安",
                    "洛克斯",
                    "阿伊薩",
                    "格洛格魯",
                    "朵洛將恩",
                    "利則諾頓",
                    "揚奇洛斯",
                    "邦浦洛斯",
                    "邦奇諾",
                    "布魯頓",
                    "邦諾斯娜",
                    "迪基格斯",
                    "楊格斯",
                    "克雷爾",
                    "克克爾",
                    "克洛爾",
                    "里斯基",
                    "克拉爾",
                    "拉斯基",
                    "里拉拉",
                    "克達達",
                    "卡比特",
                    "凱比",
                    "昆伊",
                    "凱比特",
                    "卡卡特",
                    "昆依特",
                    "比特",
                    "可卡特",
                    "格爾頓",
                    "奇拉頓",
                    "齊爾格爾頓",
                    "格爾格",
                    "司爾頓",
                    "格爾希洛",
                    "梅爾頓",
                    "戈登爾頓",
                    "修寶",
                    "卡拉寶斯",
                    "布克布克",
                    "多洛加",
                    "布魯寶",
                    "藍寶",
                    "瑞德寶",
                    "毛寶",
                    "卡克爾",
                    "巴克",
                    "鮑",
                    "卡拉卡利",
                    "歐林吉魯",
                    "史凱魯",
                    "耶普魯",
                    "芭拉芭",
                    "貝魯卡",
                    "貝魯伊卡",
                    "格魯西斯",
                    "金格薩貝魯",
                    "普魯夏",
                    "薩格魯",
                    "瑪斯貝卡",
                    "多利諾布斯",
                    "貝恩達斯",
                    "多利凱拉",
                    "多洛布斯",
                    "麥丁布斯",
                    "加耶布斯",
                    "迪米布斯",
                    "玻洛布斯",
                    "克邦凱斯",
                    "加克拉",
                    "加格",
                    "邦恩吉",
                    "迪加",
                    "砂鯊",
                    "波波頓",
                    "梅魯莎",
                    "奇卡洛斯",
                    "奇娜",
                    "卡卡金寶",
                    "奇卡寶斯",
                    "尤里蛙",
                    "裘裡蛙",
                    "艾爾蛙",
                    "里昂蛙",
                    "邦奇",
                    "姆伊",
                    "海主人",
                    "多魯寶",
                    "歐瑟菲",
                    "莫拉司",
                    "瑪斯特",
                    "沙瓦列",
                    "布洛多斯",
                    "布林帖斯",
                    "布拉奇多斯",
                    "斯天多斯",
                    "邦恩多斯",
                    "嘎吱拉",
                    "哥斯哥斯",
                    "蒙哥拉斯",
                    "瑪恩摩",
                    "恩摩摩",
                    "瑪摩那斯",
                    "瑪恩摩洛斯",
                    "固力摩",
                    "摩吉摩吉",
                    "摩米索拉",
                    "摩酷羅",
                    "帖拉格恩",
                    "洛卡倫恩",
                    "加寶格恩",
                    "朵拉比斯",
                    "可可恩",
                    "克洛恩",
                    "布蘭恩",
                    "迪布恩",
                    "火雞",
                    "克克洛斯",
                    "霍爾克",
                    "奇寶",
                    "格裡蘭",
                    "摩里",
                    "瑞里西爾",
                    "塔斯夫",
                    "奧卡洛斯",
                    "左迪洛斯",
                    "巴朵蘭恩",
                    "帖拉所伊朵",
                    "朵巴奈特",
                    "阿米朵",
                    "邦司涼朵",
                    "布伊德",
                    "摩娜西普",
                    "卡伊霍恩",
                    "拉伊霍恩",
                    "邦達霍恩",
                    "布萊茲",
                    "布依倫斯",
                    "伊夫霍恩",
                    "史多拉奇頓",
                    "薩美洛斯",
                    "阿利給洛斯",
                    "達伊諾洛斯",
                    "拉可洛斯",
                    "萊姆洛斯",
                    "朱利洛斯",
                    "辛普洛斯",
                    "邦洛洛克斯",
                    "蘭貝魯斯",
                    "可利多洛斯",
                    "諾斯多洛斯",
                    "立杜魯斯",
                    "諾克斯",
                    "巴克亞司",
                    "雷德力克斯",
                ];

                return kingPetList.Contains(name);
            }

            static void EstimateNextBoostLevel(uint boostLevel, bool isKingPet, double sumStats, Vector<double> baseStats, double[] newBaseStats)
            {
                //calculate the new points to be divided amonst the stats
                double newPoints = (1400 - sumStats) / 10;
                if (boostLevel == 150)
                {
                    newPoints += isKingPet ? 210 : 160;
                }

                //porportionally divide the points into str/tgh/dex
                double ratio = (1 + newPoints / sumStats);
                newBaseStats[1] = baseStats[1] * ratio;
                newBaseStats[2] = baseStats[2] * ratio;
                newBaseStats[3] = baseStats[3] * ratio;

                //adjust the attack if it's too low
                if (boostLevel == 150)
                {
                    newBaseStats[1] += Math.Max(newBaseStats[1] / -1.11 + 337, 0);
                }
            }
        }
                
        internal static readonly Matrix<double> A = Matrix<double>.Build.DenseOfArray(new double[,] {
            { 4.0, 1.0, 1.0, 1.0 },
            { 0.1, 1.0, 0.1, 0.05},
            { 0.1, 0.1, 1.0, 0.05},
            { 0.0, 0.0, 0.0, 1.0 },
        });
        internal static Vector<double> EstimateBaseStats(Pet pet)
            => EstimateBaseStats([(int)pet.MaxHP, (int)pet.Attack, (int)pet.Armor, (int)pet.Speed]);
        internal static Vector<double> EstimateBaseStats(int[] stats)
            => A.Solve(Vector<double>.Build.Dense(Array.ConvertAll(stats, Convert.ToDouble)));

        internal static Vector<double> EstimateStatsFromBaseStats(double[] baseStats)
            => A * (Vector<double>.Build.Dense(baseStats));

        internal static readonly Vector<double> DRAGON_EVOLVE_MULTIPLIER = Vector<double>.Build.Dense([1.21, 1.38, 1.15, 1.33]);
        internal static Vector<double> EstimateDragonEvolveStats(double[] evolveBaseStats)
        {
            var results = Vector<double>.Build.Dense(evolveBaseStats);

            DRAGON_EVOLVE_MULTIPLIER.PointwiseMultiply(results, results);
            A.Multiply(results, results);

            return results;
        }

        internal static string FormatPetStats(Pet pet, StringCollection format)
        {
            var sb = new StringBuilder();
            var baseStats = EstimateBaseStats(pet);
            var rates = CalculateGrowthRate(pet, Properties.Settings.Default.UseLinearRegression);
            var predictStats = PredictStats(pet, rates, Convert.ToUInt32(Properties.Settings.Default.PredictLevel));
            var predictBaseStats = predictStats.Length > 0 ? EstimateBaseStats(predictStats) : default;
            var boostStats = PredictBoost(pet, Convert.ToUInt32(Properties.Settings.Default.BoostLevel));

            //parse or lookup lv1 stats
            var lv1Stats = ParseNicknameStats(pet);
            if (lv1Stats.Length == 0)
            {   //lookup from history instead
                var petStats = PetJournal.GetFirstStatsRecord(pet);
                lv1Stats = petStats.IsInitialized ? [(int)petStats.HP, (int)petStats.Attack, (int)petStats.Armor, (int)petStats.Speed, (int)petStats.Level] :
                                                    [];
            }

            foreach (var identifier in format)
            {
                if (identifier.StartsWith("*"))
                {
                    sb.Append(identifier.Substring(1));

                    continue;
                }

                switch (identifier)
                {
                    case "Tab":
                        sb.Append("\t");
                        break;
                    case "Space":
                        sb.Append(" ");
                        break;
                    case "Bar":
                        sb.Append("|");
                        break;
                    case "Name":
                        sb.Append(pet.Name);
                        break;
                    case "Nickname":
                        sb.Append(pet.Nickname);
                        break;
                    case "Level":
                        sb.Append(pet.Level);
                        break;
                    case "Reincarnation":
                        sb.Append(pet.Reincarnation);
                        break;
                    case "HP":
                        sb.Append(pet.MaxHP);
                        break;
                    case "Attack":
                        sb.Append(pet.Attack);
                        break;
                    case "Armor":
                        sb.Append(pet.Armor);
                        break;
                    case "Speed":
                        sb.Append(pet.Speed);
                        break;
                    case "Lv1HP":
                        if (lv1Stats.Length == 0) continue;
                        sb.Append(lv1Stats[0]);
                        break;
                    case "Lv1Attack":
                        if (lv1Stats.Length == 0) continue;
                        sb.Append(lv1Stats[1]);
                        break;
                    case "Lv1Armor":
                        if (lv1Stats.Length == 0) continue;
                        sb.Append(lv1Stats[2]);
                        break;
                    case "Lv1Speed":
                        if (lv1Stats.Length == 0) continue;
                        sb.Append(lv1Stats[3]);
                        break;
                    case "Vitality":
                        sb.AppendFormat("{0:0.###}", baseStats[0]);
                        break;
                    case "Strength":
                        sb.AppendFormat("{0:0.###}", baseStats[1]);
                        break;
                    case "Defense":
                        sb.AppendFormat("{0:0.###}", baseStats[2]);
                        break;
                    case "Dexterity":
                        sb.AppendFormat("{0:0.###}", baseStats[3]);
                        break;
                    case "HPRate":
                        if (rates.Length == 0) continue;
                        sb.AppendFormat("{0:0.###}", rates[0]);
                        break;
                    case "AttackRate":
                        if (rates.Length == 0) continue;
                        sb.AppendFormat("{0:0.###}", rates[1]);
                        break;
                    case "ArmorRate":
                        if (rates.Length == 0) continue;
                        sb.AppendFormat("{0:0.###}", rates[2]);
                        break;
                    case "SpeedRate":
                        if (rates.Length == 0) continue;
                        sb.AppendFormat("{0:0.###}", rates[3]);
                        break;
                    case "CombinedRate":
                        if (rates.Length == 0) continue;
                        sb.AppendFormat("{0:0.###}", rates[4]);
                        break;
                    case "PredictLevel":
                        sb.Append(Properties.Settings.Default.PredictLevel);
                        break;
                    case "PredictHP":
                        if (predictStats.Length == 0) continue;
                        sb.Append(predictStats[0]);
                        break;
                    case "PredictAttack":
                        if (predictStats.Length == 0) continue;
                        sb.Append(predictStats[1]);
                        break;
                    case "PredictArmor":
                        if (predictStats.Length == 0) continue;
                        sb.Append(predictStats[2]);
                        break;
                    case "PredictSpeed":
                        if (predictStats.Length == 0) continue;
                        sb.Append(predictStats[3]);
                        break;
                    case "PredictVitality":
                        if (predictBaseStats.Count == 0) continue;
                        sb.AppendFormat("{0:0.###}", predictBaseStats[0]);
                        break;
                    case "PredictStrength":
                        if (predictBaseStats.Count == 0) continue;
                        sb.AppendFormat("{0:0.###}", predictBaseStats[1]);
                        break;
                    case "PredictDefense":
                        if (predictBaseStats.Count == 0) continue;
                        sb.AppendFormat("{0:0.###}", predictBaseStats[2]);
                        break;
                    case "PredictDexterity":
                        if (predictBaseStats.Count == 0) continue;
                        sb.AppendFormat("{0:0.###}", predictBaseStats[3]);
                        break;
                    case "BoostLevel":
                        sb.Append(Properties.Settings.Default.BoostLevel);
                        break;
                    case "BoostHP":
                        if (boostStats.Length == 0) continue;
                        sb.Append(boostStats[0]);
                        break;
                    case "BoostAttack":
                        if (boostStats.Length == 0) continue;
                        sb.Append(boostStats[1]);
                        break;
                    case "BoostArmor":
                        if (boostStats.Length == 0) continue;
                        sb.Append(boostStats[2]);
                        break;
                    case "BoostSpeed":
                        if (boostStats.Length == 0) continue;
                        sb.Append(boostStats[3]);
                        break;
                    case "Newline":
                        sb.AppendLine();
                        break;
                    default:
                        throw new FormatException($"Unknown format identifier: {identifier}");
                }
            }

            return (Properties.Settings.Default.AutoRemoveWhitespace) ? sb.ToString().Trim() : sb.ToString();
        }
    }
}
