using System;
using System.Diagnostics;
using System.Threading;

namespace Classroom_Killer
{
    /// <summary>
    /// 驱动看门狗：定时检查内核驱动服务的注册表项和运行状态。
    /// 服务被 sc delete / sc stop 后，几秒内自动重建并重新启动，
    /// 顺带重新把本进程挂回保护名单。
    /// </summary>
    internal static class DriverWatchdog
    {
        private const string ServiceName = "ClassKillerProtect";
        private const string ServiceRegistryPath =
            @"SYSTEM\CurrentControlSet\Services\" + ServiceName;
        private const string DriverBinPath =
            @"System32\drivers\ClassKillerProtect.sys";
        private const int IntervalMs = 8000;

        private static int _started;

        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            var thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "DriverWatchdog"
            };
            thread.Start();
        }

        private static void Loop()
        {
            while (true)
            {
                try
                {
                    Ensure();
                }
                catch (Exception)
                {
                    // 任何异常都不允许打断看门狗
                }

                Thread.Sleep(IntervalMs);
            }
        }

        private static void Ensure()
        {
            // 自启动项跟着设置走，被删了立刻写回；设置里关掉就不再写
            if (ScheduleManager.IsAutoStartEnabled())
                StartupRegistrar.Ensure();
            else
                StartupRegistrar.Remove();

            if (!ServiceExists())
            {
                RunSc("create {0} type= kernel start= auto binPath= \"{1}\"",
                      ServiceName, DriverBinPath);
                RunSc("start {0}", ServiceName);
                ProcessProtect.ProtectSelf();
                return;
            }

            if (!IsRunning())
            {
                RunSc("start {0}", ServiceName);
                ProcessProtect.ProtectSelf();
            }
        }

        private static bool ServiceExists()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(ServiceRegistryPath))
                {
                    return key != null;
                }
            }
            catch (Exception)
            {
                // 读不到就当作存在，避免在权限不足时反复重建
                return true;
            }
        }

        private static bool IsRunning()
        {
            string output = RunSc("query {0}", ServiceName);
            return output.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string RunSc(string format, params object[] args)
        {
            string arguments = string.Format(format, args);

            try
            {
                var psi = new ProcessStartInfo("sc.exe", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process p = Process.Start(psi))
                {
                    string stdout = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    return stdout;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
