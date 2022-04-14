using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameEyeBehavior : MonoBehaviour
{
    public int frame_delay;

    //scale of trails
    public float trail_scale;

    //offset for modifying the trail end posiion
    public Vector3 trail_offset;

    //right and left eye starting transforms for trail calculations
    public GameObject right_eye;
    public GameObject left_eye;

    //right and left eye trails
    public LineRenderer right_trail;
    public LineRenderer left_trail;

    private int frame_counter;

    //right and left eye end transforms for trail calculations
    private GameObject re_target;
    private GameObject le_target;

    //the position of the right and left eye starting transforms on the previous frame
    private Vector3 r_eye_lpos;
    private Vector3 l_eye_lpos;

    //handles setting target positions for line renderer
    public void UpdateTargetPositions()
    {
        //get the resultant vectors of positions between this frame and the last
        Vector3 r_dir = r_eye_lpos - right_eye.transform.position;
        Vector3 l_dir = l_eye_lpos - left_eye.transform.position;

        r_dir += trail_offset;
        l_dir += trail_offset;

        //modify the target positions
        re_target.transform.position = right_eye.transform.position + r_dir * trail_scale;
        le_target.transform.position = left_eye.transform.position + l_dir * trail_scale;
    }

    //function for setting values of trails
    public void UpdateTrails()
    {
        Vector3 re_start = right_eye.transform.position;
        Vector3 le_start = left_eye.transform.position;
        
        Vector3 re_end = re_target.transform.position;
        Vector3 le_end = le_target.transform.position;

        for(int i=0; i<right_trail.positionCount; i++)
        {
            //special cases
            if(i == 0)
            {
                right_trail.SetPosition(i, re_start);
                left_trail.SetPosition(i, le_start);
                continue;
            }
            if(i == 4)
            {
                right_trail.SetPosition(i, re_end);
                left_trail.SetPosition(i, le_end);
                continue;
            }

            float lerp_factor = Mathf.Pow((float)i / 5f, 2f);

            Vector3 rlerp_direction = r_eye_lpos - re_start;
            Vector3 llerp_direction = l_eye_lpos - le_start;

            Debug.Log($"{rlerp_direction}, {llerp_direction}");

            Vector3 rlerp = Vector3.Lerp(re_start, rlerp_direction + re_start, lerp_factor) + ((float)i/5f) * trail_offset;
            Vector3 llerp = Vector3.Lerp(le_start, llerp_direction + le_start, lerp_factor) + ((float)i/5f) * trail_offset;

            right_trail.SetPosition(i, rlerp);
            left_trail.SetPosition(i, llerp);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        frame_counter = 0;

        re_target = right_eye.transform.GetChild(0).gameObject;
        le_target = left_eye.transform.GetChild(0).gameObject;

        r_eye_lpos = right_eye.transform.position;
        l_eye_lpos = left_eye.transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        frame_counter++;

        UpdateTargetPositions();
    
        if (frame_counter >= frame_delay)
        {
            UpdateTrails();

            r_eye_lpos = right_eye.transform.position;
            l_eye_lpos = left_eye.transform.position;
            frame_counter = 0;
        }
        right_trail.SetPosition(0, right_eye.transform.position);
        left_trail.SetPosition(0, left_eye.transform.position);
    }
}
