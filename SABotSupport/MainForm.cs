using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using SABotSupport.ASSAStructures;
using MySettings = SABotSupport.Properties.Settings;
using AssaInstance = SABotSupport.AssaAutomation.AssaInstance;
using System.Collections.Concurrent;
using static SABotSupport.ChatLine;

namespace SABotSupport
{
    public partial class MainForm : Form
    {
        private readonly struct PetDisplay(GroupBox petGroup, Label nickname, TextBox status,
                          Label level, Label exp,
                          Label next, Label hp,
                          Label hpRate, Label attack, Label attackRate,
                          Label totalRate,
                          Label loyalty, Label attribute1,
                          Label attribute2, Label id)
        {
            public readonly GroupBox PetGroup = petGroup;
            public readonly Label Nickname = nickname;
            public readonly TextBox Status = status;
            public readonly Label Level = level;
            public readonly Label EXP = exp;
            public readonly Label Next = next;
            public readonly Label HP = hp;
            public readonly Label HPRate = hpRate;
            public readonly Label Attack = attack;
            public readonly Label AttackRate = attackRate;
            public readonly Label TotalRate = totalRate;
            public readonly Label Loyalty = loyalty;
            public readonly Label Attribute1 = attribute1;
            public readonly Label Attribute2 = attribute2;
            public readonly Label ID = id;
        }

        private const string TOOLTIP_DATABASE_PATH = "tooltipData.csv";
        private const string PET_IMAGE_FOLDER = "PetScreenshots";

        private AssaInstance displayedInstance = null;
        private volatile bool shouldMainMenuStayOpen = false;

        //UI objects
        private readonly List<PetDisplay> petDisplays = [];
        private readonly HashSet<Label> petNicknameDisplays;
        private readonly HashSet<Label> petStatsDisplays;
        private readonly HashSet<Label> petRateDisplays;
        private readonly List<Label> allyDisplays = [];
        private readonly List<Label> allyPetDisplays = [];
        private readonly List<Label> enemyDisplays = [];
        private readonly List<string> itemGridViewColumns = [];

        public MainForm()
        {
            InitializeComponent();

            //right-aligned autosize label implementation
            var x = warningLabel.Parent.Width - warningLabel.Right;
            warningLabel.SizeChanged += (s, e) => warningLabel.Location = new(warningLabel.Parent.Width - x - warningLabel.Width, warningLabel.Top);

            //initialize datasource
            attachedInstancesListbox.DataSource = GameStatePoller.AttachedAssaInstances;
            textColorCombobox.DataSource = ChatLine.ChatColors;

            //initialize form control containers
            petDisplays.Add(new PetDisplay(
                pet1Groupbox,
                pet1NicknameDisplay,
                pet1StatusTextbox,
                pet1LevelDisplay,
                pet1EXPDisplay,
                pet1NextDisplay,
                pet1HPDisplay,
                pet1HPRateDisplay,
                pet1AttackDisplay,
                pet1AttackRateDisplay,
                pet1TotalRateDisplay,
                pet1LoyaltyDisplay,
                pet1Attribute1Display,
                pet1Attribute2Display,
                pet1IDDisplay));
            petDisplays.Add(new PetDisplay(
                pet2Groupbox,
                pet2NicknameDisplay,
                pet2StatusTextbox,
                pet2LevelDisplay,
                pet2EXPDisplay,
                pet2NextDisplay,
                pet2HPDisplay,
                pet2HPRateDisplay,
                pet2AttackDisplay,
                pet2AttackRateDisplay,
                pet2TotalRateDisplay,
                pet2LoyaltyDisplay,
                pet2Attribute1Display,
                pet2Attribute2Display,
                pet2IDDisplay));
            petDisplays.Add(new PetDisplay(
                pet3Groupbox,
                pet3NicknameDisplay,
                pet3StatusTextbox,
                pet3LevelDisplay,
                pet3EXPDisplay,
                pet3NextDisplay,
                pet3HPDisplay,
                pet3HPRateDisplay,
                pet3AttackDisplay,
                pet3AttackRateDisplay,
                pet3TotalRateDisplay,
                pet3LoyaltyDisplay,
                pet3Attribute1Display,
                pet3Attribute2Display,
                pet3IDDisplay));
            petDisplays.Add(new PetDisplay(
                pet4Groupbox,
                pet4NicknameDisplay,
                pet4StatusTextbox,
                pet4LevelDisplay,
                pet4EXPDisplay,
                pet4NextDisplay,
                pet4HPDisplay,
                pet4HPRateDisplay,
                pet4AttackDisplay,
                pet4AttackRateDisplay,
                pet4TotalRateDisplay,
                pet4LoyaltyDisplay,
                pet4Attribute1Display,
                pet4Attribute2Display,
                pet4IDDisplay));
            petDisplays.Add(new PetDisplay(
                pet5Groupbox,
                pet5NicknameDisplay,
                pet5StatusTextbox,
                pet5LevelDisplay,
                pet5EXPDisplay,
                pet5NextDisplay,
                pet5HPDisplay,
                pet5HPRateDisplay,
                pet5AttackDisplay,
                pet5AttackRateDisplay,
                pet5TotalRateDisplay,
                pet5LoyaltyDisplay,
                pet5Attribute1Display,
                pet5Attribute2Display,
                pet5IDDisplay));
            petNicknameDisplays = new HashSet<Label>() {
                pet1NicknameDisplay,
                pet2NicknameDisplay,
                pet3NicknameDisplay,
                pet4NicknameDisplay,
                pet5NicknameDisplay,
            };
            petStatsDisplays = new HashSet<Label> {
                pet1HPDisplay, pet1AttackDisplay,
                pet2HPDisplay, pet2AttackDisplay,
                pet3HPDisplay, pet3AttackDisplay,
                pet4HPDisplay, pet4AttackDisplay,
                pet5HPDisplay, pet5AttackDisplay,
            };
            petRateDisplays = new HashSet<Label> {
                pet1HPRateDisplay, pet1AttackRateDisplay, pet1TotalRateDisplay,
                pet2HPRateDisplay, pet2AttackRateDisplay, pet2TotalRateDisplay,
                pet3HPRateDisplay, pet3AttackRateDisplay, pet3TotalRateDisplay,
                pet4HPRateDisplay, pet4AttackRateDisplay, pet4TotalRateDisplay,
                pet5HPRateDisplay, pet5AttackRateDisplay, pet5TotalRateDisplay,
            };

            //initialize item grid view
            for (int i = 0; i < 5; i++)
            {
                itemGridView.Rows.Add();
            }
            itemGridViewColumns.AddRange(itemGridView.Columns.Cast<DataGridViewColumn>().Select(x => x.Name));

            //initialize battle displays
            allyDisplays.Add(allyDisplay1);
            allyDisplays.Add(allyDisplay2);
            allyDisplays.Add(allyDisplay3);
            allyDisplays.Add(allyDisplay4);
            allyDisplays.Add(allyDisplay5);
            allyPetDisplays.Add(allyPetDisplay1);
            allyPetDisplays.Add(allyPetDisplay2);
            allyPetDisplays.Add(allyPetDisplay3);
            allyPetDisplays.Add(allyPetDisplay4);
            allyPetDisplays.Add(allyPetDisplay5);
            enemyDisplays.Add(enemyDisplay1);
            enemyDisplays.Add(enemyDisplay2);
            enemyDisplays.Add(enemyDisplay3);
            enemyDisplays.Add(enemyDisplay4);
            enemyDisplays.Add(enemyDisplay5);
            enemyDisplays.Add(enemyDisplay6);
            enemyDisplays.Add(enemyDisplay7);
            enemyDisplays.Add(enemyDisplay8);
            enemyDisplays.Add(enemyDisplay9);
            enemyDisplays.Add(enemyDisplay10);

            //subscribe to events
            ChatLog.NewChatAvailableEvent += UpdateChatWindow;
            ChatLog.NewChatAvailableEvent += PlayerJournal.OnNewChatAvailable;
            GameStatePoller.NewDataAvailableEvent += (s, e) => Invoke(new Action(() => OnNewDataAvailable(s, e)));
            GameStatePoller.NewStatusMessageAvailableEvent += (s, e) => Invoke(new Action(() => OnNewStatusMessageAvailable(s, e)));
            GameStatePoller.AttachedInstanceListChangedEvent += (s, e) => Invoke(new Action(() => OnAttachedInstanceListChanged(s, e)));
            UpdateChecker.NewVersionAvailableEvent += (s, e) => Invoke(new Action(() => newVersionAvailableLabel.Visible = true));

            //clear debug texts and controls
#if !DEBUG
            testButton.Visible = false;

            pointsDisplay.Text = String.Empty;

            teammateDisplay.Text = String.Empty;
            playerInfoTableLayoutPanel.Visible = false;
            loginDurationDisplay.Text = "上線時間:";

            pet1Groupbox.Visible = pet2Groupbox.Visible = pet3Groupbox.Visible = pet4Groupbox.Visible = pet5Groupbox.Visible = false;

            leftAccessoryDisplay.Text = String.Empty;
            helmetDisplay.Text = String.Empty;
            rightAccessoryDisplay.Text = String.Empty;
            weaponDisplay.Text = String.Empty;
            armorDisplay.Text = String.Empty;
            shieldDisplay.Text = String.Empty;
            glovesDisplay.Text = String.Empty;
            beltDisplay.Text = String.Empty;
            bootsDisplay.Text = String.Empty;
            itemDescriptionDisplay.Text = String.Empty;

            allyDisplay1.Text = String.Empty;
            allyDisplay2.Text = String.Empty;
            allyDisplay3.Text = String.Empty;
            allyDisplay4.Text = String.Empty;
            allyDisplay5.Text = String.Empty;
            allyPetDisplay1.Text = String.Empty;
            allyPetDisplay2.Text = String.Empty;
            allyPetDisplay3.Text = String.Empty;
            allyPetDisplay4.Text = String.Empty;
            allyPetDisplay5.Text = String.Empty;
            enemyDisplay1.Text = String.Empty;
            enemyDisplay2.Text = String.Empty;
            enemyDisplay3.Text = String.Empty;
            enemyDisplay4.Text = String.Empty;
            enemyDisplay5.Text = String.Empty;
            enemyDisplay6.Text = String.Empty;
            enemyDisplay7.Text = String.Empty;
            enemyDisplay8.Text = String.Empty;
            enemyDisplay9.Text = String.Empty;
            enemyDisplay10.Text = String.Empty;
#endif
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    helpToolStripMenuItem.PerformClick();
                    return true;
                case Keys.F2:
                    linearRegressionModeLabel.Visible = MySettings.Default.UseLinearRegression = !MySettings.Default.UseLinearRegression;
                    MySettings.Default.Save();
                    UpdateForm(GameStatePoller.GetData(displayedInstance));
                    return true;
                case Keys.F3:
                    break;
                case Keys.F4:
                    break;
                case Keys.F5:
                    UpdateForm(GameStatePoller.GetData(displayedInstance));
                    return true;
                case Keys.Alt | Keys.Menu:
                    shouldMainMenuStayOpen = mainMenuStrip.Visible = !mainMenuStrip.Visible;
                    return true;
                default:
                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            TooltipDatabase.LoadDatabase(TOOLTIP_DATABASE_PATH);

            //upgrade settings as necessary
            if (MySettings.Default.UpgradeRequired)
            {
                MySettings.Default.Upgrade();
                MySettings.Default.UpgradeRequired = false;
                MySettings.Default.Save();
            }

            //set up update checker
            //note: start checking for updates 1 minute after launch, then every 24 hours
            _ = Task.Run(() => Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => UpdateChecker.ScheduleUpdates(ProductVersion)));

