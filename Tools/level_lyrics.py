#!/usr/bin/env python3
"""
Write lyrics.json (lyrics + song name shown in game, see LyricsDisplay.cs) into level folders.

auto_chart.py already makes <name>_lyrics.json for new levels; this is for levels that
were charted another way. Each folder needs its video and a lyrics.txt. The song name and
artist come from the level name ("Song Name by Artist Name").

Usage:
  python level_lyrics.py "path/to/Levels/Some Song"          # one level
  python level_lyrics.py "path/to/Levels"/*                  # every level with a lyrics.txt
  python level_lyrics.py --style box --words 3 ...           # highlight style / words per line
  python level_lyrics.py --force ...                         # redo existing lyrics.json
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import auto_chart

VIDEO_EXTS = [".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv"]


def level_name(folder: Path) -> str:
    j = folder / "level.json"
    if j.exists():
        try:
            name = json.loads(j.read_text(encoding="utf-8-sig") or "{}").get("name")
            if name:
                return name
        except ValueError:
            pass
    return folder.name


def split_name(name: str) -> tuple[str, str]:
    """"Song Name by Artist Name" -> ("Song Name", "Artist Name")."""
    m = re.match(r"(.*\S)\s+by\s+(\S.*)$", name, flags=re.IGNORECASE)
    return (m.group(1), m.group(2)) if m else (name, "")


def main():
    ap = argparse.ArgumentParser(description="Write lyrics.json into level folders.")
    ap.add_argument("folders", nargs="+", type=Path)
    ap.add_argument("--style", choices=["fill", "box", "plain"], default="fill")
    ap.add_argument("--words", type=int, default=0, help="words per line (0 = whole lyric lines)")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    ffmpeg = os.environ.get("FFMPEG") or shutil.which("ffmpeg")
    if not ffmpeg:
        sys.exit("FFmpeg not found.")

    for folder in args.folders:
        if not folder.is_dir():
            continue
        out = folder / "lyrics.json"
        video = next((p for p in folder.iterdir() if p.stem.lower() == "video" and p.suffix.lower() in VIDEO_EXTS), None)
        lyrics = folder / "lyrics.txt"
        if video is None or not lyrics.exists():
            print(f"skip (needs video + lyrics.txt)  {folder.name}")
            continue
        if out.exists() and not args.force:
            print(f"skip (has lyrics.json)          {folder.name}")
            continue
        title, artist = split_name(level_name(folder))
        with tempfile.TemporaryDirectory() as tmp:
            wav = Path(tmp) / "song.wav"
            subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(video), "-vn",
                            "-ac", "2", "-ar", "44100", str(wav)], check=True)
            notes = Path(tmp) / "song_notes.csv"
            try:
                auto_chart.main([str(wav), str(lyrics), "-o", str(notes), "--words", str(args.words),
                                 "--karaoke", args.style, "--no-video", "--title", title, "--artist", artist])
            except SystemExit as e:      # e.g. lyrics.txt has no lyrics yet: skip this one, keep going
                print(f"skip ({e})  {folder.name}")
                continue
            shutil.copy(Path(tmp) / "song_lyrics.json", out)
        print(f"wrote lyrics.json  {folder.name}  ({title} / {artist})", flush=True)


if __name__ == "__main__":
    main()
