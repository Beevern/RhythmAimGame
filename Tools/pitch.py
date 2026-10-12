"""
Pitch of each note, so the game can place higher-sung notes higher on screen.

Used by auto_chart.py (new charts) and add_pitch.py (existing level folders).

  Pitch  = MIDI note number of the sung pitch at the note (60 = middle C, 72 = an octave up),
           blank when the note isn't clearly pitched (spoken, whispered, silent).
  Height = 0..1 within the song: 0 = among its lowest notes, 1 = among its highest
           (5th..95th percentile, so a few outliers don't squash everything else).
"""

import numpy as np
import librosa

PITCH_SR = 16000
HOP = 160                 # 10 ms frames
FMIN, FMAX = 70.0, 1000.0 # roughly the range of a singing voice


def pitch_track(vocals: np.ndarray, sr: int):
    """(frame times, f0 in Hz, voiced mask) for an isolated vocal stem (mono or stereo)."""
    mono = librosa.to_mono(vocals) if vocals.ndim > 1 else vocals
    y = librosa.resample(mono.astype(np.float32), orig_sr=sr, target_sr=PITCH_SR)
    f0 = librosa.yin(y, fmin=FMIN, fmax=FMAX, sr=PITCH_SR, frame_length=1024, hop_length=HOP)
    rms = librosa.feature.rms(y=y, frame_length=1024, hop_length=HOP)[0]
    n = min(len(f0), len(rms))
    f0, rms = f0[:n], rms[:n]
    # Count a frame as sung when it's within 30 dB of the loudest part of the vocal
    db = librosa.amplitude_to_db(rms, ref=np.max)
    voiced = db > -30.0
    times = librosa.frames_to_time(np.arange(n), sr=PITCH_SR, hop_length=HOP)
    return times, f0, voiced


def note_pitch(track, start: float, duration: float) -> float:
    """MIDI pitch sung at a note (median over its first ~0.3 s), or NaN if unclear."""
    times, f0, voiced = track
    length = min(max(duration, 0.08), 0.3)
    mask = (times >= start) & (times < start + length) & voiced
    vals = f0[mask]
    if len(vals) < 3:
        return float("nan")
    return float(librosa.hz_to_midi(np.median(vals)))


def heights(pitches) -> list:
    """0..1 per note within the song; NaN stays NaN (the game then picks a random height)."""
    p = np.asarray(pitches, dtype=float)
    ok = ~np.isnan(p)
    out = np.full(len(p), np.nan)
    if ok.sum() < 2:
        return out.tolist()
    lo, hi = np.percentile(p[ok], [5, 95])
    if hi - lo < 1.0:            # basically monotone: put everything in the middle
        out[ok] = 0.5
    else:
        out[ok] = np.clip((p[ok] - lo) / (hi - lo), 0.0, 1.0)
    return out.tolist()


def fmt(v: float, digits: int) -> str:
    """CSV cell for a value that may be NaN (blank)."""
    return "" if v is None or np.isnan(v) else f"{v:.{digits}f}"
