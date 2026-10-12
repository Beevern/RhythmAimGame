using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using V = LevelEditorView;

//Level Editor scene: load any level, add/delete/move notes, rename it, replace its video or cover,
//watch it play back with the notes on top, then Save.
//
//The UI lives in LevelEditorView (saved into the scene with Tools > Rebuild Level Editor UI, or
//built here at Play if it isn't there). This script is the behaviour.
//
//Times in the note list are chart times (what's in the CSVs). The preview applies the level's
//offset and video delay, so it matches what the game shows.
//
//Shortcuts: Space/K play-pause, J/L back/forward 2s, N add tap, H add hold, Delete remove,
//Left/Right nudge 10ms (Shift: 50ms), Up/Down select previous/next note, Ctrl+Z undo, Ctrl+S save.
public class LevelEditor : MonoBehaviour
{
    const float ApproachTime = 3f;          //approach disc grows for 3s before the note (as in game)

    V view;

    //Level data
    EditableLevel level;
    EditNote selected;
    bool dirty;
    readonly List<List<EditNote>> undo = new List<List<EditNote>>();

    //Video
    VideoPlayer video;
    RenderTexture videoTexture;
    double duration = 60;
    float playhead;
    float lastTickTime;
    bool halfSpeed;
    bool hitSounds = true;
    AudioSource sfx;

    //Runtime-only pieces
    int listTop;
    readonly List<Image> ticks = new List<Image>();
    readonly List<NoteSprite> notePool = new List<NoteSprite>();
    readonly List<ZoomMarker> zoomPool = new List<ZoomMarker>();
    float statusUntil;
    string pendingDiscard;
    float pendingDiscardUntil;
    EditNote dragging;
    float dragOffset;
    bool dragMoved;
    //Per-note results from the hit log (every finished run of this level)
    Dictionary<long, HitLog.NoteStats> log = new Dictionary<long, HitLog.NoteStats>();

    class NoteSprite { public RectTransform root; public Image fill, rim, sel; }
    class ZoomMarker { public RectTransform bar; public Image img; public TextMeshProUGUI label; }

    //---------------------------------------------------------------- setup

    void Start()
    {
        EnsureSceneBasics();
        LevelHolder.ReloadAll();

        view = FindFirstObjectByType<LevelEditorView>(FindObjectsInactive.Include);
        if (view == null) view = LevelEditorView.Build();
        view.gameObject.SetActive(true);
        sfx = Sfx.Source(view.gameObject);

        CreateVideoPlayer();
        Wire();
        view.videoImage.texture = Texture2D.blackTexture;
        view.coverImage.texture = Texture2D.blackTexture;
        RebuildLevelList();

        //Open the song that was last selected, or the first one
        string first = SongSelect2.theSong != null && LevelHolder.levels.ContainsKey(SongSelect2.theSong)
            ? SongSelect2.theSong : (LevelHolder.allSongs.Length > 0 ? LevelHolder.allSongs[0] : null);
        if (first != null) OpenLevel(LevelHolder.levels[first].folder);
        else SetStatus("No levels found.", Theme.Miss);
    }

    //The scene may only hold the UI; add a camera, audio listener and event system if missing
    static void EnsureSceneBasics()
    {
        if (FindFirstObjectByType<Camera>() == null)
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Background;
        }
        if (FindFirstObjectByType<AudioListener>() == null)
            FindFirstObjectByType<Camera>().gameObject.AddComponent<AudioListener>();
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CreateVideoPlayer()
    {
        var go = new GameObject("VideoPlayer");
        go.transform.SetParent(transform, false);
        video = go.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.renderMode = VideoRenderMode.RenderTexture;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        video.source = VideoSource.Url;
        video.prepareCompleted += OnVideoPrepared;
        video.errorReceived += (vp, msg) => SetStatus("Video error: " + msg, Theme.Miss, 6f);
    }

