using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPadBehavior : MonoBehaviour
{
    public float launch_power;
    private Transform origin;
    private Transform end;

    private void Start()
    {
        origin = transform.GetChild(2);
        end = transform.GetChild(3);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.GetComponent<Rigidbody>().velocity = (end.position - origin.position) *launch_power;
        }
    }
}
