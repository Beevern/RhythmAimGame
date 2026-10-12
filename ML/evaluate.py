"""Scoring predicted notes against a human chart.

Two questions, answered separately:
  1. Did we put notes where the human did?   precision / recall / F1, a predicted note
     counts if it's within MATCH_WINDOW (50 ms) of a human note (each matched once).
  2. How exact is the timing of those notes? on the song's own video frames:
     exact frame = same frame as the human, +-1 frame = at most one frame off.
"""
import numpy as np
import mir_eval

import config


def score(pred: list[float], ref: list[float], fps: float) -> dict:
    pred, ref = np.sort(np.asarray(pred, float)), np.sort(np.asarray(ref, float))
    pairs = mir_eval.util.match_events(ref, pred, config.MATCH_WINDOW) if len(pred) and len(ref) else []
    hits = len(pairs)
    p = hits / len(pred) if len(pred) else 0.0
    r = hits / len(ref) if len(ref) else 0.0
    out = {"notes": len(ref), "predicted": len(pred), "precision": p, "recall": r,
           "f1": 2 * p * r / (p + r) if p + r else 0.0}
    if hits:
        i, j = np.array(pairs).T
        err = pred[j] - ref[i]
        frame_diff = np.abs(np.round(pred[j] * fps) - np.round(ref[i] * fps))
        out.update({
            "exact_frame": float(np.mean(frame_diff == 0)),
            "within_1_frame": float(np.mean(frame_diff <= 1)),
            "median_abs_ms": float(np.median(np.abs(err)) * 1000),
            "bias_ms": float(np.mean(err) * 1000),   # + = predicted later than the human
        })
    return out


def summarize(rows: dict[str, dict]) -> dict:
    """Totals over songs: hit counts are pooled, timing stats are note-weighted."""
    tot = {k: sum(r[k] for r in rows.values()) for k in ("notes", "predicted")}
    hits = sum(r["recall"] * r["notes"] for r in rows.values())
    p, r = hits / max(tot["predicted"], 1), hits / max(tot["notes"], 1)
    out = {**tot, "precision": p, "recall": r, "f1": 2 * p * r / (p + r) if p + r else 0.0}
    for k in ("exact_frame", "within_1_frame", "median_abs_ms", "bias_ms"):
        w = [(v[k], v["recall"] * v["notes"]) for v in rows.values() if k in v]
        if w:
            out[k] = sum(a * b for a, b in w) / max(sum(b for _, b in w), 1)
    return out


def print_table(rows: dict[str, dict], title: str):
    print(f"\n{title}")
    print(f"{'song':44s} {'F1':>5s} {'prec':>5s} {'rec':>5s} {'exact':>6s} {'±1fr':>6s} {'|err|':>6s} {'bias':>6s}")
    for name, r in list(rows.items()) + [("ALL", summarize(rows))]:
        print(f"{name[:44]:44s} {r['f1']:5.2f} {r['precision']:5.2f} {r['recall']:5.2f} "
              f"{r.get('exact_frame', 0):6.0%} {r.get('within_1_frame', 0):6.0%} "
              f"{r.get('median_abs_ms', 0):5.0f}ms {r.get('bias_ms', 0):+5.0f}ms")
