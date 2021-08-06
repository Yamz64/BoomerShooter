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

    private void OnDrawGizmos()
    {
        if (spawnable != null)
        {
            GameObject temp = (GameObject)spawnable;
            bool is_pickup = temp.GetComponent<PickupBehavior>() != null;

            Mesh mesh = null;
            Material mat = null;

            if (is_pickup)
            {
                PickupBehavior stats = temp.GetComponent<PickupBehavior>();
                if (stats.stats && stats.stats.weapon != null)
                {
                    if (stats.stats.weapon.model != null) mesh = stats.stats.weapon.model;
                    if (stats.stats.weapon.mats.Length > 0) mat = stats.stats.weapon.mats[0];
                    if (mat != null) mat.SetPass(0);
                    if (mesh != null) Graphics.DrawMeshNow(mesh, transform.position, transform.rotation);
                    return;
                }
            }

            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            mat = temp.GetComponent<MeshRenderer>().sharedMaterial;
            if(mat != null) mat.SetPass(0);
            if(mesh != null) Graphics.DrawMeshNow(mesh, transform.position, transform.rotation);
        }
    }
}
