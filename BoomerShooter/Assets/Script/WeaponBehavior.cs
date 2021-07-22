using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    public Weapon[] weapons;

    private List<Weapon> held_weapons;
    private GameObject viewmodel;

    void UpdateViewmodel(Weapon weapon)
    {
        //update the the viewmodel's model
        viewmodel.GetComponent<MeshFilter>().mesh = weapon.model;
        Material[] new_mats = new Material[weapon.mats.Length];
        viewmodel.GetComponent<MeshRenderer>().materials = new_mats;
        for(int i=0; i<weapon.mats.Length; i++) { viewmodel.GetComponent<MeshRenderer>().materials[i] = weapon.mats[i]; }

        //update the viewmodel's position and shot_spawn position
        viewmodel.transform.localPosition = new Vector3(0.0f, -.5f, 1f) + weapon.weapon_spawn;
        viewmodel.transform.GetChild(0).localPosition = weapon.shot_spawn;
    }

    // Start is called before the first frame update
    void Start()
    {
        //load all weapons found in resources into the weapons list
        weapons = Resources.LoadAll<Weapon>("Weapons");

        held_weapons = new List<Weapon>();
        held_weapons.Add(weapons[0]);

        //load the first active weapon to the viewmodel
        viewmodel = transform.GetChild(0).GetChild(0).gameObject;
        UpdateViewmodel(held_weapons[0]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
