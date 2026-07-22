using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using static SABotSupport.AssaAutomation;
using MySettings = SABotSupport.Properties.Settings;

namespace SABotSupport
{
    public partial class AutoLogonForm : Form
    {
        internal List<int> AttachedInstanceProcessIDs { get; set; }

        private static AutoLogonForm openInstance = default;

        private readonly DataTable accountsDataTable;
        private readonly DataTable assaPathsDataTable;
        private volatile bool isBusy = false;

        private readonly CheckBox scriptModeCheckbox;
        private readonly Button logOutButton;
        private readonly Button cancelScriptButton;
        private volatile bool useScriptMode = false;
        private CancellationTokenSource cancellationTokenSource = new();

        internal delegate void NewAssaAvailableEventHandler(object sender, NewAssaAvailableEventArgs e);
        internal event NewAssaAvailableEventHandler NewAssaAvailableEvent = delegate { };  //avoids null check when firing events

        public AutoLogonForm()
        {
            InitializeComponent();

            openInstance = this;
            openInstance.FormClosed += (s, e) => openInstance = default;

            int colIndex = 3;

            scriptModeCheckbox = new()
            {
                Anchor = AnchorStyles.Right,
                AutoSize = true,
                Name = nameof(scriptModeCheckbox),
                Checked = useScriptMode,
                Text = "腳本模式",
            };
            scriptModeCheckbox.CheckedChanged += (s, e) =>
            {
                useScriptMode = cancelScriptButton.Enabled = scriptModeCheckbox.Checked;
                UpdateStartButtonEnabledState();
            };
            tableLayoutPanel1.Controls.Add(scriptModeCheckbox, colIndex++, 4);

            logOutButton = new()
            {
                Name = nameof(logOutButton),
                Text = "全部登出",
            };
            logOutButton.Click += async (s, e) =>
            {
                var runningInstancesDict = await GetAssaProcesses(this.Handle, searchAllProcesses: MySettings.Default.SearchAllProcesses);                
                var assaPathList = assaPathsDataGridView.Rows.Cast<DataGridViewRow>().Where(x => (bool)x.Cells["PathEnabled"].Value).Select(x => (string)x.Cells["Path"].Value).ToArray();
                HashSet<Task> tasks = new();
                foreach (var element in assaPathList.Where(x => runningInstancesDict.ContainsKey(x)).Select(x => AutomationElement.FromHandle(runningInstancesDict[x])))
                {
                    tasks.Add(LogOff(element));
                }
                await Task.WhenAll(tasks);
            };
            tableLayoutPanel1.Controls.Add(logOutButton, colIndex++, 4);

            cancelScriptButton = new()
            {
                Name = nameof(cancelScriptButton),
                Text = "取消腳本",
                Enabled = false,
            };
            cancelScriptButton.Click += (s, e) => cancellationTokenSource.Cancel();
            tableLayoutPanel1.Controls.Add(cancelScriptButton, colIndex++, 4);

            //context menu strip-------------------------------------------------------------

            ToolStripMenuItem menuItem = new();
            menuItem.Name = "enableAllStatusDoneMenuItem";
            menuItem.Text = "啟用狀態為 Done 的項目";
            menuItem.Click += EnableAllStatusDoneMenuItem;
            contextMenuStrip1.Items.Add(menuItem);

            menuItem = new();
            menuItem.Name = "disableAllStatusDoneMenuItem";
            menuItem.Text = "停用狀態為 Done 的項目";
            menuItem.Click += DisableAllStatusDoneMenuItem;
            contextMenuStrip1.Items.Add(menuItem);

            //load data from settings
            if (String.IsNullOrEmpty(MySettings.Default.AccountPasswordCollection))
            {
                //initialize accounts datatable
                accountsDataTable = new();
                DataColumn col = new("AccountEnabled", typeof(bool))
                {
                    DefaultValue = false,
                };
                accountsDataTable.Columns.Add(col);
                col = new("Account", typeof(string))
                {
                    MaxLength = 12
                };
                accountsDataTable.Columns.Add(col);
                col = new("Password", typeof(string))
                {
                    MaxLength = 12
                };
                accountsDataTable.Columns.Add(col);
                col = new("Server", typeof(int));
                accountsDataTable.Columns.Add(col);
                col = new("Character", typeof(int));
                accountsDataTable.Columns.Add(col);
            }
            else
            {
                accountsDataTable = DecodeDataTableFromString(MySettings.Default.AccountPasswordCollection);
            }

            //add script path column if necessary
            if (!accountsDataTable.Columns.Contains("Script"))
            {
                accountsDataTable.Columns.Add(new DataColumn("Script", typeof(string)));
            }

            if (String.IsNullOrEmpty(MySettings.Default.AssaPathCollection))
            {
                //initialize assaPaths datatable
                assaPathsDataTable = new();
                DataColumn col = new("PathEnabled", typeof(bool))
                {
                    DefaultValue = false
                };
                assaPathsDataTable.Columns.Add(col);
                col = new("Path", typeof(string));
                assaPathsDataTable.Columns.Add(col);
            }
            else
            {
                assaPathsDataTable = DecodeDataTableFromString(MySettings.Default.AssaPathCollection);

                //reset enabled state to false
                foreach (var row in assaPathsDataTable.AsEnumerable())
                {
                    row["PathEnabled"] = false;
                }
            }

            //bind to datagridviews
            accountsDataGridView.AutoGenerateColumns = false;
            accountsDataGridView.DataSource = accountsDataTable;
            assaPathsDataGridView.AutoGenerateColumns = false;
            assaPathsDataGridView.DataSource = assaPathsDataTable;

            //adjust accounts datacolumns
            accountsDataGridView.Columns["AccountEnabled"].DataPropertyName = "AccountEnabled";
            accountsDataGridView.Columns["Account"].DataPropertyName = "Account";
            accountsDataGridView.Columns["Password"].DataPropertyName = "Password";
            accountsDataGridView.Columns["Server"].DataPropertyName = "Server";
            accountsDataGridView.Columns["Character"].DataPropertyName = "Character";
            (accountsDataGridView.Columns["Character"] as DataGridViewComboBoxColumn).DataSource = new List<Tuple<string, int>>() { 
                new Tuple<string, int>("一", 0),
                new Tuple<string, int>("二", 1),
            };
            (accountsDataGridView.Columns["Character"] as DataGridViewComboBoxColumn).DisplayMember = "Item1";
            (accountsDataGridView.Columns["Character"] as DataGridViewComboBoxColumn).ValueMember = "Item2";
            accountsDataGridView.Columns.Add("Script", "腳本");
            accountsDataGridView.Columns["Script"].DataPropertyName = "Script";
            accountsDataGridView.Columns["Script"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            accountsDataGridView.Columns["Script"].ReadOnly = true;
            accountsDataGridView.Columns.Add("Status", "狀態");
            accountsDataGridView.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            accountsDataGridView.Columns["Status"].ReadOnly = true;

            //generate new binding list for server selection
            int maxValue = accountsDataTable.AsEnumerable().Max(x => x.Field<int>("Server") as int?) ?? 1;  //if no records, assume 2 servers are possible
            var tupleList = Enumerable.Range(0, maxValue + 1).Select(x => new Tuple<string, int>($"分流{x}", x)).ToList();
            (accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).DataSource =
                new BindingList<Tuple<string, int>>(tupleList) { RaiseListChangedEvents = false };  //list will be updated at once, so better to disable auto events and raise manually
            (accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).DisplayMember = "Item1";
            (accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).ValueMember = "Item2";

            //adjust assaPaths datacolumns
            assaPathsDataGridView.Columns["PathEnabled"].DataPropertyName = "PathEnabled";
            assaPathsDataGridView.Columns["Path"].DataPropertyName = "Path";
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

        private void AutoLogonForm_Load(object sender, EventArgs e)
        {
            LoadServerlistINI();
            LoadAttachedInstancesIntoAssaPaths();
            UpdateStartButtonEnabledState();
        }

        private void AutoLogonForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isBusy &&
                MessageBox.Show("Operation in progress, close anyway?", "Force close", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void AccountsDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == accountsDataGridView.Columns["Script"].Index)
            {
                e.Value = System.IO.Path.GetFileName(e.Value?.ToString());
            }

            if (e.ColumnIndex != accountsDataGridView.Columns["Password"].Index || e.Value == default)
                return;

            accountsDataGridView.Rows[e.RowIndex].Tag = e.Value;
            e.Value = new String('\u25CF', e.Value.ToString().Length);
        }

        private void AccountsDataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (accountsDataGridView.CurrentCell.ColumnIndex == accountsDataGridView.Columns["Password"].Index &&
                accountsDataGridView.CurrentRow.Tag != default)
            {
                e.Control.Text = accountsDataGridView.CurrentRow.Tag.ToString();
            }
        }

