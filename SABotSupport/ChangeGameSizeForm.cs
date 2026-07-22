using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SABotSupport
{
    public partial class ChangeGameSizeForm : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        private enum ShowWindowCommands : int
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>      
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
            /// that owns the window is not responding. This flag should only be
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);


        private const int WM_MOVE = 0x0003;

        [DllImport("user32.dll")]
        public static extern Int64 SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static ChangeGameSizeForm openInstance = default;
        private static readonly BindingList<Size> oldSizes = new();

        private decimal ratio;
        private volatile bool isSuppressValueChanged = false;

        public ChangeGameSizeForm()
        { 
            InitializeComponent();

            openInstance = this;
            openInstance.FormClosed += (s, e) => openInstance = default;
        }

        public static bool ActivateOpenInstance()
        {
            if (openInstance != default)
            {
                openInstance.Activate();
                return true;
            }
            else
                return false;
        }

        private void ChangeGameSizeDialog_Load(object sender, EventArgs e)
        {
            oldSizesListbox.DataSource = oldSizes;

            refreshButton.PerformClick();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            var handle = Process.GetProcessesByName("sa_8002a").FirstOrDefault(x => x.MainWindowHandle != IntPtr.Zero)?.MainWindowHandle;
            if (!handle.HasValue)
            {
                currentSizeLabel.Text = "現在大小: 沒有找到石器視窗!";
                NumericUpDownGroupbox.Enabled = oldSizesListbox.Enabled = false;
            }
            else
            {
                GetWindowRect(handle.Value, out RECT rect);

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                currentSizeLabel.Text = $"現在大小: {width}x{height}";
                NumericUpDownGroupbox.Enabled = oldSizesListbox.Enabled = true;

                try
                {
                    isSuppressValueChanged = true;
                    widthNumericUpDown.Value = width;
                    heightNumericUpDown.Value = height;
                    ratio = width * 1.0m / height;
                }
                finally
                {
                    isSuppressValueChanged = false;
                }

                UpdateOldSizes(new Size(width, height));
            }

            applyButton.Enabled = okButton.Enabled = false;
        }

        private void WidthNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            applyButton.Enabled = okButton.Enabled = true;

            if (!keepRatioCheckbox.Checked || isSuppressValueChanged)
                return;

            try
            {
                isSuppressValueChanged = true;
                heightNumericUpDown.Value = Math.Round(widthNumericUpDown.Value / ratio, 0);
            }
            finally
            {
                isSuppressValueChanged = false;
            }
        }

        private void HeightNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            applyButton.Enabled = okButton.Enabled = true;

            if (!keepRatioCheckbox.Checked || isSuppressValueChanged)
                return;

            try
            {
                isSuppressValueChanged = true;
                widthNumericUpDown.Value = Math.Round(heightNumericUpDown.Value * ratio, 0);
            }
            finally
            {
                isSuppressValueChanged = false;
            }
        }

        private void KeepRatioCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (!keepRatioCheckbox.Checked)
                return;

            ratio = widthNumericUpDown.Value * 1.0m / heightNumericUpDown.Value;
        }

        private void OldSizesListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (oldSizesListbox.SelectedIndex < 0)
                return;

            Debug.Assert(oldSizesListbox.SelectedIndex < oldSizes.Count);

            var size = oldSizes[oldSizesListbox.SelectedIndex];

            try
            {
                isSuppressValueChanged = true;
                widthNumericUpDown.Value = size.Width;
                heightNumericUpDown.Value = size.Height;
                ratio = size.Width * 1.0m / size.Height;
            }
            finally
            {
                isSuppressValueChanged = false;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            applyButton.PerformClick();

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            int width = Convert.ToInt32(widthNumericUpDown.Value);
            int height = Convert.ToInt32(heightNumericUpDown.Value);
            bool isRanOnce = false;

            foreach (var process in Process.GetProcessesByName("sa_8002a"))
            {
                var h = process.MainWindowHandle;
                if (h == IntPtr.Zero)
                    continue;

                isRanOnce = true;

                GetWindowRect(h, out RECT rect);
                MoveWindow(h, rect.Left, rect.Top, width, height, true);
                SendMessage(h, WM_MOVE, IntPtr.Zero, IntPtr.Zero);
            }

            if (isRanOnce)
            {
                UpdateOldSizes(new Size(width, height));
            }
            else
            {
                MessageBox.Show("沒有找到石器視窗!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            applyButton.Enabled = false;
        }

        private void UpdateOldSizes(Size newSize)
        {
            if (!oldSizes.Contains(newSize))
            {
                if (oldSizes.Count >= 4)
                {
                    oldSizes.RemoveAt(0);
                }

                oldSizes.Add(newSize);
            }
        }

    }
}
