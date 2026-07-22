using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

namespace SABotSupport
{    
    internal static class AssaAutomation
    {
        private const uint GW_HWNDNEXT = 2;
        private const uint GW_OWNER = 4;
        [DllImport("User32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int VK_UP = 0x26;
        private const int VK_DOWN = 0x28;
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetTopWindow(IntPtr hWnd);

        internal class AssaInstance : IEquatable<AssaInstance>
        {
            public AutomationElement MainForm { get; }
            public AutomationElement HideInstanceCheckbox { get; private set; }
            public AutomationElement ShowInfoButton { get; private set; }
            public AutomationElement CombatSettingsButton { get; private set; }
            public AutomationElement ScriptEditorButton { get; private set; }
            public string CachedName { get; set; } = String.Empty;      //filled by customer
            public string CachedAccount { get; set; } = String.Empty;   //filled by customer
            public int CachedCharacter { get; set; } = 0;               //filled by customer
            private int _processID = 0;
            public int ProcessID => (_processID > 0) ? _processID : GetProcessID();

            public AssaInstance(AutomationElement mainForm)
            {
                MainForm = mainForm;
                
                GetProcessID();
            }

            public async Task InitializeSecondaryControls()
            {
                int count = 0;
                COMException cachedException = null;

                Trace.WriteLine($"Begin InitializeSecondaryControls for {ProcessID}");

                while (HideInstanceCheckbox == default || ShowInfoButton == default || ScriptEditorButton == default)
                {
                    if (count++ >= 3)
                        throw new ApplicationException("Failed to initialize secondary controls within retry limit", cachedException);

                    try
                    {
                        if (HideInstanceCheckbox == default)
                        {
                            HideInstanceCheckbox = await GetControl(MainForm, ControlType.CheckBox, "隱藏石器");
                        }
                        if (ShowInfoButton == default)
                        {
                            ShowInfoButton = await GetControl(MainForm, ControlType.Button, "資料顯示");
                        }
                        if (ScriptEditorButton == default)
                        {
                            ScriptEditorButton = await GetControl(MainForm, ControlType.Button, "腳本制作");
                        }

                        Trace.WriteLine($"[{ProcessID}] 隱藏石器: {HideInstanceCheckbox != default}  資料顯示: {ShowInfoButton != default}  腳本制作: {ScriptEditorButton != default}");
                    }
                    catch (COMException ex)
                    {
                        //timeout exception can happen if the system is busy?
                        //try to wait awhile and retry
                        await Task.Delay(100).ConfigureAwait(false);

                        Trace.WriteLine($"[{ProcessID}] Exception occured at iteration {count}");
                        Trace.Indent();
                        Trace.WriteLine(ex);
                        Trace.Unindent();

                        cachedException = ex;
                    }
                }

                Trace.WriteLine($"End InitializeSecondaryControls for {ProcessID}");
                Trace.Flush();
            }

            public override string ToString()
                => CachedName;

            [MethodImpl(MethodImplOptions.NoOptimization)]
            public bool IsAlive()
            {
                try
                {
                    var a = MainForm.Current.ProcessId;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// Calling this once will set the process ID backing field and the function won't be called again.
            /// May silently fail and give 0 if an exception is encountered.
            /// </summary>
            private int GetProcessID()
            {
                try
                {
                    return _processID = MainForm.Current.ProcessId;
                }
                catch (Exception)
                {
                    return 0;
                }
            }

            public override int GetHashCode()
                => ProcessID.GetHashCode();

            public bool Equals(AssaInstance other)
                => ProcessID == other?.ProcessID;

            public override bool Equals(object other)
                => (other is AssaInstance instance) && this.Equals(instance);

            public static bool operator ==(AssaInstance first, AssaInstance second)
                => ReferenceEquals(first, second) || (first?.Equals(second) ?? false);

            public static bool operator !=(AssaInstance first, AssaInstance second)
                => !(first == second);
        }

        internal static Task<AutomationElement> GetControl(AutomationElement rootElement, ControlType type, TreeScope scope = TreeScope.Children)
        {
            PropertyCondition condition = new(AutomationElement.ControlTypeProperty, type);

            return Task.Run(() => rootElement.FindFirst(scope, condition));
        }
        internal static Task<AutomationElement> GetControl(AutomationElement rootElement, ControlType type, string label)
        {
            AndCondition condition = new(
                new PropertyCondition(AutomationElement.ControlTypeProperty, type),
                new PropertyCondition(AutomationElement.NameProperty, label));

            return Task.Run(() => rootElement.FindFirst(TreeScope.Descendants, condition));
        }
        internal static Task<AutomationElement> GetControl(AutomationElement rootElement, ControlType type, int id)
        {
            AndCondition condition = new(
                new PropertyCondition(AutomationElement.ControlTypeProperty, type),
                new PropertyCondition(AutomationElement.AutomationIdProperty, id.ToString()));

            return Task.Run(() => rootElement.FindFirst(TreeScope.Descendants, condition));
        }

        internal static Task InvokeElement(AutomationElement element)
            => Task.Run(() => (element?.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern)?.Invoke());

        /// <summary>
        /// Returns a dictionary of running ASSA instances with the filepath as key and window handle as value.
        /// </summary>
        internal static async Task<Dictionary<string, IntPtr>> GetAssaProcesses(IntPtr formHandle, bool includeAllVirtualDesktops = false, bool searchAllProcesses = false)
        {
            Trace.WriteLine("Enter GetAssaProcesses");
            Trace.WriteLine($"{nameof(includeAllVirtualDesktops)}: {includeAllVirtualDesktops}");
            Trace.Indent();

            Dictionary<string, IntPtr> output = new(StringComparer.OrdinalIgnoreCase);
            Process[] assaProcesses = default;
            
            //initialize virtual desktop related vars
            //note: on legacy platforms we disable all calls to VDM because the COM component doesn't exist
            VirtualDesktopManager vdm = new();
            Guid myDesktopID = default;
            if (!includeAllVirtualDesktops && VirtualDesktopManager.IsSupported)
            {
                myDesktopID = vdm.GetWindowDesktopId(formHandle);

                Trace.WriteLine($"Current desktop ID is {myDesktopID}");
            }
            else
            {
                Trace.WriteLineIf(!includeAllVirtualDesktops, $"Forcing include all virtual desktops because VirtualDesktopManager is unsupported");

                includeAllVirtualDesktops = true;
            }

            try
            {
                if (searchAllProcesses)
                {   //search all processes and look at the FileDescription field to find ASSA instances with filenames changed to something other than default
                    var processes = await Task.Run(Process.GetProcesses).ConfigureAwait(false);

                    List<Process> temp = [];
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (process.MainModule.FileVersionInfo.FileDescription == "Assa8.0")
                            {
                                temp.Add(process);
                            }
                            else
                            {
                                process.Dispose();
                            }
                        }
                        catch (Exception)
                        {   //ignore access denied entries
                            process.Dispose();
                            continue;
                        }
                    }

                    assaProcesses = temp.ToArray();
                }
                else
                {
                    assaProcesses = await Task.Run(() => Process.GetProcessesByName("Assa8.0B5")).ConfigureAwait(false);
                }

                Trace.WriteLine($"Enumerating {assaProcesses.Length} processes ...");
                Trace.Indent();

                foreach (var process in assaProcesses)
                {
                    var handles = EnumerateProcessWindowHandles(process);
                    Guid desktopID = default;
                    IntPtr mainHandle = handles.First();    //default to first handle, and if VDM is supported try to find the actual main window instead of possibly a popup

                    if (!includeAllVirtualDesktops)
                    {
                        bool skipThisProcess = false;

                        foreach (var handle in handles)
                        {
                            try
                            {
                                desktopID = vdm.GetWindowDesktopId(handle);
                                if (desktopID == Guid.Empty)
                                    continue;   //ignore popup windows that show on every desktop

                                AutomationElement element = AutomationElement.FromHandle(handle);
                                if (element.Current.Name.StartsWith("情報顯示"))
                                    continue;   //ignore 情報顯示 popup window

                                mainHandle = handle;
                                Trace.WriteLine($"[{handle}] desktop ID is {desktopID}");
                                break;
                            }
                            catch (COMException ex)
                            {
                                Trace.WriteLine($"[{handle}] failed to get desktop ID:");
                                Trace.Indent();
                                Trace.WriteLine(ex);
                                Trace.Unindent();

                                //this occurs if the target window is hidden
                                //if so we exclude it for now since other UIAutomation tasks will fail on these
                                skipThisProcess = true;
                                break;
                            }
                        }

                        if (skipThisProcess)
                            continue;
                    }

                    if (includeAllVirtualDesktops || desktopID == myDesktopID)
                    {
                        output.Add(process.MainModule.FileName, mainHandle);

                        Trace.WriteLine($"[{mainHandle}] append to output: {process.MainModule.FileName}");
                    }
                    else
                    {
                        Trace.WriteLine($"[{mainHandle}] ignored due to desktopID mismatch: {process.MainModule.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                throw;
            }
            finally
            {
                if (assaProcesses != default)
                {
                    foreach (var process in assaProcesses)
                        process.Dispose();
                }

                Trace.Unindent();
                Trace.Unindent();
                Trace.WriteLine("Exit GetAssaProcesses");
                Trace.Flush();
            }

            return output;
        }

        /// <param name="formHandle">The main form handle. Only used if includeAllVirtualDesktops is true.</param>
        /// <param name="includeAllVirtualDesktops">Only supported on Windows 10 and later, and ignored in previous versions.</param>
        /// <param name="searchAllProcesses">Uses the file description field instead of exe filename. Search speed is much slower as a result.</param>
        /// <returns></returns>
        internal static async Task<List<AssaInstance>> GetAssaInstances(IntPtr formHandle, bool includeAllVirtualDesktops = false, bool searchAllProcesses = false)
        {
            List<AssaInstance> output = new();

            var assaHandles = (await GetAssaProcesses(formHandle, includeAllVirtualDesktops, searchAllProcesses).ConfigureAwait(false)).AsEnumerable().Select(x => x.Value);

            foreach (var handle in SortByZOrder(assaHandles))
            {
                output.Add(new AssaInstance(AutomationElement.FromHandle(handle)));
            }

            return output;

            static IEnumerable<IntPtr> SortByZOrder(IEnumerable<IntPtr> unsorted)
            {
                var byHandle = unsorted.ToDictionary(x => x);

                for (IntPtr hWnd = GetTopWindow(IntPtr.Zero); hWnd != IntPtr.Zero; hWnd = GetWindow(hWnd, GW_HWNDNEXT))
                {
                    if (byHandle.ContainsKey(hWnd))
                        yield return byHandle[hWnd];
                }
            }
        }

        internal static async Task<List<AssaInstance>> GetAssaInstances_v1_5()
        {
            List<AssaInstance> output = new();
            AutomationElementCollection instances;

            try
            {
                instances = await Task.Run(() => AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ClassNameProperty, "ThunderRT6FormDC"))).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return output;
            }

            foreach (AutomationElement instance in instances)
            {
                try
                {
                    var name = instance.Current.Name;
                    if (!name.StartsWith("Assa"))
                        continue;

                    output.Add(new(instance));
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return output;
        }

        internal static async Task<AutomationElement> GetScriptSubPanelButton(AutomationElement mainWindow, string buttonLabel)
        {
            //get the execute button if the script sub panel is already active
            var button = await GetControl(mainWindow, ControlType.Button, buttonLabel).ConfigureAwait(false);
            if (button != default)
                return button;

            await ActivateSubPanel(mainWindow, "腳本").ConfigureAwait(false);

            return await GetControl(mainWindow, ControlType.Button, buttonLabel).ConfigureAwait(false);
        }

        internal static async Task ActivateSubPanel(AutomationElement mainWindow, string label)
        {
            var panelRadioButton = await GetControl(mainWindow, ControlType.RadioButton, label).ConfigureAwait(false);
            if (panelRadioButton == default)
            {
                mainWindow.SetFocus();
                await Task.Delay(100).ConfigureAwait(false);
                panelRadioButton = await GetControl(mainWindow, ControlType.RadioButton, label).ConfigureAwait(false);
            }
            if (panelRadioButton == default)
                throw new ElementNotAvailableException("Failed to get panel radio button");

            //return fast if the sub panel is already active
            if (IsChecked(panelRadioButton))
                return;

            //click radio button and wait for state to change
            await InvokeElement(panelRadioButton).ConfigureAwait(false);
            do
            {
                await Task.Delay(200).ConfigureAwait(false);
            } while (!IsChecked(panelRadioButton));

            return;

            static bool IsChecked(AutomationElement radioButton)
                => (radioButton.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern).Current.IsSelected;
        }

        internal static async Task<AutomationElement> GetPlayerInfoWindow(AssaInstance instance)
            => await GetChildWindow(instance.MainForm, "資料顯示").ConfigureAwait(false) ??
               await OpenChildWindow(instance.MainForm, instance.ShowInfoButton, "資料顯示").ConfigureAwait(false);

        internal static async Task<AutomationElement> SetPlayerInfoWindowActiveTab(AutomationElement playerInfoWindow, string tabLabel, string paneLabel)
        {
            //tab may already be open, if so skip activation
            var pane = await GetControl(playerInfoWindow, ControlType.Pane, paneLabel).ConfigureAwait(false);
            if (pane != default)
                return pane;

            var tabPage = await GetControl(playerInfoWindow, ControlType.TabItem, tabLabel).ConfigureAwait(false);
            (tabPage?.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern)?.Select();

            return await GetControl(playerInfoWindow, ControlType.Pane, TreeScope.Descendants).ConfigureAwait(false);
        }

        internal static async Task InvokePetContextMenu(AutomationElement playerInfoWindow, AutomationElement tabPane, int petLocation, bool[] petSlotFilledStatus)
        {
            Debug.Assert(petLocation >= 1 && petLocation <= 5, "Pet location must be between 1 and 5, inclusive");
            Debug.Assert(petSlotFilledStatus.Length == 5, "petSlotFilledStatus must have length of 5");

            //wait for the window to be ready
            while (playerInfoWindow.Current.NativeWindowHandle == 0)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }

            var panes = await Task.Run(() => tabPane.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane))).ConfigureAwait(false);
            if (panes.Count == 0)
                throw new ApplicationException("Failed to get pane controls");

            //count the number of clickable panes
            //note: don't call this function if there are no pets available
            double X = 0;
            int numPets = 0;
            foreach (AutomationElement pane in panes)
            {
                if (numPets == 0)
                {
                    X = pane.Current.BoundingRectangle.X;
                    numPets++;
                    continue;
                }

                if (pane.Current.BoundingRectangle.X > X)
                    break;

                X = pane.Current.BoundingRectangle.X;
                numPets++;
            }

            //find the index by iterating from the end and counting the number of available pets
            int index = 0;
            for (int i = petSlotFilledStatus.Length - 1; i >= 0; i--)
            {
                if (i == petLocation - 1)
                    break;
                else if (petSlotFilledStatus[i])
                {
                    index++;
                }
            }

            var targetPane = panes.Cast<AutomationElement>().ElementAt(index);
            System.Windows.Point point;
            try
            {
                point = targetPane.GetClickablePoint();
            }
            catch (NoClickablePointException)
            {
                throw new ApplicationException("Failed to get clickable point");
            }

            var lastPos = Cursor.Position;
            Cursor.Position = new Point((int)point.X, (int)point.Y);

            await Task.Delay(50).ConfigureAwait(false);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, new IntPtr());
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, new IntPtr());

            Cursor.Position = lastPos;

            HideWindow(playerInfoWindow);
        }

