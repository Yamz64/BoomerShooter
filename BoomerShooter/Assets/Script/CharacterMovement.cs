using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public bool frame_check;
    public bool grounded;
    public bool crouching;
    public bool cam_position;
    public float ground_accel;
    public float max_ground_accel;
    public float air_accel;
    public float max_air_accel;
    public float friction;
    public float jump_speed;
    public float crouch_speed_divider;
    public Vector2 mouse_sensitivity;

    private float rot_x;
    private float rot_y;
    private float height;
    private Camera cam;
    private Rigidbody rb;
    private CapsuleCollider col;
    
    void Look()
    {
        rot_x += Input.GetAxis("Mouse X") * mouse_sensitivity.x;
        rot_y += Input.GetAxis("Mouse Y") * mouse_sensitivity.y;

        rot_y = Mathf.Clamp(rot_y, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(-rot_y, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, rot_x, 0f);

        //normal cam position
        if (cam_position == false) cam.transform.localPosition = new Vector3(0.0f, height/4f, 0.0f);
        //ground crouched
        else if (cam_position == true) cam.transform.localPosition = new Vector3(0.0f, -height/4f, 0.0f);
    }

    Vector3 Accelerate(Vector3 accel_dir, Vector3 prev_vel, float accel, float max_vel)
    {
        float proj_vel = Vector3.Dot(prev_vel, accel_dir);
        float accel_vel = accel * Time.fixedDeltaTime;

        if (proj_vel + accel_vel > max_vel) accel_vel = max_vel - proj_vel;
        
        return prev_vel + accel_dir * accel_vel;
    }

    Vector3 MoveGround(Vector3 accel_dir, Vector3 prev_vel)
    {
        float speed = prev_vel.magnitude;
        if(speed != 0)
        {
            float drop = speed * friction * Time.fixedDeltaTime;
            prev_vel *= Mathf.Max(speed - drop, 0) / speed;
        }

        if (!crouching)
            return Accelerate(accel_dir, prev_vel, ground_accel, max_ground_accel);
        else
            return Accelerate(accel_dir, prev_vel, ground_accel / crouch_speed_divider, max_ground_accel / crouch_speed_divider);
    }

    Vector3 MoveAir(Vector3 accel_dir, Vector3 prev_vel)
    {
        return Accelerate(accel_dir, prev_vel, air_accel, max_air_accel);
    }

    void Crouch()
    {
        if (Input.GetButton("Crouch"))
        {
            //first see if already crouched
            if (crouching) return;
            else
            {
                crouching = true;
                //if grounded bring the top of the collider down for a crouch
                if (grounded)
                {
                    cam_position = true;
                    col.center = new Vector3(0.0f, -col.height / 4f, 0.0f);
                    col.height = height / 2f;
                }
                //if not grounded bring the bottom of the collider up for a crouch
                else
                {
                    col.center = new Vector3(0.0f, col.height / 4f, 0.0f);
                    col.height = height / 2f;
                }
            }
        }
        else
        {
            //reset transforms if crouching
            if (crouching)
            {
                cam_position = false;
                crouching = false;
                col.center = Vector3.zero;
                col.height = height;
            }
            else
            {
                return;
            }
        }
    }
    
    public GameObject GetCam() { return cam.gameObject; }

    // Start is called before the first frame update
    void Start()
    {
        rot_x = transform.rotation.eulerAngles.x;
        rot_y = 0.0f;
        cam = transform.GetChild(0).gameObject.GetComponent<Camera>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        height = col.height;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Look();
        Crouch();
        //check if the player is grounded (wait a frame before calculations are accepted
        float ray_length = (col.height / 2f) + .005f;
        if(Physics.Raycast(transform.position + col.center, -Vector3.up, ray_length, ~LayerMask.GetMask("Player", "Pickup")))
        {
            if (!frame_check) frame_check = true;
            else grounded = true;
        }
        else
        {
            frame_check = false;
            grounded = false;
        }

        if (frame_check && Input.GetButton("Jump")) rb.velocity += new Vector3(0.0f, jump_speed, 0.0f);

        Vector3 move_dir_forward = transform.forward;
        Vector3 move_dir_right = -(Vector3.Cross(move_dir_forward, Vector3.up).normalized);
        move_dir_forward *= Input.GetAxis("Vertical");
        move_dir_right *= Input.GetAxis("Horizontal");
        Vector3 move_dir = (move_dir_forward + move_dir_right).normalized;
        if (grounded) rb.velocity = MoveGround(move_dir, rb.velocity);
        else rb.velocity = MoveAir(move_dir, rb.velocity);
    }
}