            //remove old chat logs
            try
            {
#if RELEASE
                await Task.Run(() => ChatLog.PurgeOldRecords(2));
#else
                await Task.Run(() => ChatLog.PurgeOldRecords(30));
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            GameStatePoller.StopPolling();
            MySettings.Default.Save();
            ChatLog.SaveAllChatLogs(0, waitForCompletion: true);
        }

        private async void OnNewAssaAvailable(object sender, NewAssaAvailableEventArgs e)
        {
            if (GameStatePoller.AttachedAssaInstances.Contains(e.Instance) || !e.Data.IsInitialized)
                return;

            attachToolStripMenuItem.Enabled = attachedInstancesListbox.Enabled = false;
            var pollTask = GameStatePoller.StartPolling(e.Instance, e.Data);
            attachToolStripMenuItem.Enabled = attachedInstancesListbox.Enabled = true;

            if (displayedInstance == default)
            {
                attachedInstancesListbox.SelectedIndex = 0;
                AttachedInstancesListbox_SelectedIndexChanged(null, null);
            }

            await e.Instance.InitializeSecondaryControls().ConfigureAwait(false);

            //capture any aggregate exceptions
            try
            {
                await pollTask.ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                //we are shutting down, ignore
            }
        }

        private void OnNewDataAvailable(object sender, NewDataAvailableEventArgs e)
        {
            Lv1EncountDatabase.TrackEncounter(e.Instance.ProcessID, e.Data);
            PetJournal.Update(e.Instance.ProcessID, e.Data);
            ChatLog.Update(e.Instance.ProcessID, e.Data);
            PlayerJournal.Update(e.Data);

            if (e.Instance == displayedInstance)
            {
                UpdateForm(e.Data);
            }
        }

        private void OnNewStatusMessageAvailable(object sender, NewStatusMessageAvailableEventArgs e)
        {
            //update warning label if necessary (prevents flicker)
            if (warningLabel.Text != e.Message)
            {
                warningLabel.Text = e.Message;
            }

            warningLabel.Visible = !String.IsNullOrEmpty(e.Message);
        }

        private void OnAttachedInstanceListChanged(object sender, AttachedInstanceListChangedEventArgs e)
        {
            GameStatePoller.AttachedAssaInstances.ResetBindings();

            if (e != default)
            {
                PlayerJournal.GetPlayerInfo(e.DeadInstance.CachedAccount, e.DeadInstance.CachedCharacter).ResetLastLoginTime();

                if (e.DeadInstance == displayedInstance)
                {
                    displayedInstance = default;
                }
            }
        }

#region Main Menu Strip

        private void ShowMenuTimer_Tick(object sender, EventArgs e)
        {   //show menu if mouse comes close to top border of form title and this form is the active form
            var pos = this.PointToClient(MousePosition);

            if (Form.ActiveForm != default && !petContextMenuStrip.Visible &&
                this.ClientRectangle.Contains(pos) && pos.Y < 8)
            {
                mainMenuStrip.Visible = true;
            }
        }

        private void HideMenuTimer_Tick(object sender, EventArgs e)
        {   //hide menu if mouse leaves the title area and none of the menu items are active
            var pos = mainMenuStrip.PointToClient(MousePosition);
            var area = mainMenuStrip.ClientRectangle;

            //adjust hit area to include the title bar
            area.X -= 10;
            area.Width += 20;
            area.Y -= 40;
            area.Height += 40;

            if (shouldMainMenuStayOpen ||                                                               //menu was opened via ALT
                area.Contains(pos) ||                                                                   //mouse is still in title area
                mainMenuStrip.Items.Cast<ToolStripMenuItem>().Any(x => x.DropDown.Visible == true))     //any of the menuitems are showing
                return;

            mainMenuStrip.Visible = false;
        }

        private async void AttachToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GameStatePoller.StopPolling();

            attachToolStripMenuItem.Enabled = attachedInstancesListbox.Enabled = false;
            warningLabel.Text = "正在擷取ASSA外掛...";
            warningLabel.Visible = true;

            Task pollTask = Task.FromResult(false);
            List<AssaInstance> assaInstances;
            try
            {
                assaInstances = (MySettings.Default.UseOldAssaCapture) ? 
                    await AssaAutomation.GetAssaInstances_v1_5() : 
                    await AssaAutomation.GetAssaInstances(this.Handle, searchAllProcesses: MySettings.Default.SearchAllProcesses);
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 5)
                {
                    MessageBox.Show($"正在執行的ASSA程式中有以管理員身分執行的{Environment.NewLine}請重開本程式並以管理員身分執行", "Access Denied Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
                    return;
                }

                throw;
            }
            if (assaInstances.Count == 0)
            {
                MessageBox.Show("找不到運行中的ASSA", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                pollTask = GameStatePoller.StartPolling(assaInstances);
                attachedInstancesListbox.SelectedIndex = 0;
                AttachedInstancesListbox_SelectedIndexChanged(null, null);
            }

            attachToolStripMenuItem.Enabled = attachedInstancesListbox.Enabled = true;
            warningLabel.Text = String.Empty;
            warningLabel.Visible = false;

            //delayed population of instance controls that aren't needed right away
            foreach (var task in assaInstances.Select(x => x.InitializeSecondaryControls()))
            {
                await task.ConfigureAwait(false);
            }

            //capture any aggregate exceptions
            try
            {
                await pollTask.ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                //we are shutting down, ignore
            }
        }

        private void AutoLogonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AutoLogonForm.ActivateOpenInstance())
                return;

            AutoLogonForm form = new() {
                AttachedInstanceProcessIDs = GameStatePoller.AttachedAssaInstances.Select(x => x.ProcessID).ToList(),
            };

            form.NewAssaAvailableEvent += OnNewAssaAvailable;

