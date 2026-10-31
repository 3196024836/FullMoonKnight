"""女骑士（小红帽）的动作（骨骼见 build_knight_rig.py）。

动作：idle_loop、relaxed_loop、blink（第 2 轨，由 C# 组件随机触发）、attack、cast、hurt、die，以及三幕休息处的循环。
骨骼的绑定旋转都是 0，所以这里的旋转角就是相对绑定姿势的增量；角度逆时针为正（角色面朝右）。
做法：
  - 循环动作：正弦摆动（呼吸、风吹披风和头发）加常量姿势偏移，首尾相接
  - 关键帧动作：主动骨骼按关键帧缓入缓出，叠加在待机第一帧上
  - 跟随摆动：头、兜帽尖、头发、披风、裙摆按臀部的加速度和躯干的角加速度做阻尼弹簧（自动滞后和回弹）
  - 死亡：剑和盾在倒地过程中脱手，按世界坐标落到地面；死亡和休息姿势按需抬高臀部，保证上身不低于地面
全部按 30 帧每秒采样成线性关键帧。
"""

import math

import spine_pose as sp

SampleRate = 30  # 采样帧率
SimulationRate = 240  # 弹簧积分频率
IdlePeriod = 2.4  # 待机周期（秒）
RelaxedPeriod = 3.6  # 放松待机周期
FadeTime = 0.15  # 关键帧动作末尾，弹簧摆动淡出的时长（与下一个动作衔接）
NeutralValues = {"rotate": [0.0], "translate": [0.0, 0.0], "scale": [1.0, 1.0]}  # 各种时间轴的初值
FieldNames = {"rotate": ["value"], "translate": ["x", "y"], "scale": ["x", "y"]}  # 各种时间轴的字段
IgnoredSlots = {"shadow", "slash", "glow"}  # 检查是否入地时不算的插槽

# 循环摆动：{骨骼: {种类: (幅度, 相位)}}；translate、scale 的幅度是 (x, y)，scale 为 1 加幅度
Breath = {
    "hip": {"translate": ((0, -5), 0.0)},
    "torso": {"rotate": (1.2, 0.35), "scale": ((0.0, 0.014), 0.2)},
    "neck": {"rotate": (-0.8, 0.5)},
    "head": {"rotate": (1.6, 0.8)},
    "hood_tip": {"rotate": (5.0, 1.5)},
    "hair_a1": {"rotate": (3.0, 1.2)},
    "hair_a2": {"rotate": (5.0, 1.8)},
    "hair_b1": {"rotate": (2.5, 1.3)},
    "hair_b2": {"rotate": (4.0, 1.9)},
    "cape_root": {"rotate": (2.5, 1.0)},
    "cape1": {"rotate": (5.0, 1.6)},
    "cape2": {"rotate": (8.0, 2.2)},
    "skirt_b": {"rotate": (1.5, 1.1)},
    "skirt_f": {"rotate": (1.2, 1.3)},
    "arm_upper": {"rotate": (1.8, 0.6)},
    "arm_lower": {"rotate": (1.4, 0.9)},
    "sword": {"rotate": (-1.5, 1.1)},
    "shield_arm": {"rotate": (1.5, 0.7)},
    "shield": {"rotate": (-1.0, 0.9)},
}

# 跟随摆动的弹簧：{骨骼: (平移系数, 转动系数, 频率 Hz, 阻尼比)}
# 受力 = 平移系数 × (骨骼方向 × 反向加速度) − 转动系数 × 躯干角加速度；稳态偏角 = 受力 / ω²
Springs = {
    "head": (0.06, 0.45, 3.5, 0.45),
    "hood_tip": (0.10, 0.30, 2.6, 0.30),
    "hair_a1": (0.11, 0.30, 2.6, 0.32),
    "hair_a2": (0.09, 0.20, 2.1, 0.30),
    "hair_b1": (0.10, 0.30, 2.6, 0.32),
    "hair_b2": (0.08, 0.20, 2.1, 0.30),
    "cape_root": (0.05, 0.25, 2.2, 0.40),
    "cape1": (0.07, 0.25, 1.8, 0.32),
    "cape2": (0.08, 0.20, 1.5, 0.30),
    "skirt_b": (0.04, 0.0, 2.4, 0.40),
    "skirt_f": (0.04, 0.0, 2.4, 0.40),
}
TorsoChildren = {"head", "hood_tip", "hair_a1", "hair_a2", "hair_b1", "hair_b2", "cape_root", "cape1", "cape2"}  # 随躯干转

