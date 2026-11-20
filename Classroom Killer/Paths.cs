using System;
using System.IO;

namespace Classroom_Killer
{
    /// <summary>
    /// 数据文件（Date\*.config）的定位。
    ///
    /// 以前直接用 AppDomain.BaseDirectory，也就是"exe 所在目录"。
    /// 实测踩过坑：被服务拉起、或被别的进程用奇怪的工作目录启动时，
    /// 拿到的可能是 C:\Windows\SysWOW64 之类的地方，于是登录界面报
    /// "Cannot read the password file"，而文件明明在 Program Files 里躺着。
    ///
    /// 现在改成按候选目录逐个探测，谁下面有 Date\DateBase.config 就用谁。
    /// </summary>
    internal static class Paths
    {
        private static string _root;

        /// <summary>exe 所在目录（仅作参考，不保证是数据目录）。</summary>
        public static string BaseDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>真正存放 Date\ 的目录。</summary>
        public static string DataRoot
        {
            get
            {
                if (_root != null)
                    return _root;

                string[] candidates =
                {
                    BaseDirectory,
                    SafeCombine(ProgramFolders.Native, "Classroom Killer"),
                    SafeCombine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                "Classroom Killer"),
                    SafeCombine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                "Classroom Killer"),
                    Environment.CurrentDirectory
                };

                foreach (string dir in candidates)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(dir))
                            continue;

                        if (File.Exists(Path.Combine(dir, "Date", "DateBase.config")))
                        {
                            _root = dir;
                            return _root;
                        }
                    }
                    catch (Exception)
                    {
                        // 单个候选目录读不了不影响继续试下一个
                    }
                }

                // 一个都没找到：还是回到 exe 目录，
                // 后面的错误提示会把这里拼出来的完整路径显示出来。
                _root = BaseDirectory;
                return _root;
            }
        }

        /// <summary>&lt;数据目录&gt;\Date\DateBase.config</summary>
        public static string ConfigFile
        {
            get { return Combine("Date", "DateBase.config"); }
        }

        /// <summary>&lt;数据目录&gt;\Date\settings.config</summary>
        public static string SettingsFile
        {
            get { return Combine("Date", "settings.config"); }
        }

        public static string ProcessCheck
        {
            get { return Combine("windowsprocesscheck.exe"); }
        }

        public static string ProcessCheckBeta
        {
            get { return Combine("windowsprocesscheckbeta.exe"); }
        }

        private static string SafeCombine(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return null;

            try
            {
                return Path.Combine(a, b);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Combine(params string[] parts)
        {
            string path = DataRoot;
            foreach (string part in parts)
                path = Path.Combine(path, part);
            return path;
        }
    }
}
