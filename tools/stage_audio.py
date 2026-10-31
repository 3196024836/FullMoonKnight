"""按素材映射表整理模组音效（assets/mod/audio，不进 git）。由 stage_assets.py 调用。

映射表 data/mod-assets.json 的 "audio"：{"attack": "sword_queen", ...}，来源为 assets/original/audio/<名>.ogg。
处理：峰值统一到 -3 dB，超过 MaxSeconds 的截断并在末尾淡出，重新编码为 OGG。需要本机 ffmpeg。
"""

import json
import re
import subprocess
from pathlib import Path

RepoDir = Path(__file__).resolve().parent.parent  # 仓库根目录
SourceDir = RepoDir / "assets" / "original" / "audio"  # 原作音效
OutputDir = RepoDir / "assets" / "mod" / "audio"  # 输出目录
MapPath = RepoDir / "data" / "mod-assets.json"  # 映射表
TargetPeak = -3.0  # 目标峰值（dB）
MaxSeconds = 2.0  # 最长时长
FadeSeconds = 0.3  # 截断时的淡出时长


def peak(path: Path) -> float:
    """音频峰值（dB）。"""
    output = subprocess.run(["ffmpeg", "-hide_banner", "-i", str(path), "-af", "volumedetect", "-f", "null", "-"],
                            capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stderr
    return float(re.search(r"max_volume: (-?[\d.]+) dB", output).group(1))


def duration(path: Path) -> float:
    """音频时长（秒）。"""
    output = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)],
                            capture_output=True, text=True, check=True).stdout
    return float(output)


def stage(name: str, source: str) -> None:
    """整理一段音效。"""
    path = SourceDir / f"{source}.ogg"
    filters = [f"volume={TargetPeak - peak(path):.2f}dB"]
    if duration(path) > MaxSeconds:
        filters.append(f"afade=t=out:st={MaxSeconds - FadeSeconds}:d={FadeSeconds}")
        filters.append(f"atrim=0:{MaxSeconds}")
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(path), "-af", ",".join(filters),
                    "-c:a", "libvorbis", "-q:a", "5", str(OutputDir / f"{name}.ogg")], check=True)


def main() -> None:
    """整理全部音效。"""
    mapping = json.loads(MapPath.read_text(encoding="utf-8")).get("audio", {})
    OutputDir.mkdir(parents=True, exist_ok=True)
    for old in OutputDir.glob("*.ogg"):
        old.unlink()
    for name, source in mapping.items():
        stage(name, source)
    print(f"staged {len(mapping)} sounds into {OutputDir}")


if __name__ == "__main__":
    main()
