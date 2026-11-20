namespace Classroom_Killer
{
    partial class Settings
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
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupSchedule = new System.Windows.Forms.GroupBox();
            this.textLockTimes = new System.Windows.Forms.TextBox();
            this.labelDuration = new System.Windows.Forms.Label();
            this.textDuration = new System.Windows.Forms.TextBox();
            this.labelTip = new System.Windows.Forms.Label();
            this.labelExitHold = new System.Windows.Forms.Label();
            this.textExitHold = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupSchedule.SuspendLayout();
            this.SuspendLayout();
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(326, 470);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 31);
            this.button1.TabIndex = 0;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // checkBox1
            //
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(14, 28);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(121, 25);
            this.checkBox1.TabIndex = 1;
            this.checkBox1.Text = "taskmgr.exe";
            this.checkBox1.UseVisualStyleBackColor = true;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(320, 46);
            this.label1.TabIndex = 2;
            this.label1.Text = "SoftWare Settings";
            //
            // checkBox2
            //
            this.checkBox2.AutoSize = true;
            this.checkBox2.Checked = true;
            this.checkBox2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox2.Location = new System.Drawing.Point(14, 59);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(92, 25);
            this.checkBox2.TabIndex = 4;
            this.checkBox2.Text = "cmd.exe";
            this.checkBox2.UseVisualStyleBackColor = true;
            //
            // checkBox3
            //
            this.checkBox3.AutoSize = true;
            this.checkBox3.Checked = true;
            this.checkBox3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox3.Location = new System.Drawing.Point(160, 28);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(125, 25);
            this.checkBox3.TabIndex = 5;
            this.checkBox3.Text = "perfmon.exe";
            this.checkBox3.UseVisualStyleBackColor = true;
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 172);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(227, 21);
            this.label3.TabIndex = 6;
            this.label3.Text = "Software auto-start settings:";
            //
            // checkBox4
            //
            this.checkBox4.AutoSize = true;
            this.checkBox4.Checked = true;
            this.checkBox4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox4.Location = new System.Drawing.Point(160, 59);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(141, 25);
            this.checkBox4.TabIndex = 7;
            this.checkBox4.Text = "powershell.exe";
            this.checkBox4.UseVisualStyleBackColor = true;
            //
            // comboBox1
            //
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "True",
            "False"});
            this.comboBox1.Location = new System.Drawing.Point(20, 196);
            this.comboBox1.MaxDropDownItems = 2;
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 29);
            this.comboBox1.TabIndex = 8;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.checkBox3);
            this.groupBox1.Controls.Add(this.checkBox4);
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Location = new System.Drawing.Point(20, 78);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(390, 85);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Monitor the process:";
            //
            // groupSchedule
            //
            this.groupSchedule.Controls.Add(this.textLockTimes);
            this.groupSchedule.Controls.Add(this.labelDuration);
            this.groupSchedule.Controls.Add(this.textDuration);
            this.groupSchedule.Controls.Add(this.labelTip);
            this.groupSchedule.Location = new System.Drawing.Point(20, 236);
            this.groupSchedule.Name = "groupSchedule";
            this.groupSchedule.Size = new System.Drawing.Size(390, 175);
            this.groupSchedule.TabIndex = 10;
            this.groupSchedule.TabStop = false;
            this.groupSchedule.Text = "下课时间表（每行一个，HH:mm）";
            //
            // textLockTimes
            //
            this.textLockTimes.AcceptsReturn = true;
            this.textLockTimes.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textLockTimes.Location = new System.Drawing.Point(12, 28);
            this.textLockTimes.Multiline = true;
            this.textLockTimes.Name = "textLockTimes";
            this.textLockTimes.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textLockTimes.Size = new System.Drawing.Size(160, 122);
            this.textLockTimes.TabIndex = 0;
            //
            // labelDuration
            //
            this.labelDuration.AutoSize = true;
            this.labelDuration.Location = new System.Drawing.Point(196, 30);
            this.labelDuration.Name = "labelDuration";
            this.labelDuration.Size = new System.Drawing.Size(85, 21);
            this.labelDuration.TabIndex = 1;
            this.labelDuration.Text = "每段锁几分钟";
            //
            // textDuration
            //
            this.textDuration.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textDuration.Location = new System.Drawing.Point(196, 54);
            this.textDuration.Name = "textDuration";
            this.textDuration.Size = new System.Drawing.Size(100, 26);
            this.textDuration.TabIndex = 2;
            //
            // labelTip
            //
            this.labelTip.ForeColor = System.Drawing.Color.Gray;
            this.labelTip.Location = new System.Drawing.Point(196, 86);
            this.labelTip.Name = "labelTip";
            this.labelTip.Size = new System.Drawing.Size(180, 90);
            this.labelTip.TabIndex = 3;
            this.labelTip.Text = "例：08:45\r\n到点后锁定 10 分钟。\r\n重启时若还在\r\n课间会立即锁屏。";
            //
            // labelExitHold
            //
            this.labelExitHold.ForeColor = System.Drawing.Color.Gray;
            this.labelExitHold.Location = new System.Drawing.Point(20, 432);
            this.labelExitHold.Name = "labelExitHold";
            this.labelExitHold.Size = new System.Drawing.Size(390, 20);
            this.labelExitHold.TabIndex = 11;
            this.labelExitHold.Text = "退出后不再拉起（分钟，0 = 直到下次开机）";
            //
            // textExitHold
            //
            this.textExitHold.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textExitHold.Location = new System.Drawing.Point(20, 456);
            this.textExitHold.Name = "textExitHold";
            this.textExitHold.Size = new System.Drawing.Size(80, 26);
            this.textExitHold.TabIndex = 12;
            //
            // Settings
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(430, 520);
            this.ControlBox = false;
            this.Controls.Add(this.textExitHold);
            this.Controls.Add(this.labelExitHold);
            this.Controls.Add(this.groupSchedule);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupSchedule.ResumeLayout(false);
            this.groupSchedule.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupSchedule;
        private System.Windows.Forms.TextBox textLockTimes;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.TextBox textDuration;
        private System.Windows.Forms.Label labelTip;
        private System.Windows.Forms.Label labelExitHold;
        private System.Windows.Forms.TextBox textExitHold;
    }
}
