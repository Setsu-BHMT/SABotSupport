using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation.Providers;

namespace SABotSupport
{
    internal static class RecipeScriptGenerator
    {
        [Flags]
        private enum StoreType
        {
            Meat = 1 << 0,
            Fish = 1 << 1,
            Vegetable1 = 1 << 2,
            Vegetable2 = 1 << 3,
            Fruit = 1 << 4,
            Spice = 1 << 5,
            Special = 1 << 6,
            NonPurchasable = 1 << 7,
            HeroIslandPurchasable = 1 << 8,
            Huntable = 1 << 9,
        }

        private readonly struct Ingredient
        {
            public readonly StoreType StoreType;
            public readonly string Item;

            public Ingredient(StoreType storeType, string item)
            {
                StoreType = storeType;
                Item = item;
            }
        }

        private static readonly string[] animalIngredients = new string[] {
            "雞蛋", "蜥蜴", "青蛙" };
        private static readonly string[] animalIngredientVariations = new string[] {
            "蛋", "", ""};
        private static readonly string meatBaseName = "肉";
        private static readonly char[] ingredientBaseLevels = new char[] { '1', '2', '3', '4' };
        private static readonly string[] meatTrueNames = new string[] {
            "小的肉", "乾燥肉", "大的肉", "高級肉"};
        private static readonly string[] seafoodIngredients = new string[] {
            "螃蟹", "花枝", "章魚", "蝦", "海星", "海藻", "貝" };
        private static readonly string[] seafoodIngredientVariations = new string[] {
            "蟹", "", "章鱼", "蝦子", "", "藻", "" };
        private static readonly string fishBaseName = "魚";
        private static readonly string[] fishTrueNames = new string[] { 
            "曬乾的魚", "新鮮的魚", "深海魚", "高級魚"};
        private static readonly string[] vegetableIngredients1 = new string[] {
            "紅蘿蔔", "青椒", "蕃茄", "茄子", "包心菜", "蘆筍", "小黃瓜" };
        private static readonly string[] vegetableIngredient1Variations = new string[] {
            "蘿蔔", "", "番茄", "", "", "", "黃瓜" };
        private static readonly string[] vegetableIngredients2 = new string[] {
            "蔥", "大蒜", "豆子", "香菇", "馬鈴薯", "地瓜", "橡樹果", "白菜", "竹筍" };
        private static readonly string[] vegetableIngredient2Variations = new string[] {
            "蔥", "蒜", "豆", "菇", "", "", "", "", "" };
        private static readonly string[] fruitIngredients = new string[] {
            "桃子", "蘋果", "草莓", "梨子", "葡萄", "櫻桃", "橘子", "柿子" };
        private static readonly string[] fruitIngredientVariations = new string[] {
            "桃", "", "", "梨", "", "", "橘", "柿" };
        private static readonly string[] spiceIngredients = new string[] {
            "米", "鹽", "砂糖", "油", "生薑", "香草", "串", "水", "炭" };
        private static readonly string[] spiceIngredientVariations = new string[] {
            "", "", "糖", "", "薑", "", "", "", "木炭" };
        private static readonly string[] specialIngredients = new string[] {
            "海藻4", "貝4",
            "紅蘿蔔4", "青椒4", "蕃茄4", "茄子4", "包心菜4", "蘆筍4", "小黃瓜4",
            "蔥4", "大蒜4", "豆子4", "香菇4", "馬鈴薯4", "地瓜4", "白菜4",
            "桃子4", "蘋果4", "草莓4", "梨子4", "葡萄4", "櫻桃4", "橘子4", "柿子4",
            "米4", "鹽4", "砂糖4", "油4", "生薑4", "水4", "炭4", "胡椒4" };
        private static readonly string[] nonPurchasableIngredients = new string[] {
            "忠誠Ａ", "忠誠Ｂ", "竹筍4", "南瓜4" };
        private static readonly string[] huntableIngredients = new string[] {
            "最棒的肉", "大塊肉" };
        private static readonly string[] heroIslandPurchasableIngredients = new string[] {
            "胡椒1", "胡椒2", "胡椒3" };

