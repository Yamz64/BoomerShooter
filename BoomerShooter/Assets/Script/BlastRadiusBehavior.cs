using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlastRadiusBehavior : MonoBehaviour
{
    [HideInInspector]
    public float knock_back;

    IEnumerator BlastDeath()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(BlastDeath());
    }

    private void OnTriggerStay(Collider other)
    {
        if(LayerMask.GetMask("Player") == (LayerMask.GetMask("Player") | 1 << other.gameObject.layer))
        {
            float falloff_factor = Mathf.Clamp(1f - (Vector3.Distance(other.gameObject.transform.position, gameObject.transform.position) / GetComponent<SphereCollider>().radius), 0f, Mathf.Infinity);
            Debug.Log(falloff_factor);
            other.GetComponent<Rigidbody>().AddForce((other.gameObject.transform.position - transform.position).normalized * knock_back * falloff_factor);
        }
    }
}
