using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ScoreDesc : MonoBehaviour
{
    public float timeToDestroy = 0.55f;
    public int scoreToAdd;
    //Hit before the target appeared: shows "TOO EARLY" instead of a timing judgement
    public bool early;
    //Judgement text drawn above the note
    private TextMeshPro label;
    private float lifeTime;
    private float age;
    // Start is called before the first frame update
    void Start()
    {
        //Prefab may still carry the old 0.3s lifetime; the animation needs a bit longer
        timeToDestroy = Mathf.Max(timeToDestroy, 0.55f);
        lifeTime = timeToDestroy;
        //Hide the legacy TextMesh renderers; the themed label replaces them
        foreach (MeshRenderer r in GetComponentsInChildren<MeshRenderer>())
        {
            r.enabled = false;
        }

        GameObject go = new GameObject("Judgement");
        go.transform.SetParent(transform, false);
        label = go.AddComponent<TextMeshPro>();
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 1.5f;
        label.characterSpacing = 6f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
        if (early)
        {
            label.text = "TOO EARLY";
            label.color = Theme.Early;
            RunStats.judgements[RunStats.EarlyIdx]++;
        }
        else
        {
            label.text = Theme.JudgementLabel(scoreToAdd);
            label.color = Theme.JudgementColor(scoreToAdd);
            RunStats.AddJudgement(scoreToAdd);
        }
        label.sortingOrder = 50;
        Animate();
    }

    // Update is called once per frame
    void Update()
    {
        timeToDestroy -= Time.deltaTime;
        if (timeToDestroy <=0)
        {

            Destroy(this.gameObject);
            return;
        }
        Animate();
    }

    //Pop in, drift up, fade out
    private void Animate()
    {
        age = 1f - Mathf.Clamp01(timeToDestroy / lifeTime);
        float pop = Mathf.Clamp01(age / 0.18f);
        float scale = Mathf.Lerp(1.3f, 1f, 1f - (1f - pop) * (1f - pop));
        label.transform.localScale = Vector3.one * scale;
        label.transform.localPosition = new Vector3(0f, NoteVisual.Diameter * 0.75f + age * 0.12f, 0f);
        label.alpha = age < 0.6f ? 1f : 1f - (age - 0.6f) / 0.4f;
    }
}
