#!/usr/bin/env python3
import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "original" / "HandShaker.Detector.exe"
OUTPUT = ROOT / "dist" / "HandShaker.Detector.exe"
SOURCE_SHA256 = "4732405f7a9cc014c8d95e0de2a4050538a2408ae82f40fc05b9ed71daec1b88"

# SwitchToAoa 中的三个常量: 首轮间隔, 首轮次数, 驱动安装后的间隔.
# 末项把 OpenDevice 或 SendCommand 失败时的返回值改为成功, 避免把切换前的设备交给 ADB.
PATCHES = {
    0x2925: (bytes.fromhex("d0 07 00 00"), bytes.fromhex("fa 00 00 00")),
    0x2940: (bytes.fromhex("0f"), bytes.fromhex("03")),
    0x2954: (bytes.fromhex("d0 07 00 00"), bytes.fromhex("f4 01 00 00")),
    0x299A: (
        bytes.fromhex("28 04 00 00 06 2c 07 28 03 00 00 06 17 2a 16 2a"),
        bytes.fromhex("28 04 00 00 06 2c 07 28 03 00 00 06 17 2a 17 2a"),
    ),
}


def main() -> None:
    data = bytearray(SOURCE.read_bytes())
    digest = hashlib.sha256(data).hexdigest()
    if digest != SOURCE_SHA256:
        raise SystemExit(f"不支持的 Detector 版本: {digest}")

    for offset, (expected, replacement) in PATCHES.items():
        actual = bytes(data[offset : offset + len(expected)])
        if actual != expected:
            raise SystemExit(f"0x{offset:X} 校验失败: {actual.hex()}")
        data[offset : offset + len(expected)] = replacement

    OUTPUT.parent.mkdir(exist_ok=True)
    OUTPUT.write_bytes(data)
    print(f"已生成 {OUTPUT}")
    print(hashlib.sha256(data).hexdigest())


if __name__ == "__main__":
    main()
