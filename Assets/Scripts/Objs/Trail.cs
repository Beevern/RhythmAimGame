using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Trail : MonoBehaviour
{
    //Positions
    private Vector3 finalPos;
    //GameObjects
    private GameObject lowestCounter;
    private GameObject secondCounter;
    //Instances
    private double speed;
    // Start is called before the first frame update
    void Start()
    {
        if (GameObject.FindGameObjectsWithTag("Counter") != null)
        {

            lowestCounter = GameObject.FindGameObjectsWithTag("Counter")[0];
            secondCounter = GameObject.FindGameObjectsWithTag("Counter")[1];
            finalPos = secondCounter.transform.position;
            speed = Mathf.Sqrt(Mathf.Pow(finalPos.x - transform.position.x, 2) + Mathf.Pow(finalPos.y - transform.position.y, 2)) / (secondCounter.GetComponent<Counter>().destroyTimer);
        }
    }
    void Update()
    {
        Debug.Log(finalPos);
        transform.position = Vector3.MoveTowards(transform.position, finalPos, (float)speed*Time.deltaTime);
        if (transform.position == finalPos)
        {
            Destroy(this.gameObject);
        }
    }
}
