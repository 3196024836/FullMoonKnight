"""生成角色界面图片（assets/mod/images/ui/，不进 git）。由 stage_assets.py 在最后调用。

来源：生成的选人背景和海报、能量圆盘底座和角色分层（assets/generated）、原作行动点沙漏，以及游戏自带的铁甲战士多人手势和地图标记（重新上色）。
输出：
  ui/char_select_bg.png            选人背景（1920x1080，从生成图右侧裁出 16:9）
  ui/char_select.png、_locked.png  选人按钮（132x195）
  ui/top_icon.png、_outline.png    顶栏头像和描边（128）
  ui/map_marker.png                地图标记（49x64）
  ui/hands/<手势>.png              多人手势（422x1200）
  ui/energy/layer<1-5>.png         能量计数器各层（256，只有第 4 层是沙漏）
"""

import colorsys
import io
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_select_rig  # noqa: E402
from pck import read_directory  # noqa: E402

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
AssetsDir = RepoDir / "assets"  # 素材根目录
OutputDir = AssetsDir / "mod" / "images" / "ui"  # 输出目录
GamePck = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck")  # 游戏资源包
LayerDir = AssetsDir / "generated" / "character" / "layers"  # 角色分层（透明背景）
HeadLayers = ["head"]  # 头像用的层（从后到前；兜帽、头发和脸在同一个部件里）
Hourglass = AssetsDir / "original" / "character" / "xingdongli.png"  # 原作行动点沙漏
SelectPoster = AssetsDir / "generated" / "select" / "poster.jpg"  # 选人海报（背景加人物，build_select_rig.py 输出）
EnergyPlate = AssetsDir / "generated" / "ui" / "energy_plate.png"  # 生成的能量圆盘底座（透明背景）
Gestures = ["point", "rock", "paper", "scissors"]  # 多人手势


def game_image(name: str) -> Image.Image:
    """从游戏资源包读取无损纹理（文件名包含 name 的 ctex）。"""
    with GamePck.open("rb") as f:
        for path, offset, size in read_directory(GamePck):
            if name in path and path.endswith(".ctex"):
                f.seek(offset)
                data = f.read(size)
                length = struct.unpack_from("<I", data, 52)[0]
                return Image.open(io.BytesIO(data[56:56 + length])).convert("RGBA")
    raise FileNotFoundError(name)


def recolor(image: Image.Image, rule) -> Image.Image:
    """逐像素改色：rule(h, s, v) 返回新的 (h, s, v)，h 为 0–1。"""
    array = np.asarray(image).astype(float) / 255
    out = array.copy()
    flat = array.reshape(-1, 4)
    result = out.reshape(-1, 4)
    for i in np.nonzero(flat[:, 3] > 0)[0]:
        h, s, v = colorsys.rgb_to_hsv(*flat[i, :3])
        result[i, :3] = colorsys.hsv_to_rgb(*rule(h, s, v))
    return Image.fromarray((out * 255).astype(np.uint8))


def save(image: Image.Image, relative: str) -> None:
    """写出 PNG。"""
    target = OutputDir / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target)


# ===== 选人 =====


def select_background() -> None:
    """选人背景：与选人骨骼同一裁切（人物由骨骼画在上面）。"""
    save(build_select_rig.background(), "char_select_bg.png")


def select_buttons() -> None:
    """选人按钮：选人海报里的头肩；未解锁为压暗去色的同一张图。"""
    image = Image.open(SelectPoster).convert("RGBA")
    # 头肩区域（按钮宽高比 132:195）
    top, height = round(image.height * 0.08), round(image.height * 0.5)
    width = round(height * 132 / 195)
    left = round(image.width * 0.686) - width // 2  # 脸在海报里的横向位置
    crop = image.crop((left, top, left + width, top + height)).resize((132, 195), Image.LANCZOS)
    save(_framed(crop.copy()), "char_select.png")
    gray = crop.convert("L").point(lambda v: v * 0.3).convert("RGBA")
    save(_framed(gray), "char_select_locked.png")


