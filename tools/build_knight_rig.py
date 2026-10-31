"""用分层立绘绑定女骑士的 Spine 4.2 骨骼（尖塔 2 角色风格的剪纸动画）。

来源：assets/generated/character/layers/（assemble_knight_parts.py 由生成的部件图拼成，画布 1024x1024）。
做法：
  - 骨骼按画布上的关节点放置，全部旋转为 0（动画里的旋转角即世界角度的增量）
  - 每层生成网格（按像素格三角化），顶点按到可用骨骼线段的距离加权，关节处平滑弯曲
  - 各层打包成一张图集
  - 刀光、盾光、脚下影子由程序生成；闭眼、刀光、盾光默认隐藏，动作里切换
  - 动作在 knight_animations.py
输出：assets/mod/animations/knight/knight.skel、knight.atlas、knight.png（打包脚本转成导入格式）
用法：python tools/build_knight_rig.py [--preview]（预览帧写到 .work/spine_preview/）
"""

import hashlib
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import knight_animations  # noqa: E402
import spine_binary as sb  # noqa: E402
import spine_pose as sp  # noqa: E402

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
LayerDir = RepoDir / "assets" / "generated" / "character" / "layers"  # 分层来源
OutputDir = RepoDir / "assets" / "mod" / "animations" / "knight"  # 输出目录
PreviewDir = RepoDir / ".work" / "spine_preview"  # 预览输出
RuntimeVersion = "4.2.43"  # 游戏的 Spine 运行时版本
Origin = (540, 865)  # 骨骼原点（两脚之间的地面），画布坐标
GridStep = 12  # 网格格子边长（像素）
AtlasSize = (2048, 1024)  # 图集页宽高
Padding = 2  # 图集内各图间距
ShadowSize = (440, 70)  # 脚下影子的宽高（像素）

# 骨骼：名字、父骨骼、关节点、末端点（画布坐标）。末端点只用来算权重。
# 腿以脚为轴挂在 root 上（身体移动时脚不滑，腿上端藏在裙下）；头发、兜帽尖、披风、裙摆是跟随摆动的次级骨骼
Bones = [
    ("root", None, (540, 865), (540, 865)),
    ("hip", "root", (545, 525), (545, 560)),
    ("torso", "hip", (545, 525), (555, 378)),
    ("neck", "torso", (555, 378), (560, 350)),
    ("head", "neck", (560, 350), (585, 250)),
    ("hood_tip", "head", (455, 290), (372, 348)),
    ("hair_a1", "head", (495, 315), (470, 400)),
    ("hair_a2", "hair_a1", (470, 400), (440, 480)),
    ("hair_b1", "head", (602, 322), (610, 385)),
    ("hair_b2", "hair_b1", (610, 385), (618, 445)),
    ("cape_root", "torso", (555, 400), (440, 465)),
    ("cape1", "cape_root", (440, 465), (330, 545)),
    ("cape2", "cape1", (330, 545), (300, 720)),
    ("skirt_b", "hip", (480, 560), (370, 770)),
    ("skirt_f", "hip", (610, 560), (720, 770)),
    ("shield_arm", "torso", (625, 432), (685, 490)),
    ("shield", "shield_arm", (685, 490), (700, 600)),
    ("glow", "shield", (688, 515), (688, 515)),
    ("leg_b", "root", (418, 858), (432, 782)),
    ("leg_f", "root", (690, 852), (668, 778)),
    ("arm_upper", "torso", (495, 428), (468, 490)),
    ("arm_lower", "arm_upper", (468, 490), (418, 572)),
    ("hand", "arm_lower", (418, 572), (405, 585)),
    ("sword", "hand", (410, 580), (208, 640)),
    ("slash", "torso", (495, 428), (495, 428)),
]

# 各层（从后到前）和可用的骨骼。shadow、slash、glow 是程序生成的特效层，不在分层来源里
# 部件是分开画的，多数只挂一两根骨骼（剪纸式刚性摆动），关节处靠部件互相遮挡
Layers = [
    ("shadow", ["root"]),
    ("cape", ["torso", "cape_root", "cape1", "cape2"]),
    ("shield", ["shield"]),
    ("glow", ["glow"]),
    ("shield_arm", ["shield_arm"]),
    ("leg_b", ["leg_b"]),
    ("leg_f", ["leg_f"]),
    ("skirt", ["hip", "skirt_b", "skirt_f"]),
    ("torso", ["hip", "torso"]),
    ("hair_b", ["hair_b1", "hair_b2"]),
    ("head", ["neck", "head", "hood_tip"]),
    ("eyes_closed", ["head"]),
    ("hair_a", ["hair_a1", "hair_a2"]),
    ("arm_upper", ["arm_upper"]),
    ("sword", ["sword"]),
    ("arm_lower", ["arm_lower", "hand"]),
    ("slash", ["slash"]),
]
HiddenSlots = {"eyes_closed", "glow", "slash"}  # 默认不显示的插槽（动作里切换）
AdditiveSlots = {"glow", "slash"}  # 叠加混合的插槽
SlashCenter = (495, 428)  # 刀光圆心（肩），画布坐标
SlashRadius = (150, 300)  # 刀光内外半径
SlashAngles = (118, -38)  # 刀光起止角度（世界角度，逆时针为正）


