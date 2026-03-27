using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
public class Target : MonoBehaviour
{
    //Instance
    public float lifeTime = 0f;
    //Other Objs
    public GameObject scoreDesc;
    //Sprites
    public Sprite Circle;
    public Sprite Goonswarm;
    public Sprite Align;
    //Materials
    public Material reg;
    public Material nBlue;
    // Start is called before the first frame update
    void Start()
    {
        ScoreKeeper.notesGiven++;
        lifeTime = 0f;
        //changeSprite();
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime += Time.deltaTime;
        
    }

    //Destroy if hit by player
    public static void manualDestroy()
    {
        ScoreKeeper.noteHits++;
        ScoreKeeper.combo++;
        //Why not "this"?
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }
    public void naturalDestroy()
    {
        ScoreKeeper.combo = 0;
        GameObject scoreDesc1 = Instantiate(scoreDesc, GameObject.FindGameObjectWithTag("Target").GetComponent<Transform>().position, Quaternion.identity);
        scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = 0;
        //Why not "this"?
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }

    //Change sprite for certain levels?
    public void changeSprite()
    {

        if (SongSelect2.theSong.Equals("We're Only Goonswarm by Srsli"))
        {
            /*
            Destroy(this.gameObject.GetComponent<MeshRenderer>());
            Destroy(this.gameObject.GetComponent<MeshFilter>());
            this.gameObject.AddComponent<SpriteRenderer>();
            GetComponent<SpriteRenderer>().sprite = Goonswarm;
            */
            GetComponent<SpriteRenderer>().sprite = Goonswarm;
            GetComponent<Renderer>().material = reg;
        }
        else if (SongSelect2.theSong.Equals("How to Stay Aligned by Cearul"))
        {
            GetComponent<SpriteRenderer>().sprite = Align;
            GetComponent<Renderer>().material = reg;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = Circle;
            GetComponent<Renderer>().material = nBlue;
        }
    }
}
