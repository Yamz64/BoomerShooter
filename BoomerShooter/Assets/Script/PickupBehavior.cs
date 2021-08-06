using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupBehavior : MonoBehaviour
{
    public Pickup stats;

    void RestoreStat(PlayerStats player)
    {
        bool restored = false;
        switch (stats.stat)
        {
            case Pickup.StatType.HEALTH:
                if (player.GetHealth() < player.GetMaxHealth())
                {
                    restored = true;
                    if (stats.flat) player.SetHealth(player.GetHealth() + (int)stats.amount);
                    else player.SetHealth(player.GetHealth() + (int)((float)player.GetMaxHealth() * Mathf.Clamp01(stats.amount)));
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
                if (player.GetArmor() < player.GetMaxArmor())
                {
                    restored = true;
                    player.SetArmorType(1);
                    player.SetMaxArmor(100);
                }
                if (stats.flat) player.SetArmor(player.GetArmor() + (int)stats.amount);
                else player.SetArmor(player.GetArmor() + (int)((float)player.GetMaxArmor() * Mathf.Clamp01(stats.amount)));
                break;
            case Pickup.StatType.SUPERARMOR:
                if (player.GetArmor() < 200)
                {
                    restored = true;
                    player.SetArmorType(2);
                    player.SetMaxArmor(200);
                }
                if (stats.flat) player.SetArmor(player.GetArmor() + (int)stats.amount);
                else player.SetArmor(player.GetArmor() + (int)((float)player.GetMaxArmor() * Mathf.Clamp01(stats.amount)));
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
                if (player.GetMaxBullets() < stats.amount) restored = true;
                player.SetMaxBullets((int)stats.amount);
                player.SetMaxShells((int)stats.amount);
                player.SetMaxExplosives((int)stats.amount);
                player.SetMaxEnergy((int)stats.amount);
                break;
            default:
                break;
        }
        if (restored) { Destroy(gameObject); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            RestoreStat(other.GetComponent<PlayerStats>());
        }
    }
}
