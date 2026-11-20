using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Classroom_Killer
{
    /// <summary>
    /// 内核驱动 ClassKillerProtect 的用户态入口。
    /// 驱动通过 ObRegisterCallbacks 摘掉外部 OpenProcess 的 PROCESS_TERMINATE 位，
    /// 因此任务管理器 / taskkill / Process Hacker 等一切用户态方式都杀不掉被保护的进程。
    /// </summary>
    internal static class ProcessProtect
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        private const uint IOCTL_PROTECT_PROCESS = 0x222000;
        private const uint IOCTL_UNPROTECT_PROCESS = 0x222004;

        private const string DevicePath = @"\\.\ClassKillerProtect";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            uint nInBufferSize,
            byte[] lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static readonly IntPtr InvalidHandle = new IntPtr(-1);

        /// <summary>最近一次失败的错误码：2=找不到设备，5=拒绝访问，其他见 Win32 错误码。</summary>
        public static int LastError { get; private set; }

        public static bool IsDeviceAvailable
        {
            get { return LastError == 0; }
        }

        private static bool SendIoctl(uint ioctl, uint pid)
        {
            IntPtr h = CreateFileW(
                DevicePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero);

            if (h == InvalidHandle || h == IntPtr.Zero)
            {
                LastError = Marshal.GetLastWin32Error();
                return false;
            }

            try
            {
                byte[] inBuf = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(pid), 0, inBuf, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(0u), 0, inBuf, 4, 4);

                uint bytesReturned;
                bool ok = DeviceIoControl(h, ioctl, inBuf, (uint)inBuf.Length,
                                          null, 0, out bytesReturned, IntPtr.Zero);

                LastError = ok ? 0 : Marshal.GetLastWin32Error();
                return ok;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public static bool Protect(uint pid)
        {
            if (pid == 0) return false;
            return SendIoctl(IOCTL_PROTECT_PROCESS, pid);
        }

        public static bool Unprotect(uint pid)
        {
            if (pid == 0) return false;
            return SendIoctl(IOCTL_UNPROTECT_PROCESS, pid);
        }

        /// <summary>保护当前进程，成功返回 true。</summary>
        public static bool ProtectSelf()
        {
            try
            {
                return Protect((uint)Process.GetCurrentProcess().Id);
            }
            catch
            {
                return false;
            }
        }
    }
}
