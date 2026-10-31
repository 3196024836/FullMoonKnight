"""选人背景的 Spine 4.2 骨骼动画：月夜雪坡上的小红帽（原版选人背景同样是 SpineSprite + NSpineAutoPlayer + 粒子）。

来源（assets/generated/select/，imagen 生成）：
  character.png        灰底人物（开朗：近手伸直、剑顺着手臂指向左下，远手持盾只露内侧）
  character_blink.png  人物头部闭眼图（以 CharacterBlinkBox 区域放大 2.5 倍编辑）
  clouds.png           灰底的三朵云
  scene.png            不含人物的雪夜背景（由 make_ui_art.py 输出为 ui/char_select_bg.png，场景里用 TextureRect 铺底）
做法：
  - 人物整张切出后做一张网格，按颜色和区域蒙版加权到各骨骼：披风（紫色）、两缕白发、兜帽尖、持剑的近手、剑、裙摆，其余按高度分给臀、躯干、头
  - 闭眼只取眼睛附近的差异像素（同 assemble_knight_parts.py 的做法），挂在头上
  - 云、月光、剑尖闪光是单骨骼网格；月光和闪光为叠加混合
  - 只有一个动作 animation（NSpineAutoPlayer 要求只有一个），12 秒循环：呼吸、风吹披风和头发、两次眨眼、云来回漂、月光脉动、剑尖两次闪光
坐标：骨骼原点在屏幕（1920x1080）底边中点，y 向上；屏幕像素即骨骼单位。
输出：assets/mod/animations/knight_select/knight_select.skel、.atlas、.png，以及海报 assets/generated/select/poster.jpg
用法：python tools/build_select_rig.py [--preview]（预览帧写到 .work/select_preview/）
"""

import hashlib
import math
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import spine_binary as sb  # noqa: E402
import spine_pose as sp  # noqa: E402
from assemble_knight_parts import fill_holes  # noqa: E402

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
SourceDir = RepoDir / "assets" / "generated" / "select"  # 来源目录
OutputDir = RepoDir / "assets" / "mod" / "animations" / "knight_select"  # 输出目录
PreviewDir = RepoDir / ".work" / "select_preview"  # 预览输出
PosterPath = SourceDir / "poster.jpg"  # 选人海报（背景加第 0 秒的人物；选人按钮和创意工坊封面用）
PosterLimit = 1_000_000  # 海报文件大小上限（创意工坊封面要求小于 1 MB）
DenoiseStrength = 7  # 人物图去噪强度（非局部均值；生成图上的浅色斑点、头发里的灰色脏块，线稿保留）
RuntimeVersion = "4.2.43"  # 游戏的 Spine 运行时版本
Screen = (1920, 1080)  # 屏幕尺寸（场景控件大小）
AtlasSize = (2048, 2048)  # 图集页宽高
Padding = 2  # 图集内各图间距
GridStep = 20  # 人物网格格子边长（人物原图像素）
AlphaRange = (14, 46)  # 与灰底的颜色距离：低于下限全透明，高于上限全不透明
Loop = 12.0  # 循环时长（秒）
SampleRate = 30  # 采样帧率

# 选人界面把背景放大 1.1 倍并随鼠标平移，实际可见的大约是屏幕坐标 x 150–1895、y 67–1049；左侧中部被角色介绍面板挡住
# 背景：从生成图裁出 16:9 再铺满屏幕（裁掉顶部一截，让月亮高过介绍面板）
SceneCrop = (0, 90, 1513)  # 生成图里裁取的左、上、宽
# 人物原图像素 -> 屏幕像素：缩放和平移（人物在画面右侧，剑尖留在可见范围内，脚出画）
CharacterScale = 0.92  # 人物缩放
CharacterAnchor = ((690, 45), (1290, 105))  # 人物原图的点 -> 屏幕上的点
CharacterBlinkBox = (520, 60, 900, 440)  # 闭眼图对应人物原图的区域
EyeBox = (105, 120, 310, 225)  # 眼睛所在范围（CharacterBlinkBox 内的坐标）

