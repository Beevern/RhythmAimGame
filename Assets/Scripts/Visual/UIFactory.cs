using UnityEngine;
using UnityEngine.UI;

//Helpers for building themed UI from code
public static class UIFactory
{
    //How many canvas units equal one pixel on a 1920x1080 screen, for the canvas this element lives on.
    //The project's canvases use an 800x600 reference, so sizes designed at 1080p need converting.
    public static float Unit(Component c)
    {
        Canvas canvas = c.GetComponentInParent<Canvas>(true);
        if (canvas == null) return 1f;
        CanvasScaler scaler = canvas.rootCanvas.GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return 1f;
        Vector2 r = scaler.referenceResolution;
        return Mathf.Lerp(r.x / 1920f, r.y / 1080f, scaler.matchWidthOrHeight);
    }

    public static Image FullScreenImage(Transform parent, Color color, Sprite sprite = null)
    {
        GameObject go = new GameObject("Image", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        Stretch(img.rectTransform);
        return img;
    }

    public static Image MakeImage(Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        GameObject go = new GameObject("Image", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    //Pin a rect to a screen corner/edge. anchor is e.g. (1,1) for top-right.
    public static void Pin(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    public static Canvas OverlayCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    //Themed horizontal slider (track, accent fill, round handle), sized to `size` in parent units
    public static Slider MakeSlider(Transform parent, Vector2 size, float min, float max, bool whole, float value)
    {
        GameObject go = new GameObject("Slider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = size;
        Slider slider = go.AddComponent<Slider>();

        Image track = MakeImage(go.transform, Theme.RoundedRect, Theme.Track, Vector2.zero);
        track.type = Image.Type.Sliced;
        track.pixelsPerUnitMultiplier = 3f;
        CentreBand(track.rectTransform, 6f);

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(go.transform, false);
        CentreBand(fillArea, 6f);
        Image fill = MakeImage(fillArea, Theme.RoundedRect, Theme.Accent, Vector2.zero);
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 3f;
        Stretch(fill.rectTransform);

        RectTransform handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(go.transform, false);
        Stretch(handleArea);
        Image handle = MakeImage(handleArea, Theme.Circle, Color.white, Vector2.zero);
        handle.raycastTarget = true;
        handle.rectTransform.sizeDelta = new Vector2(size.y, 0f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = whole;
        slider.SetValueWithoutNotify(value);
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.highlightedColor = Theme.Text;
        cb.pressedColor = Theme.Accent;
        cb.selectedColor = Color.white;
        slider.colors = cb;
        //Clicking anywhere on the bar should work, so the track takes raycasts too
        track.raycastTarget = true;
        return slider;
    }

    //Full width, fixed height, vertically centred
    private static void CentreBand(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, height);
    }
}
