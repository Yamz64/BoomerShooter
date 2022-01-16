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
        rooms = new List<GenerationUtils.Room>();
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

        //create a Delauney Triangulation connecting all of the rooms
        GenerationUtils.Triangle triangle = new GenerationUtils.Triangle(new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f));
        Debug.Log(triangle.WithinCircumCircle(new Vector2(0f, 0f)));
        List<Tuple<int, int>> d_triangulation_edges = new List<Tuple<int, int>>(GenerateDTriangulation());
        for(int i=0; i<d_triangulation_edges.Count; i++)
        {
            Debug.Log(d_triangulation_edges[i]);
        }
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
        rooms.Add(room);
    }

    //Will generate a circular room of random sides 6-16 given a bbox
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
        rooms.Add(room);
    }

    //driver for generating rooms
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

    //Returns a list of tuples storing the edges between rooms after forming a Delauney Triangulation
    public List<Tuple<int, int>> GenerateDTriangulation()
    {
        //1) loop through all of the rooms in the current generation and generate a list of their midpoints
        List<Vector2> midpoints = new List<Vector2>();

        for(int i=0; i<rooms.Count; i++)
        {
            Vector2 midpoint = (new Vector2(rooms[i].GetBounds().top_right.x - rooms[i].GetBounds().bottom_left.x, rooms[i].GetBounds().top_right.y - rooms[i].GetBounds().bottom_left.y) / 2f) + 
                rooms[i].GetBounds().bottom_left;
            midpoints.Add(midpoint);
        }

        //2) Now create a huge triangle that encompasses all of the points
        //first find the midpoint of all of the points
        Vector2 median = Vector2.zero;
        for(int i=0; i<midpoints.Count; i++)
        {
            median.x += midpoints[i].x;
            median.y += midpoints[i].y;
        }
        median /= midpoints.Count;

        //now find the furthest point's distance from the median point
        float furthest = 0;
        for(int i=0; i<midpoints.Count; i++)
        {
            if(Vector2.Distance(median, midpoints[i]) > furthest) furthest = Vector2.Distance(median, midpoints[i]);
        }

        //generate an equilateral triangle with medians of the furthest point's distance make sure that these points are marked for later when the domain changes
        List<GenerationUtils.Triangle> triangles = new List<GenerationUtils.Triangle>();
        GenerationUtils.Triangle super_triangle = new GenerationUtils.Triangle();

        triangles.Add(new GenerationUtils.Triangle());
        for(int i=0; i<3; i++)
        {
            float degrees = 0;
            switch (i)
            {
                case 0:
                    degrees = -45;
                    Vector2 tri_vert = median + Rotate2DVectorDeg(Vector2.down, degrees) * furthest;
                    triangles[0].SetA(tri_vert.x, tri_vert.y);
                    break;
                case 1:
                    degrees = -180;
                    tri_vert = median + Rotate2DVectorDeg(Vector2.down, degrees) * furthest;
                    triangles[0].SetB(tri_vert.x, tri_vert.y);
                    break;
                case 2:
                    degrees = -315;
                    tri_vert = median + Rotate2DVectorDeg(Vector2.down, degrees) * furthest;
                    triangles[0].SetC(tri_vert.x, tri_vert.y);
                    break;
                default:
                    break;
            }
        }

        //check to see if all points are inside of the triangle, if not then double the triangle's size
        bool valid = false;
        while (!valid)
        {
            valid = true;
            for(int i=0; i<midpoints.Count; i++)
            {
                BrushUtils utility = new BrushUtils();
                if(!utility.WithinTri(triangles[0].GetA(), triangles[0].GetB(), triangles[0].GetC(), midpoints[i]))
                {
                    valid = false;
                    break;
                }
            }
            if (!valid)
            {
                //consider the super triangle's verts and calculate their median vector from the centerpoint, double this to create a new vert
                Vector2 point = new Vector2(triangles[0].GetA().x, triangles[0].GetA().y);
                Vector2 median_vector = point - median;
                median_vector = median + median_vector * 2f;
                triangles[0].SetA(median_vector.x, median_vector.y);

                point = new Vector2(triangles[0].GetB().x, triangles[0].GetB().y);
                median_vector = point - median;
                median_vector = median + median_vector * 2f;
                triangles[0].SetB(median_vector.x, median_vector.y);

                point = new Vector2(triangles[0].GetC().x, triangles[0].GetC().y);
                median_vector = point - median;
                median_vector = median + median_vector * 2f;
                triangles[0].SetC(median_vector.x, median_vector.y);
            }
        }
        super_triangle = new GenerationUtils.Triangle(triangles[0].GetA(), triangles[0].GetB(), triangles[0].GetC());

        List<Vector2> domain = new List<Vector2>() { triangles[0].GetA(), triangles[0].GetB(), triangles[0].GetC() };
        GenerationUtils gen_utility = new GenerationUtils();
        //loop through every midpoint to generate the Delauney Triangulation 
        for(int i=0; i<midpoints.Count; i++)
        {
            /*
            //3) Consider a point, remove any triangles that it lies within, and update their borders as the domain
            valid = false;
            while (!valid)
            {
                valid = true;
                for (int j = 0; j < triangles.Count; j++)
                {
                    BrushUtils utility = new BrushUtils();
                    if (utility.WithinTri(triangles[j].GetA(), triangles[j].GetB(), triangles[j].GetC(), midpoints[i]))
                    {
                        domain.Add(triangles[j].GetA());
                        domain.Add(triangles[j].GetB());
                        domain.Add(triangles[j].GetC());

                        triangles.RemoveAt(j);
                        valid = false;
                        break;
                    }
                }
            }
            */
            gen_utility.SortClockwise(ref domain);

            //4) Create triangles with every other border edge of the domain if this is the first iteration then remove the super triangle from the list
            for (int j = 0; j < domain.Count; j++)
            {
                GenerationUtils.Triangle new_tri = new GenerationUtils.Triangle();
                if (j == domain.Count - 1)
                {
                    new_tri = new GenerationUtils.Triangle(midpoints[i], domain[j], domain[0]);
                }
                else new_tri = new GenerationUtils.Triangle(midpoints[i], domain[j], domain[j + 1]);
                triangles.Add(new_tri);
            }

            if (i == 0) triangles.RemoveAt(0);

            //5) Consider another point and see if any of the new triangles violate the delauney condition, delete these triangles,
            //these triangle's border edges will become the new domain, remove duplicates in the domain
            domain.Clear();
            if (i != midpoints.Count - 1)
            {
                valid = false;
                while (!valid)
                {
                    valid = true;
                    for (int j = 0; j < triangles.Count; j++)
                    {
                        if(triangles[j].WithinCircumCircle(midpoints[i + 1]))
                        {
                            domain.Add(triangles[j].GetA());
                            domain.Add(triangles[j].GetB());
                            domain.Add(triangles[j].GetC());
                            triangles.RemoveAt(j);
                            valid = false;
                            break;
                        }
                    }
                }

                valid = false;
                while (!valid) {
                    valid = true;
                    for (int j = 0; j < domain.Count; j++)
                    {
                        for (int k = 0; k < domain.Count; k++)
                        {
                            if (j == k) continue;
                            if (domain[j] == domain[k])
                            {
                                domain.RemoveAt(k);
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) break;
                    }
                }
            }

        }
        //6) When all points have been considered remove any triangles that have to do with the super triangle
        valid = false;
        while (!valid)
        {
            valid = true;
            for (int j = 0; j < triangles.Count; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Vector2 considered_vert = new Vector2();
                    switch (k)
                    {
                        case 0:
                            considered_vert = triangles[j].GetA();
                            break;
                        case 1:
                            considered_vert = triangles[j].GetB();
                            break;
                        case 2:
                            considered_vert = triangles[j].GetC();
                            break;
                        default:
                            considered_vert = triangles[j].GetA();
                            break;
                    }

                    if (considered_vert == super_triangle.GetA() || considered_vert == super_triangle.GetB() || considered_vert == super_triangle.GetC())
                    {
                        triangles.RemoveAt(j);
                        valid = false;
                        break;
                    }
                }
                if (!valid) break;
            }
        }

        //7) Convert all triangles to edges, remove duplicates, and then return
        List<Tuple<int, int>> exported_edges = new List<Tuple<int, int>>();
        for(int i=0; i<triangles.Count; i++)
        {
            int a_index = -1;
            int b_index = -1;
            int c_index = -1;
            for(int j=0; j<midpoints.Count; j++)
            {
                if (triangles[i].GetA() == midpoints[j]) a_index = j;
                else if (triangles[i].GetB() == midpoints[j]) b_index = j;
                else if (triangles[i].GetC() == midpoints[j]) c_index = j;
                if (a_index != -1 && b_index != -1 && c_index != -1) break;
            }
            exported_edges.Add(new Tuple<int, int>(a_index, b_index));
            exported_edges.Add(new Tuple<int, int>(b_index, c_index));
            exported_edges.Add(new Tuple<int, int>(c_index, a_index));
        }

        valid = false;
        while (!valid)
        {
            valid = true;
            for (int i = 0; i < exported_edges.Count; i++)
            {
                for (int j = 0; j < exported_edges.Count; j++)
                {
                    if (i == j) continue;
                    if((exported_edges[i].Item1 == exported_edges[j].Item1 && exported_edges[i].Item2 == exported_edges[j].Item2)
                        || (exported_edges[i].Item1 == exported_edges[j].Item2 && exported_edges[i].Item2 == exported_edges[j].Item1))
                    {
                        exported_edges.RemoveAt(j);
                        valid = false;
                        break;
                    }
                }
                if (!valid) break;
            }
        }

        return exported_edges;
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
