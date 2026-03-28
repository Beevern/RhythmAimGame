#!/usr/bin/env python3
"""
Convert v0sakkhi7k-style transcript JSON to a per-word EDL.

Usage:
  python json_to_edl.py input.json output.edl

Assumptions:
- JSON has top-level key "results" with entries that include:
    - type == "word"
    - start_time (seconds, float)
    - alternatives[0].content (word text)
- Timecode:
    - 30 fps
    - timeline starts at 01:00:00:00
- One EDL event per word, 1-frame duration
- Marker line format like DaVinci Resolve:
    |C:ResolveColorBlue |M:<word> |D:1
"""

import json
import sys
from pathlib import Path

FPS = 30
START_TC = (1, 0, 0, 0)  # 01:00:00:00
REEL = "001"
TRACK = "V"
TRANSITION = "C"
COLOR = "ResolveColorBlue"


def tc_to_total_frames(h, m, s, f, fps=FPS):
    return ((h * 3600 + m * 60 + s) * fps) + f


def total_frames_to_tc(total_frames, fps=FPS):
    secs, frames = divmod(total_frames, fps)
    hours, rem = divmod(secs, 3600)
    minutes, seconds = divmod(rem, 60)
    return f"{hours:02d}:{minutes:02d}:{seconds:02d}:{frames:02d}"


START_OFFSET_FRAMES = tc_to_total_frames(*START_TC, fps=FPS)


def seconds_to_tc(seconds, fps=FPS, offset_frames=START_OFFSET_FRAMES):
    total_frames = int(round(seconds * fps)) + offset_frames
    return total_frames_to_tc(total_frames, fps=fps)


def json_to_edl(input_path: Path, output_path: Path, title: str = "Bad transcript markers"):
    with input_path.open("r", encoding="utf-8") as f:
        data = json.load(f)

    results = data.get("results", [])
    lines = []
    lines.append(f"TITLE: {title}")
    lines.append("FCM: NON-DROP FRAME")
    lines.append("")

    event_idx = 1

    for item in results:
        if item.get("type") != "word":
            continue

        alts = item.get("alternatives") or []
        if not alts:
            continue
        word = alts[0].get("content", "").strip()
        if not word:
            continue

        start_seconds = item.get("start_time")
        if start_seconds is None:
            continue

        in_tc = seconds_to_tc(start_seconds)
        # 1-frame duration
        h, m, s, f = map(int, in_tc.split(":"))
        in_frames = tc_to_total_frames(h, m, s, f, fps=FPS)
        out_frames = in_frames + 1
        out_tc = total_frames_to_tc(out_frames, fps=FPS)

        event_no = f"{event_idx:03d}"
        edl_line = (
            f"{event_no}  {REEL}      {TRACK}     {TRANSITION}        "
            f"{in_tc} {out_tc} {in_tc} {out_tc}  "
        )
        marker_line = f" |C:{COLOR} |M:{word} |D:1"

        lines.append(edl_line)
        lines.append(marker_line)
        lines.append("")

        event_idx += 1

    with output_path.open("w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))


def main(argv=None):
    if argv is None:
        argv = sys.argv[1:]

    if len(argv) != 2:
        print("Usage: python json_to_edl.py input.json output.edl")
        sys.exit(1)

    in_path = Path(argv[0])
    out_path = Path(argv[1])

    if not in_path.is_file():
        print(f"Input file not found: {in_path}")
        sys.exit(1)

    json_to_edl(in_path, out_path)
    print(f"EDL written to {out_path}")


if __name__ == "__main__":
    main()