        internal static async Task<AutomationElement> GetScriptEditorWindow(AssaInstance instance)
            => await GetChildWindow(instance.MainForm, "腳本制作").ConfigureAwait(false) ??
               await OpenChildWindow(instance.MainForm, instance.ScriptEditorButton, "腳本制作").ConfigureAwait(false);

        internal static (AutomationElement treeItemElement, bool isLastItem) FindScriptInTree(AutomationElement treeElement, string scriptPath)
        {
            Stack<string> folderStack = new();
            int folderDepth = -1;
            string filename = Path.GetFileName(scriptPath);
            bool exitOnNextIteration = false;
            AutomationElement output = default;

            foreach (var itemElement in EnumerateTree(treeElement))
            {
                //we have already found our element and confirmed it is not the last element, so return
                if (exitOnNextIteration)
                    return (output, false);

                string name = itemElement.Current.Name; //filename or foldername

                //do not process if it is not part of the path
                if (!scriptPath.Contains(name))
                    continue;

                int depth = Int32.Parse((itemElement.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern).Current.Value);

                //if the depth is more than 1 away from folderDepth, we are in the wrong branch
                if (depth > folderDepth + 1)
                    continue;

                //if the depth decreased, pop until we are 1 past the current depth, so that the next if check will push
                while (depth <= folderDepth)
                {
                    folderStack.Pop();
                    folderDepth--;
                }

                //if the depth increased, push name onto stack, assuming it's a folder element
                if (depth > folderDepth)
                {
                    folderStack.Push(name);
                    folderDepth++;
                }

                //check if filename matches
                if (name == filename)
                {
                    //build folder path and make sure the full path is found in the argument path
                    if (scriptPath.Contains(Path.Combine(folderStack.Skip(1).Reverse().ToArray())))
                    {
                        output = itemElement;
                        exitOnNextIteration = true;
                    }
                }
            }

            return (output, true);
        }

