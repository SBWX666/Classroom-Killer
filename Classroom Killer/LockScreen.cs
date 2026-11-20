using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Classroom_Killer
{
    /// <summary>
    /// 下课锁屏。全屏黑底、置顶、无边框。
    /// 只有输入软件密码才能解除；Alt+F4 / 任务管理器关闭都被拦下。
    /// 锁定窗口结束（时间到）会自动关闭。
    /// 控件全部在 LockScreen.Designer.cs 里。
    /// </summary>
    internal sealed partial class LockScreen : Form
    {
        private readonly Timer _ticker;
        private bool _unlocked;

        /// <summary>是否通过了密码验证。锁屏因时间窗口结束自动关闭时为 false。</summary>
        public bool WasUnlocked
        {
            get { return _unlocked; }
        }

        /// <summary>是否为自动过期关闭（而不是输密码解锁）。</summary>
        private bool _autoExpired;

        public LockScreen(string reason)
        {
            InitializeComponent();

            // 全屏铺满主屏，运行期才能知道分辨率
            Bounds = Screen.PrimaryScreen.Bounds;
            WindowState = FormWindowState.Maximized;

            labelHint.Text = string.IsNullOrEmpty(reason)
                ? "请离开教室。需要继续使用请输入解锁密码。"
                : reason + "　—　输入解锁密码可继续使用";

            CenterInputs();

            textPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TryUnlock();
                }
            };

            _ticker = new Timer { Interval = 1000 };
            _ticker.Tick += (s, e) =>
            {
                labelClock.Text = DateTime.Now.ToString("yyyy-MM-dd   HH:mm:ss");

                // 锁定窗口结束就自动解锁。
                // ShowDialog 是模态的，不主动关的话时间过了界面还挂着。
                if (!ScheduleManager.ShouldLockNow())
                {
                    _autoExpired = true;
                    _unlocked = true;
                    _ticker.Stop();
                    Close();
                }
            };
            _ticker.Start();

            labelClock.Text = DateTime.Now.ToString("yyyy-MM-dd   HH:mm:ss");

            Resize += (s, e) => CenterInputs();

            Shown += (s, e) => textPassword.Focus();
        }

        private void CenterInputs()
        {
            int left = Math.Max(20, (panelInput.Width - (textPassword.Width + 12 + buttonUnlock.Width)) / 2);
            textPassword.Left = left;
            buttonUnlock.Left = textPassword.Right + 12;
        }

        private void buttonUnlock_Click(object sender, EventArgs e)
        {
            TryUnlock();
        }

        private void TryUnlock()
        {
            string stored = PasswordPrompt.ReadStoredPassword();

            if (!string.IsNullOrEmpty(stored) && PasswordHasher.Verify(textPassword.Text, stored))
            {
                _unlocked = true;
                _ticker.Stop();
                Close();
                return;
            }

            textPassword.Clear();
            SystemSounds.Beep.Play();
            labelHint.Text = "密码错误";
            labelHint.ForeColor = Color.FromArgb(200, 70, 70);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 只有密码验证通过（或时间到）才允许关闭
            if (!_unlocked)
            {
                e.Cancel = true;
                labelHint.Text = "输入解锁密码才能关闭";
                labelHint.ForeColor = Color.FromArgb(200, 70, 70);
                return;
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 吞掉 Alt+F4 / Alt+Tab / Win 键，其余（包括密码输入）正常放行
            if (keyData == (Keys.Alt | Keys.F4)) return true;
            if (keyData == (Keys.Alt | Keys.Tab)) return true;
            if (keyData == Keys.LWin || keyData == Keys.RWin) return true;
            if (keyData == (Keys.Control | Keys.Escape)) return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
