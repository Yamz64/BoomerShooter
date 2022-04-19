using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PM_WeaponBehavior : NetworkBehaviour
{
    [SyncVar]
    public GameObject owner;
    [SyncVar]
    public Vector3 position;
    [SyncVar]
    public Quaternion rotation;
    [SyncVar]
    public Vector3 scale;

    private void Start()
    {
        transform.parent = owner.GetComponent<AnimatePlayer>().weapon_spawn;
        transform.localPosition = position;
        transform.localRotation = rotation;
        transform.localScale = scale;
    }
}
