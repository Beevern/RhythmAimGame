using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using V = LevelEditorView;

//Settings > Note colours: pick a colour for each kind of note from a row of swatches,
//with a live preview of how they look in game. Saved immediately (NoteColors).
public class NoteColorEditor : MonoBehaviour
{
    private const float CardW = 1100f, CardH = 560f;
    private const float RowsX = 400f, RowsY = 100f, RowH = 72f;
    private const float Swatch = 34f, SwatchGap = 8f;

    private static NoteColorEditor instance;
    public static bool IsOpen => instance != null;

    private RectTransform card;
    private float openT;
    private readonly List<Image>[] rings = new List<Image>[5];
    private readonly Image[] previewFill = new Image[5];
    private readonly Image[] previewRim = new Image[5];

    public static void Open()
    {
        if (instance != null) return;
        Canvas canvas = UIFactory.OverlayCanvas("NoteColorEditor", 150);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        instance = canvas.gameObject.AddComponent<NoteColorEditor>();
        instance.Build();
    }

    private void Build()
    {
        Image scrim = UIFactory.FullScreenImage(transform, new Color(0f, 0f, 0f, 0.7f));
        scrim.raycastTarget = true;

        Image cardImg = UIFactory.MakeImage(transform, Theme.RoundedRect, Theme.Hex("#1D1F26"), new Vector2(CardW, CardH));
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 1.2f;
        cardImg.raycastTarget = true;
        card = cardImg.rectTransform;
        V.Box(card, 14, 0, CardW - 28, 3, Theme.Accent, Theme.Square);
        V.Label(card, "NOTE COLOURS", 40, 30, 400, 44, 32, true, Theme.Text, TextAlignmentOptions.Left).characterSpacing = 4;

        BuildPreview();
        for (int k = 0; k < NoteColors.Labels.Length; k++) BuildRow(k);

        Button reset = V.MakeButton(card, "RESET TO DEFAULT", 40, CardH - 84, 320, 52, false, out _);
        reset.onClick.AddListener(() => { NoteColors.ResetToDefaults(); Refresh(); });
        Button done = V.MakeButton(card, "DONE", CardW - 40 - 220, CardH - 84, 220, 52, true, out _);
        done.onClick.AddListener(Close);
        Refresh();
    }

    //One example of each note kind, side by side on a dark backdrop
    private void BuildPreview()
    {
        Image box = V.Box(card, 40, RowsY, 320, 5 * RowH - 10, Theme.Hex("#121317"), Theme.Square);
        for (int k = 0; k < 5; k++)
        {
            float cy = 12 + k * RowH + (RowH - 10) / 2 - 2;
            RectTransform slot = new GameObject("Preview " + k, typeof(RectTransform)).GetComponent<RectTransform>();
            slot.SetParent(box.transform, false);
            V.Place(slot, 130, cy - 28, 56, 56);
            previewFill[k] = UIFactory.MakeImage(slot, Theme.Circle, Color.white, new Vector2(56, 56));
            previewRim[k] = UIFactory.MakeImage(slot, Theme.Ring, Color.white, new Vector2(56, 56));
            previewFill[k].rectTransform.anchoredPosition = previewRim[k].rectTransform.anchoredPosition = new Vector2(28, -28);
            previewFill[k].rectTransform.anchorMin = previewFill[k].rectTransform.anchorMax = new Vector2(0, 1);
            previewRim[k].rectTransform.anchorMin = previewRim[k].rectTransform.anchorMax = new Vector2(0, 1);
        }
    }

    private void BuildRow(int k)
    {
        float y = RowsY + k * RowH;
        V.Label(card, NoteColors.Labels[k], RowsX, y, 180, Swatch + 8, 16, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 3;
        rings[k] = new List<Image>();
        for (int i = 0; i < NoteColors.Palette.Length; i++)
        {
            Color c = NoteColors.Palette[i];
            float x = RowsX + 190 + i * (Swatch + SwatchGap);
            Image ring = V.Box(card, x - 4, y, Swatch + 8, Swatch + 8, Color.white, Theme.Ring);
            rings[k].Add(ring);
            Image sw = V.Box(card, x, y + 4, Swatch, Swatch, c, Theme.Circle);
            sw.raycastTarget = true;
            Button b = sw.gameObject.AddComponent<Button>();
            b.targetGraphic = sw;
            NoteColors.Kind kind = (NoteColors.Kind)k;
            b.onClick.AddListener(() => { NoteColors.Set(kind, c); Refresh(); });
            sw.gameObject.AddComponent<ButtonFeedback>();
        }
    }

    private void Refresh()
    {
        for (int k = 0; k < 5; k++)
        {
            Color chosen = NoteColors.Get((NoteColors.Kind)k);
            for (int i = 0; i < rings[k].Count; i++)
                rings[k][i].enabled = Same(NoteColors.Palette[i], chosen);

            //Draw each kind the way the game does
            switch ((NoteColors.Kind)k)
            {
                case NoteColors.Kind.Target:
                    previewFill[k].color = Theme.Background.WithAlpha(0.6f);
                    previewRim[k].color = chosen;
                    break;
                case NoteColors.Kind.Upcoming:
                    previewFill[k].color = chosen.WithAlpha(0.35f);
                    previewRim[k].color = Color.clear;
                    break;
                case NoteColors.Kind.Next:
                case NoteColors.Kind.NextHeld:
                    previewFill[k].color = chosen.WithAlpha(0.75f);
                    previewRim[k].color = Color.clear;
                    break;
                case NoteColors.Kind.Held:
                    previewFill[k].color = chosen.WithAlpha(0.9f);
                    previewRim[k].color = NoteColors.Get(NoteColors.Kind.NextHeld);
                    break;
            }
        }
    }

    private static bool Same(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

    private void Close()
    {
        PlayerPrefs.Save();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        openT = Mathf.Min(1f, openT + Time.unscaledDeltaTime / 0.18f);
        float e = 1f - (1f - openT) * (1f - openT) * (1f - openT);
        card.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, e);
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }
}
