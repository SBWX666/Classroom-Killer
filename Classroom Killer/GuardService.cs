using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace Classroom_Killer
{
    /// <summary>
    /// 后台守护服务（LocalSystem 运行）。
    /// - 保护自己（任务管理器杀不掉）
    /// - 守护内核驱动服务
    /// - 守护界面进程（被杀 5 秒内拉起）
    /// </summary>
    internal sealed class GuardService : ServiceBase
    {
        public const string Name = "ClassKillerGuard";

        public GuardService()
        {
            ServiceName = Name;

            CanStop = false;
            CanPauseAndContinue = false;
            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = true;
            CanShutdown = true;
        }

        protected override void OnStart(string[] args)
        {
            StartupRegistrar.Ensure();
            DriverWatchdog.Start();

            // 等驱动就绪后把服务进程自己也挂进保护名单
            StartSelfProtect();

            // 界面守护
            StartUiWatchdog();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                case SessionChangeReason.ConsoleConnect:
                case SessionChangeReason.RemoteConnect:
                    SessionLauncher.Launch(changeDescription.SessionId);
                    break;
            }
        }

        private static void StartSelfProtect()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (ProcessProtect.ProtectSelf())
                            return;
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(2000);
                }
            })
            {
                IsBackground = true,
                Name = "SelfProtect"
            };
            thread.Start();
        }

        private static void StartUiWatchdog()
        {
            var thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "UiWatchdog"
            };
            thread.Start();
        }

        private static void Loop()
        {
            while (true)
            {
                try
                {
                    if (!SessionLauncher.IsRunning() && !ExitGate.IsExitAllowed())
                    {
                        int sessionId = SessionLauncher.ActiveSessionId;
                        if (sessionId > 0)
                            SessionLauncher.Launch(sessionId);
                    }
                }
                catch (Exception)
                {
                }

                // 1 秒一轮：即使界面被强杀，也几乎立刻回来
                Thread.Sleep(1000);
            }
        }

        protected override void OnShutdown()
        {
        }
    }

    [RunInstaller(true)]
    public sealed class GuardServiceInstaller : Installer
    {
        public GuardServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = GuardService.Name,
                DisplayName = "Classroom Killer Guard",
                Description = "Keeps the Classroom Killer kernel driver loaded and running.",
                StartType = ServiceStartMode.Automatic,
                DelayedAutoStart = false
            };

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }

    /// <summary>-install / -uninstall / -service</summary>
    internal static class ServiceSetup
    {
        private const string WatchTaskName = "ClassKillerGuardWatch";

        private static readonly string LogPath = BuildLogPath();

        public static bool Handle(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            string command = args[0].TrimStart('-', '/').ToLowerInvariant();

            switch (command)
            {
                case "install":
                    Report.Run("Install", Install);
                    return true;

                case "uninstall":
                    // 默认要密码；安装包/自动化传 -force 跳过
                    if (!HasFlag(args, "force") && !ConfirmUninstall())
                        return true;

                    bool fromInstaller = HasFlag(args, "force");
                    Report.Run("Uninstall", () => Uninstall(fromInstaller));

                    // 用户手动卸载：驱动服务清完后，交给 msiexec 删文件。
                    // 先延迟启动，等本进程退出释放 exe 的文件锁。
                    if (!fromInstaller)
                        LaunchMsiCleanup();
                    return true;

                case "service":
                    RunService();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>判断命令行里是否带了某个开关，例如 -force / /force。</summary>
        public static bool HasFlag(string[] args, string flag)
        {
            if (args == null) return false;

            foreach (string raw in args)
            {
                if (string.IsNullOrEmpty(raw)) continue;

                string value = raw.TrimStart('-', '/').ToLowerInvariant();
                if (value == flag) return true;
            }
            return false;
        }

        /// <summary>卸载前的密码确认。没有交互桌面时直接拒绝。</summary>
        private static bool ConfirmUninstall()
        {
            if (!Environment.UserInteractive)
                return false;

            return PasswordPrompt.Confirm(null, "Uninstall",
                "Enter the password to uninstall Classroom Killer:", "Uninstall");
        }

        public static void RunService()
        {
            ServiceBase.Run(new GuardService());
        }

        private static string ExecutablePath
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        /// <summary>
        /// 把 exe 收敛到 Program Files\Classroom Killer\，服务永远指向这个规范路径。
        /// 避免 in 哪儿运行就注册哪儿的路径（之前出现过 SysWOW64\_cache_* 这种）。
        /// </summary>
        private static string Canonicalize()
        {
            string target = Path.Combine(
                ProgramFolders.Native,
                "Classroom Killer",
                "Classroom Killer.exe");

            string current = ExecutablePath;

            try
            {
                if (string.Equals(Path.GetFullPath(current), Path.GetFullPath(target),
                                  StringComparison.OrdinalIgnoreCase))
                {
                    return target;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target));

                bool needCopy = true;
                if (File.Exists(target))
                {
                    try
                    {
                        // 不能只比长度：驱动和 exe 每次都是同样大小，内容却不同
                        needCopy = !SameContent(target, current);
                    }
                    catch (Exception)
                    {
                        needCopy = true;
                    }
                }

                if (needCopy)
                {
                    File.Copy(current, target, true);

                    // File.Copy 会把源文件的隐藏/系统/只读属性一起带过来。
                    // 之前从 _cache_ 副本复制时把隐藏属性传染到了 Program Files，
                    // 结果 exe 在资源管理器里隐身、属性页还改不回来。
                    try
                    {
                        File.SetAttributes(target, FileAttributes.Normal);
                    }
                    catch (Exception)
                    {
                    }
                }

                return target;
            }
            catch (Exception ex)
            {
                Log("canonicalize failed: " + ex.Message);
                return current;
            }
        }

        /// <summary>按内容比较两个文件，长度一样但内容不同也算不同。</summary>
        private static bool SameContent(string a, string b)
        {
            try
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] ha, hb;

                    using (var fs = File.OpenRead(a))
                        ha = sha.ComputeHash(fs);

                    using (var fs = File.OpenRead(b))
                        hb = sha.ComputeHash(fs);

                    if (ha.Length != hb.Length) return false;

                    for (int i = 0; i < ha.Length; i++)
                    {
                        if (ha[i] != hb[i]) return false;
                    }
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        private static string Install()
        {
            if (!IsAdministrator())
                return "FAILED: run this from an elevated (Run as administrator) prompt.";

            return Provision(exe => LaunchUi(exe));
        }

        /// <summary>
        /// 首次运行时的静默补齐：装驱动 + 建服务 + 写 Run 项，不弹窗、不拉界面。
        /// 安装包只需要复制文件，剩下的由这里完成。
        /// </summary>
        internal static string ProvisionSilently()
        {
            return Provision(null);
        }

        /// <summary>
        /// 共享的配置流程。
        /// onDone 为 null 表示静默模式（首次运行自检），否则安装完成后拉起界面。
        /// </summary>
        private static string Provision(Action<string> onDone)
        {
            // ① exe 收敛到规范路径
            string exe = Canonicalize();
            Log("exe = " + exe);

            if (!File.Exists(exe))
                return "FAILED: cannot place exe at " + exe;

            // ② 驱动：释放 .sys + 注册内核服务
            string driverResult = DriverInstaller.Install();
            Log("driver: " + driverResult);

            // 驱动写不进去（旧驱动还占着文件）就别再往下装了，
            // 否则会显示"服务 OK"但驱动其实是老版本，非常容易误判。
            if (driverResult.StartsWith("FAILED", StringComparison.Ordinal))
                return driverResult;

            // ③ 旧服务处理
            //
            // 这里有两个坑，都是"服务明明装过却越装越没有"的元凶：
            //  1) sc delete 之后服务不会立刻消失，注册表项要等进程真正退出。
            //     以前只 Sleep(600) 就去 create，抢不到 → 错误 1072。
            //  2) 守护服务是 CanStop=false 的，sc delete 之后它根本停不下来，
            //     注册表项会一直挂着 marked-for-deletion，create 永远失败。
            // 所以：已经在跑、且指向同一个 exe 的服务，直接复用，不要删。
            string createOutput;

            if (ServiceExists() && GuardRunning() && BinPathMatches(exe))
            {
                createOutput = "reused: guard service already running";
                Log(createOutput);
            }
            else
            {
                RunSc("delete {0}", GuardService.Name);

                // 等注册表项真的消失，最多 12 秒
                for (int i = 0; i < 24 && ServiceExists(); i++)
                    Thread.Sleep(500);

                // ④ 建服务，binPath 指向规范路径
                createOutput = RunSc(
                    "create {0} binPath= \"\\\"{1}\\\"\" type= own start= auto DisplayName= \"Classroom Killer Guard\"",
                    GuardService.Name, exe);
                Log(createOutput);

                // 1072 = 已被标记删除、还没走干净，等两秒再建一次
                if (createOutput.IndexOf("1072", StringComparison.Ordinal) >= 0)
                {
                    Thread.Sleep(2000);
                    createOutput = RunSc(
                        "create {0} binPath= \"\\\"{1}\\\"\" type= own start= auto DisplayName= \"Classroom Killer Guard\"",
                        GuardService.Name, exe);
                    Log("create retry: " + createOutput);
                }

                if (!ServiceExists())
                    return "FAILED: service could not be created.\r\n\r\n" + createOutput;
            }

            // ⑤ 启动优先级 + 崩溃自愈
            RunSc("config {0} start= auto depend= \"\"", GuardService.Name);
            RunSc("failure {0} reset= 0 actions= restart/1000/restart/2000/restart/5000",
                  GuardService.Name);

            // ⑥ 自启动项
            Log("run key: " + (StartupRegistrar.Ensure() ? "ok" : "FAILED"));

            // ⑦ 计划任务兜底：每分钟检查一次，服务没了就 net start
            string taskOutput = RunCmd("schtasks",
                "/create /tn \"{0}\" /tr \"net start {1}\" /sc minute /mo 1 /ru SYSTEM /rl HIGHEST /f",
                WatchTaskName, GuardService.Name);
            Log("schtasks: " + taskOutput);

            // ⑧ 启动（START_PENDING 要等一下，不能查到不是 RUNNING 就判失败）
            string startOutput = RunSc("start {0}", GuardService.Name);
            string query = RunSc("query {0}", GuardService.Name);

            for (int i = 0; i < 20 && query.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) < 0; i++)
            {
                Thread.Sleep(1000);
                query = RunSc("query {0}", GuardService.Name);
            }

            if (query.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) < 0)
                return driverResult + "\r\n\r\nFAILED to start guard service.\r\n\r\n" + startOutput;

            // 装完直接把界面拉起来。-install 是在用户会话里跑的，
            // 这里用 Process.Start 比等服务 watchdog 走 CreateProcessAsUser 更快。
            // 静默模式（首次运行自检）不拉，因为界面本来就在跑。
            if (onDone != null)
                onDone(exe);

            return driverResult + "\r\n\r\nOK. Guard RUNNING.\r\n\r\n"
                 + "Service exe: " + exe + "\r\n"
                 + "Exe build:   " + File.GetLastWriteTime(exe).ToString("HH:mm:ss") + "\r\n\r\n"
                 + query;
        }

        private static void LaunchUi(string exe)
        {
            try
            {
                var psi = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(exe)
                };
                Process.Start(psi);
                Log("startup ui: started " + exe);
            }
            catch (Exception ex)
            {
                Log("startup ui: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// 卸载。
        ///
        /// fromInstaller = true 表示由 MSI 卸载流程触发（-force），
        /// 此时只清驱动和服务，文件由 MSI 自己删，不能再回头调 msiexec（会递归）。
        ///
        /// fromInstaller = false 表示用户点桌面的卸载快捷方式，
        /// 清完驱动服务后主动调 msiexec /x，让 MSI 把文件、快捷方式、注册表一起删掉。
        /// </summary>
        private static string Uninstall(bool fromInstaller)
        {
            if (!IsAdministrator())
                return "FAILED: this command must run from an elevated prompt.";

            var report = new System.Text.StringBuilder();

            // ① 计划任务兜底
            report.AppendLine("task: " + RunCmd("schtasks", "/delete /tn \"{0}\" /f", WatchTaskName).Trim());

            // ② 守护服务
            RunSc("config {0} start= demand", GuardService.Name);
            RunSc("stop {0}", GuardService.Name);

            string guardOut = RunSc("delete {0}", GuardService.Name);
            report.AppendLine("guard: " + guardOut.Trim());
            Log("sc delete guard: " + guardOut);

            // ③ 内核驱动服务 —— 之前漏掉的就是这一步
            RunSc("config {0} start= demand", DriverInstaller.ServiceName);
            RunSc("stop {0}", DriverInstaller.ServiceName);   // DriverUnload=NULL，会返回 1052，正常

            string driverOut = RunSc("delete {0}", DriverInstaller.ServiceName);
            report.AppendLine("driver: " + driverOut.Trim());
            Log("sc delete driver: " + driverOut);

            // ④ 自启动项
            StartupRegistrar.Remove();
            report.AppendLine("run key: removed");

            // ⑤ 驱动文件被已加载的驱动占用，此刻删不掉。
            //    MOVEFILE_DELAY_UNTIL_REBOOT 在这台机器上没生效（重启后文件还在），
            //    改用一次性计划任务：开机时以 SYSTEM 身份删除，删完自动注销自己。
            string driverFile = DriverInstaller.TargetPath.Replace(@"\Sysnative\", @"\System32\");
            string cleanupTask = "ClassroomKillerCleanup";

            string cleanupCmd = string.Format(
                "cmd /c del /f /q \"{0}\" & schtasks /delete /tn \"{1}\" /f",
                driverFile, cleanupTask);

            string taskOut = RunCmd("schtasks",
                "/create /tn \"{0}\" /tr \"{1}\" /sc onstart /ru SYSTEM /rl HIGHEST /f",
                cleanupTask, cleanupCmd);

            report.AppendLine("cleanup task: " + taskOut.Trim());
            Log("cleanup task for " + driverFile + ": " + taskOut.Trim());

            // ⑥ 不用 InstallHelper 反注册：
            //    它内部会先 ServiceController.Stop()，而我们的服务 CanStop=false，
            //    停止请求会挂到超时，整个卸载卡死。
            //    服务本来就是 sc create 建的，sc delete 已经删干净了。

            return "Uninstalled.\r\n\r\n" + report + "\r\nReboot to finish removing the driver.";
        }

        /// <summary>
        /// 用户点快捷方式卸载时：清完驱动和服务后，延迟启动 msiexec 删文件。
        ///
        /// 不能同步等 msiexec —— 它会试图删除正在运行的 Classroom Killer.exe，
        /// 文件被自己占用导致卸载失败。所以先返回让本进程退出释放锁，
        /// 5 秒后由独立的 cmd 进程接管。
        /// </summary>
        private static void LaunchMsiCleanup()
        {
            string code = FindProductCode();
            if (string.IsNullOrEmpty(code))
            {
                Log("msi cleanup: product code not found, files must be removed manually");
                return;
            }

            // 先把其他 Classroom Killer 实例结束掉。
            // 界面的托盘进程一直占着 exe，MSI 删不掉，会弹
            // 「安装程序无法自动关闭所有请求的应用程序」。
            int self = Process.GetCurrentProcess().Id;
            int killed = 0;

            try
            {
                foreach (Process p in Process.GetProcessesByName("Classroom Killer"))
                {
                    if (p.Id == self) continue;

                    try
                    {
                        p.Kill();
                        killed++;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }

            Log("msi cleanup: killed " + killed + " instance(s)");

            try
            {
                // 不能用 timeout /t 5 —— 它需要控制台输入，
                // CreateNoWindow 下会直接报错，& 后面的 msiexec 永远不执行。
                // ping -n 6 约等于 5 秒延时，无控制台也能跑。
                // msiexec 后面再补一条 rd：MSI 不删非空目录，install.log
                // 留在里面会导致程序文件夹残留。
                string installDir = Path.GetDirectoryName(ExecutablePath);

                string cmd = "/c ping -n 6 127.0.0.1 >nul"
                           + " & msiexec /x " + code + " /qb"
                           + " & rd /s /q \"" + installDir + "\""
                           + " & shutdown /r /t 15 /c \"Classroom Killer uninstalled, rebooting in 15s\"";

                Process.Start(new ProcessStartInfo("cmd.exe", cmd)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                Log("msi cleanup: scheduled msiexec /x " + code + " + rmdir + reboot");
            }
            catch (Exception ex)
            {
                Log("msi cleanup: " + ex.Message);
            }
        }

        /// <summary>
        /// 从注册表里找出本产品的 ProductCode，用来调 msiexec /x。
        /// 遍历卸载表匹配 DisplayName，比硬编码 GUID 稳。
        /// </summary>
        private static string FindProductCode()
        {
            // 关键：本程序是 32 位进程，直接访问 HKLM\SOFTWARE 会被 WOW64
            // 重定向到 WOW6432Node，看不到 64 位安装的卸载项。
            // 必须用 OpenBaseKey 显式指定 RegistryView 两个视图都扫一遍。
            Microsoft.Win32.RegistryHive[] hives =
            {
                Microsoft.Win32.RegistryHive.LocalMachine,
                Microsoft.Win32.RegistryHive.CurrentUser
            };

            Microsoft.Win32.RegistryView[] views =
            {
                Microsoft.Win32.RegistryView.Registry64,
                Microsoft.Win32.RegistryView.Registry32
            };

            const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            foreach (Microsoft.Win32.RegistryHive hive in hives)
            {
                foreach (Microsoft.Win32.RegistryView view in views)
                {
                    try
                    {
                        using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(hive, view))
                        {
                            string found = SearchUninstallRoot(baseKey, uninstallPath);
                            if (!string.IsNullOrEmpty(found))
                            {
                                Log(string.Format("find product code: {0} ({1} {2})",
                                    found, hive, view));
                                return found;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("find product code (" + hive + "/" + view + "): " + ex.Message);
                    }
                }
            }

            // 最后兜底：Advanced Installer 项目里的固定 ProductCode
            const string knownProductCode = "{F064B1AD-85D3-473D-AC76-A3E1C96E053C}";

            try
            {
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(uninstallPath + @"\" + knownProductCode))
                {
                    if (key != null)
                    {
                        Log("find product code: using built-in fallback " + knownProductCode);
                        return knownProductCode;
                    }
                }
            }
            catch (Exception)
            {
            }

            Log("find product code: no uninstall entry matched");
            return null;
        }

        private static string SearchUninstallRoot(Microsoft.Win32.RegistryKey hive, string root)
        {
            try
            {
                using (var key = hive.OpenSubKey(root))
                {
                    if (key == null) return null;

                    string fallback = null;

                    foreach (string sub in key.GetSubKeyNames())
                    {
                        if (!sub.StartsWith("{", StringComparison.Ordinal))
                            continue;

                        using (var item = key.OpenSubKey(sub))
                        {
                            if (item == null) continue;

                            string name = item.GetValue("DisplayName") as string;
                            string location = item.GetValue("InstallLocation") as string;
                            string uninstall = item.GetValue("UninstallString") as string;
                            string product = item.GetValue("ProductName") as string;

                            if (string.Equals(name, "Classroom Killer", StringComparison.OrdinalIgnoreCase))
                                return sub;

                            if (Contains( location, "Classroom Killer") ||
                                Contains(uninstall, "Classroom Killer") ||
                                Contains(product, "Classroom Killer"))
                            {
                                if (fallback == null)
                                    fallback = sub;
                            }
                        }
                    }

                    return fallback;
                }
            }
            catch (Exception ex)
            {
                Log("find product code (" + root + "): " + ex.Message);
                return null;
            }
        }

        private static bool Contains(string text, string part)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>调用 msiexec 完成文件清理。</summary>
        private static void RunMsiUninstall(bool fromInstaller)
        {
            if (fromInstaller)
                return;   // MSI 自己就在卸载，不能递归

            string code = FindProductCode();
            if (string.IsNullOrEmpty(code))
            {
                Log("msi uninstall: product code not found, files must be removed manually");
                return;
            }

            try
            {
                var psi = new ProcessStartInfo("msiexec.exe",
                    string.Format("/x {0} /qb", code))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(180000);
                }

                Log("msi uninstall: msiexec /x " + code);
            }
            catch (Exception ex)
            {
                Log("msi uninstall: " + ex.Message);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        /// <summary>驱动还在内存里时删不掉文件，登记到重启时清理。</summary>
        private static bool ScheduleDeleteOnReboot(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return false;

                // 先试着直接删（驱动没加载时能成功）
                try
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    return false;
                }
                catch (Exception)
                {
                    // IOException / UnauthorizedAccessException 都算删不掉，
                    // 之前只 catch(IOException)，"访问被拒绝" 是后者，直接冒泡了
                }

                // 删不掉就登记重启后删除。
                // MOVEFILE_DELAY_UNTIL_REBOOT 只是把路径字符串写进注册表，
                // 真正的删除由 64 位 Session Manager 重启时执行 —— 它不认识
                // Sysnative 别名，必须换成真实的 System32 路径。
                string realPath = path.Replace(@"\Sysnative\", @"\System32\");

                bool queued = MoveFileEx(realPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);

                Log("schedule delete: " + realPath + " -> " + (queued ? "queued" : "FAILED"));
                return queued;
            }
            catch (Exception ex)
            {
                Log("schedule delete: " + ex.Message);
                return false;
            }
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------
        internal static bool ServiceExists()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + GuardService.Name))
                {
                    return key != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>守护服务当前是否处于 RUNNING。</summary>
        private static bool GuardRunning()
        {
            string output = RunSc("query {0}", GuardService.Name);
            return output.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>已注册服务的 binPath 是否就是我们要的这个 exe。</summary>
        private static bool BinPathMatches(string exe)
        {
            string output = RunSc("qc {0}", GuardService.Name);
            return output.IndexOf(exe, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string RunSc(string format, params object[] args)
        {
            return RunCmd("sc.exe", format, args);
        }

        private static string RunCmd(string fileName, string format, params object[] args)
        {
            string arguments = string.Format(format, args);

            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
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
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit(8000);
                    return stdout + stderr;
                }
            }
            catch (Exception ex)
            {
                return fileName + " failed: " + ex.Message;
            }
        }

        private static string BuildLogPath()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "ClassroomKiller");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "install.log");
            }
            catch (Exception)
            {
                return Path.Combine(Path.GetTempPath(), "ClassroomKiller_install.log");
            }
        }

        internal static void Log(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine;

            // 主路径：C:\ProgramData\ClassroomKiller\install.log
            if (TryAppend(LogPath, line))
                return;

            // 降级 1：%TEMP%
            string temp = Path.Combine(Path.GetTempPath(), "ClassroomKiller_install.log");
            if (TryAppend(temp, line))
                return;

            // 降级 2：exe 目录
            try
            {
                string local = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "install.log");
                TryAppend(local, line);
            }
            catch (Exception)
            {
            }
        }

        private static bool TryAppend(string path, string text)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(path, text);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal static string LogPathValue
        {
            get { return LogPath; }
        }
    }

    internal static class Report
    {
        public static void Run(string title, Func<string> action)
        {
            string result;
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                result = "FAILED: " + ex;
                ServiceSetup.Log(result);
            }

            MessageBox.Show(result + "\r\n\r\nLog: " + ServiceSetup.LogPathValue,
                title, MessageBoxButtons.OK,
                result.StartsWith("OK", StringComparison.Ordinal)
                    ? MessageBoxIcon.Information
                    : MessageBoxIcon.Error);
        }
    }
}
