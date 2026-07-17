#!/usr/bin/env python3
import struct
import sys
from pathlib import Path


def read_u16(data: bytearray, offset: int) -> int:
    return struct.unpack_from("<H", data, offset)[0]


def read_u32(data: bytearray, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def rva_to_offset(data: bytearray, section_table: int, section_count: int, rva: int) -> int:
    for index in range(section_count):
        section = section_table + index * 40
        virtual_size = read_u32(data, section + 8)
        virtual_address = read_u32(data, section + 12)
        raw_size = read_u32(data, section + 16)
        raw_offset = read_u32(data, section + 20)
        if virtual_address <= rva < virtual_address + max(virtual_size, raw_size):
            return raw_offset + rva - virtual_address
    raise ValueError(f"RVA 0x{rva:X} is outside PE sections")


def main() -> None:
    assembly_path = Path(sys.argv[1])
    resource_path = Path(sys.argv[2])
    resource_relative_offset = int(sys.argv[3], 0) if len(sys.argv) > 3 else 0
    data = bytearray(assembly_path.read_bytes())
    resource = resource_path.read_bytes()

    pe_offset = read_u32(data, 0x3C)
    if data[pe_offset : pe_offset + 4] != b"PE\0\0":
        raise ValueError("invalid PE signature")

    coff = pe_offset + 4
    section_count = read_u16(data, coff + 2)
    optional_size = read_u16(data, coff + 16)
    optional = coff + 20
    magic = read_u16(data, optional)
    data_directories = optional + (96 if magic == 0x10B else 112 if magic == 0x20B else 0)
    if data_directories == optional:
        raise ValueError(f"unsupported PE optional header: 0x{magic:X}")

    clr_rva = read_u32(data, data_directories + 14 * 8)
    section_table = optional + optional_size
    clr_offset = rva_to_offset(data, section_table, section_count, clr_rva)
    resources_rva = read_u32(data, clr_offset + 24)
    resources_size = read_u32(data, clr_offset + 28)
    resources_offset = rva_to_offset(data, section_table, section_count, resources_rva) + resource_relative_offset
    original_size = read_u32(data, resources_offset)

    if original_size + 4 > resources_size:
        raise ValueError("invalid CLR resource size")
    if len(resource) > original_size:
        raise ValueError(f"replacement resource grew from {original_size} to {len(resource)} bytes")

    struct.pack_into("<I", data, resources_offset, len(resource))
    start = resources_offset + 4
    data[start : start + original_size] = resource + b"\0" * (original_size - len(resource))
    assembly_path.write_bytes(data)
    print(f"Manifest resource replaced: {original_size} -> {len(resource)} bytes")


if __name__ == "__main__":
    main()
