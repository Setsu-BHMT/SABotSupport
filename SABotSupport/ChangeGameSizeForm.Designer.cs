
namespace SABotSupport
{
    partial class ChangeGameSizeForm
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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.applyButton = new System.Windows.Forms.Button();
            this.currentSizeLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.widthNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.heightNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.keepRatioCheckbox = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.oldSizesListbox = new System.Windows.Forms.ListBox();
            this.NumericUpDownGroupbox = new System.Windows.Forms.GroupBox();
            this.refreshButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.widthNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightNumericUpDown)).BeginInit();
            this.NumericUpDownGroupbox.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(157, 145);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 25);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "確定";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(240, 145);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 25);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // applyButton
            // 
            this.applyButton.Enabled = false;
            this.applyButton.Location = new System.Drawing.Point(321, 145);
            this.applyButton.Name = "applyButton";
            this.applyButton.Size = new System.Drawing.Size(75, 25);
            this.applyButton.TabIndex = 2;
            this.applyButton.Text = "套用";
            this.applyButton.UseVisualStyleBackColor = true;
            this.applyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // currentSizeLabel
            // 
            this.currentSizeLabel.AutoSize = true;
            this.currentSizeLabel.Location = new System.Drawing.Point(12, 9);
            this.currentSizeLabel.Name = "currentSizeLabel";
            this.currentSizeLabel.Size = new System.Drawing.Size(126, 17);
            this.currentSizeLabel.TabIndex = 3;
            this.currentSizeLabel.Text = "現在大小: 800x600";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 17);
            this.label2.TabIndex = 4;
            this.label2.Text = "寬:";
            // 
            // widthNumericUpDown
            // 
            this.widthNumericUpDown.Location = new System.Drawing.Point(38, 21);
            this.widthNumericUpDown.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.widthNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.widthNumericUpDown.Name = "widthNumericUpDown";
            this.widthNumericUpDown.Size = new System.Drawing.Size(72, 22);
            this.widthNumericUpDown.TabIndex = 5;
            this.widthNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.widthNumericUpDown.ValueChanged += new System.EventHandler(this.WidthNumericUpDown_ValueChanged);
            // 
            // heightNumericUpDown
            // 
            this.heightNumericUpDown.Location = new System.Drawing.Point(170, 21);
            this.heightNumericUpDown.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.heightNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.heightNumericUpDown.Name = "heightNumericUpDown";
            this.heightNumericUpDown.Size = new System.Drawing.Size(72, 22);
            this.heightNumericUpDown.TabIndex = 7;
            this.heightNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.heightNumericUpDown.ValueChanged += new System.EventHandler(this.HeightNumericUpDown_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(138, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "高:";
            // 
            // keepRatioCheckbox
            // 
            this.keepRatioCheckbox.AutoSize = true;
            this.keepRatioCheckbox.Checked = true;
            this.keepRatioCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.keepRatioCheckbox.Location = new System.Drawing.Point(270, 22);
            this.keepRatioCheckbox.Name = "keepRatioCheckbox";
            this.keepRatioCheckbox.Size = new System.Drawing.Size(100, 21);
            this.keepRatioCheckbox.TabIndex = 8;
            this.keepRatioCheckbox.Text = "等比例縮放";
            this.keepRatioCheckbox.UseVisualStyleBackColor = true;
            this.keepRatioCheckbox.CheckedChanged += new System.EventHandler(this.KeepRatioCheckbox_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 84);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 17);
            this.label4.TabIndex = 9;
            this.label4.Text = "上次紀錄:";
            // 
            // oldSizesListbox
            // 
            this.oldSizesListbox.FormattingEnabled = true;
            this.oldSizesListbox.ItemHeight = 16;
            this.oldSizesListbox.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.oldSizesListbox.Location = new System.Drawing.Point(12, 104);
            this.oldSizesListbox.Name = "oldSizesListbox";
            this.oldSizesListbox.Size = new System.Drawing.Size(120, 68);
            this.oldSizesListbox.TabIndex = 10;
            this.oldSizesListbox.SelectedIndexChanged += new System.EventHandler(this.OldSizesListbox_SelectedIndexChanged);
            // 
            // NumericUpDownGroupbox
            // 
            this.NumericUpDownGroupbox.Controls.Add(this.label2);
            this.NumericUpDownGroupbox.Controls.Add(this.widthNumericUpDown);
            this.NumericUpDownGroupbox.Controls.Add(this.label3);
            this.NumericUpDownGroupbox.Controls.Add(this.keepRatioCheckbox);
            this.NumericUpDownGroupbox.Controls.Add(this.heightNumericUpDown);
            this.NumericUpDownGroupbox.Location = new System.Drawing.Point(16, 29);
            this.NumericUpDownGroupbox.Name = "NumericUpDownGroupbox";
            this.NumericUpDownGroupbox.Size = new System.Drawing.Size(380, 52);
            this.NumericUpDownGroupbox.TabIndex = 11;
            this.NumericUpDownGroupbox.TabStop = false;
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(319, 5);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(75, 25);
            this.refreshButton.TabIndex = 12;
            this.refreshButton.Text = "更新";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // ChangeGameSizeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(412, 180);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.NumericUpDownGroupbox);
            this.Controls.Add(this.oldSizesListbox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.currentSizeLabel);
            this.Controls.Add(this.applyButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "ChangeGameSizeForm";
            this.Text = "變更石器視窗大小";
            this.Load += new System.EventHandler(this.ChangeGameSizeDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.widthNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.heightNumericUpDown)).EndInit();
            this.NumericUpDownGroupbox.ResumeLayout(false);
            this.NumericUpDownGroupbox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button applyButton;
        private System.Windows.Forms.Label currentSizeLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown widthNumericUpDown;
        private System.Windows.Forms.NumericUpDown heightNumericUpDown;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox keepRatioCheckbox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox oldSizesListbox;
        private System.Windows.Forms.GroupBox NumericUpDownGroupbox;
        private System.Windows.Forms.Button refreshButton;
    }
}