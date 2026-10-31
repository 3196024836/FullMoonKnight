"""Spine 二进制骨骼读写：读取 3.5 和 4.2，写出 4.2。

数据用普通 dict / list 表示，字段名与 Spine JSON 格式一致，方便查看和编辑。
只支持女骑士骨骼用到的部分：骨骼、插槽、区域和网格附件、IK 约束（读取）、
骨骼、插槽颜色、插槽附件、网格变形、绘制顺序、事件时间轴。
参考：spine-runtimes 3.5 和 4.2 分支的 SkeletonBinary.java。
"""

import struct

AttachmentTypes = ["region", "boundingbox", "mesh", "linkedmesh", "path", "point", "clipping"]  # 附件类型序号
BoneTimelines35 = ["rotate", "translate", "scale", "shear"]  # 3.5 骨骼时间轴类型
CurveLinear, CurveStepped, CurveBezier = 0, 1, 2  # 曲线类型


class Reader:
    """Spine DataInput：大端浮点、变长整数、UTF-8 字符串。"""

    def __init__(self, data: bytes):
        self.data = data  # 原始字节
        self.pos = 0  # 当前位置
        self.strings: list[str] = []  # 4.2 字符串表

    def byte(self) -> int:
        """读一个有符号字节。"""
        value = struct.unpack_from(">b", self.data, self.pos)[0]
        self.pos += 1
        return value

    def ubyte(self) -> int:
        """读一个无符号字节。"""
        value = self.data[self.pos]
        self.pos += 1
        return value

    def boolean(self) -> bool:
        """读布尔。"""
        return self.ubyte() != 0

    def short(self) -> int:
        """读大端 16 位整数。"""
        value = struct.unpack_from(">h", self.data, self.pos)[0]
        self.pos += 2
        return value

    def int32(self) -> int:
        """读大端 32 位整数。"""
        value = struct.unpack_from(">i", self.data, self.pos)[0]
        self.pos += 4
        return value

    def long(self) -> int:
        """读大端 64 位整数。"""
        value = struct.unpack_from(">q", self.data, self.pos)[0]
        self.pos += 8
        return value

    def float(self) -> float:
        """读大端 32 位浮点。"""
        value = struct.unpack_from(">f", self.data, self.pos)[0]
        self.pos += 4
        return value

    def varint(self, optimize_positive: bool = True) -> int:
        """读变长整数（最多 5 字节）。"""
        result = 0
        shift = 0
        while True:
            b = self.ubyte()
            result |= (b & 0x7F) << shift
            if b & 0x80 == 0 or shift == 28:
                break
            shift += 7
        result &= 0xFFFFFFFF
        if not optimize_positive:
            result = (result >> 1) ^ -(result & 1)
        elif result >= 0x80000000:
            result -= 0x100000000
        return result

    def string(self) -> str | None:
        """读字符串：长度加 1 的变长整数，0 为空引用。"""
        count = self.varint()
        if count == 0:
            return None
        if count == 1:
            return ""
        raw = self.data[self.pos:self.pos + count - 1]
        self.pos += count - 1
        return raw.decode("utf-8")

    def string_ref(self) -> str | None:
        """读 4.2 字符串表引用。"""
        index = self.varint()
        return None if index == 0 else self.strings[index - 1]

    def color(self) -> str:
        """读 RGBA8888 颜色，返回十六进制字符串。"""
        return f"{self.int32() & 0xFFFFFFFF:08x}"


class Writer:
    """Spine DataOutput，与 Reader 对应。"""

    def __init__(self):
        self.out = bytearray()  # 输出字节
        self.strings: list[str] = []  # 4.2 字符串表
        self.string_index: dict[str, int] = {}  # 字符串 -> 表中序号（从 1 开始）

    def byte(self, value: int) -> None:
        """写一个字节。"""
        self.out += struct.pack(">B", value & 0xFF)

    def boolean(self, value: bool) -> None:
        """写布尔。"""
        self.byte(1 if value else 0)

    def int32(self, value: int) -> None:
        """写大端 32 位整数。"""
        self.out += struct.pack(">I", value & 0xFFFFFFFF)

    def long(self, value: int) -> None:
        """写大端 64 位整数。"""
        self.out += struct.pack(">q", value)

    def float(self, value: float) -> None:
        """写大端 32 位浮点。"""
        self.out += struct.pack(">f", value)

    def varint(self, value: int, optimize_positive: bool = True) -> None:
        """写变长整数。"""
        if not optimize_positive:
            value = (value << 1) ^ (value >> 31)
        value &= 0xFFFFFFFF
        while True:
            b = value & 0x7F
            value >>= 7
            if value:
                self.byte(b | 0x80)
            else:
                self.byte(b)
                break

    def string(self, value: str | None) -> None:
        """写字符串。"""
        if value is None:
            self.varint(0)
            return
        raw = value.encode("utf-8")
        self.varint(len(raw) + 1)
        self.out += raw

    def string_ref(self, value: str | None) -> None:
        """写 4.2 字符串表引用（表须先用 collect_strings 建好）。"""
        self.varint(0 if value is None else self.string_index[value])

    def color(self, value: str) -> None:
        """写 RGBA8888 颜色。"""
        self.int32(int(value, 16))


