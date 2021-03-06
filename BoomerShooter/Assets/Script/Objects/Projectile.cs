using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unnamed Projectile", menuName = "ScriptableObjects/Projectile", order = 1)]
public class Projectile : ScriptableObject
{
    public int network_index;
    public int effect_index;
    public int damage;
    public bool explosive;
    public bool die_after_other_collision;
    public bool die_on_contact;
    public bool die_after_time;
    public bool spawn_effect;
    public float mass;
    public float life_time;
    public float knockback;
    public float explosion_time;
    public float explosion_radius;
    public LayerMask collision_mask;
    public LayerMask sound_mask;
    public AudioClip collide_sound;
    public AudioClip death_sound;
    [HideInInspector]
    public Object blast_box;

    void OnEnable()
    {
        blast_box = Resources.Load("ProjectileData/BlastRadius") as GameObject;
    }
}
