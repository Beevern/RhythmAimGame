using UnityEngine;

//This is the script for markers in the level maker: the chart time the marker was placed at
public class Marker : MonoBehaviour
{
    public double myTime;

    //The Marker prefab's script reference is broken, so add the component if it's missing
    public static Marker On(GameObject go)
    {
        Marker m = go.GetComponent<Marker>();
        return m != null ? m : go.AddComponent<Marker>();
    }
}
