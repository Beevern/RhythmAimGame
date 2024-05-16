using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToDo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //In Order Of Importance
        /*
         * Introduce prolonged notes
         *  I can do this by creating markers and adding them to a separate array every 2nd marker would be the end to the first one.
         *  In game, I can have a prefab that starts normal size, and decreases until reaches 0 size. The colour would be different to signal that is prolonged note.
         *  The player would hold down right click to hit the prolonged note, holding it down as long as the prolonged note has a non-zero size. 
         *  Points would be added as long as right click is being held and the cursor was initially on the prolonged note. Points added every ms starting at spawn up to 500*combo.
         * Sorting songs by stuff (name/artist/length) (We can do this by creating a dropdown with TMP Dropdown component, and writing a script which sorts by respective value, and 
           make a hashmap for the "current" var and song names)
         * Vfx 
         *  Tell which beats need vfx by hardcoding. Play animation when hit???
         * Fix Score Desc
         * Fix Click&Hover sounds
         * Fix Countdown after unpausing
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
