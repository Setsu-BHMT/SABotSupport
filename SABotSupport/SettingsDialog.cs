using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

using SABotSupport.ASSAStructures;

namespace SABotSupport
{
    public partial class SettingsDialog : Form
    {
        private readonly Dictionary<string, string> CopyFormatToLabelDictionary = new()
        {
            { "Tab", "TAB" },
            { "Space", "空格" },
            { "Bar", "|" },
            { "Name", "名稱" },
            { "Nickname", "暱稱" },
            { "Level", "等級" },
            { "Reincarnation", "轉生" },
            { "HP", "HP" },
            { "Attack", "攻擊" },
            { "Armor", "防禦" },
            { "Speed", "敏捷" },
            { "Lv1HP", "一級HP" },
            { "Lv1Attack", "一級攻擊" },
            { "Lv1Armor", "一級防禦" },
            { "Lv1Speed", "一級敏捷" },
            { "Vitality", "體力" },
            { "Strength", "腕力" },
            { "Defense", "耐力" },
            { "Dexterity", "速度" },
            { "HPRate", "血成長率" },
            { "AttackRate", "攻成長率" },
            { "ArmorRate", "防成長率" },
            { "SpeedRate", "敏成長率" },
            { "CombinedRate", "攻防敏成長率" },
            { "PredictLevel", "預測等級" },
            { "PredictHP", "預測HP" },
            { "PredictAttack", "預測攻擊" },
            { "PredictArmor", "預測防禦" },
            { "PredictSpeed", "預測敏捷" },
            { "PredictVitality", "預測體力" },
            { "PredictStrength", "預測腕力" },
            { "PredictDefense", "預測耐力" },
            { "PredictDexterity", "預測速度" },
            { "BoostLevel", "激素等級" },
            { "BoostHP", "激素HP" },
            { "BoostAttack", "激素攻擊" },
            { "BoostArmor", "激素防禦" },
            { "BoostSpeed", "激素敏捷" },
        };

        private bool copyFormatHasChanged = false;
        private volatile bool isFormResetting = false;

        private static bool isTraceEnabled = false;

        public SettingsDialog()
        { 
            InitializeComponent();

            //enlarge the form if necessary, if DPI setting is small
            if (copyFormatGroupbox.Location.X + copyFormatGroupbox.Width > this.Width)
            {
                this.Width += 100;
            }

            //databind map list combobox
            var bs = new BindingSource
            {
                DataSource = Properties.Settings.Default.MapNameList
            };
            bs.ListChanged += delegate (object sender, ListChangedEventArgs e) {
                SettingsChanged(null, null);
            };
            mapListCombobox.DataSource = bs;
            
            //sync control enabled status items
            DisableMapListCheckbox_CheckedChanged(null, null);
            ForceBoostKingPetCheckbox_CheckedChanged(null, null);

            //setup property changed event
            Properties.Settings.Default.PropertyChanged += SettingsChanged;

            //initialize the table layout panel
            LoadCopyFormatFromSettings();

            traceButton.Enabled = !isTraceEnabled;
        }

        private void SettingsChanged(object sender, PropertyChangedEventArgs e)
            => okButton.Enabled = applyButton.Enabled = true & !isFormResetting;

        private void LoadCopyFormatFromSettings()
        {
            FlowLayoutPanel flp = GenerateNewFlowLayoutPanel();
            copyFormatTableLayoutPanel.Controls.Add(flp, 0, 0);

            foreach (string identifier in Properties.Settings.Default.PetStatCopyFormat)
            {
                if (identifier.StartsWith("*"))
                {   //custom string
                    AddLabelToFlowLayoutPanel(flp, identifier, "Custom");
                }
                else if (identifier == "Newline")
                {   //new line
                    copyFormatTableLayoutPanel.RowCount += 1;
                    var row = copyFormatTableLayoutPanel.RowCount - 2;
                    flp = GenerateNewFlowLayoutPanel();
                    copyFormatTableLayoutPanel.Controls.Add(flp, 0, row);
                    copyFormatTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, copyFormatTableLayoutPanel.RowStyles[0].Height));
                }
                else if (CopyFormatToLabelDictionary.ContainsKey(identifier))
                {
                    AddLabelToFlowLayoutPanel(flp, CopyFormatToLabelDictionary[identifier], identifier);
                }
                else
                {
                    MessageBox.Show(this, $"Failed to parse format identifier: {identifier}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    this.Close();
                }
            }
        }

