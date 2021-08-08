using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    [Tooltip("Type of weapon in the held_weapons list: 0 = melee, 1 = pistols/light, 2 = shotguns, 3 = assault/heavy, 4 = explosives, 5 = energy, 6 = big")]
    public int active_type;
    [Tooltip("Position in the held_weapons[active_type] list")]
    public int active_weapon;
    public int max_bullet_holes;
    public float current_interval;
    public Weapon[] weapons;
    public Object bullet_decal;

    private bool firing;
    private float max_interval;
    private List<List<Weapon>> held_weapons;
    private GameObject physics_parent;
    private GameObject viewmodel;
    private GameObject shot_origin;

    private List<GameObject> bullet_holes;
    private Animator anim;
    private PlayerStats stats;

    //function to find the next perfect root that assists in building fixed shot patterns
    int NextRoot(int number)
    {
        //check if the number is inherently a perfect square if so return the root
        if(number != 0)
        {
            float root = (int)Mathf.Sqrt(number);
            if (root * root == number) return number;
        }
        int next = Mathf.FloorToInt(Mathf.Sqrt(number)) + 1;
        return next * next;
    }

    void UpdateViewmodel(Weapon weapon)
    {
        //update the the viewmodel's model
        viewmodel.GetComponent<MeshFilter>().mesh = weapon.model;
        Material[] new_mats = new Material[weapon.mats.Length];
        viewmodel.GetComponent<MeshRenderer>().materials = new_mats;
        for(int i=0; i<weapon.mats.Length; i++) { viewmodel.GetComponent<MeshRenderer>().materials[i] = weapon.mats[i]; }

        //update the viewmodel's position and shot_spawn position
        physics_parent.transform.localPosition = weapon.weapon_spawn;
        viewmodel.transform.rotation = physics_parent.transform.rotation;
        transform.GetChild(0).GetChild(1).localPosition = weapon.shot_spawn;

        current_interval = weapon.shot_interval;
        max_interval = weapon.shot_interval;

        anim.runtimeAnimatorController = weapon.anim;
    }

    //based on the number of shots and degrees of deviation spawn a sort of weapon spread
    List<Vector3> GenerateShotPattern(Weapon weapon)
    {
        List<Vector3> shot_paths = new List<Vector3>();
        float step = (float)weapon.deviation / (float)(Mathf.Sqrt(NextRoot(weapon.number_of_shots)) - 1);
        Vector3 look_dir = gameObject.GetComponent<CharacterMovement>().GetCam().transform.forward;
        Vector3 look_up = gameObject.GetComponent<CharacterMovement>().GetCam().transform.up;
        Vector3 look_right = gameObject.GetComponent<CharacterMovement>().GetCam().transform.right;

        for (int i = 0; i < weapon.number_of_shots; i++)
        {
            //get the initial position 
            Vector3 spawn_direction = Quaternion.AngleAxis(-weapon.deviation, look_up) * look_dir;
            spawn_direction = Quaternion.AngleAxis(-weapon.deviation, look_right) * spawn_direction;

            //offset the x position
            spawn_direction = Quaternion.AngleAxis(2f * step * (i % Mathf.Sqrt(NextRoot(weapon.number_of_shots))), look_up) * spawn_direction;

            //offset the y position
            int y_step = i / (int)Mathf.Sqrt(NextRoot(weapon.number_of_shots));
            spawn_direction = Quaternion.AngleAxis(2 * step * y_step, look_right) * spawn_direction;

            shot_paths.Add(spawn_direction);
        }
        return shot_paths;
    }

    IEnumerator BurstProjectile(Weapon weapon)
    {
        for (int i = 0; i < weapon.number_of_shots; i++)
        {
            Vector3 shot_direction = gameObject.GetComponent<CharacterMovement>().GetCam().transform.forward;
            Vector3 shot_up = gameObject.GetComponent<CharacterMovement>().GetCam().transform.up;
            Vector3 shot_right = gameObject.GetComponent<CharacterMovement>().GetCam().transform.right;
            shot_direction = Quaternion.AngleAxis(Random.Range(-weapon.deviation, weapon.deviation), shot_up) * shot_direction;
            shot_direction = Quaternion.AngleAxis(Random.Range(-weapon.deviation, weapon.deviation), shot_right) * shot_direction;

            GameObject projectile = (GameObject)Instantiate(weapon.projectile, shot_origin.transform.position, Quaternion.identity);
            if (i != 0)
                projectile.transform.forward = shot_direction;
            else
                projectile.transform.forward = gameObject.GetComponent<CharacterMovement>().GetCam().transform.forward;
            //handle shot direction override
            if (weapon.shot_direction.magnitude == 0)
                projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * projectile.transform.forward;
            else
            {
                Vector3 override_direction = projectile.transform.right * weapon.shot_direction.x + projectile.transform.up * weapon.shot_direction.y + projectile.transform.forward * weapon.shot_direction.z;
                shot_direction.Normalize();
                projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * override_direction;
            }
            projectile.GetComponent<Rigidbody>().rotation = projectile.transform.rotation;
            projectile.GetComponent<Rigidbody>().AddRelativeTorque(weapon.shot_torque);
            projectile.GetComponent<Rigidbody>().useGravity = weapon.use_gravity;
            projectile.GetComponent<ProjectileBehavior>().owner = gameObject;
            yield return new WaitForSeconds(weapon.shot_rate);
        }
    }

    IEnumerator BurstHitscan(Weapon weapon)
    {
        for (int i = 0; i < weapon.number_of_shots; i++)
        {
            Vector3 shot_direction = gameObject.GetComponent<CharacterMovement>().GetCam().transform.forward;
            Vector3 shot_up = gameObject.GetComponent<CharacterMovement>().GetCam().transform.up;
            Vector3 shot_right = gameObject.GetComponent<CharacterMovement>().GetCam().transform.right;
            shot_direction = Quaternion.AngleAxis(Random.Range(-weapon.deviation, weapon.deviation), shot_up) * shot_direction;
            shot_direction = Quaternion.AngleAxis(Random.Range(-weapon.deviation, weapon.deviation), shot_right) * shot_direction;
            
            //if this isn't the first shot, deviate
            if(i != 0)
            {
                RaycastHit hit = new RaycastHit();
                if(Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, shot_direction, out hit, Mathf.Infinity, ~LayerMask.GetMask("Pickup")))
                {
                    GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                    bullet_hole.transform.forward = -hit.normal;
                    bullet_hole.transform.position += hit.normal * .0001f;
                    bullet_holes.Add(bullet_hole);
                    CleanBulletHoles();
                }
            }
            else
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, GetComponent<CharacterMovement>().GetCam().transform.forward, out hit, Mathf.Infinity, ~LayerMask.GetMask("Pickup")))
                {
                    GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                    bullet_hole.transform.forward = -hit.normal;
                    bullet_hole.transform.position += hit.normal * .0001f;
                    bullet_holes.Add(bullet_hole);
                    CleanBulletHoles();
                }
            }

            yield return new WaitForSeconds(weapon.shot_rate);
        }
    }

    void CleanBulletHoles()
    {
        while(bullet_holes.Count > max_bullet_holes)
        {
            Destroy(bullet_holes[0].gameObject);
            bullet_holes.RemoveAt(0);
        }
    }

    void Fire(Weapon weapon)
    {
        //check to see if there is enough ammo to fire the weapon
        switch (weapon.weapon_type)
        {
            case 1:
                if (stats.GetBullets() < weapon.shot_cost) { firing = false; return; }
                break;
            case 2:
                if (stats.GetShells() < weapon.shot_cost) { firing = false; return; }
                break;
            case 3:
                if (stats.GetBullets() < weapon.shot_cost) { firing = false; return; }
                break;
            case 4:
                if (stats.GetExplosives() < weapon.shot_cost) { firing = false; return; }
                break;
            case 5:
                if (stats.GetEnergy() < weapon.shot_cost) { firing = false; return; }
                break;
            case 6:
                if (stats.GetEnergy() < weapon.shot_cost) { firing = false; return; }
                break;
            default:
                break;
        }

        //check to see if the weapon is ready to be fired
        if(current_interval >= 0.0f)
        {
            current_interval -= 1.0f * Time.deltaTime;
            firing = false;
            return;
        }

        //if the weapon is ready to be fired and the fire key is being pressed
        if(Input.GetButton("Fire1"))
        {
            firing = true;
            current_interval = max_interval;
            GetComponent<Rigidbody>().AddForce(-viewmodel.transform.forward * weapon.kick_back);
            //first see if the weapon is projectile or hitscan
            //projectile
            if (weapon.use_projectile)
            {
                //does the weapon multi-shot like a shotgun?
                if (weapon.multi_shot)
                {
                    //for every path generated in the shot pattern spawn a projectile and 
                    List<Vector3> shot_paths = new List<Vector3>(GenerateShotPattern(weapon));

                    for(int i=0; i<shot_paths.Count; i++)
                    {
                        GameObject projectile = (GameObject)Instantiate(weapon.projectile, shot_origin.transform.position, Quaternion.identity);
                        projectile.transform.forward = shot_paths[i];
                        //handle shot direction override
                        if(weapon.shot_direction.magnitude == 0)
                            projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * projectile.transform.forward;
                        else
                        {
                            Vector3 shot_direction = projectile.transform.right * weapon.shot_direction.x + projectile.transform.up * weapon.shot_direction.y + projectile.transform.forward * weapon.shot_direction.z;
                            shot_direction.Normalize();
                            projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * shot_direction;
                        }
                        projectile.GetComponent<Rigidbody>().rotation = projectile.transform.rotation;
                        projectile.GetComponent<Rigidbody>().AddRelativeTorque(weapon.shot_torque);
                        projectile.GetComponent<Rigidbody>().useGravity = weapon.use_gravity;
                        projectile.GetComponent<ProjectileBehavior>().owner = gameObject;
                    }
                }
                else
                {
                    //simply fire the weapon according to it's deviation
                    StartCoroutine(BurstProjectile(weapon));
                }
            }
            //hitscan
            else
            {
                //does the weapon multi-shot like a shotgun?
                //yes
                if (weapon.multi_shot)
                {
                    List<Vector3> shot_paths = new List<Vector3>(GenerateShotPattern(weapon));

                    for(int i=0; i<shot_paths.Count; i++)
                    {
                        //raycast along all paths in the shot_paths
                        RaycastHit hit = new RaycastHit();
                        if(Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, shot_paths[i], out hit, Mathf.Infinity, ~LayerMask.GetMask("Pickup")))
                        {
                            //if the bullet hits a wall then spawn a bullet hole
                            if(hit.collider.gameObject.layer == 0)
                            {
                                GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                                bullet_hole.transform.forward = -hit.normal;
                                bullet_hole.transform.position += hit.normal * .0001f;
                                bullet_holes.Add(bullet_hole);
                                CleanBulletHoles();
                            }
                        }
                    }
                }
                //no
                else
                {
                    //simply fire the weapon according to it's deviation
                    StartCoroutine(BurstHitscan(weapon));
                }
            }

            //subtract the ammo cost from the reserve based on type
            switch (weapon.weapon_type)
            {
                case 1:
                    stats.SetBullets(stats.GetBullets() - weapon.shot_cost);
                    break;
                case 2:
                    stats.SetShells(stats.GetShells() - weapon.shot_cost);
                    break;
                case 3:
                    stats.SetBullets(stats.GetBullets() - weapon.shot_cost);
                    break;
                case 4:
                    stats.SetExplosives(stats.GetExplosives() - weapon.shot_cost);
                    break;
                case 5:
                    stats.SetEnergy(stats.GetEnergy() - weapon.shot_cost);
                    break;
                case 6:
                    stats.SetEnergy(stats.GetEnergy() - weapon.shot_cost);
                    break;
                default:
                    break;
            }
        }
    }

    public void Animate()
    {
        if (anim != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            CharacterMovement cm = GetComponent<CharacterMovement>();
            anim.SetBool("Fire", firing);
            anim.SetFloat("Speed", Mathf.Clamp01(rb.velocity.magnitude / cm.max_ground_accel));

            Vector3 forward_proj = (Vector3.Dot(rb.velocity / cm.max_air_accel, transform.forward) / 1f) * transform.forward;
            Vector3 right_proj = (Vector3.Dot(rb.velocity / cm.max_air_accel, transform.right) / 1f) * transform.right;
            Vector3 up_proj = (Vector3.Dot(rb.velocity / cm.max_air_accel, transform.up) / 1f) * transform.up;
            physics_parent.transform.localPosition = held_weapons[active_type][active_weapon].weapon_spawn;
            physics_parent.transform.position += (forward_proj + right_proj + up_proj) * -.1f;
        }
    }

    public bool HasWeapon(Weapon weapon)
    {
        for(int i=0; i<held_weapons.Count; i++)
        {
            for(int j=0; j<held_weapons[i].Count; j++)
            {
                if (held_weapons[i][j].weapon_name == weapon.weapon_name) return true;
            }
        }
        return false;
    }

    public void AddWeapon(Weapon weapon) { held_weapons[weapon.weapon_type].Add(weapon); }

    //function handles switching of weapons
    public void SwitchWeapon()
    {
        //scrollwheel
        if (Input.mouseScrollDelta.y != 0)
        {
            //determine whether the mousewheel is being scrolled up or down
            bool up = false;
            if (Input.mouseScrollDelta.y / Mathf.Abs(Input.mouseScrollDelta.y) == 1) up = true;

            //up
            if (up)
            {
                //find the next available weapon to switch to
                bool valid = false;
                int temp_active = active_weapon;
                int temp_type = active_type;
                while (!valid)
                {
                    temp_active++;
                    if (temp_active < held_weapons[temp_type].Count) valid = true;
                    else
                    {
                        if (temp_type != 6) temp_type++;
                        else temp_type = 0;
                        temp_active = -1;
                    }
                }

                active_weapon = temp_active;
                active_type = temp_type;
            }
            //down
            else
            {
                //find the next available weapon to switch to
                bool valid = false;
                int temp_active = active_weapon;
                int temp_type = active_type;
                while (!valid)
                {
                    temp_active--;
                    if (temp_active > -1 && held_weapons[temp_type].Count > 0) valid = true;
                    else
                    {
                        if (temp_type != 0) temp_type--;
                        else temp_type = 6;
                        temp_active = held_weapons[temp_type].Count;
                    }
                }

                active_weapon = temp_active;
                active_type = temp_type;
            }

            UpdateViewmodel(held_weapons[active_type][active_weapon]);
            stats.UpdateUI();
        }
        //keyboard
        else
        {
            int key_pressed = -1;
            if (Input.GetKeyDown(KeyCode.Alpha1)) key_pressed = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) key_pressed = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) key_pressed = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) key_pressed = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5)) key_pressed = 4;
            if (Input.GetKeyDown(KeyCode.Alpha6)) key_pressed = 5;
            if (Input.GetKeyDown(KeyCode.Alpha7)) key_pressed = 6;

            if (key_pressed == -1) return;

            if (active_type != key_pressed)
            {
                if (held_weapons[key_pressed].Count > 0)
                {
                    active_type = key_pressed;
                    active_weapon = 0;
                }
            }
            else
            {
                if (active_weapon < held_weapons[active_type].Count - 1) active_weapon++;
                else active_weapon = 0;
            }

            UpdateViewmodel(held_weapons[active_type][active_weapon]);
            stats.UpdateUI();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //load all weapons found in resources into the weapons list
        //weapons = Resources.LoadAll<Weapon>("Weapons");

        held_weapons = new List<List<Weapon>>() { new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>()
                                                };
        //find the pistol and add it to the player's held weapons
        AddWeapon(Resources.Load<Weapon>("Weapons/Pistol"));
        active_type = 1;
        active_weapon = 0;

        //load the first active weapon to the viewmodel
        physics_parent = transform.GetChild(0).GetChild(0).gameObject;
        viewmodel = physics_parent.transform.GetChild(0).gameObject;
        shot_origin = transform.GetChild(0).GetChild(1).gameObject;
        anim = viewmodel.GetComponent<Animator>();
        UpdateViewmodel(held_weapons[active_type][active_weapon]);
        stats = GetComponent<PlayerStats>();

        //initialize list for keeping track of bullet holes
        bullet_holes = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        Animate();
        Fire(held_weapons[active_type][active_weapon]);

        //handle weaponswitching
        SwitchWeapon();
    }
}
