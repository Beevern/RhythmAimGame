using UnityEngine;

//One-shot hit/miss effects built from the theme's sprites
public static class HitFx
{
    public static void Burst(Vector3 pos, int score)
    {
        //Misses only show the "MISS" text, no ring
        if (score <= 0) return;
        float d = NoteVisual.Diameter;
        Color c = Theme.JudgementColor(score);
        SpriteTween.Spawn(Theme.Ring, pos, c, d, d * 1.6f, 0.9f, 0f, 0.32f, 40);
        SpriteTween.Spawn(Theme.Circle, pos, Color.white, d, d * 1.15f, 0.45f, 0f, 0.15f, 41);
    }

    //Smaller, quicker burst in the early colour
    public static void Early(Vector3 pos)
    {
        float d = NoteVisual.Diameter;
        SpriteTween.Spawn(Theme.ThinRing, pos, Theme.Early, d * 0.9f, d * 1.3f, 0.85f, 0f, 0.25f, 40);
    }
}
