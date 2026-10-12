using UnityEngine;
using UnityEngine.UI;

//Theme sprites are generated in code, so a scene can't store references to them.
//This puts the right one back on the Image whenever it loads (in Edit mode too).
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class ThemeSprite : MonoBehaviour
{
    public enum Kind { RoundedRect, Square, Circle, Ring, ThinRing, SoftBlob }
    public Kind kind;

    void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        GetComponent<Image>().sprite = Get(kind);
    }

    public static Sprite Get(Kind k)
    {
        switch (k)
        {
            case Kind.RoundedRect: return Theme.RoundedRect;
            case Kind.Square: return Theme.Square;
            case Kind.Circle: return Theme.Circle;
            case Kind.Ring: return Theme.Ring;
            case Kind.ThinRing: return Theme.ThinRing;
            default: return Theme.SoftBlob;
        }
    }

    //Adds a ThemeSprite to every Image under root that uses one of the theme's sprites
    public static void TagAll(Transform root)
    {
        foreach (Image img in root.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null || img.GetComponent<ThemeSprite>() != null) continue;
            for (Kind k = Kind.RoundedRect; k <= Kind.SoftBlob; k++)
            {
                if (img.sprite == Get(k))
                {
                    img.gameObject.AddComponent<ThemeSprite>().kind = k;
                    break;
                }
            }
        }
    }
}
