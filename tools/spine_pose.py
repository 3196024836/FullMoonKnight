"""Spine 4.2 骨骼的姿势计算和预览渲染（不依赖游戏，用于制作和检查动作）。

数据结构与 spine_binary.read_42 的结果相同。只实现女骑士用到的部分：
普通继承的骨骼、区域和网格附件、骨骼和网格变形时间轴、插槽附件和颜色时间轴。
"""

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


# ===== 时间轴求值 =====


def _bezier_value(time: float, t1: float, v1: float, curve: list, t2: float, v2: float) -> float:
    """在贝塞尔曲线上求 time 对应的数值。curve 为 [cx1, cy1, cx2, cy2]（绝对时间和数值）。"""
    cx1, cy1, cx2, cy2 = curve
    lo, hi = 0.0, 1.0
    for _ in range(30):
        s = (lo + hi) / 2
        x = (1 - s) ** 3 * t1 + 3 * (1 - s) ** 2 * s * cx1 + 3 * (1 - s) * s ** 2 * cx2 + s ** 3 * t2
        if x < time:
            lo = s
        else:
            hi = s
    s = (lo + hi) / 2
    return (1 - s) ** 3 * v1 + 3 * (1 - s) ** 2 * s * cy1 + 3 * (1 - s) * s ** 2 * cy2 + s ** 3 * v2


def sample(frames: list, time: float, keys: list[str]) -> list[float]:
    """求时间轴在 time 时的各数值（时间超出范围时取首尾帧）。"""
    if time <= frames[0]["time"]:
        return [frames[0][k] for k in keys]
    for i in range(len(frames) - 1):
        a, b = frames[i], frames[i + 1]
        if time < b["time"]:
            curve = a.get("curve")
            if curve == "stepped":
                return [a[k] for k in keys]
            result = []
            for j, k in enumerate(keys):
                if isinstance(curve, list):
                    result.append(_bezier_value(time, a["time"], a[k], curve[j * 4:j * 4 + 4], b["time"], b[k]))
                else:
                    p = (time - a["time"]) / (b["time"] - a["time"])
                    result.append(a[k] + (b[k] - a[k]) * p)
            return result
    return [frames[-1][k] for k in keys]


def _sample_deform(frames: list, time: float, length: int) -> np.ndarray:
    """求网格变形偏移（曲线数值为 0–1 的混合比例）。"""
    def full(frame):
        array = np.zeros(length)
        if frame.get("vertices"):
            start = frame["offset"]
            array[start:start + len(frame["vertices"])] = frame["vertices"]
        return array

    if time <= frames[0]["time"]:
        return full(frames[0])
    for i in range(len(frames) - 1):
        a, b = frames[i], frames[i + 1]
        if time < b["time"]:
            curve = a.get("curve")
            if curve == "stepped":
                return full(a)
            if isinstance(curve, list):
                p = _bezier_value(time, a["time"], 0.0, curve, b["time"], 1.0)
            else:
                p = (time - a["time"]) / (b["time"] - a["time"])
            return full(a) + (full(b) - full(a)) * p
    return full(frames[-1])


# ===== 姿势 =====


