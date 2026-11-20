using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Classroom_Killer
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// 带 -install / -uninstall / -service 参数时走服务相关逻辑，
        /// 由 SCM 启动（非交互会话）时自动进入服务模式，其余情况正常运行界面。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // -install / -uninstall / -service
            if (ServiceSetup.Handle(args))
                return;

            // SCM 启动的进程没有交互桌面，直接进服务循环
            if (!Environment.UserInteractive)
            {
                ServiceSetup.RunService();
                return;
            }

            // 首次运行自检：安装包只负责复制文件，驱动和守护服务由程序自己补装。
            // 这样 Advanced Installer 里不需要配置任何 Custom Action。
            EnsureInstalled();

            // 服务通过 session 变更拉起 + 注册表 Run 项拉起，
            // 两条路可能撞车，这里用命名互斥量保证只有一个界面实例。
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, @"Local\ClassroomKillerUI", out createdNew))
            {
                if (!createdNew)
                    return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 先把自己挂进内核保护名单，再显示界面。
                // 驱动没装/没启动时返回 false，不影响程序正常运行。
                EnsureProtection();

                // 看门狗：驱动服务被停/被删会自动补回来
                DriverWatchdog.Start();

                if (ScheduleManager.ShouldLockNow())
                {
                    // 课间（锁定窗口内）：走登录界面，Form1 加载时会直接锁屏
                    Application.Run(new Form1());
                }
                else
                {
                    // 上课时间：不进界面，直接蹲托盘，到点自动锁屏。
                    // 模拟"上课期间重启电脑"的场景。
                    Application.Run(new Main { StartHidden = true });
                }
            }
        }

        /// <summary>
        /// 首次运行时补齐内核驱动和守护服务。
        ///
        /// 安装包只做文件复制，这一步负责 sc create + sc start。
        /// 没管理员权限时用 runas 提权重启一个自己（会弹一次 UAC），
        /// 由那个实例执行 -install。不能用 manifest 强制提权 —— 那样
        /// 服务用 CreateProcessAsUser 拉起界面会返回 740。
        /// </summary>
        private static void EnsureInstalled()
        {
            try
            {
                // 只看服务在不在。以前是「驱动在跑 && 服务在」才放行，
                // 结果驱动装不上的机器（无签名 / Win7）每次启动都往下走，
                // 把正在跑的守护服务 delete 掉再 create，中间抢不到 → 服务直接消失。
                // 驱动由 DriverWatchdog 每 8 秒单独补，不该和这里捆一起。
                if (ServiceSetup.ServiceExists())
                    return;

                if (ServiceSetup.IsAdministrator())
                {
                    ServiceSetup.Log("first-run: provisioning driver + service");
                    ServiceSetup.Log("first-run: " + ServiceSetup.ProvisionSilently());
                    return;
                }

                // 没权限：提权重启自己跑 -install
                ServiceSetup.Log("first-run: not elevated, requesting UAC");

                var psi = new System.Diagnostics.ProcessStartInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location,
                    "-install")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                ServiceSetup.Log("first-run: " + ex.Message);
            }
        }

        /// <summary>
        /// 保护当前进程。首次失败时在后台重试，覆盖“驱动尚未加载完成”的开机竞态。
        /// </summary>
        private static void EnsureProtection()
        {
            if (ProcessProtect.ProtectSelf())
                return;

            Task.Run(async () =>
            {
                for (int i = 0; i < 24; i++)
                {
                    await Task.Delay(5000).ConfigureAwait(false);
                    if (ProcessProtect.ProtectSelf())
                        return;
                }
            });
        }
    }
}
