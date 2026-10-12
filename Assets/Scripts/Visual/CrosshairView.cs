using UnityEngine;
using UnityEngine.UI;

//Draws the crosshair from CrosshairSettings out of plain rectangles, redrawing whenever settings change.
//Units are pixels at 1080p, so put it on a canvas scaled to a 1080-tall reference.
public class CrosshairView : MonoBehaviour
{
    public static CrosshairView Create(Transform parent)
    {
        GameObject go = new GameObject("Crosshair", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        CrosshairView view = go.AddComponent<CrosshairView>();
        view.Rebuild();
        return view;
    }

    void OnEnable()
    {
        CrosshairSettings.Changed += Rebuild;
    }

    void OnDisable()
    {
        CrosshairSettings.Changed -= Rebuild;
    }

    public void Rebuild()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        //Each piece as (centre, size) in pixels
        var pieces = new System.Collections.Generic.List<(Vector2 pos, Vector2 size)>();
        if (CrosshairSettings.dot && CrosshairSettings.dotSize > 0)
        {
            pieces.Add((Vector2.zero, Vector2.one * CrosshairSettings.dotSize));
        }
        if (CrosshairSettings.lines && CrosshairSettings.lineLength > 0 && CrosshairSettings.lineThickness > 0)
        {
            float len = CrosshairSettings.lineLength, th = CrosshairSettings.lineThickness;
            float d = CrosshairSettings.lineGap + len * 0.5f;
            pieces.Add((new Vector2(0, d), new Vector2(th, len)));
            pieces.Add((new Vector2(0, -d), new Vector2(th, len)));
            pieces.Add((new Vector2(d, 0), new Vector2(len, th)));
            pieces.Add((new Vector2(-d, 0), new Vector2(len, th)));
        }

        //All outlines first so they never cover another piece's fill
        if (CrosshairSettings.outline && CrosshairSettings.outlineThickness > 0)
        {
            Color oc = Color.black.WithAlpha(CrosshairSettings.outlineOpacity * CrosshairSettings.opacity);
            float grow = CrosshairSettings.outlineThickness * 2f;
            foreach (var p in pieces) Rect(p.pos, p.size + Vector2.one * grow, oc);
        }
        Color fc = CrosshairSettings.color.WithAlpha(CrosshairSettings.opacity);
        foreach (var p in pieces) Rect(p.pos, p.size, fc);
    }

    private void Rect(Vector2 pos, Vector2 size, Color c)
    {
        Image img = UIFactory.MakeImage(transform, Theme.Square, c, size);
        img.rectTransform.anchoredPosition = pos;
    }
}