        /// <remarks>
        /// Passing in a cancellation token will cause the function to wait for the script to end.
        /// </remarks>
        internal static async Task ExecuteScript(AssaInstance controls, string script, CancellationToken cancelToken = default)
        {
            //try to get script editor window, or open it if not open
            var scriptEditorWindow = await GetScriptEditorWindow(controls).ConfigureAwait(false);

            if (!await CopyAndActivateScript(scriptEditorWindow, script).ConfigureAwait(false))
                throw new ApplicationException("傳送腳本至\"腳本製作\"視窗失敗");

            await InvokeRunScriptButton(controls.MainForm, cancelToken).ConfigureAwait(false);
        }

        /// <remarks>
        /// Passing in a cancellation token will cause the function to wait for the script to end.
        /// </remarks>
        internal static async Task ExecuteScript(AssaInstance controls, AutomationElement treeItem, bool isLastItem, CancellationToken cancelToken = default)
        {
            SelectionItemPattern pattern = treeItem.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            pattern.Select();

            int lParam = 1;
            lParam |= 1 << 30;
            lParam |= 1 << 31;

            var handle = new IntPtr(pattern.Current.SelectionContainer.Current.NativeWindowHandle);
            if (handle == IntPtr.Zero)
                throw new ApplicationException("Failed to get valid handle for tree container.");

            //send key up/down events because selecting the element alone doesn't trigger a script load event in ASSA
            if (isLastItem)
            {
                PostMessage(handle, WM_KEYDOWN, VK_UP, 1);
                PostMessage(handle, WM_KEYUP, VK_UP, lParam);
                PostMessage(handle, WM_KEYDOWN, VK_DOWN, 1);
                PostMessage(handle, WM_KEYUP, VK_DOWN, lParam);
            }
            else
            {
                PostMessage(handle, WM_KEYDOWN, VK_DOWN, 1);
                PostMessage(handle, WM_KEYUP, VK_DOWN, lParam);
                PostMessage(handle, WM_KEYDOWN, VK_UP, 1);
                PostMessage(handle, WM_KEYUP, VK_UP, lParam);
            }

            await Task.Delay(100).ConfigureAwait(false);

            await InvokeRunScriptButton(controls.MainForm, cancelToken).ConfigureAwait(false);
        }

