#!/usr/bin/env python3
"""
Adds Pitch and Height columns to existing level charts, so the game places
higher-sung notes higher on screen.

Usage:
  python add_pitch.py                      # every level in Assets/StreamingAssets/Levels
  python add_pitch.py "path/to/Level"      # just these level folders
  python add_pitch.py --skip-existing      # leave charts that already have a Height column

For each level folder: extracts the audio from video.*, separates the vocals (Demucs),
measures the sung pitch at every note in notes.csv / held.csv, and writes the two
columns back (all other columns are kept as they were).
"""

import argparse
import csv
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import numpy as np
import torch

import auto_chart
from pitch import fmt, heights, note_pitch, pitch_track

TOOLS = Path(__file__).resolve().parent
DEFAULT_LEVELS = TOOLS.parent / "Assets" / "StreamingAssets" / "Levels"
VIDEO_EXTS = (".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv")


def find_ffmpeg() -> str:
    for c in (os.environ.get("FFMPEG"), str(TOOLS / "bin" / "ffmpeg.exe"), shutil.which("ffmpeg")):
        if c and Path(c).exists():
            return c
    sys.exit("FFmpeg not found (expected Tools/bin/ffmpeg.exe).")


def read_csv(path: Path):
    with path.open(encoding="utf-8-sig", newline="") as f:
        rows = [r for r in csv.reader(f) if any(c.strip() for c in r)]
    return rows[0], rows[1:]


def column(header, name):
    for i, h in enumerate(header):
        if h.strip().strip('"').lower() == name.lower():
            return i
    return -1


STANDARD_FPS = (24000 / 1001, 24, 25, 30000 / 1001, 30, 48, 50, 60000 / 1001, 60)


def note_times(header, rows, settings):
    """Each row's chart time in seconds: the Seconds column, or else Resolve's "Record In"
    timecode (timeline starting at 01:00:00:00), read the same way the game reads it."""
    ci = column(header, "Seconds")
    if ci >= 0:
        return [float(r[ci]) for r in rows]
    ti = column(header, "Record In")
    if ti < 0:
        return None
    fps = float(settings.get("chartFps", 0) or 30)
    fps = next((s for s in STANDARD_FPS if abs(fps - s) < 0.02), fps)
    nominal = round(fps)
    out = []
    for r in rows:
        hh, mm, ss, ff = (int(x) for x in r[ti].strip().strip('"').split(":"))
        out.append((((hh - 1) * 3600 + mm * 60 + ss) * nominal + ff) / fps)
    return out


def set_cell(row, i, value):
    while len(row) <= i:
        row.append("")
    row[i] = value


def process(folder: Path, ffmpeg: str, device: str, skip_existing: bool):
    notes_csv, held_csv = folder / "notes.csv", folder / "held.csv"
    video = next((p for p in folder.iterdir() if p.stem.lower() == "video" and p.suffix.lower() in VIDEO_EXTS), None)
    if not notes_csv.exists() or video is None:
        print(f"  skip: needs notes.csv and a video file")
        return

    settings = {}
    if (folder / "level.json").exists():
        settings = json.loads((folder / "level.json").read_text(encoding="utf-8"))

    charts = {}
    times_of = {}
    for path in (notes_csv, held_csv):
        if path.exists():
            header, rows = read_csv(path)
            times = note_times(header, rows, settings)
            if times is None:
                print(f"  skip: {path.name} has no Seconds or Record In column")
                return
            charts[path] = (header, rows)
            times_of[path] = times
    if skip_existing and all(column(h, "Height") >= 0 for h, _ in charts.values()):
        print("  already has Height, skipped")
        return

    #Chart time -> time in the video (the game applies offset, and holds the video back by videoDelay)
    shift = float(settings.get("offset", 0) or 0) - float(settings.get("videoDelay", 0) or 0)

    with tempfile.TemporaryDirectory() as tmp:
        wav = Path(tmp) / "song.wav"
        subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(video), "-vn", "-ac", "2", "-ar", "44100", str(wav)],
                       check=True)
        stems, sr = auto_chart.separate_stems(wav, device)
    track = pitch_track(stems["vocals"], sr)

    #Every note (taps, and the start row of each hold), measured together so Height is song-wide
    notes = []   #(path, row index(es), time, duration)
    if notes_csv in charts:
        times = times_of[notes_csv]
        for i, t in enumerate(times):
            nxt = times[i + 1] - t if i + 1 < len(times) else 0.3
            notes.append((notes_csv, [i], t, max(0.08, min(nxt, 0.3))))
    if held_csv in charts:
        times = times_of[held_csv]
        for i in range(0, len(times) - 1, 2):
            start, end = times[i], times[i + 1]
            notes.append((held_csv, [i, i + 1], start, end - start))

    pitches = [note_pitch(track, t + shift, d) for _, _, t, d in notes]
    hs = heights(pitches)

    for path, (header, rows) in charts.items():
        pi = column(header, "Pitch")
        if pi < 0:
            header.append("Pitch")
            pi = len(header) - 1
        hi = column(header, "Height")
        if hi < 0:
            header.append("Height")
            hi = len(header) - 1
        for (p, idx, _, _), pitch, h in zip(notes, pitches, hs):
            if p != path:
                continue
            for i in idx:
                set_cell(rows[i], pi, fmt(pitch, 2))
                set_cell(rows[i], hi, fmt(h, 3))
        with path.open("w", encoding="utf-8", newline="") as f:
            w = csv.writer(f)
            w.writerow(header)
            w.writerows(rows)

    pitched = sum(1 for p in pitches if not np.isnan(p))
    print(f"  {len(notes)} notes, {pitched} with a clear pitch ({pitched * 100 // max(1, len(notes))}%)")


def main():
    ap = argparse.ArgumentParser(description="Add Pitch/Height columns to level charts.")
    ap.add_argument("folders", nargs="*", type=Path, help="level folders (default: every built-in level)")
    ap.add_argument("--skip-existing", action="store_true", help="skip charts that already have Height")
    args = ap.parse_args()

    folders = args.folders or sorted(p for p in DEFAULT_LEVELS.iterdir() if p.is_dir())
    ffmpeg = find_ffmpeg()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    print(f"Device: {device}, {len(folders)} level(s)")
    for folder in folders:
        print(folder.name)
        try:
            process(folder, ffmpeg, device, args.skip_existing)
        except Exception as e:
            print(f"  failed: {e}")


if __name__ == "__main__":
    main()