# 关键帧：{骨骼: {种类: [(时间, 数值...)]}}；"slots": {插槽: {"attachment": [(时间, 附件名)], "rgba": [(时间, 颜色)]}}
Attack = {
    "duration": 0.8,
    "hip": {"translate": [(0, 0, 0), (0.16, -22, -16), (0.28, 95, -18), (0.45, 90, -15), (0.8, 0, 0)]},
    "torso": {"rotate": [(0, 0), (0.16, 9), (0.28, -12), (0.45, -10), (0.8, 0)],
              "scale": [(0, 1, 1), (0.16, 1.02, 0.96), (0.28, 0.98, 1.03), (0.45, 1, 1), (0.8, 1, 1)]},
    "head": {"rotate": [(0, 0), (0.16, -4), (0.28, 6), (0.45, 4), (0.8, 0)]},
    "arm_upper": {"rotate": [(0, 0), (0.16, -128), (0.21, -148), (0.29, -285), (0.45, -280), (0.8, -360)]},
    "arm_lower": {"rotate": [(0, 0), (0.16, 25), (0.21, 30), (0.29, -5), (0.45, 0), (0.8, 0)]},
    "sword": {"rotate": [(0, 0), (0.16, 45), (0.21, 55), (0.29, 75), (0.45, 62), (0.62, 66), (0.8, 0)]},
    "shield_arm": {"rotate": [(0, 0), (0.16, 10), (0.29, -15), (0.45, -12), (0.8, 0)]},
    "leg_b": {"rotate": [(0, 0), (0.16, 4), (0.28, -15), (0.45, -14), (0.8, 0)]},
    "leg_f": {"translate": [(0, 0, 0), (0.16, 0, 0), (0.22, 45, 16), (0.27, 85, 0), (0.5, 85, 0), (0.8, 0, 0)],
              "rotate": [(0, 0), (0.22, -10), (0.27, -4), (0.5, -4), (0.8, 0)]},
    "slots": {"slash": {"attachment": [(0, None), (0.21, "slash"), (0.38, None)],
                        "rgba": [(0, "ffffff00"), (0.21, "ffffff00"), (0.25, "ffffffff"), (0.36, "ffffff00")]}},
}
Cast = {
    "duration": 0.9,
    "hip": {"translate": [(0, 0, 0), (0.18, -12, -10), (0.32, 30, -6), (0.6, 28, -6), (0.9, 0, 0)]},
    "torso": {"rotate": [(0, 0), (0.18, 6), (0.32, -8), (0.6, -7), (0.9, 0)]},
    "head": {"rotate": [(0, 0), (0.32, -4), (0.6, -4), (0.9, 0)]},
    "shield_arm": {"rotate": [(0, 0), (0.18, -10), (0.32, 32), (0.6, 30), (0.9, 0)]},
    "shield": {"rotate": [(0, 0), (0.18, 6), (0.32, -26), (0.6, -24), (0.9, 0)],
               "translate": [(0, 0, 0), (0.32, 18, 4), (0.6, 16, 4), (0.9, 0, 0)]},
    "arm_upper": {"rotate": [(0, 0), (0.32, -10), (0.6, -10), (0.9, 0)]},
    "sword": {"rotate": [(0, 0), (0.32, 8), (0.6, 8), (0.9, 0)]},
    "leg_f": {"translate": [(0, 0, 0), (0.18, 0, 0), (0.26, 18, 10), (0.32, 30, 0), (0.6, 30, 0), (0.9, 0, 0)]},
    "slots": {"glow": {"attachment": [(0, None), (0.28, "glow"), (0.78, None)],
                       "rgba": [(0, "ffffff00"), (0.28, "ffffff00"), (0.34, "ffffffc0"), (0.75, "ffffff00")]}},
}
Hurt = {
    "duration": 0.55,
    # 被击中：瞬间后仰被推开、身子一缩、闭眼皱眉，盾往身前收；随后低头缩一下再站稳（脚不离地）
    "hip": {"translate": [(0, 0, 0), (0.05, -48, -14), (0.18, -40, -18), (0.55, 0, 0)]},
    "torso": {"rotate": [(0, 0), (0.05, 13), (0.18, -5), (0.32, -2), (0.55, 0)],
              "scale": [(0, 1, 1), (0.05, 1.04, 0.95), (0.18, 0.99, 1.01), (0.55, 1, 1)]},
    "neck": {"rotate": [(0, 0), (0.05, 6), (0.18, -4), (0.55, 0)]},
    "head": {"rotate": [(0, 0), (0.05, 14), (0.18, -9), (0.32, -4), (0.55, 0)]},
    "shield_arm": {"rotate": [(0, 0), (0.05, 22), (0.18, 18), (0.55, 0)]},
    "shield": {"rotate": [(0, 0), (0.05, -14), (0.18, -10), (0.55, 0)],
               "translate": [(0, 0, 0), (0.05, -10, 6), (0.18, -8, 4), (0.55, 0, 0)]},
    "arm_upper": {"rotate": [(0, 0), (0.05, -22), (0.18, 6), (0.55, 0)]},
    "arm_lower": {"rotate": [(0, 0), (0.05, -12), (0.18, 8), (0.55, 0)]},
    "sword": {"rotate": [(0, 0), (0.05, 10), (0.18, -6), (0.55, 0)]},
    "slots": {"eyes_closed": {"attachment": [(0, None), (0.03, "eyes_closed"), (0.3, None)]}},
}
Die = {
    "duration": 1.6,
    "hold": True,
    # 中招后仰 → 膝盖一软跪坐（长裙铺开，剑脱手）→ 想撑起来晃了一下 → 向前瘫软低头（盾脱手），闭眼
    "hip": {"translate": [(0, 0, 0), (0.1, -30, -6), (0.42, -15, -168), (0.7, -18, -160), (1.08, -10, -172),
                          (1.25, -12, -168), (1.6, -12, -170)]},
    "torso": {"rotate": [(0, 0), (0.1, 10), (0.42, -16), (0.7, -5), (1.08, -34), (1.25, -28), (1.6, -30)]},
    "neck": {"rotate": [(0, 0), (0.42, -8), (0.7, -2), (1.08, -12), (1.6, -12)]},
    "head": {"rotate": [(0, 0), (0.1, 10), (0.42, -20), (0.7, -8), (1.08, -28), (1.25, -22), (1.6, -24)]},
    "arm_upper": {"rotate": [(0, 0), (0.1, -15), (0.42, 22), (0.7, 18), (1.08, 40), (1.6, 38)]},
    "arm_lower": {"rotate": [(0, 0), (0.42, 25), (0.7, 20), (1.08, 30), (1.6, 28)]},
    "shield_arm": {"rotate": [(0, 0), (0.1, 15), (0.42, -25), (0.7, -15), (1.08, -45), (1.6, -42)]},
    "skirt_b": {"scale": [(0, 1, 1), (0.42, 1.22, 0.58), (1.6, 1.22, 0.58)],
                "rotate": [(0, 0), (0.42, -10), (1.6, -10)]},
    "skirt_f": {"scale": [(0, 1, 1), (0.42, 1.22, 0.58), (1.6, 1.22, 0.58)],
                "rotate": [(0, 0), (0.42, 8), (1.6, 8)]},
    "cape_root": {"rotate": [(0, 0), (0.42, -4), (0.7, -8), (1.08, 12), (1.6, 10)]},
    # 低头时头发和兜帽尖顺着重力往下垂（抵消头部转动）
    "hood_tip": {"rotate": [(0, 0), (0.42, 15), (0.7, 6), (1.08, 30), (1.6, 28)]},
    "hair_a1": {"rotate": [(0, 0), (0.42, 18), (0.7, 8), (1.08, 38), (1.6, 36)]},
    "hair_a2": {"rotate": [(0, 0), (0.42, 8), (1.08, 16), (1.6, 15)]},
    "hair_b1": {"rotate": [(0, 0), (0.42, 15), (0.7, 6), (1.08, 32), (1.6, 30)]},
    "hair_b2": {"rotate": [(0, 0), (0.42, 6), (1.08, 12), (1.6, 12)]},
    "cape1": {"rotate": [(0, 0), (0.42, 10), (1.08, 14), (1.6, 14)]},
    "cape2": {"rotate": [(0, 0), (0.42, 14), (1.08, 18), (1.6, 18)]},
    # 腿收进长裙里（跪坐时看不到靴子）
    "leg_b": {"translate": [(0, 0, 0), (0.1, 0, 0), (0.42, 25, 95), (1.6, 25, 95)]},
    "leg_f": {"translate": [(0, 0, 0), (0.1, 0, 0), (0.42, -60, 95), (1.6, -60, 95)]},
    "slots": {"eyes_closed": {"attachment": [(0, None), (0.95, "eyes_closed")]}},
    # 脱手：(开始时间, 落地时间, 骨骼原点的世界坐标, 世界角度)
    "drop": {"sword": (0.3, 0.62, (110, 12), 163), "shield": (0.78, 1.1, (300, 88), -90)},
}

