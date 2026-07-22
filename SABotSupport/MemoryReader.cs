using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;

using SABotSupport.ASSAStructures;

namespace SABotSupport
{
    internal readonly struct DataPackage
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public Map Map { get; }
        public Player Player { get; }
        public List<Pet> Pets { get; }
        public Item Helmet { get; }
        public Item Armor { get; }
        public Item Weapon { get; }
        public Item LeftAccessory { get; }
        public Item RightAccessory { get; }
        public Item Belt { get; }
        public Item Shield { get; }
        public Item Boots { get; }
        public Item Gloves { get; }
        public List<Item> Items { get; }
        public List<Teammate> Teammates { get; }
        public List<BattleMember> Allies { get; }
        public List<BattleMember> AllyPets { get; }
        public List<BattleMember> Enemies { get; }
        public int Turn { get; }
        public int Encounter { get; }
        public int ScreenState { get; }
        public IntPtr GameClientHandle { get; }
        public GameClientPackage GameClientData { get; }
        public bool IsInitialized { get; }
        public bool IsOnline => ScreenState == 9 || ScreenState == 10;
        public bool IsVisible => IsWindowVisible(GameClientHandle);

        public DataPackage(RawDataComposite dataComposite, RawPetContainer petContainer,
                           RawItemContainer itemContainer, RawTeammateContainer teammateContainer,
                           RawBattleMemberContainer battleMemberContainer, string[] names, string[] rideNames,
                           GameClientPackage gameClientData)
        {
            Map = new(dataComposite.Map);
            Player = new(dataComposite.Player);
            Pets = new()
            {
                new Pet(petContainer.Pet1),
                new Pet(petContainer.Pet2),
                new Pet(petContainer.Pet3),
                new Pet(petContainer.Pet4),
                new Pet(petContainer.Pet5),
            };
            Helmet = new Item(itemContainer.Helmet);
            Armor = new Item(itemContainer.Armor);
            Weapon = new Item(itemContainer.Weapon);
            LeftAccessory = new Item(itemContainer.LeftAccessory);
            RightAccessory = new Item(itemContainer.RightAccessory);
            Belt = new Item(itemContainer.Belt);
            Shield = new Item(itemContainer.Shield);
            Boots = new Item(itemContainer.Boots);
            Gloves = new Item(itemContainer.Gloves);

            Items = new()
            {
                new Item(itemContainer.Item1),
                new Item(itemContainer.Item2),
                new Item(itemContainer.Item3),
                new Item(itemContainer.Item4),
                new Item(itemContainer.Item5),
                new Item(itemContainer.Item6),
                new Item(itemContainer.Item7),
                new Item(itemContainer.Item8),
                new Item(itemContainer.Item9),
                new Item(itemContainer.Item10),
                new Item(itemContainer.Item11),
                new Item(itemContainer.Item12),
                new Item(itemContainer.Item13),
                new Item(itemContainer.Item14),
                new Item(itemContainer.Item15)
            };

            Teammates = new()
            {
                new Teammate(teammateContainer.Teammate1),
                new Teammate(teammateContainer.Teammate2),
                new Teammate(teammateContainer.Teammate3),
                new Teammate(teammateContainer.Teammate4),
                new Teammate(teammateContainer.Teammate5)
            };

            Allies = new()
            {
                new BattleMember(battleMemberContainer.Ally1, names[0], rideNames[0]),
                new BattleMember(battleMemberContainer.Ally2, names[1], rideNames[1]),
                new BattleMember(battleMemberContainer.Ally3, names[2], rideNames[2]),
                new BattleMember(battleMemberContainer.Ally4, names[3], rideNames[3]),
                new BattleMember(battleMemberContainer.Ally5, names[4], rideNames[4])
            };

            AllyPets = new()
            {
                new BattleMember(battleMemberContainer.AllyPet1, names[5], rideNames[5]),
                new BattleMember(battleMemberContainer.AllyPet2, names[6], rideNames[6]),
                new BattleMember(battleMemberContainer.AllyPet3, names[7], rideNames[7]),
                new BattleMember(battleMemberContainer.AllyPet4, names[8], rideNames[8]),
                new BattleMember(battleMemberContainer.AllyPet5, names[9], rideNames[9])
            };

            Enemies = new();
            if (!String.IsNullOrEmpty(names[10]) ||
                !String.IsNullOrEmpty(names[11]) ||
                !String.IsNullOrEmpty(names[12]) ||
                !String.IsNullOrEmpty(names[13]) ||
                !String.IsNullOrEmpty(names[14]))
            {   //ignore the back row if they're not present
                Enemies.Add(new BattleMember(battleMemberContainer.BackEnemy1, names[10], rideNames[10]));
                Enemies.Add(new BattleMember(battleMemberContainer.BackEnemy2, names[11], rideNames[11]));
                Enemies.Add(new BattleMember(battleMemberContainer.BackEnemy3, names[12], rideNames[12]));
                Enemies.Add(new BattleMember(battleMemberContainer.BackEnemy4, names[13], rideNames[13]));
                Enemies.Add(new BattleMember(battleMemberContainer.BackEnemy5, names[14], rideNames[14]));
            }
            Enemies.Add(new BattleMember(battleMemberContainer.FrontEnemy1, names[15], rideNames[15]));
            Enemies.Add(new BattleMember(battleMemberContainer.FrontEnemy2, names[16], rideNames[16]));
            Enemies.Add(new BattleMember(battleMemberContainer.FrontEnemy3, names[17], rideNames[17]));
            Enemies.Add(new BattleMember(battleMemberContainer.FrontEnemy4, names[18], rideNames[18]));
            Enemies.Add(new BattleMember(battleMemberContainer.FrontEnemy5, names[19], rideNames[19]));

            Turn = dataComposite.Turn;
            Encounter = dataComposite.Encounter;
            ScreenState = dataComposite.ScreenState;
            GameClientHandle = new(dataComposite.GameHandle);
            GameClientData = gameClientData;

            IsInitialized = true;
        }
    }

    internal readonly struct GameClientPackage
    {
        public string CurrentAccount { get; }
        public int CurrentCharacter { get; }        //1 or 2
        public string Server { get; }
        public int InputColor { get; }
        public int CurrentChatIndex { get; }
        public (string Text, int Color)[] ChatBuffer { get; }
        public bool IsInitialized { get; }

        public GameClientPackage(string currentAccount, int currentCharacter, string server, int inputColor,
                                 int currentChatIndex, string[] chatBuffer, int[] lineColors)
        {
            CurrentAccount = currentAccount;
            CurrentCharacter = currentCharacter;
            Server = server;
            InputColor = inputColor;
            CurrentChatIndex = currentChatIndex;
            ChatBuffer = Enumerable.Zip(chatBuffer, lineColors, (x, y) => (Text: x, Color: y)).ToArray();

            IsInitialized = true;
        }
    }

    internal static class MemoryReader
    {
        private const int PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess,
            int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        internal static DataPackage GetAssaData(int processID, bool skipGameClientData = true)
        {
            const int MAP_STRUCT_OFFSET = 0xF5078;
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                using Process process = Process.GetProcessById(processID);
                int baseAddress = process.MainModule.BaseAddress.ToInt32();
                processHandle = OpenProcess(PROCESS_VM_READ, false, process.Id);

                //read data composite
                byte[] buffer = ReadBytes(processHandle, baseAddress + MAP_STRUCT_OFFSET, Marshal.SizeOf<RawDataComposite>());
                RawDataComposite dataComposite = BytesToStruct<RawDataComposite>(buffer);

                //read pets
                buffer = ReadBytes(processHandle, dataComposite.PetPointer, Marshal.SizeOf<RawPetContainer>());
                RawPetContainer petContainer = BytesToStruct<RawPetContainer>(buffer);

                //read items
                buffer = ReadBytes(processHandle, dataComposite.ItemPointer, Marshal.SizeOf<RawItemContainer>());
                RawItemContainer itemContainer = BytesToStruct<RawItemContainer>(buffer);

                //read teammate
                buffer = ReadBytes(processHandle, dataComposite.TeammatePointer, Marshal.SizeOf<RawTeammateContainer>());
                RawTeammateContainer teammateContainer = BytesToStruct<RawTeammateContainer>(buffer);

                //read battle
                buffer = ReadBytes(processHandle, dataComposite.BattlePointer, Marshal.SizeOf<RawBattleMemberContainer>());
                RawBattleMemberContainer battleMemberContainer = BytesToStruct<RawBattleMemberContainer>(buffer);

                //read names in battle
                var names = ReadUtf8StringsFromPointerList(processHandle, battleMemberContainer.GetNamePointers());
                var rideNames = ReadUtf8StringsFromPointerList(processHandle, battleMemberContainer.GetRideNamePointers());

                //read game client data
                var gameClientData = (skipGameClientData) ? default : GetGameClientData(new IntPtr(dataComposite.GameHandle));

                return new DataPackage(dataComposite, petContainer, itemContainer, teammateContainer, battleMemberContainer,
                                       names, rideNames, gameClientData);
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is ArgumentException)
            {
                //ASSA likely died
                return default;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        private static readonly Regex SERVER_REGEX = new(@"(?<=\[)[^\[\]]+(?=\])");

        internal static GameClientPackage GetGameClientData(IntPtr windowHandle)
        {
            const int SERVER_CONNECTION_MESSAGE_OFFSET = 0x415D140;
            const int CURRENT_CHAR_OFFSET = 0x42353B4;
            const int CURRENT_ACCOUNT_PACKET_BUFFER_OFFSET = 0x4AC28B8;
            const int CHAT_FIRST_LINE_OFFSET = 0x146DA8;
            const int CHAT_FIRST_COLOR_OFFSET = 0x146EAD;
            const int CHAT_LINE_BUFFER_SIZE = 0x10C;
            const int TEXT_INPUT_COLOR_OFFSET = 0x14C4F0;       //00 - 0A
            //const int VISIBLE_CHAT_LINES_OFFSET = 0x14C50C;
            const int CURRENT_CHAT_INDEX_OFFSET = 0x14C518;

            GetWindowThreadProcessId(windowHandle, out uint processID);

            using Process process = Process.GetProcessById(Convert.ToInt32(processID));
            int baseAddress;
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                baseAddress = process.MainModule.BaseAddress.ToInt32();
                processHandle = OpenProcess(PROCESS_VM_READ, false, process.Id);

                //read account data
                string currentAccount = ReadAsciiString(processHandle, baseAddress + CURRENT_ACCOUNT_PACKET_BUFFER_OFFSET, 30);
                int index = currentAccount.IndexOf("www.longzoro.com");
                currentAccount = (index > 0) ? currentAccount.Remove(index) : String.Empty;

                //read selected character
                int currentCharacter = ReadInt8(processHandle, baseAddress + CURRENT_CHAR_OFFSET);

                //read server connection message
                var match = SERVER_REGEX.Match(ReadBig5String(processHandle, baseAddress + SERVER_CONNECTION_MESSAGE_OFFSET, 30));
                string server = (match.Success) ? match.Value : String.Empty;

                //read chat data
                int inputColor = ReadInt8(processHandle, baseAddress + TEXT_INPUT_COLOR_OFFSET);
                int currentChatIndex = ReadInt8(processHandle, baseAddress + CURRENT_CHAT_INDEX_OFFSET);

                //fix chat index
                //note: chat index is 0-19 with 0 meaning the last index,
                //      so we adjust it to make it work as a direct index for the chat buffer array
                if (currentChatIndex-- == 0)
                {
                    currentChatIndex = 19;
                }

                //read chat buffer (20 lines)
                List<string> chatBuffer = new(20);
                List<int> lineColors = new(20);
                for (int i = 0; i < 20; i++)
                {
                    chatBuffer.Add(ReadBig5String(processHandle, baseAddress + CHAT_FIRST_LINE_OFFSET + CHAT_LINE_BUFFER_SIZE * i, CHAT_LINE_BUFFER_SIZE).Trim());
                    lineColors.Add(ReadInt8(processHandle, baseAddress + CHAT_FIRST_COLOR_OFFSET + CHAT_LINE_BUFFER_SIZE * i));
                }

                return new GameClientPackage(currentAccount, currentCharacter, server, inputColor, currentChatIndex, 
                                             chatBuffer.ToArray(), lineColors.ToArray());
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return default;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        private static int ReadInt8(IntPtr processHandle, int address)
            => ReadBytes(processHandle, address, 1).First();
        private static int ReadInt32(IntPtr processHandle, int address)
            => BitConverter.ToInt32(ReadBytes(processHandle, address, 4), 0);

        private static readonly Encoding BIG5 = Encoding.GetEncoding("BIG5");
        private static string ReadAsciiString(IntPtr processHandle, int address, int size)
            => ReadString(processHandle, address, size, Encoding.ASCII);
        private static string ReadBig5String(IntPtr processHandle, int address, int size)
            => ReadString(processHandle, address, size, BIG5);
        private static string ReadString(IntPtr processHandle, int address, int size, Encoding encoding)
        {
            var buffer = encoding.GetString(ReadBytes(processHandle, address, size));
            var index = buffer.IndexOf('\0');

            return (index < 0) ? buffer : buffer.Remove(index);
        }

        private static byte[] ReadBytes(IntPtr processHandle, int address, int size)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[size];

            ReadProcessMemory(processHandle, address, buffer, buffer.Length, ref bytesRead);

            return buffer;
        }

        private static T BytesToStruct<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        private static T[] BytesToStructArray<T>(byte[] bytes) where T : struct
        {
            T[] destination = new T[bytes.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        private static string[] ReadUtf8StringsFromPointerList(IntPtr processHandle, IEnumerable<int> pointers)
        {
            List<string> output = new();

            foreach (int p in pointers)
            {
                output.Add((p == 0) ? String.Empty : ReadString(processHandle, p, 32, Encoding.Unicode));
            }

            return output.ToArray();
        }

        private static IEnumerable<int> GetNamePointers(this RawBattleMemberContainer container)
        {
            yield return container.Ally1.NamePointer;
            yield return container.Ally2.NamePointer;
            yield return container.Ally3.NamePointer;
            yield return container.Ally4.NamePointer;
            yield return container.Ally5.NamePointer;
            yield return container.AllyPet1.NamePointer;
            yield return container.AllyPet2.NamePointer;
            yield return container.AllyPet3.NamePointer;
            yield return container.AllyPet4.NamePointer;
            yield return container.AllyPet5.NamePointer;
            yield return container.BackEnemy1.NamePointer;
            yield return container.BackEnemy2.NamePointer;
            yield return container.BackEnemy3.NamePointer;
            yield return container.BackEnemy4.NamePointer;
            yield return container.BackEnemy5.NamePointer;
            yield return container.FrontEnemy1.NamePointer;
            yield return container.FrontEnemy2.NamePointer;
            yield return container.FrontEnemy3.NamePointer;
            yield return container.FrontEnemy4.NamePointer;
            yield return container.FrontEnemy5.NamePointer;
        }

        private static IEnumerable<int> GetRideNamePointers(this RawBattleMemberContainer container)
        {
            yield return container.Ally1.RideNamePointer;
            yield return container.Ally2.RideNamePointer;
            yield return container.Ally3.RideNamePointer;
            yield return container.Ally4.RideNamePointer;
            yield return container.Ally5.RideNamePointer;
            yield return container.AllyPet1.RideNamePointer;
            yield return container.AllyPet2.RideNamePointer;
            yield return container.AllyPet3.RideNamePointer;
            yield return container.AllyPet4.RideNamePointer;
            yield return container.AllyPet5.RideNamePointer;
            yield return container.BackEnemy1.RideNamePointer;
            yield return container.BackEnemy2.RideNamePointer;
            yield return container.BackEnemy3.RideNamePointer;
            yield return container.BackEnemy4.RideNamePointer;
            yield return container.BackEnemy5.RideNamePointer;
            yield return container.FrontEnemy1.RideNamePointer;
            yield return container.FrontEnemy2.RideNamePointer;
            yield return container.FrontEnemy3.RideNamePointer;
            yield return container.FrontEnemy4.RideNamePointer;
            yield return container.FrontEnemy5.RideNamePointer;
        }
    }
}
