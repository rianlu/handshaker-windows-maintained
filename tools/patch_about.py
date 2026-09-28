import struct
import sys
from pathlib import Path

PROJECT_URL = "https://github.com/rianlu/handshaker-windows-maintained"
UPDATE_URL = "https://raw.githubusercontent.com/rianlu/handshaker-windows-maintained/main/update.xml"
MARKER = bytes.fromhex("99fd04039cfd0111")


def read7(buf, pos):
    result = 0
    shift = 0
    while True:
        byte = buf[pos]
        pos += 1
        result |= (byte & 0x7F) << shift
        if byte & 0x80 == 0:
            return result, pos
        shift += 7
        if shift > 28:
            raise ValueError("bad 7-bit int")


def write7(value):
    out = bytearray()
    while value >= 0x80:
        out.append((value & 0x7F) | 0x80)
        value >>= 7
    out.append(value)
    return bytes(out)


def read_string(buf, pos):
    length, pos = read7(buf, pos)
    raw = buf[pos : pos + length]
    pos += length
    if len(raw) >= 2 and raw[1] == 0:
        text = raw.decode("utf-16le")
    else:
        text = raw.decode("utf-8")
    return text, pos


def localized_values(data):
    values = []
    pos = 0
    while True:
        marker = data.find(MARKER, pos)
        if marker < 0:
            return values
        cursor = marker + len(MARKER)
        first, cursor = read7(data, cursor)
        second, cursor = read7(data, cursor)
        raw = data[cursor : cursor + second]
        text = raw.decode("utf-8")
        values.append((marker, cursor, first, second, text))
        pos = marker + 1


def replace_localized(data, old, new):
    old_bytes = old.encode("utf-8")
    new_bytes = new.encode("utf-8")
    for marker, text_at, first, second, text in localized_values(data):
        if text != old or second != len(old_bytes):
            continue
        gap = first - second
        prefix = MARKER + write7(len(new_bytes) + gap) + write7(len(new_bytes))
        updated = data[:marker] + prefix + new_bytes + data[text_at + len(old_bytes) :]
        if len(localized_values(updated)) != len(localized_values(data)):
            raise SystemExit("localized resource no longer parses after replacing " + old)
        return updated
    raise SystemExit("localized string not found: " + old)


def replace_same_length(data, old, new):
    old_bytes = old.encode("utf-8")
    new_bytes = new.encode("utf-8")
    if len(old_bytes) != len(new_bytes):
        raise SystemExit("replacement length differs for " + old)
    index = data.find(old_bytes)
    if index < 0 or data.find(old_bytes, index + 1) >= 0:
        raise SystemExit("localized string not unique: " + old)
    return data[:index] + new_bytes + data[index + len(old_bytes) :]


def patch_baml(data, replacements):
    for old, new in replacements:
        data = replace_same_length(data, old, new)
    if len(localized_values(data)) < 700:
        raise SystemExit("localized resource no longer parses")
    return data


def read_resources(data):
    magic, header_version, skip = struct.unpack_from("<III", data, 0)
    if magic != 0xBEEFCACE or header_version != 1:
        raise SystemExit("unexpected managed resource header")
    pos = 12
    _, pos = read_string(data, pos)
    _, pos = read_string(data, pos)
    pos = 12 + skip
    version, count, type_count = struct.unpack_from("<III", data, pos)
    pos += 12
    if version != 2:
        raise SystemExit("unexpected resource set version")
    for _ in range(type_count):
        _, pos = read_string(data, pos)
    if pos & 7:
        pos += 8 - (pos & 7)
    pos += 4 * count
    name_positions = list(struct.unpack_from("<" + "I" * count, data, pos))
    pos += 4 * count
    data_section = struct.unpack_from("<I", data, pos)[0]
    name_section = pos + 4
    entries = []
    for name_pos in name_positions:
        cursor = name_section + name_pos
        name, cursor = read_string(data, cursor)
        data_offset = struct.unpack_from("<I", data, cursor)[0]
        entries.append({"name": name, "offset_pos": cursor, "data_offset": data_offset, "data_section": data_section})
    return entries


def stream_bounds(data, entry):
    blob = entry["data_section"] + entry["data_offset"]
    _, content = read7(data, blob)
    length = struct.unpack_from("<I", data, content)[0]
    return content, length


def replace_named_stream(data, name, new_payload):
    entries = read_resources(data)
    matches = [entry for entry in entries if entry["name"] == name]
    if len(matches) != 1:
        raise SystemExit("resource not found: " + name)
    entry = matches[0]
    content, length = stream_bounds(data, entry)
    delta = len(new_payload) - length
    struct.pack_into("<I", data, content, len(new_payload))
    start = content + 4
    data[start : start + length] = new_payload
    if delta:
        for other in entries:
            if other["data_offset"] > entry["data_offset"]:
                other["data_offset"] += delta
                struct.pack_into("<I", data, other["offset_pos"], other["data_offset"])
    return data


def rva_to_offset(sections, rva):
    for virtual_size, virtual_address, raw_size, raw_offset in sections:
        if virtual_address <= rva < virtual_address + max(virtual_size, raw_size):
            return raw_offset + rva - virtual_address
    raise SystemExit("RVA not in a section: " + hex(rva))