# 放松待机：剑放低（常量偏移）
Relaxed = {"arm_upper": {"rotate": 12}, "sword": {"rotate": -12}, "shield_arm": {"rotate": -6}}
# 休息处：跪坐在篝火边（长裙铺开，披风拖在身后地上），剑平放在身前地上，盾靠在身后，
# 前手伸向篝火取暖，后手搭在膝上，微微前倾看着火（常量偏移，叠加在放松摆动上）
Rest = {
    "hip": {"translate": (-10, -168)},
    "torso": {"rotate": -6},
    "head": {"rotate": -6},
    "skirt_b": {"scale": (1.22, 0.58), "rotate": -10},
    "skirt_f": {"scale": (1.22, 0.58), "rotate": 8},
    "cape_root": {"rotate": -18},
    "cape1": {"rotate": 12},
    "cape2": {"rotate": 18},
    "leg_b": {"translate": (25, 95)},
    "leg_f": {"translate": (-60, 95)},
    "arm_upper": {"rotate": 35},
    "arm_lower": {"rotate": 40},
    "shield_arm": {"rotate": -5},
}
# 休息处放下的武器：{骨骼: (骨骼原点的世界坐标, 世界角度)}
RestProps = {"sword": ((-30, 12), 163), "shield": ((-255, 160), 12)}
RestNames = ["overgrowth_loop", "hive_loop", "glory_loop"]  # 三幕休息处的动作名


