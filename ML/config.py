"""Paths and settings shared by every ML script."""
from pathlib import Path

ML_DIR = Path(__file__).resolve().parent
PROJECT = ML_DIR.parent

# Level folders to learn from (same layout the game uses: video + notes.csv [+ held.csv, level.json])
LEVEL_DIRS = [PROJECT / "Assets" / "StreamingAssets" / "Levels"]

CACHE = ML_DIR / "cache"        # extracted audio, stems and features per song (git-ignored)
RUNS = ML_DIR / "runs"          # trained models, predictions and reports (git-ignored)
SPLITS = ML_DIR / "splits.json"

# Audio analysis
SR = 22050                      # stems are stored at this rate (mono)
HOP = 64                        # feature resolution: 64 / 22050 = 2.9 ms
STEMS = ["vocals", "drums", "bass", "other"]

# Matching a predicted note to a human note
MATCH_WINDOW = 0.05             # seconds; the usual onset-detection tolerance

# The game's default chart frame rate (LevelHolder.ChartFps), for "exact frame" scoring
DEFAULT_FPS = 30.0
