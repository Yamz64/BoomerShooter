using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unnamed Weapon", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    //is this weapon hitscan or projectile
    public bool use_projectile;
    public bool use_gravity;
    public float distance;
    public float duration;
    public string name;
    public Vector3 weapon_spawn;
    public Vector3 shot_spawn;
    public Mesh model;
    [SerializeField]
    public Material[] mats;
    public Object projectile;
}
