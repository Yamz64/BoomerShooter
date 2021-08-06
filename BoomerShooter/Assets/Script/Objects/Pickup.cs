using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unnamed Pickup", menuName = "ScriptableObjects/Pickup", order = 1)]
public class Pickup : ScriptableObject
{
    [Tooltip("Does this pickup restore a flat amount of a stat or a percentage")]
    public bool weapon_pickup;
    public bool flat;
    public enum StatType { HEALTH, OVERHEAL, ARMOR, SUPERARMOR, BULLETS, SHELLS, EXPLOSIVES, ENERGY, MAXAMMO };
    public StatType stat;
    public float amount;
    public Weapon weapon;
}
