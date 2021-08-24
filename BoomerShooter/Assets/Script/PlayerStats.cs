using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerStats : NetworkBehaviour
{
    [SerializeField]
    [SyncVar]
    private bool dead, interaction_lock;
    [SerializeField][SyncVar]
    private int armor, health, bullets, shells, explosives, energy;
    [SerializeField][SyncVar]
    private int max_armor, max_health, max_bullets, max_shells, max_explosives, max_energy, armor_type;
    [SyncVar]
    private float overheal_decay_delay;

    public GameObject canvas;

    private Image crosshair, health_bar, overheal_bar, armor_bar;
    private Text health_text, armor_text, current_ammo_text, bullet_text, shell_text, explosive_text, energy_text, death_text;

    //--ACCESSORS--
    public bool GetDead() { return dead; }
    public bool GetInteractionLock() { return interaction_lock; }
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
    [ClientCallback]
    public void SetDead(bool d) { dead = d; }
    [ClientCallback]
    public void SetInteractionLock(bool i) { interaction_lock = i; }
    [ClientCallback]
    public void SetArmor(int a) { armor = a; if (armor < 0) armor = 0; if (armor > max_armor) armor = max_armor; if (armor == 0) SetArmorType(0); }
    
    [ClientCallback]
    public void SetHealth(int h, bool overheal)
    {
        health = h;
        if (health < 0)
        {
            dead = true;
            health = 0;
        }
        if (health == 0) dead = true;
        if (!overheal)
        {
            if (health > max_health) health = max_health;
        }
        else
        {
            if (health > (float)max_health * 1.5) health = (int)((float)max_health * 1.5f);
        }
    }

    [ClientCallback]
    public void SetBullets(int b) { bullets = b; if (bullets > max_bullets) bullets = max_bullets; }
    [ClientCallback]
    public void SetShells(int s) { shells = s; if (shells > max_shells) shells = max_shells; }
    [ClientCallback]
    public void SetExplosives(int e) { explosives = e; if (explosives > max_explosives) explosives = max_explosives; }
    [ClientCallback]
    public void SetEnergy(int e) { energy = e; if (energy > max_energy) energy = max_energy; }
    [ClientCallback]
    public void SetMaxArmor(int a) { max_armor = a; }
    [ClientCallback]
    public void SetMaxHealth(int h) { max_health = h; }
    [ClientCallback]
    public void SetMaxBullets(int b) { max_bullets = b; }
    [ClientCallback]
    public void SetMaxShells(int s) { max_shells = s; }
    [ClientCallback]
    public void SetMaxExplosives(int e) { max_explosives = e; }
    [ClientCallback]
    public void SetMaxEnergy(int e) { max_energy = e; }
    [ClientCallback]
    public void SetArmorType(int a)
    {
        if (a > 2 || a < 0) {
            Debug.LogError($"Armor type argument must be of type 0: none, 1: armor, or 2: super armor - {a}, is an invalid argument!");
            return;
        }
        armor_type = a;
    }

    //--MISC--  
    [ClientCallback]
    public void Respawn()
    {
        //get a list of all spawn locations on the server
        GameObject[] spawn_points = GameObject.FindGameObjectsWithTag("Respawn");

        //set the player's position to a random spawnpoint reset the player's health and ammo to default values and resurrect them
        transform.position = spawn_points[Random.Range(0, spawn_points.Length)].transform.position;

        health = max_health = 150;
        armor_type = 0;
        armor = 0;
        max_armor = 100;

        shells = explosives = energy = 0;
        bullets = max_bullets = max_shells = max_explosives = max_energy = 50;

        GetComponent<WeaponBehavior>().ClearWeapons();
        dead = false;
        StartCoroutine(LateStart());
    }

    //Update all UI except for the crosshair
    public void UpdateUI()
    {
        if (isLocalPlayer)
        {
            //health
            health_text.text = $"{health}/{max_health}";
            health_bar.fillAmount = Mathf.Clamp01((float)health / (float)(max_health));
            overheal_bar.fillAmount = Mathf.Clamp01(((float)health - (float)max_health) / ((float)max_health * 1.5f - (float)max_health));
            health_bar.color = Color.Lerp(new Color(1f, 0.0f, 0.0f, .5f), new Color(0.0f, 1.0f, 0.0f, .5f), Mathf.Clamp01((float)health / (float)max_health));

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

            //death text
            death_text.enabled = dead;
        }
    }

    IEnumerator LateStart()
    {
        yield return new WaitForEndOfFrame();
        UpdateUI();
    }

    //function will decay overheal over time
    void UpdateOverheal()
    {
        //do nothing if not overhealed
        if (health <= max_health) return;

        //find the step required to decay max overheal in 15 seconds
        int step = (int)((((float)max_health * .5f)/15f) * .5f);

        if (overheal_decay_delay > 0.0f) overheal_decay_delay -= 1.0f * Time.deltaTime;
        else
        {
            overheal_decay_delay = .5f;

            //take precautions so that losing overheal will never put you below max hp
            if (health - step < max_health) SetHealth(max_health, false);
            else SetHealth(health - step, true);
        }

    }

    public override void OnStartClient()
    {
        if (isClient)
        {
            armor = shells = explosives = energy = 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isClient)
        {
            //initialize UI variables
            crosshair = canvas.transform.GetChild(0).GetComponent<Image>();
            health_text = canvas.transform.GetChild(1).GetComponent<Text>();
            health_bar = health_text.transform.GetChild(0).GetComponent<Image>();
            overheal_bar = health_text.transform.GetChild(1).GetComponent<Image>();
            armor_text = canvas.transform.GetChild(2).GetComponent<Text>();
            armor_bar = armor_text.transform.GetChild(0).GetComponent<Image>();
            current_ammo_text = canvas.transform.GetChild(3).GetComponent<Text>();
            bullet_text = canvas.transform.GetChild(4).GetComponent<Text>();
            shell_text = canvas.transform.GetChild(5).GetComponent<Text>();
            explosive_text = canvas.transform.GetChild(6).GetComponent<Text>();
            energy_text = canvas.transform.GetChild(7).GetComponent<Text>();
            death_text = canvas.transform.GetChild(8).GetComponent<Text>();

            SetDead(false);
            StartCoroutine(LateStart());
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            UpdateOverheal();
            UpdateUI();
            if (health <= 0) dead = true;
            if (dead && Input.anyKeyDown) Respawn();
        }
        if (isClient)
        {
            if (dead)
            {
                GetComponent<Collider>().enabled = false;
                GetComponent<Rigidbody>().useGravity = false;
            }
            else
            {
                GetComponent<Collider>().enabled = true;
                GetComponent<Rigidbody>().useGravity = true;
            }
        }
    }
}
