using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public float interval;
    private float max_interval;

    public Object spawnable;
    private GameObject spawned;

    // Start is called before the first frame update
    void Start()
    {
        max_interval = interval;

        spawned = (GameObject)Instantiate(spawnable, transform.position, transform.rotation);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if the object has not been spawned
        if(spawned == null)
        {
            if (interval > 0) interval -= 1.0f * Time.deltaTime;
            else
            {
                interval = max_interval;
                spawned = (GameObject)Instantiate(spawnable, transform.position, transform.rotation);
            }
        }
    }
}
