using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SewerGeneration : MonoBehaviour
{
    [SerializeField]
    public List<Material> mats;

    private int map_width;
    private int map_height;
    private int room_number;

    public void Awake()
    {
        //Get Necessary information
        GenerateMap map_generator = GameObject.FindGameObjectWithTag("GenerationManager").GetComponent<GenerateMap>();
        map_width = map_generator.map_width;
        map_height = map_generator.map_height;
        room_number = map_generator.room_number;
    }

    //called when the sewer generation event happens
    public void GenerationDriver()
    {
        GenerationUtils utility = new GenerationUtils();
        GenerationUtils.BBox bbox = new GenerationUtils.BBox(utility.GenerateMapBounds(map_width, map_height, room_number));

        //Generate shrunken bounding boxes for play
        //if this ever returns false then no rooms were generated
        if(ShrinkBoundingBoxes(bbox) == false) Debug.LogError("No valid rooms for generation unfortunately");
        Debug.Log(utility.Num_Leaves(bbox));

        //given these modified bounding boxes, create room sectors
        GenerateRooms(bbox);
    }

    //Will generate a square room given a bbox
    public void GenerateSquareRSector(GenerationUtils.BBox bbox, string name = "")
    {
        //First create the ground floor vertices
        List<Vector2> verts = new List<Vector2>()
        {
            new Vector2(bbox.bottom_left.x + 1f, bbox.bottom_left.y + 1f),
            new Vector2(bbox.bottom_left.x + 1f, bbox.top_right.y - 1f),
            new Vector2(bbox.top_right.x - 1f, bbox.top_right.y - 1f),
            new Vector2(bbox.top_right.x - 1f, bbox.bottom_left.y + 1f),
        };

        //Add linedevs to connect the sector
        Sector.LineDev a = new Sector.LineDev();
        Sector.LineDev b = new Sector.LineDev();
        Sector.LineDev c = new Sector.LineDev();
        Sector.LineDev d = new Sector.LineDev();
        a.edge = new Tuple<int, int>(0, 1);
        b.edge = new Tuple<int, int>(1, 2);
        c.edge = new Tuple<int, int>(2, 3);
        d.edge = new Tuple<int, int>(3, 0);
        List<Sector.LineDev> line_devs = new List<Sector.LineDev>() { a, b, c, d };
        
        Sector sqr_room = new Sector("Sewer_Square_Room" + "_" + name, 1.25f, 6f, verts, line_devs, mats[0], mats[1]);
        Debug.Log("Sewer_Square_Room" + "_" + name);
        sqr_room.Generate();
    }

    public void GenerateRooms(GenerationUtils.BBox root)
    {
        if(root.branches.Count == 0)
        {
            Debug.Log($"{root.bottom_left}, {root.top_right}");
            GenerateSquareRSector(root, $"{root.bottom_left}, {root.top_right}");
            return;
        }
        else
        {
            for(int i=0; i<root.branches.Count; i++)
            {
                GenerateRooms(root.branches[i]);
            }
        }
        return;
    }

    //Function will recursively attempt to reach room leaf nodes and shrink their bounding boxes down by 9 units returns false if a leaf is deemed invalid
    //this allows for removal of leaves from generation
    public bool ShrinkBoundingBoxes(GenerationUtils.BBox bbox, int depth = 0)
    {
        //reached a leaf node
        if (bbox.branches.Count == 0)
        {
            bbox.bottom_left = new Vector2Int(bbox.bottom_left.x, bbox.bottom_left.y + 9);
            bbox.top_right = new Vector2Int(bbox.top_right.x - 9, bbox.top_right.y);

            
            //some additional scaling
            int x = bbox.top_right.x - bbox.bottom_left.x;
            int y = bbox.top_right.y - bbox.bottom_left.y;
            
            Vector2Int top_left = new Vector2Int(bbox.bottom_left.x, bbox.top_right.y);
            if (x >= map_width * .75f || y >= map_height * .75f)
            {
                //make the room a square of the smallest dimension
                if (x > y) bbox.top_right = new Vector2Int(bbox.bottom_left.x + y, bbox.top_right.y);
                else bbox.bottom_left = new Vector2Int(bbox.bottom_left.x, bbox.top_right.y - x);
            }
           
            x = bbox.top_right.x - bbox.bottom_left.x;
            y = bbox.top_right.y - bbox.bottom_left.y;
            
            if (x > y * 4f / 3f) bbox.top_right = new Vector2Int((int)(bbox.bottom_left.x + (y * 4f / 3f)), bbox.top_right.y);
            else if (y > x * 4f / 3f) bbox.bottom_left = new Vector2Int(bbox.bottom_left.x, (int)(bbox.top_right.y - (x * 4f / 3f)));
            
            //see if the leaf is still a valid room for generation see if the bounds have crossed each other
            //the room must also have a width and height of at least 32 meters (2 seconds to cross the rrom))
            bool valid = true;
            if (bbox.bottom_left.x >= bbox.top_right.x || bbox.bottom_left.y >= bbox.top_right.y) valid = false;
            if (bbox.top_right.x - bbox.bottom_left.x < 32 || bbox.top_right.y - bbox.bottom_left.y < 32) valid = false;

            return valid;
        }
        //a non leaf node
        else
        {
            //call this function for all branches if one of them returns false then restart the loop
            bool valid = false;
            while (!valid)
            {
                //break early if this node loses all of it's branches
                if (bbox.branches.Count == 0) break;
                valid = true;
                int index = 0;
                for (int i = 0; i < bbox.branches.Count; i++)
                {
                    index = i;
                    valid = ShrinkBoundingBoxes(bbox.branches[i], depth + 1);
                    if(!valid) break;
                }

                //if the branch is not valid then remove it from this node
                if (!valid) bbox.branches.RemoveAt(index);
            }

            //if this function loses all of it's nodes run it in the function again otherwise return true
            if (bbox.branches.Count == 0) return ShrinkBoundingBoxes(bbox, depth);
            return true;
        }
    }
}