            form.Show(this);
        }

        private void ChangeGameSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ChangeGameSizeForm.ActivateOpenInstance())
                return;

            var form = new ChangeGameSizeForm
            {
                StartPosition = FormStartPosition.Manual
            };
            form.Location = new Point(this.Location.X + (this.Width - form.Width) / 2, this.Location.Y + (this.Height - form.Height) / 2);

            form.Show(this);
        }
        
        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new SettingsDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                //the Visible property binding only works on startup, it won't update afterwards so we need to manually update the values
                linearRegressionModeLabel.Visible = MySettings.Default.UseLinearRegression;
                copyStatusMenuItem.Visible = MySettings.Default.CopyPetStatsOnRightClick;
                copyGoupboxScreenshotMenuItem.Visible = MySettings.Default.DisplayCopyPetScreenshotMenuItem;
                saveGroupboxScreenshotMenuItem.Visible = MySettings.Default.DisplaySavePetScreenshotMenuItem;
                copyCurrentStatsMenuItem.Visible = MySettings.Default.DisplayCopyCurrentStatsMenuItem;
                copyBaseStatsMenuItem.Visible = MySettings.Default.DisplayCopyBaseStatsMenuItem;
                copyAllPetMenuItem.Visible = MySettings.Default.DisplayCopyAllPetStatsMenuItem;
                vitMenuItem.Visible = strMenuItem.Visible = defMenuItem.Visible = dexMenuItem.Visible =
                    currentSumMenuItem.Visible = MySettings.Default.DisplayBaseStats;
            }
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new();

            sb.AppendLine("將ASSA外掛開啟後，點選左上角Attach擷取外掛");
            sb.AppendLine("左邊清單會出現外掛腳色名稱，可切換右邊的資料欄");
            sb.AppendLine("左邊石器隱藏/顯示中的按鈕可以點選切換顯示狀態");
            sb.AppendLine("雙擊清單也有同樣效果");
            sb.AppendLine("可使用清單左方按鈕改變清單順序");
            sb.AppendLine();
            sb.AppendLine("寵物成長計算機:");
            sb.AppendLine("寵物欄中如果寵物的暱稱是一級四圍");
            sb.AppendLine("則會顯示該寵物的成長值 (舉例: 36.13.5.9)");
            sb.AppendLine("若不是一級則可以加底線接等級，一樣會計算");
            sb.AppendLine("(舉例: 298.128.64.66_45)");
            sb.AppendLine("若沒有暱稱數值則會嘗試自行記錄升級過程並計算");
            sb.AppendLine();
            sb.AppendLine("在寵物欄上開啟右鍵選單會顯示幾個數值:");
            sb.AppendLine("1. 如果寵物為二轉，會顯示指定預測激素等級的數值");
            sb.AppendLine("2. 如果一級四圍判斷成功，會顯示指定預測等級的數值");
            sb.AppendLine("3. 基礎四項能力的估測值");
            sb.AppendLine("4. 攻防敏的加總值");
            sb.AppendLine();
            sb.AppendLine("寵物欄各項顯示是可以按右鍵自動複製的:");
            sb.AppendLine("在暱稱上按右鍵會複製暱稱");
            sb.AppendLine("在血攻防敏數值任意位置上按右鍵會複製四圍面板數值");
            sb.AppendLine("在任意成長率位置上按右鍵會複製四圍成長率");
            sb.AppendLine("在空白處按右鍵會複製設定選單中的自定義數值");
            sb.AppendLine();
            sb.AppendLine("點選寵物欄右上角的寵物狀態會開啟Assa的寵物選單");
            sb.AppendLine("但是此功能有點慢，請耐心等候");
            sb.AppendLine();
            sb.AppendLine("補運小幫手:");
            sb.AppendLine("在道具欄中如果有補運委託書");
            sb.AppendLine("滑鼠移動到該道具位置則會顯示補運道具入手管道");
            sb.AppendLine("資料庫是tooltipData.csv，有不足可自行補足");
            sb.AppendLine();
            sb.AppendLine("在補運道具上按右鍵可以啟動自動料理腳本功能");
            sb.AppendLine("會自動產生該料理的腳本並傳送到外掛直接執行");
            sb.AppendLine("使用此功能時請自備料理寵物一隻，其他腳本會提醒");
            sb.AppendLine();
            sb.AppendLine("F2: 啟用/停用線性回歸寵物成長計算優先模式");
            sb.AppendLine("F5: 立刻更新程式頁面");
            sb.AppendLine();
            sb.AppendLine($"版本: {ProductVersion}");

            MessageBox.Show(sb.ToString(), "Setsu 製品說明", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

#endregion

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async void TestButton_Click(object sender, EventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                

                //Debugger.Break();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

#region Left Pane Controls

        private volatile bool isReoderingAttachedInstancesList = false;

        private async void HideInstanceCheckbox_Click(object sender, EventArgs e)
        {
            if (displayedInstance == default)
                return;
            else if (displayedInstance.HideInstanceCheckbox == default)
            {
                ShowTooltipAtMouse("尚未初始化完畢請稍後再次嘗試");
                return;
            }

            try
            {
                await AssaAutomation.InvokeElement(displayedInstance.HideInstanceCheckbox).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //do nothing
            }
        }

        private void AttachedInstancesListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //note: this event won't fire if you handle MouseDown and use clicks to change index, but will still fire if using keyboard

            var newActiveInstance = attachedInstancesListbox.SelectedItem as AssaInstance;

            if (isReoderingAttachedInstancesList || newActiveInstance == default || newActiveInstance == displayedInstance)
                return;

            displayedInstance = newActiveInstance;
            UpdateForm(GameStatePoller.GetData(newActiveInstance));

            //update chat window only if we are showing it
            if (playerTabControl.SelectedIndex == 4)
            {
                ReloadChatWindow();
            }
        }

        private void AttachedInstancesListbox_DoubleClick(object sender, EventArgs e)
            => HideInstanceCheckbox_Click(null, null);

        private void MoveSelectionUpButton_Click(object sender, EventArgs e)
        {
            attachedInstancesListbox.Focus();

            if (attachedInstancesListbox.SelectedItem == default)
                return;

            var index = attachedInstancesListbox.SelectedIndex;
            if (index == 0)
                return;

            ReorderAttachedInstancesListbox(index - 1, attachedInstancesListbox.SelectedItem as AssaInstance);
        }

        private void MoveSelectionDownButton_Click(object sender, EventArgs e)
        {
            attachedInstancesListbox.Focus();

            if (attachedInstancesListbox.SelectedItem == default)
                return;

            var index = attachedInstancesListbox.SelectedIndex;
            if (index == attachedInstancesListbox.Items.Count - 1)
                return;

            ReorderAttachedInstancesListbox(index + 1, attachedInstancesListbox.SelectedItem as AssaInstance);
        }

        private void ReorderAttachedInstancesListbox(int newIndex, AssaInstance objToMove)
        {
            isReoderingAttachedInstancesList = true;
            attachedInstancesListbox.SuspendLayout();
            GameStatePoller.AttachedAssaInstances.Remove(objToMove);
            GameStatePoller.AttachedAssaInstances.Insert(newIndex, objToMove);
            attachedInstancesListbox.SelectedIndex = newIndex;
            attachedInstancesListbox.ResumeLayout();
            isReoderingAttachedInstancesList = false;
        }

#endregion

        private void PlayerTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            //check if we activated the chat tab, and update if necessary
            //this is different from every other tab, which updates when we switch users
            //this is because updating the richtextbox is very costly
            if (playerTabControl.SelectedIndex == 4 && displayedInstance != default && displayedInstance.IsAlive() &&
                displayedInstance.ProcessID != activeChatWindowProcessID)
            {
                chatWindowRichTextbox.Clear();
                BeginInvoke((MethodInvoker)ReloadChatWindow);
            }
        }