# 人物骨骼：名字、父骨骼、关节点（人物原图像素）
Bones = [
    ("hip", "character", (690, 640)),
    ("torso", "hip", (690, 600)),
    ("head", "torso", (720, 400)),
    ("hood_tip", "head", (520, 190)),
    ("hair_l", "head", (560, 330)),
    ("hair_r", "head", (765, 330)),
    ("arm", "torso", (585, 440)),  # 持剑的近手：以肩为轴，跟躯干同相轻摆
    ("sword", "arm", (455, 705)),  # 剑：跟手一起动（剑压在裙子上，单独摆会扯动裙子）
    ("glint", "sword", (52, 973)),
    ("cape1", "torso", (520, 470)),
    ("cape2", "cape1", (360, 620)),
    ("cape3", "cape2", (240, 820)),
    ("skirt_l", "hip", (520, 780)),
    ("skirt_r", "hip", (860, 780)),
]

# 区域：(多边形或盒子, 羽化半径)；多边形顶点为人物原图像素
HoodTipZone = [(380, 250), (470, 160), (560, 160), (560, 260), (470, 350), (380, 350)]  # 兜帽尖（左边垂下的一片）
HairLeftZone = [(455, 320), (645, 320), (645, 445), (455, 445)]  # 左边一缕白发（避开袖口）
HairRightZone = [(700, 320), (840, 320), (840, 490), (700, 490)]  # 右边一缕白发
ArmZone = [(560, 415), (625, 425), (610, 560), (560, 640), (525, 690), (500, 752), (440, 762), (412, 700), (440, 650),
           (470, 600), (530, 520)]  # 伸直的近手（袖子、白毛袖口、手套、手）
SwordZone = [(378, 678), (452, 688), (432, 772), (72, 992), (32, 988), (44, 952)]  # 护手和剑身（不含手）
HeadZone = [(500, 30), (890, 30), (890, 330), (830, 420), (640, 420), (560, 330), (500, 250)]  # 头（兜帽、脸）

# 循环摆动：{骨骼: {种类: (幅度, 周期, 相位)}}；translate 的幅度是 (x, y)，scale 为 1 加幅度
# 幅度要小：披风是三节链，转角逐节累加；手臂、头和躯干同相，免得手在脸上滑动
Motion = {
    "hip": {"translate": ((0, -2), 3.0, 0.0)},
    "torso": {"rotate": (0.3, 3.0, 0.3), "scale": ((0.0, 0.01), 3.0, 0.2)},
    "head": {"rotate": (0.8, 3.0, 0.5)},
    "hood_tip": {"rotate": (0.8, 4.0, 1.0)},
    "hair_l": {"rotate": (0.8, 4.0, 0.9)},
    "hair_r": {"rotate": (0.8, 4.0, 1.1)},
    "arm": {"rotate": (0.3, 3.0, 0.3)},
    "cape1": {"rotate": (0.4, 4.0, 0.8)},
    "cape2": {"rotate": (0.6, 4.0, 1.3)},
    "cape3": {"rotate": (0.9, 4.0, 1.8)},
    "skirt_l": {"rotate": (0.5, 4.0, 1.0)},
    "skirt_r": {"rotate": (0.4, 4.0, 1.2)},
    "cloud_a": {"translate": ((60, 0), 12.0, 0.0)},
    "cloud_b": {"translate": ((-45, 0), 12.0, 1.5)},
    "cloud_c": {"translate": ((35, 0), 12.0, 3.4)},
}
# 云：名字、在云图里的大致中心（认连通块）、屏幕位置（中心）、缩放、不透明度
Clouds = [
    ("cloud_a", (880, 270), (1060, 130), 0.6, "ffffffc8"),
    ("cloud_b", (430, 655), (1660, 330), 0.55, "ffffffb8"),
    ("cloud_c", (1385, 665), (330, 165), 0.5, "ffffffb0"),
]
MoonSource = (509, 288, 104)  # 生成图里月亮的圆心和半径
Blinks = [2.2, 8.4, 8.75]  # 眨眼开始时间（第二次连眨两下）
BlinkLength = 0.12  # 闭眼时长
Glints = [4.0, 10.2]  # 剑尖闪光时间


def scene_scale() -> float:
    """背景从生成图到屏幕的缩放。"""
    return Screen[0] / SceneCrop[2]


def background() -> Image.Image:
    """选人背景：按 SceneCrop 裁出 16:9 后缩放到屏幕大小（make_ui_art.py 用它输出 ui/char_select_bg.png）。"""
    left, top, width = SceneCrop
    image = Image.open(SourceDir / "scene.png").convert("RGB")
    return image.crop((left, top, left + width, top + round(width * 9 / 16))).resize(Screen, Image.LANCZOS)