def pose(skeleton: dict, animation: str | None = None, time: float = 0.0) -> dict:
    """计算姿势：每根骨骼的世界变换、每个插槽当前的附件、颜色和网格变形。"""
    anim = skeleton["animations"].get(animation, {}) if animation else {}
    bone_timelines = anim.get("bones", {})
    world: dict[str, tuple] = {}
    for bone in skeleton["bones"]:
        rotation, x, y = bone["rotation"], bone["x"], bone["y"]
        scale_x, scale_y, shear_x, shear_y = bone["scaleX"], bone["scaleY"], bone["shearX"], bone["shearY"]
        timelines = bone_timelines.get(bone["name"], {})
        if "rotate" in timelines:
            rotation += sample(timelines["rotate"], time, ["value"])[0]
        if "translate" in timelines:
            dx, dy = sample(timelines["translate"], time, ["x", "y"])
            x, y = x + dx, y + dy
        if "scale" in timelines:
            sx, sy = sample(timelines["scale"], time, ["x", "y"])
            scale_x, scale_y = scale_x * sx, scale_y * sy
        if "shear" in timelines:
            hx, hy = sample(timelines["shear"], time, ["x", "y"])
            shear_x, shear_y = shear_x + hx, shear_y + hy
        # 本地变换矩阵
        la = math.cos(math.radians(rotation + shear_x)) * scale_x
        lb = math.cos(math.radians(rotation + 90 + shear_y)) * scale_y
        lc = math.sin(math.radians(rotation + shear_x)) * scale_x
        ld = math.sin(math.radians(rotation + 90 + shear_y)) * scale_y
        if "parent" not in bone:
            world[bone["name"]] = (la, lb, lc, ld, x, y)
            continue
        pa, pb, pc, pd, px, py = world[bone["parent"]]
        world[bone["name"]] = (pa * la + pb * lc, pa * lb + pb * ld, pc * la + pd * lc, pc * lb + pd * ld,
                               pa * x + pb * y + px, pc * x + pd * y + py)

    # 插槽状态
    slots = {}
    slot_timelines = anim.get("slots", {})
    for slot in skeleton["slots"]:
        attachment = slot.get("attachment")
        color = slot.get("color", "ffffffff")
        timelines = slot_timelines.get(slot["name"], {})
        if "attachment" in timelines:
            frames = timelines["attachment"]
            if time >= frames[0]["time"]:
                attachment = [f for f in frames if f["time"] <= time][-1]["name"]
        if "rgba" in timelines:
            frames = timelines["rgba"]
            channels = [[int(f["color"][i * 2:i * 2 + 2], 16) for i in range(4)] for f in frames]
            values = [{"time": f["time"], "curve": f.get("curve"), **{f"c{i}": c[i] for i in range(4)}}
                      for f, c in zip(frames, channels)]
            # 颜色曲线每个通道一条，这里各通道用同一条曲线近似
            for v in values:
                if isinstance(v["curve"], list):
                    v["curve"] = v["curve"][:4] * 4
            color = "".join(f"{int(round(c)):02x}" for c in sample(values, time, ["c0", "c1", "c2", "c3"]))
        slots[slot["name"]] = {"attachment": attachment, "color": color}
    deforms = {}
    for slot, attachments in anim.get("deform", {}).get("default", {}).items():
        for name, frames in attachments.items():
            deforms[(slot, name)] = frames
    return {"world": world, "slots": slots, "deforms": deforms, "time": time}


def attachment_vertices(skeleton: dict, state: dict, slot: dict, attachment: dict) -> np.ndarray:
    """附件在世界坐标中的顶点（n×2，y 向上）。"""
    world = state["world"]
    bones = skeleton["bones"]
    if attachment["type"] == "region":
        w, h = attachment["width"] * attachment["scaleX"], attachment["height"] * attachment["scaleY"]
        cos, sin = math.cos(math.radians(attachment["rotation"])), math.sin(math.radians(attachment["rotation"]))
        corners = [(-w / 2, -h / 2), (-w / 2, h / 2), (w / 2, h / 2), (w / 2, -h / 2)]  # 左下、左上、右上、右下
        local = [(attachment["x"] + cx * cos - cy * sin, attachment["y"] + cx * sin + cy * cos) for cx, cy in corners]
        a, b, c, d, x, y = world[slot["bone"]]
        return np.array([(a * lx + b * ly + x, c * lx + d * ly + y) for lx, ly in local])

    count = attachment["vertexCount"]
    deform_frames = state["deforms"].get((slot["name"], attachment["name"]))
    if not attachment["bonesWeighted"]:
        vertices = np.array(attachment["vertices"], dtype=float)
        if deform_frames:
            vertices = vertices + _sample_deform(deform_frames, state["time"], len(vertices))
        a, b, c, d, x, y = world[slot["bone"]]
        pts = vertices.reshape(-1, 2)
        return np.stack([a * pts[:, 0] + b * pts[:, 1] + x, c * pts[:, 0] + d * pts[:, 1] + y], axis=1)

    entries = sum(len(v) for v in attachment["vertices"])
    deform = _sample_deform(deform_frames, state["time"], entries * 2) if deform_frames else np.zeros(entries * 2)
    result = np.zeros((count, 2))
    f = 0
    for i, vertex in enumerate(attachment["vertices"]):
        for bone_index, vx, vy, weight in vertex:
            a, b, c, d, x, y = world[bones[bone_index]["name"]]
            vx, vy = vx + deform[f], vy + deform[f + 1]
            f += 2
            result[i] += np.array([a * vx + b * vy + x, c * vx + d * vy + y]) * weight
    return result


# ===== 渲染 =====


def read_atlas(path: Path) -> dict:
    """读取图集：{区域名: (x, y, 宽, 高)}（只支持未旋转的区域）。"""
    regions = {}
    lines = path.read_text(encoding="utf-8").splitlines()
    name = None
    for line in lines:
        if not line.strip():
            continue
        if ":" not in line:
            name = line.strip()
            continue
        key, value = (s.strip() for s in line.split(":", 1))
        if name and key == "xy":
            x, y = (int(v) for v in value.split(","))
            regions[name] = [x, y, 0, 0]
        elif name in regions and key == "size":
            w, h = (int(v) for v in value.split(","))
            regions[name][2:] = [w, h]
        elif name and key == "rotate" and value != "false":
            raise ValueError("rotated regions not supported")
    return {k: tuple(v) for k, v in regions.items()}


