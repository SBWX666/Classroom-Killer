namespace Classroom_Killer
{
    partial class LockScreen
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
            this.labelTitle = new System.Windows.Forms.Label();
            this.labelClock = new System.Windows.Forms.Label();
            this.labelHint = new System.Windows.Forms.Label();
            this.panelInput = new System.Windows.Forms.Panel();
            this.textPassword = new System.Windows.Forms.TextBox();
            this.buttonUnlock = new System.Windows.Forms.Button();
            this.panelInput.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelTitle.Font = new System.Drawing.Font("微软雅黑", 64F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelTitle.ForeColor = System.Drawing.Color.White;
            this.labelTitle.Location = new System.Drawing.Point(0, 60);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(1024, 566);
            this.labelTitle.TabIndex = 0;
            this.labelTitle.Text = "下课时间到";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelClock
            // 
            this.labelClock.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelClock.Font = new System.Drawing.Font("Consolas", 22F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelClock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.labelClock.Location = new System.Drawing.Point(0, 0);
            this.labelClock.Name = "labelClock";
            this.labelClock.Size = new System.Drawing.Size(1024, 60);
            this.labelClock.TabIndex = 1;
            this.labelClock.Text = "0000-00-00   00:00:00";
            this.labelClock.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelHint
            // 
            this.labelHint.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelHint.Font = new System.Drawing.Font("微软雅黑", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(140)))));
            this.labelHint.Location = new System.Drawing.Point(0, 626);
            this.labelHint.Name = "labelHint";
            this.labelHint.Size = new System.Drawing.Size(1024, 50);
            this.labelHint.TabIndex = 2;
            this.labelHint.Text = "输入解锁密码可继续使用";
            this.labelHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelInput
            // 
            this.panelInput.Controls.Add(this.buttonUnlock);
            this.panelInput.Controls.Add(this.textPassword);
            this.panelInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelInput.Location = new System.Drawing.Point(0, 676);
            this.panelInput.Name = "panelInput";
            this.panelInput.Size = new System.Drawing.Size(1024, 70);
            this.panelInput.TabIndex = 3;
            // 
            // textPassword
            // 
            this.textPassword.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textPassword.Location = new System.Drawing.Point(332, 17);
            this.textPassword.Name = "textPassword";
            this.textPassword.Size = new System.Drawing.Size(260, 36);
            this.textPassword.TabIndex = 0;
            this.textPassword.UseSystemPasswordChar = true;
            // 
            // buttonUnlock
            // 
            this.buttonUnlock.Font = new System.Drawing.Font("微软雅黑", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonUnlock.Location = new System.Drawing.Point(604, 17);
            this.buttonUnlock.Name = "buttonUnlock";
            this.buttonUnlock.Size = new System.Drawing.Size(100, 32);
            this.buttonUnlock.TabIndex = 1;
            this.buttonUnlock.Text = "解锁";
            this.buttonUnlock.UseVisualStyleBackColor = true;
            this.buttonUnlock.Click += new System.EventHandler(this.buttonUnlock_Click);
            // 
            // LockScreen
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1024, 746);
            this.ControlBox = false;
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelHint);
            this.Controls.Add(this.panelInput);
            this.Controls.Add(this.labelClock);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LockScreen";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "LockScreen";
            this.TopMost = true;
            this.panelInput.ResumeLayout(false);
            this.panelInput.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label labelClock;
        private System.Windows.Forms.Label labelHint;
        private System.Windows.Forms.Panel panelInput;
        private System.Windows.Forms.TextBox textPassword;
        private System.Windows.Forms.Button buttonUnlock;
    }
}