def to_spine(point: tuple[float, float]) -> tuple[float, float]:
    """画布坐标转骨骼坐标（y 向上，原点在两脚之间）。"""
    return point[0] - Origin[0], Origin[1] - point[1]


def shadow() -> Image.Image:
    """脚下的影子：模糊的半透明黑色椭圆。"""
    width, height = ShadowSize
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    ImageDraw.Draw(image).ellipse((14, 14, width - 14, height - 14), fill=(0, 0, 0, 110))
    return image.filter(ImageFilter.GaussianBlur(7))


def slash() -> tuple[Image.Image, tuple[int, int]]:
    """刀光：以肩为圆心的弧，起点细而淡，末端（剑当前的位置）宽而亮，外沿白、内沿淡紫。返回图片和画布偏移。"""
    y, x = np.mgrid[0:1024, 0:1024].astype(float)
    dx, dy = x - SlashCenter[0], SlashCenter[1] - y
    radius = np.hypot(dx, dy)
    angle = np.degrees(np.arctan2(dy, dx))
    start, end = SlashAngles
    u = (start - angle) / (start - end)  # 0 在起点，1 在末端
    inner, outer = SlashRadius
    width = 12 + (outer - inner - 12) * np.clip(u, 0, 1) ** 1.4
    depth = (outer - radius) / np.maximum(width, 1)  # 0 在外沿，1 在内沿
    inside = (u >= 0) & (u <= 1) & (depth >= 0) & (depth <= 1)
    # 外沿亮，向内渐隐；起点渐隐，末端圆滑收口
    alpha = np.clip(1 - depth, 0, 1) ** 1.6 * np.clip(u * 1.5, 0, 1) * np.clip((1 - u) * 14, 0, 1)
    alpha = np.where(inside, alpha, 0)
    edge = np.clip(1 - np.abs(depth - 0.06) / 0.08, 0, 1) * inside
    tint = np.clip(depth * 1.4, 0, 1)[:, :, None]
    colour = (1 - tint) * np.array([255, 255, 255]) + tint * np.array([186, 150, 255])
    colour = colour * (1 - edge[:, :, None]) + edge[:, :, None] * 255
    image = Image.fromarray(np.dstack([colour, np.clip(alpha + edge * 0.6, 0, 1) * 235]).astype(np.uint8))
    image = image.filter(ImageFilter.GaussianBlur(1.2))
    box = image.getbbox()
    return image.crop(box), (box[0], box[1])


def glow(shield: Image.Image, offset: tuple[int, int]) -> tuple[Image.Image, tuple[int, int]]:
    """盾光：盾的轮廓外扩、模糊，淡紫白色。返回图片和画布偏移。"""
    pad = 28
    alpha = np.pad(np.asarray(shield)[:, :, 3], pad).astype(np.uint8)
    halo = np.asarray(Image.fromarray(alpha).filter(ImageFilter.MaxFilter(15)).filter(ImageFilter.GaussianBlur(10)))
    inner = np.asarray(Image.fromarray(alpha).filter(ImageFilter.GaussianBlur(4)))
    strength = np.clip(halo / 255 * 0.8 - inner / 255 * 0.45, 0, 1) + inner / 255 * 0.18
    colour = np.zeros(strength.shape + (3,)) + np.array([228, 218, 255])
    image = Image.fromarray(np.dstack([colour, strength * 255]).astype(np.uint8))
    return image, (offset[0] - pad, offset[1] - pad)


