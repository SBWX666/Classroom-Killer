using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Classroom_Killer
{
    public partial class Main : Form
    {

        public Main()
        {
            InitializeComponent();
        }

        /// <summary>true 表示启动后不显示窗口，直接跑在托盘里（上课时间用）。</summary>
        public bool StartHidden { get; set; }

        private bool _exitConfirmed;
        private TrayIcon _tray;

        // 仪表盘控件全部在 Main.Designer.cs 里声明，设计器可见
        private System.Windows.Forms.Timer _uiTimer;
        private int _uiTick;
        private bool _protectKnown;
        private bool _protectOk;

        // ---------------- 仪表盘 ----------------

        private static void SetStatus(Label target, string name, bool ok, bool unknown = false)
        {
            target.Text = "●  " + name + "    " + (unknown ? "检查中…" : (ok ? "正常" : "未运行"));
            target.ForeColor = unknown
                ? Color.Gray
                : (ok ? Color.FromArgb(0, 150, 80) : Color.FromArgb(200, 60, 60));
        }

        private void StartDashboardTimer()
        {
            RefreshCountdown();
            RefreshStatus();

            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += (s, e) =>
            {
                RefreshCountdown();

                if (++_uiTick % 5 == 0)
                    RefreshStatus();
            };
            _uiTimer.Start();
        }

        private void RefreshCountdown()
        {
            System.Collections.Generic.List<TimeSpan> times = ScheduleManager.GetLockTimes();
            TimeSpan now = DateTime.Now.TimeOfDay;

            if (_scheduleList != null)
            {
                _scheduleList.BeginUpdate();
                _scheduleList.Items.Clear();

                foreach (TimeSpan t in times)
                {
                    bool passed = now >= t;
                    string mark = passed ? "  ×  " : "  ·  ";
                    _scheduleList.Items.Add(string.Format("{0}{1:hh\\:mm}{2}",
                        mark, t, passed ? "    已过" : ""));
                }

                if (times.Count == 0)
                    _scheduleList.Items.Add("  （未配置）");

                _scheduleList.EndUpdate();
            }

            TimeSpan? next = ScheduleManager.NextLockIn();

            if (next == null)
            {
                _countdown.Text = "--:--:--";
                _countdown.ForeColor = Color.FromArgb(150, 150, 150);
                _nextInfo.Text = times.Count == 0
                    ? "还没有配置下课时间，去 Software Settings 里加"
                    : "今天的时间表已全部过完";
                return;
            }

            _countdown.Text = string.Format("{0:hh\\:mm\\:ss}", next.Value);

            // 10 分钟内变橙，1 分钟内变红
            if (next.Value.TotalSeconds <= 60)
                _countdown.ForeColor = Color.FromArgb(210, 60, 60);
            else if (next.Value.TotalMinutes <= 10)
                _countdown.ForeColor = Color.FromArgb(220, 140, 0);
            else
                _countdown.ForeColor = Color.FromArgb(35, 35, 35);

            TimeSpan target = now + next.Value;
            int duration = ScheduleManager.ParseDuration(
                ScheduleManager.Get(ScheduleManager.Open(), ScheduleManager.KeyDuration));

            _nextInfo.Text = string.Format("{0:hh\\:mm} 开始锁屏，持续 {1} 分钟",
                target, duration);
        }

        private void RefreshStatus()
        {
            UpdateCourseStatus();

            SetStatus(_driverStatus, "驱动", DriverInstaller.IsRunning());

            bool guardRunning;
            try
            {
                using (var controller = new System.ServiceProcess.ServiceController(GuardService.Name))
                    guardRunning = controller.Status == System.ServiceProcess.ServiceControllerStatus.Running;
            }
            catch (Exception)
            {
                guardRunning = false;
            }
            SetStatus(_serviceStatus, "服务", guardRunning);

            // 失败过的每次都重试（比如开机时驱动还没起来），成功后就不再查
            if (!_protectOk)
                _protectOk = ProcessProtect.ProtectSelf();

            _protectKnown = true;

            if (_protectOk)
            {
                SetStatus(_protectStatus, "进程", true);
            }
            else
            {
                // 把 Win32 错误码直接摊在界面上：2=设备不存在 5=拒绝访问
                _protectStatus.Text = "●  进程    失败 (" + ProcessProtect.LastError + ")";
                _protectStatus.ForeColor = Color.FromArgb(200, 60, 60);
            }
        }

        private void Closing(object sender, FormClosingEventArgs e)
        {
            if (_exitConfirmed)
                return;

            // 主窗体关闭不再退出程序，直接收进托盘继续跑后台
            e.Cancel = true;
            Hide();

            if (_tray != null)
                _tray.ShowBalloon("Classroom Killer",
                    "已最小化到后台运行。双击托盘图标可以恢复窗口。");
        }

        // ---------------- 托盘 ----------------

        private void InitializeTray()
        {
            if (_tray != null)
                return;

            _tray = new TrayIcon(RestoreFromTray, OpenSettingsFromTray, LoginFromTray, ExitFromTray);
        }

        /// <summary>
        /// 打开登录界面。
        /// 上课时间启动时 Form1 根本没被创建（Program.Main 直接进 Main），
        /// 所以只能从托盘这里补一个入口。
        /// </summary>
        private void LoginFromTray()
        {
            try
            {
                Hide();

                using (var login = new Form1 { CreateMainOnSuccess = false })
                {
                    bool ok = login.ShowDialog() == DialogResult.OK;
                    if (ok)
                        ServiceSetup.Log("tray login: ok");

                    if (!IsDisposed && !Visible)
                    {
                        Show();
                        WindowState = FormWindowState.Normal;
                        Activate();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        private void OpenSettingsFromTray()
        {
            if (!PasswordPrompt.ConfirmSettings(this))
                return;

            using (var dialog = new Settings())
            {
                dialog.ShowDialog(this);
            }

            UpdateCourseStatus();
        }

        private void ExitFromTray()
        {
            if (!PasswordPrompt.ConfirmExit(this))
                return;

            _exitConfirmed = true;

            if (_lockTimer != null)
                _lockTimer.Stop();

            Kill("windowsprocesscheck");
            Kill("windowsprocesscheckbeta");

            if (_tray != null)
            {
                _tray.Dispose();
                _tray = null;
            }

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
                catch (Win32Exception ex)
                {
                    MessageBox.Show("Can't end the application, please find a way to end it yourself", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                    // 无法结束进程，可能有很多原因。
                    // 建议记录这个异常，如果你的程序能够处理这里的某种特定异常了，那么就需要在这里补充处理。
                    // Log.Error(ex);
                }
                catch (InvalidOperationException)
                {
                    // 进程已经退出，无法继续退出。既然已经退了，那这里也算是退出成功了。
                    // 于是这里其实什么代码也不需要执行。
                }
            }
        }//Kill
        private void aboutUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void changeTheUserPasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PasswordPrompt.Confirm(this, "Change user name",
                    "Enter the current password to change the user name:", "Continue"))
                return;

            CUN cun = new CUN();
            cun.ShowDialog(this);
        }

        private void changePassWordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PasswordPrompt.Confirm(this, "Change password",
                    "Enter the current password to change the password:", "Continue"))
                return;

            CPD cpd = new CPD();
            cpd.ShowDialog(this);
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 改设置需要密码
            if (!PasswordPrompt.ConfirmSettings(this))
                return;

            Settings set = new Settings();
            set.ShowDialog(this);

            UpdateCourseStatus();
        }

        private void userToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private System.Windows.Forms.Timer _lockTimer;

        private void Main_Load(object sender, EventArgs e)
        {
            this.toolStripStatusLabel2.Text = "Login Time:" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            // 每 5 秒检查一次下课时间，到点全屏锁住
            _lockTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _lockTimer.Tick += (s, ev) => CheckLockTime();
            _lockTimer.Start();

            InitializeTray();
            StartDashboardTimer();

            if (StartHidden)
            {
                // 上课时间启动：不进界面，直接蹲在托盘里等下课
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Hide();

                _tray.ShowBalloon("Classroom Killer",
                    "上课时间，已在后台运行。到点会自动锁屏。");
            }

            UpdateCourseStatus();
        }

        private void CheckLockTime()
        {
            KillMonitoredProcesses();

            string reason;
            if (!ScheduleManager.ShouldLockNow(out reason))
            {
                UpdateCourseStatus();
                return;
            }

            _lockTimer.Stop();

            // 主窗体可能藏在托盘里，锁屏不要绑 owner
            using (var screen = new LockScreen(reason))
            {
                screen.ShowDialog();
            }

            _lockTimer.Start();
            UpdateCourseStatus();
        }

        /// <summary>
        /// 结束设置里勾选的进程（taskmgr / cmd / perfmon / powershell）。
        /// 名单从 settings.config 的 MonitorProcesses 读，改配置下个周期就生效。
        /// </summary>
        private void KillMonitoredProcesses()
        {
            string list = ScheduleManager.GetMonitoredProcesses();
            if (string.IsNullOrWhiteSpace(list))
                return;

            int self = Process.GetCurrentProcess().Id;

            foreach (string raw in list.Split(new[] { ',', ';' },
                                              StringSplitOptions.RemoveEmptyEntries))
            {
                string name = raw.Trim();
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
                if (name.Length == 0)
                    continue;

                try
                {
                    foreach (Process p in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            if (p.Id == self)
                                continue;

                            p.Kill();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>状态栏左边：当前处于上课 / 课间锁定 / 已结束。</summary>
        private void UpdateCourseStatus()
        {
            try
            {
                if (ScheduleManager.ShouldLockNow())
                {
                    toolStripStatusLabel1.Text = "●  课间锁定中";
                    toolStripStatusLabel1.ForeColor = Color.FromArgb(210, 60, 60);
                    return;
                }

                if (ScheduleManager.GetLockTimes().Count == 0)
                {
                    toolStripStatusLabel1.Text = "●  未配置时间表";
                    toolStripStatusLabel1.ForeColor = Color.Gray;
                    return;
                }

                if (ScheduleManager.NextLockIn() == null)
                {
                    toolStripStatusLabel1.Text = "●  今日已结束";
                    toolStripStatusLabel1.ForeColor = Color.Gray;
                    return;
                }

                toolStripStatusLabel1.Text = "●  上课中";
                toolStripStatusLabel1.ForeColor = Color.FromArgb(0, 150, 80);
            }
            catch (Exception)
            {
            }
        }
    }
}
