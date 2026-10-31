# 月圆之夜：女骑士 · FullMoonKnight

《杀戮尖塔 2》角色模组，将《月圆之夜》的女骑士带入尖塔，包含装备、反制、穿刺和恶作剧等机制。

## 运行需求

- 《杀戮尖塔 2》：模组清单标记的最低版本为 0.107.1。
- 前置模组 BaseLib 3.4.7 或更新版本。

## 仓库内容

这里提供模组源码、素材及构建脚本，不包含游戏本体、BaseLib 或已打包的安装文件。部分原作素材需在本地准备；尚未验证从全新克隆完整构建资源包。

Windows 下的构建脚本使用 .NET 9 SDK、Python、Pillow、NumPy、SciPy、OpenCV 和 FFmpeg。请在准备好游戏、BaseLib 及素材后再检查 `tools/build_mod.py` 和 `tools/setup_engine.py` 的路径与覆盖行为。`build_mod.py` 会覆盖目标位置的 `FullMoonKnight` 目录，并删除同级 `FullMoonKnightTests` 目录。

## 素材说明

本模组为非官方粉丝作品，包含原作字体及基于原作素材制作的图片。游戏和相关素材的权利归各自权利人所有；使用和再分发时请遵守适用授权要求。
