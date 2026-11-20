namespace Classroom_Killer
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.userToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeTheUserPasswordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changePassWordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.softwareSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutUsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this._countdownTitle = new System.Windows.Forms.Label();
            this._countdown = new System.Windows.Forms.Label();
            this._nextInfo = new System.Windows.Forms.Label();
            this._scheduleTitle = new System.Windows.Forms.Label();
            this._scheduleList = new System.Windows.Forms.ListBox();
            this._statusTitle = new System.Windows.Forms.Label();
            this._driverStatus = new System.Windows.Forms.Label();
            this._serviceStatus = new System.Windows.Forms.Label();
            this._protectStatus = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.userToolStripMenuItem,
            this.softwareSettingsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(600, 25);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // userToolStripMenuItem
            // 
            this.userToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeTheUserPasswordToolStripMenuItem,
            this.changePassWordToolStripMenuItem});
            this.userToolStripMenuItem.Name = "userToolStripMenuItem";
            this.userToolStripMenuItem.Size = new System.Drawing.Size(167, 21);
            this.userToolStripMenuItem.Text = "User information settings";
            this.userToolStripMenuItem.Click += new System.EventHandler(this.userToolStripMenuItem_Click);
            // 
            // changeTheUserPasswordToolStripMenuItem
            // 
            this.changeTheUserPasswordToolStripMenuItem.Name = "changeTheUserPasswordToolStripMenuItem";
            this.changeTheUserPasswordToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.changeTheUserPasswordToolStripMenuItem.Text = "Change UserName";
            this.changeTheUserPasswordToolStripMenuItem.Click += new System.EventHandler(this.changeTheUserPasswordToolStripMenuItem_Click);
            // 
            // changePassWordToolStripMenuItem
            // 
            this.changePassWordToolStripMenuItem.Name = "changePassWordToolStripMenuItem";
            this.changePassWordToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.changePassWordToolStripMenuItem.Text = "Change PassWord";
            this.changePassWordToolStripMenuItem.Click += new System.EventHandler(this.changePassWordToolStripMenuItem_Click);
            // 
            // softwareSettingsToolStripMenuItem
            // 
            this.softwareSettingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem});
            this.softwareSettingsToolStripMenuItem.Name = "softwareSettingsToolStripMenuItem";
            this.softwareSettingsToolStripMenuItem.Size = new System.Drawing.Size(121, 21);
            this.softwareSettingsToolStripMenuItem.Text = "Software Settings";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutUsToolStripMenuItem});
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(55, 21);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // aboutUsToolStripMenuItem
            // 
            this.aboutUsToolStripMenuItem.Name = "aboutUsToolStripMenuItem";
            this.aboutUsToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.aboutUsToolStripMenuItem.Text = "About Us";
            this.aboutUsToolStripMenuItem.Click += new System.EventHandler(this.aboutUsToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
            this.statusStrip1.Location = new System.Drawing.Point(0, 418);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(600, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(150, 17);
            this.toolStripStatusLabel1.Text = "Welcome to this system!";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(400, 17);
            this.toolStripStatusLabel2.Spring = true;
            // 
            // _countdownTitle
            // 
            this._countdownTitle.AutoSize = true;
            this._countdownTitle.ForeColor = System.Drawing.Color.Gray;
            this._countdownTitle.Location = new System.Drawing.Point(26, 42);
            this._countdownTitle.Name = "_countdownTitle";
            this._countdownTitle.Size = new System.Drawing.Size(90, 21);
            this._countdownTitle.TabIndex = 3;
            this._countdownTitle.Text = "距下次锁屏";
            // 
            // _countdown
            // 
            this._countdown.Font = new System.Drawing.Font("Consolas", 34F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._countdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(35)))), ((int)(((byte)(35)))));
            this._countdown.Location = new System.Drawing.Point(22, 66);
            this._countdown.Name = "_countdown";
            this._countdown.Size = new System.Drawing.Size(330, 62);
            this._countdown.TabIndex = 4;
            this._countdown.Text = "--:--:--";
            // 
            // _nextInfo
            // 
            this._nextInfo.ForeColor = System.Drawing.Color.Gray;
            this._nextInfo.Location = new System.Drawing.Point(26, 134);
            this._nextInfo.Name = "_nextInfo";
            this._nextInfo.Size = new System.Drawing.Size(330, 20);
            this._nextInfo.TabIndex = 5;
            this._nextInfo.Text = "—";
            // 
            // _scheduleTitle
            // 
            this._scheduleTitle.AutoSize = true;
            this._scheduleTitle.ForeColor = System.Drawing.Color.Gray;
            this._scheduleTitle.Location = new System.Drawing.Point(26, 172);
            this._scheduleTitle.Name = "_scheduleTitle";
            this._scheduleTitle.Size = new System.Drawing.Size(90, 21);
            this._scheduleTitle.TabIndex = 6;
            this._scheduleTitle.Text = "今日时间表";
            // 
            // _scheduleList
            // 
            this._scheduleList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._scheduleList.Font = new System.Drawing.Font("Consolas", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._scheduleList.FormattingEnabled = true;
            this._scheduleList.IntegralHeight = false;
            this._scheduleList.ItemHeight = 22;
            this._scheduleList.Location = new System.Drawing.Point(24, 196);
            this._scheduleList.Name = "_scheduleList";
            this._scheduleList.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this._scheduleList.Size = new System.Drawing.Size(310, 198);
            this._scheduleList.TabIndex = 7;
            // 
            // _statusTitle
            // 
            this._statusTitle.AutoSize = true;
            this._statusTitle.ForeColor = System.Drawing.Color.Gray;
            this._statusTitle.Location = new System.Drawing.Point(370, 172);
            this._statusTitle.Name = "_statusTitle";
            this._statusTitle.Size = new System.Drawing.Size(70, 21);
            this._statusTitle.TabIndex = 8;
            this._statusTitle.Text = "保护状态";
            // 
            // _driverStatus
            // 
            this._driverStatus.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._driverStatus.ForeColor = System.Drawing.Color.Gray;
            this._driverStatus.Location = new System.Drawing.Point(370, 200);
            this._driverStatus.Name = "_driverStatus";
            this._driverStatus.Size = new System.Drawing.Size(200, 26);
            this._driverStatus.TabIndex = 9;
            this._driverStatus.Text = "●  驱动    检查中…";
            // 
            // _serviceStatus
            // 
            this._serviceStatus.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._serviceStatus.ForeColor = System.Drawing.Color.Gray;
            this._serviceStatus.Location = new System.Drawing.Point(370, 234);
            this._serviceStatus.Name = "_serviceStatus";
            this._serviceStatus.Size = new System.Drawing.Size(200, 26);
            this._serviceStatus.TabIndex = 10;
            this._serviceStatus.Text = "●  服务    检查中…";
            // 
            // _protectStatus
            // 
            this._protectStatus.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._protectStatus.ForeColor = System.Drawing.Color.Gray;
            this._protectStatus.Location = new System.Drawing.Point(370, 268);
            this._protectStatus.Name = "_protectStatus";
            this._protectStatus.Size = new System.Drawing.Size(200, 26);
            this._protectStatus.TabIndex = 11;
            this._protectStatus.Text = "●  进程    检查中…";
            // 
            // Main
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(600, 440);
            this.Controls.Add(this._protectStatus);
            this.Controls.Add(this._serviceStatus);
            this.Controls.Add(this._driverStatus);
            this.Controls.Add(this._statusTitle);
            this.Controls.Add(this._scheduleList);
            this.Controls.Add(this._scheduleTitle);
            this.Controls.Add(this._nextInfo);
            this.Controls.Add(this._countdown);
            this.Controls.Add(this._countdownTitle);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = " Form Main";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Closing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem userToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeTheUserPasswordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changePassWordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem softwareSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutUsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.Label _countdownTitle;
        private System.Windows.Forms.Label _countdown;
        private System.Windows.Forms.Label _nextInfo;
        private System.Windows.Forms.Label _scheduleTitle;
        private System.Windows.Forms.ListBox _scheduleList;
        private System.Windows.Forms.Label _statusTitle;
        private System.Windows.Forms.Label _driverStatus;
        private System.Windows.Forms.Label _serviceStatus;
        private System.Windows.Forms.Label _protectStatus;
    }
}
