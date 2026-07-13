#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
installer = root / "dist" / "HandShaker-Windows-Maintained-Offline-Setup.exe"
data = installer.read_bytes()

assert data[:2] == b"MZ"
assert len(data) > 50_000_000
assert b"HandShaker.Payload.Setup.exe" in data
assert b"HandShaker.Uninstaller.exe" in data
assert b"sf.smartisan.com" not in data
assert b"dl2.smartisan.cn" not in data
print("Windows 维护版安装包校验通过")
