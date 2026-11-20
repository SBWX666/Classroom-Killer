using System;
using System.Reflection;
using Microsoft.Win32;

namespace Classroom_Killer
{
    /// <summary>
    /// 开机自启动注册表项的守护者。
    /// 写的是 HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run，
    /// 所有用户登录都会执行，而且只有管理员能改。
    /// </summary>
    internal static class StartupRegistrar
    {
        private const string RunKeyPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private const string StartupApprovedPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        private const string ValueName = "ClassroomKiller";

        /// <summary>
        /// 确保自启动项存在且指向当前 exe。返回 true 表示已经就位。
        /// 顺带清掉任务管理器"启动"页留下的"已禁用"标记。
        /// </summary>
        public static bool Ensure()
        {
            bool written = EnsureRunValue();
            ReviveStartupApproval();
            return written;
        }

        private static bool EnsureRunValue()
        {
            try
            {
                string exe = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exe))
                    return false;

                string expected = "\"" + exe + "\"";

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null)
                        return false;

                    string current = key.GetValue(ValueName) as string;
                    if (string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
                        return true;

                    key.SetValue(ValueName, expected, RegistryValueKind.String);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 任务管理器里"禁用启动项"不会删 Run 值，而是写
        /// StartupApproved\Run 下一个 12 字节的二进制：首字节最低位 1 = 已禁用。
        /// 这里把它改回 0x06（启用），否则自启动项形同虚设。
        /// </summary>
        public static bool ReviveStartupApproval()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(StartupApprovedPath, true))
                {
                    if (key == null)
                        return false;

                    object raw = key.GetValue(ValueName);
                    byte[] value = raw as byte[];

                    if (value == null || value.Length == 0)
                        return true;   // 没有标记 = 默认启用

                    if ((value[0] & 0x01) == 0)
                        return true;   // 本来就是启用状态

                    value[0] = 0x06;   // 0x06 = enabled with timestamp
                    key.SetValue(ValueName, value, RegistryValueKind.Binary);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>删掉自启动项（设置里关掉自启时用）。</summary>
        public static void Remove()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunKeyPath, true))
                {
                    if (key != null)
                        key.DeleteValue(ValueName, false);
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(StartupApprovedPath, true))
                {
                    if (key != null)
                        key.DeleteValue(ValueName, false);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>按设置开关自启动。</summary>
        public static void ApplyAutoStart(bool enabled)
        {
            if (enabled)
                Ensure();
            else
                Remove();
        }

        /// <summary>读回当前写的是什么，便于排查。</summary>
        public static string Read()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return null;
                    return key.GetValue(ValueName) as string;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
