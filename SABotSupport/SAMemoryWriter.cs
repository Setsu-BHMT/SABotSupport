using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SABotSupport
{
    internal static class SAMemoryWriter
    {
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess,
            int lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess,
            int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
                
        internal static bool ChangeTextInputColor(IntPtr windowHandle, int colorCode)
        {
            const int TEXT_INPUT_COLOR_OFFSET = 0x14C4F0;       //00 - 0A
            byte[] buffer = new byte[1] { (byte)(colorCode & 0xFF) };

            return WriteBytes(windowHandle, TEXT_INPUT_COLOR_OFFSET, buffer);
        }

        internal static bool SendChatMessage(IntPtr windowHandle, string message)
        {
            const int TEXT_INPUT_BUFFER_OFFSET = 0x14C3E8;      //max 87 bytes
            const int TEXT_INPUT_SIZE_OFFSET = 0x14C4EF;        //max value 86
            //const int TEXT_INPUT_CARET_POS_OFFSET = 0x14C4F1;   //max value 86

            //make sure message is not more than 86 bytes (reserve 1 byte for null terminator)
            if (message.Length > 86)
            {
                message = message.Remove(86);
            }
            StringBuilder sb = new(message);
            var big5 = Encoding.GetEncoding("BIG5");
            while (big5.GetByteCount(sb.ToString()) > 86)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            byte[] buffer1 = Encoding.GetEncoding("BIG5").GetBytes(sb.ToString() + '\0');
            byte[] buffer2 = new byte[1] { (byte)((buffer1.Length - 1) & 0xFF) };

            Debug.Assert(buffer1.Length <= 87, "buffer1 cannot be larger than 87 bytes");

            var addressBufferTuples = new Tuple<int, byte[]>[] { 
                new Tuple<int, byte[]>(TEXT_INPUT_BUFFER_OFFSET, buffer1),
                new Tuple<int, byte[]>(TEXT_INPUT_SIZE_OFFSET, buffer2),
            };

            var result = WriteBytes(windowHandle, addressBufferTuples);
            if (!result)
                return result;

            //send enter key to the game window
            //WM_KEYDOWN = 0x0100
            //VK_ENTER = 0x0D
            PostMessage(windowHandle, 0x0100, 0x0D, 0);
            return result;
        }

        internal static async Task<bool> SetAccountPassword(IntPtr windowHandle, string account, string password)
        {
            const int ACCOUNT_OFFSET = 0x4151298;   //max 13 bytes, max usable 12 bytes
            const int PASSWORD_OFFSET = 0x415CA78;  //max 13 bytes

            //make sure account/password are 12 chars or less
            //note: must be ascii so 1 char = 1 byte
            Debug.Assert(windowHandle != IntPtr.Zero, "windowHandle cannot be 0");
            Debug.Assert(account.Length <= 12, "account cannot have more than 12 bytes");
            Debug.Assert(password.Length <= 12, "password cannot have more than 12 bytes");

            byte[] buffer1 = Encoding.ASCII.GetBytes(account + "\0");
            byte[] buffer2 = Encoding.ASCII.GetBytes(password + "\0");
            var addressBufferTuples = new Tuple<int, byte[]>[] {
                new Tuple<int, byte[]>(ACCOUNT_OFFSET, buffer1),
                new Tuple<int, byte[]>(PASSWORD_OFFSET, buffer2),
            };

            //write and confirm
            int count = 0;
            byte[] buffer3 = new byte[buffer1.Length];
            while (count++ < 20)
            {
                if (!WriteBytes(windowHandle, addressBufferTuples))
                    return false;

                await Task.Delay(100).ConfigureAwait(false);

                //read it back and check if we succeeded
                if (ReadBytes(windowHandle, ACCOUNT_OFFSET, buffer3) && buffer1.SequenceEqual(buffer3))
                    return true;

                await Task.Delay(300).ConfigureAwait(false);
            }

            return false;

            //turns out committing is not necessary, but working code anyways so kept here as a reference

            //const int WM_MOUSEMOVE = 0x200;
            //const int WM_LBUTTONDOWN = 0x201;
            //const int WM_LBUTTONUP = 0x202;
            //const int MK_LBUTTON = 0x1;

            //if (shouldCommitCredentials)
            //{
            //    //left click the log on button at 360,280
            //    int lParam = MakeLParam(360, 280);

            //    PostMessage(windowHandle, WM_MOUSEMOVE, 0, lParam);

            //    //necessary otherwise the button has no time to react
            //    await Task.Delay(100);

            //    PostMessage(windowHandle, WM_LBUTTONDOWN, MK_LBUTTON, lParam);
            //    PostMessage(windowHandle, WM_LBUTTONUP, 0, lParam);
            //}

            //return true;

            //static int MakeLParam(int x, int y)
            //    => (y << 16) | (x & 0xFFFF);
        }

        private static bool WriteBytes(IntPtr windowHandle, int address, byte[] buffer)
            => WriteBytes(windowHandle, Enumerable.Repeat(new Tuple<int, byte[]>(address, buffer), 1));
        private static bool WriteBytes(IntPtr windowHandle, IEnumerable<Tuple<int, byte[]>> addressBufferTuples)
        {
            GetWindowThreadProcessId(windowHandle, out uint processID);
            var process = Process.GetProcessById(Convert.ToInt32(processID));
            int baseAddress;
            IntPtr processHandle = IntPtr.Zero;
            bool result = true;

            try
            {
                baseAddress = process.MainModule.BaseAddress.ToInt32();
                processHandle = OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, process.Id);

                foreach (var tuple in addressBufferTuples)
                {
                    result &= WriteProcessMemory(processHandle, baseAddress + tuple.Item1, tuple.Item2, tuple.Item2.Length, out int _);
                }

                return result;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }

        private static bool ReadBytes(IntPtr windowHandle, int address, byte[] buffer)
        {
            GetWindowThreadProcessId(windowHandle, out uint processID);
            var process = Process.GetProcessById(Convert.ToInt32(processID));
            int baseAddress;
            IntPtr processHandle = IntPtr.Zero;

            try
            {
                baseAddress = process.MainModule.BaseAddress.ToInt32();
                processHandle = OpenProcess(PROCESS_VM_READ, false, process.Id);
                int readBytes = 0;

                return ReadProcessMemory(processHandle, baseAddress + address, buffer, buffer.Length, ref readBytes);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                    CloseHandle(processHandle);
            }
        }
    }
}