MoonCenter = ((MoonSource[0] - SceneCrop[0]) * scene_scale(), (MoonSource[1] - SceneCrop[1]) * scene_scale())  # 月亮屏幕位置
MoonRadius = round(MoonSource[2] * scene_scale())  # 月亮半径（屏幕像素）


def to_screen(point) -> np.ndarray:
    """人物原图像素 -> 屏幕像素。"""
    (cx, cy), (sx, sy) = CharacterAnchor
    point = np.asarray(point, float)
    return (point - np.array([cx, cy])) * CharacterScale + np.array([sx, sy])


def to_spine(point) -> np.ndarray:
    """屏幕像素 -> 骨骼坐标（原点在底边中点，y 向上）。"""
    point = np.asarray(point, float)
    return np.stack([point[..., 0] - Screen[0] / 2, Screen[1] - point[..., 1]], axis=-1)


# ===== 切图 =====


def cut(image: Image.Image, center: tuple[int, int] | None = None) -> tuple[Image.Image, tuple[int, int]]:
    """按灰底切出离 center 最近（缺省为最大）的连通块，边缘去灰。返回裁好的 RGBA 和它在原图的左上角。"""
    rgb = np.asarray(image.convert("RGB")).astype(float)
    background = np.median(np.concatenate([rgb[:8].reshape(-1, 3), rgb[-8:].reshape(-1, 3)]), axis=0)
    distance = np.abs(rgb - background).sum(axis=2)
    alpha = np.clip((distance - AlphaRange[0]) / (AlphaRange[1] - AlphaRange[0]), 0, 1)
    solid = cv2.morphologyEx((distance > AlphaRange[1]).astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    count, labels, stats, centroids = cv2.connectedComponentsWithStats(solid)
    big = [i for i in range(1, count) if stats[i, cv2.CC_STAT_AREA] > 2000]
    if center is None:
        label = max(big, key=lambda i: stats[i, cv2.CC_STAT_AREA])
    else:
        label = min(big, key=lambda i: math.hypot(centroids[i][0] - center[0], centroids[i][1] - center[1]))
    filled = fill_holes((labels == label).astype(np.uint8))
    interior = cv2.erode(filled, np.ones((5, 5), np.uint8)) > 0
    region = cv2.dilate(filled, np.ones((3, 3), np.uint8)) > 0
    part_alpha = np.where(interior, 1.0, np.where(region, alpha, 0))
    safe = np.maximum(part_alpha, 1e-3)[:, :, None]
    colour = np.clip((rgb - (1 - part_alpha[:, :, None]) * background) / safe, 0, 255)
    result = Image.fromarray(np.dstack([colour, part_alpha * 255]).astype(np.uint8))
    box = result.getbbox()
    return result.crop(box), (box[0], box[1])


def clean(image: Image.Image) -> Image.Image:
    """去噪：去掉生成图里的浅色斑点和脏块（保边，线稿不糊）。"""
    rgb = np.asarray(image.convert("RGB"))
    return Image.fromarray(cv2.fastNlMeansDenoisingColored(rgb, None, DenoiseStrength, DenoiseStrength, 7, 21))


def blink_patch(character: Image.Image) -> tuple[Image.Image, tuple[int, int]]:
    """闭眼补丁：闭眼图对齐到人物原图的头部，只保留眼睛附近的差异（柔边）。返回补丁和它在人物原图的左上角。"""
    left, top, right, bottom = CharacterBlinkBox
    reference = np.asarray(character.convert("RGB"))[top:bottom, left:right].astype(np.float32)
    raw = clean(Image.open(SourceDir / "character_blink.png")).resize((right - left, bottom - top),
                                                                               Image.LANCZOS)
    moving = np.asarray(raw).astype(np.float32)
    warp = np.eye(2, 3, dtype=np.float32)
    criteria = (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-6)
    gray = cv2.COLOR_RGB2GRAY
    _, warp = cv2.findTransformECC(cv2.cvtColor(reference, gray), cv2.cvtColor(moving, gray),
                                   warp, cv2.MOTION_AFFINE, criteria)
    aligned = cv2.warpAffine(moving, warp, (right - left, bottom - top), flags=cv2.INTER_LINEAR + cv2.WARP_INVERSE_MAP,
                             borderMode=cv2.BORDER_REPLICATE)
    changed = (np.abs(aligned - reference).sum(axis=2) > 60).astype(np.uint8)
    zone = np.zeros_like(changed)
    zone[EyeBox[1]:EyeBox[3], EyeBox[0]:EyeBox[2]] = 1
    mask = cv2.dilate(changed * zone, np.ones((9, 9), np.uint8)).astype(np.float32)
    mask = cv2.GaussianBlur(mask, (9, 9), 0)
    patch = Image.fromarray(np.dstack([aligned, mask * 255]).clip(0, 255).astype(np.uint8))
    box = patch.getbbox()
    return patch.crop(box), (left + box[0], top + box[1])


def moon_glow() -> Image.Image:
    """月光：以月亮为中心的柔和光晕（中间空出月面，免得把月亮冲白）。"""
    size = MoonRadius * 5
    y, x = np.mgrid[0:size, 0:size].astype(float)
    r = np.hypot(x - size / 2, y - size / 2) / MoonRadius
    halo = 0.3 * np.exp(-np.maximum(r - 1, 0) / 0.7)  # 向外慢慢变淡
    # 从月边外侧平滑淡入（不在月边留亮圈），外圈淡出到图边
    fade_in = np.clip((r - 0.98) / 0.35, 0, 1) ** 2
    fade_out = np.clip((2.45 - r) / 1.1, 0, 1)
    strength = np.clip(halo * fade_in * fade_out * fade_out * (3 - 2 * fade_out), 0, 1)
    colour = np.zeros((size, size, 3)) + np.array([200, 225, 255])
    return Image.fromarray(np.dstack([colour, strength * 255]).astype(np.uint8))


def glint() -> Image.Image:
    """剑尖闪光：四角星加一圈光。"""
    size = 160
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    c = size / 2
    for length, width in ((78, 7), (40, 4)):
        draw.polygon([(c, c - length), (c + width, c), (c, c + length), (c - width, c)], fill=(255, 255, 255, 255))
        draw.polygon([(c - length * 0.8, c), (c, c - width), (c + length * 0.8, c), (c, c + width)],
                     fill=(255, 255, 255, 255))
    halo = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ImageDraw.Draw(halo).ellipse((c - 26, c - 26, c + 26, c + 26), fill=(215, 230, 255, 150))
    halo = halo.filter(ImageFilter.GaussianBlur(10))
    halo.alpha_composite(image.filter(ImageFilter.GaussianBlur(1)))
    return halo


# ===== 网格和权重 =====


def grid_mesh(image: Image.Image, step: int) -> tuple[np.ndarray, list[int]]:
    """按像素格三角化不透明区域：返回顶点（图片像素坐标）和三角形索引。"""
    alpha = np.asarray(image)[:, :, 3] > 8
    width, height = image.size
    columns, rows = math.ceil(width / step), math.ceil(height / step)
    index: dict[tuple[int, int], int] = {}
    vertices: list[tuple[float, float]] = []
    triangles: list[int] = []

    def vertex(i: int, j: int) -> int:
        if (i, j) not in index:
            index[(i, j)] = len(vertices)
            vertices.append((min(i * step, width), min(j * step, height)))
        return index[(i, j)]

    for j in range(rows):
        for i in range(columns):
            if not alpha[j * step:(j + 1) * step, i * step:(i + 1) * step].any():
                continue
            a, b, c, d = vertex(i, j), vertex(i + 1, j), vertex(i + 1, j + 1), vertex(i, j + 1)
            triangles += [a, b, c, a, c, d]
    return np.array(vertices, float), triangles


def zone_mask(shape: tuple[int, int], polygon: list[tuple[int, int]], feather: int) -> np.ndarray:
    """多边形区域的柔边蒙版（0–1，人物原图大小）。"""
    mask = Image.new("L", (shape[1], shape[0]), 0)
    ImageDraw.Draw(mask).polygon(polygon, fill=255)
    return np.asarray(mask.filter(ImageFilter.GaussianBlur(feather))) / 255


def character_masks(character: Image.Image) -> dict[str, np.ndarray]:
    """人物各部位的柔边蒙版：披风按紫色、头发按白色，再限定在各自的区域里。"""
    rgb = np.asarray(character.convert("RGB")).astype(float) / 255
    hsv = cv2.cvtColor((rgb * 255).astype(np.uint8), cv2.COLOR_RGB2HSV).astype(float)
    hue, sat, val = hsv[:, :, 0] * 2, hsv[:, :, 1] / 255, hsv[:, :, 2] / 255
    shape = rgb.shape[:2]
    rows = np.arange(shape[0])[:, None] * np.ones(shape)
    columns = np.ones(shape) * np.arange(shape[1])[None, :]
    # 披风：紫色（含很暗的紫黑）
    dark = (val < 0.22) & (columns < 560) & (rows > 520) & (rows < 1000)  # 披风内侧很暗的紫黑（不含腿）
    purple = ((hue > 255) & (hue < 340) & (sat > 0.18)) | dark
    # 去掉脖子上的围巾、肩后的一小块，以及盾牌（深色木纹会被认成紫色）
    purple &= ~((rows < 480) | ((columns > 640) & (rows < 600)) | ((columns > 780) & (rows < 820)))
    cape = cv2.morphologyEx(purple.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((15, 15), np.uint8)).astype(float)
    cape = cv2.GaussianBlur(cape, (0, 0), 30)  # 交界过渡要宽，否则一格之内就被拉扯
    # 头发：低饱和、高亮度，限定在两缕头发的区域里
    white = ((sat < 0.18) & (val > 0.62)).astype(float)
    white = cv2.GaussianBlur(cv2.dilate(white, np.ones((9, 9), np.uint8)), (0, 0), 8)
    return {
        "cape": np.clip(cape * 1.6, 0, 1),
        "hair_l": np.clip(white * zone_mask(shape, HairLeftZone, 6), 0, 1),
        "hair_r": np.clip(white * zone_mask(shape, HairRightZone, 6), 0, 1),
        "hood_tip": zone_mask(shape, HoodTipZone, 18),
        "arm": zone_mask(shape, ArmZone, 10),
        "sword": zone_mask(shape, SwordZone, 5),
        "head": zone_mask(shape, HeadZone, 20),
        "nearest": nearest_opaque(rgb),
    }


def nearest_opaque(rgb: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """每个像素最近的不透明像素坐标（行、列）。网格外圈的顶点落在透明处，按最近的画面像素取权重，
    否则外圈顶点被分给臀部、内圈顶点跟着披风或头发动，边缘那一圈会被拉扯变形。"""
    corners = np.concatenate([rgb[:8].reshape(-1, 3), rgb[-8:].reshape(-1, 3)])
    distance = np.abs(rgb * 255 - np.median(corners, axis=0) * 255).sum(axis=2)
    _, (rows, columns) = ndimage.distance_transform_edt(distance <= AlphaRange[1], return_indices=True)
    return rows, columns


def character_weights(points: np.ndarray, masks: dict[str, np.ndarray]) -> list[list[tuple[str, float]]]:
    """人物网格顶点的权重：先按蒙版分给次级部位，剩下的按高度分给臀、躯干、头，最多四根骨骼。"""
    joints = {name: np.array(joint, float) for name, _, joint in Bones}
    result = []
    height, width = masks["cape"].shape
    for x, y in points:
        px, py = int(min(x, width - 1)), int(min(y, height - 1))
        py, px = int(masks["nearest"][0][py, px]), int(masks["nearest"][1][py, px])
        x, y = px, py  # 高度分配（裙摆、躯干）也按最近的画面像素算
        entries: dict[str, float] = {}
        remaining = 1.0

        def take(bone: str, amount: float) -> None:
            nonlocal remaining
            amount = min(max(amount, 0.0), remaining)
            if amount > 0:
                entries[bone] = entries.get(bone, 0.0) + amount
                remaining -= amount

        take("sword", masks["sword"][py, px])
        take("arm", masks["arm"][py, px])
        take("hood_tip", masks["hood_tip"][py, px])
        take("hair_l", masks["hair_l"][py, px])
        take("hair_r", masks["hair_r"][py, px])
        # 披风：沿链按到各关节的距离分配
        cape = masks["cape"][py, px] * remaining
        if cape > 0:
            near = {b: 1 / (np.hypot(*(np.array((x, y)) - joints[b])) + 40) ** 2 for b in ("cape1", "cape2", "cape3")}
            total = sum(near.values())
            for bone, value in near.items():
                take(bone, cape * value / total)
        # 裙摆：腰以下逐渐交给左右裙摆
        hem = float(np.clip((y - joints["skirt_l"][1]) / 300, 0, 1)) * remaining
        if hem > 0:
            left, right = joints["skirt_l"][0], joints["skirt_r"][0]
            side = float(np.clip((x - left) / (right - left), 0, 1))
            take("skirt_l", hem * (1 - side))
            take("skirt_r", hem * side)
        # 头、躯干、臀
        head = masks["head"][py, px] * remaining
        take("head", head)
        upper = float(np.clip((660 - y) / 200, 0, 1)) * remaining
        take("torso", upper)
        take("hip", remaining)
        top = sorted(entries.items(), key=lambda item: -item[1])[:4]
        total = sum(w for _, w in top)
        result.append([(bone, w / total) for bone, w in top if w / total > 0.01])
    return result


def mesh(name: str, image: Image.Image, origin: tuple[float, float], scale: float, weights, world: dict,
         index: dict, step: int) -> dict:
    """网格附件：image 的像素 (u, v) 放到屏幕 origin + (u, v) × scale；weights(顶点像素坐标) 给出每个顶点的骨骼权重。"""
    local, triangles = grid_mesh(image, step)
    screen = np.asarray(origin, float) + local * scale
    spine = to_spine(screen)
    vertices = []
    for (sx, sy), entries in zip(spine, weights(local)):
        total = sum(w for _, w in entries)
        vertices.append([(index[bone], sx - world[bone][0], sy - world[bone][1], w / total) for bone, w in entries])
    uvs = []
    for x, y in local:
        uvs += [x / image.width, y / image.height]
    count = len(local)
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
    """图集描述文件。"""
    lines = ["knight_select.png", f"size: {AtlasSize[0]},{AtlasSize[1]}", "format: RGBA8888",
             "filter: Linear,Linear", "repeat: none"]
    for name, (x, y, w, h) in regions.items():
        lines += [name, "  rotate: false", f"  xy: {x}, {y}", f"  size: {w}, {h}", f"  orig: {w}, {h}",
                  "  offset: 0, 0", "  index: -1"]
    return "\n".join(lines) + "\n"


# ===== 骨骼和动作 =====


def bone_list() -> list[tuple[str, str | None, np.ndarray]]:
    """全部骨骼：名字、父骨骼、骨骼坐标里的世界位置。"""
    hip = to_spine(to_screen(Bones[0][2]))
    result = [("root", None, np.zeros(2)), ("character", "root", hip), ("moon", "root", to_spine(MoonCenter))]
    result += [(name, "root", to_spine(position)) for name, _, position, _, _ in Clouds]
    result += [(name, parent, to_spine(to_screen(joint))) for name, parent, joint in Bones]
    return result


def bone_data(bones: list) -> list[dict]:
    """骨骼数据：位置为相对父骨骼的偏移，旋转为 0。"""
    world = {name: position for name, _, position in bones}
    result = []
    for name, parent, position in bones:
        offset = position - (world[parent] if parent else 0)
        bone = {"name": name, "rotation": 0.0, "x": float(offset[0]), "y": float(offset[1]), "scaleX": 1.0,
                "scaleY": 1.0, "shearX": 0.0, "shearY": 0.0, "length": 0.0, "inherit": 0, "skin": False}
        if parent:
            bone["parent"] = parent
        result.append(bone)
    return result


def _ease(p: float) -> float:
    """缓入缓出。"""
    return p * p * (3 - 2 * p)


def animation() -> dict:
    """唯一的循环动作。"""
    count = int(round(Loop * SampleRate))
    times = [Loop * i / count for i in range(count + 1)]
    bones: dict = {}
    for bone, kinds in Motion.items():
        for kind, (amplitude, period, phase) in kinds.items():
            frames = []
            for t in times:
                wave = math.sin(math.tau * t / period - phase)
                if kind == "rotate":
                    frames.append({"time": t, "value": amplitude * wave, "curve": None})
                elif kind == "scale":
                    frames.append({"time": t, "x": 1 + amplitude[0] * wave, "y": 1 + amplitude[1] * wave,
                                   "curve": None})
                else:
                    frames.append({"time": t, "x": amplitude[0] * wave, "y": amplitude[1] * wave, "curve": None})
            bones.setdefault(bone, {})[kind] = frames
    # 闭眼：切换附件
    eyes = [{"time": 0.0, "name": None}]
    for start in Blinks:
        eyes += [{"time": start, "name": "eyes_closed"}, {"time": start + BlinkLength, "name": None}]
    # 月光：6 秒一次脉动
    moon = []
    for t in times[::6]:
        level = 0.62 + 0.3 * (0.5 - 0.5 * math.cos(math.tau * t / 6.0))
        moon.append({"time": t, "color": f"ffffff{round(level * 255):02x}", "curve": None})
    # 剑尖闪光：快速亮起、慢慢消失，同时转一点、放大
    glint_alpha = [{"time": 0.0, "color": "ffffff00", "curve": None}]
    glint_scale, glint_rotate = [{"time": 0.0, "x": 0.3, "y": 0.3, "curve": None}], [{"time": 0.0, "value": 0.0,
                                                                                      "curve": None}]
    for start in Glints:
        glint_alpha += [{"time": start, "color": "ffffff00", "curve": None},
                        {"time": start + 0.12, "color": "ffffffff", "curve": None},
                        {"time": start + 0.6, "color": "ffffff00", "curve": None}]
        glint_scale += [{"time": start, "x": 0.3, "y": 0.3, "curve": None},
                        {"time": start + 0.15, "x": 1.0, "y": 1.0, "curve": None},
                        {"time": start + 0.6, "x": 0.5, "y": 0.5, "curve": None}]
        glint_rotate += [{"time": start, "value": 0.0, "curve": None},
                         {"time": start + 0.6, "value": -45.0, "curve": None}]
    glint_alpha.append({"time": Loop, "color": "ffffff00", "curve": None})
    bones["glint"] = {"scale": glint_scale, "rotate": glint_rotate}
    slots = {"eyes_closed": {"attachment": eyes}, "moon_glow": {"rgba": moon}, "glint": {"rgba": glint_alpha}}
    return {"slots": slots, "bones": bones, "ik": {}, "deform": {}, "drawOrder": [], "events": []}


def build() -> tuple[dict, Image.Image, dict]:
    """生成骨骼数据、图集图片和图集区域。"""
    bones = bone_list()
    world = {name: position for name, _, position in bones}
    index = {name: i for i, (name, _, _) in enumerate(bones)}
    # 人物
    source = clean(Image.open(SourceDir / "character.png"))
    character, corner = cut(source)
    masks = character_masks(source)
    shift = np.array(corner, float)
    eyes, eyes_corner = blink_patch(source)
    images = {"character": character, "eyes_closed": eyes}
    attachments = {
        "character": mesh("character", character, to_screen(corner), CharacterScale,
                          lambda local: character_weights(local + shift, masks), world, index, GridStep),
        "eyes_closed": mesh("eyes_closed", eyes, to_screen(eyes_corner), CharacterScale,
                            lambda local: [[("head", 1.0)]] * len(local), world, index, 12),
    }
    # 云（图集里按屏幕大小缩小存放）
    cloud_sheet = Image.open(SourceDir / "clouds.png")
    for name, center, position, scale, _ in Clouds:
        cloud, _ = cut(cloud_sheet, center)
        cloud = cloud.resize((round(cloud.width * scale), round(cloud.height * scale)), Image.LANCZOS)
        images[name] = cloud
        origin = np.asarray(position, float) - np.array(cloud.size) / 2
        attachments[name] = mesh(name, cloud, origin, 1.0, lambda local, n=name: [[(n, 1.0)]] * len(local), world,
                                 index, 40)
    # 月光、剑尖闪光
    images["moon_glow"] = moon_glow()
    origin = np.asarray(MoonCenter, float) - np.array(images["moon_glow"].size) / 2
    attachments["moon_glow"] = mesh("moon_glow", images["moon_glow"], origin, 1.0,
                                    lambda local: [[("moon", 1.0)]] * len(local), world, index, 40)
    images["glint"] = glint()
    tip = to_screen(Bones[[b[0] for b in Bones].index("glint")][2])
    attachments["glint"] = mesh("glint", images["glint"], tip - np.array(images["glint"].size) / 2, 1.0,
                                lambda local: [[("glint", 1.0)]] * len(local), world, index, 40)
    page, regions = pack(images)
    # 插槽（从后到前）：云、月光、人物、闭眼、剑尖闪光
    order = [("cloud_c", "cloud_c", 0), ("cloud_b", "cloud_b", 0), ("cloud_a", "cloud_a", 0), ("moon_glow", "moon", 1),
             ("character", "hip", 0), ("eyes_closed", "head", 0), ("glint", "glint", 1)]
    colours = {name: colour for name, _, _, _, colour in Clouds}
    slots = [{"name": name, "bone": bone, "color": colours.get(name, "ffffffff"), "dark": None,
              "attachment": None if name == "eyes_closed" else name, "blend": blend} for name, bone, blend in order]
    skeleton = {
        "skeleton": {"hash": 0, "spine": RuntimeVersion, "x": -Screen[0] / 2, "y": 0.0, "width": float(Screen[0]),
                     "height": float(Screen[1]), "referenceScale": 100.0},
        "nonessential": False,
        "bones": bone_data(bones),
        "slots": slots,
        "ik": [],
        "skins": {"default": {name: {name: attachments[name]} for name, _, _ in order}},
        "events": {},
        "animations": {"animation": animation()},
    }
    return skeleton, page, regions


def main() -> None:
    """命令行入口。"""
    skeleton, page, regions = build()
    data = sb.write_42(skeleton)
    skeleton["skeleton"]["hash"] = int.from_bytes(hashlib.sha1(data).digest()[:8], "big", signed=True)
    data = sb.write_42(skeleton)
    if sb.write_42(sb.read_42(data)) != data:
        raise SystemExit("4.2 round trip mismatch")
    OutputDir.mkdir(parents=True, exist_ok=True)
    (OutputDir / "knight_select.skel").write_bytes(data)
    (OutputDir / "knight_select.atlas").write_text(atlas_text(regions), encoding="utf-8")
    page.save(OutputDir / "knight_select.png")
    print(f"knight_select.skel {len(data)} bytes")
    poster(skeleton, page, regions)
    if "--preview" in sys.argv:
        preview(skeleton, page, regions)


def frame(skeleton: dict, page: Image.Image, regions: dict, time: float) -> Image.Image:
    """背景加某一时刻的骨骼（屏幕大小）。半透明的插槽单独按不透明渲染再乘透明度，免得逐个三角形叠加出网格缝。"""
    image = background().convert("RGBA")
    state = sp.pose(skeleton, "animation", time)
    groups: list[list[dict]] = []
    for slot in skeleton["slots"]:
        if state["slots"][slot["name"]]["color"].endswith("ff") and groups and groups[-1][0].get("opaque"):
            groups[-1].append(slot)
        else:
            groups.append([dict(slot, opaque=state["slots"][slot["name"]]["color"].endswith("ff"))])
    for group in groups:
        alpha = int(state["slots"][group[0]["name"]]["color"][6:8], 16) / 255
        solid = [dict(slot, color="ffffffff") for slot in group]
        single = dict(skeleton, slots=solid)
        layer_state = sp.pose(single, "animation", time)
        for slot in solid:
            layer_state["slots"][slot["name"]]["color"] = "ffffffff"
        layer = sp.render(single, regions, page, layer_state, size=Screen, origin=(Screen[0] / 2, Screen[1]),
                          background=(0, 0, 0, 0))
        if alpha < 1:
            layer.putalpha(layer.getchannel("A").point(lambda a: round(a * alpha)))
        image.alpha_composite(layer)
    return image


def poster(skeleton: dict, page: Image.Image, regions: dict) -> None:
    """选人海报：第 0 秒，不含月光和闪光（离线渲染不支持叠加混合）；存成 JPEG，保证小于 1 MB。
    （以前压成抖动的调色板 PNG，画面满是噪点。）"""
    hidden = {"moon_glow", "glint"}
    still = dict(skeleton, slots=[slot for slot in skeleton["slots"] if slot["name"] not in hidden])
    image = frame(still, page, regions, 0.0).convert("RGB")
    for quality in (95, 92, 90, 85, 80):
        image.save(PosterPath, quality=quality, subsampling=0 if quality >= 90 else 2, optimize=True)
        if PosterPath.stat().st_size < PosterLimit:
            break
    print(f"poster {PosterPath.stat().st_size} bytes")


def preview(skeleton: dict, page: Image.Image, regions: dict) -> None:
    """在背景上渲染若干时刻（半尺寸）。"""
    PreviewDir.mkdir(parents=True, exist_ok=True)
    for t in (0.0, 2.25, 4.1, 6.0, 9.0):
        frame(skeleton, page, regions, t).convert("RGB").resize((960, 540), Image.LANCZOS).save(
            PreviewDir / f"t{t:05.2f}.png")
    print(f"preview written to {PreviewDir}")


if __name__ == "__main__":
    main()