# ===== 3.5 读取 =====


def read_35(data: bytes) -> dict:
    """读取 Spine 3.5 二进制骨骼。"""
    r = Reader(data)
    skeleton = {"hash": r.string(), "spine": r.string(), "width": r.float(), "height": r.float()}
    nonessential = r.boolean()
    if nonessential:
        skeleton["fps"] = r.float()
        skeleton["images"] = r.string()
    result = {"skeleton": skeleton, "bones": [], "slots": [], "ik": [], "skins": {}, "events": {}, "animations": {}}

    # 骨骼
    for i in range(r.varint()):
        bone = {"name": r.string()}
        if i > 0:
            bone["parent"] = result["bones"][r.varint()]["name"]
        bone.update(rotation=r.float(), x=r.float(), y=r.float(), scaleX=r.float(), scaleY=r.float(),
                    shearX=r.float(), shearY=r.float(), length=r.float(), inherit=r.varint())
        if nonessential:
            bone["color"] = r.color()
        result["bones"].append(bone)

    # 插槽
    for _ in range(r.varint()):
        slot = {"name": r.string(), "bone": result["bones"][r.varint()]["name"], "color": r.color(),
                "attachment": r.string(), "blend": r.varint()}
        result["slots"].append(slot)

    # IK 约束
    for _ in range(r.varint()):
        ik = {"name": r.string(), "order": r.varint()}
        ik["bones"] = [result["bones"][r.varint()]["name"] for _ in range(r.varint())]
        ik["target"] = result["bones"][r.varint()]["name"]
        ik["mix"] = r.float()
        ik["bendPositive"] = r.byte() > 0
        result["ik"].append(ik)

    # 变换约束、路径约束（女骑士没有）
    if r.varint() != 0 or r.varint() != 0:
        raise ValueError("transform/path constraints not supported")

    # 皮肤
    default = _read_skin_35(r, result, nonessential)
    if default:
        result["skins"]["default"] = default
    for _ in range(r.varint()):
        name = r.string()
        result["skins"][name] = _read_skin_35(r, result, nonessential)

    # 事件
    for _ in range(r.varint()):
        name = r.string()
        result["events"][name] = {"int": r.varint(False), "float": r.float(), "string": r.string()}

    # 动画
    for _ in range(r.varint()):
        name = r.string()
        result["animations"][name] = _read_animation_35(r, result)

    if r.pos != len(data):
        raise ValueError(f"3.5 reader stopped at {r.pos} of {len(data)}")
    return result


def _read_skin_35(r: Reader, result: dict, nonessential: bool) -> dict:
    """读 3.5 皮肤：{插槽名: {附件名: 附件}}。"""
    skin: dict = {}
    for _ in range(r.varint()):
        slot = result["slots"][r.varint()]["name"]
        attachments = skin.setdefault(slot, {})
        for _ in range(r.varint()):
            key = r.string()
            attachments[key] = _read_attachment_35(r, key, nonessential)
    return skin


def _read_attachment_35(r: Reader, key: str, nonessential: bool) -> dict:
    """读 3.5 附件。"""
    name = r.string() or key
    kind = AttachmentTypes[r.byte()]
    attachment = {"name": name, "type": kind}
    if kind == "region":
        attachment.update(path=r.string(), rotation=r.float(), x=r.float(), y=r.float(), scaleX=r.float(),
                          scaleY=r.float(), width=r.float(), height=r.float(), color=r.color())
    elif kind == "mesh":
        attachment["path"] = r.string()
        attachment["color"] = r.color()
        count = r.varint()
        attachment["uvs"] = [r.float() for _ in range(count * 2)]
        attachment["triangles"] = [r.short() for _ in range(r.varint())]
        attachment["vertexCount"] = count
        attachment["vertices"], attachment["bonesWeighted"] = _read_vertices(r, count)
        attachment["hull"] = r.varint()
        if nonessential:
            attachment["edges"] = [r.short() for _ in range(r.varint())]
            attachment["width"] = r.float()
            attachment["height"] = r.float()
    elif kind == "linkedmesh":
        attachment.update(path=r.string(), color=r.color(), skin=r.string(), parent=r.string(), deform=r.boolean())
        if nonessential:
            attachment["width"] = r.float()
            attachment["height"] = r.float()
    elif kind == "boundingbox":
        count = r.varint()
        attachment["vertexCount"] = count
        attachment["vertices"], attachment["bonesWeighted"] = _read_vertices(r, count)
        if nonessential:
            attachment["color"] = r.color()
    elif kind == "point":
        attachment.update(rotation=r.float(), x=r.float(), y=r.float())
        if nonessential:
            attachment["color"] = r.color()
    else:
        raise ValueError(f"attachment type {kind} not supported")
    return attachment


