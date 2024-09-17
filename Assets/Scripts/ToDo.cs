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
         * Times only increment each 0.04
         * A non 0 or 5 scale results in off scale (eg 300 is not correct)
         * Fix timebox at the end it's a bit short
         * Custom sprites
         * Self adding levels?
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
