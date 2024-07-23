using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Trail : MonoBehaviour
{
    //Positions
    private GameObject nextNoti;
    private Vector3 startPos;
    private Vector3 finalPos;
    //GameObjects
    private GameObject thisCounter;
    private GameObject lowestCounter;
    private GameObject secondCounter;
    //Instances
    private double speed = 0.2;
    private double destroyTimer = 180.0;
    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
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
        //speed = Mathf.Sqrt(Mathf.Pow(finalPos.x - transform.position.x, 2) + Mathf.Pow(finalPos.y - transform.position.y, 2)) / (secondCounter.GetComponent<Counter>().destroyTimer);
        //Debug.Log(finalPos);
        transform.position = Vector3.MoveTowards(transform.position, finalPos, (float)speed*Time.deltaTime);
        if (transform.position == finalPos)
        {
            //Destroy(this.gameObject);
        }
        destroyTimer -= Time.deltaTime;
        if (destroyTimer<=0)
        {
            Destroy(this.gameObject);
        }
    }
}
