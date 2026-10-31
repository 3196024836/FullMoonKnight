"""把生成的女骑士部件图拼成战斗姿势的分层（assets/generated/character/layers/，进 git）。

来源：
  assets/generated/character/knight_parts.png       部件图（imagen 以全身图为参考生成，13 个部件分开摆放、灰底）
  assets/generated/character/knight_full.png        全身图（姿势参考，原作小红帽画风，朝右）
  assets/generated/character/knight_head_blink.png  闭眼的头（只取眼睛一块，做眨眼）
处理：
  - 按背景色切出部件：颜色离背景越远越不透明，边缘去掉背景灰（反混合）
  - 每个部件给两对对应点（部件图 -> 全身图），算出旋转和平移（缩放默认取 1），放回全身图的姿势
  - 全身图坐标再整体缩放平移到骨骼画布（1024，原点在两脚之间的地面）
  - 闭眼图对齐到部件图的头（ECC 仿射），只取眼睛附近与原图差异大的像素，按头的矩阵放回，输出 eyes_closed
输出：layers/<名>.png、layers/layers.json {名: {"x", "y", "depth"}}，以及对照图 .work/knight_art/assembled.png
用法：python tools/assemble_knight_parts.py
"""

import json
import math
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
CharacterDir = RepoDir / "assets" / "generated" / "character"  # 角色素材目录
SheetPath = CharacterDir / "knight_parts.png"  # 部件图
BlinkPath = CharacterDir / "knight_head_blink.png"  # 闭眼的头（imagen 编辑部件图里的头得到）
FullPath = CharacterDir / "knight_full.png"  # 全身图
OutputDir = CharacterDir / "layers"  # 输出目录
CheckPath = RepoDir / ".work" / "knight_art" / "assembled.png"  # 对照图
Canvas = 1024  # 骨骼画布边长
CanvasOrigin = (540, 865)  # 骨骼画布原点（两脚之间的地面）
FullOrigin = (660, 1172)  # 全身图里两脚之间的地面
CanvasScale = 0.61  # 全身图到骨骼画布的缩放
AlphaRange = (14, 46)  # 与背景的颜色距离：低于下限全透明，高于上限全不透明

# 部件：名字、部件图里的大致中心（用来认连通块）、两对对应点（部件图 -> 全身图）、缩放（None 为按对应点计算）
# 列表顺序就是从后到前的绘制顺序；头的对应点由 SIFT 特征匹配求出，其余按全身图手工对位
Parts = [
    ("cape", (262, 643), ((490, 400), (745, 385)), ((15, 580), (180, 625)), None),
    ("shield", (720, 1041), ((700, 895), (905, 395)), ((780, 1200), (950, 800)), None),
    ("shield_arm", (1173, 564), ((1150, 450), (800, 440)), ((1235, 620), (895, 560)), None),
    ("leg_b", (946, 1042), ((950, 915), (470, 1010)), ((930, 1140), (440, 1150)), None),
    ("leg_f", (1150, 1035), ((1140, 900), (860, 1030)), ((1130, 1130), (880, 1160)), None),
    ("skirt", (674, 700), ((690, 445), (680, 612)), ((675, 860), (650, 1060)), None),
    ("torso", (1043, 221), ((1030, 75), (690, 340)), ((1035, 315), (672, 585)), None),
    ("hair_b", (764, 228), ((795, 90), (805, 280)), ((735, 345), (725, 470)), None),
    ("head", (271, 202), ((60, 200), (420, 212)), ((460, 200), (856, 213)), None),
    ("hair_a", (601, 229), ((650, 75), (615, 265)), ((525, 245), (468, 418)), None),
    ("arm_upper", (905, 526), ((905, 445), (590, 455)), ((905, 605), (545, 555)), None),
    ("sword", (292, 1053), ((120, 1045), (458, 700)), ((570, 1055), (105, 812)), None),
    ("arm_lower", (1034, 661), ((1010, 535), (555, 540)), ((1065, 775), (455, 705)), None),
]
BlinkBox = (10, 30, 490, 360)  # 闭眼图对应部件图里头部的区域（生成闭眼图时按这块放大 2.5 倍）
EyeBox = (225, 135, 425, 245)  # 眼睛所在的范围（BlinkBox 内的坐标），只在这里取差异



