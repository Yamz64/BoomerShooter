using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WeaponBehavior : NetworkBehaviour
{
    [Tooltip("Type of weapon in the held_weapons list: 0 = melee, 1 = pistols/light, 2 = shotguns, 3 = assault/heavy, 4 = explosives, 5 = energy, 6 = big")]
    public int active_type;
    [Tooltip("Position in the held_weapons[active_type] list")]
    public int active_weapon;
    public int max_bullet_holes;
    public float current_interval;
    public Weapon[] weapons;
    public Object bullet_decal;

    [SyncVar]
    private int id;
    private bool firing;
    private float max_interval;
    [SyncVar]
    private string active_weapon_name;
    private List<List<Weapon>> held_weapons;
    private GameObject physics_parent;
    private GameObject viewmodel;
    private GameObject shot_origin;

    private List<GameObject> bullet_holes;
    private Animator anim;
    private PlayerStats stats;
    private HandleDamageNumbers damage_handler;

    IEnumerator UpdateViewmodelSync()
    {
        yield return new WaitForEndOfFrame();
        GetComponent<AnimatePlayer>().RefreshVAnim();
    }

    //Function returns if the player is currently firing
    public bool GetFiring() { return firing; }

    //Function returns the current active weapon
    public Weapon GetActiveWeapon() {
        if (held_weapons == null) return Resources.Load<Weapon>("Weapons/Pistol");
        if(held_weapons.Count == 0) return Resources.Load<Weapon>("Weapons/Pistol");
        return held_weapons[active_type][active_weapon];
    }

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
        if (weapon.view_model == null)
        {
            //clear all children of the viewmodel if any
            foreach(Transform child in viewmodel.transform)
            {
                Destroy(child.gameObject);
            }

            //enable the viewmodel's default members
            viewmodel.GetComponent<MeshRenderer>().enabled = true;

            //write to the members
            viewmodel.GetComponent<MeshFilter>().mesh = weapon.model;
            Material[] new_mats = new Material[weapon.mats.Length];
            viewmodel.GetComponent<MeshRenderer>().materials = new_mats;
            for (int i = 0; i < weapon.mats.Length; i++) { viewmodel.GetComponent<MeshRenderer>().materials[i] = weapon.mats[i]; }
            anim = viewmodel.GetComponent<Animator>();
        }
        else
        {
            //clear all children of the viewmodel if any
            foreach (Transform child in viewmodel.transform)
            {
                Destroy(child.gameObject);
            }

            //disable the viewmodel's mesh renderer
            viewmodel.GetComponent<MeshRenderer>().enabled = false;
            Material[] new_mats = new Material[0];
            viewmodel.GetComponent<MeshRenderer>().materials = new_mats;

            //spawn the viewmodel as a child and set it's animator as the animator
            GameObject new_model = (GameObject)Instantiate(weapon.view_model, viewmodel.transform);
            anim = new_model.GetComponent<Animator>();
        }

        //update the viewmodel's position and shot_spawn position
        physics_parent.transform.localPosition = weapon.weapon_spawn;
        viewmodel.transform.rotation = physics_parent.transform.rotation;
        transform.GetChild(0).GetChild(1).localPosition = weapon.shot_spawn;

        current_interval = weapon.shot_interval;
        max_interval = weapon.shot_interval;

        anim.runtimeAnimatorController = weapon.anim;

        if (isLocalPlayer)
        {
            SetWeaponName(held_weapons[active_type][active_weapon].weapon_name);
            StartCoroutine(UpdateViewmodelSync());
        }
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
    
    [Command]
    private void Cmd_SpawnProjectile(int index, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 torque, bool use_gravity, string owner_name)
    {
        //attempt to spawn a projectile at the index
        NetworkManager manager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();
        if (index < 0 || index > manager.spawnPrefabs.Count - 1) Debug.LogError($"Index provided: {index}, is invalid, no such spawnable exists.");

        //check if the spawned object is of type projectile
        GameObject temp = (GameObject)Instantiate(manager.spawnPrefabs[index], position, rotation);
        ProjectileBehavior temp_projectile = temp.GetComponent<ProjectileBehavior>();
        if (temp == null) Debug.LogError("The registered spawnable does not have a ProjectileBehavior component attached!");

        //populate all values of the projectile
        Rigidbody temp_rb = temp.GetComponent<Rigidbody>();
        temp_rb.velocity = velocity;
        temp_rb.AddRelativeTorque(torque);
        temp_rb.useGravity = use_gravity;
        temp_projectile.owner = this.gameObject;

        NetworkServer.Spawn(temp, connectionToClient);
    }

    [Command]
    private void UpdateID()
    {
        id = connectionToClient.connectionId;
    }

    public int GetID()
    {
        return id;
    }

    public string GetWeaponName() { return active_weapon_name; }

    public void SetWeaponName(string w_name)
    {
        if (isServer)
            RpcSetWeaponName(w_name);
        else
            CmdSetWeaponName(w_name);
    }

    [Command]
    public void CmdSetWeaponName(string w_name)
    {
        RpcSetWeaponName(w_name);
    }

    [ClientRpc]
    public void RpcSetWeaponName(string w_name)
    {
        active_weapon_name = w_name;
    }
    
    public void DealDamage(GameObject target, int damage)
    {
        //make sure that the target is a valid target before dealing damage
        if (target.GetComponent<NetworkIdentity>() == null || target.GetComponent<PlayerStats>() == null) return;
        Cmd_DealDamage(target, damage);
        if (isLocalPlayer) damage_handler.SpawnDamageNumber(CalculateDamage(target, damage), target.transform.position);
        if (target.GetComponent<PlayerStats>().GetHealth() - CalculateDamage(target, damage) <= 0)
        {
            int state = 0;
            //both don't have names
            if ((stats.GetPlayerName() == null || stats.GetPlayerName() == "") && (target.GetComponent<PlayerStats>().GetPlayerName() == null || target.GetComponent<PlayerStats>().GetPlayerName() == "")) state = 0;
            //the target doesn't have a name
            else if ((stats.GetPlayerName() != null || stats.GetPlayerName() != "") && (target.GetComponent<PlayerStats>().GetPlayerName() == null || target.GetComponent<PlayerStats>().GetPlayerName() == "")) state = 1;
            //this object doesn't have a name
            else if ((stats.GetPlayerName() == null || stats.GetPlayerName() == "") && (target.GetComponent<PlayerStats>().GetPlayerName() != null || target.GetComponent<PlayerStats>().GetPlayerName() != "")) state = 2;
            //they both have names
            else if ((stats.GetPlayerName() != null || stats.GetPlayerName() != "") && (target.GetComponent<PlayerStats>().GetPlayerName() != null || target.GetComponent<PlayerStats>().GetPlayerName() != "")) state = 3;

            //make sure if this connection is a host or has no netid it has something to print
            string a = null;
            string b = null;
            a = id.ToString();
            b = target.GetComponent<WeaponBehavior>().GetID().ToString();
            
            if (a == null || isServer) a = "Server";
            if (b == null || b == "0") b = "Server";

            switch (state) {
                case 0:
                    GetComponent<ChatBehavior>().SendMisc($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>{a}</color> <color=red>fragged</color> " +
                        $"<#{ColorUtility.ToHtmlStringRGB(target.GetComponent<PlayerStats>().GetPrimaryColor())}>{b}</color> " +
                        $"<color=red>with</color> {held_weapons[active_type][active_weapon].name}<color=red>!</color>");
                    break;
                case 1:
                    GetComponent<ChatBehavior>().SendMisc($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>{stats.GetPlayerName()}</color> <color=red>fragged</color> " +
                     $"<#{ColorUtility.ToHtmlStringRGB(target.GetComponent<PlayerStats>().GetPrimaryColor())}>{b}</color> " +
                     $"<color=red>with</color> {held_weapons[active_type][active_weapon].name}<color=red>!</color>");
                    break;
                case 2:
                    GetComponent<ChatBehavior>().SendMisc($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>{a}</color> <color=red>fragged</color> " +
                     $"<#{ColorUtility.ToHtmlStringRGB(target.GetComponent<PlayerStats>().GetPrimaryColor())}>{target.GetComponent<PlayerStats>().GetPlayerName()}</color> " +
                     $"<color=red>with</color> {held_weapons[active_type][active_weapon].name}<color=red>!</color>");
                    break;
                case 3:
                    GetComponent<ChatBehavior>().SendMisc($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>{stats.GetPlayerName()}</color> <color=red>fragged</color> " +
                     $"<#{ColorUtility.ToHtmlStringRGB(target.GetComponent<PlayerStats>().GetPrimaryColor())}>{target.GetComponent<PlayerStats>().GetPlayerName()}</color> " +
                     $"<color=red>with</color> {held_weapons[active_type][active_weapon].name}<color=red>!</color>");
                    break;

            }
        }
    }

    public void DealDamageRPC(GameObject target, int damage)
    {
        //make sure that the target is a valid target before dealing damage
        if (target.GetComponent<NetworkIdentity>() == null || target.GetComponent<PlayerStats>() == null) return;
        Rpc_DealDamage(target, damage);
        if (isLocalPlayer) damage_handler.SpawnDamageNumber(CalculateDamage(target, damage), target.transform.position);
        if (target.GetComponent<PlayerStats>().GetHealth() - CalculateDamage(target, damage) <= 0)
            GetComponent<ChatBehavior>().SendMisc($"<#{ColorUtility.ToHtmlStringRGB(stats.GetPrimaryColor())}>{stats.GetPlayerName()}</Color> fragged " +
                $"<#{ColorUtility.ToHtmlStringRGB(target.GetComponent<PlayerStats>().GetPrimaryColor())}>{target.GetComponent<PlayerStats>().GetPlayerName()}</Color> with {held_weapons[active_type][active_weapon].name}!");
    }

    public int CalculateDamage(GameObject target, int damage)
    {
        PlayerStats target_stats = target.GetComponent<PlayerStats>();

        //first determine what type of armor the player has before dealing the damage
        /*
        Steps for dealing armor based damage:
        1) Determine the armor type
        2) Subtract the protected damage from the armor's total
        3) Deal the rest of the damage to the player
        4) If more damage is dealt to the armor than there is armor value then add that remaining damage to be subtracted from the player's health
        */
        switch (target_stats.GetArmorType())
        {
            //no armor
            case 0:
                return damage;
            //regular armor 1/3 damage reduction
            case 1:
                int armor_damage = damage / 3;
                int player_damage = (2 * damage) / 3;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                }
                return player_damage;
            //super armor 1/2 damage reduction
            case 2:
                armor_damage = damage / 2;
                player_damage = damage / 2;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                }
                return player_damage;
            default:
                return damage;
        }
    }
    
    [Command]
    private void Cmd_DealDamage(GameObject target, int damage)
    {
        PlayerStats target_stats = target.GetComponent<PlayerStats>();

        //first determine what type of armor the player has before dealing the damage
        /*
        Steps for dealing armor based damage:
        1) Determine the armor type
        2) Subtract the protected damage from the armor's total
        3) Deal the rest of the damage to the player
        4) If more damage is dealt to the armor than there is armor value then add that remaining damage to be subtracted from the player's health
        */
        switch (target_stats.GetArmorType())
        {
            //no armor
            case 0:
                target_stats.SetHealth(target_stats.GetHealth() - damage, true);
                break;
            //regular armor 1/3 damage reduction
            case 1:
                int armor_damage = damage / 3;
                int player_damage = (2 * damage) / 3;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                    target_stats.SetArmor(0);
                }
                else target_stats.SetArmor(target_stats.GetArmor() - armor_damage);
                target_stats.SetHealth(target_stats.GetHealth() - player_damage, true);
                break;
            //super armor 1/2 damage reduction
            case 2:
                armor_damage = damage / 2;
                player_damage = damage / 2;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                    target_stats.SetArmor(0);
                }
                else target_stats.SetArmor(target_stats.GetArmor() - armor_damage);
                target_stats.SetHealth(target_stats.GetHealth() - player_damage, true);
                break;
            default:
                target_stats.SetHealth(target_stats.GetHealth() - damage, true);
                break;
        }
    }

    [ClientRpc]
    private void Rpc_DealDamage(GameObject target, int damage)
    {
        PlayerStats target_stats = target.GetComponent<PlayerStats>();

        //first determine what type of armor the player has before dealing the damage
        /*
        Steps for dealing armor based damage:
        1) Determine the armor type
        2) Subtract the protected damage from the armor's total
        3) Deal the rest of the damage to the player
        4) If more damage is dealt to the armor than there is armor value then add that remaining damage to be subtracted from the player's health
        */
        switch (target_stats.GetArmorType())
        {
            //no armor
            case 0:
                target_stats.SetHealth(target_stats.GetHealth() - damage, true);
                break;
            //regular armor 1/3 damage reduction
            case 1:
                int armor_damage = damage / 3;
                int player_damage = (2 * damage) / 3;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                    target_stats.SetArmor(0);
                }
                else target_stats.SetArmor(target_stats.GetArmor() - armor_damage);
                target_stats.SetHealth(target_stats.GetHealth() - player_damage, true);
                break;
            //super armor 1/2 damage reduction
            case 2:
                armor_damage = damage / 2;
                player_damage = damage / 2;
                if (target_stats.GetArmor() - armor_damage < 0)
                {
                    player_damage += Mathf.Abs(target_stats.GetArmor() - armor_damage);
                    target_stats.SetArmor(0);
                }
                else target_stats.SetArmor(target_stats.GetArmor() - armor_damage);
                target_stats.SetHealth(target_stats.GetHealth() - player_damage, true);
                break;
            default:
                target_stats.SetHealth(target_stats.GetHealth() - damage, true);
                break;
        }
        target_stats.SetHealth(target_stats.GetHealth() - damage, true);
    }

    public void SpawnExplosion(Vector3 position, Quaternion rotation, float radius, int damage, float knockback, GameObject owner, GameObject damaged)
    {
        if (isClient)
        {
            Cmd_SpawnExplosion(position, rotation, radius, damage, knockback, owner, damaged);
            return;
        }
        Rpc_SpawnExplosion(position, rotation, radius, damage, knockback, owner, damaged);
    }

    [Command]
    private void Cmd_SpawnExplosion(Vector3 position, Quaternion rotation, float radius, int damage, float knockback, GameObject owner, GameObject damaged)
    {
        NetworkManager manager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();

        GameObject explosion = (GameObject)Instantiate(manager.spawnPrefabs[5], position, rotation);
        BlastRadiusBehavior temp = explosion.GetComponent<BlastRadiusBehavior>();
        explosion.GetComponent<SphereCollider>().radius = radius;
        temp.damage = damage;
        temp.knock_back = knockback;
        temp.owner = owner;
        if (damaged != null) temp.AddObject(ref damaged);

        NetworkServer.Spawn(explosion);
    }

    [ClientRpc]
    private void Rpc_SpawnExplosion(Vector3 position, Quaternion rotation, float radius, int damage, float knockback, GameObject owner, GameObject damaged)
    {
        NetworkManager manager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();

        GameObject explosion = (GameObject)Instantiate(manager.spawnPrefabs[5], position, rotation);
        BlastRadiusBehavior temp = explosion.GetComponent<BlastRadiusBehavior>();
        explosion.GetComponent<SphereCollider>().radius = radius;
        temp.damage = damage;
        temp.knock_back = knockback;
        temp.owner = owner;
        if(damaged != null) temp.AddObject(ref damaged);

        NetworkServer.Spawn(explosion);
    }

    public void SpawnGeneric(int index, Vector3 position, Quaternion rotation) { Rpc_SpawnGeneric(index, position, rotation); }

    [ClientRpc]
    private void Rpc_SpawnGeneric(int index, Vector3 position, Quaternion rotation)
    {
        //attempt to spawn a projectile at the index
        NetworkManager manager = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<NetworkManager>();
        if (index < 0 || index > manager.spawnPrefabs.Count - 1) Debug.LogError($"Index provided: {index}, is invalid, no such spawnable exists.");

        GameObject generic = (GameObject)Instantiate(manager.spawnPrefabs[index], position, rotation);
        NetworkServer.Spawn(generic);
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

            Projectile data = projectile.GetComponent<ProjectileBehavior>().data;
            Rigidbody proj_rb = projectile.GetComponent<Rigidbody>();
            Debug.Log($"{data.network_index}, {projectile.transform.position}, {projectile.transform.rotation}, {proj_rb.velocity}, {weapon.shot_torque}, {weapon.use_gravity}, {name}");
            Cmd_SpawnProjectile(data.network_index, projectile.transform.position, projectile.transform.rotation, proj_rb.velocity, weapon.shot_torque, weapon.use_gravity, name);
            Destroy(projectile);
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
                    //if the player hits an object that isn't a player
                    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                    {
                        GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                        bullet_hole.transform.forward = -hit.normal;
                        bullet_hole.transform.position += hit.normal * .0001f;
                        bullet_holes.Add(bullet_hole);
                        CleanBulletHoles();

                        //Miss Sound
                        if (weapon.shot_miss != null)
                        {
                            if (weapon.shot_miss.Length != 0)
                                PlayerAudioHandler.PlaySoundAtPoint(weapon.shot_miss[Random.Range(0, weapon.shot_miss.Length)], hit.point);
                        }
                    }
                    //hit a player
                    else
                    {
                        Cmd_DealDamage(hit.collider.gameObject, weapon.damage);

                        //Hit Sound
                        if (weapon.shot_hit != null)
                        {
                            if (weapon.shot_hit.Length != 0)
                                PlayerAudioHandler.PlaySoundAtPoint(weapon.shot_hit[Random.Range(0, weapon.shot_hit.Length)], hit.point);
                        }
                    }
                }
            }
            else
            {
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, GetComponent<CharacterMovement>().GetCam().transform.forward, out hit, Mathf.Infinity, ~LayerMask.GetMask("Pickup")))
                {
                    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                    {
                        //if the player hits an object that isn't a player
                        GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                        bullet_hole.transform.forward = -hit.normal;
                        bullet_hole.transform.position += hit.normal * .0001f;
                        bullet_holes.Add(bullet_hole);
                        CleanBulletHoles();

                        //Miss Sound
                        if (weapon.shot_miss != null)
                        {
                            if (weapon.shot_miss.Length != 0)
                                PlayerAudioHandler.PlaySoundAtPoint(weapon.shot_miss[Random.Range(0, weapon.shot_miss.Length)], hit.point);
                        }
                    }
                    //hit a player
                    else DealDamage(hit.collider.gameObject, weapon.damage);
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
        if (current_interval >= 0.0f)
        {
            current_interval -= 1.0f * Time.deltaTime;
            firing = false;
            return;
        }

        //if the weapon is ready to be fired and the fire key is being pressed
        if (Input.GetButton("Fire1"))
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

                    for (int i = 0; i < shot_paths.Count; i++)
                    {
                        GameObject projectile = (GameObject)Instantiate(weapon.projectile, shot_origin.transform.position, Quaternion.identity);
                        projectile.transform.forward = shot_paths[i];
                        //handle shot direction override
                        if (weapon.shot_direction.magnitude == 0)
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
                        Physics.IgnoreCollision(GetComponent<Collider>(), projectile.GetComponent<Collider>());
                        Projectile data = projectile.GetComponent<ProjectileBehavior>().data;
                        Rigidbody proj_rb = projectile.GetComponent<Rigidbody>();
                        Cmd_SpawnProjectile(data.network_index, projectile.transform.position, projectile.transform.rotation, proj_rb.velocity, weapon.shot_torque, weapon.use_gravity, name);
                        Destroy(projectile);
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

                    Dictionary<GameObject, int> damaged_players = new Dictionary<GameObject, int>();
                    for (int i = 0; i < shot_paths.Count; i++)
                    {
                        //raycast along all paths in the shot_paths
                        RaycastHit hit = new RaycastHit();
                        if (Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, shot_paths[i], out hit, Mathf.Infinity, ~LayerMask.GetMask("Pickup")))
                        {
                            //if the player hits an object that isn't the player
                            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                            {
                                GameObject bullet_hole = (GameObject)Instantiate(bullet_decal, hit.point, Quaternion.identity);
                                bullet_hole.transform.forward = -hit.normal;
                                bullet_hole.transform.position += hit.normal * .0001f;
                                bullet_holes.Add(bullet_hole);
                                CleanBulletHoles();

                                //Miss Sound
                                if (weapon.shot_miss != null)
                                {
                                    if(weapon.shot_miss.Length != 0)
                                    PlayerAudioHandler.PlaySoundAtPoint(weapon.shot_miss[Random.Range(0, weapon.shot_miss.Length)], hit.point, 1f/(float)weapon.number_of_shots);
                                }
                            }
                            //hit a player
                            else
                            {
                                //first see if the player has been hit
                                if (damaged_players.ContainsKey(hit.collider.gameObject)) damaged_players[hit.collider.gameObject] += weapon.damage;
                                else damaged_players.Add(hit.collider.gameObject, weapon.damage);

                                //Hit Sound
                                if (weapon.shot_hit != null)
                                {
                                    if (weapon.shot_hit.Length != 0)
                                        PlayerAudioHandler.PlaySoundAtPoint(weapon.shot_hit[Random.Range(0, weapon.shot_hit.Length)], hit.point, 1f/(float)weapon.number_of_shots);
                                }
                            }
                        }
                    }
                    //calculate total damage to all players hit and apply it in one command for each player
                    foreach(KeyValuePair<GameObject, int> player in damaged_players)
                    {
                        DealDamage(player.Key, player.Value);
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

            //play the weapon's sound when it is fired at the player's position
            if(weapon.fire_sound != null) PlayerAudioHandler.PlaySoundAtPoint(weapon.fire_sound, transform.position, 1f, false);
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

    public void ClearWeapons()
    {
        held_weapons = new List<List<Weapon>>() { new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>(),
                                                  new List<Weapon>()
                                                };
        AddWeapon(Resources.Load<Weapon>("Weapons/Pistol"));
        active_type = 1;
        active_weapon = 0;
        UpdateViewmodel(held_weapons[active_type][active_weapon]);
    }

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
        damage_handler = GetComponent<HandleDamageNumbers>();

        //initialize list for keeping track of bullet holes
        bullet_holes = new List<GameObject>();

        if(isLocalPlayer)
            UpdateID();
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (!stats.GetDead())
            {
                //handle weaponswitching
                if (!stats.GetInteractionLock())
                {
                    SwitchWeapon();
                    Fire(held_weapons[active_type][active_weapon]);
                }
                Animate();
            }
        }
        else
        {
            viewmodel.SetActive(false);
        }
    }
}
