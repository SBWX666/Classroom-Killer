using System;
using System.Configuration;
using System.IO;

namespace Classroom_Killer
{
    /// <summary>
    /// 下课时间表。
    ///
    /// 配置写在 .\Date\settings.config：
    ///   LockTimes                "08:45,09:35,10:35,11:25,14:00,14:50"
    ///   LockDurationMinutes      "10"（每个下课点锁定多少分钟）
    ///   MonitorProcesses         "taskmgr.exe,cmd.exe"
    ///   AutoStart                "True"
    ///
    /// 逻辑：当前时间落在 [下课点, 下课点+时长) 区间内就应该锁屏。
    /// 所以重启后只要还在课间，程序一启动就会立刻锁住。
    /// </summary>
    internal static class ScheduleManager
    {
        public const string KeyLockTimes = "LockTimes";
        public const string KeyDuration = "LockDurationMinutes";
        public const string KeyMonitor = "MonitorProcesses";
        public const string KeyAutoStart = "AutoStart";
        public const string KeyExitHold = "ExitHoldMinutes";

        public const int DefaultDurationMinutes = 10;
        public const int DefaultExitHoldMinutes = 2;

        public static string ConfigPath
        {
            get { return Paths.SettingsFile; }
        }

        /// <summary>现在是否处于课间锁定窗口。</summary>
        public static bool ShouldLockNow(out string reason)
        {
            reason = null;

            try
            {
                if (!File.Exists(ConfigPath))
                    return false;

                Configuration configuration = Open();
                if (configuration == null)
                    return false;

                string times = Get(configuration, KeyLockTimes);
                if (string.IsNullOrWhiteSpace(times))
                    return false;

                int duration = ParseDuration(Get(configuration, KeyDuration));
                TimeSpan now = DateTime.Now.TimeOfDay;

                foreach (string raw in times.Split(new[] { ',', ';', '\r', '\n' },
                                                   StringSplitOptions.RemoveEmptyEntries))
                {
                    TimeSpan start;
                    if (!TimeSpan.TryParse(raw.Trim(), out start))
                        continue;

                    TimeSpan end = start + TimeSpan.FromMinutes(duration);
                    if (end > TimeSpan.FromDays(1))
                        end = TimeSpan.FromDays(1);

                    if (now >= start && now < end)
                    {
                        reason = string.Format("下课时间 {0:hh\\:mm}，锁定到 {1:hh\\:mm}",
                                               start, end);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool ShouldLockNow()
        {
            string ignored;
            return ShouldLockNow(out ignored);
        }

        /// <summary>下次锁定的剩余时间，用于状态栏提示；没有返回 null。</summary>
        public static TimeSpan? NextLockIn()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return null;

                Configuration configuration = Open();
                if (configuration == null) return null;

                string times = Get(configuration, KeyLockTimes);
                if (string.IsNullOrWhiteSpace(times)) return null;

                TimeSpan now = DateTime.Now.TimeOfDay;
                TimeSpan? best = null;

                foreach (string raw in times.Split(new[] { ',', ';', '\r', '\n' },
                                                   StringSplitOptions.RemoveEmptyEntries))
                {
                    TimeSpan t;
                    if (!TimeSpan.TryParse(raw.Trim(), out t)) continue;

                    TimeSpan delta = t - now;
                    if (delta < TimeSpan.Zero) continue;

                    if (best == null || delta < best.Value)
                        best = delta;
                }

                return best;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>配置里要监控并结束的进程名，逗号分隔。没有返回空串。</summary>
        public static string GetMonitoredProcesses()
        {
            try
            {
                Configuration configuration = Open();
                if (configuration == null) return string.Empty;

                string value = Get(configuration, KeyMonitor);
                return value ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>是否允许开机自启，默认 true。</summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                Configuration configuration = Open();
                if (configuration == null) return true;

                string value = Get(configuration, KeyAutoStart);
                if (string.IsNullOrWhiteSpace(value)) return true;

                return !value.Trim().Equals("False", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>配置里的全部下课时间点，升序。用于主界面时间表显示。</summary>
        public static System.Collections.Generic.List<TimeSpan> GetLockTimes()
        {
            var result = new System.Collections.Generic.List<TimeSpan>();

            try
            {
                Configuration configuration = Open();
                if (configuration == null) return result;

                string times = Get(configuration, KeyLockTimes);
                if (string.IsNullOrWhiteSpace(times)) return result;

                foreach (string raw in times.Split(new[] { ',', ';', '\r', '\n' },
                                                   StringSplitOptions.RemoveEmptyEntries))
                {
                    TimeSpan value;
                    if (TimeSpan.TryParse(raw.Trim(), out value))
                        result.Add(value);
                }

                result.Sort();
            }
            catch (Exception)
            {
            }

            return result;
        }

        /// <summary>
        /// 退出后多少分钟内不再被服务拉起。
        /// 0 = 退出后直到下次开机都不再拉起。
        /// </summary>
        public static int GetExitHoldMinutes()
        {
            try
            {
                Configuration configuration = Open();
                if (configuration == null) return DefaultExitHoldMinutes;

                string value = Get(configuration, KeyExitHold);
                if (string.IsNullOrWhiteSpace(value)) return DefaultExitHoldMinutes;

                int minutes;
                if (!int.TryParse(value.Trim(), out minutes)) return DefaultExitHoldMinutes;
                if (minutes < 0 || minutes > 1440) return DefaultExitHoldMinutes;

                return minutes;
            }
            catch (Exception)
            {
                return DefaultExitHoldMinutes;
            }
        }

        // ------------------------------------------------------------------
        public static Configuration Open()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    CreateDefault();

                var map = new ExeConfigurationFileMap { ExeConfigFilename = ConfigPath };
                return ConfigurationManager.OpenMappedExeConfiguration(
                    map, ConfigurationUserLevel.None);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Get(Configuration configuration, string key)
        {
            if (configuration == null) return null;
            KeyValueConfigurationElement entry = configuration.AppSettings.Settings[key];
            return entry == null ? null : entry.Value;
        }

        public static void Set(Configuration configuration, string key, string value)
        {
            if (configuration == null) return;

            if (configuration.AppSettings.Settings[key] == null)
                configuration.AppSettings.Settings.Add(key, value);
            else
                configuration.AppSettings.Settings[key].Value = value;
        }

        public static int ParseDuration(string text)
        {
            int value;
            if (!int.TryParse(text, out value) || value <= 0 || value > 120)
                return DefaultDurationMinutes;
            return value;
        }

        private static void CreateDefault()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string template =
                    "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" +
                    "<configuration>\r\n" +
                    "  <appSettings>\r\n" +
                    "    <add key=\"LockTimes\" value=\"\" />\r\n" +
                    "    <add key=\"LockDurationMinutes\" value=\"" + DefaultDurationMinutes + "\" />\r\n" +
                    "    <add key=\"MonitorProcesses\" value=\"\" />\r\n" +
                    "    <add key=\"AutoStart\" value=\"True\" />\r\n" +
                    "  </appSettings>\r\n" +
                    "</configuration>\r\n";

                File.WriteAllText(ConfigPath, template);
            }
            catch (Exception)
            {
            }
        }
    }
}
