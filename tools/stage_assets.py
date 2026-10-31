"""按素材映射表把原作图片和生成图片整理成模组用的图片（assets/mod/images，不进 git）。

映射表 data/mod-assets.json（进 git）：
  {"cards": {"strike": "original/cards/attack1", ...},
   "powers": {"mischief_power": "original/buffs/guaipiqi", ...},
   "relics": {"knight_oath": "source/content_fmk_bless_relic_1001", ...},
   "events": {"fairy_blessing": "original/events/banshee", ...},
   "ui": {"mana": ["original/buffs/shuimen", 128], ...}}
来源路径相对 assets/，不带扩展名。
输出：
  cards  -> images/cards/<名>.png（原尺寸）
  powers -> images/powers/<名>.png（64）和 images/powers/big/<名>.png（256）
  relics -> images/relics/<名>.png（256）和 images/relics/outline/<名>.png（白色剪影，256）
  events -> images/events/<名>.png（事件插图，较长边不足 512 时放大到 512）
  ui     -> images/ui/<名>.png（指定边长的正方形）
另外原样复制事件面板用到的原作贴图 assets/original/event_ui/<名>.png -> images/event_ui/<名>.png
（由 tools/extract_event_panels.py 导出；名字里的九宫格参数括号去掉，如 di_SLIDER(110,84,46,0,46,0) -> di_SLIDER）。
最后由 make_ui_art.py 生成角色界面图片（选人、头像、手势、地图标记、能量计数器），
make_card_art.py 生成牌面（牌框、卡图光晕、费用圈、字体），stage_audio.py 整理音效（"audio"）。
用法：python tools/stage_assets.py（tools/build_mod.py 打包前会自动调用）
"""

import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_card_art  # noqa: E402
import make_ui_art  # noqa: E402
import stage_audio  # noqa: E402

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
AssetsDir = RepoDir / "assets"  # 素材根目录
MapPath = RepoDir / "data" / "mod-assets.json"  # 映射表
OutputDir = AssetsDir / "mod" / "images"  # 输出目录
PowerSmall, PowerBig = 64, 256  # 能力图标尺寸
RelicSize = 256  # 遗物图标尺寸
OutlineGrow = 3  # 遗物剪影外扩像素
EventMinSide = 512  # 事件插图较长边的最小像素
EventUiDir = AssetsDir / "original" / "event_ui"  # 原作事件面板贴图
EventSprites = [
    "icon_coin", "fengcheng__", "icon_shengming", "icon_mofa", "icon_jingyan",  # 选项图标和价格单位
    "zhufudi", "kuang", "kuang2", "kuang3", "kuang__event", "biaotidi",  # 祝福卡：底、品质圆框、名字条
    "biaotikuang_MERGE_PUBLIC", "di", "di_SLIDER(110,84,46,0,46,0)", "faguang",  # 标题牌、文字底、价格底、选中光
    "Tsqueren", "Tsqueren-Green",  # 红色确认按钮、绿色离开按钮
]  # 事件面板用到的原作贴图


def load(source: str) -> Image.Image:
    """读取来源图片为 RGBA。"""
    path = AssetsDir / f"{source}.png"
    if not path.exists():
        raise FileNotFoundError(f"missing source image: {path}")
    return Image.open(path).convert("RGBA")


def fit_square(image: Image.Image, size: int) -> Image.Image:
    """等比缩放（小图也放大）后居中放进透明正方形。"""
    # 只缩小不放大时，128 的原作状态图标在 256 的遗物图里只占一半，36 像素的“复制”几乎看不见
    scale = size / max(image.size)
    copy = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(copy, ((size - copy.width) // 2, (size - copy.height) // 2), copy)
    return canvas


def outline(image: Image.Image) -> Image.Image:
    """遗物剪影：不透明区域外扩几像素，填白色。"""
    alpha = image.getchannel("A").point(lambda a: 255 if a > 24 else 0)
    alpha = alpha.filter(ImageFilter.MaxFilter(OutlineGrow * 2 + 1))
    white = Image.new("RGBA", image.size, (255, 255, 255, 0))
    white.putalpha(alpha)
    return white


def at_least(image: Image.Image, side: int) -> Image.Image:
    """较长边不足 side 时等比放大。"""
    longest = max(image.size)
    if longest >= side:
        return image
    scale = side / longest
    return image.resize((round(image.width * scale), round(image.height * scale)), Image.LANCZOS)


def save(image: Image.Image, relative: str) -> None:
    """写出 PNG。"""
    target = OutputDir / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target)


def main() -> None:
    """命令行入口。"""
    sys.stdout.reconfigure(encoding="utf-8")
    mapping = json.loads(MapPath.read_text(encoding="utf-8"))
    # 图片目录完全由映射表生成，先清空避免残留
    if OutputDir.exists():
        shutil.rmtree(OutputDir)
    count = 0
    for name, source in mapping.get("cards", {}).items():
        save(load(source), f"cards/{name}.png")
        count += 1
    for name, source in mapping.get("powers", {}).items():
        image = load(source)
        save(fit_square(image, PowerSmall), f"powers/{name}.png")
        save(fit_square(image, PowerBig), f"powers/big/{name}.png")
        count += 1
    for name, source in mapping.get("relics", {}).items():
        icon = fit_square(load(source), RelicSize)
        save(icon, f"relics/{name}.png")
        save(outline(icon), f"relics/outline/{name}.png")
        count += 1
    for name, source in mapping.get("events", {}).items():
        save(at_least(load(source), EventMinSide), f"events/{name}.png")
        count += 1
    for name, (source, size) in mapping.get("ui", {}).items():
        save(fit_square(load(source), size), f"ui/{name}.png")
        count += 1
    # 事件面板：原作贴图原样复制
    for name in EventSprites:
        save(Image.open(EventUiDir / f"{name}.png").convert("RGBA"), f"event_ui/{name.split('(')[0]}.png")
        count += 1
    print(f"staged {count} images into {OutputDir}")
    make_ui_art.main()
    make_card_art.main()
    stage_audio.main()


if __name__ == "__main__":
    main()
