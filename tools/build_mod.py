"""编译、打包并安装模组。

用法：python tools/build_mod.py [--target engine|game] [--skip-build] [--skip-rig]
  --skip-rig：不重建骨骼动画，沿用 assets/mod/animations/knight、knight_select 里已有的输出（角色分层图改动中时用）
  engine：安装到隔离测试引擎 .tools/engine/mods/FullMoonKnight（默认）
  game：安装到 FMK_GAME_DIR（默认正式游戏目录）的 mods/FullMoonKnight

FMK_GAME_DIR 指定编译引用；FMK_BASELIB_DIR 指定 BaseLib 来源目录。

PCK 内容（res://FullMoonKnight/ 下）：
  src/Localization/<语言>/*.json  -> localization/<语言>/*.json
  src/Resources/**                -> **（文本场景 .tscn / 资源 .tres）
  assets/mod/**                   -> **（素材管线输出的图片、骨骼、音频）
图片转成 Godot 的 .ctex（WebP 无损）并写 .import 重定向；Spine 的 .atlas/.skel 转成导入格式。
"""

import hashlib
import io
import json
import os
import shutil
import struct
import subprocess
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from pck import write_pck  # noqa: E402
import build_knight_rig  # noqa: E402
import build_select_rig  # noqa: E402
import stage_assets  # noqa: E402

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
ModId = "FullMoonKnight"  # 模组 ID，也是资源根目录名
ProjectFile = RepoDir / "src" / "FullMoonKnight.csproj"  # C# 工程
BuildOutput = RepoDir / "src" / "bin" / "Release" / "net9.0"  # 编译输出
LocalizationDir = RepoDir / "src" / "Localization"  # 本地化 JSON
ResourcesDir = RepoDir / "src" / "Resources"  # 文本场景与资源
StagedAssetsDir = RepoDir / "assets" / "mod"  # 素材管线输出
ArtifactsDir = RepoDir / "artifacts" / "build"  # 打包中间产物
Targets = {
    "engine": Path(os.environ.get("FMK_ENGINE_DIR") or RepoDir / ".tools" / "engine") / "mods" / ModId,
    "game": Path(os.environ.get("FMK_GAME_DIR") or r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2") / "mods" / ModId,
}  # 安装目标
ImportedDir = f"{ModId}/_imported"  # 导入产物在包内的目录
TextureFlags = 0x0D000000  # 与游戏导出的 ctex 相同的标志位
DataFormatWebp = 2  # ctex 数据格式：WebP
ImageFormatRgba8 = 5  # Godot Image.FORMAT_RGBA8


def ctex_from_png(data: bytes) -> bytes:
    """把 PNG 字节编码为 Godot 4 CompressedTexture2D（无 mipmap、WebP 无损）。"""
    image = Image.open(io.BytesIO(data)).convert("RGBA")
    width, height = image.size
    buffer = io.BytesIO()
    image.save(buffer, format="WEBP", lossless=True, quality=100, method=4)
    webp = buffer.getvalue()
    header = b"GST2" + struct.pack("<IIIIiIII", 1, width, height, TextureFlags, -1, 0, 0, 0)
    body = struct.pack("<IHHII", DataFormatWebp, width, height, 0, ImageFormatRgba8)
    return header + body + struct.pack("<I", len(webp)) + webp


def remap(importer: str, resource_type: str, target: str, texture: bool = False) -> bytes:
    """生成 .import 重定向文件内容。"""
    text = f'[remap]\n\nimporter="{importer}"\ntype="{resource_type}"\npath="res://{target}"\n'
    if texture:
        text += 'metadata={\n"vram_texture": false\n}\n'
    return text.encode("utf-8")


def imported_name(res_path: str, extension: str) -> str:
    """导入产物在包内的路径，用资源路径的哈希避免重名。"""
    digest = hashlib.md5(res_path.encode("utf-8")).hexdigest()
    return f"{ImportedDir}/{Path(res_path).name}-{digest}.{extension}"


def add_asset(files: dict[str, bytes], res_path: str, data: bytes) -> None:
    """按扩展名把一个素材加入包：图片和骨骼走导入格式，其余原样放入。"""
    suffix = Path(res_path).suffix.lower()
    if suffix == ".png":
        target = imported_name(res_path, "ctex")
        files[target] = ctex_from_png(data)
        files[res_path + ".import"] = remap("texture", "CompressedTexture2D", target, texture=True)
    elif suffix == ".atlas":
        target = imported_name(res_path, "spatlas")
        atlas = {
            "atlas_data": data.decode("utf-8"),
            "normal_texture_prefix": "n",
            "source_path": "res://" + res_path,
            "specular_texture_prefix": "s",
        }
        files[target] = json.dumps(atlas).encode("utf-8")
        files[res_path + ".import"] = remap("spine.atlas", "SpineAtlasResource", target)
    elif suffix == ".skel":
        target = imported_name(res_path, "spskel")
        files[target] = data
        files[res_path + ".import"] = remap("spine.skel", "SpineSkeletonFileResource", target)
    else:
        files[res_path] = data


def collect_files() -> dict[str, bytes]:
    """收集 PCK 内全部文件，返回 {包内路径: 内容}。"""
    files: dict[str, bytes] = {}
    for source in sorted(LocalizationDir.rglob("*.json")):
        relative = source.relative_to(LocalizationDir).as_posix()
        # 校验 JSON 格式，避免游戏加载时才报错
        json.loads(source.read_text(encoding="utf-8"))
        files[f"{ModId}/localization/{relative}"] = source.read_bytes()
    for root in (ResourcesDir, StagedAssetsDir):
        if not root.exists():
            continue
        for source in sorted(p for p in root.rglob("*") if p.is_file()):
            relative = source.relative_to(root).as_posix()
            add_asset(files, f"{ModId}/{relative}", source.read_bytes())
    return files


def reference_properties() -> list[str]:
    """按显式指定的游戏/前置副本构建；不改变原先默认安装路径。"""
    properties = []
    if game_dir := os.environ.get("FMK_GAME_DIR"):
        properties.append(f"-p:GameDir={Path(game_dir).resolve()}")
    if baselib_dir := os.environ.get("FMK_BASELIB_DIR"):
        properties.append(f"-p:BaseLibDll={Path(baselib_dir).resolve() / 'BaseLib.dll'}")
    return properties


def build_dll() -> None:
    """编译 C# 工程。"""
    result = subprocess.run(
        ["dotnet", "build", str(ProjectFile), "-c", "Release", "-nologo", "-v", "q", *reference_properties()],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        env=dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en"),
    )
    if result.returncode != 0:
        print(result.stdout[-3000:])
        print(result.stderr[-3000:])
        raise SystemExit("dotnet build failed")


def main() -> None:
    """命令行入口。"""
    target_name = "engine"
    if "--target" in sys.argv:
        target_name = sys.argv[sys.argv.index("--target") + 1]
    target_dir = Targets[target_name]
    if "--skip-build" not in sys.argv:
        build_dll()

    stage_assets.main()
    if "--skip-rig" not in sys.argv:
        build_knight_rig.main()
        build_select_rig.main()
    ArtifactsDir.mkdir(parents=True, exist_ok=True)
    pck_path = ArtifactsDir / f"{ModId}.pck"
    files = collect_files()
    write_pck(files, pck_path)

    # 安装：清空旧目录后复制 dll、pck、清单
    if target_dir.exists():
        shutil.rmtree(target_dir)
    target_dir.mkdir(parents=True)
    shutil.copy2(BuildOutput / f"{ModId}.dll", target_dir / f"{ModId}.dll")
    shutil.copy2(BuildOutput / f"{ModId}.pdb", target_dir / f"{ModId}.pdb")
    shutil.copy2(pck_path, target_dir / f"{ModId}.pck")
    shutil.copy2(RepoDir / f"{ModId}.json", target_dir / f"{ModId}.json")
    # 旧的测试模组引用的类型可能已经删掉，留着会在启动时报加载错误；run_tests 打包后会重新安装
    tests_dir = target_dir.parent / "FullMoonKnightTests"
    if tests_dir.exists():
        shutil.rmtree(tests_dir)
    print(f"installed to {target_dir} ({len(files)} pck entries, {pck_path.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
