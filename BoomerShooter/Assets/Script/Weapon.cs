using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unnamed Weapon", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    //is this weapon hitscan or projectile
    public bool use_projectile;
    public bool multi_shot;
    public bool use_gravity;
    public int number_of_shots;
    public float deviation;
    public float distance;
    public float shot_speed;
    public float shot_rate;
    public float shot_interval;
    public string weapon_name;
    public Vector3 weapon_spawn;
    public Vector3 shot_spawn;
    public Mesh model;
    [SerializeField]
    public Material[] mats;
    public Object projectile;
    public RuntimeAnimatorController anim;
}
