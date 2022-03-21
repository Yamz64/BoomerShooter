using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AnimatePlayer : NetworkBehaviour
{
    [SerializeField]
    private Transform weapon_spawn;
    private GameObject player_model_weapon;

    private string last_state;
    private Rigidbody rb;
    private Animator anim;
    private Animator v_anim;
    private WeaponBehavior w_behavior;
    private CharacterMovement movement;
    private List<string> firing_states;
    private PlayerStats stats;

    private bool GetFiring()
    {
        for(int i=0; i<firing_states.Count; i++)
        {
            if (anim.GetCurrentAnimatorStateInfo(1).IsName(firing_states[i])) return true;
        }
        return false;
    }

    public void RefreshVAnim() {
        v_anim = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Animator>();

        if (player_model_weapon != null) Destroy(player_model_weapon);

        Weapon active_weapon = w_behavior.GetActiveWeapon();
        player_model_weapon = (GameObject)Instantiate(active_weapon.view_model, weapon_spawn);
        player_model_weapon.transform.localPosition = active_weapon.playermodel_t_offset;
        player_model_weapon.transform.localRotation = Quaternion.Euler(active_weapon.playermodel_r_offset.x, active_weapon.playermodel_r_offset.y, active_weapon.playermodel_r_offset.z);
        player_model_weapon.transform.localScale = active_weapon.playermodel_scale / 200f;
        
        Destroy(player_model_weapon.GetComponent<Animator>());
    }

    public void AnimateWeapon()
    {
        anim.SetInteger("ActiveWeapon", w_behavior.GetActiveWeapon().animation_id);

        if (!anim.GetBool("Firing") == true && w_behavior.GetFiring())
        {
            anim.SetBool("Firing", Input.GetButton("Fire1"));
        }
        else if (GetFiring() && anim.GetCurrentAnimatorStateInfo(1).normalizedTime >= 1.0f)
        {
            bool valid = true;
            switch (w_behavior.GetActiveWeapon().weapon_type)
            {
                case 1:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                case 2:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                case 3:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                case 4:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                case 5:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                case 6:
                    if (stats.GetBullets() < w_behavior.GetActiveWeapon().shot_cost) { valid = false; }
                    break;
                default:
                    break;
            }
            if (valid)
            {
                anim.SetBool("Firing", Input.GetButton("Fire1"));
                anim.Play(anim.GetCurrentAnimatorClipInfo(1)[0].clip.name, 1, v_anim.GetCurrentAnimatorStateInfo(0).normalizedTime);
            }
            else anim.SetBool("Firing", false);
        }
    }

    public void StartJumpState() { anim.SetInteger("JumpState", 1); }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = transform.GetChild(2).GetComponent<Animator>();
        w_behavior = GetComponent<WeaponBehavior>();
        movement = GetComponent<CharacterMovement>();
        stats = GetComponent<PlayerStats>();
        RefreshVAnim();

        firing_states = new List<string>()
        {
            "Pistol_Fire",
            "Pistol_FireC",
            "Magnum_Fire",
            "Magnum_FireC",
            "Shotgun_Fire",
            "SG_FireC",
            "SSG_Fire",
            "SSG_FireC",
            "ChainGun_Fire",
            "ChainGun_FireC",
            "RL_Fire",
            "RL_FireC",
            "GL_Fire",
            "GL_FireC",
            "PR_Fire",
            "PR_FireC"
        };
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Handle weapon animations
        AnimateWeapon();

        //Handle basic linear movement
        Vector2 d_velocity = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * rb.velocity.magnitude;

        if (!movement.crouching)
        {
            anim.SetFloat("Run_X", d_velocity.x / movement.max_ground_accel);
            anim.SetFloat("Run_Y", d_velocity.y / movement.max_ground_accel);
        }
        else
        {
            anim.SetFloat("Run_X", d_velocity.x / (movement.max_ground_accel / movement.crouch_speed_divider));
            anim.SetFloat("Run_Y", d_velocity.y / (movement.max_ground_accel / movement.crouch_speed_divider));
        }

        anim.SetBool("Crouching", movement.crouching);

        //Handle Aerial animation
        //jumping
        if (Input.GetButton("Jump") && movement.GroundCheck())
        {
            anim.SetInteger("JumpState", 1);
            if(anim.GetCurrentAnimatorClipInfo(0).Length > 0)
            anim.Play(anim.GetCurrentAnimatorClipInfo(0)[0].clip.name, 0, 0.0f);
        }
        else
        {
            //falling
            if (anim.GetInteger("JumpState") == 1 && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                anim.SetInteger("JumpState", 2);
            }else
            //landing
            if (movement.GroundCheck() && !movement.grounded)
            {
                anim.SetInteger("JumpState", 3);
            }else
            //idle
            if (anim.GetInteger("JumpState") > 2 && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                anim.SetInteger("JumpState", 0);
            }
        }
    }
}
