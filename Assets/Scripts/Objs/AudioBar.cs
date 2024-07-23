using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Done by chatgpt
public class AudioBar : MonoBehaviour
{
    public AudioSource audioSource;    // The audio source to visualize
    public GameObject barPrefab;       // The prefab to use for the visualizer bars
    private int numberOfBars = 70;      // Number of bars in the visualizer
    private float barSpacing = 0.25f;    // Spacing between the bars
    private float scaleMultiplier = 10f; // Scale multiplier for the bar height

    private GameObject[] bars;
    private float[] spectrumData;

    void Start()
    {
        // Initialize the array for the spectrum data
        spectrumData = new float[1024];

        // Initialize the bars array
        bars = new GameObject[numberOfBars];

        // Create the visualizer bars
        for (int i = 0; i < numberOfBars; i++)
        {
            GameObject bar = Instantiate(barPrefab);
            bar.transform.position = new Vector3(i * barSpacing-8.6f, -3.7f, -0);
            bar.transform.SetParent(transform);
            bars[i] = bar;
        }
    }

    void Update()
    {
        // Get the spectrum data from the audio source
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);

        // Update the scale of each bar based on the spectrum data
        for (int i = 0; i < numberOfBars; i++)
        {
            float scaleY = spectrumData[i] * scaleMultiplier;
            Vector3 newScale = new Vector3(1, Mathf.Clamp(scaleY, 0.1f, 50f), 0.00000001f);
            bars[i].transform.localScale = newScale;
        }
    }
}