    //Hooks every button, field and timeline in the view up to the editor
    void Wire()
    {
        view.back.onClick.AddListener(() => Guarded("leave", () => SceneManager.LoadScene("Menu")));
        view.save.onClick.AddListener(Save);
        //UIs saved into the scene before this button existed get it added here
        if (view.deleteLevel == null)
        {
            view.deleteLevel = V.MakeDeleteButton(view.save.transform.parent);
            RectTransform st = view.statusText.rectTransform;
            st.anchoredPosition = new Vector2(600, st.anchoredPosition.y);
            st.sizeDelta = new Vector2(890, st.sizeDelta.y);
        }
        view.deleteLevel.onClick.AddListener(DeleteLevel);

        view.play.onClick.AddListener(TogglePlay);
        view.speed.onClick.AddListener(() =>
        {
            halfSpeed = !halfSpeed;
            video.playbackSpeed = halfSpeed ? 0.5f : 1f;
            view.speedLabel.text = halfSpeed ? "0.5x SPEED" : "1x SPEED";
        });
        view.sound.onClick.AddListener(() =>
        {
            hitSounds = !hitSounds;
            view.soundLabel.text = hitSounds ? "HIT SOUNDS ON" : "HIT SOUNDS OFF";
        });

        view.fullTimelineInput.onPress = (x, down, drag) => Seek(x / V.MidW * (float)duration);
        view.zoomInput.onPress = ZoomPress;

        view.timeField.onEndEdit.AddListener(t =>
        {
            if (selected == null || !TryFloat(t, out float v)) { RefreshSelected(); return; }
            PushUndo();
            float len = selected.end - selected.time;
            selected.time = Mathf.Max(0f, v);
            if (selected.hold) selected.end = selected.time + len;
            level.Sort();
            MarkDirty(true);
        });
        for (int i = 0; i < view.nudge.Length && i < V.NudgeSteps.Length; i++)
        {
            float d = V.NudgeSteps[i];
            view.nudge[i].onClick.AddListener(() => Nudge(d));
        }
        view.snap.onClick.AddListener(SnapToPlayhead);
        view.addTap.onClick.AddListener(() => AddNote(false));
        view.addHold.onClick.AddListener(() => AddNote(true));
        view.endField.onEndEdit.AddListener(t =>
        {
            if (selected == null || !selected.hold || !TryFloat(t, out float v)) { RefreshSelected(); return; }
            PushUndo();
            selected.end = Mathf.Max(selected.time + 0.05f, v);
            MarkDirty(true);
        });
        view.labelField.onEndEdit.AddListener(t =>
        {
            if (selected == null) return;
            PushUndo();
            selected.label = t.Trim();
            MarkDirty(true);
        });
        view.delete.onClick.AddListener(DeleteSelected);
        view.undo.onClick.AddListener(Undo);

        view.nameField.onEndEdit.AddListener(t =>
        {
            if (level == null || t.Trim() == level.name) return;
            level.name = t.Trim();
            MarkDirty(true);
        });
        view.replaceCover.onClick.AddListener(() => StartCoroutine(PickFile("Select Cover Image",
            new FileBrowser.Filter("Images", ".png", ".jpg", ".jpeg"), path =>
            {
                level.newCoverSource = path;
                LoadCover();
                MarkDirty(true);
            })));
        view.replaceVideo.onClick.AddListener(() => StartCoroutine(PickFile("Select Video",
            new FileBrowser.Filter("Videos", ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv"), path =>
            {
                level.newVideoSource = path;
                LoadVideo();
                MarkDirty(true);
            })));
        view.offsetField.onEndEdit.AddListener(t =>
        {
            if (level == null || !TryFloat(t, out float v)) { view.offsetField.text = level != null ? Fmt(level.offset) : ""; return; }
            level.offset = v;
            MarkDirty(true);
        });
        //Older saved UIs may not have these yet (Tools > Rebuild Level Editor UI adds them)
        if (view.volumeField != null)
        {
            view.volumeField.onEndEdit.AddListener(t =>
            {
                if (level == null || !TryFloat(t, out float v)) { RefreshLevelSettings(); return; }
                level.volume = Mathf.Clamp(v, 0f, 200f) / 100f;
                ApplyPreviewVolume();
                MarkDirty(true);
                RefreshLevelSettings();
            });
        }
        if (view.audioBars != null)
        {
            view.audioBars.onClick.AddListener(() =>
            {
                if (level == null) return;
                level.hideAudioBars = !level.hideAudioBars;
                MarkDirty(true);
                RefreshLevelSettings();
            });
        }

        for (int i = 0; i < view.rowButton.Length; i++)
        {
            int row = i;
            view.rowButton[i].onClick.AddListener(() => ClickRow(row));
        }
        view.listScroll.onValueChanged.AddListener(v => { listTop = Mathf.RoundToInt(v); RefreshList(); });
    }

    //---------------------------------------------------------------- level list (left)

    void RebuildLevelList()
    {
        RectTransform content = view.levelListContent;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
        const float rowH = 58f, gap = 6f;
        string[] songs = LevelHolder.allSongs;
        content.sizeDelta = new Vector2(0, songs.Length * (rowH + gap));
        for (int i = 0; i < songs.Length; i++)
        {
            LevelInfo info = LevelHolder.levels[songs[i]];
            bool current = level != null && string.Equals(info.folder, level.folder, StringComparison.OrdinalIgnoreCase);
            Image bg = V.Box(content, 0, i * (rowH + gap), 380, rowH, current ? Theme.SurfaceHover : Theme.Surface, Theme.RoundedRect);
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.6f;
            bg.raycastTarget = true;
            if (current) V.Box(bg.transform, 0, 0, 4, rowH, Theme.Accent, Theme.Square);
            var b = bg.gameObject.AddComponent<Button>();
            b.colors = V.Colors(current ? Theme.SurfaceHover : Theme.Surface, Theme.SurfaceHover);
            string folder = info.folder;
            b.onClick.AddListener(() => Guarded("open", () => OpenLevel(folder)));
            LevelNames.Split(info.name, out string artist, out string title);
            V.Label(bg.transform, title, 14, 6, 300, 26, 19, true, Theme.Text, TextAlignmentOptions.Left).overflowMode = TextOverflowModes.Ellipsis;
            V.Label(bg.transform, artist, 14, 30, 300, 22, 15, false, Theme.TextMuted, TextAlignmentOptions.Left).overflowMode = TextOverflowModes.Ellipsis;
            V.Label(bg.transform, "★" + info.difficulty.ToString("0.0"), 300, 0, 70, rowH, 17, true, Difficulty.ColorFor(info.difficulty), TextAlignmentOptions.Right);
        }
    }

    //Asks for a second click before throwing away unsaved changes
    void Guarded(string key, Action action)
    {
        if (dirty && !(pendingDiscard == key && Time.unscaledTime < pendingDiscardUntil))
        {
            pendingDiscard = key;
            pendingDiscardUntil = Time.unscaledTime + 3f;
            SetStatus("Unsaved changes! Click again to discard them, or press SAVE.", Theme.Good, 3f);
            return;
        }
        pendingDiscard = null;
        action();
    }

    //---------------------------------------------------------------- video

    void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
        videoTexture = new RenderTexture(Mathf.Max(2, (int)vp.width), Mathf.Max(2, (int)vp.height), 0);
        videoTexture.Create();
        vp.targetTexture = videoTexture;
        view.videoImage.texture = videoTexture;
        duration = Math.Max(1.0, vp.length);
        //Fit inside the preview, keeping the video's shape
        float s = Mathf.Min(V.MidW / vp.width, V.PreviewH / vp.height);
        RectTransform rt = view.videoImage.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(vp.width * s, vp.height * s);
        ApplyPreviewVolume();
        Seek(playhead);
        RefreshTimelineTicks();
    }

