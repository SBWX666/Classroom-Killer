using System;
using System.IO;
using Microsoft.Win32;

namespace Classroom_Killer
{
    /// <summary>
    /// 退出闸门。
    ///
    /// 服务每 5 秒检查界面是否在跑，不在就拉起——用户点 Exit 后会被拉回来。
    /// 所以退出前先写一张带时间戳的"放行票"，服务看到票就暂时不拉。
    ///
    /// 两条通道同时写：
    ///   ① HKLM\SOFTWARE\ClassroomKiller\AllowExitUntil —— 需要管理员，最稳
    ///   ② C:\Users\Public\ClassroomKiller.allow_exit   —— 普通权限也能写
    /// 任一存在且未过期就放行。②兜底是因为 Classroom Killer 未必以提权身份运行，
    /// 只写 HKLM 会静默失败，服务读不到票就照拉不误。
    /// </summary>
    internal static class ExitGate
    {
        private const string KeyPath = @"SOFTWARE\ClassroomKiller";
        private const string ValueName = "AllowExitUntil";

        private static readonly string MarkerPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                         @"..\ClassroomKiller.allow_exit");

        /// <summary>
        /// 放行窗口。从 settings.config 的 ExitHoldMinutes 读，默认 2 分钟。
        /// 0 表示退出后直到下次开机都不再拉起。
        /// </summary>
        private static TimeSpan Window
        {
            get
            {
                int minutes = ScheduleManager.GetExitHoldMinutes();
                return minutes <= 0
                    ? TimeSpan.FromDays(3650)
                    : TimeSpan.FromMinutes(minutes);
            }
        }

        private static long NowUnix()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static void AllowExit()
        {
            long until = NowUnix() + (long)Window.TotalSeconds;
            string text = until.ToString();

            // ① 注册表（管理员通道）
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(KeyPath))
                {
                    if (key != null)
                        key.SetValue(ValueName, text, RegistryValueKind.String);
                }
            }
            catch (Exception)
            {
            }

            // ② 公共目录文件（普通权限通道）
            try
            {
                string full = Path.GetFullPath(MarkerPath);
                string dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(full, text);
            }
            catch (Exception)
            {
            }
        }

        public static bool IsExitAllowed()
        {
            long now = NowUnix();

            // ① 注册表
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyPath, false))
                {
                    if (key != null)
                    {
                        long until;
                        if (long.TryParse(key.GetValue(ValueName) as string, out until) && now < until)
                            return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            // ② 公共目录文件
            try
            {
                string full = Path.GetFullPath(MarkerPath);
                if (File.Exists(full))
                {
                    long until;
                    if (long.TryParse(File.ReadAllText(full).Trim(), out until) && now < until)
                        return true;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

    /// <summary>统一的退出入口：先放行，再从底层到高层逐级硬杀。</summary>
    internal static class AppExit
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern void ExitProcess(uint exitCode);

        public static void Quit()
        {
            // 1. 告诉服务"这次是人为退出，别再拉起来"
            ExitGate.AllowExit();

            // 2. TerminateProcess(伪句柄) —— 不创建句柄、不触发 ObRegisterCallbacks
            try
            {
                TerminateProcess(GetCurrentProcess(), 0);
            }
            catch (Exception)
            {
            }

            // 3. ExitProcess
            try
            {
                ExitProcess(0);
            }
            catch (Exception)
            {
            }

            // 4. 最后兜底
            try
            {
                Environment.Exit(0);
            }
            catch (Exception)
            {
            }
        }
    }
}
