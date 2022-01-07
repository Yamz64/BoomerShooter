using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationUtils
{
    //A struct for containing information about the 2D bounds of level geometry (map, sectors, rooms, etc.)
    public class BBox {
        public Vector2Int bottom_left;
        public Vector2Int top_right;

        public BBox root;           //takes the structure of a tree to make it easier to add and remove bounding boxes
        public List<BBox> branches;

        //CONSTRUCTORS
        public BBox() : this(Vector2Int.zero, Vector2Int.zero) { }

        public BBox(Vector2Int b, Vector2Int t)
        {
            bottom_left = b;
            top_right = t;
            root = null;
            branches = new List<BBox>();
        }

        public BBox(BBox b)
        {
            bottom_left = b.bottom_left;
            top_right = b.top_right;
            root = b.root;
            branches = new List<BBox>();
            for(int i=0; i<b.branches.Count; i++)
            {
                branches.Add(new BBox(b.branches[i]));
            }
        }
    }

    //Class for organizing sectors into rooms
    public class Room
    {
        private string name;
        private BBox bounds;
        private List<Sector> sectors;
        private List<Sector> entrances;

        //CONSTRUCTOR
        public Room(string n = "Unnamed Room", BBox b = null, List<Sector> s = null, List<Sector> e = null)
        {
            name = n;
            bounds = new BBox();
            SetBounds(b);
            sectors = new List<Sector>();
            entrances = new List<Sector>();
            if (s != null) { for(int i=0; i<s.Count; i++) { sectors.Add(s[i]); } }
            if (e != null) { for (int i = 0; i < e.Count; i++) { entrances.Add(s[i]); } }
        }

        //ACCESSORS
        public string GetName() { return name; }
        public BBox GetBounds() { return bounds; }
        public List<Sector> GetSectors() { return sectors; }
        public List<Sector> GetEntrances() { return entrances; }

        //SETTERS
        public void SetName(string n) { name = n; }
        public void SetBounds(BBox b)
        {
            if (b != null)
            {
                bounds.bottom_left = b.bottom_left;
                bounds.top_right = b.top_right;
                bounds.root = b.root;
                bounds.branches = new List<BBox>();
                for (int i = 0; i < b.branches.Count; i++) { bounds.branches.Add(b.branches[i]); }
            }
        }
        public void SetSectors(List<Sector> s)
        {
            sectors = new List<Sector>();
            for(int i=0; i<s.Count; i++) { sectors.Add(s[i]); }
        }
        public void SetEntrances(List<Sector> e)
        {
            sectors = new List<Sector>();
            for (int i = 0; i < e.Count; i++) { entrances.Add(e[i]); }
        }

        //MISC
        public void AddSector(Sector s) { sectors.Add(s); }
        public void AddEntrance(Sector e) { entrances.Add(e); }
        public void RemoveSector(int i) { sectors.RemoveAt(i); }
        public void RemoveEntrance(int i) { entrances.RemoveAt(i); }
        public void Generate()
        {
            if (sectors.Count == 0 && entrances.Count == 0) return;
            for(int i=0; i<sectors.Count; i++) { sectors[i].Generate(); }
            for (int i = 0; i < entrances.Count; i++) { entrances[i].Generate(); }
        }
    }

    public int Num_Leaves(BBox node)
    {
        int leaf_number = 0;
        if (node.branches.Count == 0) return 1;
        for(int i=0; i<node.branches.Count; i++)
        {
            leaf_number += Num_Leaves(node.branches[i]);
        }
        return leaf_number;
    }

    //Function will take in map width and height dividing it into room number of smaller segments, this will serve as the bounding box for rooms in level generation
    public BBox GenerateMapBounds(int map_width, int map_height, int room_number)
    {
        //1) Create a large BBox given the width and height of the map
        BBox root = new BBox(Vector2Int.zero, new Vector2Int(map_width, map_height));

        if (room_number == 1) return root;

        //2) Continue to divide the map randomly until the room count has been reached
        int room_count = 1;
        while(room_count < room_number)
        {
            //first choose a random leaf to start dividing in the map bbox tree, also decide whether this leaf should be split by width or height
            //division orientation false = divide by height, true = divide by width
            BBox selected_room = root;
            bool division_orientation = false;
            while(selected_room.branches.Count != 0)
            {
                selected_room = selected_room.branches[Random.Range(0, selected_room.branches.Count)];
                division_orientation = !division_orientation;
            }

            //divide by a random amount between 1 and the remaining room count
            int divisions = Random.Range(1, room_number - room_count);

            List<int> partitions = new List<int>();

            for(int i=0; i < divisions; i++)
            {
                //create a new partition
                //by height
                int potential_partition = 0;
                if (!division_orientation)
                {
                    //make sure that this partition never is equal to another partition already stored
                    bool valid = false;
                    int termination_threshold = 10;
                    while (!valid)
                    {
                        valid = true;
                        //create a bell curve so that the partition will tend to lerp towards .5f
                        float lerp_amount = 0;
                        for (int j = 0; j < 30; j++) { lerp_amount += Random.Range(0f, 1f); }
                        lerp_amount /= 30f;

                        potential_partition = (int)Mathf.Lerp(selected_room.bottom_left.x, selected_room.top_right.x, Random.Range(0f, 1f));
                        if (potential_partition == 0) potential_partition = 1;
                        if (potential_partition == selected_room.top_right.y) potential_partition -= 1;

                        for (int j = 0; j < partitions.Count; j++)
                        {
                            if (potential_partition == partitions[j])
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) termination_threshold--;
                        else termination_threshold = 10;
                        if (termination_threshold == 0) valid = true;
                    }

                    //after a partition is deemed valid add it to the partitions list
                    partitions.Add(potential_partition);
                }
                //by width
                else
                {
                    //make sure that this partition never is equal to another partition already stored
                    bool valid = false;
                    int termination_threshold = 10;
                    while (!valid)
                    {
                        valid = true;
                        //create a bell curve so that the partition will tend to lerp towards .5f
                        float lerp_amount = 0;
                        for(int j=0; j<30; j++) { lerp_amount += Random.Range(0f, 1f); }
                        lerp_amount /= 30f;

                        potential_partition = (int)Mathf.Lerp(selected_room.bottom_left.y, selected_room.top_right.y, lerp_amount);
                        if (potential_partition == 0) potential_partition = 1;
                        if (potential_partition == selected_room.top_right.x) potential_partition -= 1;

                        for (int j = 0; j < partitions.Count; j++)
                        {
                            if (potential_partition == partitions[j])
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) termination_threshold--;
                        else termination_threshold = 10;
                        if (termination_threshold == 0) valid = true;
                    }

                    //after a partition is deemed valid add it to the partitions list
                    partitions.Add(potential_partition);
                }
            }

            //sort partitions for easier creation of new leaves to map bbox tree
            partitions.Sort();

            for(int i=-1; i<partitions.Count; i++)
            {
                BBox new_leaf = new BBox();
                new_leaf.root = selected_room;
                //create the first leaf
                if(i == -1)
                {
                    //by height
                    if (!division_orientation)
                    {
                        new_leaf.bottom_left = selected_room.bottom_left;
                        new_leaf.top_right = new Vector2Int(partitions[0], selected_room.top_right.y);
                    }
                    //by width
                    else
                    {
                        new_leaf.bottom_left = selected_room.bottom_left;
                        new_leaf.top_right = new Vector2Int(selected_room.top_right.x, partitions[0]);
                    }
                }
                //create the last leaf
                else if(i == partitions.Count - 1)
                {
                    //by height
                    if (!division_orientation)
                    {
                        new_leaf.bottom_left = new Vector2Int(partitions[i], selected_room.bottom_left.y);
                        new_leaf.top_right = selected_room.top_right;
                    }
                    //by width
                    else
                    {
                        new_leaf.bottom_left = new Vector2Int(selected_room.bottom_left.x, partitions[i]);
                        new_leaf.top_right = selected_room.top_right;
                    }
                }
                //all other leaf cases
                else
                {
                    //by height
                    if (!division_orientation)
                    {
                        new_leaf.bottom_left = new Vector2Int(partitions[i], selected_room.bottom_left.y);
                        new_leaf.top_right = new Vector2Int(partitions[i + 1], selected_room.top_right.y);
                    }
                    //by width
                    else
                    {
                        new_leaf.bottom_left = new Vector2Int(selected_room.bottom_left.x, partitions[i]);
                        new_leaf.top_right = new Vector2Int(selected_room.top_right.x, partitions[i + 1]);
                    }

                }

                //Add this new leaf to the root leaf
                selected_room.branches.Add(new_leaf);
            }

            //perform a recursive check to see how many rooms have been created
            room_count = Num_Leaves(root);
        }

        return root;
    }
}
