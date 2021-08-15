using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Killbox : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            other.gameObject.GetComponent<PlayerStats>().SetHealth(0, false);
        }
    }
}
