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
    private List<GenerationUtils.Room> rooms;

    public void Awake()
    {
        //Get Necessary information
        GenerateMap map_generator = GameObject.FindGameObjectWithTag("GenerationManager").GetComponent<GenerateMap>();
        map_width = map_generator.map_width;
        map_height = map_generator.map_height;
        room_number = map_generator.room_number;
    }

    //function takes a vector and returns that vector rotated by deg degrees
    public Vector2 Rotate2DVectorDeg(Vector2 vec, float deg)
    {
        float radians = Mathf.Deg2Rad * deg;

        float new_x = Vector2.Dot(new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians)), vec);
        float new_y = Vector2.Dot(new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)), vec);

        return new Vector2(new_x, new_y);        
    }

    //called when the sewer generation event happens
    public void GenerationDriver()
    {
        GenerationUtils utility = new GenerationUtils();
        GenerationUtils.BBox bbox = new GenerationUtils.BBox(utility.GenerateMapBounds(map_width, map_height, room_number));

        //Generate shrunken bounding boxes for play
        //if this ever returns false then no rooms were generated
        if(ShrinkBoundingBoxes(bbox) == false) Debug.LogError("No valid rooms for generation unfortunately");

        //given these modified bounding boxes, create room sectors
        GenerateRooms(bbox);
    }

    //Will generate a square room given a bbox
    public void GenerateSquareRSector(GenerationUtils.BBox bbox, string name = "")
    {
        //initialize some variables ahead of time
        GenerationUtils.Room room = new GenerationUtils.Room($"Sewer_Square_{name}");
        room.SetBounds(bbox);
        //1)Generate the Floor
        //First create the ground floor vertices
        List<Vector2> floor_verts = new List<Vector2>()
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
        
        //Initialize the floor and add to a list of room_sectors
        Sector floor = new Sector($"Sewer_Square_{name}_floor", 1.25f, 6f, floor_verts, line_devs, mats[0], mats[1]);
        room.AddSector(floor);

        //2) Generate the walls
        //first create trim pillars
        for(int i=0; i<floor_verts.Count; i++)
        {
            //initialize pillar variables
            List<Vector2> trim_a_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            List<Vector2> trim_b_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

            //each index will have unique pillar placements
            switch (i)
            {
                case 0:
                    //First Pillar
                    trim_a_verts[0] = new Vector2(floor_verts[0].x, floor_verts[0].y - 1);
                    trim_a_verts[1] = new Vector2(floor_verts[0].x, floor_verts[0].y);
                    trim_a_verts[2] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y);
                    trim_a_verts[3] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y - 1);

                    //Second Pillar
                    trim_b_verts[0] = new Vector2(floor_verts[0].x - 1, floor_verts[0].y);
                    trim_b_verts[1] = new Vector2(floor_verts[0].x - 1, floor_verts[0].y + 1);
                    trim_b_verts[2] = new Vector2(floor_verts[0].x, floor_verts[0].y + 1);
                    trim_b_verts[3] = new Vector2(floor_verts[0].x, floor_verts[0].y);
                    break;
                case 1:
                    //First Pillar
                    trim_a_verts[0] = new Vector2(floor_verts[1].x - 1, floor_verts[1].y - 1);
                    trim_a_verts[1] = new Vector2(floor_verts[1].x - 1, floor_verts[1].y);
                    trim_a_verts[2] = new Vector2(floor_verts[1].x, floor_verts[1].y);
                    trim_a_verts[3] = new Vector2(floor_verts[1].x, floor_verts[1].y - 1);

                    //Second Pillar
                    trim_b_verts[0] = new Vector2(floor_verts[1].x, floor_verts[1].y);
                    trim_b_verts[1] = new Vector2(floor_verts[1].x, floor_verts[1].y + 1);
                    trim_b_verts[2] = new Vector2(floor_verts[1].x + 1, floor_verts[1].y + 1);
                    trim_b_verts[3] = new Vector2(floor_verts[1].x + 1, floor_verts[1].y);
                    break;
                case 2:
                    //First Pillar
                    trim_a_verts[0] = new Vector2(floor_verts[2].x - 1, floor_verts[2].y);
                    trim_a_verts[1] = new Vector2(floor_verts[2].x - 1, floor_verts[2].y + 1);
                    trim_a_verts[2] = new Vector2(floor_verts[2].x, floor_verts[2].y + 1);
                    trim_a_verts[3] = new Vector2(floor_verts[2].x, floor_verts[2].y);

                    //Second Pillar
                    trim_b_verts[0] = new Vector2(floor_verts[2].x, floor_verts[2].y - 1);
                    trim_b_verts[1] = new Vector2(floor_verts[2].x, floor_verts[2].y);
                    trim_b_verts[2] = new Vector2(floor_verts[2].x + 1, floor_verts[2].y);
                    trim_b_verts[3] = new Vector2(floor_verts[2].x + 1, floor_verts[2].y - 1);
                    break;
                case 3:
                    //First Pillar
                    trim_a_verts[0] = new Vector2(floor_verts[3].x, floor_verts[0].y);
                    trim_a_verts[1] = new Vector2(floor_verts[3].x, floor_verts[0].y + 1);
                    trim_a_verts[2] = new Vector2(floor_verts[3].x + 1, floor_verts[0].y + 1);
                    trim_a_verts[3] = new Vector2(floor_verts[3].x + 1, floor_verts[0].y);

                    //Second Pillar
                    trim_b_verts[0] = new Vector2(floor_verts[3].x - 1, floor_verts[3].y - 1);
                    trim_b_verts[1] = new Vector2(floor_verts[3].x - 1, floor_verts[3].y);
                    trim_b_verts[2] = new Vector2(floor_verts[3].x, floor_verts[3].y);
                    trim_b_verts[3] = new Vector2(floor_verts[3].x, floor_verts[3].y - 1);
                    break;
                default:
                    Debug.LogError("Uhhh, this shouldn't be happening???");
                    break;
            }

            //Initialize Pillar Sectors
            Sector pillar_a = new Sector($"Sewer_Square_{name}_PillarA_{floor_verts[i]}", 6f, 6f, trim_a_verts, line_devs, mats[2], mats[2]);
            Sector pillar_b = new Sector($"Sewer_Square_{name}_PillarB_{floor_verts[i]}", 6f, 6f, trim_b_verts, line_devs, mats[2], mats[2]);
            room.AddSector(pillar_a);
            room.AddSector(pillar_b);
        }

        //Roll to see which wall will become an entrance
        int entrance_dir = UnityEngine.Random.Range(0, 4);
        //next create wall sectors
        for(int i=0; i < 4; i++)
        {
            //is this meant to be a wall and not an entrance?
            if (i != entrance_dir)
            {
                List<Vector2> wall_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                //generate wall based on iteration
                switch (i)
                {
                    //left
                    case 0:
                        wall_verts[0] = new Vector2(floor_verts[i].x - 1, floor_verts[i].y + 1);
                        wall_verts[1] = new Vector2(floor_verts[i].x - 1, floor_verts[i + 1].y - 1);
                        wall_verts[2] = new Vector2(floor_verts[i].x, floor_verts[i + 1].y - 1);
                        wall_verts[3] = new Vector2(floor_verts[i].x, floor_verts[i].y + 1);
                        break;
                    //up
                    case 1:
                        wall_verts[0] = new Vector2(floor_verts[i].x + 1, floor_verts[i].y);
                        wall_verts[1] = new Vector2(floor_verts[i].x + 1, floor_verts[i].y + 1);
                        wall_verts[2] = new Vector2(floor_verts[i + 1].x - 1, floor_verts[i].y + 1);
                        wall_verts[3] = new Vector2(floor_verts[i + 1].x - 1, floor_verts[i].y);
                        break;
                    //right
                    case 2:
                        wall_verts[0] = new Vector2(floor_verts[i].x, floor_verts[i + 1].y + 1);
                        wall_verts[1] = new Vector2(floor_verts[i].x, floor_verts[i].y - 1);
                        wall_verts[2] = new Vector2(floor_verts[i].x + 1, floor_verts[i].y - 1);
                        wall_verts[3] = new Vector2(floor_verts[i].x + 1, floor_verts[i + 1].y + 1);
                        break;
                    //down
                    case 3:
                        wall_verts[0] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y - 1);
                        wall_verts[1] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y);
                        wall_verts[2] = new Vector2(floor_verts[i].x - 1, floor_verts[0].y);
                        wall_verts[3] = new Vector2(floor_verts[i].x - 1, floor_verts[0].y - 1);
                        break;
                    //should literally never happen
                    default:
                        break;
                }

                Sector wall = new Sector($"Sewer_Square_{name}_Wall{i}", 6f, 6f, wall_verts, line_devs, mats[1], mats[1]);
                room.AddSector(wall);
            }
            //if it's meant to be an entrance then 5 sectors need to be generated
            else
            {
                List<Vector2> entrance_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                List<Vector2> trim_a_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                List<Vector2> trim_b_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                List<Vector2> wall_a_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
                List<Vector2> wall_b_verts = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

                //generate entrance wall based on iteration
                switch (i)
                {
                    //left
                    case 0:
                        //first generate a 9 tile wide entrance sector somewhere on the wall
                        int first_bound = (int)UnityEngine.Random.Range(floor_verts[0].y + 3, floor_verts[1].y - 3);
                        int second_bound = first_bound + 9;
                        //edge case for entrance generation
                        if (first_bound + 9 > floor_verts[1].y - 3) second_bound = first_bound - 9;

                        //order first and second bound to insure clockwise generation
                        if (second_bound < first_bound)
                        {
                            int temp = first_bound;
                            first_bound = second_bound;
                            second_bound = temp;
                        }

                        entrance_verts[0] = new Vector2(floor_verts[0].x - 1, first_bound);
                        entrance_verts[1] = new Vector2(floor_verts[0].x - 1, second_bound);
                        entrance_verts[2] = new Vector2(floor_verts[0].x, second_bound);
                        entrance_verts[3] = new Vector2(floor_verts[0].x, first_bound);

                        //next generate the trim walls
                        trim_a_verts[0] = new Vector2(floor_verts[0].x - 1, first_bound - 1);
                        trim_a_verts[1] = new Vector2(floor_verts[0].x - 1, first_bound);
                        trim_a_verts[2] = new Vector2(floor_verts[0].x, first_bound);
                        trim_a_verts[3] = new Vector2(floor_verts[0].x, first_bound - 1);

                        trim_b_verts[0] = new Vector2(floor_verts[0].x - 1, second_bound);
                        trim_b_verts[1] = new Vector2(floor_verts[0].x - 1, second_bound + 1);
                        trim_b_verts[2] = new Vector2(floor_verts[0].x, second_bound + 1);
                        trim_b_verts[3] = new Vector2(floor_verts[0].x, second_bound);

                        //now generate walls
                        wall_a_verts[0] = new Vector2(floor_verts[0].x - 1, floor_verts[0].y + 1);
                        wall_a_verts[1] = new Vector2(floor_verts[0].x - 1, first_bound - 1);
                        wall_a_verts[2] = new Vector2(floor_verts[0].x, first_bound - 1);
                        wall_a_verts[3] = new Vector2(floor_verts[0].x, floor_verts[0].y + 1);

                        wall_b_verts[0] = new Vector2(floor_verts[0].x - 1, second_bound + 1);
                        wall_b_verts[1] = new Vector2(floor_verts[0].x - 1, floor_verts[1].y - 1);
                        wall_b_verts[2] = new Vector2(floor_verts[3].x, floor_verts[1].y - 1);
                        wall_b_verts[3] = new Vector2(floor_verts[3].x, second_bound + 1);
                        break;
                    //up
                    case 1:
                        //first generate a 9 tile wide entrance sector somewhere on the wall
                        first_bound = (int)UnityEngine.Random.Range(floor_verts[1].x + 3, floor_verts[2].x - 3);
                        second_bound = first_bound + 9;
                        //edge case for entrance generation
                        if (first_bound + 9 > floor_verts[2].x - 3) second_bound = first_bound - 9;

                        //order first and second bound to insure clockwise generation
                        if (second_bound < first_bound)
                        {
                            int temp = first_bound;
                            first_bound = second_bound;
                            second_bound = temp;
                        }
                        
                        entrance_verts[0] = new Vector2(first_bound, floor_verts[1].y);
                        entrance_verts[1] = new Vector2(first_bound, floor_verts[1].y + 1);
                        entrance_verts[2] = new Vector2(second_bound, floor_verts[1].y + 1);
                        entrance_verts[3] = new Vector2(second_bound, floor_verts[1].y);

                        //next generate the trim walls
                        trim_a_verts[0] = new Vector2(first_bound - 1, floor_verts[1].y);
                        trim_a_verts[1] = new Vector2(first_bound - 1, floor_verts[1].y + 1);
                        trim_a_verts[2] = new Vector2(first_bound, floor_verts[1].y + 1);
                        trim_a_verts[3] = new Vector2(first_bound, floor_verts[1].y);

                        trim_b_verts[0] = new Vector2(second_bound, floor_verts[1].y);
                        trim_b_verts[1] = new Vector2(second_bound, floor_verts[1].y + 1);
                        trim_b_verts[2] = new Vector2(second_bound + 1, floor_verts[1].y + 1);
                        trim_b_verts[3] = new Vector2(second_bound + 1, floor_verts[1].y);

                        //now generate walls
                        wall_a_verts[0] = new Vector2(floor_verts[1].x + 1, floor_verts[1].y);
                        wall_a_verts[1] = new Vector2(floor_verts[1].x + 1, floor_verts[1].y + 1);
                        wall_a_verts[2] = new Vector2(first_bound - 1, floor_verts[1].y + 1);
                        wall_a_verts[3] = new Vector2(first_bound - 1, floor_verts[1].y);

                        wall_b_verts[0] = new Vector2(second_bound + 1, floor_verts[1].y);
                        wall_b_verts[1] = new Vector2(second_bound + 1, floor_verts[1].y + 1);
                        wall_b_verts[2] = new Vector2(floor_verts[2].x - 1, floor_verts[1].y + 1);
                        wall_b_verts[3] = new Vector2(floor_verts[2].x - 1, floor_verts[1].y);
                        break;
                    //right
                    case 2:
                        //first generate a 9 tile wide entrance sector somewhere on the wall
                        first_bound = (int)UnityEngine.Random.Range(floor_verts[3].y + 3, floor_verts[2].y - 3);
                        second_bound = first_bound + 9;
                        //edge case for entrance generation
                        if (first_bound + 9 > floor_verts[1].y - 3) second_bound = first_bound - 9;

                        //order first and second bound to insure clockwise generation
                        if (second_bound < first_bound)
                        {
                            int temp = first_bound;
                            first_bound = second_bound;
                            second_bound = temp;
                        }

                        entrance_verts[0] = new Vector2(floor_verts[3].x, first_bound);
                        entrance_verts[1] = new Vector2(floor_verts[3].x, second_bound);
                        entrance_verts[2] = new Vector2(floor_verts[3].x + 1, second_bound);
                        entrance_verts[3] = new Vector2(floor_verts[3].x + 1, first_bound);

                        //next generate the trim walls
                        trim_a_verts[0] = new Vector2(floor_verts[3].x, first_bound - 1);
                        trim_a_verts[1] = new Vector2(floor_verts[3].x, first_bound);
                        trim_a_verts[2] = new Vector2(floor_verts[3].x + 1, first_bound);
                        trim_a_verts[3] = new Vector2(floor_verts[3].x + 1, first_bound - 1);

                        trim_b_verts[0] = new Vector2(floor_verts[3].x, second_bound);
                        trim_b_verts[1] = new Vector2(floor_verts[3].x, second_bound + 1);
                        trim_b_verts[2] = new Vector2(floor_verts[3].x + 1, second_bound + 1);
                        trim_b_verts[3] = new Vector2(floor_verts[3].x + 1, second_bound);

                        //now generate walls
                        wall_a_verts[0] = new Vector2(floor_verts[3].x, floor_verts[3].y + 1);
                        wall_a_verts[1] = new Vector2(floor_verts[3].x, first_bound - 1);
                        wall_a_verts[2] = new Vector2(floor_verts[3].x + 1, first_bound - 1);
                        wall_a_verts[3] = new Vector2(floor_verts[3].x + 1, floor_verts[3].y + 1);

                        wall_b_verts[0] = new Vector2(floor_verts[3].x, second_bound + 1);
                        wall_b_verts[1] = new Vector2(floor_verts[3].x, floor_verts[2].y - 1);
                        wall_b_verts[2] = new Vector2(floor_verts[3].x + 1, floor_verts[2].y - 1);
                        wall_b_verts[3] = new Vector2(floor_verts[3].x + 1, second_bound + 1);
                        break;
                    //down
                    case 3:
                        //first generate a 9 tile wide entrance sector somewhere on the wall
                        first_bound = (int)UnityEngine.Random.Range(floor_verts[0].x + 3, floor_verts[3].x - 3);
                        second_bound = first_bound + 9;
                        //edge case for entrance generation
                        if (first_bound + 9 > floor_verts[3].x - 3) second_bound = first_bound - 9;

                        //order first and second bound to insure clockwise generation
                        if (second_bound < first_bound)
                        {
                            int temp = first_bound;
                            first_bound = second_bound;
                            second_bound = temp;
                        }

                        entrance_verts[0] = new Vector2(first_bound, floor_verts[0].y - 1);
                        entrance_verts[1] = new Vector2(first_bound, floor_verts[0].y);
                        entrance_verts[2] = new Vector2(second_bound, floor_verts[0].y);
                        entrance_verts[3] = new Vector2(second_bound, floor_verts[0].y - 1);

                        //next generate the trim walls
                        trim_a_verts[0] = new Vector2(first_bound - 1, floor_verts[0].y - 1);
                        trim_a_verts[1] = new Vector2(first_bound - 1, floor_verts[0].y);
                        trim_a_verts[2] = new Vector2(first_bound, floor_verts[0].y);
                        trim_a_verts[3] = new Vector2(first_bound, floor_verts[0].y - 1);

                        trim_b_verts[0] = new Vector2(second_bound, floor_verts[0].y - 1);
                        trim_b_verts[1] = new Vector2(second_bound, floor_verts[0].y);
                        trim_b_verts[2] = new Vector2(second_bound + 1, floor_verts[0].y);
                        trim_b_verts[3] = new Vector2(second_bound + 1, floor_verts[0].y - 1);

                        //now generate walls
                        wall_a_verts[0] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y - 1);
                        wall_a_verts[1] = new Vector2(floor_verts[0].x + 1, floor_verts[0].y);
                        wall_a_verts[2] = new Vector2(first_bound - 1, floor_verts[0].y);
                        wall_a_verts[3] = new Vector2(first_bound - 1, floor_verts[0].y - 1);

                        wall_b_verts[0] = new Vector2(second_bound + 1, floor_verts[0].y - 1);
                        wall_b_verts[1] = new Vector2(second_bound + 1, floor_verts[0].y);
                        wall_b_verts[2] = new Vector2(floor_verts[3].x - 1, floor_verts[0].y);
                        wall_b_verts[3] = new Vector2(floor_verts[3].x - 1, floor_verts[0].y - 1);
                        break;
                    //Should literally never happen
                    default:
                        break;
                }

                Sector entrance = new Sector($"Sewer_Square_{name}Entrance{i}", 1f, 6f, entrance_verts, line_devs, mats[0], mats[1]);
                Sector trim_a = new Sector($"Sewer_Square_{name}EntranceTrimA{i}", 6f, 6f, trim_a_verts, line_devs, mats[2], mats[2]);
                Sector trim_b = new Sector($"Sewer_Square_{name}EntranceTrimB{i}", 6f, 6f, trim_b_verts, line_devs, mats[2], mats[2]);
                Sector wall_a = new Sector($"Sewer_Square_{name}EntranceWallA{i}", 6f, 6f, wall_a_verts, line_devs, mats[1], mats[1]);
                Sector wall_b = new Sector($"Sewer_Square_{name}EntranceWallB{i}", 6f, 6f, wall_b_verts, line_devs, mats[1], mats[1]);

                room.AddEntrance(entrance);
                room.AddSector(trim_a);
                room.AddSector(trim_b);
                room.AddSector(wall_a);
                room.AddSector(wall_b);

            }
        }
        room.Generate();
    }

    public void GenerateCircularRSector(GenerationUtils.BBox bbox, string name = "")
    {
        //initialize some variables ahead of time
        GenerationUtils.Room room = new GenerationUtils.Room($"Sewer_Square_{name}");
        room.SetBounds(bbox);
        //1)Generate the Floor
        //First find the centerpoint of the bounding box
        Vector2 center = new Vector2(bbox.bottom_left.x + (bbox.top_right.x - bbox.bottom_left.x) / 2f, 
            bbox.bottom_left.y + (bbox.top_right.y - bbox.bottom_left.y) / 2f);

        //Next find the radius of the circular room
        float radius = 0f;
        if (bbox.top_right.x - bbox.bottom_left.x > bbox.top_right.y - bbox.bottom_left.y) radius = (bbox.top_right.y - bbox.bottom_left.y) / 2f;
        else radius = (bbox.top_right.x - bbox.bottom_left.x) / 2f;

        //rotate a vector around the centerpoint by a preset random increment to generate the circular room's floor and ceiling verts
        //14 sometimes produces a mysterious bug that prevents sectors from generating edges, never allow 14 to generate
        int segments = UnityEngine.Random.Range(6, 17);
        if (segments == 14) segments = 6;

        List<Vector2> floor_verts = new List<Vector2>();
        
        for(int i=0; i<segments; i++)
        {
            //find vert location by adding rotated vector to centerpoint
            float degrees = (360f / (float)segments) * i;

            Vector2 new_vert = center + Rotate2DVectorDeg(-Vector2.up, -degrees) * radius;

            floor_verts.Add(new_vert);
        }

        //generate the linedevs between sector's verts
        List<Sector.LineDev> floor_line_devs = new List<Sector.LineDev>();
        for(int i=0; i<floor_verts.Count; i++)
        {
            Sector.LineDev floor_line_dev = new Sector.LineDev();
            //the last case
            if (i == floor_verts.Count - 1) floor_line_dev.edge = new Tuple<int, int>(i, 0);
            else floor_line_dev.edge = new Tuple<int, int>(i, i + 1);

            floor_line_devs.Add(floor_line_dev);
        }

        //finally generate the floor sector
        Sector floor = new Sector($"Sewer_Circle_{name}_floor", 1.25f, 6f, floor_verts, floor_line_devs, mats[0], mats[1]);
        room.AddSector(floor);

        room.Generate();
    }

    public void GenerateRooms(GenerationUtils.BBox root)
    {
        if(root.branches.Count == 0)
        {
            //1 in 3 chance that generated room is circular
            int circle_chance = UnityEngine.Random.Range(0, 3);
            if (circle_chance != 0) GenerateSquareRSector(root, $"{root.bottom_left}, {root.top_right}");
            else GenerateCircularRSector(root, $"{root.bottom_left}, {root.top_right}");
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
