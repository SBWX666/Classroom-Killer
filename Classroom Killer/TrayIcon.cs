using System;
using System.Drawing;
using System.Windows.Forms;

namespace Classroom_Killer
{
    /// <summary>
    /// 右下角托盘图标。双击恢复主界面，右键菜单：打开 / 设置 / 退出。
    /// </summary>
    internal sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _icon;
        private bool _disposed;

        public TrayIcon(Action onShow, Action onSettings, Action onLogin, Action onExit)
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open Classroom Killer", null, (s, e) => onShow());
            menu.Items.Add("Login", null, (s, e) => onLogin());
            menu.Items.Add("Settings", null, (s, e) => onSettings());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => onExit());

            _icon = new NotifyIcon
            {
                Icon = LoadIcon(),
                Text = "Classroom Killer",
                Visible = true,
                ContextMenuStrip = menu
            };

            _icon.DoubleClick += (s, e) => onShow();
        }

        private static Icon LoadIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                    return icon;
            }
            catch (Exception)
            {
            }

            return SystemIcons.Application;
        }

        public void ShowBalloon(string title, string text)
        {
            try
            {
                _icon.ShowBalloonTip(2500, title, text, ToolTipIcon.Info);
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _icon.Visible = false;
                _icon.Dispose();
            }
            catch (Exception)
            {
            }
        }
    }
}