def _read_vertices(r: Reader, count: int) -> tuple[list, bool]:
    """读顶点：不加权时为 x,y 列表；加权时为 [[(骨骼序号, x, y, 权重), ...], ...]。"""
    if not r.boolean():
        return [r.float() for _ in range(count * 2)], False
    vertices = []
    for _ in range(count):
        vertices.append([(r.varint(), r.float(), r.float(), r.float()) for _ in range(r.varint())])
    return vertices, True


def _read_curve_35(r: Reader) -> list | str | None:
    """读 3.5 曲线：None 线性、"stepped"、或 [cx1, cy1, cx2, cy2]（0–1 归一化）。"""
    kind = r.byte()
    if kind == CurveStepped:
        return "stepped"
    if kind == CurveBezier:
        return [r.float(), r.float(), r.float(), r.float()]
    return None


def _read_animation_35(r: Reader, result: dict) -> dict:
    """读 3.5 动画，返回 JSON 风格的时间轴（曲线为归一化贝塞尔）。"""
    animation: dict = {"slots": {}, "bones": {}, "ik": {}, "deform": {}, "drawOrder": [], "events": []}

    # 插槽时间轴
    for _ in range(r.varint()):
        slot = result["slots"][r.varint()]["name"]
        timelines = animation["slots"].setdefault(slot, {})
        for _ in range(r.varint()):
            kind = r.byte()
            count = r.varint()
            frames = []
            for i in range(count):
                if kind == 0:
                    frames.append({"time": r.float(), "name": r.string()})
                    continue
                frame = {"time": r.float(), "color": r.color()}
                if kind == 2:
                    frame["dark"] = f"{r.int32() & 0xFFFFFF:06x}"
                if i < count - 1:
                    frame["curve"] = _read_curve_35(r)
                frames.append(frame)
            timelines[["attachment", "color", "twoColor"][kind]] = frames

    # 骨骼时间轴
    for _ in range(r.varint()):
        bone = result["bones"][r.varint()]["name"]
        timelines = animation["bones"].setdefault(bone, {})
        for _ in range(r.varint()):
            kind = BoneTimelines35[r.byte()]
            count = r.varint()
            frames = []
            for i in range(count):
                if kind == "rotate":
                    frame = {"time": r.float(), "value": r.float()}
                else:
                    frame = {"time": r.float(), "x": r.float(), "y": r.float()}
                if i < count - 1:
                    frame["curve"] = _read_curve_35(r)
                frames.append(frame)
            timelines[kind] = frames

    # IK 时间轴
    for _ in range(r.varint()):
        ik = result["ik"][r.varint()]["name"]
        count = r.varint()
        frames = []
        for i in range(count):
            frame = {"time": r.float(), "mix": r.float(), "bendPositive": r.byte() > 0}
            if i < count - 1:
                frame["curve"] = _read_curve_35(r)
            frames.append(frame)
        animation["ik"][ik] = frames

    # 变换、路径约束时间轴（女骑士没有）
    if r.varint() != 0 or r.varint() != 0:
        raise ValueError("constraint timelines not supported")

    # 网格变形
    skin_names = list(result["skins"])
    for _ in range(r.varint()):
        skin = skin_names[r.varint()]
        for _ in range(r.varint()):
            slot = result["slots"][r.varint()]["name"]
            for _ in range(r.varint()):
                attachment = r.string()
                count = r.varint()
                frames = []
                for i in range(count):
                    frame = {"time": r.float()}
                    end = r.varint()
                    if end:
                        frame["offset"] = r.varint()
                        frame["vertices"] = [r.float() for _ in range(end)]
                    if i < count - 1:
                        frame["curve"] = _read_curve_35(r)
                    frames.append(frame)
                animation["deform"].setdefault(skin, {}).setdefault(slot, {})[attachment] = frames

    # 绘制顺序
    for _ in range(r.varint()):
        frame = {"time": r.float(), "offsets": []}
        for _ in range(r.varint()):
            frame["offsets"].append({"slot": result["slots"][r.varint()]["name"], "offset": r.varint()})
        animation["drawOrder"].append(frame)

    # 事件
    event_names = list(result["events"])
    for _ in range(r.varint()):
        event = {"time": r.float(), "name": event_names[r.varint()], "int": r.varint(False), "float": r.float()}
        if r.boolean():
            event["string"] = r.string()
        animation["events"].append(event)
    return animation


# ===== 4.2 读取 =====

BoneTimelines42 = ["rotate", "translate", "translatex", "translatey", "scale", "scalex", "scaley", "shear", "shearx",
                   "sheary", "inherit"]  # 4.2 骨骼时间轴类型
TwoValueTimelines = {"translate", "scale", "shear"}  # 两个数值的骨骼时间轴
SlotTimelines42 = ["attachment", "rgba", "rgb", "rgba2", "rgb2", "alpha"]  # 4.2 插槽时间轴类型
SlotChannels = {"rgba": 4, "rgb": 3, "rgba2": 7, "rgb2": 6, "alpha": 1}  # 每种颜色时间轴的通道数


