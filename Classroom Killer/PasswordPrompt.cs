using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Classroom_Killer
{
    /// <summary>
    /// 通用密码确认框。退出程序、进设置、改用户名、改密码都走这里。
    /// 密码读自 Date\DateBase.config 的 PassWord 项，校验走 PasswordHasher。
    /// 控件全部在 PasswordPrompt.Designer.cs 里。
    /// </summary>
    internal sealed partial class PasswordPrompt : Form
    {

        private const int MaxAttempts = 3;

        private PasswordPrompt(string title, string message, string okText)
        {
            InitializeComponent();

            Text = title;
            labelMessage.Text = message;
            buttonOk.Text = okText;

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        public string EnteredPassword
        {
            get { return textPassword.Text; }
        }

        /// <summary>
        /// 弹出密码框。返回 true 表示密码正确。
        /// 取消或连续输错 <see cref="MaxAttempts"/> 次都返回 false。
        /// </summary>
        public static bool Confirm(IWin32Window owner, string title, string message, string okText)
        {
            string stored = ReadStoredPassword();

            if (string.IsNullOrEmpty(stored))
            {
                string detail = string.IsNullOrEmpty(LastError) ? "" : "\r\nReason: " + LastError;

                MessageBox.Show(owner,
                    "Cannot read the password file.\r\n\r\nPath: " + Paths.ConfigFile + detail,
                    "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                using (var dialog = new PasswordPrompt(title, message, okText))
                {
                    if (dialog.ShowDialog(owner) != DialogResult.OK)
                        return false;

                    if (PasswordHasher.Verify(dialog.EnteredPassword, stored))
                        return true;
                }

                MessageBox.Show(owner, "Wrong password.", "ERROR!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        /// <summary>退出程序前的确认。</summary>
        public static bool ConfirmExit(IWin32Window owner)
        {
            return Confirm(owner, "Confirm exit",
                "Enter the current password to close Classroom Killer:", "Exit");
        }

        /// <summary>打开设置界面前的确认。</summary>
        public static bool ConfirmSettings(IWin32Window owner)
        {
            return Confirm(owner, "Settings",
                "Enter the password to change settings:", "Continue");
        }

        /// <summary>最近一次读取失败的原因，给错误框用。</summary>
        internal static string LastError { get; private set; }

        internal static string ReadStoredPassword()
        {
            LastError = null;

            try
            {
                string configPath = Paths.ConfigFile;

                if (!File.Exists(configPath))
                {
                    LastError = "file not found";
                    return null;
                }

                // 不用 ConfigurationManager：它内部要搭配置文件映射、节处理器、
                // 缓存一整套东西，一旦中间某步抛异常就只剩"读不到"，且把原因吞掉，
                // 之前只能靠瞎猜。这里直接当普通 XML 解析，少一层黑盒。
                var doc = new XmlDocument();
                doc.Load(configPath);

                XmlNode node = doc.SelectSingleNode(
                    "/configuration/appSettings/add[@key='PassWord']");

                if (node == null || node.Attributes == null)
                {
                    LastError = "no <add key=\"PassWord\"> element";
                    return null;
                }

                XmlAttribute value = node.Attributes["value"];
                if (value == null || string.IsNullOrEmpty(value.Value.Trim()))
                {
                    LastError = "PassWord value is empty";
                    return null;
                }

                return value.Value.Trim();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }
    }
}
