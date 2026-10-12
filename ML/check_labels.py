"""Step 2: check every chart lines up with its audio before learning from it.

A chart made on a 29.97 fps timeline but read as 30 (or 23.976 read as 24) drifts:
notes slide 0.1% further off the music every second, up to ~0.2 s by the end of a
song. That's poison for exact-frame training, so for each song this finds the time
scale (1, 1.001 or 1/1.001) and offset that best line the notes up with sound
onsets, and writes label_fixes.json. Datasets apply the scale (the offset is the
human's own habit, which is exactly what the model should learn, so it's only reported).

    python check_labels.py
"""
import json

import numpy as np

import config
from levels import all_levels

SCALES = {"1": 1.0, "x1.001 (29.97/23.976 read as 30/24)": 1.001, "/1.001": 1 / 1.001}


def onset_strength_curve(f) -> np.ndarray:
    """All stems' onsets on one scale: each normalised by its own 99th percentile."""
    curves = []
    for s in config.STEMS:
        e = f[f"onset_{s}"]
        curves.append(np.clip(e / (np.percentile(e, 99) + 1e-9), 0, 1))
    return np.max(curves, axis=0)


def fit(times: np.ndarray, curve: np.ndarray) -> tuple[float, float, float]:
    """Best (scale, offset, score): score = mean onset strength right at the notes."""
    frame = config.SR / config.HOP
    # Strongest onset within +-2 frames (6 ms) of a position, so the fit isn't knife-edge
    local = np.max([np.roll(curve, k) for k in range(-2, 3)], axis=0)
    best = (1.0, 0.0, -1.0)
    for scale in SCALES.values():
        for off in np.arange(-0.15, 0.15 + 1e-9, 1 / frame):
            idx = np.round((times * scale + off) * frame).astype(int)
            idx = idx[(idx >= 0) & (idx < len(local))]
            s = float(local[idx].mean()) if len(idx) else 0.0
            if s > best[2]:
                best = (scale, float(off), s)
    return best


def main():
    fixes = {}
    print(f"{'song':48s} {'scale':>8s} {'offset':>8s} {'score':>6s} {'(no fix)':>8s}")
    for lvl in all_levels():
        path = lvl.cache / "features.npz"
        if not path.exists():
            print(f"{lvl.name[:48]:48s}  not prepared yet (run prepare.py)")
            continue
        curve = onset_strength_curve(np.load(path))
        t = np.array(lvl.notes)
        scale, off, s = fit(t, curve)
        # Same fit with the scale held at 1, to see how much the scale fix buys
        base = max(fit_fixed(t, curve, 1.0))
        if scale != 1.0 and s < base * 1.03:
            scale = 1.0          # not clearly better: leave the chart alone
        fixes[lvl.name] = {"scale": scale, "offset_ms": round(off * 1000, 1),
                           "score": round(s, 3), "score_unscaled": round(base, 3)}
        mark = "  <- drift fixed" if scale != 1.0 else ""
        print(f"{lvl.name[:48]:48s} {scale:8.4f} {off * 1000:+7.0f}ms {s:6.3f} {base:8.3f}{mark}")
    (config.ML_DIR / "label_fixes.json").write_text(json.dumps(fixes, indent=2))
    print("\nWrote label_fixes.json. Offsets are how early/late you chart vs the sound "
          "(+ = notes before the onset); the model learns that, so it isn't removed.")


def fit_fixed(times, curve, scale):
    frame = config.SR / config.HOP
    local = np.max([np.roll(curve, k) for k in range(-2, 3)], axis=0)
    for off in np.arange(-0.15, 0.15 + 1e-9, 1 / frame):
        idx = np.round((times * scale + off) * frame).astype(int)
        idx = idx[(idx >= 0) & (idx < len(local))]
        yield float(local[idx].mean()) if len(idx) else 0.0


if __name__ == "__main__":
    main()