        internal static async Task<AutomationElement> LaunchASSA(string path)
        {
            //launch exe
            using Process process = new();
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            await Task.Run(() => process.WaitForInputIdle()).ConfigureAwait(false);

            //get owned windows that the process launches
            var childWindowHandles = EnumerateProcessWindowHandles(process);
            AutomationElement mainWindow = AutomationElement.FromHandle(childWindowHandles.First());

            //get the launch button and press it
            var launchButton = await GetControl(mainWindow, ControlType.Button, 66).ConfigureAwait(false);

            //press and confirm success
            bool success = false;
            while (!success)
            {
                await InvokeElement(launchButton).ConfigureAwait(false);

                for (int i = 0; i < 50; i++)
                {
                    await Task.Delay(100).ConfigureAwait(false);

                    if (success = !launchButton.Current.IsEnabled)
                        break;
                }
            }

            return mainWindow;
        }

        internal static async Task LogOff(AutomationElement mainWindow)
        {
            await ActivateSubPanel(mainWindow, "副控").ConfigureAwait(false);
            var autoLogOnCheckbox = await GetControl(mainWindow, ControlType.CheckBox, "自動登陸").ConfigureAwait(false);
            var logOffCheckbox = await GetControl(mainWindow, ControlType.CheckBox, "快速原登").ConfigureAwait(false);
            if (autoLogOnCheckbox == default)
                throw new ApplicationException($"failed to get {nameof(autoLogOnCheckbox)}");
            if (logOffCheckbox == default)
                throw new ApplicationException($"failed to get {nameof(logOffCheckbox)}");

            //uncheck the auto logon checkbox
            if (IsCheckboxChecked(autoLogOnCheckbox))
            {
                await InvokeElement(autoLogOnCheckbox).ConfigureAwait(false);
            }

            //retry until we successfully get the dialog
            AutomationElement dialog;
            do
            {
                dialog = await TryInvokeDialog().ConfigureAwait(false);
            } while (dialog == default);

            //confirm dialog
            var okButton = await GetControl(dialog, ControlType.Button, 1).ConfigureAwait(false);
            if (okButton == default)
                throw new ApplicationException($"failed to get {nameof(okButton)}");
            await InvokeElement(okButton).ConfigureAwait(false);

            //wait for the log off checkbox to return to unchecked state
            int count = 0;
            do
            {
                await Task.Delay(100).ConfigureAwait(false);

                if (++count == 10)
                {
                    //try clicking the button again
                    try
                    {
                        await InvokeElement(okButton).ConfigureAwait(false);
                        count = 0;
                    }
                    catch (ElementNotAvailableException)
                    {
                        //button has been destroyed, keep waiting...
                    }
                }
            } while (IsCheckboxChecked(logOffCheckbox));

            return;

            async Task<AutomationElement> TryInvokeDialog()
            {
                //build tcs that will be auto cancelled after 2 seconds
                TaskCompletionSource<AutomationElement> tcs = new();
                using CancellationTokenSource cts = new(2000);
                cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

                //add automation event handler to capture window open event
                AutomationEventHandler handler = default;
                Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, mainWindow, TreeScope.Children,
                   handler = (sender, e) =>
                   {
                       var element = sender as AutomationElement;

                       try
                       {
                           Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, mainWindow, handler);
                           tcs.TrySetResult(element);
                       }
                       catch (Exception ex) when (ex is ElementNotAvailableException || ex is UnauthorizedAccessException)
                       {
                           return;
                       }
                   });

