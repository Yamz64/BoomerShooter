using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupBehavior : MonoBehaviour
{
    public Pickup stats;
    public AudioClip sound;
    private MeshRenderer rend;
    private MeshFilter filter;

    public IEnumerator LateArmor(PlayerStats player)
    {
        yield return new WaitForEndOfFrame();
        if (stats.flat) player.SetArmor(player.GetArmor() + (int)stats.amount);
        else player.SetArmor(player.GetArmor() + (int)((float)player.GetMaxArmor() * Mathf.Clamp01(stats.amount)));
    }

    public IEnumerator LateDestroy()
    {
        GetComponent<Collider>().enabled = false;
        if (sound != null) PlayerAudioHandler.PlaySoundAtPoint(sound, transform.position);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }

    void RestoreStat(PlayerStats player)
    {
        bool restored = false;
        switch (stats.stat)
        {
            case Pickup.StatType.HEALTH:
                if (player.GetHealth() < player.GetMaxHealth())
                {
                    restored = true;
                    if (stats.flat) player.SetHealth(player.GetHealth() + (int)stats.amount, false);
                    else player.SetHealth(player.GetHealth() + (int)((float)player.GetMaxHealth() * Mathf.Clamp01(stats.amount)), false);
                }
                break;
            case Pickup.StatType.OVERHEAL:
                if (player.GetHealth() < player.GetMaxHealth() * 1.5f)
                {
                    restored = true;
                    if (stats.flat) player.SetHealth(player.GetHealth() + (int)stats.amount, true);
                    else player.SetHealth(player.GetHealth() + (int)((float)player.GetMaxHealth() * Mathf.Clamp01(stats.amount)), true);
                }
                break;
            case Pickup.StatType.ARMOR:
                if (player.GetArmor() < 100)
                {
                    restored = true;
                    player.SetArmorType(1);
                    player.SetMaxArmor(100);
                }
                StartCoroutine(LateArmor(player));
                break;
            case Pickup.StatType.SUPERARMOR:
                if (player.GetArmor() < 200)
                {
                    restored = true;
                    player.SetArmorType(2);
                    player.SetMaxArmor(200);
                }
                StartCoroutine(LateArmor(player));
                break;
            case Pickup.StatType.BULLETS:
                if (player.GetBullets() < player.GetMaxBullets()) restored = true;
                if (stats.flat) player.SetBullets(player.GetBullets() + (int)stats.amount);
                else player.SetBullets(player.GetBullets() + (int)((float)player.GetMaxBullets() * Mathf.Clamp01(stats.amount)));
                break;
            case Pickup.StatType.SHELLS:
                if (player.GetShells() < player.GetMaxShells()) restored = true;
                if (stats.flat) player.SetShells(player.GetShells() + (int)stats.amount);
                else player.SetShells(player.GetShells() + (int)((float)player.GetMaxShells() * Mathf.Clamp01(stats.amount)));
                break;
            case Pickup.StatType.EXPLOSIVES:
                if (player.GetExplosives() < player.GetMaxExplosives()) restored = true;
                if (stats.flat) player.SetExplosives(player.GetExplosives() + (int)stats.amount);
                else player.SetExplosives(player.GetExplosives() + (int)((float)player.GetMaxExplosives() * Mathf.Clamp01(stats.amount)));
                break;
            case Pickup.StatType.ENERGY:
                if (player.GetEnergy() < player.GetMaxEnergy()) restored = true;
                if (stats.flat) player.SetEnergy(player.GetEnergy() + (int)stats.amount);
                else player.SetEnergy(player.GetEnergy() + (int)((float)player.GetMaxEnergy() * (int)Mathf.Clamp01(stats.amount)));
                break;
            case Pickup.StatType.MAXAMMO:
                if (player.GetMaxBullets() < stats.amount || player.GetMaxShells() < stats.amount || player.GetMaxExplosives() < stats.amount || player.GetMaxEnergy() < stats.amount)
                {
                    restored = true;
                    player.SetMaxBullets((int)stats.amount);
                    player.SetMaxShells((int)stats.amount);
                    player.SetMaxExplosives((int)stats.amount);
                    player.SetMaxEnergy((int)stats.amount);
                }
                if(player.GetBullets() < player.GetMaxBullets() || player.GetShells() < player.GetMaxShells() || player.GetExplosives() < player.GetMaxExplosives() || player.GetEnergy() < player.GetMaxEnergy())
                {
                    restored = true;
                    player.SetBullets(player.GetBullets() + (int)stats.amount / 2);
                    player.SetShells(player.GetShells() + (int)stats.amount / 2);
                    player.SetExplosives(player.GetExplosives() + (int)stats.amount / 2);
                    player.SetEnergy(player.GetEnergy() + (int)stats.amount / 2);
                }
                break;
            case Pickup.StatType.ALLMAX:
                if (player.GetHealth() < (int)((float)player.GetMaxHealth() * 1.5f) || player.GetArmor() < 200 || player.GetMaxBullets() < stats.amount || player.GetMaxShells() < stats.amount
                    || player.GetMaxExplosives() < stats.amount || player.GetMaxEnergy() < stats.amount)
                {
                    restored = true;
                    player.SetHealth((int)((float)player.GetMaxHealth() * 1.5f), true);
                    player.SetArmorType(2);
                    player.SetMaxArmor(200);
                    player.SetMaxBullets((int)stats.amount);
                    player.SetMaxShells((int)stats.amount);
                    player.SetMaxExplosives((int)stats.amount);
                    player.SetMaxEnergy((int)stats.amount);
                }
                break;
            default:
                break;
        }
        if (restored) { StartCoroutine(LateDestroy()); }
    }

    void AddWeapon(WeaponBehavior player)
    {
        if (stats.weapon != null)
        {
            if (!player.HasWeapon(stats.weapon))
            {
                player.GetComponent<WeaponBehavior>().AddWeapon(stats.weapon);
                PlayerAudioHandler.PlayVoiceClipAtPoint(11, 7, transform.position, 1f, false);
                Destroy(gameObject);
            }
            //if the player does have the weapon already, see if the player's ammo count for this weapon isn't full and use the pickup as ammo
            bool replenished = false;
            PlayerStats player_stats = player.gameObject.GetComponent<PlayerStats>();

            switch (stats.weapon.weapon_type)
            {
                case 1:
                    if (player_stats.GetBullets() < player_stats.GetMaxBullets())
                    {
                        replenished = true;
                        player_stats.SetBullets(player_stats.GetBullets() + (int)stats.amount);
                    }
                    break;
                case 2:
                    if (player_stats.GetShells() < player_stats.GetMaxShells())
                    {
                        replenished = true;
                        player_stats.SetShells(player_stats.GetShells() + (int)stats.amount);
                    }
                    break;
                case 3:
                    if (player_stats.GetBullets() < player_stats.GetMaxBullets())
                    {
                        replenished = true;
                        player_stats.SetBullets(player_stats.GetBullets() + (int)stats.amount);
                    }
                    break;
                case 4:
                    if (player_stats.GetExplosives() < player_stats.GetMaxExplosives())
                    {
                        replenished = true;
                        player_stats.SetExplosives(player_stats.GetExplosives() + (int)stats.amount);
                    }
                    break;
                case 5:
                    if (player_stats.GetEnergy() < player_stats.GetMaxEnergy())
                    {
                        replenished = true;
                        player_stats.SetEnergy(player_stats.GetEnergy() + (int)stats.amount);
                    }
                    break;
                case 6:
                    if (player_stats.GetEnergy() < player_stats.GetMaxEnergy())
                    {
                        replenished = true;
                        player_stats.SetEnergy(player_stats.GetEnergy() + (int)stats.amount);
                    }
                    break;
                default:
                    break;
            }
            if (replenished) Destroy(gameObject);
            
        }
        else Debug.LogError("Weapon pickup is null!");

    }

    void RecursiveSetLayer(GameObject g)
    {
        g.layer = LayerMask.NameToLayer("Default");
        if (g.transform.childCount == 0) return;
        else
        {
            for(int i=0; i<g.transform.childCount; i++)
            {
                RecursiveSetLayer(g.transform.GetChild(i).gameObject);
            }
        }
    }

    private void Start()
    {
        rend = GetComponent<MeshRenderer>();
        filter = GetComponent<MeshFilter>();

        //initialize unique mesh at starting frame if it is a weapon pickup
        if (stats.weapon_pickup)
        {
            /*
            filter.mesh = stats.weapon.model;
            rend.materials = new Material[stats.weapon.mats.Length];
            for(int i=0; i<stats.weapon.mats.Length; i++)
            {
                rend.materials[i] = stats.weapon.mats[i];
            }
            */

            GameObject visual = (GameObject)Instantiate(stats.weapon.view_model, transform);
            visual.transform.localScale = Vector3.one * .5f;
            Destroy(visual.GetComponent<Animator>());
            RecursiveSetLayer(visual);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (stats.weapon_pickup)
            {
                if (sound != null) PlayerAudioHandler.PlaySoundAtPoint(sound, transform.position);
                AddWeapon(other.GetComponent<WeaponBehavior>());
            }
            else RestoreStat(other.GetComponent<PlayerStats>());
        }
    }

    private void OnDrawGizmos()
    {
        if (stats == null) return;
        if (stats.weapon_pickup && stats.weapon != null)
        {
            Mesh mesh = null;
            Material mat = null;
            if(stats.weapon.model != null) mesh = stats.weapon.model;
            if(stats.weapon.mats.Length > 0) mat = stats.weapon.mats[0];
            if (mat != null) mat.SetPass(0);
            if (mesh != null) Graphics.DrawMeshNow(mesh, transform.position, transform.rotation);
        }
    }
}
