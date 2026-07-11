# HandShaker Windows Maintained

维护 HandShaker Windows 版 USB 检测器, 修复非锤子 Android 设备在切换 AOA 前固定等待约 40 秒的问题.

## 构建

```powershell
py tools\patch_detector.py
py tools\check_detector.py
```

构建结果位于 `dist/HandShaker.Detector.exe`.

## 当前修改

- 将首次 WinUSB 检测由 15 次, 每次 2 秒调整为 3 次, 每次 250 毫秒.
- 将安装 ADB 驱动后的检测间隔由 2 秒调整为 500 毫秒, 保留 5 次重试.
- 保留原程序集结构, 资源, 依赖和主程序通信方式.

原始文件仅接受 SHA-256 `4732405f7a9cc014c8d95e0de2a4050538a2408ae82f40fc05b9ed71daec1b88`.

## 验证范围

脚本会校验原始文件哈希和修改位置. 最终连接行为必须在 Windows 真机验证, 包括首次驱动安装, Android AOA 授权弹窗和后续连接.