#region Pet Tab

        private void PetNicknameLabel_MouseEnter(object sender, EventArgs e)
            => toolTip1.SetToolTip(sender as Control, (sender as Label).Text);
       
        private async void PetStatusTextbox_MouseClick(object sender, MouseEventArgs e)
        {
            if (displayedInstance.ShowInfoButton == default)
            {
                ShowTooltipAtMouse("尚未初始化完畢請稍後再次嘗試");
                return;
            }

            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
            {
                ShowTooltipAtMouse("尚未取得外掛資料請稍後再次嘗試");
                return;
            }

            //build slot filled status array
            var petSlotFilledStatus = data.Pets.Select(x => x.IsPresent).ToArray();

            ShowTooltipAtMouse("正在聯絡ASSA ...");

            //try to get player info window, or open it if not open
            var playerInfoWindow = await AssaAutomation.GetPlayerInfoWindow(displayedInstance);

            var tabPane = await AssaAutomation.SetPlayerInfoWindowActiveTab(playerInfoWindow, "人寵資料", "frmSub0");
            if (tabPane == default)
            {
                ShowTooltipAtMouse("切換至 人寵資料 頁面失敗");
                return;
            }

            try
            {
                await AssaAutomation.InvokePetContextMenu(playerInfoWindow, tabPane, int.Parse((sender as Control).Tag as string), petSlotFilledStatus);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PetContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            e.Cancel = !data.IsInitialized;
            if (e.Cancel)
                return;

            var callerControl = (sender as ContextMenuStrip).SourceControl;
            var petIndex = int.Parse(callerControl.Tag as string);
            var pet = data.Pets[petIndex];

            //calculate all estimations and determine which ones succeeded
            var rates = PetCalculator.CalculateGrowthRate(pet, MySettings.Default.UseLinearRegression);
            var predictStats = PetCalculator.PredictStats(pet, rates, Convert.ToUInt32(MySettings.Default.PredictLevel));
            var boostStats = PetCalculator.PredictBoost(pet, Convert.ToUInt32(MySettings.Default.BoostLevel));
            bool showPredictStats = (predictStats.Length > 0) & MySettings.Default.DisplayPredictStats;
            bool showBoostStats = (boostStats.Length > 0) & MySettings.Default.DisplayBoostStats;

            //adjust visibility of context menu items that have other criteria than the saved settings
            copyPredictStatsMenuItem.Visible = (predictStats.Length > 0) & MySettings.Default.DisplayCopyPredictStatsMenuItem;
            copyBoostStatsMenuItem.Visible = (boostStats.Length > 0) & MySettings.Default.DisplayCopyBoostStatsMenuItem;
            hpMenuItem.Visible = attackMenuItem.Visible = armorMenuItem.Visible = speedMenuItem.Visible = 
                predictSumMenuItem.Visible = predictedStatsSeparator.Visible = showPredictStats;
            boostHPMenuItem.Visible = boostAttackMenuItem.Visible = boostArmorMenuItem.Visible = boostSpeedMenuItem.Visible =
                boostSumMenuItem.Visible = boostStatsSeparator.Visible = showBoostStats;

            //update stat displays as necessary
            if (showBoostStats)
            {
                var boostBaseStats = PetCalculator.EstimateBaseStats(boostStats);
                boostHPMenuItem.Text = $"{$"HP: {boostStats[0]}",-15}體: {boostBaseStats[0]:0.##}";
                boostAttackMenuItem.Text = $"{$"攻: {boostStats[1]}",-16}腕: {boostBaseStats[1]:0.##}";
                boostArmorMenuItem.Text = $"{$"防: {boostStats[2]}",-16}耐: {boostBaseStats[2]:0.##}";
                boostSpeedMenuItem.Text = $"{$"敏: {boostStats[3]}",-16}速: {boostBaseStats[3]:0.##}";
                boostSumMenuItem.Text = MySettings.Default.AssumeBoostKingPet ?
                    $"{$"三圍總和: {boostStats.Skip(1).Sum()}",-15}(寵物王)(LV{MySettings.Default.BoostLevel}預測)" :
                    $"{$"三圍總和: {boostStats.Skip(1).Sum()}",-20}(LV{MySettings.Default.BoostLevel}預測)";
            }
            if (showPredictStats)
            {
                var predictBaseStats = PetCalculator.EstimateBaseStats(predictStats);
                hpMenuItem.Text = $"{$"HP: {predictStats[0]}",-15}體: {predictBaseStats[0]:0.##}";
                attackMenuItem.Text = $"{$"攻: {predictStats[1]}",-16}腕: {predictBaseStats[1]:0.##}";
                armorMenuItem.Text = $"{$"防: {predictStats[2]}",-16}耐: {predictBaseStats[2]:0.##}";
                speedMenuItem.Text = $"{$"敏: {predictStats[3]}",-16}速: {predictBaseStats[3]:0.##}";
                predictSumMenuItem.Text = $"{$"三圍總和: {predictStats.Skip(1).Sum()}",-20}(LV{MySettings.Default.PredictLevel}預測)";
            }
            if (MySettings.Default.DisplayBaseStats)
            {
                var baseStats = PetCalculator.EstimateBaseStats(pet);
                vitMenuItem.Text = $"體: {baseStats[0]:0.##}";
                strMenuItem.Text = $"腕: {baseStats[1]:0.##}";
                defMenuItem.Text = $"耐: {baseStats[2]:0.##}";
                dexMenuItem.Text = $"速: {baseStats[3]:0.##}";
                currentSumMenuItem.Text = $"三圍總和: {pet.Attack + pet.Armor + pet.Speed}";
            }

            DragonEvolveMenuHelper();

            if (!MySettings.Default.CopyPetStatsOnRightClick)
                return;

            //handle auto copy of nickname
            if (petNicknameDisplays.Contains(callerControl))
            {
                copyStatusMenuItem.Text = TryClipboardSetText(callerControl.Text) ?
                    "已複製: 暱稱" : "自動複製失敗: 剪貼簿存取錯誤";

                return;
            }

            //handle auto copy of current stats
            if (petStatsDisplays.Contains(callerControl) & MySettings.Default.EnableDifferentialCopyBasedOnClickTarget)
            {
                copyStatusMenuItem.Text = TryClipboardSetText($"{pet.Level}\t{pet.MaxHP}\t{pet.Attack}\t{pet.Armor}\t{pet.Speed}") ?
                    "已複製: 面板數值" : "自動複製失敗: 剪貼簿存取錯誤";

                return;
            }

            //handle auto copy of rates
            //note: no need to check if rates are unavailable because in that case the rate labels won't show
            if (petRateDisplays.Contains(callerControl) & MySettings.Default.EnableDifferentialCopyBasedOnClickTarget)
            {
                copyStatusMenuItem.Text = TryClipboardSetText(String.Join("\t", rates)) ? 
                    "已複製: 成長率" : "自動複製失敗: 剪貼簿存取錯誤";

                return;
            }

            //handle auto copy of custom format
            string text = PetCalculator.FormatPetStats(pet, MySettings.Default.PetStatCopyFormat);
            copyStatusMenuItem.Text = TryClipboardSetText(text) ? "已複製: 自定義格式" : "自動複製失敗: 剪貼簿存取錯誤";

            return;

            void DragonEvolveMenuHelper()
            {
                var dragons = new List<string>() { "揚奇洛斯", "邦奇諾", "利則諾頓", "布魯頓", "邦浦洛斯" };
                predictDragonEvolveMenuItem.Visible = false;

                if (dragons.All(x => x != pet.Name))
                    return;

                dragons.Remove("邦浦洛斯"); //not a valid candidate choice

                var evolveBaseStats = new double[4];

                for (int i = 0; i < 5; i++)
                {
                    if (i == petIndex)
                        continue;

                    var candidate = data.Pets[i];
                    if (!candidate.IsPresent || candidate.Level > 140)
                        return;

                    var index = dragons.IndexOf(candidate.Name);
                    if (index < 0)
                        return;

                    var baseStats = PetCalculator.EstimateBaseStats(candidate);
                    evolveBaseStats[index] = baseStats[index];  //dragon list was initialized with a specific order so that this would work

                    dragons[index] = String.Empty;  //prevent the same type from being chosen twice
                }

                predictDragonEvolveMenuItem.Visible = true;

                var evolveStats = PetCalculator.EstimateDragonEvolveStats(evolveBaseStats);
                dragonEvolveHPMenuItem.Text = $"HP: {Math.Truncate(evolveStats[0]):F0}";
                dragonEvolveAttackMenuItem.Text = $"攻: {Math.Truncate(evolveStats[1]):F0}";
                dragonEvolveArmorMenuItem.Text = $"防: {Math.Truncate(evolveStats[2]):F0}";
                dragonEvolveSpeedMenuItem.Text = $"敏: {Math.Truncate(evolveStats[3]):F0}";
            }
        }

        private void CopyGroupboxScreenshotMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, var pet, var rates, _) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
            {
                ShowTooltipAtMouse("截圖失敗, 請重新嘗試或再次 Attach");
                return;
            }

            var bitmap = MakeGroupboxImage(pet, rates, groupbox);
            Clipboard.SetImage(bitmap);
            bitmap.Dispose();
        }

        private void SaveGroupboxScreenshotMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, var pet, var rates, _) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
            {
                ShowTooltipAtMouse("截圖失敗, 請重新嘗試或再次 Attach");
                return;
            }

            string filename = rates.Count() > 0 ?
                $"{pet.Name} LV{pet.Level} {pet.Reincarnation}轉 HP{pet.MaxHP} 成長{rates.Last():0.###}.jpg":
                $"{pet.Name} LV{pet.Level} {pet.Reincarnation}轉 HP{pet.MaxHP}.jpg";

            if (!Directory.Exists(PET_IMAGE_FOLDER))
            {
                Directory.CreateDirectory(PET_IMAGE_FOLDER);
            }

            var bitmap = MakeGroupboxImage(pet, rates, groupbox);
            bitmap.Save(Path.Combine(PET_IMAGE_FOLDER, filename), System.Drawing.Imaging.ImageFormat.Jpeg);
            bitmap.Dispose();
        }

        private void CopyCurrentStatsMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, var pet, _, _) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
                return;

            TryClipboardSetText($"{pet.Level}\t{pet.MaxHP}\t{pet.Attack}\t{pet.Armor}\t{pet.Speed}");
        }

        private void CopyBaseStatsMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, _, _, var baseStats) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
                return;

            TryClipboardSetText(String.Join("\t", baseStats));
        }

        private void CopyPredictStatsMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, var pet, var rates, _) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
                return;

            var predictStats = PetCalculator.PredictStats(pet, rates, Convert.ToUInt32(MySettings.Default.PredictLevel));

            TryClipboardSetText(String.Join("\t", predictStats));
        }

        private void CopyBoostStatsMenuItem_Click(object sender, EventArgs e)
        {
            (var groupbox, var pet, _, _) = PetControlAndStatsHelper(sender);
            if (groupbox == default)
                return;

            var boostStats = PetCalculator.PredictBoost(pet, Convert.ToUInt32(MySettings.Default.BoostLevel));

            TryClipboardSetText(String.Join("\t", boostStats));
        }

        private void CopyAllPetCustomFormatMenuItem_Click(object sender, EventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            StringBuilder buffer = new();

            foreach (var pet in data.Pets)
            {
                if (!pet.IsPresent)
                    continue;
                if (PetCalculator.CalculateGrowthRate(pet, MySettings.Default.UseLinearRegression).Length == 0)
                    continue;

                buffer.AppendLine(PetCalculator.FormatPetStats(pet, MySettings.Default.PetStatCopyFormat));
            }

            if (buffer.Length > 0)
            {
                TryClipboardSetText(buffer.ToString());
            }
        }

        private void CopyAllPetCurrentStatsMenuItem_Click(object sender, EventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            StringBuilder buffer = new();

            foreach (var pet in data.Pets)
            {
                if (!pet.IsPresent)
                    continue;

                buffer.AppendLine($"{pet.Level}\t{pet.MaxHP}\t{pet.Attack}\t{pet.Armor}\t{pet.Speed}");
            }

            if (buffer.Length > 0)
            {
                TryClipboardSetText(buffer.ToString());
            }
        }

        private void CopyAllPetBaseStatsMenuItem_Click(object sender, EventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            StringBuilder buffer = new();

            foreach (var pet in data.Pets)
            {
                if (!pet.IsPresent)
                    continue;

                buffer.AppendLine(String.Join("\t", PetCalculator.EstimateBaseStats(pet)));
            }

            if (buffer.Length > 0)
            {
                TryClipboardSetText(buffer.ToString());
            }
        }

        private void CopyAllPetPredictStatsMenuItem_Click(object sender, EventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            StringBuilder buffer = new();

            foreach (var pet in data.Pets)
            {
                if (!pet.IsPresent)
                    continue;

                var rates = PetCalculator.CalculateGrowthRate(pet, MySettings.Default.UseLinearRegression);
                var predictStats = PetCalculator.PredictStats(pet, rates, Convert.ToUInt32(MySettings.Default.PredictLevel));

                if (predictStats.Length == 0)
                    continue;

                buffer.AppendLine($"{MySettings.Default.PredictLevel}\t{String.Join("\t", predictStats)}");
            }

            if (buffer.Length > 0)
            {
                TryClipboardSetText(buffer.ToString());
            }
        }

        private void CopyAllPetBoostStatsMenuItem_Click(object sender, EventArgs e)
        {
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            StringBuilder buffer = new();

            foreach (var pet in data.Pets)
            {
                if (!pet.IsPresent)
                    continue;

                var boostStats = PetCalculator.PredictBoost(pet, Convert.ToUInt32(MySettings.Default.BoostLevel));

                if (boostStats.Length == 0)
                    continue;

                buffer.AppendLine($"{MySettings.Default.BoostLevel}\t{String.Join("\t", boostStats)}");
            }

            if (buffer.Length > 0)
            {
                TryClipboardSetText(buffer.ToString());
            }
        }

        private (GroupBox Groupbox, Pet Pet, decimal[] Rates, Vector<double> BaseStats) PetControlAndStatsHelper(object sender)
        {
            var menuItem = sender as ToolStripMenuItem;
            var menuStrip = menuItem?.Owner as ContextMenuStrip;
            GroupBox groupbox = menuStrip?.SourceControl is GroupBox ?
                menuStrip?.SourceControl as GroupBox :
                menuStrip?.SourceControl?.Parent as GroupBox;
            if (groupbox == default)
                return (default, default, default, default);

            var petLocation = int.Parse(groupbox.Tag as string);
            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return (default, default, default, default);

            var pet = data.Pets[petLocation];
            var rates = PetCalculator.CalculateGrowthRate(pet, MySettings.Default.UseLinearRegression);
            var baseStats = PetCalculator.EstimateBaseStats(pet);
            return (groupbox, pet, rates, baseStats);
        }

        private Bitmap MakeGroupboxImage(Pet pet, decimal[] rates, GroupBox groupbox)
        {
            var predictStats = PetCalculator.PredictStats(pet, rates, Convert.ToUInt32(MySettings.Default.PredictLevel));

            Bitmap output;

            if (predictStats.Count() > 0)
            {
                //build groupbox control
                using GroupBox virtualGroupbox = new();
                using Label virtualNicknameLabel = new();
                using Label virtualLevelLabel = new();
                using Label virtualHPLabel = new();
                using Label virtualAttackLabel = new();
                virtualGroupbox.Controls.Add(virtualNicknameLabel);
                virtualGroupbox.Controls.Add(virtualLevelLabel);
                virtualGroupbox.Controls.Add(virtualHPLabel);
                virtualGroupbox.Controls.Add(virtualAttackLabel);
                virtualGroupbox.Size = new Size(87, 215);
                virtualGroupbox.BackColor = Color.White;
                virtualGroupbox.Scale(new SizeF(1, (float)groupbox.Height / virtualGroupbox.Height));
                virtualNicknameLabel.AutoSize = true;
                virtualNicknameLabel.Location = new Point(5, 18);
                virtualNicknameLabel.Size = new Size(80, 20);
                virtualNicknameLabel.Text = "預測數值";
                virtualLevelLabel.AutoSize = true;
                virtualLevelLabel.Location = new Point(6, 41);
                virtualLevelLabel.Size = new Size(32, 17);
                virtualLevelLabel.Text = $"LV: {MySettings.Default.PredictLevel}";
                virtualHPLabel.AutoSize = true;
                virtualHPLabel.Location = new Point(6, 101);
                virtualHPLabel.Size = new Size(84, 17);
                virtualAttackLabel.AutoSize = true;
                virtualAttackLabel.Location = new Point(6, 118);
                virtualAttackLabel.Size = new Size(32, 17);

                virtualHPLabel.Text = $"HP: {predictStats[0]}";
                virtualAttackLabel.Text = $"攻擊: {predictStats[1]}{Environment.NewLine}防禦: {predictStats[2]}{Environment.NewLine}敏捷: {predictStats[3]}";
                
                output = new Bitmap(groupbox.Width + virtualGroupbox.Width, groupbox.Height);
                using Graphics painter = Graphics.FromImage(output);
                using Bitmap bitmap1 = new(groupbox.Width, groupbox.Height);
                using Bitmap bitmap2 = new(virtualGroupbox.Width, groupbox.Height);

                groupbox.DrawToBitmap(bitmap1, groupbox.ClientRectangle);
                virtualGroupbox.DrawToBitmap(bitmap2, virtualGroupbox.ClientRectangle);
                painter.DrawImage(bitmap1, 0, 0);
                painter.DrawImage(bitmap2, groupbox.Width, 0);
            }
            else
            {
                output = new Bitmap(groupbox.Width, groupbox.Height);
                groupbox.DrawToBitmap(output, groupbox.ClientRectangle);
            }

            return output;
        }