        private static readonly string settings = String.Join(Environment.NewLine, new string[] {
            "set 腳本延時,5",
            "set 快速行走,1",
            "set 關閉特效,1",
            "set 吃補血肉,0",
            "set 丟非血肉,0",
            "set 自動堆疊,1",
            "dim @對話中,@材料,@失敗繼續,@retry",
            "label start",
        });
        private static readonly string beginMovement = String.Join(Environment.NewLine, new string[] {
            "ifmap 2009,+2 '瑪麗娜絲的便利商店 ",
            "call 去漁村24",
            "if @失敗繼續,=,1,巡迴繼續",
        });
        private static readonly string moveToMeatVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "label 巡迴繼續",
            "ifpos 25,15,肉店continue",
            "walkpos 25,26",
            "walkpos 25,24",
            "walkpos 25,22",
            "walkpos 25,20",
            "walkpos 25,18",
            "walkpos 25,16",
            "walkpos 25,15",
            "goto +6",
            "label 肉店error",
            "eo",
            "delay 2000",
            "label 肉店continue",
            "walkpos 25,15",
            "w 25,15,C",
            "call 買肉",
            "if @retry,=,1,肉店error",
        });
        private static readonly string moveToFishVendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "label 巡迴繼續",
            "ifpos 25,17,魚店continue",
            "walkpos 25,26",
            "walkpos 25,24",
            "walkpos 25,22",
            "walkpos 25,20",
            "walkpos 25,18",
            "walkpos 25,17",
            "goto +6",
            "label 魚店error",
            "eo",
            "delay 2000",
            "label 魚店continue",
            "walkpos 25,17",
            "w 25,17,C",
            "call 買魚",
            "if @retry,=,1,魚店error",
        });
        private static readonly string moveToFishVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,16",
            "walkpos 25,17",
            "goto +5",
            "label 魚店error",
            "eo",
            "delay 2000",
            "walkpos 25,17",
            "w 25,17,C",
            "call 買魚",
            "if @retry,=,1,魚店error",
        });
        private static readonly string moveToVegetable1VendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "label 巡迴繼續",
            "ifpos 25,23,蔬菜1店continue",
            "walkpos 25,26",
            "walkpos 25,24",
            "walkpos 25,23",
            "goto +6",
            "label 蔬菜1店error",
            "eo",
            "delay 2000",
            "label 蔬菜1店continue",
            "walkpos 25,23",
            "w 25,23,C",
            "call 買蔬菜1",
            "if @retry,=,1,蔬菜1店error",
        });
        private static readonly string moveToVegetable1Vendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,18",
            "walkpos 25,19",
            "walkpos 25,20",
            "walkpos 25,21",
            "walkpos 25,22",
            "walkpos 25,23",
            "goto +5",
            "label 蔬菜1店error",
            "eo",
            "delay 2000",
            "walkpos 25,23",
            "w 25,23,C",
            "call 買蔬菜1",
            "if @retry,=,1,蔬菜1店error",
        });
        private static readonly string moveToVegetable2VendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "walkpos 25,27",
            "label 巡迴繼續",
            "walkpos 25,26",
            "goto +5",
            "label 蔬菜2店error",
            "eo",
            "delay 2000",
            "walkpos 25,26",
            "w 25,26,C",
            "call 買蔬菜2",
            "if @retry,=,1,蔬菜2店error",
        });
        private static readonly string moveToVegetable2Vendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,24",
            "walkpos 25,25",
            "walkpos 25,26",
            "goto +5",
            "label 蔬菜2店error",
            "eo",
            "delay 2000",
            "walkpos 25,26",
            "w 25,26,C",
            "call 買蔬菜2",
            "if @retry,=,1,蔬菜2店error",
        });
        private static readonly string moveToFruitVendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "walkpos 25,26",
            "walkpos 25,28",
            "label 巡迴繼續",
            "walkpos 25,29",
            "goto +5",
            "label 水果店error",
            "eo",
            "delay 2000",
            "walkpos 25,29",
            "w 25,29,C",
            "call 買水果",
            "if @retry,=,1,水果店error",
        });
        private static readonly string moveToFruitVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,27",
            "walkpos 25,28",
            "walkpos 25,29",
            "goto +5",
            "label 水果店error",
            "eo",
            "delay 2000",
            "walkpos 25,29",
            "w 25,29,C",
            "call 買水果",
            "if @retry,=,1,水果店error",
        });
        private static readonly string moveToSpiceVendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "walkpos 25,26",
            "walkpos 25,27",
            "walkpos 25,28",
            "walkpos 25,29",
            "walkpos 25,30",
            "walkpos 25,31",
            "walkpos 25,32",
            "walkpos 25,33",
            "label 巡迴繼續",
            "walkpos 25,34",
            "goto +5",
            "label 米店error",
            "eo",
            "delay 2000",
            "walkpos 25,34",
            "w 25,34,C",
            "call 買米",
            "if @retry,=,1,米店error",
        });
        private static readonly string moveToSpiceVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,30",
            "walkpos 25,31",
            "walkpos 25,32",
            "walkpos 25,33",
            "walkpos 25,34",
            "goto +5",
            "label 米店error",
            "eo",
            "delay 2000",
            "walkpos 25,34",
            "w 25,34,C",
            "call 買米",
            "if @retry,=,1,米店error",
        });
        private static readonly string moveToSpecialVendorAsFirstVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 20,26",
            "walkpos 22,26",
            "walkpos 24,26",
            "walkpos 25,26",
            "walkpos 25,27",
            "walkpos 25,28",
            "walkpos 25,29",
            "walkpos 25,30",
            "walkpos 25,31",
            "walkpos 25,32",
            "walkpos 25,33",
            "walkpos 25,34",
            "walkpos 25,35",
            "walkpos 25,36",
            "label 巡迴繼續",
            "walkpos 25,37",
            "goto +5",
            "label 特產店error",
            "eo",
            "delay 2000",
            "walkpos 25,37",
            "w 25,37,C",
            "call 買特產",
            "if @retry,=,1,特產店error",
        });
        private static readonly string moveToSpecialVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,35",
            "walkpos 25,36",
            "walkpos 25,37",
            "goto +5",
            "label 特產店error",
            "eo",
            "delay 2000",
            "walkpos 25,37",
            "w 25,37,C",
            "call 買特產",
            "if @retry,=,1,特產店error",
        });
        private static readonly string moveBackToNeutral = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,36",
            "walkpos 25,34",
            "label 從米店",
            "walkpos 25,32",
            "walkpos 25,30",
            "label 從水果店",
            "walkpos 25,28",
            "label 移動完畢",
        });
        private static readonly string moveBackToFruitVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,36",
            "walkpos 25,34",
            "label 從米店",
            "walkpos 25,32",
            "walkpos 25,30",
            "walkpos 25,29",
            "label 移動完畢",
        });
        private static readonly string moveBackToSpiceVendor = String.Join(Environment.NewLine, new string[] {
            "walkpos 25,36",
            "walkpos 25,34",
            "label 移動完畢",
        });
        private static readonly string purchaseLoop = String.Join(Environment.NewLine, new string[] {
            "label 買材料",
            "ifitem ?,=,@材料,+2",
            "goto +2",
            "return",
            "if @對話中,=,1,+4",
            "w 0,0,C ",
            "delay 300",
            "say buy",
            "waitdlg ?,0,3,+7",
            "let @對話中,=,1",
            "delay 600",
            "buy @材料,1",
            "waititem ?,@材料,3,+3",
            "delay 500",
            "return",
            "let @retry,=,1",
            "return",
        });
        private static readonly string warpToConvenientStore = String.Join(Environment.NewLine, new string[] {
            "label 去漁村24",
            "call 傳漁村",
            "walkpos 63,58 ",
            "walkpos 61,58 ",
            "walkpos 61,60 ",
            "walkpos 61,62 ",
            "walkpos 61,64 ",
            "walkpos 62,65 ",
            "walkpos 63,66 ",
            "walkpos 64,67 ",
            "walkpos 65,68 ",
            "walkpos 65,70 ",
            "walkpos 65,72 ",
            "walkpos 66,73 ",
            "walkpos 67,74 ",
            "walkpos 68,75 ",
            "walkpos 69,76 ",
            "walkpos 71,76 ",
            "walkpos 72,77 ",
            "walkpos 73,78 ",
            "walkpos 74,79 ",
            "walkpos 74,81 ",
            "walkpos 74,83 ",
            "walkpos 74,85 ",
            "walkpos 74,87 ",
            "walkpos 74,89 ",
            "walkpos 74,91 ",
            "walkpos 74,93 ",
            "walkpos 73,94 ",
            "walkpos 72,95 ",
            "walkpos 71,96 ",
            "walkpos 70,97 ",
            "walkpos 70,99 ",
            "walkpos 70,101 ",
            "walkpos 70,103 ",
            "walkpos 70,104 ",
            "walkpos 71,104 ",
            "walkpos 72,104 ",
            "chmap 73,104 ",
            "waitmap 2009,3,-4 '瑪麗娜絲的便利商店 ",
            "walkpos 12,26 ",
            "walkpos 14,26 ",
            "walkpos 16,26 ",
            "walkpos 18,26 ",
            "return",
            "label 傳漁村",
            "delay 300",
            "ifitem 飛行至瑪麗娜絲,=,0,+3",
            "useitem ?飛行至瑪麗娜絲",
            "goto 傳漁村go",
            "call 回薩姆吉爾",
            "waitmap 1000,5,-1",
            "label 傳漁村resume",
            "delay 250",
            "walkpos 91,99",
            "walkpos 90,99",
            "w 90,99,A",
            "say hi",
            "waitdlg ?,0,5,傳漁村error",
            "button 1",
            "label 傳漁村go",
            "waitmap 2000,5,傳漁村error",
            "delay 300",
            "return",
            "label 傳漁村error",
            "eo",
            "delay 2000",
            "goto 傳漁村resume",
            "label 回薩姆吉爾",
            "ifstone >=,10000,+3",
            "msg 沒有石幣了!",
            "pause",
            "ifmap 1000,+2",
            "goto +2",
            "ifpos 92,99,+2",
            "goto +2",
            "return",
            "ifpos 93,101,buyFeatherEnd",
            "ifitem 飛行至薩姆吉爾,=,0,+3",
            "useitem ?飛行至薩姆吉爾",
            "goto buyFeather",
            "log 1",
            "waitmap 1000,5,回薩姆吉爾",
            "return",
            "label buyFeather",
            "waitmap 1000,5,回薩姆吉爾",
            "delay 300",
            "walkpos 92,100",
            "walkpos 92,101",
            "walkpos 93,101",
            "w 0,0,C",
            "say buy",
            "waitdlg ?,0,3,-4",
            "buy 1,1",
            "delay 500",
            "waititem ?,飛行至薩姆吉爾,2,buyFeather",
            "w 0,0,G",
            "say /pile",
            "delay 500",
            "call moveFeather",
            "label buyFeatherEnd",
            "walkpos 92,101",
            "walkpos 92,100",
            "walkpos 92,99",
            "return",
            "label moveFeather",
            "ifitem 空位,>,0,+2",
            "return",
            "dim @pos",
            "let @pos,=,15",
            "ifitem @pos,=,空位,+3",
            "let @pos,-,1",
            "goto -2",
            "moveitem 飛行至薩姆吉爾,@pos",
            "dim -@pos",
            "return",
        });
        private static readonly string warpToPolaIsland = String.Join(Environment.NewLine, new string[] {
            "label 傳波拉島",
            "delay 300",
            "call 回薩姆吉爾",
            "waitmap 1000,5,-1",
            "label 傳波拉島resume",
            "delay 250",
            "walkpos 91,99",
            "walkpos 89,99",
            "w 89,99,H ",
            "say hi",
            "waitdlg ?,0,5,傳波拉島error",
            "button 下一頁",
            "waitdlg ?,0,3,傳波拉島error",
            "button 1",
            "waitmap 500,5,傳波拉島error",
            "delay 300",
            "return",
            "label 傳波拉島error",
            "eo",
            "delay 2000",
            "goto 傳波拉島resume",
        });
        private static readonly string buyPepper = String.Join(Environment.NewLine, new string[] {
            "label 買胡椒",
            "set 自動逃跑,1",
            "call 傳波拉島",
            "walkpos 214,346 ",
            "walkpos 216,348 ",
            "walkpos 218,350 ",
            "walkpos 220,352 ",
            "walkpos 222,354 ",
            "walkpos 224,356 ",
            "walkpos 226,358 ",
            "walkpos 228,360 ",
            "walkpos 230,362 ",
            "walkpos 232,364 ",
            "walkpos 234,366 ",
            "walkpos 236,368 ",
            "walkpos 238,368 ",
            "walkpos 240,368 ",
            "walkpos 242,368 ",
            "walkpos 244,368 ",
            "walkpos 246,370 ",
            "walkpos 248,370 ",
            "walkpos 250,368 ",
            "walkpos 251,368 ",
            "walkpos 253,368 ",
            "walkpos 255,368 ",
            "walkpos 257,368 ",
            "walkpos 258,367 ",
            "walkpos 260,367 ",
            "walkpos 262,367 ",
            "walkpos 264,367 ",
            "walkpos 266,367 ",
            "walkpos 268,367 ",
            "walkpos 269,368 ",
            "walkpos 269,369 ",
            "walkpos 271,369 ",
            "walkpos 273,369 ",
            "walkpos 274,370 ",
            "walkpos 276,370 ",
            "walkpos 278,370 ",
            "walkpos 280,370 ",
            "walkpos 282,370 ",
            "walkpos 284,370 ",
            "walkpos 286,370 ",
            "walkpos 288,370 ",
            "walkpos 287,370 ",
            "walkpos 289,370 ",
            "walkpos 291,370 ",
            "walkpos 293,370 ",
            "walkpos 295,370 ",
            "walkpos 295,368 ",
            "walkpos 295,366 ",
            "walkpos 295,364 ",
            "walkpos 296,363 ",
            "walkpos 297,362 ",
            "walkpos 298,361 ",
            "walkpos 300,361 ",
            "walkpos 301,360 ",
            "walkpos 302,359 ",
            "walkpos 303,358 ",
            "walkpos 304,357 ",
            "walkpos 305,356 ",
            "walkpos 306,355 ",
            "walkpos 307,354 ",
            "walkpos 307,352 ",
            "walkpos 307,350 ",
            "walkpos 307,348 ",
            "walkpos 307,346 ",
            "walkpos 307,344 ",
            "walkpos 307,342 ",
            "walkpos 307,340 ",
            "walkpos 309,340 ",
            "walkpos 311,340 ",
            "walkpos 313,340 ",
            "walkpos 315,340 ",
            "walkpos 316,341 ",
            "walkpos 317,342 ",
            "walkpos 319,342 ",
            "walkpos 321,342 ",
            "walkpos 323,342 ",
            "walkpos 325,342 ",
            "walkpos 327,342 ",
            "walkpos 329,342 ",
            "walkpos 331,342 ",
            "walkpos 333,342 ",
            "walkpos 334,341 ",
            "walkpos 335,340 ",
            "walkpos 337,340 ",
            "walkpos 339,340 ",
            "walkpos 341,340 ",
            "walkpos 343,340 ",
            "walkpos 344,339 ",
            "walkpos 346,339 ",
            "walkpos 348,339 ",
            "walkpos 350,339 ",
            "walkpos 351,340 ",
            "walkpos 352,341 ",
            "walkpos 353,341 ",
            "walkpos 354,341 ",
            "chmap 355,341 ",
            "waitmap 5500,3,-4 '避難洞窟 ",
            "walkpos 2,19 ",
            "walkpos 4,19 ",
            "walkpos 6,19 ",
            "walkpos 8,19 ",
            "walkpos 10,19 ",
            "walkpos 12,19 ",
            "walkpos 14,19 ",
            "walkpos 16,19 ",
            "walkpos 17,18 ",
            "walkpos 18,17 ",
            "walkpos 19,16 ",
            "walkpos 20,15 ",
            "walkpos 21,14 ",
            "walkpos 22,13 ",
            "walkpos 23,12 ",
            "walkpos 24,11 ",
            "walkpos 25,10 ",
            "walkpos 26,9 ",
            "walkpos 28,9 ",
            "walkpos 30,9 ",
            "walkpos 32,9 ",
            "walkpos 34,9 ",
            "w 34,9,B ",
            "say 我非常需要胡椒",
            "waitdlg ?,0,3,-4",
            "buy @材料,3",
            "waititem ?,@材料,3,-6",
            "delay 500",
            "buy @材料,2",
            "delay 500",
            "say /pile",
            "delay 500",
            "return",
            "label .err.",
            "ifmap 500,start",
            "beep",
            "pause '不明的錯誤",
            "end",
        });
        private static readonly string huntMeat = String.Join(Environment.NewLine, new string[] {
            "label 打肉",
            "call 傳波拉島",
            "walkpos 213,345 ",
            "walkpos 215,347 ",
            "walkpos 217,349 ",
            "walkpos 219,351 ",
            "walkpos 221,353 ",
            "walkpos 223,355 ",
            "walkpos 225,357 ",
            "walkpos 227,359 ",
            "walkpos 229,361 ",
            "set 自動逃跑,0",
            "set 自動堆疊,0",
            "say /pile off",
            "say /encount on",
            "delay 1000",
            "delay 500",
            "ifitem @材料,<,3,-1",
            "say /encount off",
            "delay 500",
            "iffight !=,0,-1",
            "say /pile",
            "set 自動堆疊,1",
            "say /pile on",
            "return",
        });

        private static readonly Regex isRecipeParser = new(@"^(\w+[\dＡＢ肉]+\+)+");
        private static readonly Regex ingredientParser = new(@"(?<=^|\+)\w+[\dＡＢ肉]+");

        internal static (bool Success, string Script) GenerateRecipeScript(string recipeName, string ingredients)
        {
            if (!isRecipeParser.Match(ingredients).Success)
                return (false, "Does not match recipe regex");

            //try to parse ingredients
            var matches = ingredientParser.Matches(ingredients);
            if (matches.Count == 0)
                return (false, "Ingredient parser failed to parse ingredients");

            var ingredientList = new List<Ingredient>();
            foreach (string match in matches.Cast<Match>().Select(x => x.Value))
            {
                string item = match;
                string name = item.Remove(item.Count() - 1);

                //fix names that are abbreviations
                if (animalIngredientVariations.Any(x => x == name))
                {
                    item = item.Replace(name, animalIngredients[Array.IndexOf(animalIngredientVariations, name)]);
                }
                else if (seafoodIngredientVariations.Any(x => x == name))
                {
                    item = item.Replace(name, seafoodIngredients[Array.IndexOf(seafoodIngredientVariations, name)]);
                }
                else if (vegetableIngredient1Variations.Any(x => x == name))
                {
                    item = item.Replace(name, vegetableIngredients1[Array.IndexOf(vegetableIngredient1Variations, name)]);
                }
                else if (vegetableIngredient2Variations.Any(x => x == name))
                {
                    item = item.Replace(name, vegetableIngredients2[Array.IndexOf(vegetableIngredient2Variations, name)]);
                }
                else if (fruitIngredientVariations.Any(x => x == name))
                {
                    item = item.Replace(name, fruitIngredients[Array.IndexOf(fruitIngredientVariations, name)]);
                }
                else if (spiceIngredientVariations.Any(x => x == name))
                {
                    item = item.Replace(name, spiceIngredients[Array.IndexOf(spiceIngredientVariations, name)]);
                }
                name = item.Remove(item.Count() - 1);

                //match the ingredient to the store type category
                if (specialIngredients.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.Special, item));
                }
                else if (animalIngredients.Any(x => x == name))
                {
                    ingredientList.Add(new Ingredient(StoreType.Meat, item));
                }
                else if (meatBaseName == name)
                {   //actual meats have special names
                    char level = item.Last();
                    item = meatTrueNames[Array.IndexOf(ingredientBaseLevels, level)];
                    ingredientList.Add(new Ingredient(StoreType.Meat, item));
                }
                else if (meatTrueNames.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.Meat, item));
                }
                else if (seafoodIngredients.Any(x => x == name))
                {
                    ingredientList.Add(new Ingredient(StoreType.Fish, item));
                }
                else if (fishBaseName == name)
                {   //must use true name for fish otherwise it also buys octopi
                    char level = item.Last();
                    item = fishTrueNames[Array.IndexOf(ingredientBaseLevels, level)];
                    ingredientList.Add(new Ingredient(StoreType.Fish, item));
                }
                else if (meatTrueNames.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.Meat, item));
                }
                else if (vegetableIngredients1.Any(x => x == name))
                {
                    ingredientList.Add(new Ingredient(StoreType.Vegetable1, item));
                }
                else if (vegetableIngredients2.Any(x => x == name))
                {
                    //special case for 豆子3 because it can conflict with 聰明的豆子3
                    if (item == "豆子3")
                    {
                        item = "水分多的豆子";
                    }

                    ingredientList.Add(new Ingredient(StoreType.Vegetable2, item));
                }
                else if (fruitIngredients.Any(x => x == name))
                {
                    ingredientList.Add(new Ingredient(StoreType.Fruit, item));
                }
                else if (spiceIngredients.Any(x => x == name))
                {
                    ingredientList.Add(new Ingredient(StoreType.Spice, item));
                }
                else if (nonPurchasableIngredients.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.NonPurchasable, item));
                }
                else if (huntableIngredients.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.Huntable, item));
                }
                else if (heroIslandPurchasableIngredients.Any(x => x == item))
                {
                    ingredientList.Add(new Ingredient(StoreType.HeroIslandPurchasable, item));
                }
                else
                {
                    return (false, $"{item} is not a recognized ingredient");
                }
            }

            //build script
            var sb = new StringBuilder();

            sb.AppendLine($"print 自動料理腳本: {recipeName},1");
            sb.AppendLine($"print *本腳本由Assa外掛輔助程式自動生成 by Setsu,7");
            sb.AppendLine($"print 請確認身上有料理寵物一隻在任意位置(腳本不會檢查),4");
            sb.AppendLine(settings);

            //check non-purchasable ingredients
            int count = 0;
            foreach (var ingredient in ingredientList.Where(x => x.StoreType == StoreType.NonPurchasable))
            {
                sb.AppendLine($"ifitem {ingredient.Item},>,0,+3");
                sb.AppendLine($"print 請自行取得 {ingredient.Item} 後再執行腳本,6");
                sb.AppendLine($"end");
                count++;
            }

            //check hero-island-purchasable ingredients, and purchase them first
            foreach (var ingredient in ingredientList.Where(x => x.StoreType == StoreType.HeroIslandPurchasable))
            {
                sb.AppendLine($"ifitem {ingredient.Item},>,0,+3");
                sb.AppendLine($"let @材料,=,{ingredient.Item}");
                sb.AppendLine($"call 買胡椒");
                count++;
            }

            //check meat ingredients that require hunting, and acquire them first
            foreach (var ingredient in ingredientList.Where(x => x.StoreType == StoreType.Huntable))
            {
                sb.AppendLine($"ifitem {ingredient.Item},>,0,+9");
                sb.AppendLine($"say /igi 0 巴朵蘭恩的肉");
                sb.AppendLine($"say /igi 1 布洛多斯的肉");
                sb.AppendLine($"say /igi 2 {(ingredient.Item == "最棒的肉" ? "大塊肉" : "最棒的肉")}");
                sb.AppendLine($"let @材料,=,{ingredient.Item}");
                sb.AppendLine($"call 打肉");
                sb.AppendLine($"say /igi 0 0");
                sb.AppendLine($"say /igi 1 0");
                sb.AppendLine($"say /igi 2 0");
                count++;
            }

            //check empty space
            int spaces = ingredientList.Count - count + 1;
            sb.AppendLine($"ifitem 空位,>=,{spaces},+4");
            sb.AppendLine($"beep");
            sb.AppendLine($"print 道具欄空位不足{spaces}格,6");
            sb.AppendLine($"pause");

            //start of movement
            sb.AppendLine(beginMovement);

            //determine starting vendor, then determine intermediate and ending vendors
            if (NeedVendor(ingredientList, StoreType.Meat))
            {
                sb.AppendLine(moveToMeatVendor);

                if (NeedVendor(ingredientList, 
                    StoreType.Fish | StoreType.Vegetable1 | StoreType.Vegetable2 | 
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToFishVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Vegetable1 | StoreType.Vegetable2 |
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToVegetable1Vendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Vegetable2 |
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToVegetable2Vendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToFruitVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToSpiceVendor);
                }
                else
                {
                    sb.AppendLine("goto 從水果店");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 從米店");
                }

                sb.AppendLine(moveBackToNeutral);
            }
            else if (NeedVendor(ingredientList, StoreType.Fish))
            {
                sb.AppendLine(moveToFishVendorAsFirstVendor);

                if (NeedVendor(ingredientList,
                    StoreType.Vegetable1 | StoreType.Vegetable2 |
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToVegetable1Vendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Vegetable2 |
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToVegetable2Vendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToFruitVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToSpiceVendor);
                }
                else
                {
                    sb.AppendLine("goto 從水果店");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 從米店");
                }

                sb.AppendLine(moveBackToNeutral);
            }
            else if (NeedVendor(ingredientList, StoreType.Vegetable1))
            {
                sb.AppendLine(moveToVegetable1VendorAsFirstVendor);

                if (NeedVendor(ingredientList,
                    StoreType.Vegetable2 |
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToVegetable2Vendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToFruitVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToSpiceVendor);
                }
                else
                {
                    sb.AppendLine("goto 從水果店");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 從米店");
                }

                sb.AppendLine(moveBackToNeutral);
            }
            else if (NeedVendor(ingredientList, StoreType.Vegetable2))
            {
                sb.AppendLine(moveToVegetable2VendorAsFirstVendor);

                if (NeedVendor(ingredientList,
                    StoreType.Fruit | StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToFruitVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToSpiceVendor);
                }
                else
                {
                    sb.AppendLine("goto 從水果店");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 從米店");
                }

                sb.AppendLine(moveBackToNeutral);
            }
            else if (NeedVendor(ingredientList, StoreType.Fruit))
            {
                sb.AppendLine(moveToFruitVendorAsFirstVendor);

                if (NeedVendor(ingredientList,
                    StoreType.Spice | StoreType.Special))
                {
                    sb.AppendLine(moveToSpiceVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 從米店");
                }

                sb.AppendLine(moveBackToFruitVendor);
            }
            else if (NeedVendor(ingredientList, StoreType.Spice))
            {
                sb.AppendLine(moveToSpiceVendorAsFirstVendor);

                if (NeedVendor(ingredientList,
                    StoreType.Special))
                {
                    sb.AppendLine(moveToSpecialVendor);
                }
                else
                {
                    sb.AppendLine("goto 移動完畢");
                }

                sb.AppendLine(moveBackToSpiceVendor);
            }
            else
            {
                sb.AppendLine(moveToSpecialVendorAsFirstVendor);
            }

            //move ingredients into position
            count = 0;
            foreach (var item in ingredientList.Select(x => x.Item))
            {
                sb.AppendLine($"moveitem {item},{++count}");
                sb.AppendLine($"delay 300");
                sb.AppendLine($"waititem {count},{item},3,-2");
            }

            //cook packet command
            sb.AppendLine($"LL 0,料理,{String.Join("|", ingredientList.Select(x => x.Item))}");
            sb.AppendLine($"waititem 1,{recipeName},1.2,+5");
            sb.AppendLine($"ifitem 1,=,異世界的{recipeName},+4");
            sb.AppendLine($"print 料理 {recipeName} 成功!,2");
            sb.AppendLine(recipeName.Contains("肉") ? "" : $"set 吃補血肉,1");
            sb.AppendLine($"end");
            sb.AppendLine($"print 料理 {recipeName} 似乎失敗了...,4");
            sb.AppendLine($"let @失敗繼續,=,1");

            //checks for saving ingredients
            foreach (var item in ingredientList.Select(x => x.Item))
            {
                sb.AppendLine($"ifitem 1,=,{item},start");
            }

            //default action for failed products
            sb.AppendLine($"ifitem 1,!=,回復,+3");
            sb.AppendLine($"useitem 1");
            sb.AppendLine($"delay 300");
            sb.AppendLine($"ifitem 1,=,空位,start");
            sb.AppendLine($"doffitem 1");
            sb.AppendLine($"delay 300");
            sb.AppendLine($"goto start");

            GenerateIngredientPurchaseCode(sb, ingredientList, "買肉", "超商的肉店", StoreType.Meat,
                NeedVendor(ingredientList, StoreType.Meat));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買魚", "超商的魚店", StoreType.Fish,
                NeedVendor(ingredientList, StoreType.Fish));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買蔬菜1", "蔬果店（之１", StoreType.Vegetable1,
                NeedVendor(ingredientList, StoreType.Vegetable1));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買蔬菜2", "蔬菜店（之２", StoreType.Vegetable2,
                NeedVendor(ingredientList, StoreType.Vegetable2));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買水果", "水果店", StoreType.Fruit,
                NeedVendor(ingredientList, StoreType.Fruit));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買米", "瑪麗娜絲的米店", StoreType.Spice,
                NeedVendor(ingredientList, StoreType.Spice));
            GenerateIngredientPurchaseCode(sb, ingredientList, "買特產", "地方特產品販賣員", StoreType.Special,
                NeedVendor(ingredientList, StoreType.Special));

            sb.AppendLine(purchaseLoop);
            sb.AppendLine(warpToConvenientStore);
            if (ingredientList.Any(x => x.StoreType == StoreType.HeroIslandPurchasable))
            {
                sb.AppendLine(warpToPolaIsland);
                sb.AppendLine(buyPepper);
            }
            if (ingredientList.Any(x => x.StoreType == StoreType.Huntable))
            {
                sb.AppendLine(warpToPolaIsland);
                sb.AppendLine(huntMeat);
            }

            return (true, sb.ToString());
        }

        private static void GenerateIngredientPurchaseCode(StringBuilder sb, List<Ingredient> ingredientList, string label, string shopName, StoreType type, bool isNeeded)
        {
            sb.AppendLine($"label {label}");

            if (!isNeeded)
            {
                sb.AppendLine($"return");
                return;
            }

            //open the dialog and confirm shop name
            sb.AppendLine($"let @對話中,=,0");
            sb.AppendLine($"let @retry,=,1");
            sb.AppendLine($"say buy");
            sb.AppendLine($"waitdlg {shopName},1,3,{label}end");
            sb.AppendLine($"let @對話中,=,1");
            sb.AppendLine($"let @retry,=,0");

            //append code to buy each individual ingredient
            foreach (var ingredientToBuy in ingredientList.Where(x => x.StoreType == type))
            {
                sb.AppendLine($"let @材料,=,{ingredientToBuy.Item}");
                sb.AppendLine($"call 買材料");
                sb.AppendLine($"if @retry,=,1,{label}end");
            }

            sb.AppendLine($"label {label}end");
            sb.AppendLine($"return");
        }

        private static bool NeedVendor(List<Ingredient> ingredeintList, StoreType vendors)
            => ingredeintList.Any(x => vendors.HasFlag(x.StoreType));
    }
}
