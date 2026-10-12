using UnityEngine;
using UnityEngine.UI;

//Quiet animated background for menu scenes: soft top gradient plus two slow-drifting colour glows
public class MenuBackdrop : MonoBehaviour
{
    private RectTransform glowA;
    private RectTransform glowB;

    public static void Create()
    {
        if (FindFirstObjectByType<MenuBackdrop>() != null) return;
        Canvas canvas = UIFactory.OverlayCanvas("MenuBackdrop", -100);
        MenuBackdrop backdrop = canvas.gameObject.AddComponent<MenuBackdrop>();
        Transform root = canvas.transform;

        UIFactory.FullScreenImage(root, Theme.Background);
        Image grad = UIFactory.FullScreenImage(root, Theme.BackgroundTop, Theme.VerticalGradient);
        grad.type = Image.Type.Simple;

        backdrop.glowA = UIFactory.MakeImage(root, Theme.SoftBlob, Theme.Accent.WithAlpha(0.07f), new Vector2(1100, 1100)).rectTransform;
        backdrop.glowB = UIFactory.MakeImage(root, Theme.SoftBlob, Theme.Perfect.WithAlpha(0.05f), new Vector2(1300, 1300)).rectTransform;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        glowA.anchoredPosition = new Vector2(-520 + Mathf.Sin(t * 0.11f) * 160, 220 + Mathf.Cos(t * 0.09f) * 90);
        glowB.anchoredPosition = new Vector2(560 + Mathf.Cos(t * 0.08f) * 180, -260 + Mathf.Sin(t * 0.13f) * 110);
    }
}