        private void AccountsDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception is ArgumentException && e.Exception.Message.Contains("MaxLength limit"))
            {
                MessageBox.Show("長度不可超過13個字元", $"{e.Context} Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                accountsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = String.Empty;
                accountsDataGridView.BeginEdit(false);

                e.Cancel = true;
            }
            else
            {
                e.ThrowException = true;
            }
        }

        private void AccountsDataGridView_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells["AccountEnabled"].Value = true;
            e.Row.Cells["Server"].Value = 0;
            e.Row.Cells["Character"].Value = 0;
        }

        private void AccountsDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
            => UpdateStartButtonEnabledState();
        
        private void AccountsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= accountsDataTable.Rows.Count)
                return;

            if (e.ColumnIndex == accountsDataGridView.Columns["AccountEnabled"].Index)
            {
                accountsDataTable.Rows[e.RowIndex][e.ColumnIndex] = !(bool)accountsDataTable.Rows[e.RowIndex][e.ColumnIndex];

                UpdateStartButtonEnabledState();
            }
            else if (e.ColumnIndex == accountsDataGridView.Columns["Status"].Index)
            {
                var errorMessage = accountsDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ErrorText;

                if (!String.IsNullOrEmpty(errorMessage))
                {
                    Clipboard.SetText(errorMessage);
                }
            }
        }

        private void AccountsDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != accountsDataGridView.Columns["Script"].Index ||
                e.RowIndex < 0 ||
                e.RowIndex >= accountsDataTable.Rows.Count)
                return;

            using OpenFileDialog dialog = new() {
                CheckFileExists = true,
                Multiselect = false,
                Filter = "asc files (*.asc)|*.asc",
                Title = "選擇腳本",
            };

            if (dialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty(dialog.FileName))
            {
                accountsDataTable.Rows[e.RowIndex][e.ColumnIndex] = dialog.FileName;
                UpdateStartButtonEnabledState();
            }
        }

        private void EnableAllMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var row in accountsDataTable.AsEnumerable())
            {
                row["AccountEnabled"] = true;
            }

            UpdateStartButtonEnabledState();
        }

        private void DisableAllMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var row in accountsDataTable.AsEnumerable())
            {
                row["AccountEnabled"] = false;
            }

            UpdateStartButtonEnabledState();
        }

        private void EnableSelectedMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var row in accountsDataGridView.SelectedCells.Cast<DataGridViewCell>().Select(x => x.OwningRow).Distinct())
            {
                if (row.IsNewRow)
                    continue;

                DataRow source = ((DataRowView)row.DataBoundItem).Row;
                source["AccountEnabled"] = true;
            }

            UpdateStartButtonEnabledState();
        }

        private void DisableSelectedMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var row in accountsDataGridView.SelectedCells.Cast<DataGridViewCell>().Select(x => x.OwningRow).Distinct())
            {
                if (row.IsNewRow)
                    continue;

                DataRow source = ((DataRowView)row.DataBoundItem).Row;
                source["AccountEnabled"] = false;
            }

            UpdateStartButtonEnabledState();
        }

        private void AssaPathsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != assaPathsDataGridView.Columns["PathEnabled"].Index || 
                e.RowIndex < 0 ||
                e.RowIndex >= assaPathsDataTable.Rows.Count ||
                isBusy)
                return;

            assaPathsDataTable.Rows[e.RowIndex][e.ColumnIndex] = !(bool)assaPathsDataTable.Rows[e.RowIndex][e.ColumnIndex];

            UpdateStartButtonEnabledState();
        }

        private void AssaPathsDataGridView_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
            => UpdateStartButtonEnabledState();

        private void AddPathButton_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new() {
                CheckFileExists = true,
                Multiselect = false,
                FileName = "Assa8.0B5.exe",
                Filter = "exe files (*.exe)|*.exe",
                Title = "請選擇 Assa8.0B5.exe 的路徑",
            };

            if (dialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty(dialog.FileName))
            {
                var assaPaths = assaPathsDataTable.AsEnumerable().Select(x => (string)x["Path"]);

                if (assaPaths.Contains(dialog.FileName))
                {
                    MessageBox.Show("該 ASSA 外掛已經在路徑表中", "路徑選擇錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    var row = assaPathsDataTable.NewRow();

                    row["Path"] = dialog.FileName;

                    assaPathsDataTable.Rows.Add(row);
                }
            }
        }

        private static readonly Regex serverListItemRegex = new(@"(?<=\d=).+?(?=,)");

        private void ConfigureServerINIButton_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new()
            {
                CheckFileExists = true,
                Multiselect = false,
                FileName = "serverlist.ini",
                Filter = "ini files (*.ini)|*.ini",
                Title = "請指定 serverlist.ini 的路徑",
            };

            if (!String.IsNullOrEmpty(MySettings.Default.ServerlistINIPath))
            {
                dialog.FileName = MySettings.Default.ServerlistINIPath;
            }

            if (dialog.ShowDialog() == DialogResult.OK && !String.IsNullOrEmpty(dialog.FileName))
            {
                MySettings.Default.ServerlistINIPath = dialog.FileName;
                LoadServerlistINI();
            }
        }

        private void LoadServerlistINI()
        {
            if (String.IsNullOrEmpty(MySettings.Default.ServerlistINIPath) ||
                !File.Exists(MySettings.Default.ServerlistINIPath))
                return;

            var buffer = File.ReadAllText(MySettings.Default.ServerlistINIPath, Encoding.GetEncoding("big5"));
            var matches = serverListItemRegex.Matches(buffer);

            //update the datacolumn for server
            //note: we iterate and update because it is possible the new list is smaller than the old one
            //      then, we add any leftover entries parsed from the ini file
            var serverList = (accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).DataSource as BindingList<Tuple<string, int>>;
            for (int i = 0; i < serverList.Count; i++)
            {
                serverList[i] = new Tuple<string, int>(matches[i].Value, i);
            }
            for (int i = serverList.Count; i < matches.Count; i++)
            {
                serverList.Add(new Tuple<string, int>(matches[i].Value, i));
            }
            serverList.ResetBindings();
        }

        private async void StartButton_Click(object sender, EventArgs e)
        {
            if (useScriptMode)
            {
                isBusy = cancelScriptButton.Enabled = true;
                startButton.Enabled = logOutButton.Enabled = scriptModeCheckbox.Enabled = false;

                await DoScriptMode();

                isBusy = cancelScriptButton.Enabled = false;
                startButton.Enabled = logOutButton.Enabled = scriptModeCheckbox.Enabled = true;

                return;
            }

            isBusy = true;
            tableLayoutPanel1.Enabled = false;

            var runningInstancesDict = await GetAssaProcesses(this.Handle, searchAllProcesses: MySettings.Default.SearchAllProcesses);
            var accountAssaPairs = Enumerable.Zip(accountsDataTable.AsEnumerable().Where(x => x.Field<bool>("AccountEnabled")),
                                                  assaPathsDataTable.AsEnumerable().Where(x => x.Field<bool>("PathEnabled")),
                                                  (x, y) => new Tuple<string, string, int, int, string>(x.Field<string>("Account"),
                                                                                                        x.Field<string>("Password"),
                                                                                                        x.Field<int>("Server"),
                                                                                                        x.Field<int>("Character"),
                                                                                                        y.Field<string>("Path")));
            var serverNames = ((accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).DataSource as BindingList<Tuple<string, int>>).Select(x => x.Item1).ToArray();

            HashSet<Task> tasks = new();

            foreach (var pair in accountAssaPairs)
            {
                var account = pair.Item1.Trim();
                var password = pair.Item2.Trim();
                var serverIndex = pair.Item3;
                var charIndex = pair.Item4;
                var assaPath = pair.Item5;

                tasks.Add(Task.Run(() => DoAccountLoginWithAssa(runningInstancesDict, serverNames, account, password, serverIndex, charIndex, assaPath)));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex) when (ex is ElementNotAvailableException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "自動登陸 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isBusy = false;
                tableLayoutPanel1.Enabled = true;
            }
        }

        private async Task<AutomationElement> DoAccountLoginWithAssa(IDictionary<string, IntPtr> runningInstancesDict, string[] serverNames, string account, string password, int serverIndex, int charIndex, string assaPath)
        {
            //check if we need to create process ourselves
            AutomationElement mainWindowElement;
            try
            {
                mainWindowElement = runningInstancesDict.ContainsKey(assaPath) ?
                        AutomationElement.FromHandle(runningInstancesDict[assaPath]) :
                        await LaunchASSA(assaPath).ConfigureAwait(false);
            }
            catch (ElementNotAvailableException)
            {
                //the assa instance from the dictionary has died
                mainWindowElement = await LaunchASSA(assaPath).ConfigureAwait(false);
            }
            runningInstancesDict[assaPath] = new(mainWindowElement.Current.NativeWindowHandle);

            //get data until the game handle is non-zero
            DataPackage data;
            int retryCount = 0;
            while ((data = await Task.Run(() => MemoryReader.GetAssaData(mainWindowElement.Current.ProcessId, skipGameClientData: false)).ConfigureAwait(false)).GameClientHandle == IntPtr.Zero)
            {
                await Task.Delay(200).ConfigureAwait(false);

                if (++retryCount >= 10)
                    throw new ApplicationException("Failed to get valid game client handle within retry limit");
            }

            //notify mainform that we have a new ASSA instance it may want to keep track of
            this.Invoke(new Action(() => NewAssaAvailableEvent(this, new(mainWindowElement, data))));

            //hide game if requested
            if (MySettings.Default.AutoHideOnLogin && data.IsVisible)
            {
                await InvokeElement(await GetControl(mainWindowElement, ControlType.CheckBox, "隱藏石器").ConfigureAwait(false)).ConfigureAwait(false);
            }

            //check if we need to log out first
            if (data.IsOnline)
            {
                await LogOff(mainWindowElement).ConfigureAwait(false);
                await Task.Delay(2000).ConfigureAwait(false);
            }

            //copy over credentials
            retryCount = 0;
            while (!await SAMemoryWriter.SetAccountPassword(data.GameClientHandle, account, password).ConfigureAwait(false))
            {
                await Task.Delay(200).ConfigureAwait(false);

                if (++retryCount >= 10)
                    throw new ApplicationException("Failed to set account/password within retry limit");
            }

            //log on
            await LogOn(mainWindowElement,
                server: serverNames[serverIndex],
                character: new string[] { "第一人物", "第二人物" }[charIndex]).ConfigureAwait(false);

            return mainWindowElement;
        }

        private void EnableAllStatusDoneMenuItem(object sender, EventArgs e)
        {
            foreach (var row in accountsDataGridView.Rows.Cast<DataGridViewRow>().Where(x => (string)x.Cells["Status"].Value == "Done"))
            {
                if (row.IsNewRow)
                    continue;

                DataRow source = ((DataRowView)row.DataBoundItem).Row;
                source["AccountEnabled"] = true;
            }

            UpdateStartButtonEnabledState();
        }

        private void DisableAllStatusDoneMenuItem(object sender, EventArgs e)
        {
            foreach (var row in accountsDataGridView.Rows.Cast<DataGridViewRow>().Where(x => (string)x.Cells["Status"].Value == "Done"))
            {
                if (row.IsNewRow)
                    continue;

                DataRow source = ((DataRowView)row.DataBoundItem).Row;
                source["AccountEnabled"] = false;
            }

            UpdateStartButtonEnabledState();
        }

        private static readonly Regex unsupportedRegex = new(@"^\s*run\s", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private static readonly Regex loginTimeRegex = new(@"^'#LOGINTIME\s+(?<Delay>\d+)");
        private static readonly Regex failMsgRegex = new(@"^'#FAILMSG\s+(?<Message>.+)");

        private async Task DoScriptMode()
        {
            //reset status column
            foreach (DataGridViewRow row in accountsDataGridView.Rows)
            {
                row.Cells["Status"].Value = String.Empty;
                row.Cells["Status"].ErrorText = String.Empty;
            }

            var runningInstancesDict = await GetAssaProcesses(this.Handle, searchAllProcesses: MySettings.Default.SearchAllProcesses);
            var serverNames = ((accountsDataGridView.Columns["Server"] as DataGridViewComboBoxColumn).DataSource as BindingList<Tuple<string, int>>).Select(x => x.Item1).ToArray();
            ConcurrentDictionary<string, string> scriptDict = new();  // filename x file contents
            string sharedScriptPath = String.Empty;
            bool skipMoveNext = false;  //needed because we check MoveNext at the end of the main while loop

            //log out of all assa instances
            var assaPathList = assaPathsDataGridView.Rows.Cast<DataGridViewRow>().Where(x => (bool)x.Cells["PathEnabled"].Value).Select(x => (string)x.Cells["Path"].Value).ToArray();
            if (!await TryLogOffAll(assaPathList.Where(x => runningInstancesDict.ContainsKey(x)).Select(x => AutomationElement.FromHandle(runningInstancesDict[x]))))
                return;

            cancellationTokenSource.Dispose();
            cancellationTokenSource = new();
            var cancelToken = cancellationTokenSource.Token;

            using var accountIterator = accountsDataGridView.Rows.Cast<DataGridViewRow>().Where(x => !x.IsNewRow && (bool)x.Cells["AccountEnabled"].Value).GetEnumerator();
                        
            while (!cancelToken.IsCancellationRequested)
            {
                HashSet<Task<bool>> tasks = new();
                HashSet<AssaInstance> idleInstances = new();

                foreach (string assaPath in assaPathList)
                {
                    if (!skipMoveNext && !accountIterator.MoveNext() || cancelToken.IsCancellationRequested)
                        break;

                    skipMoveNext = false;

                    var account = accountIterator.Current.Cells["Account"].Value as string;
                    var password = accountIterator.Current.Cells["Password"].Value as string;
                    var serverIndex = (int)accountIterator.Current.Cells["Server"].Value;
                    var charIndex = (int)accountIterator.Current.Cells["Character"].Value;
                    var scriptPath = accountIterator.Current.Cells["Script"].Value as string;
                    var statusCell = accountIterator.Current.Cells["Status"];

                    sharedScriptPath = String.IsNullOrEmpty(scriptPath) ? sharedScriptPath : scriptPath;

                    //load the script
                    string script = String.Empty;
                    if (!TryLoadScript(ref script, scriptDict, sharedScriptPath, statusCell))
                        continue;

                    var playerinfo = PlayerJournal.GetPlayerInfo(account, charIndex + 1);
                    playerinfo.ResetLastLoginTime();

                    //login
                    statusCell.Value = "Login";
                    AutomationElement mainWindowElement;
                    try
                    {
                        mainWindowElement = await DoAccountLoginWithAssa(runningInstancesDict, serverNames, account, password, serverIndex, charIndex, assaPath);
                    }
                    catch (Exception ex) when (ex is ElementNotAvailableException || ex is UnauthorizedAccessException || ex is ApplicationException)
                    {
                        MessageBox.Show(ex.Message, "自動腳本 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    //try to find the script in tree for direct script execution
                    await ActivateSubPanel(mainWindowElement, "腳本");
                    var treeElement = await GetControl(mainWindowElement, ControlType.Tree, TreeScope.Descendants);
                    var (scriptElement, isLastItem) = await Task.Run(() => FindScriptInTree(treeElement, sharedScriptPath));

                    if (scriptElement == default && (String.IsNullOrEmpty(script) || unsupportedRegex.IsMatch(script)))
                    {
                        statusCell.Value = "Fail";
                        statusCell.ErrorText = "Script is incompatible with copy execute and direct execute failed.";
                        continue;
                    }

                    //build the task to run
                    statusCell.Value = "Running";
                    idleInstances.Add(new(mainWindowElement));
                    tasks.Add(BuildRunScriptTask(scriptElement, isLastItem, script, playerinfo, statusCell, mainWindowElement, cancelToken));
                }

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (ex is ElementNotAvailableException || ex is UnauthorizedAccessException || ex is ApplicationException)
                {
                    MessageBox.Show(ex.Message, "自動腳本 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }

                //check if we should end processing
                if (cancelToken.IsCancellationRequested || tasks.All(x => x.Result == false))
                    break;

                //check if we are done
                //note: this allows the last set of accounts to remain logged in
                if (!accountIterator.MoveNext())
                    break;

                skipMoveNext = true;

                //log off
                if (!await TryLogOffAll(idleInstances.Where(x => x.IsAlive()).Select(x => x.MainForm)))
                    break;
            }

            return;

            static async Task<bool> TryLogOffAll(IEnumerable<AutomationElement> assaWindowElements)
            {
                try
                {
                    if (assaWindowElements.Any())
                    {
                        assaWindowElements.First().SetFocus();  //switch active virtual desktop if needed
                        await Task.Delay(300).ConfigureAwait(false);
                        await Task.WhenAll(assaWindowElements.Select(x => LogOff(x)));
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("TryLogOffAll encountered an exception:");
                    Trace.WriteLine(ex);
                    MessageBox.Show(ex.Message, "自動登出 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            static bool TryLoadScript(ref string script, IDictionary<string, string> scriptDict, string scriptPath, DataGridViewCell statusCell)
            {
                if (String.IsNullOrEmpty(scriptPath))
                {
                    statusCell.Value = "Fail";
                    statusCell.ErrorText = "No script specified";

                    return false;
                }
                else if (scriptDict.ContainsKey(scriptPath))
                {
                    script = scriptDict[scriptPath];
                }
                else
                {
                    script = scriptDict[scriptPath] = File.ReadAllText(scriptPath, Encoding.GetEncoding("big5"));
                }

                return true;
            }

            static Task<bool> BuildRunScriptTask(AutomationElement scriptElement, bool isLastItem, string script, PlayerInfo playerInfo, DataGridViewCell statusCell, AutomationElement mainWindowElement, CancellationToken cancelToken)
            {
                return Task.Run(async () =>
                {
                    //initialize assa instance
                    AssaInstance assaInstance = new(mainWindowElement);
                    await assaInstance.InitializeSecondaryControls().ConfigureAwait(false);

                    //wait until log in has completed
                    while (playerInfo.LastLoginTime == DateTime.MinValue)
                    {
                        await Task.Delay(100, cancelToken).ConfigureAwait(false);
                        _ = mainWindowElement.Current.Name; //throws if assa died
                    }

                    //check for any command directives
                    var failMessages = await ProcessCommandDirectives().ConfigureAwait(false);

                    //use direct execution if possible
                    if (scriptElement != default)
                    {
                        await ExecuteScript(assaInstance, scriptElement, isLastItem, cancelToken).ConfigureAwait(false);
                    }
                    else
                    {   //failed to find script in tree element, using copy script method
                        await ExecuteScript(assaInstance, script, cancelToken).ConfigureAwait(false);
                    }

                    //check any fail messages
                    if (failMessages.Any())
                    {
                        var data = MemoryReader.GetAssaData(assaInstance.ProcessID, skipGameClientData: false);
                        var detectedMessages = data.GameClientData.ChatBuffer.Select(x => x.Text.Trim()).Where(failMessages.Contains);

                        if (detectedMessages.Any())
                            throw new ApplicationException($"Detected error messages in chat stream:{Environment.NewLine}" +
                                                           $"{String.Join(Environment.NewLine, detectedMessages)}");
                    }
                }, cancelToken).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        statusCell.Value = "Error";
                        statusCell.ErrorText = t.Exception.ToStringDemystified();

                        return false;   //return false to indicate main task did not complete successfully
                    }
                    else if (t.IsCanceled)
                    {
                        statusCell.Value = "Cancel";

                        return false;   //return false to indicate main task did not complete successfully
                    }
                    else
                    {
                        statusCell.Value = "Done";

                        return true;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                async Task<HashSet<string>> ProcessCommandDirectives()
                {
                    using StringReader sr = new(script);
                    string line = String.Empty;
                    HashSet<string> failMessages = new();

                    while ((line = sr.ReadLine()) != default)
                    {
                        Match match;
                        if ((match = loginTimeRegex.Match(line)).Success)
                        {   //poll until login time reaches value
                            var duration = TimeSpan.FromSeconds(Int32.Parse(match.Groups["Delay"].Value));

                            //switch to the script tab early
                            await ActivateSubPanel(mainWindowElement, "腳本").ConfigureAwait(false);

                            while (playerInfo.LastLoginTime == DateTime.MinValue ||
                                   DateTime.Now - playerInfo.LastLoginTime < duration)
                            {
                                await Task.Delay(2000, cancelToken).ConfigureAwait(false);
                                _ = mainWindowElement.Current.Name; //throws if assa died
                            }
                        }
                        else if ((match = failMsgRegex.Match(line)).Success)
                        {   //set messages to check for that indicates a failure of the script
                            failMessages.Add(match.Groups["Message"].Value);
                        }
                        else
                            break;
                    }

                    return failMessages;
                }
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            MySettings.Default.AccountPasswordCollection = MySettings.Default.SaveAccountPasswords ? 
                EncodeDataTableAsString(accountsDataTable) : String.Empty;
            MySettings.Default.AssaPathCollection = EncodeDataTableAsString(assaPathsDataTable);
            MySettings.Default.Save();

            this.Close();
        }

        private void LoadAttachedInstancesIntoAssaPaths()
        {
            var assaPathsDict = assaPathsDataTable.AsEnumerable().ToDictionary(x => (string)x["Path"], x => x);

            foreach (int id in AttachedInstanceProcessIDs)
            {
                using Process process = Process.GetProcessById(id);
                string path = process.MainModule.FileName;

                if (assaPathsDict.Keys.Contains(path))
                {   //enable existing entry
                    var row = assaPathsDict[path];

                    row["PathEnabled"] = true;
                }
                else
                {   //add a new entry
                    var row = assaPathsDataTable.NewRow();

                    row["PathEnabled"] = true;
                    row["Path"] = path;

                    assaPathsDataTable.Rows.Add(row);
                }
            }
        }

        private void UpdateStartButtonEnabledState()
        {
            if (accountsDataTable == default || assaPathsDataTable == default)
                return;

            int numAccounts = 0;
            warningLabel.Visible = false;
            startButton.Enabled = false;
            bool firstScriptSet = false;

            foreach (DataRow row in accountsDataTable.Rows)
            {
                if ((bool)row["AccountEnabled"] == false)
                    continue;

                string account = row.Field<string>("Account");
                string password = row.Field<string>("Password");
                string scriptPath = row.Field<string>("Script");
                firstScriptSet = firstScriptSet || !String.IsNullOrEmpty(scriptPath);

                if (String.IsNullOrWhiteSpace(account) || String.IsNullOrWhiteSpace(password))
                {
                    warningLabel.Text = "帳號/密碼不能為空白";
                    warningLabel.Visible = true;
                    break;
                }
                else if (ContainsNonAsciiChars(account) || ContainsNonAsciiChars(password))
                {
                    warningLabel.Text = "帳號/密碼只能有英文或數字";
                    warningLabel.Visible = true;
                    break;
                }
                else if (useScriptMode && !firstScriptSet && String.IsNullOrEmpty(scriptPath))
                {
                    warningLabel.Text = "第一個啟用的帳號必須有設定腳本";
                    warningLabel.Visible = true;
                    break;
                }
                else
                {
                    numAccounts++;
                }
            }

            if (warningLabel.Visible)
                return;

            int numAssa = 0;

            foreach (DataRow row in assaPathsDataTable.Rows)
            {
                if ((bool)row["PathEnabled"])
                {
                    numAssa++;
                }
            }

            if (useScriptMode && numAssa == 0 || !useScriptMode && numAccounts > numAssa)
            {
                warningLabel.Text = "選取的 ASSA 外掛不足";
                warningLabel.Visible = true;
            }
            else if (numAccounts > 0 && !isBusy)
            {
                startButton.Enabled = true;
            }

            accountsLabel.Text = $"要登入的帳號: (已選擇 {numAccounts})";
            assaPathsLabel.Text = $"要使用的 ASSA 外掛: (已選擇 {numAssa})";
        }

        private static bool ContainsNonAsciiChars(string input)
            => Regex.IsMatch(input, @"[^\u0020-\u007E]+");

        private static string EncodeDataTableAsString(DataTable table)
        {
            using MemoryStream ms = new();
            BinaryFormatter bf = new();
            bf.Serialize(ms, table);
            ms.Position = 0;
            byte[] buffer = new byte[(int)ms.Length];
            ms.Read(buffer, 0, buffer.Length);
            var encoded = StringCipher.Encrypt(Convert.ToBase64String(buffer), "WeLoveSA2021");

            return encoded;
        }

        private static DataTable DecodeDataTableFromString(string encoded)
        {
            var decoded = StringCipher.Decrypt(encoded, "WeLoveSA2021");
            using MemoryStream ms = new(Convert.FromBase64String(decoded));
            BinaryFormatter bf = new();

            return (DataTable)bf.Deserialize(ms);
        }

        private static class StringCipher
        {
            // This constant is used to determine the keysize of the encryption algorithm in bits.
            // We divide this by 8 within the code below to get the equivalent number of bytes.
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string Encrypt(string plainText, string passPhrase)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
                var keyBytes = password.GetBytes(Keysize / 8);
                using var symmetricKey = new RijndaelManaged();
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();
                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                var cipherTextBytes = saltStringBytes;
                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                memoryStream.Close();
                cryptoStream.Close();
                return Convert.ToBase64String(cipherTextBytes);
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations);
                var keyBytes = password.GetBytes(Keysize / 8);
                using var symmetricKey = new RijndaelManaged();
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                using var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
                using var memoryStream = new MemoryStream(cipherTextBytes);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                var plainTextBytes = new byte[cipherTextBytes.Length];
                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                memoryStream.Close();
                cryptoStream.Close();
                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using RNGCryptoServiceProvider rngCsp = new();

                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);

                return randomBytes;
            }
        }

    }
    internal class NewAssaAvailableEventArgs : EventArgs
    {
        public AssaInstance Instance { get; }
        public DataPackage Data { get; }

        public NewAssaAvailableEventArgs(AutomationElement mainWindowElement, DataPackage data)
        {
            Instance = new(mainWindowElement);
            Data = data;
        }
    }
}