def duration(animation: dict) -> float:
    """动作时长：所有时间轴最后一帧的最大时间。"""
    times = [frames[-1]["time"] for timelines in animation["bones"].values() for frames in timelines.values()]
    times += [frames[-1]["time"] for timelines in animation["slots"].values() for frames in timelines.values()]
    return max(times)


def _ease(p: float) -> float:
    """缓入缓出。"""
    return p * p * (3 - 2 * p)


def _keyed(keys: list[tuple], time: float) -> list[float]:
    """关键帧在 time 时的数值（缓入缓出）。"""
    if time <= keys[0][0]:
        return list(keys[0][1:])
    for a, b in zip(keys, keys[1:]):
        if time <= b[0]:
            p = _ease((time - a[0]) / (b[0] - a[0]))
            return [va + (vb - va) * p for va, vb in zip(a[1:], b[1:])]
    return list(keys[-1][1:])


def _combine(kind: str, base: list[float], extra: list[float]) -> list[float]:
    """叠加两组数值：缩放相乘，其余相加。"""
    if kind == "scale":
        return [b * e for b, e in zip(base, extra)]
    return [b + e for b, e in zip(base, extra)]


def _breath(bone: str, kind: str, time: float, period: float, strength: float) -> list[float] | None:
    """循环摆动在 time 时的数值（没有这种摆动时为 None）。"""
    if kind not in Breath.get(bone, {}):
        return None
    amplitude, phase = Breath[bone][kind]
    wave = math.sin(math.tau * time / period - phase) * strength
    if kind == "rotate":
        return [amplitude * wave]
    if kind == "scale":
        return [1 + amplitude[0] * wave, 1 + amplitude[1] * wave]
    return [amplitude[0] * wave, amplitude[1] * wave]


