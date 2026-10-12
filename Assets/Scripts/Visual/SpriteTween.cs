using UnityEngine;

//Scales and fades a sprite once, then destroys itself
public class SpriteTween : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    private float fromScale, toScale, fromAlpha, toAlpha, duration, t;

    public static SpriteTween Spawn(Sprite sprite, Vector3 pos, Color color, float fromScale, float toScale,
        float fromAlpha, float toAlpha, float duration, int order)
    {
        GameObject go = new GameObject("Fx");
        go.transform.position = pos;
        SpriteTween tw = go.AddComponent<SpriteTween>();
        tw.sr = go.AddComponent<SpriteRenderer>();
        tw.sr.sprite = sprite;
        tw.sr.sortingOrder = order;
        tw.color = color;
        tw.fromScale = fromScale;
        tw.toScale = toScale;
        tw.fromAlpha = fromAlpha;
        tw.toAlpha = toAlpha;
        tw.duration = duration;
        tw.Apply(0f);
        return tw;
    }

    void Update()
    {
        t += Time.deltaTime / duration;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        Apply(t);
    }

    private void Apply(float p)
    {
        float e = 1f - (1f - p) * (1f - p) * (1f - p);
        transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, e);
        sr.color = color.WithAlpha(Mathf.Lerp(fromAlpha, toAlpha, p));
    }
}