def _framed(image: Image.Image) -> Image.Image:
    """加深色描边和内侧亮线。"""
    draw = ImageDraw.Draw(image)
    w, h = image.size
    draw.rectangle((0, 0, w - 1, h - 1), outline=(20, 20, 28, 255), width=3)
    draw.rectangle((3, 3, w - 4, h - 4), outline=(255, 255, 255, 50), width=1)
    return image


def top_icon() -> None:
    """顶栏头像：角色分层合成的头部，外加白色剪影描边。"""
    table = json.loads((LayerDir / "layers.json").read_text(encoding="utf-8"))
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    for name in HeadLayers:
        canvas.alpha_composite(Image.open(LayerDir / f"{name}.png").convert("RGBA"), (table[name]["x"], table[name]["y"]))
    left, top, right, bottom = canvas.getbbox()
    side = max(right - left, bottom - top) + 8
    cx, cy = (left + right) // 2, (top + bottom) // 2
    head = canvas.crop((cx - side // 2, cy - side // 2, cx + side // 2, cy + side // 2)).resize((128, 128), Image.LANCZOS)
    save(head, "top_icon.png")
    alpha = head.getchannel("A").point(lambda a: 255 if a > 24 else 0).filter(ImageFilter.MaxFilter(7))
    white = Image.new("RGBA", head.size, (255, 255, 255, 0))
    white.putalpha(alpha)
    save(white, "top_icon_outline.png")


# ===== 地图和手势 =====


def map_marker() -> None:
    """地图标记：铁甲战士的红色标记改成月光蓝。"""
    marker = game_image("map_marker_ironclad.png")
    save(recolor(marker, lambda h, s, v: (0.6, s * 0.75, v)), "map_marker.png")


def hands() -> None:
    """多人手势：护臂改银色，手改深红手套，袖子改披风紫。"""
    def rule(h: float, s: float, v: float) -> tuple[float, float, float]:
        if 0.2 < h < 0.5:
            return 0.98, min(s * 1.4, 0.65), v * 0.6  # 绿色的手 -> 深红手套
        if v > 0.45 and s > 0.25:
            return 0.62, s * 0.25, v * 0.72  # 橙色护臂 -> 银色
        return 0.78, min(s * 1.2, 0.5), v * 0.9  # 深色袖子 -> 紫色

    for gesture in Gestures:
        save(recolor(game_image(f"multiplayer_hand_ironclad_{gesture}.png"), rule), f"hands/{gesture}.png")


# ===== 能量计数器 =====


def energy_layers() -> None:
    """能量计数器：第 1 层是生成的圆盘底座（铜边深色圆盘，没能量时被原版暗化材质压暗），
    第 4 层是原作大沙漏，坐在铜边里面；其余层为空图，场景结构不变。256 像素，场景里缩到 128。"""
    size = 256
    blank = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    for index in (2, 3, 5):
        save(blank, f"energy/layer{index}.png")
    save(centered(Image.open(EnergyPlate).convert("RGBA"), size, 236), "energy/layer1.png")
    save(centered(Image.open(Hourglass).convert("RGBA"), size, 200), "energy/layer4.png")


def centered(image: Image.Image, size: int, fit: int) -> Image.Image:
    """把图片等比缩到 fit 以内，放在 size 见方的透明画布正中。"""
    image = image.copy()
    image.thumbnail((fit, fit), Image.LANCZOS)
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    layer.alpha_composite(image, ((size - image.width) // 2, (size - image.height) // 2))
    return layer


def main() -> None:
    """生成全部界面图片。"""
    select_background()
    select_buttons()
    top_icon()
    map_marker()
    hands()
    energy_layers()
    print(f"ui art written to {OutputDir}")


if __name__ == "__main__":
    main()
