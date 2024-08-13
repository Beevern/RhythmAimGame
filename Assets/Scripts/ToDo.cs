using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToDo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //In Order Of Importance?
        /*
         * Custom sprites
         * Self adding levels?
         *  - We can do this by allowing user to add mp3 or mp4, and a slider that represents where we are in the video. We can press a certain key to create a marker with
         *    a property which contains the time of the video when it was created. Then, cycle through all markers and get their times, which then becomes a level
         *  - For the audio bar, we can do a large panel, and markers are childs of it, when the playtime is at the edge, we can move the panel to the side 
         *  - We would need to add the song to AllSongs in SongSelect2, to the dictionaries in playMusic (video and texture), SongSelect2 (images), and Spawner (array)
         *    This creates an interesting problem as I would need to somehow declare the image, texture, and video and add to dictionaries
         * Optimize initializing dictionaries
        */
    }

    // Update is called once per frame
    void Update()
    {
        //Songs to Add
        /*
         * Blood in the water
         * Super Villain 
         * Whispers
         * Light it Up (Jex)
         * Flowers need rain
         * Let's Just
         * Cravin
         * Scared
         * Wonderland
         * Castle
         * Careless
         * Angel with a shotgun
         */
    }
}
