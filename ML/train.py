"""Step 3: train model v1 and score it against the human charts.

Two small gradient-boosted tree models (LightGBM):
  picker  - which candidates become notes (probability a human charted it)
  timing  - how far to nudge each chosen note to land on the human's exact frame

Songs are split train / val / test (splits.json). The note threshold is tuned on val.
Test is only scored with --test, so it stays an honest final check.

Also scores two reference points, so you know what "better" means:
  baseline  vocal onsets only, roughly what auto_chart.py does without lyrics
  ceiling   the best any candidate-based model could do (every right candidate, perfect picks)

    python train.py            # train, report on val
    python train.py --test     # also report the locked test songs
"""
import argparse
import json
import time

import lightgbm as lgb
import numpy as np
import pandas as pd

import config
import dataset
from evaluate import print_table, score, summarize
from levels import all_levels

MIN_GAP = 0.10   # two predicted notes can't be closer than this (seconds)


def load_split(levels):
    s = json.loads(config.SPLITS.read_text())
    by = {"test": [], "val": [], "train": []}
    for lvl in levels:
        key = ("test" if any(lvl.is_named(n) for n in s["test"]) else
               "val" if any(lvl.is_named(n) for n in s["val"]) else "train")
        by[key].append(lvl)
    return by


def pick(df: pd.DataFrame, prob: np.ndarray, threshold: float) -> np.ndarray:
    """Candidates above the threshold, strongest first, skipping any too close to one already picked."""
    order = np.argsort(-prob)
    chosen, times = [], []
    t = df["time"].to_numpy()
    for i in order:
        if prob[i] < threshold:
            break
        if all(abs(t[i] - u) >= MIN_GAP for u in times[-200:]):
            chosen.append(i)
            times.append(t[i])
    return np.sort(np.array(chosen, dtype=int))


def baseline(df: pd.DataFrame) -> np.ndarray:
    """Vocal onsets only (no learning): strong vocal peaks, at least 0.12 s apart."""
    strong = (df["peak_vocals"] == 1) & (df["on_vocals"] > 0.25) & (df["vocal_active"] == 1)
    t, out = df["time"].to_numpy(), []
    for i in np.where(strong)[0]:
        if not out or t[i] - t[out[-1]] >= 0.12:
            out.append(i)
    return t[out]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--test", action="store_true", help="also score the locked test songs")
    args = ap.parse_args()

    fixes = dataset.label_fixes()
    if not fixes:
        print("(no label_fixes.json yet: run check_labels.py first for drift-corrected labels)")
    levels = [l for l in all_levels() if (l.cache / "features.npz").exists()]
    split = load_split(levels)
    print({k: len(v) for k, v in split.items()}, "songs")

    tables = {l.name: dataset.build(l, fixes) for l in levels}
    fps = {l.name: l.video_fps for l in levels}
    notes = {l.name: dataset.human_notes(l, fixes) for l in levels}
    train = pd.concat([tables[l.name] for l in split["train"]], ignore_index=True)
    cols = dataset.feature_columns(train)
    covered = train["y"].sum() / sum(len(notes[l.name]) for l in split["train"])
    print(f"{len(train)} training candidates, {int(train['y'].sum())} are human notes "
          f"({covered:.0%} of human notes have a candidate within 50 ms)")

    picker = lgb.LGBMClassifier(n_estimators=500, learning_rate=0.03, num_leaves=31,
                                min_child_samples=20, subsample=0.8, subsample_freq=1,
                                colsample_bytree=0.8, verbose=-1)
    picker.fit(train[cols], train["y"])
    pos = train[train["y"] == 1]
    timing = lgb.LGBMRegressor(n_estimators=400, learning_rate=0.03, num_leaves=15,
                               min_child_samples=20, verbose=-1)
    timing.fit(pos[cols], pos["offset"])

    def predict(name, threshold):
        df = tables[name]
        prob = picker.predict_proba(df[cols])[:, 1]
        idx = pick(df, prob, threshold)
        return df["time"].to_numpy()[idx] + timing.predict(df.iloc[idx][cols]) if len(idx) else np.array([])

    # Threshold that gives the best F1 on the validation songs
    best = (0.5, -1)
    for th in np.arange(0.15, 0.8, 0.025):
        f1 = summarize({l.name: score(predict(l.name, th), notes[l.name], fps[l.name])
                        for l in split["val"]})["f1"]
        if f1 > best[1]:
            best = (th, f1)
    threshold = best[0]
    print(f"note threshold {threshold:.3f} (best F1 on val)")

    groups = ["val"] + (["test"] if args.test else [])
    report = {}
    for g in groups:
        model_rows = {l.name: score(predict(l.name, threshold), notes[l.name], fps[l.name]) for l in split[g]}
        base_rows = {l.name: score(baseline(tables[l.name]), notes[l.name], fps[l.name]) for l in split[g]}
        ceil_rows = {}
        for l in split[g]:
            df = tables[l.name]
            hit = df[df["y"] == 1]
            ceil_rows[l.name] = score(hit["time"].to_numpy(), notes[l.name], fps[l.name])
        print_table(base_rows, f"[{g}] BASELINE: vocal onsets, no learning")
        print_table(model_rows, f"[{g}] MODEL v1")
        print_table(ceil_rows, f"[{g}] CEILING: perfect picks among candidates, no timing fix")
        report[g] = {"model": summarize(model_rows), "baseline": summarize(base_rows),
                     "ceiling": summarize(ceil_rows)}

    # What the picker pays attention to
    imp = sorted(zip(picker.feature_importances_, cols), reverse=True)[:12]
    print("\nmost-used features:", ", ".join(c for _, c in imp))

    run = config.RUNS / time.strftime("%Y%m%d-%H%M%S")
    run.mkdir(parents=True, exist_ok=True)
    picker.booster_.save_model(str(run / "picker.txt"))
    timing.booster_.save_model(str(run / "timing.txt"))
    (run / "report.json").write_text(json.dumps({"threshold": threshold, "features": cols, **report}, indent=2))
    print(f"\nsaved to {run}")


if __name__ == "__main__":
    main()
