using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SABotSupport
{
    internal static class StringConverter
    {
        internal static string Big5ToUtf8(byte[] buffer)
        {
            string result = Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding("big5"), Encoding.UTF8, buffer));
            var index = result.IndexOf('\0');

            return index >= 0 ? result.Remove(index) : result;
        }
    }

    namespace ASSAStructures
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct RawDataComposite
        {
            [FieldOffset(0)]
            public RawMap Map;
            [FieldOffset(0xBC)]
            public RawPlayer Player;
            [FieldOffset(0x18C)]
            public int PetPointer;
            [FieldOffset(0x1A8)]
            public int ItemPointer;
            [FieldOffset(0x1C4)]
            public int TeammatePointer;
            [FieldOffset(0x23C)]
            public int BattlePointer;
            [FieldOffset(0x250)]
            public int Turn;
            [FieldOffset(0x254)]
            public int Encounter;
            [FieldOffset(0x280)]
            public int ScreenState;
            [FieldOffset(0x288)]
            public int GameHandle;
            [FieldOffset(0x2A4)]
            public int BattleDuration;  //in ms
            [FieldOffset(0x2A8)]
            public int TurnAverageDuration;  //in ms
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawMap
        {
            public uint X;
            public uint Y;
            public uint MouseHoverX;
            public uint MouseHoverY;
            public uint LastX;
            public uint LastY;
            public uint MapID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] Name;
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public int Unknown4;
            public uint Unknown5;
            public uint MousePixelX;
            public uint MousePixelY;
        }

        internal struct Map
        {
            public readonly uint X;
            public readonly uint Y;
            public readonly uint MouseHoverX;
            public readonly uint MouseHoverY;
            public readonly uint LastX;
            public readonly uint LastY;
            public readonly uint MapID;
            public readonly string Name;
            public readonly uint MousePixelX;
            public readonly uint MousePixelY;

            public Map(RawMap data)
            {
                X = data.X;
                Y = data.Y;
                MouseHoverX = data.MouseHoverX;
                MouseHoverY = data.MouseHoverY;
                LastX = data.LastX;
                LastY = data.LastY;
                MapID = data.MapID;
                Name = StringConverter.Big5ToUtf8(data.Name);
                MousePixelX = data.MousePixelX;
                MousePixelY = data.MousePixelY;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawPlayer
        {
            public uint Direction;
            public uint HP;
            public uint MaxHP;
            public uint MP;
            public uint MaxMP;
            public uint VIT;
            public uint STR;
            public uint DEF;
            public uint DEX;
            public int EXP;
            public int NextEXP;
            public uint Level;
            public uint Attack;
            public uint Armor;
            public uint Speed;
            public uint Charisma;
            public uint Unknown1;
            public uint Earth;
            public uint Water;
            public uint Fire;
            public uint Wind;
            public int Money;
            public uint Unknown2;
            public int Unknown3;
            public uint Unknown4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public byte[] Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
            public byte[] Nickname;
            public uint Unknown5;
            public ulong Unknown6;
            public uint Unknown7;
            public ushort Unknown8;
            public ushort Unknown9;
            public uint Unknown10;
            public ushort Unknown11;
            public short BattlePetPos;
            public short IsPet1Ready;
            public short IsPet2Ready;
            public short IsPet3Ready;
            public short IsPet4Ready;
            public short IsPet5Ready;
            public short MailPetPos;
            public ushort Unknown12;
            public uint Unknown13;
            public ushort Unknown14;
            public short Reincarnation;
            public int RidePetPos;
            public int Points;
        }

        internal struct Player
        {
            public readonly uint Direction;
            public readonly uint HP;
            public readonly uint MaxHP;
            public readonly uint MP;
            public readonly uint MaxMP;
            public readonly uint VIT;
            public readonly uint STR;
            public readonly uint DEF;
            public readonly uint DEX;
            public readonly int EXP;
            public readonly int NextEXP;
            public readonly uint Level;
            public readonly uint Attack;
            public readonly uint Armor;
            public readonly uint Speed;
            public readonly uint Charisma;
            public readonly uint Earth;
            public readonly uint Water;
            public readonly uint Fire;
            public readonly uint Wind;
            public readonly int Money;
            public readonly string Name;
            public readonly string Nickname;
            public readonly int BattlePetPos;
            public readonly bool[] IsPetReady;
            public readonly int MailPetPos;
            public readonly short Reincarnation;
            public readonly int RidePetPos;
            public readonly int Points;

            public Player(RawPlayer data)
            {
                Direction = data.Direction;
                HP = data.HP;
                MaxHP = data.MaxHP;
                MP = data.MP;
                MaxMP = data.MaxMP;
                VIT = data.VIT;
                STR = data.STR;
                DEF = data.DEF;
                DEX = data.DEX;
                EXP = data.EXP;
                NextEXP = data.NextEXP;
                Level = data.Level;
                Attack = data.Attack;
                Armor = data.Armor;
                Speed = data.Speed;
                Charisma = data.Charisma;
                Earth = data.Earth / 10;
                Water = data.Water / 10;
                Fire = data.Fire / 10;
                Wind = data.Wind / 10;
                Money = data.Money;
                Name = StringConverter.Big5ToUtf8(data.Name);
                Nickname = StringConverter.Big5ToUtf8(data.Nickname);
                BattlePetPos = data.BattlePetPos;
                IsPetReady = new bool[] {
                data.IsPet1Ready == 1,
                data.IsPet2Ready == 1,
                data.IsPet3Ready == 1,
                data.IsPet4Ready == 1,
                data.IsPet5Ready == 1};
                MailPetPos = data.MailPetPos;
                Reincarnation = data.Reincarnation;
                RidePetPos = data.RidePetPos;
                Points = data.Points;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct RawPetContainer
        {
            [FieldOffset(0)]
            public RawPet Pet1;
            [FieldOffset(0xB18)]
            public RawPet Pet2;
            [FieldOffset(0x1630)]
            public RawPet Pet3;
            [FieldOffset(0x2148)]
            public RawPet Pet4;
            [FieldOffset(0x2C60)]
            public RawPet Pet5;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawPet
        {
            public uint PetID;
            public uint HP;
            public uint MaxHP;
            public uint Unknown1;
            public uint Unknown2;
            public int EXP;
            public int NextEXP;
            public uint Level;
            public uint Attack;
            public uint Armor;
            public uint Speed;
            public uint Loyalty;
            public uint Earth;
            public uint Water;
            public uint Fire;
            public uint Wind;
            public uint Unknown3;
            public uint Reincarnation;
            public ulong Unknown4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public byte[] Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public byte[] Nickname;
            public short IsPresent;
            public ushort Unknown5;
            public ushort Unknown6;
        }

        public struct Pet
        {
            public uint PetID;
            public uint HP;
            public uint MaxHP;
            public int EXP;
            public int NextEXP;
            public uint Level;
            public uint Attack;
            public uint Armor;
            public uint Speed;
            public uint Loyalty;
            public uint Earth;
            public uint Water;
            public uint Fire;
            public uint Wind;
            public uint Reincarnation;
            public string Name;
            public string Nickname;
            public bool IsPresent;

            public Pet(RawPet data)
            {
                PetID = data.PetID;
                HP = data.HP;
                MaxHP = data.MaxHP;
                EXP = data.EXP;
                NextEXP = data.NextEXP;
                Level = data.Level;
                Attack = data.Attack;
                Armor = data.Armor;
                Speed = data.Speed;
                Loyalty = data.Loyalty;
                Earth = data.Earth / 10;
                Water = data.Water / 10;
                Fire = data.Fire / 10;
                Wind = data.Wind / 10;
                Reincarnation = data.Reincarnation;
                Name = StringConverter.Big5ToUtf8(data.Name);
                Nickname = StringConverter.Big5ToUtf8(data.Nickname);
                IsPresent = data.IsPresent == 1;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawItemContainer
        {
            public uint Unknown1;
            public uint Unknown2;
            public RawItem Helmet;
            public RawItem Armor;
            public RawItem Weapon;
            public RawItem LeftAccessory;
            public RawItem RightAccessory;
            public RawItem Belt;
            public RawItem Shield;
            public RawItem Boots;
            public RawItem Gloves;
            public RawItem Item1;
            public RawItem Item2;
            public RawItem Item3;
            public RawItem Item4;
            public RawItem Item5;
            public RawItem Item6;
            public RawItem Item7;
            public RawItem Item8;
            public RawItem Item9;
            public RawItem Item10;
            public RawItem Item11;
            public RawItem Item12;
            public RawItem Item13;
            public RawItem Item14;
            public RawItem Item15;
        }

        [StructLayout(LayoutKind.Sequential, Size = 0x184)]
        internal struct RawItem
        {
            public uint Stack;
            public uint Unknown1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public byte[] Unknown2;
            public short IsPresent;
            public ushort Unknown3;
            public uint Unknown4;
            public ushort Unknown5;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 46)]
            public byte[] Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 85)]
            public byte[] Description;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Durability;
        }

        internal struct Item
        {
            public uint Stack;
            public bool IsPresent;
            public string Name;
            public string Description;
            public bool IsFragile;
            public int Durability;

            private static readonly Regex DURABILITY_REGEX = new(@"\d+");

            public Item(RawItem data)
            {
                Stack = data.Stack;
                IsPresent = data.IsPresent == 1;
                Name = StringConverter.Big5ToUtf8(data.Name);
                Description = StringConverter.Big5ToUtf8(data.Description);

                string buffer = StringConverter.Big5ToUtf8(data.Durability);
                IsFragile = !buffer.Contains("不會損壞");
                var match = DURABILITY_REGEX.Match(buffer);
                Durability = match.Success ? int.Parse(match.Value) : -1;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawTeammateContainer
        {
            public RawTeammate Teammate1;
            public RawTeammate Teammate2;
            public RawTeammate Teammate3;
            public RawTeammate Teammate4;
            public RawTeammate Teammate5;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawTeammate
        {
            public uint IsPresent;
            public uint Unknown1;
            public uint Level;
            public uint MaxHP;
            public uint HP;
            public uint MP;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Name;
            public uint Unknown2;
        }

        internal struct Teammate
        {
            public bool IsPresent;
            public uint Level;
            public uint MaxHP;
            public uint HP;
            public uint MP;
            public string Name;

            public Teammate(RawTeammate data)
            {
                IsPresent = data.IsPresent == 1;
                Level = data.Level;
                MaxHP = Math.Max(data.MaxHP, data.HP);
                HP = data.HP;
                MP = data.MP;
                Name = StringConverter.Big5ToUtf8(data.Name);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawBattleMemberContainer
        {
            public RawBattleMember Ally1;
            public RawBattleMember Ally2;
            public RawBattleMember Ally3;
            public RawBattleMember Ally4;
            public RawBattleMember Ally5;
            public RawBattleMember AllyPet1;
            public RawBattleMember AllyPet2;
            public RawBattleMember AllyPet3;
            public RawBattleMember AllyPet4;
            public RawBattleMember AllyPet5;
            public RawBattleMember BackEnemy1;
            public RawBattleMember BackEnemy2;
            public RawBattleMember BackEnemy3;
            public RawBattleMember BackEnemy4;
            public RawBattleMember BackEnemy5;
            public RawBattleMember FrontEnemy1;
            public RawBattleMember FrontEnemy2;
            public RawBattleMember FrontEnemy3;
            public RawBattleMember FrontEnemy4;
            public RawBattleMember FrontEnemy5;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RawBattleMember
        {
            public int NamePointer;        //32 bytes
            public int UnknownPointer1;	   //nickname
            public uint ID;
            public uint Level;
            public uint HP;
            public uint MaxHP;
            public uint Unknown1;          //BC_flag
            public uint HasRide;
            public int RideNamePointer;
            public uint RideLevel;
            public uint RideHP;
            public uint RideMaxHP;
        }

        internal struct BattleMember
        {
            public readonly string Name;
            public readonly uint ID;
            public readonly uint Level;
            public readonly uint HP;
            public readonly uint MaxHP;
            public readonly bool HasRide;
            public readonly string RideName;
            public readonly uint RideLevel;
            public readonly uint RideHP;
            public readonly uint RideMaxHP;

            public BattleMember(RawBattleMember data, string name, string rideName)
            {
                Name = name;
                ID = data.ID;
                Level = data.Level;
                HP = data.HP;
                MaxHP = data.MaxHP;
                HasRide = data.HasRide == 1;
                RideName = rideName;
                RideLevel = data.RideLevel;
                RideHP = data.RideHP;
                RideMaxHP = data.RideMaxHP;
            }
        }
    }

    namespace GameStructures
    {
        /// <summary>
        /// These structures are not used. In testing it was shown that it resulted in slower performance.
        /// </summary>
        /// 

        //[StructLayout(LayoutKind.Sequential)]
        //public struct RawChatContainer
        //{
        //    public RawChat Line1;
        //    public RawChat Line2;
        //    public RawChat Line3;
        //    public RawChat Line4;
        //    public RawChat Line5;
        //    public RawChat Line6;
        //    public RawChat Line7;
        //    public RawChat Line8;
        //    public RawChat Line9;
        //    public RawChat Line10;
        //    public RawChat Line11;
        //    public RawChat Line12;
        //    public RawChat Line13;
        //    public RawChat Line14;
        //    public RawChat Line15;
        //    public RawChat Line16;
        //    public RawChat Line17;
        //    public RawChat Line18;
        //    public RawChat Line19;
        //    public RawChat Line20;
        //}

        //[StructLayout(LayoutKind.Sequential)]
        //public struct RawChat
        //{
        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x105)]
        //    public byte[] Text;
        //    public byte Color;
        //    public int Unknown1;
        //}

        //public readonly struct ChatBuffer
        //{
        //    public readonly string[] LineText;
        //    public readonly int[] LineColors;

        //    public ChatBuffer(RawChatContainer container)
        //    {
        //        LineText = new string[20] {
        //            StringConverter.Big5ToUtf8(container.Line1.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line2.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line3.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line4.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line5.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line6.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line7.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line8.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line9.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line10.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line11.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line12.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line13.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line14.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line15.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line16.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line17.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line18.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line19.Text).TrimEnd(),
        //            StringConverter.Big5ToUtf8(container.Line20.Text).TrimEnd(),
        //        };

        //        LineColors = new int[20] {
        //            container.Line1.Color,
        //            container.Line2.Color,
        //            container.Line3.Color,
        //            container.Line4.Color,
        //            container.Line5.Color,
        //            container.Line6.Color,
        //            container.Line7.Color,
        //            container.Line8.Color,
        //            container.Line9.Color,
        //            container.Line10.Color,
        //            container.Line11.Color,
        //            container.Line12.Color,
        //            container.Line13.Color,
        //            container.Line14.Color,
        //            container.Line15.Color,
        //            container.Line16.Color,
        //            container.Line17.Color,
        //            container.Line18.Color,
        //            container.Line19.Color,
        //            container.Line20.Color,
        //        };
        //    }
        //}
    }
}
