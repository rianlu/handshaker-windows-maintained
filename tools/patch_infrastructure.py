#!/usr/bin/env python3
import hashlib
import sys
from pathlib import Path

SOURCE_SHA256 = "79fc68918de65300f6a51cbc937471972a18a14f691e16bd4bfa33a7b5e329bf"
METHOD_ENTRY = 0x4AD4


def main() -> None:
    path = Path(sys.argv[1])
    data = bytearray(path.read_bytes())
    if hashlib.sha256(data).hexdigest() != SOURCE_SHA256:
        raise SystemExit("不支持的 HandShaker.Infrastructure.dll 版本")
    if data[METHOD_ENTRY] != 0x2B:
        raise SystemExit("TryAndRestartAdb 入口校验失败")

    data[METHOD_ENTRY] = 0x2A  # ret
    path.write_bytes(data)
    print("已禁用旧版 ADB 重启逻辑")


if __name__ == "__main__":
    main()
