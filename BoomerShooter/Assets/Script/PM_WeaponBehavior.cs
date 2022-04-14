using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PM_WeaponBehavior : NetworkBehaviour
{
    [SyncVar(hook = nameof(SetParentID))]
    public uint parent_id;

    IEnumerator ParentRoutine()
    {
        yield return new WaitUntil(() => NetworkIdentity.spawned.ContainsKey(parent_id));
        GameObject parent_object = NetworkIdentity.spawned[parent_id].gameObject;

        transform.parent = parent_object.GetComponent<AnimatePlayer>().weapon_spawn;
        Debug.Log(parent_id);
    }
    
    public void SetId(uint id) { Debug.Log(id); SetIdCmd(id); }

    [Command]
    public void SetIdCmd(uint id) { parent_id = id; }

    public void SetParentID(uint old_id, uint new_id)
    {
        Debug.Log("ocurring");
        parent_id = new_id;
    }

    public override void OnStartClient()
    {
        StartCoroutine(ParentRoutine());
    }
}