def bone_data() -> list[dict]:
    """骨骼数据：位置为相对父骨骼的偏移，旋转为 0。"""
    joints = {name: joint for name, _, joint, _ in Bones}
    result = []
    for name, parent, joint, _ in Bones:
        x, y = to_spine(joint)
        if parent is not None:
            px, py = to_spine(joints[parent])
            x, y = x - px, y - py
        bone = {"name": name, "rotation": 0.0, "x": float(x), "y": float(y), "scaleX": 1.0, "scaleY": 1.0,
                "shearX": 0.0, "shearY": 0.0, "length": 0.0, "inherit": 0, "skin": False}
        if parent is not None:
            bone["parent"] = parent
        result.append(bone)
    return result


def segment_distance(points: np.ndarray, a: tuple, b: tuple) -> np.ndarray:
    """点到线段的距离。"""
    a, b = np.array(a, float), np.array(b, float)
    ab = b - a
    length = float(ab @ ab)
    t = np.zeros(len(points)) if length == 0 else np.clip((points - a) @ ab / length, 0, 1)
    return np.linalg.norm(points - (a + t[:, None] * ab), axis=1)


def grid_mesh(image: Image.Image) -> tuple[np.ndarray, list[int]]:
    """按像素格三角化不透明区域：返回顶点（图片像素坐标）和三角形索引。"""
    alpha = np.asarray(image)[:, :, 3] > 8
    width, height = image.size
    columns, rows = math.ceil(width / GridStep), math.ceil(height / GridStep)
    index: dict[tuple[int, int], int] = {}
    vertices: list[tuple[float, float]] = []
    triangles: list[int] = []

    def vertex(i: int, j: int) -> int:
        if (i, j) not in index:
            index[(i, j)] = len(vertices)
            vertices.append((min(i * GridStep, width), min(j * GridStep, height)))
        return index[(i, j)]

    for j in range(rows):
        for i in range(columns):
            if not alpha[j * GridStep:(j + 1) * GridStep, i * GridStep:(i + 1) * GridStep].any():
                continue
            a, b, c, d = vertex(i, j), vertex(i + 1, j), vertex(i + 1, j + 1), vertex(i, j + 1)
            triangles += [a, b, c, a, c, d]
    return np.array(vertices, float), triangles


def weights(points: np.ndarray, names: list[str]) -> list[list[tuple[int, float]]]:
    """顶点权重：到骨骼线段距离的反比三次方，取最大的两个，归一化。"""
    table = {name: (i, joint, tip) for i, (name, _, joint, tip) in enumerate(Bones)}
    influence = np.stack([1 / (segment_distance(points, table[n][1], table[n][2]) + 6) ** 3 for n in names], axis=1)
    result = []
    for row in influence:
        best = np.argsort(row)[::-1][:2]
        total = row[best].sum()
        entries = [(table[names[k]][0], row[k] / total) for k in best if row[k] / total > 0.04]
        norm = sum(w for _, w in entries)
        result.append([(bone, w / norm) for bone, w in entries])
    return result


def mesh_attachment(name: str, image: Image.Image, offset: tuple[int, int], names: list[str],
                    world: dict[str, tuple[float, float]]) -> dict:
    """一层的加权网格附件。"""
    local, triangles = grid_mesh(image)
    canvas = local + np.array(offset, float)
    vertex_weights = weights(canvas, names)
    vertices = []
    for (cx, cy), entries in zip(canvas, vertex_weights):
        sx, sy = to_spine((cx, cy))
        vertices.append([(bone, sx - world[Bones[bone][0]][0], sy - world[Bones[bone][0]][1], weight)
                         for bone, weight in entries])
    count = len(local)
    uvs = []
    for x, y in local:
        uvs += [x / image.width, y / image.height]
    hull = count * 2 - 2 - len(triangles) // 3  # 网格边界顶点数（欧拉公式）
    return {"name": name, "type": "mesh", "path": None, "color": "ffffffff", "hull": hull,
            "vertexCount": count, "vertices": vertices, "bonesWeighted": True, "uvs": uvs, "triangles": triangles}


def pack(images: dict[str, Image.Image]) -> tuple[Image.Image, dict[str, tuple[int, int, int, int]]]:
    """按高度排序逐行摆放到一页图集。"""
    page = Image.new("RGBA", AtlasSize, (0, 0, 0, 0))
    regions = {}
    x = y = row = 0
    for name in sorted(images, key=lambda n: -images[n].height):
        image = images[name]
        if x + image.width > AtlasSize[0]:
            x, y, row = 0, y + row + Padding, 0
        if y + image.height > AtlasSize[1]:
            raise SystemExit("atlas page too small")
        page.alpha_composite(image, (x, y))
        regions[name] = (x, y, image.width, image.height)
        x += image.width + Padding
        row = max(row, image.height)
    return page, regions


