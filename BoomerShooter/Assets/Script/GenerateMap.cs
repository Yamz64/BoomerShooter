using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GenerateMap : MonoBehaviour
{
    public int generation_method;
    public int map_width;
    public int map_height;
    public int room_number;

    public List<UnityEvent> generation_events;

    public void Start()
    {
        if (generation_method < 0) generation_method = 0;
        if (generation_method >= generation_events.Count && generation_events.Count > 0) generation_method = generation_events.Count - 1;
        if (room_number <= 0) room_number = 1;

        if(generation_events.Count > 0)
        generation_events[generation_method].Invoke();

        //Test Generating map bounds
        GenerationUtils utility = new GenerationUtils();
        GenerationUtils.BBox bbox = new GenerationUtils.BBox(utility.GenerateMapBounds(map_width, map_height, room_number));
    }
}