def _times(length: float) -> list[float]:
    """按采样帧率划分的时间点（含首尾）。"""
    count = max(1, int(round(length * SampleRate)))
    return [length * i / count for i in range(count + 1)]


def _frames(times: list[float], channels: dict) -> dict:
    """{骨骼: {种类: [数值列表]}} 转成线性关键帧。"""
    bones = {}
    for bone in sorted(channels):
        bones[bone] = {kind: [{"time": t, **dict(zip(FieldNames[kind], v)), "curve": None}
                              for t, v in zip(times, values)] for kind, values in channels[bone].items()}
    return bones


def _empty(bones: dict) -> dict:
    """动作数据结构。"""
    return {"slots": {}, "bones": bones, "ik": {}, "deform": {}, "drawOrder": [], "events": []}


def _loop(period: float, offsets: dict, strength: float) -> dict:
    """循环动作：正弦摆动加常量偏移（姿势）。"""
    times = _times(period)
    channels: dict = {}
    for bone in set(Breath) | set(offsets):
        for kind in FieldNames:
            offset = offsets.get(bone, {}).get(kind)
            if kind not in Breath.get(bone, {}) and offset is None:
                continue
            extra = NeutralValues[kind] if offset is None else ([offset] if kind == "rotate" else list(offset))
            channels.setdefault(bone, {})[kind] = [
                _combine(kind, _breath(bone, kind, t, period, strength) or NeutralValues[kind], extra) for t in times]
    return _empty(_frames(times, channels))


def _spring_offsets(spec: dict, length: float) -> dict[str, list[float]]:
    """跟随摆动：按臀部平移和躯干转动的关键帧积分弹簧，返回 {骨骼: 每个采样时间的偏角}。"""
    from build_knight_rig import Bones  # 骨骼方向取绑定姿势（避免循环导入，放在函数里）
    direction = {}
    for name, _, joint, tip in Bones:
        angle = math.atan2(joint[1] - tip[1], tip[0] - joint[0])  # 画布 y 向下，转成 y 向上的角度
        direction[name] = angle
    hip_keys = spec.get("hip", {}).get("translate", [(0, 0, 0)])
    torso_keys = spec.get("torso", {}).get("rotate", [(0, 0)])
    dt = 1 / SimulationRate
    steps = int(round(length * SimulationRate))

    def position(t: float) -> tuple[float, float, float]:
        x, y = _keyed(hip_keys, t)
        return x, y, _keyed(torso_keys, t)[0]

    # 二阶差分求加速度
    samples = [position(i * dt) for i in range(-1, steps + 2)]
    result = {}
    for bone, (linear, angular, hertz, damping) in Springs.items():
        omega = math.tau * hertz
        angle = velocity = 0.0
        series = []
        for i in range(steps + 1):
            before, now, after = samples[i], samples[i + 1], samples[i + 2]
            ax, ay, alpha = ((after[k] - 2 * now[k] + before[k]) / dt ** 2 for k in range(3))
            heading = direction[bone] + (math.radians(now[2]) if bone in TorsoChildren else 0.0)
            # 骨骼方向与反向加速度的叉积：加速时末端向后甩
            force = linear * (math.cos(heading) * -ay - math.sin(heading) * -ax) - angular * alpha
            velocity += (force - 2 * damping * omega * velocity - omega ** 2 * angle) * dt
            angle += velocity * dt
            series.append(angle)
        result[bone] = series
    return result


