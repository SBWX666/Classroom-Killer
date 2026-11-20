# Classroom Killer

下课锁屏软件。老师在课间拖堂，学生就被锁在屏幕里 —— 这个程序负责准点锁屏、准点解锁。

## 结构

```
Classroom Killer.sln          Visual Studio 解决方案（.NET Framework 4.8 / WinForms）
Classroom Killer\             主程序
  Date\                       密码哈希、时间表配置
  Driver\ClassKillerProtect.sys  内嵌的内核驱动（构建时打进 exe）
driver\                       KMDF 驱动源码（独立工程，需 WDK）
```

## 主程序

- **锁屏**：按时间表到点全屏锁定，拦 Alt+F4 / Alt+Tab / Win 键
- **守护**：Windows 服务 `ClassKillerGuard`，`CanStop=false`，崩溃自愈
- **托盘**：关闭窗口进托盘，右键打开 / 登录 / 设置 / 退出
- **密码**：退出、设置、改密码都要验证，PBKDF2-SHA256 存储
- **部署**：驱动以嵌入资源打包，`-install` 一键装好服务和驱动

## 驱动（driver\）

KMDF 驱动，用 `ObRegisterCallbacks` 摘掉外部 `OpenProcess` 的
`PROCESS_TERMINATE`，使任务管理器 / taskkill 无法结束受保护进程。

需要 WDK 构建，加载前须开启 `bcdedit /set testsigning on` 并关闭 Secure Boot。

## 构建

```
msbuild "Classroom Killer.sln" /p:Configuration=Release
```

产物 `Classroom Killer\bin\Release\Classroom Killer.exe`，构建后会自动补齐到 666 KB。

## 已知限制

- 驱动未签名，Win7 上无法加载（其余功能正常）
- 安全模式下整套保护失效
- Win7 只能发 MSI 安装包，单文件 SETUP 的引导器要求 Windows 10 1809+
