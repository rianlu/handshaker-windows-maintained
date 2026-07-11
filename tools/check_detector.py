#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
data = (ROOT / "dist" / "HandShaker.Detector.exe").read_bytes()

assert data[0x2925:0x2929] == bytes.fromhex("fa 00 00 00")
assert data[0x2940] == 3
assert data[0x2954:0x2958] == bytes.fromhex("f4 01 00 00")
assert data[:2] == b"MZ"
print("Detector 补丁校验通过")
