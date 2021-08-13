using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BlastRadiusBehavior : NetworkBehaviour
{
    [HideInInspector]
    public float knock_back;
    [HideInInspector]
    public float damage;

    [HideInInspector]
    public GameObject owner;

    private List<GameObject> damaged_objects;

    IEnumerator BlastDeath()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }

    public void AddObject(ref GameObject g_object)
    {
        if (damaged_objects == null) damaged_objects = new List<GameObject>();
        damaged_objects.Add(g_object);
    }

    public bool DealtDamage(ref GameObject g_object)
    {
        foreach(GameObject temp in damaged_objects)
        {
            if (temp == g_object) return true;
        }
        return false;
    }

    private void Start()
    {
        StartCoroutine(BlastDeath());
        if(damaged_objects == null)
        damaged_objects = new List<GameObject>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (LayerMask.GetMask("Player") == (LayerMask.GetMask("Player") | 1 << other.gameObject.layer))
        {
            float falloff_factor = Mathf.Clamp(1f - (Vector3.Distance(other.gameObject.transform.position, gameObject.transform.position) / GetComponent<SphereCollider>().radius), 0f, Mathf.Infinity);
            other.GetComponent<Rigidbody>().AddForce((other.gameObject.transform.position - transform.position).normalized * knock_back * falloff_factor);
            GameObject temp = other.gameObject;
            if (!DealtDamage(ref temp))
            {
                owner.GetComponent<WeaponBehavior>().DealDamage(other.gameObject, (int)(damage * falloff_factor));
                damaged_objects.Add(other.gameObject);
            }
        }
    }
}