    void ApplyPreviewVolume()
    {
        if (video != null && video.isPrepared)
            video.SetDirectAudioVolume(0, PlayerPrefs.GetFloat("volume", 0.5f) * (level != null ? level.volume : 1f));
    }

    void RefreshLevelSettings()
    {
        if (level == null) return;
        if (view.volumeField != null) view.volumeField.SetTextWithoutNotify(Mathf.RoundToInt(level.volume * 100f).ToString());
        if (view.audioBarsLabel != null) view.audioBarsLabel.text = level.hideAudioBars ? "AUDIO BARS OFF" : "AUDIO BARS ON";
    }

    void TogglePlay()
    {
        if (string.IsNullOrEmpty(video.url) || !video.isPrepared) return;
        if (video.isPlaying)
        {
            video.Pause();
            playhead = (float)video.time;
        }
        else
        {
            lastTickTime = playhead;
            video.Play();
        }
    }

    void Seek(float t)
    {
        playhead = Mathf.Clamp(t, 0f, (float)duration);
        lastTickTime = playhead;
        if (video.isPrepared) video.time = playhead;
    }

    //---------------------------------------------------------------- timelines

    void ZoomPress(float x, bool down, bool drag)
    {
        float t = ZoomTimeAt(x);
        if (down)
        {
            dragging = null;
            //Pick the closest note marker within 10px
            EditNote best = null;
            float bestPx = 10f;
            if (level != null)
            {
                foreach (EditNote n in level.notes)
                {
                    float px = Mathf.Abs(ZoomX(PreviewTime(n.time)) - x);
                    if (px < bestPx) { bestPx = px; best = n; }
                }
            }
            if (best != null)
            {
                Select(best, false);
                PushUndo();
                dragging = best;
                dragMoved = false;
                dragOffset = PreviewTime(best.time) - t;
            }
            else Seek(t);
        }
        else if (drag && dragging != null)
        {
            float newTime = Mathf.Max(0f, ChartTime(t + dragOffset));
            float len = dragging.end - dragging.time;
            dragging.time = (float)Math.Round(newTime, 3);
            if (dragging.hold) dragging.end = dragging.time + len;
            dragMoved = true;
            MarkDirty(false);
        }
        else if (!down && !drag && dragging != null)
        {
            dragging = null;
            if (dragMoved)
            {
                level.Sort();
                MarkDirty(true);
            }
            else if (undo.Count > 0)
            {
                undo.RemoveAt(undo.Count - 1);   //just a click to select: nothing to undo
            }
        }
        else if (drag) Seek(t);
    }

    float ZoomStart => playhead - V.ZoomBefore;
    float ZoomTimeAt(float x) => ZoomStart + x / V.MidW * (V.ZoomBefore + V.ZoomAfter);
    float ZoomX(float t) => (t - ZoomStart) / (V.ZoomBefore + V.ZoomAfter) * V.MidW;

