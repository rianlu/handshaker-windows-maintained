<div align="center">
  <img src="./assets/ic_launcher.png" alt="HandShaker Windows Maintained" width="120" height="120">
  <h1>HandShaker Windows Maintained</h1>
  <p>面向现代 Android 设备的 HandShaker Windows 非官方维护版.</p>
  <p>
    <a href="https://github.com/rianlu/handshaker-windows-maintained/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/rianlu/handshaker-windows-maintained?display_name=tag"></a>
    <img alt="Windows" src="https://img.shields.io/badge/Windows-8%20and%20later-0078D4">
  </p>
</div>

> [!IMPORTANT]
> 本项目与 HandShaker 原厂无官方关联, 仅用于兼容性维护, 学习和非商业研究.

## 项目简介

本项目维护 HandShaker Windows 客户端, 修复非锤子 Android 设备通过 USB 连接时, 切换 AOA 前长时间等待的问题, 并提供可离线安装的维护版. 主程序仍沿用官方客户端.

## 功能状态

- 基于官方 Windows 离线安装包制作维护版.
- 连接 Android 手机时自动切换 AOA, 无需手动将 USB 用途改为「文件传输」.
- 缩短非锤子设备切换 AOA 前的固定等待.
- 使用 Android platform-tools 37.0.0.
- 可在菜单中检查并下载维护版更新.

## 下载与使用

安装包不在仓库里. 从 [Releases](https://github.com/rianlu/handshaker-windows-maintained/releases) 下载, 最新版本见 [Latest Release](https://github.com/rianlu/handshaker-windows-maintained/releases/latest). 应用内「检查更新」使用的地址写在 `update.xml`.

1. 运行安装包.
2. 若 SmartScreen 提示未知发布者, 选择仍要运行.
3. 从桌面或开始菜单打开 HandShaker.
4. 用数据线连接手机. 维护版会自动切换 AOA.

可在菜单中选择「检查更新」. 发现新版本时下载安装包并打开.

当前安装包未使用付费 Windows 代码签名证书. 部分受管控设备可能禁止运行未签名程序.

## 兼容性

已实测:

- Windows 真机上的首次驱动安装, Android AOA 授权弹窗和完整数据加载.

原程序目标环境为 Windows 8 及以上, 程序为 32 位. 其他 Windows 版本, 手机和安全策略可能存在差异.

## 构建

构建分为离线 Payload 和 WPF 安装外壳两步. Payload 在 macOS 维护环境生成, WPF 外壳在 Windows 使用系统 MSBuild 生成.

```powershell
py tools\patch_detector.py
py tools\check_detector.py
```

在 macOS 维护环境中准备完整离线 Payload, 需要 Python 3, Mono 和 NSIS. 脚本会用 `original/platform-tools` 里固定的 Android platform-tools 37.0.0 覆盖官方包中的 ADB, 不读取本机 Android SDK. 非锤子设备的首次 WinUSB 检测为 3 次, 每次 250 毫秒. 安装 ADB 驱动后的检测间隔为 500 毫秒, 保留 5 次重试. 补丁脚本会拒绝未知版本的 Detector.

```sh
./tools/build_payload.sh
```

将仓库和生成的 `build/HandShaker.Payload.Setup.exe` 同步到 Windows 后, 构建最终安装包:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build_installer.ps1
py tools\check_installer.py
```

本地产物在 `dist/`, 这个目录不提交:

- `dist/HandShaker.Detector.exe`
- `dist/handshaker-windows-maintained-2.6.0-r1-x86.exe`

发布时把安装包上传到 GitHub Releases, 并更新 `update.xml` 里的版本号, 下载地址, 文件名和 MD5. 已发布的文件以 Releases 为准, 不要从旧提交里取出 `dist/` 下的安装包.

受支持的原始 Detector SHA-256:

```text
4732405f7a9cc014c8d95e0de2a4050538a2408ae82f40fc05b9ed71daec1b88
```

## 仓库结构

```text
.
├── original/                   # 原始完整包, Detector, platform-tools 37.0.0
├── assets/                     # 应用图标和 README 资源
├── src/
│   ├── HandShaker.Detector.il  # Detector 反编译 IL
│   ├── HandShaker.Setup/       # 原版 WPF 安装界面维护源码
│   └── HandShaker.Uninstaller/ # 同风格 WPF 卸载界面维护源码
├── installer/                  # 内部静默 Payload 安装引擎
├── tools/                      # 补丁, 构建和校验脚本
├── update.xml                  # 应用内检查更新使用的版本说明
├── build/                      # 构建缓存与中间产物, 不入库
└── dist/                       # 本地安装包, 不入库
```

## 已知限制

- 本仓库不是重写后的 Windows 源码工程, 维护范围限于官方客户端上的连接, 安装和更新.
- 不同厂商设备可能需要补充对应 USB 硬件 ID. 首次连接仍可能需要安装驱动并授权 USB 调试.
- 未购买 Windows 代码签名证书, 首次运行可能出现安全提示.
- 驱动安装和设备枚举仍受 Windows, USB 数据线和手机 USB 模式影响.

## 相关项目

- [HandShaker Android Maintained](https://github.com/rianlu/handshaker-android-maintained)
- [HandShaker Mac Maintained](https://github.com/rianlu/handshaker-mac-maintained)

## 友情链接

- [LINUX DO](https://linux.do/): 真诚, 友善, 团结, 专业, 共建你我引以为荣之社区.

## 版权与免责声明

原始应用及相关名称, 商标, 代码和资源归原权利人所有. 本仓库不对原始应用主张权利, 未对整体内容授予通用开源许可证. 公开分发, 商业使用或二次集成前, 请自行评估相关风险.