                await InvokeElement(logOffCheckbox).ConfigureAwait(false);

                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return default;
                }
            }
        }

        /// <param name="server">The server label to be selected. If empty will not change the current selection.</param>
        /// <param name="mainWindow">The character label to be selected. If empty will not change the current selection.</param>
        /// <remarks>
        /// Should be called AFTER you have set the account/password.
        /// </remarks>
        internal static async Task LogOn(AutomationElement mainWindow, string server = "", string character = "")
        {
            await ActivateSubPanel(mainWindow, "副控").ConfigureAwait(false);
            var autoLogOnCheckbox = await GetControl(mainWindow, ControlType.CheckBox, "自動登陸").ConfigureAwait(false);
            var autoReconnectCheckbox = await GetControl(mainWindow, ControlType.CheckBox, "斷線重登").ConfigureAwait(false);
            var serverSelectCombobox = await GetControl(mainWindow, ControlType.ComboBox, 74).ConfigureAwait(false);
            var charSelectCombobox = await GetControl(mainWindow, ControlType.ComboBox, 75).ConfigureAwait(false);
            if (autoLogOnCheckbox == default || serverSelectCombobox == default || charSelectCombobox == default)
                throw new ElementNotAvailableException("Could not acquire log on related controls.");

            await SelectComboboxItem(serverSelectCombobox, server).ConfigureAwait(false);
            await SelectComboboxItem(charSelectCombobox, character).ConfigureAwait(false);

            if (!IsCheckboxChecked(autoLogOnCheckbox))
            {
                await InvokeElement(autoLogOnCheckbox).ConfigureAwait(false);
            }
            if (autoReconnectCheckbox != default && !IsCheckboxChecked(autoReconnectCheckbox))
            {
                await InvokeElement(autoReconnectCheckbox).ConfigureAwait(false);
            }
        }