    //Chart time <-> time in the video (the game applies offset, and holds the video back by videoDelay)
    float PreviewTime(float chart) => chart + (level?.offset ?? 0f) - (level?.videoDelay ?? 0f);
    float ChartTime(float videoTime) => videoTime - (level?.offset ?? 0f) + (level?.videoDelay ?? 0f);

    void RefreshTimelineTicks()
    {
        int n = level?.notes.Count ?? 0;
        while (ticks.Count < n) ticks.Add(V.Box(view.fullTimeline, 0, 0, 2, 44, Color.white, Theme.Square));
        for (int i = 0; i < ticks.Count; i++)
        {
            bool on = i < n;
            ticks[i].gameObject.SetActive(on);
            if (!on) continue;
            EditNote note = level.notes[i];
            float x = PreviewTime(note.time) / (float)duration * V.MidW;
            ticks[i].rectTransform.anchoredPosition = new Vector2(x, note.hold ? -14 : 0);
            ticks[i].rectTransform.sizeDelta = new Vector2(note == selected ? 3 : 1.5f, note.hold ? 16 : 44);
            ticks[i].color = note == selected ? Color.white : (note.hold ? Theme.HeldNote : Theme.UpcomingNote).WithAlpha(0.7f);
        }
        view.playheadLine.SetAsLastSibling();
    }

    //---------------------------------------------------------------- file picking

    IEnumerator PickFile(string title, FileBrowser.Filter filter, Action<string> onPicked)
    {
        if (level == null) yield break;
        FileBrowser.SetFilters(true, filter);
        FileBrowser.SetDefaultFilter(filter.extensions[0]);
        if (!FileBrowser.ShowLoadDialog(null, null, FileBrowser.PickMode.Files, false, FileSelection.DownloadsFolder(), null, title, "Select"))
            yield break;
        //Make sure the browser draws above the editor
        FileBrowser browser = FindFirstObjectByType<FileBrowser>();
        Canvas bc = browser != null ? browser.GetComponentInParent<Canvas>() : null;
        if (bc != null) bc.sortingOrder = 500;
        while (FileBrowser.IsOpen) yield return null;
        if (FileBrowser.Success) onPicked(FileBrowser.Result[0]);
    }

    //---------------------------------------------------------------- note list (right)

    void ClickRow(int row)
    {
        int i = listTop + row;
        if (level == null || i >= level.notes.Count) return;
        Select(level.notes[i], true);
    }

    void RefreshList()
    {
        int n = level?.notes.Count ?? 0;
        int maxTop = Mathf.Max(0, n - V.ListRows);
        listTop = Mathf.Clamp(listTop, 0, maxTop);
        view.listScroll.maxValue = Mathf.Max(1, maxTop);
        view.listScroll.SetValueWithoutNotify(listTop);
        view.listScroll.interactable = maxTop > 0;

        for (int r = 0; r < view.rowBg.Length; r++)
        {
            int i = listTop + r;
            bool on = i < n;
            view.rowBg[r].gameObject.SetActive(on);
            if (!on) continue;
            EditNote note = level.notes[i];
            bool sel = note == selected;
            view.rowBg[r].color = sel ? Theme.Accent.WithAlpha(0.35f) : (i % 2 == 0 ? Color.white.WithAlpha(0.03f) : Color.clear);
            view.rowIdx[r].text = (i + 1).ToString();
            view.rowTime[r].text = Fmt(note.time);
            float gap = i + 1 < n ? level.notes[i + 1].time - note.time : -1f;
            view.rowDelta[r].text = gap >= 0f ? Fmt(gap) : "—";
            //Very close notes are easy to miss when charting: flag them
            view.rowDelta[r].color = gap >= 0f && gap < 0.15f ? Theme.Miss : Theme.Text;
            view.rowType[r].text = note.hold ? "HOLD " + (note.end - note.time).ToString("0.0") + "s" : "TAP";
            view.rowType[r].color = note.hold ? Theme.HeldNote : Theme.Text;
            view.rowWord[r].text = note.label;
            if (view.rowLog != null && r < view.rowLog.Length)
            {
                HitLog.NoteStats st = HitLog.Find(log, note.time);
                if (st == null) { view.rowLog[r].text = "—"; view.rowLog[r].color = Theme.TextMuted; }
                else
                {
                    view.rowLog[r].text = Mathf.RoundToInt(st.MissRate * 100f) + "%";
                    view.rowLog[r].color = st.MissRate >= 0.3f ? Theme.Miss : st.EarlyRate >= 0.3f ? Theme.Early : Theme.Text;
                }
            }
        }
    }

    //Keeps the selected note's row on screen
    void ScrollToSelected()
    {
        if (level == null || selected == null) return;
        int i = level.notes.IndexOf(selected);
        if (i < listTop) listTop = i;
        else if (i >= listTop + V.ListRows) listTop = i - V.ListRows + 1;
    }

