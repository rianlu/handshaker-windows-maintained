#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
installer = root / "dist" / "HandShaker-Windows-Maintained-Setup.exe"
data = installer.read_bytes()

assert data[:2] == b"MZ"
assert len(data) > 4_000_000
print("Windows 维护版安装包校验通过")
