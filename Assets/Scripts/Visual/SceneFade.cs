using UnityEngine;
using UnityEngine.UI;

//Short fade-from-black whenever a scene loads
public class SceneFade : MonoBehaviour
{
    private const float Duration = 0.3f;
    private Image image;
    private float t;

    public static void Play()
    {
        GameObject go = new GameObject("SceneFade");
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        SceneFade fade = go.AddComponent<SceneFade>();
        fade.image = UIFactory.FullScreenImage(go.transform, Theme.Background);
        fade.image.raycastTarget = false;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float a = 1f - Mathf.Clamp01(t / Duration);
        image.color = Theme.Background.WithAlpha(a * a);
        if (t >= Duration) Destroy(gameObject);
    }
}