    //---------------------------------------------------------------- loading

    void OpenLevel(string folder)
    {
        level = EditableLevel.Load(folder);
        selected = null;
        undo.Clear();
        dirty = false;
        playhead = 0f;
        listTop = 0;
        view.nameField.SetTextWithoutNotify(level.name);
        view.offsetField.SetTextWithoutNotify(Fmt(level.offset));
        log = HitLog.Load(level.name);
        RefreshLevelSettings();
        LoadCover();
        LoadVideo();
        RebuildLevelList();
        RefreshAll();
        SetStatus("Opened " + level.name, Theme.TextMuted);
    }

    void LoadVideo()
    {
        video.Stop();
        view.videoImage.texture = Texture2D.blackTexture;
        string path = level.PreviewVideo;
        view.videoNameText.text = path != null ? (level.newVideoSource != null ? "New video: " : "Video: ") + Path.GetFileName(path) : "No video";
        if (path == null)
        {
            video.url = "";
            duration = level.notes.Count > 0 ? level.notes[level.notes.Count - 1].time + 5 : 60;
            return;
        }
        video.url = path;
        video.controlledAudioTrackCount = 1;
        video.EnableAudioTrack(0, true);
        video.Prepare();
    }

    void LoadCover()
    {
        string path = level.PreviewCover;
        view.coverImage.color = Color.white;
        if (path == null || !File.Exists(path)) { view.coverImage.texture = Texture2D.blackTexture; return; }
        var tex = new Texture2D(2, 2);
        if (tex.LoadImage(File.ReadAllBytes(path))) view.coverImage.texture = tex;
    }

    //---------------------------------------------------------------- editing

    void Select(EditNote note, bool seek)
    {
        selected = note;
        ShowNoteLog(note);
        if (seek && note != null) Seek(PreviewTime(note.time) - 1.5f);
        ScrollToSelected();
        RefreshSelected();
        RefreshList();
        RefreshTimelineTicks();
    }

    //Full hit-log history of a note in the status line
    void ShowNoteLog(EditNote note)
    {
        if (note == null) return;
        HitLog.NoteStats s = HitLog.Find(log, note.time);
        if (s == null) { SetStatus("No plays logged for this note yet.", Theme.TextMuted, 4f); return; }
        string r = s.AvgReactionMs > 0 ? $" · avg reaction {Mathf.RoundToInt(s.AvgReactionMs)} ms" : "";
        SetStatus($"Played {s.plays}×: {s.perfect} perfect, {s.great} great, {s.good} good, {s.hit} hit, {s.early} early, {s.miss} missed{r}",
            s.MissRate >= 0.3f ? Theme.Miss : Theme.Text, 8f);
    }

    void RefreshSelected()
    {
        bool has = selected != null && level != null;
        view.selectedText.text = has ? $"Note #{level.notes.IndexOf(selected) + 1}  ·  {(selected.hold ? "HOLD" : "TAP")}" : "No note selected";
        view.timeField.SetTextWithoutNotify(has ? Fmt(selected.time) : "");
        view.endField.SetTextWithoutNotify(has && selected.hold ? Fmt(selected.end) : "");
        view.endField.interactable = has && selected.hold;
        view.labelField.SetTextWithoutNotify(has ? selected.label : "");
    }

    void AddNote(bool hold)
    {
        if (level == null) return;
        PushUndo();
        float t = (float)Math.Round(Mathf.Max(0f, ChartTime(playhead)), 3);
        var note = new EditNote { time = t, hold = hold, end = t + 1f };
        level.notes.Add(note);
        level.Sort();
        selected = note;
        ScrollToSelected();
        MarkDirty(true);
    }

    void DeleteSelected()
    {
        if (level == null || selected == null) return;
        PushUndo();
        int i = level.notes.IndexOf(selected);
        level.notes.Remove(selected);
        selected = level.notes.Count > 0 ? level.notes[Mathf.Clamp(i, 0, level.notes.Count - 1)] : null;
        MarkDirty(true);
    }

    void Nudge(float delta)
    {
        if (selected == null) return;
        PushUndo();
        selected.time = (float)Math.Round(Mathf.Max(0f, selected.time + delta), 3);
        if (selected.hold) selected.end = (float)Math.Round(selected.end + delta, 3);
        level.Sort();
        MarkDirty(true);
    }

    void SnapToPlayhead()
    {
        if (selected == null) return;
        PushUndo();
        float len = selected.end - selected.time;
        selected.time = (float)Math.Round(Mathf.Max(0f, ChartTime(playhead)), 3);
        if (selected.hold) selected.end = selected.time + len;
        level.Sort();
        MarkDirty(true);
    }

    void SelectNeighbour(int dir)
    {
        if (level == null || level.notes.Count == 0) return;
        int i = selected != null ? level.notes.IndexOf(selected) + dir : 0;
        Select(level.notes[Mathf.Clamp(i, 0, level.notes.Count - 1)], true);
    }

