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

    public static void PlaySoundAtPoint(AudioClip clip, Vector3 position, float volume = 1, bool use_spatial_blending = true, float range_factor = 1)
    {
        volume = Mathf.Clamp01(volume);
        audio_object = Resources.Load<Object>("LoadablePrefabs/Sound");
        GameObject sound = (GameObject)MonoBehaviour.Instantiate(audio_object, position, Quaternion.identity);

        sound.GetComponent<AudioSource>().clip = clip;
        sound.GetComponent<AudioSource>().volume = volume;
        sound.GetComponent<AudioSource>().maxDistance = sound.GetComponent<AudioSource>().maxDistance * range_factor;
        sound.GetComponent<AudioSource>().Play();
        if (!use_spatial_blending) sound.GetComponent<AudioSource>().spatialBlend = 0;

        Init();
        starter.StartCoroutine(DestroyAudioClip(sound));
        starter.StartCoroutine(DestroyHandler());
    }

    public static void PlayVoiceClipAtPoint(int type, int index, Vector3 position, float volume = 1, bool use_spaial_blending = true, float range_factor = 1)
    {
        Object[] clips = Resources.LoadAll("Sound/VA", typeof(AudioClip));

        List<AudioClip> clip_of_type = new List<AudioClip>();

        string prefix = "";
        switch (type)
        {
            case 0:
                prefix = "Airshot";
                break;
            case 1:
                prefix = "Battlecry";
                break;
            case 2:
                prefix = "Die";
                break;
            case 3:
                prefix = "Heal";
                break;
            case 4:
                prefix = "Meatshot";
                break;
            case 5:
                prefix = "Pain";
                break;
            case 6:
                prefix = "DoubleKill";
                break;
            case 7:
                prefix = "TripleKill";
                break;
            case 8:
                prefix = "QuadraKill";
                break;
            case 9:
                prefix = "PentaKill";
                break;
            case 10:
                prefix = "Revive";
                break;
            case 11:
                prefix = "Wep";
                break;
            default:
                prefix = "Airshot";
                break;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name.Contains(prefix)) clip_of_type.Add((AudioClip)clips[i]);
        }

        if (index >= clip_of_type.Count) return;

        PlaySoundAtPoint(clip_of_type[index], position, volume, use_spaial_blending, range_factor);
    }
}
