"""Godot 4 PCK（格式版本 3）读写。

读取：python tools/pck.py list <pck> [过滤子串]
提取：python tools/pck.py extract <pck> <res路径前缀> <输出目录>
写入由 build_mod.py 调用 write_pck()。
"""

import hashlib
import struct
import sys
from pathlib import Path

Magic = 0x43504447  # "GDPC"
FlagDirEncrypted = 1  # 目录加密（游戏包未加密）
FlagRelativeBase = 2  # 文件偏移相对 file_base


def read_directory(path: Path) -> list[tuple[str, int, int]]:
    """读取 PCK 目录，返回 (res 路径, 绝对偏移, 大小) 列表。"""
    with path.open("rb") as f:
        head = f.read(4)
        if struct.unpack("<I", head)[0] != Magic:
            # 内嵌在可执行文件末尾的包：从尾部找
            f.seek(-12, 2)
            size, magic = struct.unpack("<QI", f.read(12))
            if magic != Magic:
                raise ValueError("not a pck")
            start = f.tell() - 12 - size
            f.seek(start)
            f.read(4)
        else:
            start = 0
        version, major, minor, patch, flags = struct.unpack("<5I", f.read(20))
        file_base = struct.unpack("<Q", f.read(8))[0]
        if version >= 3:
            dir_offset = struct.unpack("<Q", f.read(8))[0]
            f.seek(start + dir_offset)
        else:
            f.read(64)
        if flags & FlagDirEncrypted:
            raise ValueError("encrypted directory")
        base = start + file_base if flags & FlagRelativeBase else start
        count = struct.unpack("<I", f.read(4))[0]
        entries = []
        for _ in range(count):
            length = struct.unpack("<I", f.read(4))[0]
            name = f.read(length).rstrip(b"\0").decode("utf-8")
            offset, size = struct.unpack("<QQ", f.read(16))
            f.read(16)  # md5
            f.read(4)  # 文件标志
            if not name.startswith("res://"):
                name = "res://" + name
            entries.append((name, base + offset, size))
        return entries


def extract(path: Path, prefix: str, out_dir: Path) -> int:
    """提取以 prefix 开头的文件到 out_dir，返回文件数。"""
    count = 0
    with path.open("rb") as f:
        for name, offset, size in read_directory(path):
            if not name.startswith(prefix):
                continue
            f.seek(offset)
            target = out_dir / name[len("res://"):]
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(f.read(size))
            count += 1
    return count


def write_pck(files: dict[str, bytes], target: Path, version: tuple[int, int, int] = (4, 5, 1)) -> None:
    """把 {res 相对路径: 内容} 写成未加密的 PCK。"""
    entries = []
    for name, data in sorted(files.items()):
        encoded = name.encode("utf-8")
        encoded += b"\0" * ((-len(encoded)) % 4)
        entries.append((encoded, data))
    header_size = 104  # 魔数、版本、标志、file_base、dir_offset 与保留区
    directory_size = 4 + sum(4 + len(p) + 8 + 8 + 16 + 4 for p, _ in entries)
    header = struct.pack("<IIIIIIQQ", Magic, 3, *version, 0, 0, header_size) + b"\0" * 64
    directory = struct.pack("<I", len(entries))
    payload = bytearray()
    offset = header_size + directory_size
    for encoded, data in entries:
        directory += struct.pack("<I", len(encoded)) + encoded
        directory += struct.pack("<QQ", offset, len(data)) + hashlib.md5(data).digest() + struct.pack("<I", 0)
        payload += data
        offset += len(data)
    target.write_bytes(header + directory + bytes(payload))


def main() -> None:
    """命令行入口。"""
    command, pck = sys.argv[1], Path(sys.argv[2])
    if command == "list":
        needle = sys.argv[3] if len(sys.argv) > 3 else ""
        for name, offset, size in read_directory(pck):
            if needle in name:
                print(f"{size:>10} {name}")
    elif command == "extract":
        print(extract(pck, sys.argv[3], Path(sys.argv[4])), "files")


if __name__ == "__main__":
    main()
