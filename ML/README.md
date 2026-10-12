# Auto-charting with machine learning

Goal: given a song's video, place notes where a human charter would, on the exact frame,
for lyrics **and** instrumental parts.

## How it works (model v1)

1. **Candidates**: every moment any part of the song has a sound onset. Demucs splits the
   soundtrack into vocals, drums, bass and other instruments, and each part's onsets are
   candidates. That's how instrumental notes are covered.
2. **Picker**: a LightGBM model looks at the sound around each candidate (onset strength
   and loudness per part, where it sits on the beat, how busy that section is) and gives
   the probability that a human charted a note there.
3. **Timing**: a second LightGBM model learns how far the human's note sits from the onset
   (people tend to chart a little before or after the sound) and nudges each note,
   aiming for the human's exact frame.

Training data is every level in `Assets/StreamingAssets/Levels` whose chart a human made.
Tool-made charts (from `auto_chart.py`) are skipped automatically, because learning from
them would only teach the model the tool's mistakes.

## Run it

Use the Python in `Tools/.venv` (it already has PyTorch, Demucs and librosa):

```
cd ML
..\Tools\.venv\Scripts\python -m pip install -r requirements.txt   # once
..\Tools\.venv\Scripts\python prepare.py        # 1. audio + stems + features (~30 s per new song, cached)
..\Tools\.venv\Scripts\python check_labels.py   # 2. find charts that drift against their audio
..\Tools\.venv\Scripts\python train.py          # 3. train, score on the val songs
..\Tools\.venv\Scripts\python train.py --test   #    ...and on the locked test songs
..\Tools\.venv\Scripts\python predict.py "C:\path\song.mp4"   # chart a new song
```

## Reading the scores

| Column | Meaning |
|---|---|
| **F1** | Overall placement score, 0 to 1. A predicted note counts if it's within 50 ms of a human note. |
| **prec** | Of the notes the model placed, the share a human also placed. Low means extra notes. |
| **rec** | Of the human's notes, the share the model found. Low means missed notes. |
| **exact** | Of the matched notes, the share on *exactly* the human's frame (song's own frame rate). |
| **±1fr** | The share at most one frame off. |
| **bias** | Average timing error. + means the model is later than the human. |

Each report shows three rows of tables:
- **BASELINE**: vocal onsets with no learning, roughly what `auto_chart.py` does without lyrics.
- **MODEL v1**: the trained model.
- **CEILING**: the best score possible if the model picked every right candidate, before
  any timing fix. If the ceiling's recall is low, the candidates are missing notes, and that's
  what to improve first.

## Files

| File | What it does |
|---|---|
| `config.py` | Paths and settings |
| `levels.py` | Finds levels, reads charts (Seconds or timecode at the right frame rate), converts to audio time using `offset`/`videoDelay` |
| `prepare.py` | Extracts the video's audio, runs Demucs, computes features into `cache/` |
| `check_labels.py` | Detects charts made on a 29.97/23.976 timeline but read as 30/24 (they drift 0.1%), writes `label_fixes.json` |
| `dataset.py` | Builds candidates, their features, and labels from the human chart |
| `train.py` | Trains the picker and timing models, tunes the threshold on val, reports, saves to `runs/` |
| `evaluate.py` | The scoring above |
| `predict.py` | Charts a new video with the latest model |
| `splits.json` | Which songs are train / val / test (split by song, never by note) |

`cache/` and `runs/` are git-ignored.

## Getting better: what to try next

1. **More human charts.** This matters more than anything else. Every song you chart by hand
   and save in a level folder becomes training data. Re-run `prepare.py` then `train.py`.
2. **Measure your own consistency.** Chart one song twice, a few days apart, and score one
   chart against the other with `evaluate.score`. That's the realistic ceiling for "exact frame":
   the model can't agree with you more often than you agree with yourself.
3. **Tap or hold.** Train a third model that decides tap vs. hold (and the hold's length) for
   each picked note. The held.csv files already hold the labels.
4. **Lyrics as a feature.** Add "a lyric word was aligned near here" from `auto_chart.py`'s
   alignment as a feature, so the picker knows which vocal onsets are words.
5. **Neural network (v2).** Once there are many more charts: a small CNN on spectrogram frames
   that predicts "note here?" for every 3 ms slice (see the paper *Dance Dance Convolution*,
   Donahue et al. 2017). Pretraining on osu! beatmaps (a huge public set of human-timed charts)
   and then fine-tuning on your levels is a common way around having few charts.
6. **Plug it into the game.** Give `auto_chart.py` a mode that uses the model, so the Level
   Maker can chart instrumental songs and sections.