        private void SettingsDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.PropertyChanged -= SettingsChanged;
            Properties.Settings.Default.Reload();
        }

        private void ColorLabel_Click(object sender, EventArgs e)
        {
            var label = sender as Label;
            colorDialog1.Color = label.BackColor;

            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                label.BackColor = colorDialog1.Color;
            }
        }

        private void MapListCombobox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && 
                !String.IsNullOrWhiteSpace(mapListCombobox.Text) &&
                !mapListCombobox.Items.Contains(mapListCombobox.Text))
            {
                var bs = mapListCombobox.DataSource as BindingSource;
                bs.Add(mapListCombobox.Text);
                mapListCombobox.SelectedIndex = -1;

                e.SuppressKeyPress = true;
            }
        }

        private void DeleteMapEntryButton_Click(object sender, EventArgs e)
        {
            var indexesToRemove = new List<int>();
            var bs = mapListCombobox.DataSource as BindingSource;

            for (int i = mapListCombobox.Items.Count - 1; i >= 0; i--)
            {
                if ((string)mapListCombobox.Items[i] == mapListCombobox.Text)
                {
                    indexesToRemove.Add(i);
                }
            }

            foreach (int index in indexesToRemove)
            {
                bs.RemoveAt(index);
            }

            mapListCombobox.SelectedIndex = -1;
        }

        private void DisableMapListCheckbox_CheckedChanged(object sender, EventArgs e)
            => mapListCombobox.Enabled = whitelistRadioButton.Enabled = blacklistRadioButton.Enabled = deleteMapEntryButton.Enabled = !disableMapListCheckbox.Checked;

        private void BoostLevelNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (boostLevelNumericUpDown.Value % 5 != 0)
            {
                boostLevelNumericUpDown.Value -= boostLevelNumericUpDown.Value % 5;
            }
        }

        private void ForceBoostKingPetCheckbox_CheckedChanged(object sender, EventArgs e)
            => autoDetermineBoostKingPet.Enabled = !forceBoostKingPetCheckbox.Checked;

        private void AddLineButton_Click(object sender, EventArgs e)
        {
            copyFormatTableLayoutPanel.SuspendLayout();
            copyFormatTableLayoutPanel.RowCount += 1;
            copyFormatTableLayoutPanel.Controls.Add(GenerateNewFlowLayoutPanel(), 0, copyFormatTableLayoutPanel.RowCount - 2);
            copyFormatTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, copyFormatTableLayoutPanel.RowStyles[0].Height));
            copyFormatTableLayoutPanel.ResumeLayout(true);
        }

        private FlowLayoutPanel GenerateNewFlowLayoutPanel()
        {
            var flp = new FlowLayoutPanel{
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                //BorderStyle = BorderStyle.FixedSingle,  //debug use
                Margin = new Padding(),
                AllowDrop = true,
                Dock = DockStyle.Fill
            };

            flp.DragOver += delegate (Object sender, DragEventArgs e) {
                e.Effect = DragDropEffects.Copy;
            };
            flp.DragDrop += FlowLayoutPanel_DragDrop;

            return flp;
        }

        private void DeleteLineButton_Click(object sender, EventArgs e)
        {
            if (copyFormatTableLayoutPanel.RowCount == 2)
                return;

            copyFormatTableLayoutPanel.SuspendLayout();

            var flp = copyFormatTableLayoutPanel.GetControlFromPosition(0, copyFormatTableLayoutPanel.RowCount - 2);

            foreach (Control control in flp.Controls)
            {
                control.Dispose();
            }

            copyFormatTableLayoutPanel.RowCount -= 1;
            copyFormatTableLayoutPanel.RowStyles.RemoveAt(copyFormatTableLayoutPanel.RowStyles.Count - 1);
            flp.Dispose();

            copyFormatTableLayoutPanel.ResumeLayout(true);

            copyFormatHasChanged = true;
            SettingsChanged(null, null);
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            var pet = new Pet {
                PetID = 0,
                HP = 2000,
                MaxHP = 2000,
                EXP = 0,
                NextEXP = -1,
                Level = 100,
                Attack = 500,
                Armor = 400,
                Speed = 300,
                Loyalty = 100,
                Earth = 5,
                Water = 0,
                Fire = 0,
                Wind = 5,
                Reincarnation = 2,
                Name = "烏力烏力",
                Nickname = "20.12.11.10",
                IsPresent = true,
            };

            var format = new StringCollection();
            format.AddRange(ConvertCopyFormatLabelstoFormatString());
            toolTip1.Show(PetCalculator.FormatPetStats(pet, format), previewButton, 5000);
        }

        private string[] ConvertCopyFormatLabelstoFormatString()
        {
            var reverseDictionary = CopyFormatToLabelDictionary.ToDictionary(x => x.Value, x => x.Key);
            var newFormat = new List<string>();

            //convert label controls into format string list
            for (int row = 0; row < copyFormatTableLayoutPanel.RowCount - 1; row++)
            {
                var flp = copyFormatTableLayoutPanel.GetControlFromPosition(0, row) as FlowLayoutPanel;

                foreach (Label label in flp.Controls)
                {
                    //special case custom strings, otherwise lookup in reverse dictionary
                    newFormat.Add(label.Text.StartsWith("*") ? label.Text : reverseDictionary[label.Text]);
                }

                if (row < copyFormatTableLayoutPanel.RowCount - 2)
                {
                    newFormat.Add("Newline");
                }
                else if (flp.Controls.Count == 0)
                {
                    //remove trailing newline
                    newFormat.RemoveAt(newFormat.Count - 1);
                }
            }

            return newFormat.ToArray();
        }

        #region Drag Drop Methods For Pet Stat Copy Format

        private Point startCursorPosition;

        void Control_MouseDown(Object sender, MouseEventArgs e)
        {
            Control me = sender as Control;
            if (String.IsNullOrEmpty(me.Text))
                return;

            //synchronous call
            startCursorPosition = MousePosition;
            var result = me.DoDragDrop(sender, DragDropEffects.All);
            if (result != DragDropEffects.None || MousePosition == startCursorPosition)
                return;

            //check if we need to remove this control from panel
            FlowLayoutPanel flp = me.Parent as FlowLayoutPanel;
            if (flp != default)
            {
                flp.Controls.Remove(me);
                me.Dispose();

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
        }

        void Label_DragOver(Object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;

            //find the target panel and figure out the target index
            Label label = sender as Label;
            FlowLayoutPanel flp = label.Parent as FlowLayoutPanel;
            int index = flp.Controls.GetChildIndex(sender as Label);

            //check if we can move the control
            Label draggedLabel = e.Data.GetData(typeof(Label)) as Label;
            if (draggedLabel?.Parent == label.Parent)
            {   //move
                flp.Controls.SetChildIndex(draggedLabel, index);

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
        }

        void Label_DragDrop(Object sender, DragEventArgs e)
        {
            Label label = sender as Label;
            Label droppedLabel = e.Data.GetData(typeof(Label)) as Label;

            //check if we need to add it to the panel
            if (droppedLabel != default && droppedLabel.Parent != label.Parent)
            {
                AddLabelToFlowLayoutPanel(label.Parent as FlowLayoutPanel, droppedLabel);

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
            else if (droppedLabel == default)
            {
                //handle the case of the custom label
                TextBox textbox = e.Data.GetData(typeof(TextBox)) as TextBox;
                AddLabelToFlowLayoutPanel(label.Parent as FlowLayoutPanel, textbox);

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
        }

        void FlowLayoutPanel_DragDrop(Object sender, DragEventArgs e)
        {
            FlowLayoutPanel flp = sender as FlowLayoutPanel;
            Label droppedLabel = e.Data.GetData(typeof(Label)) as Label;

            //check if we need to add it to our collection
            if (droppedLabel != default && droppedLabel.Parent != flp)
            {
                AddLabelToFlowLayoutPanel(flp, droppedLabel);

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
            else if (droppedLabel == default)
            {
                //handle the case of the custom label
                TextBox textbox = e.Data.GetData(typeof(TextBox)) as TextBox;
                AddLabelToFlowLayoutPanel(flp, textbox);

                copyFormatHasChanged = true;
                SettingsChanged(null, null);
            }
        }

        private void AddLabelToFlowLayoutPanel(FlowLayoutPanel flp, Control cloneTarget)
            => AddLabelToFlowLayoutPanel(flp, cloneTarget.GetType() == typeof(Label) ? cloneTarget.Text : $"*{cloneTarget.Text}", cloneTarget.Tag);
        private void AddLabelToFlowLayoutPanel(FlowLayoutPanel flp, string text, object tag)
        {
            Label newLabel = new()
            {
                AutoSize = true,
                MinimumSize = new Size(0, 18),
                BorderStyle = BorderStyle.FixedSingle,
                Text = text,
                Tag = tag,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(1, 1, 1, 0),
                AllowDrop = true
            };

            newLabel.MouseDown += Control_MouseDown;
            newLabel.DragOver += Label_DragOver;
            newLabel.DragDrop += Label_DragDrop;

            flp.Controls.Add(newLabel);
        }

        #endregion

        private void DefaultButton_Click(object sender, EventArgs e)
        {
            //confirm un-cancelable action
            if (MessageBox.Show(this, "你確定要全數還原為預設值嗎? (無法取消)", "還原預設值", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            okButton.Enabled = applyButton.Enabled = false;
            isFormResetting = true;
            Properties.Settings.Default.Reset();

            //rebind map list combobox
            var bs = new BindingSource
            {
                DataSource = Properties.Settings.Default.MapNameList
            };
            bs.ListChanged += delegate (object s, ListChangedEventArgs ee) {
                SettingsChanged(null, null);
            };
            mapListCombobox.DataSource = bs;
            mapListCombobox.Text = String.Empty;
            mapListCombobox.SelectedIndex = -1;

            //re-initialize the table layout panel
            copyFormatTableLayoutPanel.Hide();
            while (copyFormatTableLayoutPanel.RowCount > 2)
            {
                deleteLineButton.PerformClick();
            }
            var flp = copyFormatTableLayoutPanel.GetControlFromPosition(0, 0);
            copyFormatTableLayoutPanel.Controls.Remove(flp);
            foreach (Control control in flp.Controls)
            {
                control.Dispose();
            }
            flp.Dispose();
            LoadCopyFormatFromSettings();
            copyFormatTableLayoutPanel.Show();
            copyFormatHasChanged = false;

            isFormResetting = false;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SaveCopyFormatToSettings();
            Properties.Settings.Default.Save();

            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();

            this.Close();
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            SaveCopyFormatToSettings();
            Properties.Settings.Default.Save();

            applyButton.Enabled = false;
        }

        private void SaveCopyFormatToSettings()
        {
            if (!copyFormatHasChanged)
                return;

            Properties.Settings.Default.PetStatCopyFormat.Clear();
            Properties.Settings.Default.PetStatCopyFormat.AddRange(ConvertCopyFormatLabelstoFormatString());

            copyFormatHasChanged = false;
        }

        private void TraceButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否開啟除錯紀錄?", "Debug", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                Trace.Listeners.Add(new TextWriterTraceListener("Debug.log"));
                Trace.AutoFlush = true;
                Trace.IndentLevel = 0;
                Trace.WriteLine("=======================================================");
                Trace.WriteLine(DateTime.Now);

                isTraceEnabled = true;
                traceButton.Enabled = false;

                MessageBox.Show("除錯紀錄將儲存至 Debug.log", "Debug");
            }
        }
    }
}