def cut_parts(sheet: np.ndarray) -> dict[str, Image.Image]:
    """按背景色切出各部件（RGBA，画在部件图大小的透明画布上）。"""
    rgb = sheet[:, :, :3].astype(float)
    background = np.median(np.concatenate([rgb[:8].reshape(-1, 3), rgb[-8:].reshape(-1, 3)]), axis=0)
    distance = np.abs(rgb - background).sum(axis=2)
    alpha = np.clip((distance - AlphaRange[0]) / (AlphaRange[1] - AlphaRange[0]), 0, 1)
    # 连通块：闭运算连上细缝，按中心认部件
    solid = cv2.morphologyEx((distance > AlphaRange[1]).astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    count, labels, stats, centroids = cv2.connectedComponentsWithStats(solid)
    # 大块（排除噪点）
    big = [i for i in range(1, count) if stats[i, cv2.CC_STAT_AREA] > 2000]
    parts = {}
    for name, center, _, _, _ in Parts:
        # 质心离给定中心最近的大块
        label = min(big, key=lambda i: math.hypot(centroids[i][0] - center[0], centroids[i][1] - center[1]))
        # 填上内部的洞：银发的暗部接近背景灰，按颜色会被切成半透明
        filled = fill_holes((labels == label).astype(np.uint8))
        # 内部全不透明，只在外轮廓一圈保留按颜色算的柔边；柔边只外扩 1 像素，免得带上背景噪点
        interior = cv2.erode(filled, np.ones((5, 5), np.uint8)) > 0
        region = cv2.dilate(filled, np.ones((3, 3), np.uint8)) > 0
        part_alpha = np.where(interior, 1.0, np.where(region, alpha, 0))
        # 反混合：边缘像素去掉背景灰
        safe = np.maximum(part_alpha, 1e-3)[:, :, None]
        colour = np.clip((rgb - (1 - part_alpha[:, :, None]) * background) / safe, 0, 255)
        rgba = np.dstack([colour, part_alpha * 255]).astype(np.uint8)
        parts[name] = Image.fromarray(rgba)
    return parts


def fill_holes(mask: np.ndarray) -> np.ndarray:
    """填上掩码内部的洞（从边框灌水，灌不到的都算内部）。"""
    padded = np.pad(mask, 1)
    flood = padded.copy()
    cv2.floodFill(flood, np.zeros((flood.shape[0] + 2, flood.shape[1] + 2), np.uint8), (0, 0), 1)
    return (padded | (1 - flood))[1:-1, 1:-1]


def similarity(source: tuple, target: tuple, scale: float | None) -> np.ndarray:
    """两对对应点算出的 2x3 仿射矩阵（旋转、平移，缩放可固定）。"""
    (p1, p2), (q1, q2) = source, target
    vp = np.subtract(p2, p1)
    vq = np.subtract(q2, q1)
    angle = math.atan2(vq[1], vq[0]) - math.atan2(vp[1], vp[0])
    factor = scale if scale is not None else float(np.hypot(*vq) / np.hypot(*vp))
    c, s = factor * math.cos(angle), factor * math.sin(angle)
    rotation = np.array([[c, -s], [s, c]])
    offset = np.asarray(q1) - rotation @ np.asarray(p1)
    return np.hstack([rotation, offset[:, None]])


def to_canvas() -> np.ndarray:
    """全身图坐标到骨骼画布的 2x3 矩阵。"""
    return np.array([[CanvasScale, 0, CanvasOrigin[0] - CanvasScale * FullOrigin[0]],
                     [0, CanvasScale, CanvasOrigin[1] - CanvasScale * FullOrigin[1]]])


def compose(first: np.ndarray, second: np.ndarray) -> np.ndarray:
    """矩阵相乘：先 first 再 second。"""
    return (np.vstack([second, [0, 0, 1]]) @ np.vstack([first, [0, 0, 1]]))[:2]


def blink_patch(sheet: np.ndarray) -> Image.Image:
    """闭眼补丁：对齐到部件图里的头，只保留眼睛附近的差异（柔边），画在部件图大小的透明画布上。"""
    left, top, right, bottom = BlinkBox
    reference = sheet[top:bottom, left:right, :3].astype(np.float32)
    raw = Image.open(BlinkPath).convert("RGB").resize((right - left, bottom - top), Image.LANCZOS)
    moving = np.asarray(raw).astype(np.float32)
    # 对齐：生成图与原图只差一点缩放和平移
    warp = np.eye(2, 3, dtype=np.float32)
    criteria = (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-6)
    gray = cv2.COLOR_RGB2GRAY
    _, warp = cv2.findTransformECC(cv2.cvtColor(reference, gray), cv2.cvtColor(moving, gray),
                                   warp, cv2.MOTION_AFFINE, criteria)
    aligned = cv2.warpAffine(moving, warp, (right - left, bottom - top), flags=cv2.INTER_LINEAR + cv2.WARP_INVERSE_MAP,
                             borderMode=cv2.BORDER_REPLICATE)
    # 差异掩码：限定在眼睛范围，膨胀后羽化
    changed = (np.abs(aligned - reference).sum(axis=2) > 60).astype(np.uint8)
    zone = np.zeros_like(changed)
    zone[EyeBox[1]:EyeBox[3], EyeBox[0]:EyeBox[2]] = 1
    mask = cv2.dilate(changed * zone, np.ones((9, 9), np.uint8)).astype(np.float32)
    mask = cv2.GaussianBlur(mask, (9, 9), 0)
    patch = np.zeros((sheet.shape[0], sheet.shape[1], 4), np.uint8)
    patch[top:bottom, left:right] = np.dstack([aligned, mask * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(patch)


def main() -> None:
    """命令行入口。"""
    sheet = np.asarray(Image.open(SheetPath).convert("RGBA"))
    full = Image.open(FullPath).convert("RGBA")
    parts = cut_parts(sheet)
    eyes = blink_patch(sheet)
    OutputDir.mkdir(parents=True, exist_ok=True)
    for old in OutputDir.glob("*.png"):
        old.unlink()
    table = {}
    # 对照图：左边原全身图，右边拼装结果（都在全身图坐标里）
    assembled = Image.new("RGBA", full.size, (128, 128, 128, 255))
    canvas_matrix = to_canvas()
    for index, (name, _, first, second, scale) in enumerate(Parts):
        matrix = similarity((first[0], second[0]), (first[1], second[1]), scale)
        image = parts[name]
        placed = cv2.warpAffine(np.asarray(image), matrix, full.size, flags=cv2.INTER_LINEAR,
                                borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        assembled.alpha_composite(Image.fromarray(placed))
        layer = cv2.warpAffine(np.asarray(image), compose(matrix, canvas_matrix), (Canvas, Canvas),
                               flags=cv2.INTER_AREA, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        picture = Image.fromarray(layer)
        box = picture.getbbox()
        picture.crop(box).save(OutputDir / f"{name}.png")
        # 深度：越靠后越大（与旧版 see-through 的约定一致）
        table[name] = {"x": box[0], "y": box[1], "depth": round(1 - index / len(Parts), 4)}
        if name == "head":
            # 闭眼补丁用头的矩阵放回，不进对照图
            layer = cv2.warpAffine(np.asarray(eyes), compose(matrix, canvas_matrix), (Canvas, Canvas),
                                   flags=cv2.INTER_AREA, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
            picture = Image.fromarray(layer)
            box = picture.getbbox()
            picture.crop(box).save(OutputDir / "eyes_closed.png")
            table["eyes_closed"] = {"x": box[0], "y": box[1], "depth": table[name]["depth"]}
    (OutputDir / "layers.json").write_text(json.dumps(table, indent=2) + "\n", encoding="utf-8")
    CheckPath.parent.mkdir(parents=True, exist_ok=True)
    check = Image.new("RGBA", (full.width * 2, full.height), (128, 128, 128, 255))
    check.alpha_composite(full, (0, 0))
    check.alpha_composite(assembled, (full.width, 0))
    check.convert("RGB").save(CheckPath)
    print(f"assembled {len(table)} layers into {OutputDir}; check {CheckPath}")


if __name__ == "__main__":
    main()
