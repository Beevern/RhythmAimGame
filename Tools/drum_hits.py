#!/usr/bin/env python3
"""
Write drums.csv (drum hits, for in-game drum animations) into level folders.

auto_chart.py already makes <name>_drums.csv for new levels; this is for levels that
were charted another way. For each level folder it takes the video's soundtrack,
separates the drums with Demucs and writes drums.csv next to notes.csv.

Usage:
  python drum_hits.py "path/to/Levels/Some Song"            # one level
  python drum_hits.py "path/to/Levels"/*                    # every level
  python drum_hits.py --force ...                           # redo existing drums.csv
"""
import argparse
import subprocess
import sys
import tempfile
from pathlib import Path

import torch

from auto_chart import drum_hits, separate_stems, write_drums_csv

VIDEO_EXTS = [".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv"]


def main():
    import os
    import shutil
    ap = argparse.ArgumentParser(description="Write drums.csv into level folders.")
    ap.add_argument("folders", nargs="+", type=Path)
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    ffmpeg = os.environ.get("FFMPEG") or shutil.which("ffmpeg")
    if not ffmpeg:
        sys.exit("FFmpeg not found.")
    device = "cuda" if torch.cuda.is_available() else "cpu"

    for folder in args.folders:
        if not folder.is_dir():
            continue
        out = folder / "drums.csv"
        video = next((p for p in folder.iterdir() if p.stem.lower() == "video" and p.suffix.lower() in VIDEO_EXTS), None)
        if video is None:
            print(f"skip (no video)   {folder.name}")
            continue
        if out.exists() and not args.force:
            print(f"skip (has drums)  {folder.name}")
            continue
        with tempfile.TemporaryDirectory() as tmp:
            wav = Path(tmp) / "song.wav"
            subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(video), "-vn",
                            "-ac", "2", "-ar", "44100", str(wav)], check=True)
            stems, sr = separate_stems(wav, device)
        hits = drum_hits(stems["drums"], sr)
        write_drums_csv(out, hits)
        print(f"{len(hits):5d} hits      {folder.name}", flush=True)


if __name__ == "__main__":
    main()