    void PushUndo()
    {
        if (level == null) return;
        var snap = new List<EditNote>();
        foreach (EditNote n in level.notes) snap.Add(n.Clone());
        undo.Add(snap);
        if (undo.Count > 200) undo.RemoveAt(0);
    }

    void Undo()
    {
        if (level == null || undo.Count == 0) return;
        float selTime = selected?.time ?? -1f;
        level.notes.Clear();
        level.notes.AddRange(undo[undo.Count - 1]);
        undo.RemoveAt(undo.Count - 1);
        selected = level.notes.Find(n => Mathf.Abs(n.time - selTime) < 0.0005f);
        MarkDirty(true);
        SetStatus("Undone", Theme.TextMuted, 1.5f);
    }

    void MarkDirty(bool full)
    {
        dirty = true;
        if (full)
        {
            RefreshAll();
        }
        else
        {
            RefreshTimelineTicks();
            RefreshSelected();
        }
    }

    void RefreshAll()
    {
        RefreshList();
        RefreshSelected();
        RefreshTimelineTicks();
        if (level != null)
        {
            float d = level.Difficulty();
            view.statsText.text = $"{level.TapCount} taps · {level.HoldCount} holds · <color=#{ColorUtility.ToHtmlStringRGB(Difficulty.ColorFor(d))}>★ {d:0.0}</color>";
        }
    }

    //---------------------------------------------------------------- deleting

    float deleteArmedUntil;
    string deleteArmedFor;

    //Permanently deletes the open level's folder (needs a second click within 3 seconds)
    void DeleteLevel()
    {
        if (level == null) return;
        if (deleteArmedFor != level.folder || Time.unscaledTime > deleteArmedUntil)
        {
            deleteArmedFor = level.folder;
            deleteArmedUntil = Time.unscaledTime + 3f;
            SetStatus("Click DELETE LEVEL again to permanently delete \"" + level.name + "\".", Theme.Miss, 3f);
            return;
        }
        deleteArmedFor = null;

        string folder = level.folder, name = level.name;
        //Release the video file before removing its folder
        video.Stop();
        video.url = "";
        try
        {
            Directory.Delete(folder, true);
            //Unity keeps a .meta next to every folder inside Assets
            if (File.Exists(folder + ".meta")) File.Delete(folder + ".meta");
        }
        catch (Exception e)
        {
            SetStatus("Couldn't delete the level: " + e.Message, Theme.Miss, 6f);
            LoadVideo();
            return;
        }
        CustomLevelManager.RemoveFromRegistry(name);
        string hitLog = HitLog.FileFor(name);
        if (File.Exists(hitLog)) File.Delete(hitLog);
        if (SongSelect2.theSong == name) SongSelect2.theSong = null;

        level = null;
        selected = null;
        dirty = false;
        undo.Clear();
        LevelHolder.ReloadAll();
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        if (LevelHolder.allSongs.Length > 0) OpenLevel(LevelHolder.levels[LevelHolder.allSongs[0]].folder);
        else
        {
            RebuildLevelList();
            RefreshAll();
            view.videoImage.texture = Texture2D.blackTexture;
            view.coverImage.texture = Texture2D.blackTexture;
        }
        SetStatus("Deleted \"" + name + "\".", Theme.Good, 4f);
    }

    //---------------------------------------------------------------- saving

