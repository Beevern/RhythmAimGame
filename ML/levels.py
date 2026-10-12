"""Finds levels and loads their human charts as times in the song's audio.

A note at "Seconds" s in notes.csv is hit at audio time  s + offset - videoDelay
(the game spawns notes on the level clock and starts the video after the countdown
plus videoDelay; see Spawner.cs / playMusic.cs).
"""
import csv
import hashlib
import json
import re
import shutil
import subprocess
from dataclasses import dataclass, field
from fractions import Fraction
from pathlib import Path

import config

VIDEO_EXTS = [".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv"]


@dataclass
class Level:
    name: str
    folder: Path
    video: Path
    video_fps: float               # frame rate of the level's video
    chart_fps: float               # timeline rate the chart's timecodes were placed at
    taps: list[float]              # audio times of tap notes
    holds: list[tuple[float, float]] = field(default_factory=list)
    tool_made: bool = False        # chart came from auto_chart.py (not a human): don't learn from it
    lyrics: Path | None = None

    @property
    def slug(self) -> str:
        return re.sub(r"[^A-Za-z0-9]+", "_", self.name).strip("_")

    @property
    def key(self) -> str:
        """Cache folder name: a fingerprint of the video, so renaming the level doesn't lose its cache."""
        h = hashlib.sha1(str(self.video.stat().st_size).encode())
        with open(self.video, "rb") as f:
            h.update(f.read(1 << 20))
        return h.hexdigest()[:12]

    @property
    def cache(self) -> Path:
        return config.CACHE / self.key

    def is_named(self, name: str) -> bool:
        """Same song as `name`, however the folder is worded
        ("Sad Song by We The Kings" == "We The Kings - Sad Song")."""
        return song_words(self.name) == song_words(name)

    @property
    def notes(self) -> list[float]:
        """Every moment a note starts (taps + hold starts), sorted."""
        return sorted(self.taps + [s for s, _ in self.holds])


def song_words(name: str) -> frozenset:
    return frozenset(w for w in re.findall(r"[a-z0-9]+", name.lower()) if w not in {"by", "ft", "feat", "and"})


def ffprobe() -> str | None:
    ff = shutil.which("ffmpeg")
    probe = shutil.which("ffprobe") or (str(Path(ff).with_name("ffprobe.exe")) if ff else None)
    return probe if probe and Path(probe).exists() else None


def video_fps(path: Path) -> float:
    probe = ffprobe()
    if not probe:
        return config.DEFAULT_FPS
    out = subprocess.run([probe, "-v", "error", "-select_streams", "v:0", "-show_entries",
                          "stream=r_frame_rate", "-of", "csv=p=0", str(path)],
                         capture_output=True, text=True).stdout.split()
    try:
        return float(Fraction(out[0]))
    except (IndexError, ValueError, ZeroDivisionError):
        return config.DEFAULT_FPS


def read_chart(path: Path, fps: float) -> tuple[list[float], bool]:
    """Times from a chart CSV (the "Seconds" column, else the "Record In" timecode),
    and whether it looks tool-made (auto_chart.py fills in Word/Aligned)."""
    rows = list(csv.reader(open(path, encoding="utf-8-sig")))
    if not rows:
        return [], False
    header = [h.strip().strip('"') for h in rows[0]]
    col = {h.lower(): i for i, h in enumerate(header)}
    sec, tc = col.get("seconds"), col.get("record in", 10)
    aligned = col.get("aligned")
    times, tool = [], False
    for r in rows[1:]:
        if aligned is not None and aligned < len(r) and r[aligned].strip():
            tool = True
        if sec is not None and sec < len(r) and r[sec].strip():
            times.append(float(r[sec]))
        elif tc < len(r) and r[tc].count(":") == 3:
            h, m, s, f = map(int, r[tc].split(":"))
            times.append((((h - 1) * 3600 + m * 60 + s) * round(fps) + f) / fps)
    return times, tool


def load_level(folder: Path) -> Level | None:
    notes = folder / "notes.csv"
    video = next((p for p in folder.iterdir() if p.stem.lower() == "video" and p.suffix.lower() in VIDEO_EXTS), None)
    if not notes.exists() or video is None:
        return None
    settings = {}
    if (folder / "level.json").exists():
        settings = json.loads((folder / "level.json").read_text(encoding="utf-8-sig") or "{}")
    vfps = video_fps(video)
    chart_fps = float(settings.get("chartFps") or 0) or config.DEFAULT_FPS
    shift = float(settings.get("offset", 0)) - float(settings.get("videoDelay", 0))

    taps, tool = read_chart(notes, chart_fps)
    held, tool_h = ([], False)
    if (folder / "held.csv").exists():
        held, tool_h = read_chart(folder / "held.csv", chart_fps)
    holds = [(held[i] + shift, held[i + 1] + shift) for i in range(0, len(held) - 1, 2)]
    lyrics = folder / "lyrics.txt"
    return Level(name=settings.get("name") or folder.name, folder=folder, video=video,
                 video_fps=vfps, chart_fps=chart_fps, taps=sorted(t + shift for t in taps),
                 holds=holds, tool_made=tool or tool_h, lyrics=lyrics if lyrics.exists() else None)


def all_levels(include_tool_made: bool = False) -> list[Level]:
    found = []
    for root in config.LEVEL_DIRS:
        for folder in sorted(p for p in root.iterdir() if p.is_dir()):
            lvl = load_level(folder)
            if lvl and (include_tool_made or not lvl.tool_made):
                found.append(lvl)
    return found


if __name__ == "__main__":
    for lvl in all_levels(include_tool_made=True):
        flag = "  (tool-made, skipped)" if lvl.tool_made else ""
        print(f"{lvl.video_fps:7.3f} fps  {len(lvl.taps):4d} taps  {len(lvl.holds):3d} holds  {lvl.name}{flag}")
