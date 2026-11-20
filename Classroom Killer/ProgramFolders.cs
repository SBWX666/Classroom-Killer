using System;

namespace Classroom_Killer
{
    /// <summary>
    /// 32 位进程里 Environment.SpecialFolder.ProgramFiles 指向的是
    /// "Program Files (x86)"，会让服务绑定到另一个副本（曾经出现过两个安装目录）。
    /// 这里优先读 ProgramW6432，它永远是 64 位的 "C:\Program Files"。
    /// </summary>
    internal static class ProgramFolders
    {
        public static string Native
        {
            get
            {
                string path = Environment.GetEnvironmentVariable("ProgramW6432");

                if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
                    path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                return path;
            }
        }
    }
}