    void Save()
    {
        if (level == null) return;
        string oldName = level.originalName;
        //Release the video file (it may be replaced or its folder renamed)
        bool replacingFiles = level.newVideoSource != null || level.name != level.originalName;
        if (replacingFiles)
        {
            video.Stop();
            video.url = "";
        }
        string error = level.Save();
        if (error != null)
        {
            SetStatus(error, Theme.Miss, 6f);
            if (replacingFiles) LoadVideo();
            return;
        }
        dirty = false;
        float keepPlayhead = playhead;
        float keepSelected = selected?.time ?? -1f;
        LevelHolder.ReloadAll();
        if (SongSelect2.theSong == oldName) SongSelect2.theSong = level.name;
        //Reload from disk so what you see is exactly what was saved
        string folder = level.folder;
        level = EditableLevel.Load(folder);
        selected = level.notes.Find(n => Mathf.Abs(n.time - keepSelected) < 0.0005f);
        view.nameField.SetTextWithoutNotify(level.name);
        view.offsetField.SetTextWithoutNotify(Fmt(level.offset));
        log = HitLog.Load(level.name);
        RefreshLevelSettings();
        playhead = keepPlayhead;
        if (replacingFiles) LoadVideo();
        LoadCover();
        RebuildLevelList();
        RefreshAll();
        SetStatus("Saved ✓", Theme.Great, 3f);
#if UNITY_EDITOR
        //Let Unity pick up new/renamed files in StreamingAssets
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    //---------------------------------------------------------------- per frame

    void Update()
    {
        if (level == null) return;

        if (video.isPlaying) playhead = (float)video.time;
        HandleKeys();
        HandleListScroll();
        PlayHitSounds();
        DrawPreviewNotes();
        DrawZoom();

        view.timeText.text = $"{FmtClock(playhead)}  <color=#8E94A0>/ {FmtClock((float)duration)}</color>";
        view.playLabel.text = video.isPlaying ? "❚❚ PAUSE" : "▶ PLAY";
        view.playheadLine.anchoredPosition = new Vector2(playhead / (float)duration * V.MidW, 0);

        if (statusUntil > 0 && Time.unscaledTime > statusUntil) statusUntil = 0;
        if (statusUntil == 0)
        {
            view.statusText.text = dirty ? "Unsaved changes" : "";
            view.statusText.color = Theme.Good;
        }
    }

    void HandleKeys()
    {
        //Typing in a text box shouldn't trigger shortcuts
        GameObject focus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        TMP_InputField field = focus != null ? focus.GetComponent<TMP_InputField>() : null;
        if (field != null && field.isFocused) return;

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (ctrl && Input.GetKeyDown(KeyCode.S)) { Save(); return; }
        if (ctrl && Input.GetKeyDown(KeyCode.Z)) { Undo(); return; }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K)) TogglePlay();
        if (Input.GetKeyDown(KeyCode.J)) Seek(playhead - 2f);
        if (Input.GetKeyDown(KeyCode.L)) Seek(playhead + 2f);
        if (Input.GetKeyDown(KeyCode.N)) AddNote(false);
        if (Input.GetKeyDown(KeyCode.H)) AddNote(true);
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) DeleteSelected();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Nudge(shift ? -0.05f : -0.01f);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Nudge(shift ? 0.05f : 0.01f);
        if (Input.GetKeyDown(KeyCode.UpArrow)) SelectNeighbour(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) SelectNeighbour(1);
    }

    void HandleListScroll()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (wheel == 0f || !RectTransformUtility.RectangleContainsScreenPoint(view.listArea, Input.mousePosition, null)) return;
        listTop -= Mathf.RoundToInt(wheel * 3);
        RefreshList();
    }

    void PlayHitSounds()
    {
        if (!video.isPlaying) { lastTickTime = playhead; return; }
        if (playhead < lastTickTime) lastTickTime = playhead;   //looped or seeked back
        if (hitSounds)
        {
            int played = 0;
            foreach (EditNote n in level.notes)
            {
                float t = PreviewTime(n.time);
                if (t > lastTickTime && t <= playhead && played++ < 3)
                {
                    sfx.pitch = n.hold ? 1.0f : 1.35f;
                    sfx.PlayOneShot(Sfx.Tick, 0.8f);
                }
            }
        }
        lastTickTime = playhead;
    }

    //Notes as the player sees them: approach discs growing for 3s, then the target.
    //Positions are random like in game, but fixed per note so the preview doesn't jump around.
    void DrawPreviewNotes()
    {
        float area = V.PreviewH * 0.3f;     //half-size of the spawn square
        float d = area * 2f * (NoteVisual.Diameter / 2.8f);
        int used = 0;
        EditNote next = null;
        foreach (EditNote n in level.notes)
        {
            if (PreviewTime(n.time) > playhead) { next = n; break; }
        }

        for (int i = 0; i < level.notes.Count && used < 40; i++)
        {
            EditNote n = level.notes[i];
            float t = PreviewTime(n.time);
            float until = t - playhead;
            float holdLen = n.hold ? n.end - n.time : 0f;
            bool approaching = until > 0f && until <= ApproachTime;
            bool showingTarget = until <= 0f && -until <= (n.hold ? holdLen : 0.3f);
            if (!approaching && !showingTarget) continue;

            NoteSprite s = GetNoteSprite(used++);
            //Same placement as the game: chart height (sung pitch) if known, otherwise random
            Vector2 pos = NotePosition(i);
            if (!float.IsNaN(n.height)) pos.y = Mathf.Lerp(-1f, 1f, Mathf.Clamp01(n.height));
            s.root.anchoredPosition = pos * area;
            bool isNext = n == next;
            if (approaching)
            {
                float p = 1f - until / ApproachTime;
                Color c = isNext ? (n.hold ? Theme.NextHeld : Theme.NextNote) : (n.hold ? Theme.HeldNote : Theme.UpcomingNote);
                s.fill.color = c.WithAlpha(isNext ? 0.75f : 0.35f);
                s.fill.rectTransform.sizeDelta = Vector2.one * d * p;
                s.rim.enabled = false;
            }
            else if (n.hold)
            {
                float left = 1f - (-until / Mathf.Max(0.01f, holdLen));
                s.fill.color = Theme.HeldNote.WithAlpha(0.9f);
                s.fill.rectTransform.sizeDelta = Vector2.one * d * left;
                s.rim.enabled = true;
                s.rim.color = Theme.NextHeld;
                s.rim.rectTransform.sizeDelta = Vector2.one * d * left;
            }
            else
            {
                float fade = 1f - (-until / 0.3f);
                s.fill.color = Theme.Background.WithAlpha(0.6f * fade);
                s.fill.rectTransform.sizeDelta = Vector2.one * d;
                s.rim.enabled = true;
                s.rim.color = Theme.Note.WithAlpha(fade);
                s.rim.rectTransform.sizeDelta = Vector2.one * d;
            }
            s.sel.enabled = n == selected;
            s.sel.rectTransform.sizeDelta = Vector2.one * d * 1.3f;
        }
        for (int i = used; i < notePool.Count; i++) notePool[i].root.gameObject.SetActive(false);
    }

    NoteSprite GetNoteSprite(int i)
    {
        while (notePool.Count <= i)
        {
            var root = new GameObject("Note", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(view.previewNotes, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            notePool.Add(new NoteSprite
            {
                root = root,
                fill = UIFactory.MakeImage(root, Theme.Circle, Color.white, Vector2.zero),
                rim = UIFactory.MakeImage(root, Theme.Ring, Color.white, Vector2.zero),
                sel = UIFactory.MakeImage(root, Theme.ThinRing, Color.white, Vector2.zero),
            });
        }
        NoteSprite n = notePool[i];
        n.root.gameObject.SetActive(true);
        n.root.SetSiblingIndex(0);   //later notes underneath, the next note on top
        return n;
    }

    //Fixed pseudo-random spot in [-1,1]^2 for note i
    static Vector2 NotePosition(int i)
    {
        uint h = (uint)i * 2654435761u + 0x9E3779B9u;
        h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
        float x = (h & 0xFFFF) / 65535f * 2f - 1f;
        float y = ((h >> 16) & 0xFFFF) / 65535f * 2f - 1f;
        return new Vector2(x, y);
    }

    void DrawZoom()
    {
        int used = 0;
        float start = ZoomStart, end = playhead + V.ZoomAfter;
        foreach (EditNote n in level.notes)
        {
            float t = PreviewTime(n.time);
            float tEnd = n.hold ? PreviewTime(n.end) : t;
            if (tEnd < start || t > end) continue;
            if (used >= 80) break;
            ZoomMarker m = GetZoomMarker(used++);
            float x = ZoomX(t);
            bool sel = n == selected;
            Color c = sel ? Color.white : (n.hold ? Theme.HeldNote : Theme.UpcomingNote);
            if (n.hold)
            {
                m.bar.anchoredPosition = new Vector2(x, -46);
                m.bar.sizeDelta = new Vector2(Mathf.Max(4f, ZoomX(tEnd) - x), 22);
                m.img.color = c.WithAlpha(sel ? 1f : 0.6f);
            }
            else
            {
                m.bar.anchoredPosition = new Vector2(x - 1.5f, -26);
                m.bar.sizeDelta = new Vector2(sel ? 4 : 3, 70);
                m.img.color = c;
            }
            m.label.text = n.label;
            m.label.color = sel ? Color.white : Theme.TextMuted;
            m.label.rectTransform.anchoredPosition = new Vector2(x + 4, -4);
        }
        for (int i = used; i < zoomPool.Count; i++)
        {
            zoomPool[i].bar.gameObject.SetActive(false);
            zoomPool[i].label.gameObject.SetActive(false);
        }
        view.zoomPlayhead.SetAsLastSibling();
    }

    ZoomMarker GetZoomMarker(int i)
    {
        while (zoomPool.Count <= i)
        {
            Image img = V.Box(view.zoomStrip, 0, 0, 3, 70, Color.white, Theme.Square);
            TextMeshProUGUI label = V.Label(view.zoomStrip, "", 0, 0, 120, 20, 13, true, Theme.TextMuted, TextAlignmentOptions.Left);
            zoomPool.Add(new ZoomMarker { bar = img.rectTransform, img = img, label = label });
        }
        ZoomMarker m = zoomPool[i];
        m.bar.gameObject.SetActive(true);
        m.label.gameObject.SetActive(true);
        return m;
    }

    void OnDestroy()
    {
        if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
    }

    //---------------------------------------------------------------- helpers

    void SetStatus(string text, Color color, float seconds = 2.5f)
    {
        view.statusText.text = text;
        view.statusText.color = color;
        statusUntil = Time.unscaledTime + seconds;
    }

    static string Fmt(float v) => v.ToString("0.000", CultureInfo.InvariantCulture);

    static string FmtClock(float t)
    {
        t = Mathf.Max(0f, t);
        return $"{(int)(t / 60)}:{t % 60:00.000}";
    }

    static bool TryFloat(string s, out float v) =>
        float.TryParse((s ?? "").Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
}
