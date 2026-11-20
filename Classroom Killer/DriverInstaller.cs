using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Classroom_Killer
{
    /// <summary>
    /// 驱动的一键部署：驱动已作为嵌入资源编译进 exe，
    /// 安装时释放到 System32\drivers 并注册成内核服务。
    /// 这样不会出现"服务建好了但 .sys 文件没拷过去"的 0x2 错误。
    /// </summary>
    internal static class DriverInstaller
    {
        public const string ServiceName = "ClassKillerProtect";
        private const string ResourceName = "ClassKillerProtect.sys";
        private const string BinPath = @"System32\drivers\ClassKillerProtect.sys";

        /// <summary>
        /// 必须是真正的 C:\Windows\System32\drivers。
        ///
        /// 这里有两个坑：
        ///  1) Environment.SpecialFolder.System 在 32 位进程里直接返回 SysWOW64
        ///  2) 即使拼出 "C:\Windows\System32"，32 位进程访问它也会被 WOW64
        ///     文件系统重定向到 SysWOW64，写"成功"但文件落错了地方。
        ///
        /// 32 位进程要用 Sysnative 别名才能摸到真正的 64 位 System32。
        /// </summary>
        public static string TargetPath
        {
            get
            {
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                if (string.IsNullOrEmpty(systemRoot))
                    systemRoot = @"C:\Windows";

                string folder = Environment.Is64BitProcess ? "System32" : "Sysnative";

                return Path.Combine(systemRoot, folder, "drivers", "ClassKillerProtect.sys");
            }
        }

        /// <summary>释放驱动文件并确保服务在运行。</summary>
        public static string Install()
        {
            try
            {
                // ① 释放 .sys
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream source = asm.GetManifestResourceStream(ResourceName))
                {
                    if (source == null)
                        return "FAILED: embedded driver resource not found";

                    string target = TargetPath;
                    Directory.CreateDirectory(Path.GetDirectoryName(target));

                    // 文件被已加载的驱动占用时会抛 IOException
                    using (FileStream dest = new FileStream(target, FileMode.Create,
                                                            FileAccess.Write, FileShare.None))
                    {
                        source.CopyTo(dest);
                    }

                    // 确保驱动文件是普通属性，别继承任何隐藏/系统位
                    try
                    {
                        File.SetAttributes(target, FileAttributes.Normal);
                    }
                    catch (Exception)
                    {
                    }
                }

                // 验证文件真的落地在 System32\drivers
                var info = new FileInfo(TargetPath);
                if (!info.Exists || info.Length == 0)
                    return "FAILED: driver file not found after write: " + TargetPath;

                // ② 注册服务（已存在会报 1073，忽略）
                RunSc("create {0} type= kernel start= auto binPath= \"{1}\"",
                      ServiceName, BinPath);

                // ③ 启动
                string startOutput = RunSc("start {0}", ServiceName);
                string query = RunSc("query {0}", ServiceName);

                if (query.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Driver OK: " + TargetPath + "\r\n\r\n" + query;

                return "Driver registered but NOT running.\r\n\r\n" + startOutput;
            }
            catch (IOException ex)
            {
                return "FAILED: cannot write " + TargetPath +
                       "\r\nThe old driver is still loaded and holds the file lock." +
                       "\r\nRun:  sc delete ClassKillerGuard & sc delete ClassKillerProtect  then REBOOT, then run -install again." +
                       "\r\n\r\n" + ex.Message;
            }
            catch (Exception ex)
            {
                return "FAILED: " + ex.Message;
            }
        }

        /// <summary>驱动服务当前是否在跑。</summary>
        public static bool IsRunning()
        {
            string output = RunSc("query {0}", ServiceName);
            return output.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string RunSc(string format, params object[] args)
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", string.Format(format, args))
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
                return "sc.exe failed: " + ex.Message;
            }
        }
    }
}