def read_42(data: bytes) -> dict:
    """读取 Spine 4.2 二进制骨骼（不含物理、路径、变换约束和序列帧）。"""
    r = Reader(data)
    skeleton = {"hash": r.long(), "spine": r.string(), "x": r.float(), "y": r.float(), "width": r.float(),
                "height": r.float(), "referenceScale": r.float()}
    nonessential = r.boolean()
    if nonessential:
        skeleton.update(fps=r.float(), images=r.string(), audio=r.string())
    result = {"skeleton": skeleton, "nonessential": nonessential, "bones": [], "slots": [], "ik": [], "skins": {},
              "events": {}, "animations": {}}
    r.strings = [r.string() for _ in range(r.varint())]
    result["strings"] = r.strings

    # 骨骼
    for i in range(r.varint()):
        bone = {"name": r.string()}
        if i > 0:
            bone["parent"] = result["bones"][r.varint()]["name"]
        bone.update(rotation=r.float(), x=r.float(), y=r.float(), scaleX=r.float(), scaleY=r.float(),
                    shearX=r.float(), shearY=r.float(), length=r.float(), inherit=r.byte(), skin=r.boolean())
        if nonessential:
            bone.update(color=r.color(), icon=r.string(), visible=r.boolean())
        result["bones"].append(bone)

    # 插槽
    for _ in range(r.varint()):
        slot = {"name": r.string(), "bone": result["bones"][r.varint()]["name"], "color": r.color()}
        dark = r.int32()
        slot["dark"] = None if dark == -1 else f"{dark & 0xFFFFFF:06x}"
        slot["attachment"] = r.string_ref()
        slot["blend"] = r.varint()
        if nonessential:
            slot["visible"] = r.boolean()
        result["slots"].append(slot)

    # IK 约束
    for _ in range(r.varint()):
        ik = {"name": r.string(), "order": r.varint()}
        ik["bones"] = [result["bones"][r.varint()]["name"] for _ in range(r.varint())]
        ik["target"] = result["bones"][r.varint()]["name"]
        ik["flags"] = r.ubyte()
        if ik["flags"] & 32:
            ik["mix"] = r.float() if ik["flags"] & 64 else 1.0
        if ik["flags"] & 128:
            ik["softness"] = r.float()
        result["ik"].append(ik)
    for kind in ("transform", "path", "physics"):
        if r.varint() != 0:
            raise ValueError(f"{kind} constraints not supported")

    # 皮肤
    skin_names = []
    count = r.varint()
    if count:
        result["skins"]["default"] = _read_skin_slots_42(r, result, count, nonessential)
        skin_names.append("default")
    for _ in range(r.varint()):
        name = r.string()
        if nonessential:
            r.int32()
        for _ in range(5):
            if r.varint() != 0:
                raise ValueError("skin bones/constraints not supported")
        result["skins"][name] = _read_skin_slots_42(r, result, r.varint(), nonessential)
        skin_names.append(name)

    # 事件
    for _ in range(r.varint()):
        name = r.string()
        event = {"int": r.varint(False), "float": r.float(), "string": r.string(), "audio": r.string()}
        if event["audio"] is not None:
            event["volume"] = r.float()
            event["balance"] = r.float()
        result["events"][name] = event

    # 动画
    for _ in range(r.varint()):
        name = r.string()
        result["animations"][name] = _read_animation_42(r, result, skin_names)

    if r.pos != len(data):
        raise ValueError(f"4.2 reader stopped at {r.pos} of {len(data)}")
    return result


def _read_skin_slots_42(r: Reader, result: dict, slot_count: int, nonessential: bool) -> dict:
    """读 4.2 皮肤的插槽附件。"""
    skin: dict = {}
    for _ in range(slot_count):
        slot = result["slots"][r.varint()]["name"]
        attachments = skin.setdefault(slot, {})
        for _ in range(r.varint()):
            key = r.string_ref()
            attachments[key] = _read_attachment_42(r, key, nonessential)
    return skin


