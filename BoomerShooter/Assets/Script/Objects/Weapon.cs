using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unnamed Weapon", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    //is this weapon hitscan or projectile
    [Tooltip("Type of weapon: 0 = melee, 1 = pistols/light, 2 = shotguns, 3 = assault/heavy, 4 = explosives, 5 = energy, 6 = big")]
    public int weapon_type;
    [Tooltip("How much ammo does it require to fire this weapon once?")]
    public int shot_cost;
    [Tooltip("Only applies to hitscan weapons, projectiles use their own damage property!")]
    public int damage;
    public bool use_projectile;
    public bool multi_shot;
    public bool use_gravity;
    public int number_of_shots;
    public float deviation;
    public float distance;
    [Tooltip("If this weapon is a projectile weapon, how fast does the projectile travel")]
    public float shot_speed;
    [Tooltip("If this weapon is a burst type weapon, how fast inbetween each bullet of a burst")]
    public float shot_rate;
    [Tooltip("How fast inbetween each attack interval of a weapon")]
    public float shot_interval;
    public float kick_back;
    public string weapon_name;
    public Vector3 weapon_spawn;
    public Vector3 shot_spawn;
    [Tooltip("Override for shot direction of projectile weapons, if set to (0, 0, 0) this member is ignored")]
    public Vector3 shot_direction;
    [Tooltip("Torque added to projectile weapons")]
    public Vector3 shot_torque;
    public Mesh model;
    [SerializeField]
    public Material[] mats;
    public Object projectile;
    public RuntimeAnimatorController anim;
}