#region Private Methods

        private static async Task<AutomationElement> GetChildWindow(AutomationElement mainWindow, string childWindowCaption)
        {
            TreeWalker walker = new(new PropertyCondition(AutomationElement.ClassNameProperty, "ThunderRT6FormDC"));
            AutomationElement instance = default;

            try
            {
                instance = await Task.Run(() => walker.GetFirstChild(AutomationElement.RootElement)).ConfigureAwait(false);
            }
            catch (COMException)
            {
                //if we get a COM exception it could be because the target element isn't ready yet
                //it's up to the caller to wait and retry
                return default;
            }

            while (instance != default)
            {
                if (instance.Current.Name.StartsWith(childWindowCaption) &&
                    HasSameOwnerWindow(mainWindow.Current.NativeWindowHandle, instance.Current.NativeWindowHandle))
                {
                    return instance;
                }

                instance = await Task.Run(() => walker.GetNextSibling(instance)).ConfigureAwait(false);
            }

            return default;
        }

        private static async Task<AutomationElement> OpenChildWindow(AutomationElement mainWindow, AutomationElement button, string childWindowCaption)
        {
            AutomationElement result = default;

            do
            {
                //build tcs that will be auto cancelled after 6 seconds
                TaskCompletionSource<AutomationElement> tcs = new();
                using CancellationTokenSource cts = new(6000);
                cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

                //add automation event handler to capture window open event
                AutomationEventHandler handler = default;
                Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Children,
                   handler = (sender, e) =>
                   {
                       var element = sender as AutomationElement;

                       try
                       {
                           if (element.Current.ClassName == "ThunderRT6FormDC" && element.Current.Name.StartsWith(childWindowCaption) &&
                               HasSameOwnerWindow(element.Current.NativeWindowHandle, mainWindow.Current.NativeWindowHandle))
                           {
                               tcs.TrySetResult(element);
                           }
                       }
                       catch (Exception ex) when (ex is ElementNotAvailableException || ex is UnauthorizedAccessException)
                       {
                           return;
                       }
                   });

                await InvokeElement(button).ConfigureAwait(false);
                try
                {
                    result = await tcs.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    //the window may be open, search for it
                    result = await GetChildWindow(mainWindow, childWindowCaption).ConfigureAwait(false);
                }
                finally
                {
                    Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, handler);
                }
            } while (result == default);

            return result;
        }

        private static IEnumerable<AutomationElement> EnumerateTree(AutomationElement treeElement)
        {
            if (treeElement == default)
                yield break;

            var walker = new TreeWalker(new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem),
                                                         new PropertyCondition(AutomationElement.ProcessIdProperty, treeElement.Current.ProcessId)));

            var element = walker.GetFirstChild(treeElement);
            yield return element;

            while((element = walker.GetNextSibling(element)) != default)
                yield return element;
        }

        private static async Task<bool> CopyAndActivateScript(AutomationElement scriptEditorWindow, string script)
        {
            var element = await GetControl(scriptEditorWindow, ControlType.CheckBox, "記錄鼠標").ConfigureAwait(false);
            var walker = new TreeWalker(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
            element = await Task.Run(() => walker.GetNextSibling(element)).ConfigureAwait(false);
            if (element == null)
                return false;

            await SetText(element, script).ConfigureAwait(false);

            var button = await GetControl(scriptEditorWindow, ControlType.Button, "加載").ConfigureAwait(false);
            await InvokeElement(button).ConfigureAwait(false);

            HideWindow(scriptEditorWindow);

            return true;
        }

        private static async Task InvokeRunScriptButton(AutomationElement mainWindow, CancellationToken cancelToken)
        {
            var executeScriptButton = await GetScriptSubPanelButton(mainWindow, "啟動").ConfigureAwait(false);
            if (executeScriptButton == default || !executeScriptButton.Current.IsEnabled)
                throw new ApplicationException("啟動腳本按鈕失敗");

            TreeWalker walker = new(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.List));
            var scriptListbox = walker.GetNextSibling(executeScriptButton);

            //make sure script actually ran
            if (scriptListbox != default)
            {
                var pattern = scriptListbox.GetCurrentPattern(SelectionPattern.Pattern) as SelectionPattern;
                bool success = false;

                while (!success)
                {
                    await InvokeElement(executeScriptButton).ConfigureAwait(false);

                    //listbox will get a selection when the script starts running
                    for (int i = 0; i < 50; i++)
                    {
                        await Task.Delay(100, cancelToken).ConfigureAwait(false);

                        if (success = pattern.Current.GetSelection().Length > 0)
                            break;
                    }
                }
            }
            else
            {   //failed to find script list box, so wait 2 seconds instead
                await InvokeElement(executeScriptButton).ConfigureAwait(false);
                await Task.Delay(2000, cancelToken).ConfigureAwait(false);
            }

            if (cancelToken != default)
            {
                while (!executeScriptButton.Current.IsEnabled)
                {
                    await Task.Delay(500, cancelToken).ConfigureAwait(false);
                }
            }
        }

        private static bool HasSameOwnerWindow(int handle1, int handle2)
            => GetWindow(new(handle1), GW_OWNER) == GetWindow(new(handle2), GW_OWNER);

        private static void HideWindow(AutomationElement childWindow)
            => ShowWindow(new IntPtr(childWindow.Current.NativeWindowHandle), 0);

        private static Task SetText(AutomationElement element, string text)
            => Task.Run(() => (element?.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern)?.SetValue(text));

        private static bool IsCheckboxChecked(AutomationElement checkbox)
            => (checkbox.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern).Current.ToggleState == ToggleState.On;
                
        private static async Task SelectComboboxItem(AutomationElement combobox, string label)
        {
            if (String.IsNullOrEmpty(label))
                return;

            //check if the current selection is already what we want
            var valuePattern = combobox.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
            if (valuePattern.Current.Value == label)
                return;

            //expand the dropdown, find the element, and select it
            await Task.Run(() => (combobox.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern).Expand()).ConfigureAwait(false);            
            var listItem = await GetControl(combobox, ControlType.ListItem, label).ConfigureAwait(false);
            await Task.Run(() => (listItem?.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern)?.Select()).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        }

        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process process)
        {
            List<IntPtr> handles = new();

            foreach (ProcessThread thread in process.Threads)
            { 
                EnumThreadWindows(thread.Id, Callback, IntPtr.Zero); 
            }

            return handles;

            bool Callback(IntPtr hWnd, IntPtr lParam)
            {
                StringBuilder sb = new(256);
                GetClassName(hWnd, sb, sb.Capacity);

                if (sb.ToString().Equals("ThunderRT6FormDC", StringComparison.InvariantCultureIgnoreCase))
                {
                    handles.Add(hWnd);
                }
                
                return true;
            }
        }

#endregion
    }
}