def _read_attachment_42(r: Reader, key: str, nonessential: bool) -> dict:
    """读 4.2 附件。"""
    flags = r.ubyte()
    attachment = {"name": r.string_ref() if flags & 8 else key, "type": AttachmentTypes[flags & 7]}
    kind = attachment["type"]
    if kind in ("region", "mesh", "linkedmesh"):
        attachment["path"] = r.string_ref() if flags & 16 else None
        attachment["color"] = r.color() if flags & 32 else "ffffffff"
        if flags & 64:
            raise ValueError("sequences not supported")
    if kind == "region":
        attachment["rotation"] = r.float() if flags & 128 else 0.0
        attachment.update(x=r.float(), y=r.float(), scaleX=r.float(), scaleY=r.float(), width=r.float(),
                          height=r.float())
    elif kind == "mesh":
        attachment["hull"] = r.varint()
        count = r.varint()
        attachment["vertexCount"] = count
        attachment["vertices"], attachment["bonesWeighted"] = _read_vertices_42(r, count, bool(flags & 128))
        attachment["uvs"] = [r.float() for _ in range(count * 2)]
        attachment["triangles"] = [r.varint() for _ in range((count * 2 - attachment["hull"] - 2) * 3)]
        if nonessential:
            attachment["edges"] = [r.varint() for _ in range(r.varint())]
            attachment["width"] = r.float()
            attachment["height"] = r.float()
    elif kind == "linkedmesh":
        attachment["timelines"] = bool(flags & 128)
        attachment["skin"] = r.varint()
        attachment["parent"] = r.string_ref()
        if nonessential:
            attachment["width"] = r.float()
            attachment["height"] = r.float()
    elif kind in ("boundingbox", "clipping"):
        if kind == "clipping":
            attachment["end"] = r.varint()
        count = r.varint()
        attachment["vertexCount"] = count
        attachment["vertices"], attachment["bonesWeighted"] = _read_vertices_42(r, count, bool(flags & 16))
        if nonessential:
            attachment["color"] = r.color()
    elif kind == "point":
        attachment.update(rotation=r.float(), x=r.float(), y=r.float())
        if nonessential:
            attachment["color"] = r.color()
    else:
        raise ValueError(f"attachment type {kind} not supported")
    return attachment


def _read_vertices_42(r: Reader, count: int, weighted: bool) -> tuple[list, bool]:
    """读 4.2 顶点（顶点数已读出）。"""
    if not weighted:
        return [r.float() for _ in range(count * 2)], False
    vertices = []
    for _ in range(count):
        vertices.append([(r.varint(), r.float(), r.float(), r.float()) for _ in range(r.varint())])
    return vertices, True


def _read_curve_42(r: Reader, values: int) -> list | str | None:
    """读 4.2 曲线：None 线性、"stepped"、或每个数值 4 个浮点（绝对时间和数值）。"""
    kind = r.byte()
    if kind == CurveStepped:
        return "stepped"
    if kind == CurveBezier:
        return [r.float() for _ in range(values * 4)]
    return None


def _read_values_timeline_42(r: Reader, frame_count: int, read_values, values: int) -> list:
    """首帧数值，然后每帧依次是"下一帧时间和数值、两帧之间的曲线"。read_values 读一帧的数值。"""
    frames = [{"time": r.float(), **read_values()}]
    for _ in range(frame_count - 1):
        frame = {"time": r.float(), **read_values()}
        frames[-1]["curve"] = _read_curve_42(r, values)
        frames.append(frame)
    return frames


def _read_animation_42(r: Reader, result: dict, skin_names: list[str]) -> dict:
    """读 4.2 动画。"""
    animation: dict = {"slots": {}, "bones": {}, "ik": {}, "deform": {}, "drawOrder": [], "events": []}
    r.varint()  # 时间轴总数，写出时重新计算

    # 插槽时间轴
    for _ in range(r.varint()):
        slot = result["slots"][r.varint()]["name"]
        timelines = animation["slots"].setdefault(slot, {})
        for _ in range(r.varint()):
            kind = SlotTimelines42[r.byte()]
            count = r.varint()
            if kind == "attachment":
                timelines[kind] = [{"time": r.float(), "name": r.string_ref()} for _ in range(count)]
                continue
            channels = SlotChannels[kind]
            r.varint()  # 贝塞尔数量，写出时重新计算
            timelines[kind] = _read_values_timeline_42(
                r, count, lambda: {"color": bytes(r.ubyte() for _ in range(channels)).hex()}, channels)

    # 骨骼时间轴
    for _ in range(r.varint()):
        bone = result["bones"][r.varint()]["name"]
        timelines = animation["bones"].setdefault(bone, {})
        for _ in range(r.varint()):
            kind = BoneTimelines42[r.byte()]
            count = r.varint()
            if kind == "inherit":
                timelines[kind] = [{"time": r.float(), "inherit": r.byte()} for _ in range(count)]
                continue
            r.varint()
            keys = ["x", "y"] if kind in TwoValueTimelines else ["value"]
            timelines[kind] = _read_values_timeline_42(r, count, lambda: {k: r.float() for k in keys}, len(keys))

    # IK 时间轴（标志位与数值按原样保存）
    for _ in range(r.varint()):
        ik = result["ik"][r.varint()]["name"]
        count = r.varint()
        r.varint()
        frames = []
        for i in range(count):
            flags = r.ubyte()
            frame = {"flags": flags, "time": r.float()}
            if flags & 1 and flags & 2:
                frame["mix"] = r.float()
            if flags & 4:
                frame["softness"] = r.float()
            if i > 0 and flags & 128:
                frames[-1]["curve"] = [r.float() for _ in range(8)]
            frames.append(frame)
        animation["ik"][ik] = frames
    for kind in ("transform", "path", "physics"):
        if r.varint() != 0:
            raise ValueError(f"{kind} timelines not supported")

    # 附件时间轴（网格变形）
    for _ in range(r.varint()):
        skin = skin_names[r.varint()]
        for _ in range(r.varint()):
            slot = result["slots"][r.varint()]["name"]
            for _ in range(r.varint()):
                attachment = r.string_ref()
                if r.byte() != 0:
                    raise ValueError("sequence timelines not supported")
                count = r.varint()
                r.varint()
                frames = []
                time = r.float()
                for i in range(count):
                    frame = {"time": time}
                    end = r.varint()
                    if end:
                        frame["offset"] = r.varint()
                        frame["vertices"] = [r.float() for _ in range(end)]
                    frames.append(frame)
                    if i == count - 1:
                        break
                    time = r.float()
                    frame["curve"] = _read_curve_42(r, 1)
                animation["deform"].setdefault(skin, {}).setdefault(slot, {})[attachment] = frames

    # 绘制顺序
    for _ in range(r.varint()):
        frame = {"time": r.float(), "offsets": []}
        for _ in range(r.varint()):
            frame["offsets"].append({"slot": result["slots"][r.varint()]["name"], "offset": r.varint()})
        animation["drawOrder"].append(frame)

    # 事件
    event_names = list(result["events"])
    for _ in range(r.varint()):
        event = {"time": r.float(), "name": event_names[r.varint()], "int": r.varint(False), "float": r.float(),
                 "string": r.string()}
        if result["events"][event["name"]]["audio"] is not None:
            event["volume"] = r.float()
            event["balance"] = r.float()
        animation["events"].append(event)
    return animation


