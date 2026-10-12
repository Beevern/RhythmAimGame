using UnityEngine;

//Flat, osu-style rendering for counters (approach), targets and held targets.
//The original sphere renderer is hidden but its collider stays, so hit detection is unchanged.
//The visual lives on its own object (not a child) because counters are rotated and squashed.
public class NoteVisual : MonoBehaviour
{
    public enum Kind { Approach, Target, Held }

    //World diameter of a target (sphere mesh is 1 unit, prefabs are scaled 0.5)
    public const float Diameter = 0.5f;
    private const float PopTime = 0.09f;

    private Transform owner;
    private Kind kind;
    private Counter counter;
    private SpriteRenderer shadow;
    private SpriteRenderer fill;
    private SpriteRenderer rim;
    private float age;
    //Optional custom art drawn instead of the default target circle
    private Sprite skin;

    public static NoteVisual Attach(GameObject ownerObj, Kind kind, Sprite skin = null)
    {
        foreach (Renderer r in ownerObj.GetComponents<Renderer>())
        {
            r.enabled = false;
        }
        GameObject go = new GameObject(kind + "Visual");
        go.transform.position = ownerObj.transform.position;
        NoteVisual v = go.AddComponent<NoteVisual>();
        v.owner = ownerObj.transform;
        v.kind = kind;
        v.counter = ownerObj.GetComponent<Counter>();
        v.skin = skin;
        v.Build();
        v.LateUpdate();
        return v;
    }

    private SpriteRenderer Layer(string name, Sprite sprite, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private void Build()
    {
        switch (kind)
        {
            case Kind.Approach:
                fill = Layer("Fill", Theme.Circle, 10);
                break;
            case Kind.Target when skin != null:
                //Custom art inside the same outline ring as a normal target
                shadow = Layer("Shadow", Theme.Circle, 30);
                fill = Layer("Skin", skin, 31);
                rim = Layer("Rim", Theme.Ring, 32);
                shadow.color = Color.black.WithAlpha(0.35f);
                rim.color = Theme.Note;
                break;
            case Kind.Target:
                shadow = Layer("Shadow", Theme.Circle, 30);
                fill = Layer("Fill", Theme.Circle, 31);
                rim = Layer("Rim", Theme.Ring, 32);
                shadow.color = Color.black.WithAlpha(0.35f);
                fill.color = Theme.Background.WithAlpha(0.6f);
                rim.color = Theme.Note;
                break;
            case Kind.Held:
                shadow = Layer("Shadow", Theme.Circle, 30);
                fill = Layer("Fill", Theme.Circle, 31);
                rim = Layer("Rim", Theme.Ring, 32);
                shadow.color = Color.black.WithAlpha(0.35f);
                shadow.transform.localScale = Vector3.one * Diameter * 1.12f;
                rim.transform.localScale = Vector3.one * Diameter;
                break;
        }
    }

    void LateUpdate()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = owner.position;
        age += Time.deltaTime;

        switch (kind)
        {
            case Kind.Approach: UpdateApproach(); break;
            case Kind.Target: UpdateTarget(); break;
            case Kind.Held: UpdateHeld(); break;
        }
    }

    private void UpdateApproach()
    {
        //Counters always count down from 3 seconds; the disc grows to full size as it nears zero
        float p = counter != null ? Mathf.Clamp01(1f - counter.destroyTimer / 3f) : 1f;
        bool held = counter != null && counter.isHeld;
        bool next = counter != null && counter.IsPriority;
        float fadeIn = Mathf.Clamp01(age / 0.3f);

        Color baseColor = next ? (held ? Theme.NextHeld : Theme.NextNote) : (held ? Theme.HeldNote : Theme.UpcomingNote);
        fill.color = baseColor.WithAlpha((next ? 0.75f : 0.35f) * fadeIn);
        fill.transform.localScale = Vector3.one * Diameter * p;

        //Next note draws above the rest
        fill.sortingOrder = next ? 20 : 10;
    }

    private void UpdateTarget()
    {
        float t = Mathf.Clamp01(age / PopTime);
        float s = Diameter * Mathf.Lerp(0.85f, 1f, 1f - (1f - t) * (1f - t));
        if (skin != null)
        {
            //Fit the art's longest side inside the ring, whatever its import size
            Vector2 size = skin.bounds.size;
            fill.transform.localScale = Vector3.one * (s * 0.86f / Mathf.Max(size.x, size.y, 0.0001f));
        }
        else
        {
            fill.transform.localScale = Vector3.one * s;
        }
        rim.transform.localScale = Vector3.one * s;
        shadow.transform.localScale = Vector3.one * s * 1.12f;
    }

    private void UpdateHeld()
    {
        //HeldTarget shrinks its own transform over its duration; the whole note shrinks with it
        float size = Mathf.Max(0f, owner.localScale.x);
        fill.transform.localScale = Vector3.one * size;
        rim.transform.localScale = Vector3.one * size;
        shadow.transform.localScale = Vector3.one * size * 1.12f;
        bool holding = Player.holding;
        //Yellow note, purple rim; brightens while being held
        fill.color = Theme.HeldNote.WithAlpha(holding ? 1f : 0.8f);
        rim.color = Theme.NextHeld.WithAlpha(holding ? 1f : 0.85f);
    }
}
