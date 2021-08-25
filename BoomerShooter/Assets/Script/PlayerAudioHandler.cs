using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerAudioHandler
{
    public class CoroutineStarter : MonoBehaviour { }

    private static CoroutineStarter starter;
    private static GameObject game_object;

    public static Object audio_object;

    private static IEnumerator DestroyAudioClip(GameObject sound)
    {
        yield return new WaitUntil(() => sound.GetComponent<AudioSource>().isPlaying);
        yield return new WaitUntil(() => !sound.GetComponent<AudioSource>().isPlaying);
        MonoBehaviour.Destroy(sound);
    }

    private static IEnumerator DestroyHandler()
    {
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Sound") == null);
        MonoBehaviour.Destroy(game_object);
    }

    private static void Init()
    {
        if(starter == null)
        {
            game_object = new GameObject("MyStatic");

            starter = game_object.AddComponent<CoroutineStarter>();
        }
    }

    public static void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1, bool use_spatial_blending = true)
    {
        volume = Mathf.Clamp01(volume);
        audio_object = Resources.Load<Object>("LoadablePrefabs/Sound");
        GameObject sound = (GameObject)MonoBehaviour.Instantiate(audio_object, position, Quaternion.identity);

        sound.GetComponent<AudioSource>().clip = clip;
        sound.GetComponent<AudioSource>().Play();
        sound.GetComponent<AudioSource>().volume = volume;
        if (!use_spatial_blending) sound.GetComponent<AudioSource>().spatialBlend = 0;

        Init();
        starter.StartCoroutine(DestroyAudioClip(sound));
        starter.StartCoroutine(DestroyHandler());
    }
}
