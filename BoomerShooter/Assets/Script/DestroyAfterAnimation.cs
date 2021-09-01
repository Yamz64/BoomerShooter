using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    IEnumerator DestroyAfterAnimFinished()
    {
        Animator anim = transform.GetChild(0).GetComponent<Animator>();
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).length > anim.GetCurrentAnimatorStateInfo(0).normalizedTime);
        yield return new WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f);
        Destroy(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DestroyAfterAnimFinished());
    }
}