#endregion

#region Item Tab

        private void ItemGridView_Scroll(object sender, ScrollEventArgs e)
            => e.NewValue = e.OldValue; //disables scrolling

        private void EquipmentDisplay_MouseEnter(object sender, EventArgs e)
            => itemDescriptionDisplay.Text = (sender as Control).Tag as string;

        private void EquipmentDisplay_Click(object sender, EventArgs e)
        {
            if (!MySettings.Default.CopyItemNameOnClick)
                return;

            Label label = sender as Label;
            string itemName = label.Text;

            //remove durability text
            if (itemName.Contains(Environment.NewLine))
            {
                itemName = itemName.Remove(itemName.IndexOf(Environment.NewLine));
            }

            if (String.IsNullOrWhiteSpace(itemName))
                return;

            if (!TryClipboardSetText(itemName))
            {
                ShowTooltipAtMouse("自動複製失敗: 剪貼簿存取錯誤");
            }
        }

        private static readonly Regex dailyLuckItemNameParser = new(@"\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}止");
        private static readonly Regex dailyLuckItemDescriptionParser = new(@"(?<=綁定]).+及.+");
        private static readonly Regex autoWordWrapParser = new(@"(.{50})");

        private void ItemGridView_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex > 5 ||
                e.RowIndex < 0 || e.RowIndex > 4)
                return;

            itemGridView.ContextMenuStrip = null;

            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            int i = e.ColumnIndex / 2 * 5 + e.RowIndex;
            var item = data.Items[i];

            if (!item.IsPresent)
            {
                itemDescriptionDisplay.Text = String.Empty;
            }
            else if (item.IsFragile)
            {
                itemDescriptionDisplay.Text = $"[ 耐: {item.Durability}%]  {item.Description}";
            }
            else
            {
                itemDescriptionDisplay.Text = $"[不會損壞]  {item.Description}";
            }

            //handle tooltip

            if (!item.IsPresent || !dailyLuckItemNameParser.IsMatch(item.Name))
                return;

            var match = dailyLuckItemDescriptionParser.Match(item.Description);
            if (!match.Success)
                return;

            itemGridView.ContextMenuStrip = itemContextMenuStrip;
            createAndExecuteScriptMenuItem.Visible = 
                recipeNameMenuItem.Visible = ingredientListMenuItem.Visible = false;


            var split = match.Value.Split('及');
            StringBuilder sb = new();
            foreach (var identifier in split)
            {
                string name = identifier.Replace(",", String.Empty);
                var description = TooltipDatabase.GetTooltip(name);
                string wordWrapped = autoWordWrapParser.Replace(description, $"$1{Environment.NewLine}").Trim();
                sb.AppendLine($"[ {name} ] {wordWrapped}");

                //special case to generate recipe script for 愛情御飯團
                if (name == "結婚蛋糕")
                {
                    name = "愛情御飯團";
                    description = TooltipDatabase.GetTooltip(name);
                }

                //set up context menu
                if (RecipeScriptGenerator.GenerateRecipeScript(name, description).Success)
                {
                    createAndExecuteScriptMenuItem.Tag = (name, description);
                    recipeNameMenuItem.Text = name;
                    ingredientListMenuItem.Text = description;
                    createAndExecuteScriptMenuItem.Visible =
                        recipeNameMenuItem.Visible = ingredientListMenuItem.Visible = true;
                }
                else 
                {
                    //this assumes that pets are listed last, as it might be executed twice if it's an item/pet combo
                    copyRequestedPetNameToClipboardMenuItem.Text = $"複製 [ {name} ] 至剪貼簿";
                    copyRequestedPetNameToClipboardMenuItem.Tag = name;
                }
            }

            ShowTooltipAtMouse(sb.ToString());
        }

        private void ItemGridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
            => itemGridView.ContextMenuStrip = null;

        private void ItemGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!MySettings.Default.CopyItemNameOnClick ||
                e.ColumnIndex < 1 || e.ColumnIndex > 5 || e.ColumnIndex == 2 || e.ColumnIndex == 4 ||
                e.RowIndex < 0 || e.RowIndex > 4)
                return;

            string itemName = itemGridView.Rows[e.RowIndex].Cells[itemGridViewColumns[e.ColumnIndex]].Value as String;
            if (String.IsNullOrWhiteSpace(itemName))
                return;

            if (!TryClipboardSetText(itemName))
            {
                ShowTooltipAtMouse("自動複製失敗: 剪貼簿存取錯誤");
            }
        }

        private async void CreateAndExecuteScriptMenuItem_Click(object sender, EventArgs e)
        {
            if (displayedInstance.ScriptEditorButton == default)
            {
                ShowTooltipAtMouse("尚未初始化完畢請稍後再次嘗試");
                return;
            }

            var tuple = (sender as ToolStripMenuItem).Tag;
            (string item, string description) = (ValueTuple<string, string>)tuple;
            (_, var script) = RecipeScriptGenerator.GenerateRecipeScript(item, description);

            ShowTooltipAtMouse("正在聯絡ASSA ...");

            try
            {
                await AssaAutomation.ExecuteScript(displayedInstance, script).ConfigureAwait(false);
            }
            catch (ApplicationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyRequestedPetNameToClipboardMenuItem_Click(object sender, EventArgs e)
        {
            var bytes = Encoding.Unicode.GetBytes((sender as ToolStripMenuItem).Tag as string);
            var big5 = Encoding.GetEncoding("BIG5");
            bytes = Encoding.Convert(Encoding.Unicode, big5, bytes);

            Clipboard.SetText(big5.GetString(bytes), TextDataFormat.Text);

            if (!TryClipboardSetText(big5.GetString(bytes)))
            {
                ShowTooltipAtMouse("自動複製失敗: 剪貼簿存取錯誤");
            }
        }

#endregion

#region Chat Window Tab

        private volatile bool chatWindowInitializing = true;
        private volatile bool allChannelCheckboxCheckstateChanging = false;
        private int activeChatWindowProcessID;

        private void AllChannelsCheckbox_Click(object sender, EventArgs e)
        {
            if (chatWindowInitializing)
                return;

            allChannelsCheckbox.CheckState = (allChannelsCheckbox.CheckState == CheckState.Checked) ? CheckState.Unchecked : CheckState.Checked;

            allChannelCheckboxCheckstateChanging = true;

            //update all settings
            MySettings.Default.ShowTeamChannel = MySettings.Default.ShowPrivateChannel =
                MySettings.Default.ShowFamilyChannel = MySettings.Default.ShowJobChannel =
                MySettings.Default.ShowAnnounceChannel = MySettings.Default.ShowWorldChannel =
                MySettings.Default.ShowInterworldChannel = MySettings.Default.ShowBroadcastChannel = 
                MySettings.Default.ShowSystemChannel = allChannelsCheckbox.Checked;

            ReloadChatWindow();

            allChannelCheckboxCheckstateChanging = false;
        }

        private void ChannelCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (allChannelCheckboxCheckstateChanging)
                return;
            else if (teamChannelCheckbox.Checked && privateChannelCheckbox.Checked &&
                familyChannelCheckbox.Checked && jobChannelCheckbox.Checked &&
                announceChannelCheckbox.Checked && worldChannelCheckbox.Checked &&
                broadcastChannelCheckbox.Checked && systemChannelCheckbox.Checked &&
                interworldChannelCheckbox.Checked)
            {
                allChannelsCheckbox.CheckState = CheckState.Checked;
            }
            else if (!teamChannelCheckbox.Checked && !privateChannelCheckbox.Checked &&
                !familyChannelCheckbox.Checked && !jobChannelCheckbox.Checked &&
                !announceChannelCheckbox.Checked && !worldChannelCheckbox.Checked &&
                !broadcastChannelCheckbox.Checked && !systemChannelCheckbox.Checked &&
                !interworldChannelCheckbox.Checked)
            {
                allChannelsCheckbox.CheckState = CheckState.Unchecked;
            }
            else
            {
                allChannelsCheckbox.CheckState = CheckState.Indeterminate;
            }

            if (chatWindowInitializing)
                return;

            BeginInvoke((MethodInvoker) ReloadChatWindow);
        }

        private void EnableMergedChatMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chatWindowInitializing)
                return;

            BeginInvoke((MethodInvoker)ReloadChatWindow);
        }

        private void ClearChatButton_Click(object sender, EventArgs e)
        {
            if (activeChatWindowProcessID == default)
                return;

            chatWindowRichTextbox.Clear();
            ChatLog.SaveChatLog(activeChatWindowProcessID);
        }

        private void TextColorCombobox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            if (!e.State.HasFlag(DrawItemState.ComboBoxEdit))
            {
                e.DrawBackground();
            }

            var color = (Color)textColorCombobox.Items[e.Index];
            var rect = new Rectangle(e.Bounds.Left + 1, e.Bounds.Top + 1, 2 * (e.Bounds.Height - 2), e.Bounds.Height - 2);
            using SolidBrush b = new(color);
            e.Graphics.FillRectangle(b, rect);
            e.Graphics.DrawRectangle(Pens.Black, rect);
        }

        private void TextColorCombobox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (textColorCombobox.SelectedIndex < 0)
                return;

            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            //we can use selected index mapping because there is a 1:1 mapping between the index and the color code used by the game
            SAMemoryWriter.ChangeTextInputColor(data.GameClientHandle, textColorCombobox.SelectedIndex);

            if (textInputCombobox.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() => textInputCombobox.Focus()));
            }
        }

        private void TextInputCombobox_KeyDown(object sender, KeyEventArgs e)
        {
            if (textColorCombobox.SelectedIndex < 0)
                return;

            var data = GameStatePoller.GetData(displayedInstance);
            if (!data.IsInitialized)
                return;

            if (e.KeyCode == Keys.Enter)
            {
                var text = textInputCombobox.Text;
                if (text.Length > 86)
                {
                    text = text.Remove(86);
                }

                SAMemoryWriter.SendChatMessage(data.GameClientHandle, text);
                textInputCombobox.Items.Insert(0, text);
                textInputCombobox.ResetText();

                //limit size of sent chat cache
                if (textInputCombobox.Items.Count > 100)
                {
                    textInputCombobox.Items.RemoveAt(textInputCombobox.Items.Count - 1);
                }

                e.SuppressKeyPress = true;
            }
        }

        private void ReloadChatWindow()
        {
            if (displayedInstance == default || !displayedInstance.IsAlive())
                return;

            activeChatWindowProcessID = displayedInstance.ProcessID;

            chatWindowRichTextbox.Clear();
            ChatLog.RequestChatRecord(activeChatWindowProcessID);
        }

        private void UpdateChatWindow(object sender, NewChatAvailableEventArgs e)
        {
            if (displayedInstance == default || activeChatWindowProcessID == default || 
                !MySettings.Default.EnableMergedChatMode && activeChatWindowProcessID != e.ProcessID)
                return;

            int cursorPos = chatWindowRichTextbox.SelectionStart;
            bool shouldRestoreScrollPos = !chatWindowRichTextbox.IsScrollBarAtBottom();
            bool isFromNonActiveInstance = activeChatWindowProcessID != e.ProcessID;

            //filter lines to ignore channels based on settings
            var filteredLines = e.ChatLines.Where(x => (x.Channel == ChannelType.Local) ||
                                                       (x.Channel == ChannelType.Custom) ||
                                                       (MySettings.Default.ShowTeamChannel && x.Channel == ChannelType.Team) ||
                                                       (MySettings.Default.ShowPrivateChannel && x.Channel == ChannelType.Private) ||
                                                       (MySettings.Default.ShowFamilyChannel && x.Channel == ChannelType.Family) ||
                                                       (MySettings.Default.ShowAnnounceChannel && x.Channel == ChannelType.Announce) ||
                                                       (MySettings.Default.ShowJobChannel && x.Channel == ChannelType.Job) ||
                                                       (MySettings.Default.ShowWorldChannel && x.Channel == ChannelType.World) ||
                                                       (MySettings.Default.ShowInterworldChannel && x.Channel == ChannelType.Interworld) ||
                                                       (MySettings.Default.ShowBroadcastChannel && x.Channel == ChannelType.Broadcast) ||
                                                       (MySettings.Default.ShowSystemChannel && x.Channel == ChannelType.System)).ToList();

            //filter out unwanted merging lines
            IList<ChatLine> chatLines = filteredLines;
            if (isFromNonActiveInstance)
            {
                HashSet<ChatLine> lastLines = new(chatWindowRichTextbox.DisplayedChatLines.Reverse().Take(20));
                chatLines = chatLines.Where(x => !ChatLine.ChannelType.IgnoreIfMerging.HasFlag(x.Channel) && !lastLines.Contains(x)).ToList();
            }

            if (chatLines.Count == 0)
                return;

            chatWindowRichTextbox.SuspendDrawing();
            if (shouldRestoreScrollPos)
            {
                chatWindowRichTextbox.SaveScrollPos();
            }

            chatWindowRichTextbox.AppendLines(chatLines, x => x.SourceProcessID != activeChatWindowProcessID);

            if (shouldRestoreScrollPos)
            {
                chatWindowRichTextbox.SelectionStart = cursorPos;
                chatWindowRichTextbox.RestoreScrollPos();
            }
            else
            {
                chatWindowRichTextbox.ScrollToEnd();
            }
            chatWindowRichTextbox.ResumeDrawing();

            chatWindowInitializing = false;
        }