# ===== 4.2 写出 =====


def write_42(skeleton: dict) -> bytes:
    """写出 Spine 4.2 二进制骨骼（结构与 read_42 的结果相同）。"""
    w = Writer()
    info = skeleton["skeleton"]
    nonessential = skeleton.get("nonessential", False)
    w.long(info["hash"])
    w.string(info["spine"])
    for key in ("x", "y", "width", "height", "referenceScale"):
        w.float(info[key])
    w.boolean(nonessential)
    if nonessential:
        w.float(info["fps"])
        w.string(info["images"])
        w.string(info["audio"])

    # 字符串表：读入时的原表原样保留（可能有重复，引用取第一个），再按首次引用的顺序补上新的
    for value in skeleton.get("strings", []):
        w.strings.append(value)
        w.string_index.setdefault(value, len(w.strings))
    for value in _string_refs(skeleton):
        if value not in w.string_index:
            w.strings.append(value)
            w.string_index[value] = len(w.strings)
    w.varint(len(w.strings))
    for value in w.strings:
        w.string(value)

    # 骨骼
    bone_index = {bone["name"]: i for i, bone in enumerate(skeleton["bones"])}
    w.varint(len(skeleton["bones"]))
    for i, bone in enumerate(skeleton["bones"]):
        w.string(bone["name"])
        if i > 0:
            w.varint(bone_index[bone["parent"]])
        for key in ("rotation", "x", "y", "scaleX", "scaleY", "shearX", "shearY", "length"):
            w.float(bone[key])
        w.byte(bone.get("inherit", 0))
        w.boolean(bone.get("skin", False))
        if nonessential:
            w.color(bone["color"])
            w.string(bone["icon"])
            w.boolean(bone["visible"])

    # 插槽
    slot_index = {slot["name"]: i for i, slot in enumerate(skeleton["slots"])}
    w.varint(len(skeleton["slots"]))
    for slot in skeleton["slots"]:
        w.string(slot["name"])
        w.varint(bone_index[slot["bone"]])
        w.color(slot.get("color", "ffffffff"))
        w.int32(-1 if slot.get("dark") is None else int(slot["dark"], 16))
        w.string_ref(slot.get("attachment"))
        w.varint(slot.get("blend", 0))
        if nonessential:
            w.boolean(slot["visible"])

    # IK 约束
    w.varint(len(skeleton["ik"]))
    for ik in skeleton["ik"]:
        w.string(ik["name"])
        w.varint(ik["order"])
        w.varint(len(ik["bones"]))
        for name in ik["bones"]:
            w.varint(bone_index[name])
        w.varint(bone_index[ik["target"]])
        w.byte(ik["flags"])
        if ik["flags"] & 32 and ik["flags"] & 64:
            w.float(ik["mix"])
        if ik["flags"] & 128:
            w.float(ik["softness"])
    for _ in range(3):
        w.varint(0)  # 变换、路径、物理约束

    # 皮肤
    skins = skeleton["skins"]
    skin_names = list(skins)
    if "default" in skins:
        _write_skin_slots_42(w, skins["default"], slot_index, nonessential)
    else:
        w.varint(0)
    others = [name for name in skin_names if name != "default"]
    w.varint(len(others))
    for name in others:
        w.string(name)
        if nonessential:
            w.int32(-1)
        for _ in range(5):
            w.varint(0)
        _write_skin_slots_42(w, skins[name], slot_index, nonessential)

    # 事件
    w.varint(len(skeleton["events"]))
    for name, event in skeleton["events"].items():
        w.string(name)
        w.varint(event["int"], False)
        w.float(event["float"])
        w.string(event["string"])
        w.string(event.get("audio"))
        if event.get("audio") is not None:
            w.float(event["volume"])
            w.float(event["balance"])

    # 动画
    w.varint(len(skeleton["animations"]))
    for name, animation in skeleton["animations"].items():
        w.string(name)
        _write_animation_42(w, skeleton, animation, bone_index, slot_index, skin_names)
    return bytes(w.out)


