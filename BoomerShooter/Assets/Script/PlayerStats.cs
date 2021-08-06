using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    [SerializeField]
    private int armor, health, bullets, shells, explosives, energy;
    private int max_armor, max_health, max_bullets, max_shells, max_explosives, max_energy, armor_type;

    public GameObject canvas;

    private Image crosshair, health_bar, armor_bar;
    private Text health_text, armor_text, current_ammo_text, bullet_text, shell_text, explosive_text, energy_text;

    //--ACCESSORS--
    public int GetArmor() { return armor; }
    public int GetHealth() { return health; }
    public int GetBullets() { return bullets; }
    public int GetShells() { return shells; }
    public int GetExplosives() { return explosives; }
    public int GetEnergy() { return energy; }
    public int GetMaxArmor() { return max_armor; }
    public int GetMaxHealth() { return max_health; }
    public int GetMaxBullets() { return max_bullets; }
    public int GetMaxShells() { return max_shells; }
    public int GetMaxExplosives() { return max_explosives; }
    public int GetMaxEnergy() { return max_energy; }
    public int GetArmorType() { return armor_type; }

    //--MODIFIERS--
    public void SetArmor(int a) { armor = a; if (armor > max_armor) armor = max_armor; if (armor == 0) SetArmorType(0); UpdateUI(); }
    public void SetHealth(int h) { health = h; if (health > max_health) health = max_health; UpdateUI(); }
    public void SetBullets(int b) { bullets = b; if (bullets > max_bullets) bullets = max_bullets; UpdateUI(); }
    public void SetShells(int s) { shells = s; if (shells > max_shells) shells = max_shells; UpdateUI(); }
    public void SetExplosives(int e) { explosives = e; if (explosives > max_explosives) explosives = max_explosives; UpdateUI(); }
    public void SetEnergy(int e) { energy = e; if (energy > max_energy) energy = max_energy; UpdateUI(); }
    public void SetMaxArmor(int a) { max_armor = a; UpdateUI(); }
    public void SetMaxHealth(int h) { max_health = h; UpdateUI(); }
    public void SetMaxBullets(int b) { max_bullets = b; UpdateUI(); }
    public void SetMaxShells(int s) { max_shells = s; UpdateUI(); }
    public void SetMaxExplosives(int e) { max_explosives = e; UpdateUI(); }
    public void SetMaxEnergy(int e) { max_energy = e; UpdateUI(); }
    public void SetArmorType(int a)
    {
        if (a > 2 || a < 0) {
            Debug.LogError($"Armor type argument must be of type 0: none, 1: armor, or 2: super armor - {a}, is an invalid argument!");
            return;
        }
        armor_type = a;
    }

    //Update all UI except for the crosshair
    public void UpdateUI()
    {
        //health
        health_text.text = $"{health}/{max_health}";
        health_bar.fillAmount = Mathf.Clamp01((float)health / (float)(max_health));
        if (health > max_health) health_bar.color = Color.Lerp(new Color(0.0f, 1f, 0.0f, .5f), new Color(1f, 1f, 1f, .5f), (float)health / ((float)max_health * 1.5f));
        else health_bar.color = Color.Lerp(new Color(1f, 0.0f, 0.0f, .5f), new Color(0.0f, 1.0f, 0.0f, .5f), (float)health / (float)max_health);

        //armor
        armor_text.text = $"{armor}/{max_armor}";
        armor_bar.fillAmount = Mathf.Clamp01((float)armor / (float)(max_armor));
        if (armor_type == 0 || armor_type == 1) armor_bar.color = armor_text.color = new Color(0.0f, 1f, 1f, .5f);
        else armor_bar.color = armor_text.color = new Color(1f, 1f, 0.0f, .5f);

        //current ammunition
        int active_type = GetComponent<WeaponBehavior>().active_type;
        switch (active_type)
        {
            case 0:
                current_ammo_text.text = "∞";
                break;
            case 1:
                current_ammo_text.text = $"{bullets}/{max_bullets}";
                break;
            case 2:
                current_ammo_text.text = $"{shells}/{max_shells}";
                break;
            case 3:
                current_ammo_text.text = $"{bullets}/{max_bullets}";
                break;
            case 4:
                current_ammo_text.text = $"{explosives}/{max_explosives}";
                break;
            case 5:
                current_ammo_text.text = $"{energy}/{max_energy}";
                break;
            case 6:
                current_ammo_text.text = $"{energy}/{max_energy}";
                break;
            default:
                break;
        }

        //bullets
        bullet_text.text = $"Bullets: {bullets}/{max_bullets}";

        //shells
        shell_text.text = $"Shells: {shells}/{max_shells}";

        //explosives
        explosive_text.text = $"Explosives: {explosives}/{max_explosives}";

        //energy
        energy_text.text = $"Energy: {energy}/{max_energy}";
    }

    IEnumerator LateStart()
    {
        yield return new WaitForEndOfFrame();
        UpdateUI();
    }

    // Start is called before the first frame update
    void Start()
    {
        //handle stat members
        max_armor = armor;
        max_health = health;
        max_bullets = bullets;
        max_shells = shells;
        max_explosives = explosives;
        max_energy = energy;

        armor = shells = explosives = energy = 0;

        //initialize UI variables
        crosshair = canvas.transform.GetChild(0).GetComponent<Image>();
        health_text = canvas.transform.GetChild(1).GetComponent<Text>();
        health_bar = health_text.transform.GetChild(0).GetComponent<Image>();
        armor_text = canvas.transform.GetChild(2).GetComponent<Text>();
        armor_bar = armor_text.transform.GetChild(0).GetComponent<Image>();
        current_ammo_text = canvas.transform.GetChild(3).GetComponent<Text>();
        bullet_text = canvas.transform.GetChild(4).GetComponent<Text>();
        shell_text = canvas.transform.GetChild(5).GetComponent<Text>();
        explosive_text = canvas.transform.GetChild(6).GetComponent<Text>();
        energy_text = canvas.transform.GetChild(7).GetComponent<Text>();

        StartCoroutine(LateStart());
    }
}
