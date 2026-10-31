"""生成月圆风格牌面素材（assets/mod/，不进 git）。由 stage_assets.py 在最后调用。

来源：原作经典牌框部件（assets/source/classic_*.png）和原作字体（assets/fonts/）。
输出：
  images/card_face/frame_<种类>.png  牌框（600x844，种类见 Kinds）：底色 + 原作牌框，卡图圆窗挖空
  images/cards/<名>.png              卡图：原作圆形光晕垫底（覆盖 stage_assets 写出的卡图）
  images/ui/energy.png               费用图标：原作行动牌左上角的小沙漏（128，数字叠在上面）
  images/ui/energy_text.png          正文里的能量图标：同一个沙漏缩到 24
  fonts/classic_text.ttf             原作正文字体（原样复制，运行时按字节加载）

牌框坐标：原作牌框 404x568，按 600/404 放大；卡图圆窗中心 (201.5, 212)，直径约 280。
"""

import shutil
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
SourceDir = RepoDir / "assets" / "source"  # 原作牌框部件
FontDir = RepoDir / "assets" / "fonts"  # 原作字体
ModDir = RepoDir / "assets" / "mod"  # 输出根目录
FrameSize = (600, 844)  # 牌框输出尺寸（游戏里显示为 300x422）
WindowCenter = (201.5, 212)  # 原作牌框里卡图圆窗的中心
BaseRect = (27, 12, 377, 554)  # 原作底色区域（牌框像素坐标）
Kinds = {
    "attack": (128, 48, 40),  # 攻击：暗红
    "skill": (123, 93, 55),  # 行动：原作棕
    "equip": (58, 96, 82),  # 装备：青绿
    "counter": (78, 56, 112),  # 反制：紫
    "curse": (48, 40, 52),  # 诅咒：近黑
    "status": (88, 88, 92),  # 状态：灰
}


AncientCards = ["gear_device", "intermission"]  # 先祖牌（原生先祖牌面）
AncientSize = (598, 842)  # 原生先祖卡图节点尺寸（游戏里缩放 0.5）
AncientColor = (40, 46, 74)  # 先祖卡图底色：月夜蓝
AncientArtWidth = 0.78  # 圆形卡图占卡图宽度的比例
AncientArtCenter = 0.33  # 圆形卡图中心的高度比例（标题横幅和正文底之间）


def source(name: str) -> Image.Image:
    """读取原作牌框部件。"""
    return Image.open(SourceDir / f"{name}.png").convert("RGBA")


def window_mask(frame: Image.Image) -> np.ndarray:
    """牌框里与圆窗中心连通的透明区域（向外扩 3 像素），布尔数组。"""
    alpha = np.asarray(frame)[:, :, 3]
    labels, _ = ndimage.label(alpha < 10)
    window = labels == labels[int(WindowCenter[1]), int(WindowCenter[0])]
    return ndimage.binary_dilation(window, iterations=3)


def base_layer(color: tuple[int, int, int], size: tuple[int, int], seed: int) -> Image.Image:
    """底色：上亮下暗的竖向渐变，叠一层模糊噪点做出颜料质感。"""
    width, height = size
    shade = np.linspace(1.12, 0.78, height)[:, None, None]
    rng = np.random.default_rng(seed)
    noise = ndimage.gaussian_filter(rng.normal(0, 1, (height, width)), 6)[:, :, None]
    rgb = np.clip(np.array(color, float)[None, None, :] * shade * (1 + noise * 0.35), 0, 255)
    return Image.fromarray(np.dstack([rgb, np.full((height, width), 255.0)]).astype(np.uint8))


def frames() -> None:
    """每种牌一张牌框。"""
    frame = source("classic_frame")
    window = window_mask(frame)
    left, top, right, bottom = BaseRect
    for index, (kind, color) in enumerate(Kinds.items()):
        canvas = Image.new("RGBA", frame.size, (0, 0, 0, 0))
        canvas.paste(base_layer(color, (right - left, bottom - top), index), (left, top))
        array = np.asarray(canvas).copy()
        array[window, 3] = 0
        canvas = Image.fromarray(array)
        canvas.alpha_composite(frame)
        save(canvas.resize(FrameSize, Image.LANCZOS), f"images/card_face/frame_{kind}.png")


def portraits() -> None:
    """卡图垫原作圆形光晕：光晕直径为卡图的 286/300。"""
    glow = source("classic_circle")
    for path in sorted((ModDir / "images" / "cards").glob("*.png")):
        art = Image.open(path).convert("RGBA")
        side = max(art.size)
        canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        disc = round(side * 286 / 300)
        offset = (side - disc) // 2
        canvas.alpha_composite(glow.resize((disc, disc), Image.LANCZOS), (offset, offset))
        canvas.alpha_composite(art, ((side - art.width) // 2, (side - art.height) // 2))
        canvas.save(path)


def ancient_portraits() -> None:
    """先祖牌用原生先祖牌面，卡图要铺满整张牌（透明处会透出高亮光晕，整张牌泛青）：
    圆形卡图放到月夜底色上，摆在标题和正文之间，另存 images/cards/ancient/<名>.png。"""
    width, height = AncientSize
    for name in AncientCards:
        art = Image.open(ModDir / "images" / "cards" / f"{name}.png").convert("RGBA")
        canvas = base_layer(AncientColor, AncientSize, len(name))
        # 四周压暗，中间留一点月光
        y, x = np.mgrid[0:height, 0:width]
        dist = np.hypot((x - width / 2) / width, (y - height * AncientArtCenter) / height)
        shade = np.clip(1.25 - dist * 1.6, 0.45, 1.25)[:, :, None]
        rgb = np.clip(np.asarray(canvas, float)[:, :, :3] * shade, 0, 255)
        canvas = Image.fromarray(np.dstack([rgb, np.full((height, width), 255.0)]).astype(np.uint8))
        side = round(width * AncientArtWidth)
        circle = art.resize((side, side), Image.LANCZOS)
        canvas.alpha_composite(circle, ((width - side) // 2, round(height * AncientArtCenter - side / 2)))
        save(canvas, f"images/cards/ancient/{name}.png")


def energy_icon() -> None:
    """费用图标：原作行动牌的小沙漏（黑描边、黄沙），放大到 128 留一点边；另存一份 24 像素的正文图标。"""
    size = 128
    glass = source("classic_action")
    glass = glass.resize((116, 116), Image.LANCZOS)
    badge = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    badge.alpha_composite(glass, ((size - glass.width) // 2, (size - glass.height) // 2))
    save(badge, "images/ui/energy.png")
    save(badge.resize((24, 24), Image.LANCZOS), "images/ui/energy_text.png")


def fonts() -> None:
    """复制原作正文字体。"""
    target = ModDir / "fonts"
    target.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(FontDir / "classic_text.ttf", target / "classic_text.ttf")


def save(image: Image.Image, relative: str) -> None:
    """写出 PNG。"""
    target = ModDir / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target)


def main() -> None:
    """生成全部牌面素材。"""
    frames()
    portraits()
    ancient_portraits()
    energy_icon()
    fonts()
    print(f"card art written to {ModDir}")


if __name__ == "__main__":
    main()
