using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Classroom_Killer
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();

            // Designer 里 comboBox1 的前景色原来是 Yellow，白底上看不见
            comboBox1.ForeColor = Color.Black;

            LoadSettings();
        }

        /// <summary>
        /// 按系统 DPI 手动缩放。
        /// AutoScaleMode.Font 依赖字体的相对比例，在 125% DPI 的机器上
        /// 算出来的缩放比不对，控件会互相挤压。直接量 DpiX 最准。
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                using (Graphics g = CreateGraphics())
                {
                    float scale = g.DpiX / 96f;
                    if (scale > 1.05f && scale < 3f)
                    {
                        SuspendLayout();
                        Scale(new SizeF(scale, scale));
                        ClientSize = new Size(
                            (int)(ClientSize.Width * scale),
                            (int)(ClientSize.Height * scale));
                        ResumeLayout(true);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>从 settings.config 读回全部配置，保证界面与文件同步。</summary>
        private void LoadSettings()
        {
            Configuration configuration = ScheduleManager.Open();

            if (configuration == null)
            {
                textDuration.Text = ScheduleManager.DefaultDurationMinutes.ToString();
                textExitHold.Text = ScheduleManager.DefaultExitHoldMinutes.ToString();
                comboBox1.SelectedIndex = 0;
                return;
            }

            string times = ScheduleManager.Get(configuration, ScheduleManager.KeyLockTimes);
            string duration = ScheduleManager.Get(configuration, ScheduleManager.KeyDuration);
            string monitors = ScheduleManager.Get(configuration, ScheduleManager.KeyMonitor);

            if (!string.IsNullOrEmpty(times))
            {
                textLockTimes.Lines = times
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToArray();
            }

            textDuration.Text = string.IsNullOrEmpty(duration)
                ? ScheduleManager.DefaultDurationMinutes.ToString()
                : duration;

            BindMonitorCheckboxes(monitors);

            comboBox1.SelectedIndex = ScheduleManager.IsAutoStartEnabled() ? 0 : 1;

            textExitHold.Text = ScheduleManager.GetExitHoldMinutes().ToString();
        }

        private void BindMonitorCheckboxes(string monitors)
        {
            var set = new HashSet<string>(
                (monitors ?? string.Empty)
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLowerInvariant()));

            if (set.Count == 0)
                return;   // 没配置过就保留控件默认状态

            checkBox1.Checked = set.Contains("taskmgr.exe");
            checkBox2.Checked = set.Contains("cmd.exe");
            checkBox3.Checked = set.Contains("perfmon.exe");
            checkBox4.Checked = set.Contains("powershell.exe");
        }

        private string CollectMonitors()
        {
            var list = new List<string>();

            if (checkBox1.Checked) list.Add("taskmgr.exe");
            if (checkBox2.Checked) list.Add("cmd.exe");
            if (checkBox3.Checked) list.Add("perfmon.exe");
            if (checkBox4.Checked) list.Add("powershell.exe");

            return string.Join(",", list.ToArray());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Configuration configuration = ScheduleManager.Open();
            if (configuration == null)
            {
                MessageBox.Show("Cannot open Date\\settings.config", "ERROR!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 时间表：丢掉空行和非法格式，统一成 HH:mm
            var valid = new List<string>();
            var invalid = new List<string>();

            foreach (string raw in textLockTimes.Lines)
            {
                string text = raw.Trim();
                if (text.Length == 0) continue;

                TimeSpan ts;
                if (TimeSpan.TryParse(text, out ts))
                    valid.Add(string.Format("{0:hh\\:mm}", ts));
                else
                    invalid.Add(text);
            }

            if (invalid.Count > 0)
            {
                MessageBox.Show(
                    "These entries are not valid HH:mm:\r\n" + string.Join("\r\n", invalid.ToArray()),
                    "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ScheduleManager.Set(configuration, ScheduleManager.KeyLockTimes,
                                string.Join(",", valid.ToArray()));
            ScheduleManager.Set(configuration, ScheduleManager.KeyDuration,
                                ScheduleManager.ParseDuration(textDuration.Text).ToString());
            ScheduleManager.Set(configuration, ScheduleManager.KeyMonitor, CollectMonitors());

            bool autoStart = comboBox1.SelectedIndex <= 0;
            ScheduleManager.Set(configuration, ScheduleManager.KeyAutoStart,
                                autoStart ? "True" : "False");

            int exitHold;
            if (!int.TryParse(textExitHold.Text.Trim(), out exitHold) || exitHold < 0 || exitHold > 1440)
                exitHold = ScheduleManager.DefaultExitHoldMinutes;
            ScheduleManager.Set(configuration, ScheduleManager.KeyExitHold, exitHold.ToString());

            try
            {
                configuration.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "ERROR!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 自启动开关立刻生效
            StartupRegistrar.ApplyAutoStart(autoStart);

            this.Close();
        }
    }
}
