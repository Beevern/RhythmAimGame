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
    //Other objects
    public GameObject needle;
    public GameObject box;
    public GameObject rightBound;
    public GameObject leftBound;
    public VideoPlayer vidPlayer;
    public GameObject oneTenthInc;
    public GameObject secInc;
    public Transform boxTransform;
    //Instances
    public static float boxLength;
    public bool isPlayed;
    public bool isMoved;
    public int sizeFactor;
    // Start is called before the first frame update

    void Awake()
    {
        vidPlayer.Prepare();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!isMoved)
        {
            LevelMaker.timeNow-=0.09f;
        }
        if (vidPlayer.isPrepared && !isPlayed)
        {
            isPlayed = true;
            sizeFactor = 500;
            //For every second, make a increment
            for (int i = 0; i<vidPlayer.length+9;i++)
            {
                //                                                    Move 997 so that it's time accurate
                GameObject secInc1 = Instantiate(secInc, new Vector3(-997+i*sizeFactor, 40f, 0), Quaternion.identity);
                secInc1.transform.SetParent(boxTransform);
                //For every second, make 10 increments for each 1/10th second
                for (int j = 1;j<10;j++)
                {
                    GameObject tenthInc1 = Instantiate(oneTenthInc, new Vector3(-997 + i * sizeFactor + j*sizeFactor/10, 45.5f, 0), Quaternion.identity);
                    tenthInc1.transform.SetParent(boxTransform);
                }
            }
            //Times by 2 because the width is expanded 2 ways.
            boxLength = (float)vidPlayer.length * sizeFactor * 2;
            box.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boxLength);
            vidPlayer.Play();
        }
        if (isPlayed && vidPlayer.isPlaying)
        {
            if (needle.transform.position.x > rightBound.transform.position.x && box.transform.position.x < boxLength-1920)
            {
                //Debug.Log("Moved right");
                box.transform.position += Vector3.left * sizeFactor*2;
                rightBound.transform.position += Vector3.right * sizeFactor*2;
                leftBound.transform.position += Vector3.right * sizeFactor*2;

            }
            if (needle.transform.position.x < leftBound.transform.position.x && box.transform.position.x <= -sizeFactor)
            {
                //Debug.Log("Moved left");
                box.transform.position += Vector3.right * sizeFactor*2;
                leftBound.transform.position += Vector3.left * sizeFactor*2;
                rightBound.transform.position += Vector3.left * sizeFactor*2;
            }

            needle.transform.position += Vector3.right * sizeFactor * Time.deltaTime;
        }
        
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isMoved = true;
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
        if (vidPlayer != null && vidPlayer.clip != null)
        {
            vidPlayer.time = (xPosition / (boxLength/2)) * (float)vidPlayer.clip.length;
        }
    }

}
