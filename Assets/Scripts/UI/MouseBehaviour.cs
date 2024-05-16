using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseBehaviour : MonoBehaviour
{
    public AudioSource mySource;
    public AudioClip hover;
    public AudioClip click;
    // Start is called before the first frame update
    void Start()
    {
        mySource = GameObject.FindGameObjectWithTag("Overviewer").GetComponent<AudioSource>();
    }
    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        mySource.PlayOneShot(hover);
    }
    void OnMouseClick()
    {
        mySource.PlayOneShot(click);
    }
}
