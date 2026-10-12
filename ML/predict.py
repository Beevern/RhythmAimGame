"""Chart a new song with the latest trained model.

    python predict.py "C:/path/to/song.mp4"
    python predict.py "C:/path/to/song.mp4" --run runs/20261011-120000 --threshold 0.4

Writes <video>_ml_notes.csv next to the video: the same format auto_chart.py and the
game use (a "Seconds" column plus a "Record In" timecode at the video's frame rate),
so it can go straight into the Level Maker or be compared in DaVinci Resolve.
"""
import argparse
import csv
import json
from pathlib import Path

import lightgbm as lgb
import numpy as np

import config
import dataset
from levels import Level, video_fps
from prepare import prepare_one
from train import pick


def latest_run() -> Path:
    runs = sorted(p for p in config.RUNS.iterdir() if (p / "picker.txt").exists())
    if not runs:
        raise SystemExit("No trained model yet: run train.py first.")
    return runs[-1]


def timecode(seconds: float, fps: float) -> str:
    frames = int(round(seconds * fps))
    secs, ff = divmod(frames, round(fps))
    mins, ss = divmod(secs, 60)
    hh, mm = divmod(mins, 60)
    return f"{hh + 1:02d}:{mm:02d}:{ss:02d}:{ff:02d}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("video", type=Path)
    ap.add_argument("--run", type=Path, help="model folder (default: newest in runs/)")
    ap.add_argument("--threshold", type=float, help="note threshold (default: the tuned one)")
    args = ap.parse_args()

    run = args.run or latest_run()
    report = json.loads((run / "report.json").read_text())
    picker = lgb.Booster(model_file=str(run / "picker.txt"))
    timing = lgb.Booster(model_file=str(run / "timing.txt"))
    threshold = args.threshold or report["threshold"]

    fps = video_fps(args.video)
    lvl = Level(name="new " + args.video.stem, folder=args.video.parent, video=args.video,
                video_fps=fps, chart_fps=fps, taps=[])
    print("preparing audio (stems take ~30 s on GPU) ...")
    prepare_one(args.video, lvl.key)

    df = dataset.build(lvl)
    cols = report["features"]
    prob = picker.predict(df[cols])
    idx = pick(df, prob, threshold)
    times = df["time"].to_numpy()[idx] + (timing.predict(df.iloc[idx][cols]) if len(idx) else 0)

    out = args.video.with_name(args.video.stem + "_ml_notes.csv")
    with open(out, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["#", "Word", "Seconds", "Probability", "", "", "", "", "", "", "Record In"])
        for n, (t, p) in enumerate(zip(times, prob[idx]), 1):
            w.writerow([n, "", f"{t:.3f}", f"{p:.2f}", "", "", "", "", "", "", timecode(t, fps)])
    print(f"{len(times)} notes -> {out}")


if __name__ == "__main__":
    main()