def pe_sections(data):
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    coff = pe + 4
    section_count = struct.unpack_from("<H", data, coff + 2)[0]
    optional_size = struct.unpack_from("<H", data, coff + 16)[0]
    optional = coff + 20
    magic = struct.unpack_from("<H", data, optional)[0]
    data_directories = optional + (96 if magic == 0x10B else 112)
    section_table = optional + optional_size
    sections = []
    for index in range(section_count):
        section = section_table + index * 40
        name = bytes(data[section : section + 8]).split(b"\0")[0]
        virtual_size, virtual_address, raw_size, raw_offset = struct.unpack_from("<IIII", data, section + 8)
        sections.append((section, virtual_size, virtual_address, raw_size, raw_offset, name))
    return data_directories, sections


def replace_embedded_resource(path, mutate):
    data = bytearray(path.read_bytes())
    data_directories, sections = pe_sections(data)
    clr_rva = struct.unpack_from("<I", data, data_directories + 14 * 8)[0]
    clr_offset = rva_to_offset([(item[1], item[2], item[3], item[4]) for item in sections], clr_rva)
    resources_rva, resources_size = struct.unpack_from("<II", data, clr_offset + 24)
    lookup = [(item[1], item[2], item[3], item[4]) for item in sections]
    resources_offset = rva_to_offset(lookup, resources_rva)
    original_size = struct.unpack_from("<I", data, resources_offset)[0]
    if original_size + 4 > resources_size:
        raise SystemExit(path.name + " embedded resource is truncated")
    resource = bytearray(data[resources_offset + 4 : resources_offset + 4 + original_size])
    resource = mutate(resource)
    content = resources_offset + 4
    new_end = content + len(resource)
    text = next(item for item in sections if item[5] == b".text")
    _, virtual_size, virtual_address, raw_size, raw_offset, _ = text
    raw_end = raw_offset + raw_size
    if new_end > raw_end:
        raise SystemExit(path.name + " resource no longer fits in .text")
    last_rva = (new_end - 1 - raw_offset) + virtual_address
    if last_rva >= virtual_address + virtual_size:
        next_va = min(item[2] for item in sections if item[2] > virtual_address)
        needed = last_rva - virtual_address + 1
        if virtual_address + needed > next_va:
            raise SystemExit(path.name + " resource overlaps the next section")
        struct.pack_into("<I", data, text[0] + 8, needed)
    struct.pack_into("<I", data, resources_offset, len(resource))
    data[content:new_end] = resource
    struct.pack_into("<I", data, clr_offset + 28, len(resource) + 4)
    path.write_bytes(data)


def patch_resources(path):
    def mutate(resource):
        entries = {entry["name"]: entry for entry in read_resources(resource)}
        changes = {
            "localizable/zh-cn.baml": [
                ("V2.6.0", "2.6-r1"),
            ],
            "localizable/defaultlanguage.baml": [
                ("V2.6.0", "2.6-r1"),
            ],
        }
        for name, replacements in changes.items():
            content, length = stream_bounds(resource, entries[name])
            payload = bytes(resource[content + 4 : content + 4 + length])
            payload = patch_baml(payload, replacements)
            resource = replace_named_stream(resource, name, payload)
            entries = {entry["name"]: entry for entry in read_resources(resource)}
        return resource

    replace_embedded_resource(path, mutate)


def patch_setting(path):
    old = b"http://t.tt"
    new = PROJECT_URL.encode("ascii")

    def mutate(resource):
        name = "preference/preferenceview.baml"
        entries = {entry["name"]: entry for entry in read_resources(resource)}
        content, length = stream_bounds(resource, entries[name])
        payload = bytearray(resource[content + 4 : content + 4 + length])
        index = payload.find(old)
        if index < 0 or payload[index - 1] != len(old) or payload[index + len(old)] != 0x3F:
            raise SystemExit("about page URL not found")
        payload[index - 1] = len(new)
        payload[index : index + len(old)] = new
        return replace_named_stream(resource, name, bytes(payload))

    replace_embedded_resource(path, mutate)


def patch_config(path):
    data = path.read_bytes()
    old = b'key="UpdateUrl" value="http://dl2.smartisan.cn/app/handshaker/win/update/update.xml"'
    new = ('key="UpdateUrl" value="' + UPDATE_URL + '"').encode("ascii")
    if new in data:
        return
    if old not in data:
        raise SystemExit("UpdateUrl not found in " + path.name)
    path.write_bytes(data.replace(old, new, 1))


def main():
    stage = Path(sys.argv[1])
    if len(sys.argv) > 2 and sys.argv[2] == "setting":
        patch_setting(stage / "HandShaker.Setting.dll")
        print("about link patched")
        return
    patch_resources(stage / "HandShaker.Resources.dll")
    patch_config(stage / "HandShaker.exe.config")
    patch_config(stage / "HandShaker.Detector.exe.config")
    print("about text and update URL patched")


if __name__ == "__main__":
    main()