#endregion

#region Lv1 Stats Tab

        private void RefreshLv1StatsButton_Click(object sender, EventArgs e)
        {
            lv1StatsGridView.Rows.Clear();

            foreach (var (MapID, MapName, Locations, PetName, MinHPRange, MaxHPRange) in Lv1EncountDatabase.GetLv1EncountStats())
            {
                if (!MySettings.Default.DisableMapList)
                {
                    bool skip;

                    if (MySettings.Default.UseMapListAsWhitelist)
                    {   //whitelist mode
                        skip = true;

                        foreach (var map in MySettings.Default.MapNameList)
                        {
                            if (MapName.Contains(map))
                            {
                                skip = false;
                                break;
                            }
                        }
                    }
                    else
                    {   //blacklist mode
                        skip = false;

                        foreach (var map in MySettings.Default.MapNameList)
                        {
                            if (MapName.Contains(map))
                            {
                                skip = true;
                                break;
                            }
                        }
                    }

                    if (skip)
                        continue;
                }

                lv1StatsGridView.Rows.Add($"{MapName} ({MapID})", String.Join(String.Empty, Locations.Select(x => $"({x.X},{x.Y})")), PetName, MinHPRange, MaxHPRange);
            }
        }

        private void ClearLv1StatsButton_Click(object sender, EventArgs e)
        {
            lv1StatsGridView.Rows.Clear();
            Lv1EncountDatabase.Clear();
        }