def _string_refs(skeleton: dict):
    """按写出顺序列出所有字符串引用。"""
    for slot in skeleton["slots"]:
        if slot.get("attachment") is not None:
            yield slot["attachment"]
    for skin in skeleton["skins"].values():
        for attachments in skin.values():
            for key, attachment in attachments.items():
                yield key
                if attachment["name"] != key:
                    yield attachment["name"]
                if attachment.get("path") is not None:
                    yield attachment["path"]
                if attachment["type"] == "linkedmesh":
                    yield attachment["parent"]
    for animation in skeleton["animations"].values():
        for timelines in animation["slots"].values():
            for frame in timelines.get("attachment", []):
                if frame["name"] is not None:
                    yield frame["name"]
        for slots in animation["deform"].values():
            for attachments in slots.values():
                yield from attachments


def _write_skin_slots_42(w: Writer, skin: dict, slot_index: dict, nonessential: bool) -> None:
    """写 4.2 皮肤的插槽附件。"""
    w.varint(len(skin))
    for slot, attachments in skin.items():
        w.varint(slot_index[slot])
        w.varint(len(attachments))
        for key, attachment in attachments.items():
            w.string_ref(key)
            _write_attachment_42(w, key, attachment, nonessential)


def _write_attachment_42(w: Writer, key: str, attachment: dict, nonessential: bool) -> None:
    """写 4.2 附件。"""
    kind = attachment["type"]
    flags = AttachmentTypes.index(kind)
    if attachment["name"] != key:
        flags |= 8
    if kind in ("region", "mesh", "linkedmesh"):
        if attachment.get("path") is not None:
            flags |= 16
        if attachment.get("color", "ffffffff") != "ffffffff":
            flags |= 32
    if kind == "region" and attachment.get("rotation", 0.0) != 0.0:
        flags |= 128
    if kind == "mesh" and attachment["bonesWeighted"]:
        flags |= 128
    if kind == "linkedmesh" and attachment["timelines"]:
        flags |= 128
    if kind in ("boundingbox", "clipping") and attachment["bonesWeighted"]:
        flags |= 16
    w.byte(flags)
    if flags & 8:
        w.string_ref(attachment["name"])
    if kind in ("region", "mesh", "linkedmesh"):
        if flags & 16:
            w.string_ref(attachment["path"])
        if flags & 32:
            w.color(attachment["color"])
    if kind == "region":
        if flags & 128:
            w.float(attachment["rotation"])
        for name in ("x", "y", "scaleX", "scaleY", "width", "height"):
            w.float(attachment[name])
    elif kind == "mesh":
        w.varint(attachment["hull"])
        _write_vertices_42(w, attachment)
        for value in attachment["uvs"]:
            w.float(value)
        for value in attachment["triangles"]:
            w.varint(value)
        if nonessential:
            w.varint(len(attachment["edges"]))
            for value in attachment["edges"]:
                w.varint(value)
            w.float(attachment["width"])
            w.float(attachment["height"])
    elif kind == "linkedmesh":
        w.varint(attachment["skin"])
        w.string_ref(attachment["parent"])
        if nonessential:
            w.float(attachment["width"])
            w.float(attachment["height"])
    elif kind in ("boundingbox", "clipping"):
        if kind == "clipping":
            w.varint(attachment["end"])
        _write_vertices_42(w, attachment)
        if nonessential:
            w.color(attachment["color"])
    elif kind == "point":
        for name in ("rotation", "x", "y"):
            w.float(attachment[name])
        if nonessential:
            w.color(attachment["color"])


def _write_vertices_42(w: Writer, attachment: dict) -> None:
    """写顶点数和顶点。"""
    w.varint(attachment["vertexCount"])
    if not attachment["bonesWeighted"]:
        for value in attachment["vertices"]:
            w.float(value)
        return
    for vertex in attachment["vertices"]:
        w.varint(len(vertex))
        for bone, x, y, weight in vertex:
            w.varint(bone)
            w.float(x)
            w.float(y)
            w.float(weight)


def _bezier_count(frames: list, values: int) -> int:
    """时间轴里贝塞尔曲线的数量（每段每个数值一条）。"""
    return sum(values for frame in frames[:-1] if isinstance(frame.get("curve"), list))