def render(skeleton: dict, atlas: dict, texture: Image.Image, state: dict, size=(900, 900), origin=(450, 820),
           scale: float = 1.0, background=(60, 60, 70, 255), bones: bool = False) -> Image.Image:
    """把姿势画成图片。origin 为骨骼原点在图片中的位置。"""
    canvas = Image.new("RGBA", size, background)
    tex = np.asarray(texture.convert("RGBA"))

    def to_px(points: np.ndarray) -> np.ndarray:
        return np.stack([origin[0] + points[:, 0] * scale, origin[1] - points[:, 1] * scale], axis=1)

    skin = skeleton["skins"]["default"]
    for slot in skeleton["slots"]:
        current = state["slots"][slot["name"]]
        name = current["attachment"]
        if name is None or name not in skin.get(slot["name"], {}):
            continue
        attachment = skin[slot["name"]][name]
        rx, ry, rw, rh = atlas[attachment.get("path") or attachment["name"]]
        dest = to_px(attachment_vertices(skeleton, state, slot, attachment))
        if attachment["type"] == "region":
            uv = np.array([(0, 1), (0, 0), (1, 0), (1, 1)], dtype=float)
            triangles = [0, 1, 2, 2, 3, 0]
        else:
            uv = np.array(attachment["uvs"], dtype=float).reshape(-1, 2)
            triangles = attachment["triangles"]
        src = np.stack([rx + uv[:, 0] * rw, ry + uv[:, 1] * rh], axis=1)
        alpha = int(current["color"][6:8], 16) / 255
        for t in range(0, len(triangles), 3):
            _draw_triangle(canvas, tex, src[triangles[t:t + 3]], dest[triangles[t:t + 3]], alpha)

    if bones:
        draw = ImageDraw.Draw(canvas)
        for bone in skeleton["bones"]:
            a, b, c, d, x, y = state["world"][bone["name"]]
            start = to_px(np.array([[x, y]]))[0]
            end = to_px(np.array([[x + a * bone["length"], y + c * bone["length"]]]))[0]
            draw.line([tuple(start), tuple(end)], fill=(255, 220, 0, 255), width=2)
            draw.text(tuple(start), bone["name"].replace("bone", ""), fill=(0, 255, 255, 255))
    return canvas


def _draw_triangle(canvas: Image.Image, tex: np.ndarray, src: np.ndarray, dest: np.ndarray, alpha: float) -> None:
    """把纹理三角形仿射映射到画布三角形。"""
    x0, y0 = np.floor(dest.min(axis=0)).astype(int)
    x1, y1 = np.ceil(dest.max(axis=0)).astype(int)
    x0, y0 = max(x0, 0), max(y0, 0)
    x1, y1 = min(x1, canvas.width - 1), min(y1, canvas.height - 1)
    if x1 <= x0 or y1 <= y0:
        return
    # 目标 -> 源 的仿射矩阵
    d = np.hstack([dest, np.ones((3, 1))])
    try:
        m = np.linalg.solve(d, src)
    except np.linalg.LinAlgError:
        return
    ys, xs = np.mgrid[y0:y1 + 1, x0:x1 + 1]
    px = np.stack([xs.ravel() + 0.5, ys.ravel() + 0.5, np.ones(xs.size)], axis=1)
    # 重心坐标判断像素在三角形内（略放宽避免接缝）
    inv = np.linalg.solve(d.T, px.T).T
    inside = (inv >= -0.02).all(axis=1)
    if not inside.any():
        return
    uv = px[inside] @ m
    u = np.clip(uv[:, 0].astype(int), 0, tex.shape[1] - 1)
    v = np.clip(uv[:, 1].astype(int), 0, tex.shape[0] - 1)
    colors = tex[v, u].astype(float)
    colors[:, 3] *= alpha
    region = np.asarray(canvas.crop((x0, y0, x1 + 1, y1 + 1))).astype(float).reshape(-1, 4)
    target = region[inside]
    a = colors[:, 3:4] / 255
    target[:, :3] = colors[:, :3] * a + target[:, :3] * (1 - a)
    target[:, 3] = np.maximum(target[:, 3], colors[:, 3])
    region[inside] = target
    canvas.paste(Image.fromarray(region.reshape(y1 - y0 + 1, x1 - x0 + 1, 4).astype(np.uint8)), (x0, y0))
