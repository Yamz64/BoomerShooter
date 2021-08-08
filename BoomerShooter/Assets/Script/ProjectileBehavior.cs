using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    public Projectile data;
    public GameObject owner;

    private float life_time;
    private bool collided;

    public delegate void DeathSequence(GameObject player = null);
    public DeathSequence DeathFunction;

    void Explode(GameObject player = null)
    {
        GameObject blast = (GameObject)Instantiate(data.blast_box, transform.position, Quaternion.identity);
        blast.GetComponent<SphereCollider>().radius = data.explosion_radius;
        blast.GetComponent<BlastRadiusBehavior>().knock_back = data.knockback;
    }

    void BreakOnCollide(GameObject player = null)
    {
        if (player != null)
        {
            if (!data.explosive) player.GetComponent<Rigidbody>().AddForce((player.transform.position - transform.position).normalized * data.knockback);
        }
        Destroy(gameObject);
    }

    void BreakAfterTime(GameObject player = null)
    {
        if (life_time <= 0)
        {
            DeathFunction(player);
        }
    }
    

    // Start is called before the first frame update
    void Start()
    {
        life_time = data.life_time;
        collided = false;

        //the projectile cannot collide with it's owner
        Physics.IgnoreCollision(owner.GetComponent<Collider>(), GetComponent<Collider>());

        if (data.explosive) DeathFunction += Explode;
        if (data.die_on_contact) DeathFunction += BreakOnCollide;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (data.die_after_time)
        {
            life_time -= Time.deltaTime;
            BreakAfterTime();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (data.collision_mask == (data.collision_mask | (1 << other.gameObject.layer)) && owner != other.gameObject)
        {
            if (data.die_on_contact)
            {
                if (!collided)
                {
                    if (other.gameObject.GetComponent<Rigidbody>() == null)
                        DeathFunction();
                    else
                        DeathFunction(other.gameObject);
                }
            }
        }
        else if (!data.die_after_other_collision && owner != other.gameObject) { collided = true; }
    }
}