def _write_curve_42(w: Writer, curve) -> None:
    """写帧间曲线。"""
    if curve == "stepped":
        w.byte(CurveStepped)
    elif isinstance(curve, list):
        w.byte(CurveBezier)
        for value in curve:
            w.float(value)
    else:
        w.byte(CurveLinear)


def _write_values_timeline_42(w: Writer, frames: list, write_values) -> None:
    """写首帧数值，然后每帧依次写下一帧时间和数值、两帧之间的曲线。"""
    for i, frame in enumerate(frames):
        w.float(frame["time"])
        write_values(frame)
        if i > 0:
            _write_curve_42(w, frames[i - 1].get("curve"))


def _write_animation_42(w: Writer, skeleton: dict, animation: dict, bone_index: dict, slot_index: dict,
                        skin_names: list[str]) -> None:
    """写 4.2 动画。"""
    deform_count = sum(len(a) for slots in animation["deform"].values() for a in slots.values())
    total = (sum(len(t) for t in animation["slots"].values()) + sum(len(t) for t in animation["bones"].values())
             + len(animation["ik"]) + deform_count + (1 if animation["drawOrder"] else 0)
             + (1 if animation["events"] else 0))
    w.varint(total)

    # 插槽时间轴
    w.varint(len(animation["slots"]))
    for slot, timelines in animation["slots"].items():
        w.varint(slot_index[slot])
        w.varint(len(timelines))
        for kind, frames in timelines.items():
            w.byte(SlotTimelines42.index(kind))
            w.varint(len(frames))
            if kind == "attachment":
                for frame in frames:
                    w.float(frame["time"])
                    w.string_ref(frame["name"])
                continue
            w.varint(_bezier_count(frames, SlotChannels[kind]))
            _write_values_timeline_42(w, frames, lambda f: w.out.extend(bytes.fromhex(f["color"])))

    # 骨骼时间轴
    w.varint(len(animation["bones"]))
    for bone, timelines in animation["bones"].items():
        w.varint(bone_index[bone])
        w.varint(len(timelines))
        for kind, frames in timelines.items():
            w.byte(BoneTimelines42.index(kind))
            w.varint(len(frames))
            if kind == "inherit":
                for frame in frames:
                    w.float(frame["time"])
                    w.byte(frame["inherit"])
                continue
            keys = ["x", "y"] if kind in TwoValueTimelines else ["value"]
            w.varint(_bezier_count(frames, len(keys)))
            _write_values_timeline_42(w, frames, lambda f, keys=keys: [w.float(f[k]) for k in keys])

    # IK 时间轴
    ik_index = {ik["name"]: i for i, ik in enumerate(skeleton["ik"])}
    w.varint(len(animation["ik"]))
    for ik, frames in animation["ik"].items():
        w.varint(ik_index[ik])
        w.varint(len(frames))
        w.varint(_bezier_count(frames, 2))
        for i, frame in enumerate(frames):
            w.byte(frame["flags"])
            w.float(frame["time"])
            if frame["flags"] & 1 and frame["flags"] & 2:
                w.float(frame["mix"])
            if frame["flags"] & 4:
                w.float(frame["softness"])
            if i > 0 and frame["flags"] & 128:
                for value in frames[i - 1]["curve"]:
                    w.float(value)
    for _ in range(3):
        w.varint(0)

    # 附件时间轴（网格变形）
    w.varint(len(animation["deform"]))
    for skin, slots in animation["deform"].items():
        w.varint(skin_names.index(skin))
        w.varint(len(slots))
        for slot, attachments in slots.items():
            w.varint(slot_index[slot])
            w.varint(len(attachments))
            for attachment, frames in attachments.items():
                w.string_ref(attachment)
                w.byte(0)
                w.varint(len(frames))
                w.varint(_bezier_count(frames, 1))
                w.float(frames[0]["time"])
                for i, frame in enumerate(frames):
                    vertices = frame.get("vertices")
                    w.varint(len(vertices) if vertices else 0)
                    if vertices:
                        w.varint(frame["offset"])
                        for value in vertices:
                            w.float(value)
                    if i == len(frames) - 1:
                        break
                    w.float(frames[i + 1]["time"])
                    _write_curve_42(w, frame.get("curve"))

    # 绘制顺序
    w.varint(len(animation["drawOrder"]))
    for frame in animation["drawOrder"]:
        w.float(frame["time"])
        w.varint(len(frame["offsets"]))
        for item in frame["offsets"]:
            w.varint(slot_index[item["slot"]])
            w.varint(item["offset"])

    # 事件
    event_names = list(skeleton["events"])
    w.varint(len(animation["events"]))
    for event in animation["events"]:
        w.float(event["time"])
        w.varint(event_names.index(event["name"]))
        w.varint(event["int"], False)
        w.float(event["float"])
        w.string(event.get("string"))
        if skeleton["events"][event["name"]].get("audio") is not None:
            w.float(event["volume"])
            w.float(event["balance"])
