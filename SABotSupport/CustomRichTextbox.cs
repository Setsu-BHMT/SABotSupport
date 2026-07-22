using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SABotSupport
{
    public partial class CustomRichTextbox : RichTextBox
    {
        private const int WM_USER = 0x0400;
        private const int EM_SETEVENTMASK = WM_USER + 69;
        private const int WM_SETREDRAW = 0x0b; 
        private const int WM_HSCROLL = 0x0114;
        private const int WM_VSCROLL = 0x0115;

        private volatile bool isDrawingSuspended = false;
        private IntPtr oldEventMask;
        private SCROLLINFO oldHorzScrollInfo = new();
        private SCROLLINFO oldVertScrollInfo = new();
        private readonly Object syncRoot = new();
        private CancellationTokenSource cancelAppendTokenSource;

        private readonly List<ChatLine> displayedChatLines = new();
        internal IReadOnlyCollection<ChatLine> DisplayedChatLines
        {
            get
            {
                return displayedChatLines.AsReadOnly();
            }
        }

        #region PInvoke

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, IntPtr wParam, ref Point lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public enum ScrollInfoMask : uint
        {
            SIF_RANGE = 0x1,
            SIF_PAGE = 0x2,
            SIF_POS = 0x4,
            SIF_DISABLENOSCROLL = 0x8,
            SIF_TRACKPOS = 0x10,
            SIF_ALL = (SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS),
        }
        public enum SBOrientation : int
        {
            SB_HORZ = 0x0,
            SB_VERT = 0x1,
            SB_CTL = 0x2,
            SB_BOTH = 0x3
        }
        [Serializable, StructLayout(LayoutKind.Sequential)]
        struct SCROLLINFO
        {
            public int cbSize; // (uint) int is because of Marshal.SizeOf
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

        [DllImport("user32.dll")]
        static extern int SetScrollInfo(IntPtr hwnd, int fnBar, [In] ref SCROLLINFO lpsi, bool fRedraw);


        #endregion

        public CustomRichTextbox()
        {
            InitializeComponent();

            oldHorzScrollInfo.cbSize = Marshal.SizeOf(oldHorzScrollInfo);
            oldHorzScrollInfo.fMask = (int)ScrollInfoMask.SIF_ALL;
            oldVertScrollInfo.cbSize = Marshal.SizeOf(oldVertScrollInfo);
            oldVertScrollInfo.fMask = (int)ScrollInfoMask.SIF_ALL;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }

        public void SuspendDrawing()
        {
            if (isDrawingSuspended)
                return;

            SendMessage(this.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            oldEventMask = (IntPtr)SendMessage(this.Handle, EM_SETEVENTMASK, IntPtr.Zero, IntPtr.Zero);

            isDrawingSuspended = true;
        }

        public void ResumeDrawing()
        {
            if (!isDrawingSuspended)
                return;

            SendMessage(this.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            SendMessage(this.Handle, EM_SETEVENTMASK, IntPtr.Zero, oldEventMask);
            this.Invalidate();

            isDrawingSuspended = false;
        }

        public void SaveScrollPos()
        {
            GetScrollInfo(this.Handle, (int)SBOrientation.SB_HORZ, ref oldHorzScrollInfo);
            GetScrollInfo(this.Handle, (int)SBOrientation.SB_VERT, ref oldVertScrollInfo);
        }

        public void RestoreScrollPos()
        {
            SetScrollInfo(this.Handle, (int)SBOrientation.SB_VERT, ref oldVertScrollInfo, true);
            SetScrollInfo(this.Handle, (int)SBOrientation.SB_HORZ, ref oldHorzScrollInfo, true);
            SendMessage(Handle, WM_VSCROLL, new IntPtr(5 + 0x10000 * oldVertScrollInfo.nPos), IntPtr.Zero);
            SendMessage(Handle, WM_HSCROLL, new IntPtr(5 + 0x10000 * oldHorzScrollInfo.nPos), IntPtr.Zero);
        }

        public void ScrollToEnd()
        {
            this.SelectionStart = this.TextLength;
            this.ScrollToCaret();
        }

        public bool IsScrollBarAtBottom()
        {
            Point p = new(this.Width, this.Height);
            int index = this.GetCharIndexFromPosition(p);

            return index + 1 >= this.TextLength;
        }

        /// <summary>
        /// Appends text to the current text of the textbox, and ends the line.
        /// </summary>
        /// <remarks>
        /// The second parameter is a delegate that will determine which lines need name displays.
        /// </remarks>
        internal void AppendLines(IEnumerable<ChatLine> lines, Func<ChatLine, bool> nameDisplaySelector)
        {
            //when the RTB is empty line count will be 0, which is what we should return
            //when the RTB is not empty then we need to return line count - 1
            //the Math.Max call ensures we take care of both cases
            var pos = Math.Max(0, Lines.Length - 1);

            lock (syncRoot)
            {
                base.AppendText(String.Join(String.Empty, lines.Select(FormatLine)));
                displayedChatLines.AddRange(lines);
            }

            var smallFont = new Font(this.Font.FontFamily, 8);
            cancelAppendTokenSource = new();

            //paint the lines after we append
            for (int i = lines.Count() - 1; i >= Math.Max(0, lines.Count() - 100); i--)
            {
                if (cancelAppendTokenSource.Token.IsCancellationRequested)
                    break;

                var index = pos + i;
                var line = displayedChatLines[index];
                var textStart = this.GetFirstCharIndexFromLine(index) + 15; //assumes the timestamp length is 14 + 1 space

                //timestamp section is left as default font

                //paint text section
                this.Select(textStart, line.Text.Length);
                this.SelectionColor = line.Color;
                this.SelectionBackColor = (line.Color == Color.White) ? Color.DarkGray : this.BackColor;

                //paint name section, if exists
                if (nameDisplaySelector(line))
                {
                    var nameStart = textStart + line.Text.Length + 1;

                    this.Select(nameStart, line.Name.Length);
                    this.SelectionColor = Color.DarkGray;
                    this.SelectionFont = smallFont;
                    this.SelectionAlignment = HorizontalAlignment.Right;    //this actually causes the entire line to be right aligned
                }
            }

            this.DeselectAll(); //necessary otherwise restoring scroll position won't work

            return;

            string FormatLine(ChatLine line)
            {   //note: if any of these formats changes then the index numbers above must also be adjusted
                return nameDisplaySelector(line) ? 
                    $"<{line.Timestamp: hh:mm:ss tt}> {line.Text} {line.Name}{Environment.NewLine}" :
                    $"<{line.Timestamp: hh:mm:ss tt}> {line.Text}{Environment.NewLine}";
            }
        }

        /// <summary>
        /// Clears all text from the textbox control.
        /// </summary>
        internal new void ResetText()
            => Clear();

        /// <summary>
        /// Clears all text from the textbox control.
        /// </summary>
        internal new void Clear()
        {
            cancelAppendTokenSource?.Cancel();

            lock (syncRoot)
            {
                base.Clear();
                displayedChatLines.Clear();
            }
        }
    }
}
