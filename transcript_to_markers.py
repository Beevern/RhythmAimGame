#!/usr/bin/env python3
import json
import csv
import argparse
from pathlib import Path

def seconds_to_tc(seconds: float, fps: int = 24) -> str:
    h = int(seconds // 3600)
    m = int((seconds % 3600) // 60)
    s = int(seconds % 60)
    f = int(round((seconds - int(seconds)) * fps))
    if f >= fps:
        f = 0
        s += 1
        if s >= 60:
            s = 0
            m += 1
            if m >= 60:
                m = 0
                h += 1
    return f"{h:02d}:{m:02d}:{s:02d}:{f:02d}"

def convert(input_json: Path, output_csv: Path, fps: int = 24, offset: float = 0.0):
    with input_json.open("r", encoding="utf-8") as f:
        data = json.load(f)

    chunks = data.get("chunks", [])
    fieldnames = [
        "No","Name","Tracks","Timecode In","Timecode Out",
        "Tape","Start","End","Channels","Loc TC","Loc Name","Loc Color"
    ]

    with output_csv.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()

        idx = 1
        for ch in chunks:
            text = (ch.get("text") or "").strip()
            ts = ch.get("timestamp") or []
            if not text or len(ts) < 1:
                continue

            start_sec = float(ts[0]) + offset
            if start_sec < 0:
                continue

            tc = seconds_to_tc(start_sec, fps=fps)

            row = {
                "No": idx,
                "Name": input_json.stem,
                "Tracks": "V",
                "Timecode In": tc,
                "Timecode Out": tc,
                "Tape": "AX",
                "Start": tc,
                "End": tc,
                "Channels": "",
                "Loc TC": tc,
                "Loc Name": text,
                "Loc Color": ""
            }
            writer.writerow(row)
            idx += 1

def main():
    parser = argparse.ArgumentParser(
        description="Convert transcript.json (with chunks + timestamp) to preset EDL CSV for Resolve markers."
    )
    parser.add_argument("input", help="Input transcript.json path")
    parser.add_argument("-o", "--output", help="Output CSV path")
    parser.add_argument("--fps", type=int, default=24, help="Frame rate (default: 24)")
    parser.add_argument("--offset", type=float, default=0.0,
                        help="Time offset in seconds to add to all markers (negative to shift earlier)")
    args = parser.parse_args()

    input_path = Path(args.input)
    if args.output:
        output_path = Path(args.output)
    else:
        output_path = input_path.with_suffix(".markers.csv")

    convert(input_path, output_path, fps=args.fps, offset=args.offset)
    print(f"Written: {output_path}")

if __name__ == "__main__":
    main()