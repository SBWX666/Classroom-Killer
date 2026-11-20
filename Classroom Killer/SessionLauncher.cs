using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Classroom_Killer
{
    /// <summary>
    /// 在已登录用户的交互桌面上拉起 Classroom Killer。
    /// 服务跑在 Session 0，直接 Process.Start 出来的窗口用户在桌面上看不到，
    /// 所以必须借用户 token 走 CreateProcessAsUser。
    /// </summary>
    internal static class SessionLauncher
    {
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const string ProcessName = "Classroom Killer";

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);

        [DllImport("userenv.dll", SetLastError = true)]
        private static extern bool DestroyEnvironmentBlock(IntPtr environment);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUser(
            IntPtr token,
            string applicationName,
            StringBuilder commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        // --- 权限提升：CreateProcessAsUser 需要显式启用这两个特权 ---

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LookupPrivilegeValue(string systemName, string name, out long luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges,
            ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenPrivileges
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;

        /// <summary>
        /// LocalSystem 拥有这些特权，但默认是 disabled 状态。
        /// 不显式启用的话 CreateProcessAsUser 直接返回 1314 (ERROR_PRIVILEGE_NOT_HELD)，
        /// 表现就是"服务在跑但界面永远拉不起来"。
        /// </summary>
        private static void EnablePrivilege(string name)
        {
            IntPtr token;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                return;

            try
            {
                long luid;
                if (!LookupPrivilegeValue(null, name, out luid))
                    return;

                var privileges = new TokenPrivileges
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                AdjustTokenPrivileges(token, false, ref privileges, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(token);
            }
        }

        private static void EnableLaunchPrivileges()
        {
            EnablePrivilege("SeAssignPrimaryTokenPrivilege");
            EnablePrivilege("SeIncreaseQuotaPrivilege");
            EnablePrivilege("SeImpersonatePrivilege");
        }

        /// <summary>
        /// 当前活动控制台会话 ID，没有用户登录时返回 -1。
        /// </summary>
        public static int ActiveSessionId
        {
            get
            {
                uint id = WTSGetActiveConsoleSessionId();
                return id == 0xFFFFFFFF ? -1 : (int)id;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>
        /// 当前是否已经有界面实例在跑。
        /// 关键：服务进程自己的名字也叫 "Classroom Killer"，但它跑在 Session 0，
        /// 必须排掉，否则永远认为界面已经在了。
        /// </summary>
        public static bool IsRunning()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName(ProcessName))
                {
                    try
                    {
                        if (p.SessionId != 0)
                            return true;
                    }
                    catch (Exception)
                    {
                        // 拿不到 SessionId 的算在跑，宁可少拉一次
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>在指定会话的交互桌面上启动 Classroom Killer。</summary>
        public static bool Launch(int sessionId)
        {
            if (sessionId < 1)
            {
                ServiceSetup.Log("launch: invalid session " + sessionId);
                return false;
            }

            if (IsRunning())
                return true;

            string exe = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                ServiceSetup.Log("launch: exe missing " + exe);
                return false;
            }

            EnableLaunchPrivileges();

            IntPtr token = IntPtr.Zero;
            IntPtr environment = IntPtr.Zero;

            try
            {
                if (!WTSQueryUserToken((uint)sessionId, out token) || token == IntPtr.Zero)
                {
                    ServiceSetup.Log("launch: WTSQueryUserToken failed " +
                                     Marshal.GetLastWin32Error());
                    return false;
                }

                CreateEnvironmentBlock(out environment, token, false);

                var startup = new StartupInfo
                {
                    cb = Marshal.SizeOf(typeof(StartupInfo)),
                    lpDesktop = @"winsta0\default"
                };

                var commandLine = new StringBuilder("\"" + exe + "\"");
                ProcessInformation info;

                bool ok = CreateProcessAsUser(
                    token,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CREATE_UNICODE_ENVIRONMENT,
                    environment,
                    Path.GetDirectoryName(exe),
                    ref startup,
                    out info);

                if (ok)
                {
                    CloseHandle(info.hThread);
                    CloseHandle(info.hProcess);
                    ServiceSetup.Log("launch: ok session " + sessionId + " pid " + info.dwProcessId);
                }
                else
                {
                    ServiceSetup.Log("launch: CreateProcessAsUser failed " +
                                     Marshal.GetLastWin32Error());
                }

                return ok;
            }
            catch (Exception ex)
            {
                ServiceSetup.Log("launch: " + ex);
                return false;
            }
            finally
            {
                if (environment != IntPtr.Zero)
                    DestroyEnvironmentBlock(environment);

                if (token != IntPtr.Zero)
                    CloseHandle(token);
            }
        }
    }
}
