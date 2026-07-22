
namespace SABotSupport
{
    partial class AutoLogonForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.closeButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.assaPathsLabel = new System.Windows.Forms.Label();
            this.accountsLabel = new System.Windows.Forms.Label();
            this.accountsDataGridView = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.enableAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.enableSelectedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disableSelectedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.assaPathsDataGridView = new System.Windows.Forms.DataGridView();
            this.PathEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Path = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.saveAccountPasswordsCheckbox = new System.Windows.Forms.CheckBox();
            this.startButton = new System.Windows.Forms.Button();
            this.warningLabel = new System.Windows.Forms.Label();
            this.addPathButton = new System.Windows.Forms.Button();
            this.configureServerINIButton = new System.Windows.Forms.Button();
            this.autoHideOnLoginCheckbox = new System.Windows.Forms.CheckBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.AccountEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Account = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Password = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Server = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Character = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accountsDataGridView)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.assaPathsDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(690, 410);
            this.closeButton.Margin = new System.Windows.Forms.Padding(10);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(100, 30);
            this.closeButton.TabIndex = 0;
            this.closeButton.Text = "關閉";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 6;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.assaPathsLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.closeButton, 5, 7);
            this.tableLayoutPanel1.Controls.Add(this.accountsLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.accountsDataGridView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.assaPathsDataGridView, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.saveAccountPasswordsCheckbox, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.startButton, 5, 2);
            this.tableLayoutPanel1.Controls.Add(this.warningLabel, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.addPathButton, 5, 5);
            this.tableLayoutPanel1.Controls.Add(this.configureServerINIButton, 5, 6);
            this.tableLayoutPanel1.Controls.Add(this.autoHideOnLoginCheckbox, 0, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // assaPathsLabel
            // 
            this.assaPathsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.assaPathsLabel.AutoSize = true;
            this.assaPathsLabel.Location = new System.Drawing.Point(5, 264);
            this.assaPathsLabel.Margin = new System.Windows.Forms.Padding(5, 5, 3, 0);
            this.assaPathsLabel.Name = "assaPathsLabel";
            this.assaPathsLabel.Size = new System.Drawing.Size(140, 17);
            this.assaPathsLabel.TabIndex = 3;
            this.assaPathsLabel.Text = "要使用的 ASSA 外掛:";
            // 
            // accountsLabel
            // 
            this.accountsLabel.AutoSize = true;
            this.accountsLabel.Location = new System.Drawing.Point(5, 5);
            this.accountsLabel.Margin = new System.Windows.Forms.Padding(5, 5, 3, 0);
            this.accountsLabel.Name = "accountsLabel";
            this.accountsLabel.Size = new System.Drawing.Size(96, 17);
            this.accountsLabel.TabIndex = 1;
            this.accountsLabel.Text = "要登入的帳號:";
            // 
            // accountsDataGridView
            // 
            this.accountsDataGridView.AllowUserToResizeRows = false;
            this.accountsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.accountsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.AccountEnabled,
            this.Account,
            this.Password,
            this.Server,
            this.Character});
            this.tableLayoutPanel1.SetColumnSpan(this.accountsDataGridView, 6);
            this.accountsDataGridView.ContextMenuStrip = this.contextMenuStrip1;
            this.accountsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.accountsDataGridView.Location = new System.Drawing.Point(5, 27);
            this.accountsDataGridView.Margin = new System.Windows.Forms.Padding(5);
            this.accountsDataGridView.Name = "accountsDataGridView";
            this.accountsDataGridView.RowHeadersWidth = 30;
            this.accountsDataGridView.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.accountsDataGridView.RowTemplate.Height = 24;
            this.accountsDataGridView.Size = new System.Drawing.Size(790, 167);
            this.accountsDataGridView.TabIndex = 2;
            this.accountsDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.AccountsDataGridView_CellClick);
            this.accountsDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.AccountsDataGridView_CellDoubleClick);
            this.accountsDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.AccountsDataGridView_CellFormatting);
            this.accountsDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.AccountsDataGridView_CellValueChanged);
            this.accountsDataGridView.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.AccountsDataGridView_DataError);
            this.accountsDataGridView.DefaultValuesNeeded += new System.Windows.Forms.DataGridViewRowEventHandler(this.AccountsDataGridView_DefaultValuesNeeded);
            this.accountsDataGridView.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.AccountsDataGridView_EditingControlShowing);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableAllMenuItem,
            this.disableAllMenuItem,
            this.enableSelectedMenuItem,
            this.disableSelectedMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(191, 100);
            // 
            // enableAllMenuItem
            // 
            this.enableAllMenuItem.Name = "enableAllMenuItem";
            this.enableAllMenuItem.Size = new System.Drawing.Size(190, 24);
            this.enableAllMenuItem.Text = "啟用全部";
            this.enableAllMenuItem.Click += new System.EventHandler(this.EnableAllMenuItem_Click);
            // 
            // disableAllMenuItem
            // 
            this.disableAllMenuItem.Name = "disableAllMenuItem";
            this.disableAllMenuItem.Size = new System.Drawing.Size(190, 24);
            this.disableAllMenuItem.Text = "停用全部";
            this.disableAllMenuItem.Click += new System.EventHandler(this.DisableAllMenuItem_Click);
            // 
            // enableSelectedMenuItem
            // 
            this.enableSelectedMenuItem.Name = "enableSelectedMenuItem";
            this.enableSelectedMenuItem.Size = new System.Drawing.Size(190, 24);
            this.enableSelectedMenuItem.Text = "啟用選取的項目";
            this.enableSelectedMenuItem.Click += new System.EventHandler(this.EnableSelectedMenuItem_Click);
            // 
            // disableSelectedMenuItem
            // 
            this.disableSelectedMenuItem.Name = "disableSelectedMenuItem";
            this.disableSelectedMenuItem.Size = new System.Drawing.Size(190, 24);
            this.disableSelectedMenuItem.Text = "停用選取的項目";
            this.disableSelectedMenuItem.Click += new System.EventHandler(this.DisableSelectedMenuItem_Click);
            // 
            // assaPathsDataGridView
            // 
            this.assaPathsDataGridView.AllowUserToAddRows = false;
            this.assaPathsDataGridView.AllowUserToResizeRows = false;
            this.assaPathsDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.assaPathsDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.PathEnabled,
            this.Path});
            this.tableLayoutPanel1.SetColumnSpan(this.assaPathsDataGridView, 5);
            this.assaPathsDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.assaPathsDataGridView.Location = new System.Drawing.Point(5, 286);
            this.assaPathsDataGridView.Margin = new System.Windows.Forms.Padding(5);
            this.assaPathsDataGridView.Name = "assaPathsDataGridView";
            this.assaPathsDataGridView.ReadOnly = true;
            this.assaPathsDataGridView.RowHeadersVisible = false;
            this.assaPathsDataGridView.RowHeadersWidth = 51;
            this.tableLayoutPanel1.SetRowSpan(this.assaPathsDataGridView, 3);
            this.assaPathsDataGridView.RowTemplate.Height = 24;
            this.assaPathsDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.assaPathsDataGridView.Size = new System.Drawing.Size(670, 159);
            this.assaPathsDataGridView.TabIndex = 4;
            this.assaPathsDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.AssaPathsDataGridView_CellClick);
            this.assaPathsDataGridView.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.AssaPathsDataGridView_UserDeletedRow);
            // 
            // PathEnabled
            // 
            this.PathEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.PathEnabled.HeaderText = "";
            this.PathEnabled.MinimumWidth = 6;
            this.PathEnabled.Name = "PathEnabled";
            this.PathEnabled.ReadOnly = true;
            this.PathEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.PathEnabled.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.PathEnabled.Width = 23;
            // 
            // Path
            // 
            this.Path.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Path.HeaderText = "路徑";
            this.Path.MinimumWidth = 6;
            this.Path.Name = "Path";
            this.Path.ReadOnly = true;
            // 
            // saveAccountPasswordsCheckbox
            // 
            this.saveAccountPasswordsCheckbox.AutoSize = true;
            this.saveAccountPasswordsCheckbox.Checked = global::SABotSupport.Properties.Settings.Default.SaveAccountPasswords;
            this.saveAccountPasswordsCheckbox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::SABotSupport.Properties.Settings.Default, "SaveAccountPasswords", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.saveAccountPasswordsCheckbox.Location = new System.Drawing.Point(5, 202);
            this.saveAccountPasswordsCheckbox.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.saveAccountPasswordsCheckbox.Name = "saveAccountPasswordsCheckbox";
            this.saveAccountPasswordsCheckbox.Size = new System.Drawing.Size(114, 21);
            this.saveAccountPasswordsCheckbox.TabIndex = 7;
            this.saveAccountPasswordsCheckbox.Text = "記錄帳號密碼";
            this.toolTip1.SetToolTip(this.saveAccountPasswordsCheckbox, "若不選擇則每次重開都會清空");
            this.saveAccountPasswordsCheckbox.UseVisualStyleBackColor = true;
            // 
            // startButton
            // 
            this.startButton.Enabled = false;
            this.startButton.Location = new System.Drawing.Point(685, 204);
            this.startButton.Margin = new System.Windows.Forms.Padding(5);
            this.startButton.Name = "startButton";
            this.tableLayoutPanel1.SetRowSpan(this.startButton, 2);
            this.startButton.Size = new System.Drawing.Size(100, 50);
            this.startButton.TabIndex = 8;
            this.startButton.Text = "開始登陸";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // warningLabel
            // 
            this.warningLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.warningLabel.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.warningLabel, 4);
            this.warningLabel.Font = new System.Drawing.Font("Microsoft YaHei", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.warningLabel.ForeColor = System.Drawing.Color.Crimson;
            this.warningLabel.Location = new System.Drawing.Point(541, 204);
            this.warningLabel.Margin = new System.Windows.Forms.Padding(5);
            this.warningLabel.Name = "warningLabel";
            this.tableLayoutPanel1.SetRowSpan(this.warningLabel, 2);
            this.warningLabel.Size = new System.Drawing.Size(134, 23);
            this.warningLabel.TabIndex = 9;
            this.warningLabel.Text = "錯誤訊息顯示處!";
            this.warningLabel.Visible = false;
            // 
            // addPathButton
            // 
            this.addPathButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addPathButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.addPathButton.Location = new System.Drawing.Point(680, 286);
            this.addPathButton.Margin = new System.Windows.Forms.Padding(0, 5, 5, 5);
            this.addPathButton.Name = "addPathButton";
            this.addPathButton.Size = new System.Drawing.Size(90, 30);
            this.addPathButton.TabIndex = 5;
            this.addPathButton.Text = "新增路徑";
            this.addPathButton.UseVisualStyleBackColor = true;
            this.addPathButton.Click += new System.EventHandler(this.AddPathButton_Click);
            // 
            // configureServerINIButton
            // 
            this.configureServerINIButton.Location = new System.Drawing.Point(680, 326);
            this.configureServerINIButton.Margin = new System.Windows.Forms.Padding(0, 5, 5, 5);
            this.configureServerINIButton.Name = "configureServerINIButton";
            this.configureServerINIButton.Size = new System.Drawing.Size(90, 30);
            this.configureServerINIButton.TabIndex = 10;
            this.configureServerINIButton.Text = "設定分流";
            this.configureServerINIButton.UseVisualStyleBackColor = true;
            this.configureServerINIButton.Click += new System.EventHandler(this.ConfigureServerINIButton_Click);
            // 
            // autoHideOnLoginCheckbox
            // 
            this.autoHideOnLoginCheckbox.AutoSize = true;
            this.autoHideOnLoginCheckbox.Checked = global::SABotSupport.Properties.Settings.Default.AutoHideOnLogin;
            this.autoHideOnLoginCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoHideOnLoginCheckbox.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::SABotSupport.Properties.Settings.Default, "AutoHideOnLogin", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.autoHideOnLoginCheckbox.Location = new System.Drawing.Point(5, 229);
            this.autoHideOnLoginCheckbox.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.autoHideOnLoginCheckbox.Name = "autoHideOnLoginCheckbox";
            this.autoHideOnLoginCheckbox.Size = new System.Drawing.Size(156, 21);
            this.autoHideOnLoginCheckbox.TabIndex = 11;
            this.autoHideOnLoginCheckbox.Text = "登陸時自動隱藏石器";
            this.autoHideOnLoginCheckbox.UseVisualStyleBackColor = true;
            // 
            // AccountEnabled
            // 
            this.AccountEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.AccountEnabled.HeaderText = "";
            this.AccountEnabled.MinimumWidth = 6;
            this.AccountEnabled.Name = "AccountEnabled";
            this.AccountEnabled.ReadOnly = true;
            this.AccountEnabled.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.AccountEnabled.Width = 24;
            // 
            // Account
            // 
            this.Account.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Account.HeaderText = "帳號";
            this.Account.MinimumWidth = 6;
            this.Account.Name = "Account";
            // 
            // Password
            // 
            this.Password.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Password.HeaderText = "密碼";
            this.Password.MinimumWidth = 6;
            this.Password.Name = "Password";
            // 
            // Server
            // 
            this.Server.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Server.HeaderText = "分流";
            this.Server.MinimumWidth = 70;
            this.Server.Name = "Server";
            this.Server.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Server.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Server.Width = 70;
            // 
            // Character
            // 
            this.Character.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Character.HeaderText = "角色";
            this.Character.MinimumWidth = 60;
            this.Character.Name = "Character";
            this.Character.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Character.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Character.Width = 65;
            // 
            // AutoLogonForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MinimumSize = new System.Drawing.Size(700, 450);
            this.Name = "AutoLogonForm";
            this.Text = "自動登陸";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutoLogonForm_FormClosing);
            this.Load += new System.EventHandler(this.AutoLogonForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.accountsDataGridView)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.assaPathsDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label accountsLabel;
        private System.Windows.Forms.DataGridView accountsDataGridView;
        private System.Windows.Forms.Label assaPathsLabel;
        private System.Windows.Forms.DataGridView assaPathsDataGridView;
        private System.Windows.Forms.Button addPathButton;
        private System.Windows.Forms.CheckBox saveAccountPasswordsCheckbox;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Label warningLabel;
        private System.Windows.Forms.DataGridViewCheckBoxColumn PathEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn Path;
        private System.Windows.Forms.Button configureServerINIButton;
        private System.Windows.Forms.CheckBox autoHideOnLoginCheckbox;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem enableAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disableAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem enableSelectedMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disableSelectedMenuItem;
        private System.Windows.Forms.DataGridViewCheckBoxColumn AccountEnabled;
        private System.Windows.Forms.DataGridViewTextBoxColumn Account;
        private System.Windows.Forms.DataGridViewTextBoxColumn Password;
        private System.Windows.Forms.DataGridViewComboBoxColumn Server;
        private System.Windows.Forms.DataGridViewComboBoxColumn Character;
    }
}