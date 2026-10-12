"""Candidate notes + features + labels for one song.

Model v1 works on *candidates*: every moment any stem (vocals, drums, bass, other
instruments, or the full mix) has a sound onset. That covers lyrics and instrumental
parts alike. Each candidate gets a row of numbers describing the sound around it,
and two labels from the human chart:
  y       1 if a human put a note here (the nearest candidate within 50 ms of a note)
  offset  human note time minus candidate time (what the timing model learns)
"""
import json

import librosa
import numpy as np
import pandas as pd

import config
from levels import Level

FRAME = config.SR / config.HOP          # feature frames per second (~344)
SOURCES = config.STEMS + ["mix"]
CONTEXT = list(range(-16, 17, 4))       # envelope samples around a candidate (+-46 ms)


def label_fixes() -> dict:
    p = config.ML_DIR / "label_fixes.json"
    return json.loads(p.read_text()) if p.exists() else {}


def human_notes(lvl: Level, fixes: dict | None = None) -> np.ndarray:
    """The human's note times, with any drift fix from check_labels.py applied."""
    scale = next((v.get("scale", 1.0) for k, v in (fixes or {}).items() if lvl.is_named(k)), 1.0)
    return np.array(lvl.notes) * scale


def load_features(lvl: Level) -> dict:
    return dict(np.load(lvl.cache / "features.npz"))


def normalised(f: dict) -> dict[str, np.ndarray]:
    return {s: np.clip(f[f"onset_{s}"] / (np.percentile(f[f"onset_{s}"], 99) + 1e-9), 0, 2) for s in SOURCES}


def candidate_frames(env: dict[str, np.ndarray]) -> np.ndarray:
    """Onset peaks from every source, merged when closer than ~12 ms."""
    peaks = []
    for s in SOURCES:
        peaks.append(librosa.util.peak_pick(env[s], pre_max=6, post_max=6, pre_avg=30,
                                            post_avg=30, delta=0.04, wait=6))
    frames = np.unique(np.concatenate(peaks))
    strength = np.max([env[s] for s in SOURCES], axis=0)
    merged = []
    for fr in frames:
        if merged and fr - merged[-1] <= 4:
            if strength[fr] > strength[merged[-1]]:
                merged[-1] = fr
        else:
            merged.append(fr)
    return np.array(merged, dtype=int)


def build(lvl: Level, fixes: dict | None = None) -> pd.DataFrame:
    f = load_features(lvl)
    env = normalised(f)
    n = len(env["mix"])
    cand = candidate_frames(env)
    t = cand / FRAME
    rows = {"song": lvl.name, "frame": cand, "time": t, "pos": t / (n / FRAME)}

    def at(arr, frames):
        return arr[np.clip(frames, 0, len(arr) - 1)]

    peaks = {s: set(librosa.util.peak_pick(env[s], pre_max=6, post_max=6, pre_avg=30, post_avg=30,
                                            delta=0.04, wait=6)) for s in SOURCES}
    for s in SOURCES:
        e, db = env[s], f[f"db_{s}"]
        rows[f"on_{s}"] = at(e, cand)
        rows[f"onmax_{s}"] = np.array([e[max(0, c - 8):c + 9].max() for c in cand])
        rows[f"peak_{s}"] = np.array([any(c + k in peaks[s] for k in range(-4, 5)) for c in cand], dtype=np.int8)
        rows[f"db_{s}"] = at(db, cand) - db.max()
        rows[f"rise_{s}"] = at(db, cand + 8) - at(db, cand - 8)
    for s in ("mix", "vocals", "drums"):
        for k in CONTEXT:
            rows[f"ctx_{s}_{k}"] = at(env[s], cand + k)

    # Rhythm: where the candidate sits relative to the detected beat grid
    beats = f["beats"]
    if len(beats) >= 2:
        i = np.clip(np.searchsorted(beats, t), 1, len(beats) - 1)
        prev, nxt = beats[i - 1], beats[i]
        rows["beat_phase"] = np.clip((t - prev) / np.maximum(nxt - prev, 1e-3), 0, 1)
        rows["beat_dist"] = np.minimum(np.abs(t - prev), np.abs(nxt - t))
    else:
        rows["beat_phase"] = rows["beat_dist"] = np.zeros_like(t)
    rows["tempo"] = np.full_like(t, float(f["tempo"][0]))

    # Neighbourhood: how busy this part of the song is
    rows["dt_prev"] = np.diff(t, prepend=t[0] - 1.0)
    rows["dt_next"] = np.diff(t, append=t[-1] + 1.0)
    rows["density"] = np.searchsorted(t, t + 1.0) - np.searchsorted(t, t - 1.0)
    rows["vocal_active"] = (rows["db_vocals"] > -35).astype(np.int8)

    df = pd.DataFrame(rows)

    # Labels: each human note claims its nearest candidate within the match window
    notes = human_notes(lvl, fixes)
    df["y"] = 0
    df["offset"] = np.nan
    taken = set()
    for note in notes:
        j = int(np.searchsorted(t, note))
        options = [k for k in (j - 1, j) if 0 <= k < len(t) and k not in taken]
        if not options:
            continue
        k = min(options, key=lambda k: abs(t[k] - note))
        if abs(t[k] - note) <= config.MATCH_WINDOW:
            taken.add(k)
            df.loc[k, "y"] = 1
            df.loc[k, "offset"] = note - t[k]
    return df


def feature_columns(df: pd.DataFrame) -> list[str]:
    return [c for c in df.columns if c not in ("song", "frame", "time", "y", "offset")]