#endregion

        private void UpdateForm(DataPackage data)
        {
            if (!data.IsInitialized)
                return;

            if (data.IsVisible)
            {
                hideInstanceCheckbox.Checked = false;
                hideInstanceCheckbox.Text = "石器顯示中";
            }
            else
            {
                hideInstanceCheckbox.Checked = true;
                hideInstanceCheckbox.Text = "石器隱藏中";
            }

            UpdatePlayerControls();
            UpdatePlayerInfoControls();
            UpdatePetControls();
            UpdateItemControls();
            UpdateBattleControls();
            UpdateChatControls();

            return;

            void UpdatePlayerControls()
            {
                nameDisplay.Text = data.Player.Name;
                nicknameDisplay.Text = $"暱稱: {data.Player.Nickname}";

                var playerInfo = PlayerJournal.GetPlayerInfo(data.GameClientData.CurrentAccount, data.GameClientData.CurrentCharacter);

                expDisplay.Text = (playerInfo.ExpDifference == 0) ?
                    $"EXP: {FormatLargeNumbers(data.Player.EXP)}" :
                    $"EXP: {FormatLargeNumbers(data.Player.EXP)}  +{FormatLargeNumbers(playerInfo.ExpDifference)}";

                levelDisplay.Text = data.Player.Reincarnation > 0 ? $"LV: {data.Player.Level}  轉生 {data.Player.Reincarnation}" : $"LV: {data.Player.Level}";
                nextDisplay.Text = data.Player.NextEXP != -1 ? $"Next: {FormatLargeNumbers(data.Player.NextEXP)}  尚需: {FormatLargeNumbers(data.Player.NextEXP - data.Player.EXP)}" :
                                                               $"Next: {FormatLargeNumbers(data.Player.NextEXP)}";

                decimal ratio;

                hpDisplay.Text = $"HP: {data.Player.HP} / {data.Player.MaxHP}";
                ratio = (data.Player.MaxHP == 0) ? 100 : data.Player.HP * 1.0m / data.Player.MaxHP * 100;
                if (ratio < MySettings.Default.HPLimit2)
                {
                    hpDisplay.ForeColor = MySettings.Default.HPColor2;
                }
                else if (ratio < MySettings.Default.HPLimit1)
                {
                    hpDisplay.ForeColor = MySettings.Default.HPColor1;
                }
                else
                {
                    hpDisplay.ForeColor = Color.Black;
                }

                mpDisplay.Text = $"MP: {data.Player.MP} / {data.Player.MaxMP}";
                ratio = (data.Player.MaxMP == 0) ? 100 : data.Player.MP * 1.0m / data.Player.MaxMP * 100;
                if (ratio < MySettings.Default.MPLimit2)
                {
                    mpDisplay.ForeColor = MySettings.Default.MPColor2;
                }
                else if (ratio < MySettings.Default.MPLimit1)
                {
                    mpDisplay.ForeColor = MySettings.Default.MPColor1;
                }
                else
                {
                    mpDisplay.ForeColor = Color.Black;
                }

                attackDisplay.Text = $"攻擊力: {data.Player.Attack}{Environment.NewLine}" +
                                     $"防禦力: {data.Player.Armor}{Environment.NewLine}" +
                                     $"敏捷力: {data.Player.Speed}";

                charismaDisplay.Text = $"魅力: {data.Player.Charisma}";
                if (data.Player.Charisma <= MySettings.Default.CharismaLimit2)
                {
                    charismaDisplay.ForeColor = MySettings.Default.CharismaColor2;
                }
                else if (data.Player.Charisma <= MySettings.Default.CharismaLimit1)
                {
                    charismaDisplay.ForeColor = MySettings.Default.CharismaColor1;
                }
                else
                {
                    charismaDisplay.ForeColor = Color.Black;
                }

                earthDisplay.Text = new string('■', (int)data.Player.Earth);
                waterDisplay.Text = new string('■', (int)data.Player.Water);
                fireDisplay.Text = new string('■', (int)data.Player.Fire);
                windDisplay.Text = new string('■', (int)data.Player.Wind);

                vitDisplay.Text = $"體力: {data.Player.VIT}{Environment.NewLine}" +
                                  $"耐力: {data.Player.DEF}";
                strDisplay.Text = $"腕力: {data.Player.STR}{Environment.NewLine}" +
                                  $"速度: {data.Player.DEX}";

                pointsDisplay.Text = $"點數: {data.Player.Points}";
                pointsDisplay.Visible = data.Player.Points != 0;

                moneyDisplay.Text = $"石幣: {data.Player.Money:N0}";
                mapDisplay.Text = $"地圖: {data.Map.Name}{Environment.NewLine}" +
                                  $"編號: {data.Map.MapID}";

                positionDisplay.Text = $"現在座標   東: {data.Map.X}";
                position2Display.Text = $"南: {data.Map.Y}";

                position3Display.Text = $"前次座標   東: {data.Map.LastX}{Environment.NewLine}" +
                                        $"滑鼠座標   東: {data.Map.MouseHoverX}{Environment.NewLine}" +
                                        $"游標位置   X: {data.Map.MousePixelX} Y: {data.Map.MousePixelY}";
                position4Display.Text = $"南: {data.Map.LastY}{Environment.NewLine}" +
                                        $"南: {data.Map.MouseHoverY}";

                StringBuilder sb = new();
                for (int i = 0; i < 5; i++)
                {
                    var teammate = data.Teammates[i];

                    if (teammate.IsPresent)
                    {
                        sb.AppendLine($"隊員{i + 1}: {teammate.Name}");
                        sb.AppendLine($"    LV: {teammate.Level}  HP: {teammate.MaxHP}  MP: {teammate.MP}");
                    }
                    else
                    {
                        sb.AppendLine($"隊員{i + 1}:");
                        sb.AppendLine();
                    }
                }
                teammateDisplay.Text = sb.ToString();
            }

            void UpdatePlayerInfoControls()
            {
                playerInfoTableLayoutPanel.Visible = true;

                var playerInfo = PlayerJournal.GetPlayerInfo(data.GameClientData.CurrentAccount, data.GameClientData.CurrentCharacter);

                playerInfo1Display.Text = $"聲望: {FormatNumber(playerInfo.Fame)}{Environment.NewLine}" +
                                          $"活力: {FormatNumber(playerInfo.Vigour)}{Environment.NewLine}" +
                                          $"銀幣: {FormatNumber(playerInfo.Silver)}{Environment.NewLine}" +
                                          $"會員: {playerInfo.MemberLabel}";
                playerInfo1Display.Enabled = playerInfo.Fame >= 0 ||
                                             playerInfo.Vigour >= 0 ||
                                             playerInfo.Silver >= 0 ||
                                             !String.IsNullOrEmpty(playerInfo.MemberLabel);

                playerInfo2Display.Text = $"氣勢: {FormatNumber(playerInfo.Momentum)}{Environment.NewLine}" +
                                          $"戰鬥: {FormatNumber(playerInfo.CombatPoints)}{Environment.NewLine}" +
                                          $"回饋: {FormatNumber(playerInfo.Appreciation)}{Environment.NewLine}" +
                                          $"{FormatDuration(playerInfo.MemberExpiration)}";
                playerInfo2Display.Enabled = playerInfo.Momentum >= 0 ||
                                             playerInfo.CombatPoints >= 0 ||
                                             playerInfo.Appreciation >= 0 ||
                                             playerInfo.MemberExpiration != DateTime.MinValue;

                adventureInfo1Display.Text = $"冒險等級: {FormatNumber(playerInfo.AdventureLevel)}{Environment.NewLine}" +
                                             $"冒險點數: {FormatNumber(playerInfo.AdventurePoints)}{Environment.NewLine}" +
                                             $"冒險次數: {FormatNumber(playerInfo.AdventureQuests)} {((playerInfo.AdventureQuests >= 0) ? "次" : "")}{Environment.NewLine}" +
                                             $"鐵匠等級: {FormatNumber(playerInfo.SmithLevel)}";
                adventureInfo1Display.Enabled = playerInfo.AdventureLevel >= 0 ||
                                                playerInfo.AdventurePoints >= 0 ||
                                                playerInfo.AdventureQuests >= 0 ||
                                                playerInfo.SmithLevel >= 0;

                adventureInfo2Display.Text = $"{FormatNumber(playerInfo.AdventureExp)} {((playerInfo.AdventureExp >= 0) ? "/" : "")} {FormatNumber(playerInfo.AdventureNextExp)}{Environment.NewLine}" +
                                             $"{Environment.NewLine}" +
                                             $"{FormatDuration(playerInfo.AdventureResetTime)}{Environment.NewLine}" +
                                             $"{FormatNumber(playerInfo.SmithExp)} {((playerInfo.SmithExp >= 0) ? "/" : "")} {FormatNumber(playerInfo.SmithNextExp)}";
                adventureInfo2Display.Enabled = playerInfo.AdventureExp >= 0 ||
                                                playerInfo.AdventureNextExp >= 0 ||
                                                playerInfo.AdventureResetTime != DateTime.MinValue ||
                                                playerInfo.SmithExp >= 0 ||
                                                playerInfo.SmithNextExp >= 0;

                loginDurationDisplay.Text = $"上線時間: {FormatDuration(playerInfo.LastLoginTime, isPast: true, forceShowSeconds: true)}";
                loginDurationDisplay.Enabled = playerInfo.LastLoginTime != DateTime.MinValue;

                expBoostDurationDisplay.Text = $"學習經驗提升: {FormatDuration(playerInfo.ExpBoostExpiration, forceShowSeconds: true)}";
                expBoostDurationDisplay.Enabled = playerInfo.ExpBoostExpiration != DateTime.MinValue;

                static string FormatNumber(int num)
                {
                    if (num < 0)
                        return String.Empty;
                    else
                        return $"{num:n0}";
                }

                static string FormatDuration(DateTime time, bool isPast = false, bool forceShowSeconds = false)
                {
                    if (time == DateTime.MinValue)
                        return String.Empty;
                    else
                    {
                        var duration = isPast ? DateTime.Now - time : time - DateTime.Now;
                        duration = (duration.Ticks >= 0) ? duration : new(0);

                        return (duration.Days > 0) ? $"{duration.Days} 天 {duration.Hours} 時" + (forceShowSeconds ? $" {duration.Seconds} 秒" : String.Empty) :
                               (duration.Hours > 0) ? $"{duration.Hours} 時 {duration.Minutes} 分" + (forceShowSeconds ? $" {duration.Seconds} 秒" : String.Empty) :
                                                        $"{duration.Minutes} 分 {duration.Seconds} 秒";
                    }
                }
            }

            void UpdatePetControls()
            {
                var playerInfo = PlayerJournal.GetPlayerInfo(data.GameClientData.CurrentAccount, data.GameClientData.CurrentCharacter);

                for (int i = 0; i < data.Pets.Count; i++)
                {
                    var pet = data.Pets[i];
                    var display = petDisplays[i];

                    display.PetGroup.Visible = pet.IsPresent;
                    if (!pet.IsPresent)
                        continue;

                    var rates = PetCalculator.CalculateGrowthRate(pet, MySettings.Default.UseLinearRegression);

                    if (display.PetGroup.Text != pet.Name)
                    {
                        display.PetGroup.Text = pet.Name;
                    }

                    display.Nickname.Text = pet.Nickname;

                    //status textbox
                    if (data.Player.RidePetPos == i)
                    {
                        display.Status.Text = "騎";
                    }
                    else if (data.Player.BattlePetPos == i)
                    {
                        display.Status.Text = "戰";
                    }
                    else if (data.Player.MailPetPos == i)
                    {
                        display.Status.Text = "郵";
                    }
                    else if (data.Player.IsPetReady[i])
                    {
                        display.Status.Text = "等";
                    }
                    else
                    {
                        display.Status.Text = "休";
                    }

                    int diff = PetJournal.GetExpDifference(pet);
                    display.EXP.Text = (diff == 0) ?
                        $"EXP: {FormatLargeNumbers(pet.EXP)}" :
                        $"EXP: {FormatLargeNumbers(pet.EXP)}  +{FormatLargeNumbers(diff)}";

                    display.Level.Text = pet.Reincarnation > 0 ? $"LV: {pet.Level}  轉生 {pet.Reincarnation}" : $"LV: {pet.Level}";
                    display.Next.Text = pet.NextEXP != -1 ? $"Next: {FormatLargeNumbers(pet.NextEXP)}  尚需: {FormatLargeNumbers(pet.NextEXP - pet.EXP)}" :
                                                            $"Next: {FormatLargeNumbers(pet.NextEXP)}";

                    display.HP.Text = $"HP: {pet.HP} / {pet.MaxHP}";
                    decimal ratio = (pet.MaxHP == 0) ? 100 : pet.HP * 1.0m / pet.MaxHP * 100;
                    if (ratio < MySettings.Default.HPLimit2)
                    {
                        display.HP.ForeColor = MySettings.Default.HPColor2;
                    }
                    else if (ratio < MySettings.Default.HPLimit1)
                    {
                        display.HP.ForeColor = MySettings.Default.HPColor1;
                    }
                    else
                    {
                        display.HP.ForeColor = Color.Black;
                    }

                    display.Attack.Text = $"攻擊: {pet.Attack}{Environment.NewLine}" +
                                          $"防禦: {pet.Armor}{Environment.NewLine}" +
                                          $"敏捷: {pet.Speed}";

                    display.Loyalty.Text = $"忠誠: {pet.Loyalty}";
                    if (pet.Loyalty <= MySettings.Default.LoyaltyLimit2)
                    {
                        display.Loyalty.ForeColor = MySettings.Default.LoyaltyColor2;
                    }
                    else if (pet.Loyalty <= MySettings.Default.LoyaltyLimit1)
                    {
                        display.Loyalty.ForeColor = MySettings.Default.LoyaltyColor1;
                    }
                    else
                    {
                        display.Loyalty.ForeColor = Color.Black;
                    }

                    display.ID.Text = $"ID: {pet.PetID:X}";

                    //attributes
                    bool assignedOne = false;
                    display.Attribute2.Visible = false;
                    if (pet.Earth > 0)
                    {
                        display.Attribute1.ForeColor = Color.Green;
                        display.Attribute1.Text = $"地{pet.Earth}";
                        assignedOne = true;
                    }
                    if (pet.Water > 0)
                    {
                        if (!assignedOne)
                        {
                            display.Attribute1.ForeColor = Color.Blue;
                            display.Attribute1.Text = $"水{pet.Water}";
                            assignedOne = true;
                        }
                        else
                        {
                            display.Attribute2.ForeColor = Color.Blue;
                            display.Attribute2.Text = $"水{pet.Water}";
                            display.Attribute2.Visible = true;
                        }
                    }
                    if (pet.Fire > 0)
                    {
                        if (!assignedOne)
                        {
                            display.Attribute1.ForeColor = Color.Red;
                            display.Attribute1.Text = $"火{pet.Fire}";
                            assignedOne = true;
                        }
                        else
                        {
                            display.Attribute2.ForeColor = Color.Red;
                            display.Attribute2.Text = $"火{pet.Fire}";
                            display.Attribute2.Visible = true;
                        }
                    }
                    if (pet.Wind > 0)
                    {
                        if (!assignedOne)
                        {
                            display.Attribute1.ForeColor = Color.Gold;
                            display.Attribute1.Text = $"風{pet.Wind}";
                            assignedOne = true;
                        }
                        else
                        {
                            display.Attribute2.ForeColor = Color.Gold;
                            display.Attribute2.Text = $"風{pet.Wind}";
                            display.Attribute2.Visible = true;
                        }
                    }
                    else if (!assignedOne)
                    {
                        display.Attribute1.ForeColor = Color.Gray;
                        display.Attribute1.Text = "無";
                    }

                    //rates
                    if (rates.Count() == 0)
                    {
                        display.HPRate.Visible = false;
                        display.AttackRate.Visible = false;
                        display.TotalRate.Visible = false;
                    }
                    else
                    {
                        display.HPRate.Visible = true;
                        display.AttackRate.Visible = true;
                        display.TotalRate.Visible = true;
                        display.HPRate.Text = rates[0].ToString("0.###");
                        display.AttackRate.Text = $"{rates[1]:0.###}{Environment.NewLine}{rates[2]:0.###}{Environment.NewLine}{rates[3]:0.###}";
                        display.TotalRate.Text = rates[4].ToString("0.###");
                    }
                }
            }

            void UpdateItemControls()
            {
                UpdateEquipmentControl(leftAccessoryDisplay, data.LeftAccessory);
                UpdateEquipmentControl(helmetDisplay, data.Helmet);
                UpdateEquipmentControl(rightAccessoryDisplay, data.RightAccessory);
                UpdateEquipmentControl(weaponDisplay, data.Weapon);
                UpdateEquipmentControl(armorDisplay, data.Armor);
                UpdateEquipmentControl(shieldDisplay, data.Shield);
                UpdateEquipmentControl(glovesDisplay, data.Gloves);
                UpdateEquipmentControl(beltDisplay, data.Belt);
                UpdateEquipmentControl(bootsDisplay, data.Boots);

                int row = 0;
                int columnGroup = 0;
                for (int i = 0; i < 15; i++)
                {
                    var item = data.Items[i];

                    itemGridView.Rows[row].Cells[itemGridViewColumns[columnGroup]].Value = item.IsPresent ? item.Stack.ToString() : String.Empty;
                    itemGridView.Rows[row].Cells[itemGridViewColumns[columnGroup + 1]].Value = item.IsPresent ? item.Name : String.Empty;

                    if (row == 4)
                    {
                        row = 0;
                        columnGroup += 2;
                    }
                    else
                    {
                        row++;
                    }
                }

                static void UpdateEquipmentControl(Label display, Item equipment)
                {
                    if (!equipment.IsPresent)
                    {
                        display.BackColor = Color.FromKnownColor(KnownColor.Control);
                        display.Text = String.Empty;
                        display.Tag = String.Empty;
                    }
                    else if (equipment.IsFragile)
                    {
                        if (equipment.Durability <= MySettings.Default.DurabilityLimit2)
                        {
                            display.BackColor = MySettings.Default.DurabilityColor2;
                        }
                        else if (equipment.Durability <= MySettings.Default.DurabilityLimit1)
                        {
                            display.BackColor = MySettings.Default.DurabilityColor1;
                        }
                        else
                        {
                            display.BackColor = Color.FromKnownColor(KnownColor.Control);
                        }

                        display.Text = $"{equipment.Name}{Environment.NewLine}耐: {equipment.Durability}%";
                        display.Tag = $"[ 耐: {equipment.Durability}%]  {equipment.Description}";
                    }
                    else
                    {
                        display.BackColor = Color.FromKnownColor(KnownColor.Control);
                        display.Text = $"{equipment.Name}";
                        display.Tag = $"[不會損壞]  {equipment.Description}";
                    }
                }
            }

            void UpdateBattleControls()
            {
                var playerInfo = PlayerJournal.GetPlayerInfo(data.GameClientData.CurrentAccount, data.GameClientData.CurrentCharacter);
                encountDisplay.Text = $"第 {playerInfo.Encounter} 局{Environment.NewLine}第 {playerInfo.Turn} 回合";

                for (int i = 0; i < 5; i++)
                {
                    var unit = data.Allies[i];

                    if (String.IsNullOrEmpty(unit.Name))
                    {
                        allyDisplays[i].Text = String.Empty;
                    }
                    else if (unit.HasRide)
                    {
                        allyDisplays[i].Text = $"{unit.Name}  LV: {unit.Level} ({unit.HP} / {unit.MaxHP}){Environment.NewLine}{unit.RideName}  LV: {unit.RideLevel} ({unit.RideHP} / {unit.RideMaxHP})";
                    }
                    else
                    {
                        allyDisplays[i].Text = $"{unit.Name}  LV: {unit.Level} ({unit.HP} / {unit.MaxHP})";
                    }

                    var pet = data.AllyPets[i];

                    if (String.IsNullOrEmpty(pet.Name))
                    {
                        allyPetDisplays[i].Text = String.Empty;
                    }
                    else
                    {
                        allyPetDisplays[i].Text = $"{pet.Name}{Environment.NewLine}LV: {pet.Level} ({pet.HP} / {pet.MaxHP})";
                    }
                }

                for (int i = 0; i < 10; i++)
                {
                    if (i >= data.Enemies.Count)
                    {
                        enemyDisplays[i].Text = String.Empty;
                        continue;
                    }

                    var enemy = data.Enemies[i];

                    if (String.IsNullOrEmpty(enemy.Name))
                    {
                        enemyDisplays[i].Text = String.Empty;
                    }
                    else if (enemy.HasRide)
                    {
                        enemyDisplays[i].Text = $"{enemy.Name} {enemy.Level} ({enemy.HP}／{enemy.MaxHP}){Environment.NewLine}{enemy.RideLevel} ({enemy.RideHP}／{enemy.RideMaxHP})";
                    }
                    else
                    {
                        enemyDisplays[i].Text = $"{enemy.Name}{Environment.NewLine}LV: {enemy.Level} ({enemy.HP} / {enemy.MaxHP})";
                    }
                }
            }

            void UpdateChatControls()
            {
                if (!textColorCombobox.DroppedDown)
                {
                    textColorCombobox.SelectedIndex = data.GameClientData.InputColor;
                }
            }
        }

        private void ShowTooltipAtMouse(string message, int duration = 8000)
            => toolTip1.Show(message, this, PointToClient(new Point(Cursor.Position.X + 15, Cursor.Position.Y + 45)), duration);

        private static bool TryClipboardSetText(string text, TextDataFormat format = TextDataFormat.Text)
        {
            try
            {
                Clipboard.SetText(text, format);

                return true;
            }
            catch (Exception ex) when (ex is ExternalException || ex is ArgumentNullException)
            {
                return false;
            }
        }

        private static string FormatLargeNumbers(int num)
        {
            if (num > 1000000)
            {
                Decimal newNum = num;
                newNum /= 1000000;

                return newNum.ToString("0.# m");
            }
            else if (num > 1000)
            {
                Decimal newNum = num;
                newNum /= 1000;

                return newNum.ToString("0.# k");
            }
            else
            {
                return num.ToString();
            }
        }

    }
}