"""Step 1: turn every level into cached audio data.

For each level (cached, so re-runs only do new songs):
  cache/<song>/mix.flac              the video's soundtrack (what the player hears)
  cache/<song>/<stem>.flac           Demucs stems: vocals, drums, bass, other
  cache/<song>/features.npz          onset strength + loudness per stem, beats

The soundtrack comes from the video, not audio.mp3: MP3 encoding adds a short silence
at the start, which would shift every label by up to a frame.

    python prepare.py            # all levels
    python prepare.py --force    # redo everything
"""
import argparse
import shutil
import subprocess
import sys
from pathlib import Path

import librosa
import numpy as np
import soundfile as sf
import torch

import config
from levels import all_levels


def extract_audio(video: Path, out: Path, sr: int = 44100):
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        sys.exit("ffmpeg not found on PATH")
    subprocess.run([ffmpeg, "-y", "-loglevel", "error", "-i", str(video), "-vn",
                    "-ac", "2", "-ar", str(sr), str(out)], check=True)


def separate(wav: Path, device: str, model) -> dict[str, np.ndarray]:
    """Demucs htdemucs: all four stems, each mono at config.SR."""
    from demucs.apply import apply_model
    y, sr = librosa.load(str(wav), sr=model.samplerate, mono=False)
    if y.ndim == 1:
        y = np.stack([y, y])
    mix = torch.from_numpy(y).float()
    ref = mix.mean(0)
    mean, std = ref.mean(), ref.std() + 1e-8
    with torch.no_grad():
        out = apply_model(model, ((mix - mean) / std)[None], device=device, split=True,
                          overlap=0.25, progress=False)[0] * std + mean
    stems = {}
    for i, name in enumerate(model.sources):
        mono = out[i].mean(0).cpu().numpy()
        stems[name] = librosa.resample(mono, orig_sr=model.samplerate, target_sr=config.SR)
    return stems


def features(stems: dict[str, np.ndarray], mix: np.ndarray) -> dict[str, np.ndarray]:
    """Frame-level features at config.HOP resolution."""
    f = {}
    for name, y in list(stems.items()) + [("mix", mix)]:
        env = librosa.onset.onset_strength(y=y, sr=config.SR, hop_length=config.HOP, n_fft=1024)
        f[f"onset_{name}"] = env.astype(np.float32)
        rms = librosa.feature.rms(y=y, frame_length=1024, hop_length=config.HOP)[0]
        f[f"db_{name}"] = librosa.amplitude_to_db(rms, ref=1.0).astype(np.float32)
    tempo, beats = librosa.beat.beat_track(y=mix, sr=config.SR, hop_length=512, units="time")
    f["beats"] = np.asarray(beats, dtype=np.float32)
    f["tempo"] = np.atleast_1d(np.asarray(tempo, dtype=np.float32))
    n = min(len(v) for k, v in f.items() if k.startswith(("onset_", "db_")))
    for k in list(f):
        if k.startswith(("onset_", "db_")):
            f[k] = f[k][:n]
    return f


_model = None


def prepare_one(video: Path, slug: str, force: bool = False) -> Path:
    """Audio, stems and features for one video into cache/<slug>/. Returns that folder."""
    global _model
    d = config.CACHE / slug
    if (d / "features.npz").exists() and not force:
        return d
    if _model is None:
        from demucs.pretrained import get_model
        _model = get_model("htdemucs").eval()
    device = "cuda" if torch.cuda.is_available() else "cpu"
    d.mkdir(parents=True, exist_ok=True)
    wav = d / "mix_44k.wav"
    extract_audio(video, wav)
    stems = separate(wav, device, _model)
    mix = librosa.load(str(wav), sr=config.SR, mono=True)[0]
    sf.write(d / "mix.flac", mix, config.SR)
    for name, y in stems.items():
        sf.write(d / f"{name}.flac", np.clip(y, -1, 1), config.SR)
    np.savez_compressed(d / "features.npz", **features(stems, mix))
    wav.unlink()
    return d


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    for lvl in all_levels(include_tool_made=True):
        if (lvl.cache / "features.npz").exists() and not args.force:
            print(f"cached   {lvl.name}")
            continue
        print(f"preparing {lvl.name} ...", flush=True)
        prepare_one(lvl.video, lvl.key, args.force)
    print("done")


if __name__ == "__main__":
    main()