def _authored(skeleton: dict, spec: dict) -> dict:
    """关键帧动作：叠加在待机第一帧上，加上跟随摆动和插槽切换。"""
    length = spec["duration"]
    times = _times(length)
    springs = _spring_offsets(spec, length)
    keyed_bones = {k for k in spec if k not in ("duration", "hold", "slots", "drop")}
    channels: dict = {}
    for bone in set(Breath) | keyed_bones | set(Springs):
        for kind in FieldNames:
            base = _breath(bone, kind, 0.0, IdlePeriod, 1.0)
            keys = spec.get(bone, {}).get(kind)
            spring = springs.get(bone) if kind == "rotate" else None
            if base is None and keys is None and spring is None:
                continue
            values = []
            for t in times:
                value = base or list(NeutralValues[kind])
                if keys:
                    value = _combine(kind, value, _keyed(keys, t))
                if spring:
                    # 末尾淡出，免得切回待机时跳动（死亡停在最后姿势，不淡出）
                    fade = 1.0 if spec.get("hold") else min(1.0, (length - t) / FadeTime)
                    value = [value[0] + spring[int(round(t * SimulationRate))] * _ease(max(0.0, fade))]
                values.append(value)
            channels.setdefault(bone, {})[kind] = values
    animation = _empty(_frames(times, channels))
    for slot, timelines in spec.get("slots", {}).items():
        animation["slots"][slot] = {}
        if "attachment" in timelines:
            animation["slots"][slot]["attachment"] = [{"time": t, "name": n} for t, n in timelines["attachment"]]
        if "rgba" in timelines:
            animation["slots"][slot]["rgba"] = [{"time": t, "color": c, "curve": None} for t, c in timelines["rgba"]]
    if spec.get("hold"):
        _ground(skeleton, animation, {"leg_b", "leg_f", "sword", "shield", "glow"})
    for bone, (start, end, target, angle) in spec.get("drop", {}).items():
        _drop(skeleton, animation, times, bone, start, end, target, angle)
    return animation


def _world(skeleton: dict, animation: dict, time: float) -> dict:
    """某一时刻各骨骼的世界变换。"""
    probe = dict(skeleton, animations={"probe": animation})
    return sp.pose(probe, "probe", time)["world"]


def _timelines(animation: dict, times: list[float], bone: str) -> tuple[list, list]:
    """骨骼的旋转和平移时间轴（没有时补上全零关键帧）。"""
    timelines = animation["bones"].setdefault(bone, {})
    if "rotate" not in timelines:
        timelines["rotate"] = [{"time": t, "value": 0.0, "curve": None} for t in times]
    if "translate" not in timelines:
        timelines["translate"] = [{"time": t, "x": 0.0, "y": 0.0, "curve": None} for t in times]
    return timelines["rotate"], timelines["translate"]


def _place(skeleton: dict, animation: dict, bone: str, r_frame: dict, t_frame: dict, target: tuple, angle: float,
           previous: float | None) -> float:
    """改写一帧，让骨骼原点落在世界坐标 target、世界角度为 angle；返回写入的局部角度（与上一帧连续）。"""
    setup = next(b for b in skeleton["bones"] if b["name"] == bone)
    pa, pb, pc, pd, px, py = _world(skeleton, animation, r_frame["time"])[setup["parent"]]
    det = pa * pd - pb * pc
    lx = (pd * (target[0] - px) - pb * (target[1] - py)) / det
    ly = (-pc * (target[0] - px) + pa * (target[1] - py)) / det
    t_frame["x"], t_frame["y"] = lx - setup["x"], ly - setup["y"]
    local = angle - math.degrees(math.atan2(pc, pa)) - setup["rotation"]
    # 避免绕远路
    if previous is not None:
        local = previous + (local - previous + 180) % 360 - 180
    r_frame["value"] = local
    return local


