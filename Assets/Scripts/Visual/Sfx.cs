using UnityEngine;

//Small UI sounds synthesised at runtime, so no audio files are needed
public static class Sfx
{
    private const int Rate = 44100;
    private static AudioClip tick;
    private static AudioClip slam;

    //Short bright blip for counters ticking up
    public static AudioClip Tick => tick != null ? tick : (tick = Make("Tick", 0.035f, t =>
    {
        float env = Mathf.Exp(-t * 140f);
        return (Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 3600f * t) * 0.2f) * env;
    }));

    //Heavy impact: a pitch-dropping sine thump plus a burst of noise for the hit
    public static AudioClip Slam => slam != null ? slam : (slam = Make("Slam", 0.6f, t =>
    {
        //Frequency sweeps from ~140Hz down to ~40Hz; phase is the integral of that sweep
        float phase = 2f * Mathf.PI * (40f * t + 100f * (1f - Mathf.Exp(-t * 18f)) / 18f);
        float body = Mathf.Sin(phase) * Mathf.Exp(-t * 6f);
        float crack = (Random.value * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.5f;
        return Mathf.Clamp(body * 0.9f + crack, -1f, 1f);
    }));

    private static AudioClip Make(string name, float seconds, System.Func<float, float> sample)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = sample((float)i / Rate);
        }
        //Tiny fade at the end so clips never click
        int fade = Mathf.Min(n, Rate / 200);
        for (int i = 0; i < fade; i++)
        {
            data[n - 1 - i] *= (float)i / fade;
        }
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    //A 2D source on the given object, at the player's saved volume
    public static AudioSource Source(GameObject host)
    {
        AudioSource src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = PlayerPrefs.GetFloat("volume", 0.5f);
        return src;
    }
}
