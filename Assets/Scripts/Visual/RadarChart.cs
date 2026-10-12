using UnityEngine;
using UnityEngine.UI;

//Spider/radar chart drawn straight into a UI mesh. Values are 0..1, first axis points straight up,
//the rest go clockwise. `progress` scales the data shape for the grow-in animation.
//CanvasRenderer must exist before the graphic enables (like Image/RawImage), or the canvas never draws it.
[RequireComponent(typeof(CanvasRenderer))]
public class RadarChart : MaskableGraphic
{
    public float[] values = new float[0];
    [Range(0, 1)] public float progress = 1f;
    public int rings = 4;
    public Color gridColor = new Color(1f, 1f, 1f, 0.18f);
    public Color fillColor = new Color(1f, 0.4f, 0.67f, 0.35f);
    public Color lineColor = new Color(1f, 0.4f, 0.67f, 1f);
    public float gridWidth = 2f;
    public float lineWidth = 3f;
    public float dotSize = 9f;

    public float Radius => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;

    //Unit direction of an axis, in the chart's local space
    public Vector2 AxisDirection(int i)
    {
        float a = Mathf.PI * 0.5f - i * Mathf.PI * 2f / values.Length;
        return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    public void SetProgress(float p)
    {
        progress = p;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int n = values.Length;
        if (n < 3) return;
        Vector2 c = rectTransform.rect.center;
        float r = Radius;

        //Grid rings and spokes
        for (int ring = 1; ring <= rings; ring++)
        {
            float rr = r * ring / rings;
            for (int i = 0; i < n; i++)
            {
                Line(vh, c + AxisDirection(i) * rr, c + AxisDirection((i + 1) % n) * rr, gridWidth, gridColor);
            }
        }
        for (int i = 0; i < n; i++)
        {
            Line(vh, c, c + AxisDirection(i) * r, gridWidth, gridColor);
        }

        //Data shape: triangle fan from the centre
        Vector2[] pts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            pts[i] = c + AxisDirection(i) * r * Mathf.Clamp01(values[i]) * progress;
        }
        int centre = vh.currentVertCount;
        vh.AddVert(c, fillColor, Vector2.zero);
        for (int i = 0; i < n; i++)
        {
            vh.AddVert(pts[i], fillColor, Vector2.zero);
        }
        for (int i = 0; i < n; i++)
        {
            vh.AddTriangle(centre, centre + 1 + i, centre + 1 + (i + 1) % n);
        }

        //Outline and corner dots
        for (int i = 0; i < n; i++)
        {
            Line(vh, pts[i], pts[(i + 1) % n], lineWidth, lineColor);
        }
        for (int i = 0; i < n; i++)
        {
            Quad(vh, pts[i], dotSize * 0.5f, lineColor);
        }
    }

    private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color col)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 0.0001f) return;
        //Extend slightly so neighbouring segments meet without gaps at the corners
        Vector2 dir = d.normalized;
        a -= dir * width * 0.5f;
        b += dir * width * 0.5f;
        Vector2 nrm = new Vector2(-dir.y, dir.x) * width * 0.5f;
        int v = vh.currentVertCount;
        vh.AddVert(a - nrm, col, Vector2.zero);
        vh.AddVert(a + nrm, col, Vector2.zero);
        vh.AddVert(b + nrm, col, Vector2.zero);
        vh.AddVert(b - nrm, col, Vector2.zero);
        vh.AddTriangle(v, v + 1, v + 2);
        vh.AddTriangle(v, v + 2, v + 3);
    }

    //Diamond marker
    private static void Quad(VertexHelper vh, Vector2 p, float half, Color col)
    {
        int v = vh.currentVertCount;
        vh.AddVert(p + new Vector2(0, half), col, Vector2.zero);
        vh.AddVert(p + new Vector2(half, 0), col, Vector2.zero);
        vh.AddVert(p + new Vector2(0, -half), col, Vector2.zero);
        vh.AddVert(p + new Vector2(-half, 0), col, Vector2.zero);
        vh.AddTriangle(v, v + 1, v + 2);
        vh.AddTriangle(v, v + 2, v + 3);
    }
}