def _drop(skeleton: dict, animation: dict, times: list[float], bone: str, start: float, end: float, target: tuple,
          angle: float) -> None:
    """脱手：从 start 起骨骼按世界坐标加速落到 target（世界角度 angle），改写该骨骼的平移和旋转关键帧。"""
    rotate, translate = _timelines(animation, times, bone)
    # 脱手瞬间的世界姿态
    a, _, c, _, x0, y0 = _world(skeleton, animation, start)[bone]
    angle0 = math.degrees(math.atan2(c, a))
    turn = (angle - angle0 + 180) % 360 - 180
    previous = None
    for r_frame, t_frame in zip(rotate, translate):
        t = r_frame["time"]
        if t <= start:
            continue
        progress = min(1.0, (t - start) / (end - start))
        p = progress ** 2  # 下落加速
        point = (x0 + (target[0] - x0) * p, y0 + (target[1] - y0) * p)
        # 先翻转再落地，免得剑尖、盾角扎进地面
        previous = _place(skeleton, animation, bone, r_frame, t_frame, point, angle0 + turn * math.sqrt(progress),
                          previous)


def _pin(skeleton: dict, animation: dict, props: dict) -> None:
    """放下的道具：每一帧都把骨骼固定在世界坐标里（不随身体摆动）。props 为 {骨骼: (世界坐标, 世界角度)}。"""
    times = [frame["time"] for frame in animation["bones"]["hip"]["translate"]]
    for bone, (target, angle) in props.items():
        rotate, translate = _timelines(animation, times, bone)
        previous = None
        for r_frame, t_frame in zip(rotate, translate):
            previous = _place(skeleton, animation, bone, r_frame, t_frame, target, angle, previous)


def _ground(skeleton: dict, animation: dict, ignored: set[str]) -> None:
    """抬高臀部：每一帧里上身最低点低于地面时，加到臀部的平移上（脚和影子留在地面）。"""
    reference = next(iter(animation["bones"].values()))
    frames = next(iter(reference.values()))
    lift = []
    for frame in frames:
        state = sp.pose(dict(skeleton, animations={"probe": animation}), "probe", frame["time"])
        lowest = 0.0
        for slot in skeleton["slots"]:
            name = state["slots"][slot["name"]]["attachment"]
            if name is None or slot["name"] in IgnoredSlots | ignored:
                continue
            attachment = skeleton["skins"]["default"][slot["name"]][name]
            lowest = min(lowest, float(sp.attachment_vertices(skeleton, state, slot, attachment)[:, 1].min()))
        lift.append(-lowest)
    hip = animation["bones"]["hip"]["translate"]
    for frame, amount in zip(hip, lift):
        frame["y"] += amount


def _blink() -> dict:
    """眨眼：闭眼 0.12 秒（放在第 2 轨，只切换闭眼插槽）。"""
    animation = _empty({})
    animation["slots"]["eyes_closed"] = {"attachment": [{"time": 0.0, "name": "eyes_closed"},
                                                        {"time": 0.12, "name": None}]}
    return animation


def build(skeleton: dict) -> dict:
    """生成全部动作。"""
    die = _authored(skeleton, Die)
    rest = _loop(RelaxedPeriod, Rest, 0.7)
    _ground(skeleton, rest, {"leg_b", "leg_f"} | set(RestProps))
    _pin(skeleton, rest, RestProps)
    return {
        "idle_loop": _loop(IdlePeriod, {}, 1.0),
        "relaxed_loop": _loop(RelaxedPeriod, Relaxed, 0.7),
        "blink": _blink(),
        "attack": _authored(skeleton, Attack),
        "cast": _authored(skeleton, Cast),
        "hurt": _authored(skeleton, Hurt),
        "die": die,
        **{name: rest for name in RestNames},
    }