def atlas_text(regions: dict[str, tuple[int, int, int, int]]) -> str:
    """图集描述文件（与原作图集相同的格式）。"""
    lines = ["knight.png", f"size: {AtlasSize[0]},{AtlasSize[1]}", "format: RGBA8888", "filter: Linear,Linear",
             "repeat: none"]
    for name, (x, y, w, h) in regions.items():
        lines += [name, "  rotate: false", f"  xy: {x}, {y}", f"  size: {w}, {h}", f"  orig: {w}, {h}",
                  "  offset: 0, 0", "  index: -1"]
    return "\n".join(lines) + "\n"


def build() -> tuple[dict, Image.Image, dict]:
    """生成骨骼数据、图集图片和图集区域。"""
    table = json.loads((LayerDir / "layers.json").read_text(encoding="utf-8"))
    world = {name: to_spine(joint) for name, _, joint, _ in Bones}
    effects = {"shadow", "slash", "glow"}
    images = {name: Image.open(LayerDir / f"{name}.png").convert("RGBA") for name, _ in Layers if name not in effects}
    offsets = {name: (table[name]["x"], table[name]["y"]) for name in images}
    images["shadow"] = shadow()
    offsets["shadow"] = (Origin[0] - ShadowSize[0] // 2, Origin[1] - ShadowSize[1] // 2)
    images["slash"], offsets["slash"] = slash()
    images["glow"], offsets["glow"] = glow(images["shield"], offsets["shield"])
    page, regions = pack(images)
    slots, attachments = [], {}
    for name, names in Layers:
        # 槽位挂在第一根可用骨骼上（加权网格的位置只由权重决定）
        slots.append({"name": name, "bone": names[0], "color": "ffffffff", "dark": None,
                      "attachment": None if name in HiddenSlots else name, "blend": 1 if name in AdditiveSlots else 0})
        attachments[name] = {name: mesh_attachment(name, images[name], offsets[name], names, world)}
    top = min(to_spine((0, offsets[n][1]))[1] for n, _ in Layers)
    skeleton = {
        "skeleton": {"hash": 0, "spine": RuntimeVersion, "x": -Origin[0], "y": 0.0, "width": 1024.0,
                     "height": float(Origin[1] - top), "referenceScale": 100.0},
        "nonessential": False,
        "bones": bone_data(),
        "slots": slots,
        "ik": [],
        "skins": {"default": attachments},
        "events": {},
        "animations": {},
    }
    skeleton["animations"] = knight_animations.build(skeleton)
    return skeleton, page, regions


def main() -> None:
    """命令行入口。"""
    skeleton, page, regions = build()
    data = sb.write_42(skeleton)
    # 内容哈希，便于运行时区分版本
    skeleton["skeleton"]["hash"] = int.from_bytes(hashlib.sha1(data).digest()[:8], "big", signed=True)
    data = sb.write_42(skeleton)
    # 自检：读回再写出，字节不变
    if sb.write_42(sb.read_42(data)) != data:
        raise SystemExit("4.2 round trip mismatch")
    OutputDir.mkdir(parents=True, exist_ok=True)
    (OutputDir / "knight.skel").write_bytes(data)
    (OutputDir / "knight.atlas").write_text(atlas_text(regions), encoding="utf-8")
    page.save(OutputDir / "knight.png")
    print(f"knight.skel {len(data)} bytes, animations: {', '.join(skeleton['animations'])}")
    if "--preview" in sys.argv:
        preview(skeleton, page, regions)


def preview(skeleton: dict, page: Image.Image, regions: dict) -> None:
    """渲染绑定姿势（带骨骼）和每个动作的帧序列（横排）。"""
    PreviewDir.mkdir(parents=True, exist_ok=True)
    for old in PreviewDir.glob("*.png"):
        old.unlink()
    size, origin = (1000, 900), (500, 860)
    sp.render(skeleton, regions, page, sp.pose(skeleton), size=size, origin=origin, bones=True).save(
        PreviewDir / "setup.png")
    for name, animation in skeleton["animations"].items():
        duration = knight_animations.duration(animation)
        count = 8
        strip = Image.new("RGBA", (300 * count, 270))
        for i in range(count):
            frame = sp.render(skeleton, regions, page, sp.pose(skeleton, name, duration * i / (count - 1)), size=size,
                              origin=origin)
            strip.paste(frame.resize((300, 270)), (300 * i, 0))
        strip.save(PreviewDir / f"{name}.png")
        print(f"preview {name}: {duration:.2f}s")


if __name__ == "__main__":
    main()
