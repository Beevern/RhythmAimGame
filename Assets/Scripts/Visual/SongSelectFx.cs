using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Song select polish: neighbouring covers are dimmed, moving left/right slides the covers
//across like a carousel while the new centre cover fades up, and the selected song's
//difficulty is shown to the left of the Play button.
public class SongSelectFx : MonoBehaviour
{
    private static readonly Color SideTint = new Color(0.4f, 0.4f, 0.42f, 1f);
    private const float AnimTime = 0.25f;

    private SongSelect2 select;
    private Image center;
    private RectTransform[] covers;
    private Vector2[] basePos;
    //Distance between neighbouring covers, used as the slide distance
    private float spacing;

    private string lastSong;
    private int lastIndex;
    private int dir;
    private float t = 1f;

    private TextMeshProUGUI difficultyText;
    private string difficultyShownFor;

    void Start()
    {
        select = GetComponent<SongSelect2>();
        if (select.image != null) center = select.image.GetComponent<Image>();
        Tint(select.leftImage);
        Tint(select.rightImage);

        covers = new[] { Rect(select.leftImage), Rect(select.image), Rect(select.rightImage) };
        basePos = new Vector2[covers.Length];
        for (int i = 0; i < covers.Length; i++)
        {
            if (covers[i] != null) basePos[i] = covers[i].anchoredPosition;
        }
        if (covers[1] != null && covers[2] != null)
        {
            spacing = covers[2].anchoredPosition.x - covers[1].anchoredPosition.x;
        }

        lastSong = SongSelect2.theSong;
        lastIndex = SongSelect2.current;
        BuildDifficulty();
    }

    //Right-aligned against the Play button's left edge, vertically centred on it
    private void BuildDifficulty()
    {
        GameObject play = GameObject.Find("Play");
        if (play == null) return;
        RectTransform pr = (RectTransform)play.transform;

        GameObject go = new GameObject("Difficulty", typeof(RectTransform));
        go.transform.SetParent(pr.parent, false);
        difficultyText = go.AddComponent<TextMeshProUGUI>();
        RectTransform rt = difficultyText.rectTransform;
        rt.anchorMin = rt.anchorMax = pr.anchorMin;
        rt.pivot = new Vector2(1f, 0.5f);
        float gap = 14f;
        rt.anchoredPosition = pr.anchoredPosition + new Vector2(-pr.sizeDelta.x * pr.pivot.x - gap, (0.5f - pr.pivot.y) * pr.sizeDelta.y);
        rt.sizeDelta = new Vector2(200f, pr.sizeDelta.y);
        difficultyText.alignment = TextAlignmentOptions.Right;
        difficultyText.textWrappingMode = TextWrappingModes.NoWrap;
        difficultyText.fontSize = 34f;
        difficultyText.fontStyle = FontStyles.Bold;
        difficultyText.lineSpacing = -12f;
        difficultyText.raycastTarget = false;
        TextPop pop = go.AddComponent<TextPop>();
        pop.fromScale = 1.2f;
        pop.duration = 0.2f;
    }

    private void UpdateDifficulty()
    {
        string song = SongSelect2.theSong;
        difficultyShownFor = song;
        if (song == null || !LevelHolder.levels.TryGetValue(song, out LevelInfo level))
        {
            difficultyText.text = "";
            return;
        }
        Color c = Difficulty.ColorFor(level.difficulty);
        difficultyText.text = "<size=38%><color=#8E94A0>DIFFICULTY</color></size>\n<color=#" +
            ColorUtility.ToHtmlStringRGB(c) + ">★ " + level.difficulty.ToString("0.0") + "</color>";
    }

    private static RectTransform Rect(GameObject go)
    {
        return go != null ? go.GetComponent<RectTransform>() : null;
    }

    private static void Tint(GameObject go)
    {
        if (go == null) return;
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = SideTint;
    }

    void LateUpdate()
    {
        if (difficultyText != null && SongSelect2.theSong != difficultyShownFor) UpdateDifficulty();
        if (SongSelect2.current != lastIndex || SongSelect2.theSong != lastSong)
        {
            //Work out which way we moved (wrapping around the list); search/sort just fades
            int n = select.songNames != null ? select.songNames.Length : 0;
            int step = n > 0 ? ((SongSelect2.current - lastIndex) % n + n) % n : 0;
            dir = step == 1 ? 1 : (n > 1 && step == n - 1) ? -1 : 0;
            if (n == 2 && step == 1) dir = SongSelect2.current > lastIndex ? 1 : -1;

            lastIndex = SongSelect2.current;
            lastSong = SongSelect2.theSong;
            t = 0f;
        }

        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / AnimTime);
        float e = 1f - (1f - t) * (1f - t) * (1f - t);

        //Covers start one slot over (in the direction they came from) and ease into place
        float offset = dir * spacing * (1f - e);
        for (int i = 0; i < covers.Length; i++)
        {
            if (covers[i] != null) covers[i].anchoredPosition = basePos[i] + new Vector2(offset, 0f);
        }

        if (center != null)
        {
            center.color = Color.Lerp(SideTint, Color.white, e);
        }
    }
}
