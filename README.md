<div align="center">
  <h1>HandShaker Windows Maintained</h1>
  <p>面向现代 Android 设备的 HandShaker Windows 非官方维护版.</p>
  <p><img alt="Windows" src="https://img.shields.io/badge/Windows-8%20and%20later-0078D4"></p>
</div>

> [!IMPORTANT]
> 本项目与 HandShaker 原厂无官方关联, 仅用于兼容性维护, 学习和非商业研究.

## 项目简介

本项目维护 HandShaker Windows 客户端的 USB Detector, 修复非锤子 Android 设备切换 AOA 模式前固定等待约 40 秒的问题. 当前不重写主程序, 仅对根因所在的 `HandShaker.Detector.exe` 进行最小修改.

## 功能状态

- 保留原版 HandShaker 主程序, 资源, 依赖和通信方式.
- 将首次 WinUSB 检测从 15 次, 每次 2 秒调整为 3 次, 每次 250 毫秒.
- 将安装 ADB 驱动后的检测间隔从 2 秒调整为 500 毫秒, 保留 5 次重试.
- 提供可重复执行的补丁和校验脚本.
- 拒绝修改未知版本, 避免错误覆盖其他 Detector 文件.

## 使用说明

当前仓库产出的是维护后的 `HandShaker.Detector.exe`, 不是完整安装器.

1. 安装官方 Windows 版 HandShaker.
2. 完全退出 HandShaker 和 Detector.
3. 备份安装目录中的原始 `HandShaker.Detector.exe`.
4. 使用 `dist/HandShaker.Detector.exe` 替换原文件.
5. 重新启动 HandShaker.

当前文件未使用付费 Windows 代码签名证书. SmartScreen 或 UAC 可能显示"未知发布者"; 少数企业设备可能禁止运行未签名程序.

## 兼容性

- 原程序目标环境为 Windows 8 及以上版本.
- 原程序集为 32 位 .NET Framework 4.5.2 WPF 程序.
- Android 设备在 Windows 上使用 USB 连接时, 通常需要将 USB 用途切换为"文件传输".
- 首次驱动安装, Android AOA 授权弹窗和完整数据加载仍需在 Windows 真机验证.

## 构建

构建脚本仅依赖 Python 3 标准库.

```powershell
py tools\patch_detector.py
py tools\check_detector.py
```

产物输出到 `dist/HandShaker.Detector.exe`.

受支持的原始文件 SHA-256:

```text
4732405f7a9cc014c8d95e0de2a4050538a2408ae82f40fc05b9ed71daec1b88
```

## 仓库结构

```text
.
├── original/           # 原始 Detector 文件
├── src/                # 反编译 IL, 用于审计修改位置
├── tools/              # 补丁和校验脚本
└── dist/               # 修复后的 Detector 文件
```

## 已知限制

- 当前仅维护 Detector 的 USB/AOA 切换逻辑, 不维护完整 Windows 主程序.
- 修改后的文件不再具有原厂 Authenticode 签名.
- 驱动安装和设备枚举仍受 Windows, USB 数据线和手机 USB 模式影响.

## 相关项目

- [HandShaker Android Maintained](https://github.com/rianlu/handshaker-android-maintained)
- [HandShaker Mac Maintained](https://github.com/rianlu/handshaker-mac-maintained)

## 友情链接

- [LINUX DO](https://linux.do/): 真诚, 友善, 团结, 专业, 共建你我引以为荣之社区.

## 版权与免责声明

原始应用及相关名称, 商标, 代码和资源归原权利人所有. 本仓库不对原始应用主张权利, 未对整体内容授予通用开源许可证. 公开分发, 商业使用或二次集成前, 请自行评估相关风险.
