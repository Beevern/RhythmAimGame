using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
//Partially done by chatgpt
public class TimeBox : MonoBehaviour, IPointerDownHandler
{
    public GameObject needle;
    public GameObject box;
    public VideoPlayer vidPlayer;
    public GameObject oneTenthInc;
    public GameObject secInc;
    public Transform boxTransform;
    public static float boxLength;
    public bool isPlayed;
    public int sizeFactor;
    // Start is called before the first frame update

    void Awake()
    {
        vidPlayer.Prepare();
    }
    void Start()
    {
        sizeFactor = 100;
        //For every second, make a increment
        for (int i = 0; i<vidPlayer.length+9;i++)
        {
            GameObject secInc1 = Instantiate(secInc, new Vector3(-1000+i*100, 40f, 0), Quaternion.identity);
            secInc1.transform.SetParent(boxTransform);
            //For every second, make 10 increments for each 1/10th second
            for (int j = 1;j<10;j++)
            {
                GameObject tenthInc1 = Instantiate(oneTenthInc, new Vector3(-1000 + i * 100 + j*10, 45.5f, 0), Quaternion.identity);
                tenthInc1.transform.SetParent(boxTransform);
            }
        }
        //Times by 2 because the width is expanded 2 ways.
        boxLength = (float)vidPlayer.length * sizeFactor * 2;
        box.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boxLength);
    }

    // Update is called once per frame
    void Update()
    {
        if (vidPlayer.isPrepared && isPlayed)
        {
            vidPlayer.Play();
            isPlayed = true;
        }
        if (vidPlayer.isPlaying)
        {
            /*
            if (needle.transform.position.x >= 1840)// && (vidPlayer.time/vidPlayer.length)<=0.95)
            {
                box.transform.position += Vector3.left * 1000;
            }
            
            if (needle.transform.position.x <= -850)
            {
                box.transform.position += Vector3.right * 1000;
            }
            */
            needle.transform.position += Vector3.right * 100 * Time.deltaTime;
        }
        
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localPoint;
        // Convert the screen point to a local point in the timeline's RectTransform
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(box.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out localPoint))
        {
            // Move the needle to the click position
            MoveNeedleToPosition(localPoint.x);

            // Calculate the corresponding time and set it in the video/audio
            SetTimeFromNeedlePosition(localPoint.x);
        }
    }

    private void MoveNeedleToPosition(float xPosition)
    {
        needle.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPosition, needle.GetComponent<RectTransform>().anchoredPosition.y);
    }

    private void SetTimeFromNeedlePosition(float xPosition)
    {
        float durationInSeconds = GetSongLengthInSeconds();
        float timelineWidth = boxLength/2;

        // Calculate the new time based on the needle's position
        float normalizedTime = xPosition / timelineWidth;
        float newTime = normalizedTime * durationInSeconds;

        // Set the new time in the video or audio player
        if (vidPlayer != null && vidPlayer.clip != null)
        {
            vidPlayer.time = newTime;
        }
    }

    float GetSongLengthInSeconds()
    {
        // Determine the length of the song (in seconds)
        if (vidPlayer != null && vidPlayer.clip != null)
        {
            return (float)vidPlayer.clip.length;
        }
        else
        {
            Debug.LogError("No valid audio or video clip found.");
            return 0;
        }
    }

}
