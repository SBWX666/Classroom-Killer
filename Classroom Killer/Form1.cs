using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Classroom_Killer
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 登录成功后是否由本窗体自己创建主界面。
        /// 从托盘 Login 打开时设为 false —— 那边已经有 Main 实例了，
        /// 再 new 一个就会同时出现两个主窗口。
        /// </summary>
        public bool CreateMainOnSuccess { get; set; } = true;
        private const int SC_CLOSE = 0xF060;
        private const int MF_ENABLED = 0x00000000;
        private const int MF_GRAYED = 0x00000001;
        private const int MF_DISABLED = 0x00000002;
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, int bRevert);[DllImport("User32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, int uIDEnableItem, int uEnable);
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            HardExit();
        }//Exit
        private void button1_Click(object sender, EventArgs e)
        {
            string filePath = Paths.ConfigFile;

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    "Unable to find important software files!\r\nThe software does not continue to run!\r\n" +
                    "The following files are missing:\r\n" + filePath + "\r\n\r\n" +
                    "Press OK and the app will be closed!",
                    "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Kill("windowsprocesscheck");
                Kill("windowsprocesscheckbeta");
                HardExit();
                return;
            }

            Configuration configuration;
            try
            {
                ExeConfigurationFileMap map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = filePath;
                configuration = ConfigurationManager.OpenMappedExeConfiguration(
                    map, ConfigurationUserLevel.None);
            }
            catch (Exception)
            {
                MessageBox.Show("File read error!", "ERROR!",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Kill("windowsprocesscheck");
                Kill("windowsprocesscheckbeta");
                HardExit();
                return;
            }

            string storedName = configuration.AppSettings.Settings["UserName"] == null
                ? null : configuration.AppSettings.Settings["UserName"].Value;
            string storedPassword = configuration.AppSettings.Settings["PassWord"] == null
                ? null : configuration.AppSettings.Settings["PassWord"].Value;

            if (string.IsNullOrWhiteSpace(storedName) || string.IsNullOrWhiteSpace(storedPassword))
            {
                MessageBox.Show("File read error!", "ERROR!",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                Kill("windowsprocesscheck");
                Kill("windowsprocesscheckbeta");
                HardExit();
                return;
            }

            string inputName = this.textBox1.Text.Trim();
            string inputPassword = this.textBox2.Text.Trim();

            // 用户名、密码分别校验，两边都通过才算登录成功
            bool ok = PasswordHasher.Verify(inputName, storedName)
                   && PasswordHasher.Verify(inputPassword, storedPassword);

            if (!ok)
            {
                this.textBox2.Clear();
                MessageBox.Show("Wrong username or wrong password!", "ERROR!",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 旧 MD5 记录：登录成功后顺手升级成 PBKDF2，用户无感
            bool upgraded = false;
            if (!PasswordHasher.IsCurrentFormat(storedName))
            {
                configuration.AppSettings.Settings["UserName"].Value = PasswordHasher.Hash(inputName);
                upgraded = true;
            }
            if (!PasswordHasher.IsCurrentFormat(storedPassword))
            {
                configuration.AppSettings.Settings["PassWord"].Value = PasswordHasher.Hash(inputPassword);
                upgraded = true;
            }
            if (upgraded)
            {
                try
                {
                    configuration.Save();
                }
                catch (Exception)
                {
                    // 升级失败不影响本次登录
                }
            }

            if (CreateMainOnSuccess)
            {
                Main main = new Main();
                this.Hide();
                main.Show();
            }
            else
            {
                // 从托盘打开：宿主已经有 Main 了，只回传登录结果
                DialogResult = DialogResult.OK;
                Close();
            }
        }//Login Check
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            use USE = new use();
            USE.ShowDialog();
        }

        /// <summary>
        /// 彻底退出进程。
        /// Application.Exit() 在有模态对话框 / 未处理消息时会失效，
        /// 而且进程被驱动保护时常规退出路径也可能被拦，这里先放行再硬杀。
        /// </summary>
        private static void HardExit()
        {
            AppExit.Quit();
        }

        private void Kill(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch (Win32Exception)
                {
                    // 无法结束进程，可能有很多原因。不弹窗打断退出流程。
                    return;
                }
                catch (InvalidOperationException)
                {
                    // 进程已经退出，无法继续退出。既然已经退了，那这里也算是退出成功了。
                }
            }
        }//Kill
        private void timer2_Tick(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("windowsprocesscheck");
            if (processes.Length == 0)
            {
                if (File.Exists(Paths.ProcessCheck))
                {
                    Process.Start(Paths.ProcessCheck);
                    return;
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            Process[] processes2 = Process.GetProcessesByName("windowsprocesscheckbeta");
            if (processes2.Length == 0)
            {
                if (File.Exists(Paths.ProcessCheckBeta))
                {
                    Process.Start(Paths.ProcessCheckBeta);
                    return;
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IntPtr hMenu = GetSystemMenu(this.Handle, 0);
            EnableMenuItem(hMenu, SC_CLOSE, MF_DISABLED | MF_GRAYED);

            // 登录界面不提供退出入口：老师点一下就能关掉程序，整个监控就废了。
            // 真要退出请走主界面 ×（要密码）或托盘右键 Exit。
            linkLabel1.Visible = false;

            // 开机/重启后先看时间：还在课间就直接锁住，不给登录界面
            Shown += (s, ev) => CheckLockTime();
        }

        /// <summary>当前是否处于课间锁定窗口，是则全屏锁住。</summary>
        private void CheckLockTime()
        {
            string reason;
            if (!ScheduleManager.ShouldLockNow(out reason))
                return;

            using (var screen = new LockScreen(reason))
            {
                screen.ShowDialog(this);

                // 锁屏里已经验过密码了，不再要求登录一次，直接进主界面。
                // 如果是时间窗口到期自动关的，说明课间结束了，也直接进去。
                if (screen.WasUnlocked)
                    OpenMain();
            }
        }

        /// <summary>切到主界面（登录成功、或锁屏解锁后共用）。</summary>
        private void OpenMain()
        {
            Main main = new Main();
            Hide();
            main.Show();
        }
    }
}
