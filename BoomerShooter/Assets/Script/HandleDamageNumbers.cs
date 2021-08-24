using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleDamageNumbers : MonoBehaviour
{
    public float damage_number_lifetime;

    public Object damage_prefab;
    public GameObject damage_parent;

    private Camera cam;

    IEnumerator DestroyNumber(GameObject damage_number)
    {
        float progress = 0.0f;
        Vector3 initial_pos = damage_number.transform.localPosition;
        Color initial_color = damage_number.GetComponent<Text>().color;
        while(progress < damage_number_lifetime)
        {
            progress += 1f / 60f;
            damage_number.GetComponent<Text>().color = Color.Lerp(initial_color, new Color(initial_color.r, initial_color.g, initial_color.b, 0.0f), progress);
            damage_number.transform.localPosition = Vector3.Lerp(initial_pos, initial_pos + new Vector3(0.0f, 75f, 0.0f), progress);
            yield return new WaitForSeconds(1f / 60f);
        }
        Destroy(damage_number);
    }

    private void Start()
    {
        cam = transform.GetChild(0).GetComponent<Camera>();
    }

    public void SpawnDamageNumber(int damage, Vector3 worldspace_pos)
    {
        //spawn a damage number in
        GameObject damage_number = (GameObject)Instantiate(damage_prefab, damage_parent.transform);

        //convert it's worldspace coords to screenspace
        damage_number.transform.localPosition = cam.WorldToScreenPoint(worldspace_pos) - new Vector3(Screen.width/2f, Screen.height/2f, 0f);
        damage_number.transform.localPosition = new Vector3(damage_number.transform.localPosition.x, damage_number.transform.localPosition.y, 0f);

        damage_number.GetComponent<Text>().text = $"-{damage}";

        StartCoroutine(DestroyNumber(damage_number));
    }
}
