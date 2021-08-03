using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    public int max_bullet_holes;
    public float current_interval;
    public Weapon[] weapons;
    public Object bullet_decal;

    private bool firing;
    private float max_interval;
    private List<Weapon> held_weapons;
    private GameObject physics_parent;
    private GameObject viewmodel;
    private GameObject shot_origin;

    private List<GameObject> bullet_holes;
    private Animator anim;

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
        viewmodel.transform.localPosition = new Vector3(0.0f, -.5f, 1f) + weapon.weapon_spawn;
        transform.GetChild(0).GetChild(1).localPosition = weapon.shot_spawn;

        current_interval = weapon.shot_interval;
        max_interval = weapon.shot_interval;
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
            projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * projectile.transform.forward;
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
                if(Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, shot_direction, out hit, Mathf.Infinity))
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
                if (Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, GetComponent<CharacterMovement>().GetCam().transform.forward, out hit, Mathf.Infinity))
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
        //check to see if the weapon is ready to be fired
        if(current_interval >= 0.0f)
        {
            current_interval -= 1.0f * Time.deltaTime;
            return;
        }

        //if the weapon is ready to be fired and the fire key is being pressed
        if(Input.GetButton("Fire1"))
        {
            current_interval = max_interval;
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
                        projectile.GetComponent<Rigidbody>().velocity = weapon.shot_speed * projectile.transform.forward;
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
                        if(Physics.Raycast(GetComponent<CharacterMovement>().GetCam().transform.position, shot_paths[i], out hit, Mathf.Infinity))
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
            physics_parent.transform.localPosition = Vector3.zero;
            physics_parent.transform.position += (forward_proj + right_proj + up_proj) * -.1f;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //load all weapons found in resources into the weapons list
        weapons = Resources.LoadAll<Weapon>("Weapons");

        held_weapons = new List<Weapon>();
        held_weapons.Add(weapons[0]);

        //load the first active weapon to the viewmodel
        physics_parent = transform.GetChild(0).GetChild(0).gameObject;
        viewmodel = physics_parent.transform.GetChild(0).gameObject;
        shot_origin = transform.GetChild(0).GetChild(1).gameObject;
        UpdateViewmodel(held_weapons[0]);
        anim = viewmodel.GetComponent<Animator>();
        anim.runtimeAnimatorController = held_weapons[0].anim;

        //initialize list for keeping track of bullet holes
        bullet_holes = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        Animate();
        Fire(held_weapons[0]);
    }
}
