using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UpdatePlayerColor : NetworkBehaviour
{
    [ClientRpc]
    public void UpdatePlayerColorRPC()
    {
        if (isLocalPlayer) transform.GetChild(2).gameObject.SetActive(true);
        Color p_color = GetComponent<PlayerStats>().GetPrimaryColor();
        Color s_color = GetComponent<PlayerStats>().GetSecondaryColor();

        GameObject player_mesh = transform.GetChild(2).GetChild(0).gameObject;
        Material[] mats = player_mesh.GetComponent<Renderer>().materials;

        //set local materials
        //bodymat
        mats[0].SetColor("_PrimaryColor", p_color);
        mats[0].SetColor("_SecondaryColor", s_color);

        //headglobemat
        mats[1].SetColor("_PrimaryColor", new Color(p_color.r, p_color.g, p_color.b, .715f));

        //skullmat
        mats[2].SetColor("_PrimaryColor", p_color);
        if (isLocalPlayer) transform.GetChild(2).gameObject.SetActive(false);
    }

    [Command]
    public void UpdateLocalPlayerColor()
    {
        if (isLocalPlayer) transform.GetChild(2).gameObject.SetActive(true);
        Color p_color = GetComponent<PlayerStats>().GetPrimaryColor();
        Color s_color = GetComponent<PlayerStats>().GetSecondaryColor();

        GameObject player_mesh = transform.GetChild(2).GetChild(0).gameObject;
        Material[] mats = player_mesh.GetComponent<Renderer>().materials;

        //set local materials
        //bodymat
        mats[0].SetColor("_PrimaryColor", p_color);
        mats[0].SetColor("_SecondaryColor", s_color);

        //headglobemat
        mats[1].SetColor("_PrimaryColor", p_color);

        //skullmat
        mats[2].SetColor("_PrimaryColor", p_color);

        //call on all clients
        if (isLocalPlayer) transform.GetChild(2).gameObject.SetActive(false);
        UpdatePlayerColorRPC();
    }
}
