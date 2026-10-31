"""用硬链接在 .tools/engine 下建立隔离的游戏引擎副本，供自动测试使用。

副本有自己的 mods 目录和 override.cfg（独立用户目录），不改动正式游戏目录和真实存档。
用法：python tools/setup_engine.py [--force]
另一份引擎（例如用户试玩占用 .tools/engine 时）：设 FMK_ENGINE_DIR=.tools/engine-dev、FMK_USER_DIR=fmk-dev，
之后 run_tests、build_mod、balance_sim、make_preview 都带上这两个变量。
测试另一游戏分支时，额外设置 FMK_GAME_DIR；前置副本可用 FMK_BASELIB_DIR 指定。
不同游戏版本使用不同 FMK_ENGINE_DIR，避免旧硬链接混入新分支。
"""

import json
import os
import shutil
import sys
from pathlib import Path

GameDir = Path(os.environ.get("FMK_GAME_DIR") or r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2").resolve()  # 正式游戏目录，只读
RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
EngineDir = Path(os.environ.get("FMK_ENGINE_DIR") or RepoDir / ".tools" / "engine").resolve()  # 隔离引擎副本
SkipNames = {"mods", "override.cfg", ".GameInstance", "vortex.deployment.slaythespire2-root.json", "使用教程.txt"}  # 不链接的条目
UserDirName = "SlayTheSpire2/" + os.environ.get("FMK_USER_DIR", "fmk-test")  # 测试用户目录，位于 %APPDATA%/SlayTheSpire2 下
TestUserDir = Path(os.environ["APPDATA"]) / UserDirName  # 测试用户目录绝对路径
BaseLibDir = Path(os.environ.get("FMK_BASELIB_DIR") or r"C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3737335127\BaseLib").resolve()  # BaseLib 来源


def validate_engine_source(source: Path, target: Path, refresh: bool = False) -> None:
    """禁止副本覆盖原游戏，并防止跨分支复用旧引擎造成混合版本。"""
    source, target = source.resolve(), target.resolve()
    if source.is_relative_to(target) or target.is_relative_to(source):
        raise ValueError(f"engine and source must not overlap: {target} / {source}")
    if refresh:
        return
    source_release, target_release = source / "release_info.json", target / "release_info.json"
    if source_release.exists() and target_release.exists():
        expected = json.loads(source_release.read_text(encoding="utf-8"))
        actual = json.loads(target_release.read_text(encoding="utf-8"))
        if any(expected.get(key) != actual.get(key) for key in ("version", "commit")):
            raise ValueError("engine contains a different game release; choose a new FMK_ENGINE_DIR "
                             "or explicitly refresh this isolated copy with --force")


def link_tree(source: Path, target: Path) -> int:
    """递归硬链接 source 下的全部文件，返回新建链接数。"""
    count = 0
    for item in source.iterdir():
        if item.name in SkipNames:
            continue
        destination = target / item.name
        if item.is_dir():
            destination.mkdir(parents=True, exist_ok=True)
            count += link_tree(item, destination)
        elif not destination.exists():
            os.link(item, destination)
            count += 1
    return count


def main() -> None:
    """建立或刷新引擎副本。"""
    validate_engine_source(GameDir, EngineDir, refresh="--force" in sys.argv)
    if "--force" in sys.argv and EngineDir.exists():
        shutil.rmtree(EngineDir)
    EngineDir.mkdir(parents=True, exist_ok=True)
    created = link_tree(GameDir, EngineDir)

    # 独立用户目录：Godot 读取可执行文件旁的 override.cfg
    (EngineDir / "override.cfg").write_text(
        "[application]\n"
        "config/use_custom_user_dir=true\n"
        f'config/custom_user_dir_name="{UserDirName}"\n',
        encoding="utf-8",
    )

    # mods 目录：BaseLib 复制一份（Steam 关闭时不会读取创意工坊目录）
    mods = EngineDir / "mods"
    mods.mkdir(exist_ok=True)
    baselib_target = mods / "BaseLib"
    if baselib_target.exists():
        shutil.rmtree(baselib_target)
    shutil.copytree(BaseLibDir, baselib_target)
    prepare_settings()
    print(f"engine ready: {EngineDir} (new links: {created})")


def prepare_settings() -> None:
    """在测试用户目录写入设置：允许加载模组、跳过片头、窗口模式、静音。"""
    settings_path = TestUserDir / "default" / "1" / "settings.save"
    settings = {}
    if settings_path.exists():
        settings = json.loads(settings_path.read_text(encoding="utf-8"))
    settings.setdefault("schema_version", 5)
    mod_settings = settings.get("mod_settings") or {}
    mod_settings["mods_enabled"] = True
    settings["mod_settings"] = mod_settings
    settings["skip_intro_logo"] = True
    settings["seen_ea_disclaimer"] = True
    settings["fullscreen"] = False
    settings["window_size"] = {"X": 1600, "Y": 900}
    for key in ("volume_master", "volume_bgm", "volume_sfx", "volume_ambience"):
        settings[key] = 0.0
    settings_path.parent.mkdir(parents=True, exist_ok=True)
    settings_path.write_text(json.dumps(settings, indent=2), encoding="utf-8")
    backup = settings_path.with_name("settings.save.backup")
    if backup.exists():
        backup.unlink()


if __name__ == "__main__":
    main()
