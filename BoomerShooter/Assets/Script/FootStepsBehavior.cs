using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepsBehavior : MonoBehaviour
{
    public AudioClip[] step_sounds;
    public AudioClip[] jump_sounds;

    public void FootStep()
    {
        PlayerAudioHandler.PlaySoundAtPoint(step_sounds[Random.Range(0, step_sounds.Length)], transform.position, .5f);
    }

    public void JumpSound()
    {
        PlayerAudioHandler.PlaySoundAtPoint(jump_sounds[Random.Range(0, jump_sounds.Length)], transform.position, .5f);
    }
}
