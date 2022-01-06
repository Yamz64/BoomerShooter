using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector
{
    //simple structure holds information about edges in geometry
    public struct LineDev
    {
        public Tuple<int, int> edge;
        public bool direction;
    }

    private string name;                //the name of this sector for generation purposes
    private float floor_height;         //how high is the sector's floor (from 0 to infinity)
    private float ceiling_height;       //how high is the sector's ceiling (from the floor's height to infinity)
    private List<Vector2> verts;        //2Dimensional representation of map through list of vertices
    private List<LineDev> line_devs;    //basically edges of vertices
    private Material floor_mat;         //the material attached to the sector's floor brush
    private Material ceiling_mat;       //the material attached to the sector's ceiling brush

    //CONSTRUCTOR
    public Sector(string n = "Unnamed Sector", float f = 0, float c = 0, List<Vector2> v = null, List<LineDev> l = null, Material f_mat = null, Material c_mat = null)
    {
        name = n;
        floor_height = f;
        ceiling_height = c;
        verts = new List<Vector2>();
        SetVerts(v);
        line_devs = new List<LineDev>();
        SetLineDevs(l);
        floor_mat = f_mat;
        ceiling_mat = c_mat;
    }

    //ACCESSORS
    public string GetName() { return name; }
    public float GetFHeight() { return floor_height; }
    public float GetCHeight() { return ceiling_height; }
    public List<Vector2> GetVerts() { return verts; }
    public List<LineDev> GetLineDevs() { return line_devs; }

    //SETTERS
    public void SetName(string n) { name = n; }
    public void SetFHeight(float h) {
        if(h <= 0)
        {
            floor_height = 0;
            return;
        }
        floor_height = h;
    }
    public void SetCHeight(float c) {
        if(c <= floor_height)
        {
            ceiling_height = floor_height;
            return;
        }
        ceiling_height = c;
    }
    public void SetVerts(List<Vector2> v)
    {
        verts.Clear();
        for (int i = 0; i < v.Count; i++) { verts.Add(v[i]); }
    }
    public void SetLineDevs(List<LineDev> l)
    {
        line_devs.Clear();
        for(int i=0; i < l.Count; i++) { line_devs.Add(l[i]); }
    }
    public void SetFloorMat(Material m) { floor_mat = m; }
    public void SetCeilingMat(Material m) { ceiling_mat = m; }

    //MISC
    public void AddVert(Vector2 v) { verts.Add(v); }
    public void AddLineDev(LineDev l) { line_devs.Add(l); }
    public void RemoveVert(int i) { verts.RemoveAt(i); }
    public void RemoveLineDev(int i) { line_devs.RemoveAt(i); }
    public void ClearVerts() { verts.Clear(); }
    public void ClearLineDevs() { line_devs.Clear(); }

    //function will handle generation of a sector
    public void Generate()
    {
        //1) Generate an object to hold all generated brushes
        GameObject sector_geo = new GameObject("Sector Geometry");

        //2) Generate a floor Brush from the vertex data
        BrushUtils.Brush floor = new BrushUtils.Brush();
        floor.name = name + "_" + "floor";
        floor.verts = new List<Vector3>();
        floor.edges = new List<List<Tuple<int, int>>>();
        floor.faces = new List<List<int>>();
        //First generate the floor brush's bottom vertices
        for(int i=verts.Count - 1; i >= 0; i--)
        {
            floor.verts.Add(new Vector3(verts[i].x, -1.0f, verts[i].y));
        }
        //Next generate the floor brush's top vertices
        for(int i=0; i < verts.Count; i++)
        {
            floor.verts.Add(new Vector3(verts[i].x, floor_height, verts[i].y));
        }
        //create edges for those verts
        floor.edges.Add(new List<Tuple<int, int>>());
        for(int i=1; i < floor.verts.Count / 2; i++)
        {
            if(i == (floor.verts.Count / 2) - 1)
            {
                floor.edges[0].Add(new Tuple<int, int>(i - 1, i));
                floor.edges[0].Add(new Tuple<int, int>(i, 0));
                break;
            }
            floor.edges[0].Add(new Tuple<int, int>(i - 1, i));
        }
        floor.edges.Add(new List<Tuple<int, int>>());
        for(int i=(floor.verts.Count / 2) + 1; i<floor.verts.Count; i++)
        {
            if (i == floor.verts.Count - 1)
            {
                floor.edges[1].Add(new Tuple<int, int>(i - 1, i));
                floor.edges[1].Add(new Tuple<int, int>(i, floor.verts.Count / 2));
                break;
            }
            floor.edges[1].Add(new Tuple<int, int>(i - 1, i));
        }
        //loop through the rest of those verts and create side face edges initialize some variables as a sanity check
        int half = (floor.verts.Count / 2) - 1;
        int max = floor.verts.Count - 1;
        for(int i=0; i <= half; i++)
        {
            //special case at the end of the loop
            if (i == half)
            {
                //Create side verts first
                floor.verts.Add(new Vector3(floor.verts[i].x, floor.verts[i].y, floor.verts[i].z));
                floor.verts.Add(new Vector3(floor.verts[max - i].x, floor.verts[max - i].y, floor.verts[max - i].z));
                floor.verts.Add(new Vector3(floor.verts[max].x, floor.verts[max].y, floor.verts[max].z));
                floor.verts.Add(new Vector3(floor.verts[0].x, floor.verts[0].y, floor.verts[0].z));
            }
            else
            {
                //Create side verts first
                floor.verts.Add(new Vector3(floor.verts[i].x, floor.verts[i].y, floor.verts[i].z));
                floor.verts.Add(new Vector3(floor.verts[max - i].x, floor.verts[max - i].y, floor.verts[max - i].z));
                floor.verts.Add(new Vector3(floor.verts[max - i - 1].x, floor.verts[max - i - 1].y, floor.verts[max - i - 1].z));
                floor.verts.Add(new Vector3(floor.verts[i + 1].x, floor.verts[i + 1].y, floor.verts[i + 1].z));
            }

            //add the edges
            floor.edges.Add(new List<Tuple<int, int>>());
            floor.edges[2 + i].Add(new Tuple<int, int>(floor.verts.Count - 4, floor.verts.Count - 3));
            floor.edges[2 + i].Add(new Tuple<int, int>(floor.verts.Count - 3, floor.verts.Count - 2));
            floor.edges[2 + i].Add(new Tuple<int, int>(floor.verts.Count - 2, floor.verts.Count - 1));
            floor.edges[2 + i].Add(new Tuple<int, int>(floor.verts.Count - 1, floor.verts.Count - 4));
        }
        //create faces from edges
        for(int i=0; i<floor.edges.Count; i++)
        {
            floor.faces.Add(new List<int>());
            for(int j = 0; j<floor.edges[i].Count; j++)
            {
                floor.faces[i].Add(floor.edges[i][j].Item1);
            }
        }

        //Generate the Bottom Mesh
        BrushUtils utility = new BrushUtils();
        utility.mat = floor_mat;
        GameObject floor_object = utility.GenerateBrush(floor);
        floor_object.transform.parent = sector_geo.transform;

        //3) Generate Ceiling Brush
        BrushUtils.Brush ceiling = new BrushUtils.Brush();
        ceiling.name = name + "_" + "ceiling";
        ceiling.verts = new List<Vector3>();
        ceiling.edges = new List<List<Tuple<int, int>>>();
        ceiling.faces = new List<List<int>>();
        for(int i=verts.Count - 1; i >= 0; i--)
        {
            ceiling.verts.Add(new Vector3(verts[i].x, ceiling_height, verts[i].y));
        }
        //Next generate the floor brush's top vertices
        for(int i=0; i < verts.Count; i++)
        {
            ceiling.verts.Add(new Vector3(verts[i].x, ceiling_height + 1f, verts[i].y));
        }
        //create edges for those verts
        ceiling.edges.Add(new List<Tuple<int, int>>());
        for(int i=1; i < floor.verts.Count / 2; i++)
        {
            if(i == (ceiling.verts.Count / 2) - 1)
            {
                ceiling.edges[0].Add(new Tuple<int, int>(i - 1, i));
                ceiling.edges[0].Add(new Tuple<int, int>(i, 0));
                break;
            }
            ceiling.edges[0].Add(new Tuple<int, int>(i - 1, i));
        }
        ceiling.edges.Add(new List<Tuple<int, int>>());
        for(int i=(ceiling.verts.Count / 2) + 1; i<ceiling.verts.Count; i++)
        {
            if (i == ceiling.verts.Count - 1)
            {
                ceiling.edges[1].Add(new Tuple<int, int>(i - 1, i));
                ceiling.edges[1].Add(new Tuple<int, int>(i, ceiling.verts.Count / 2));
                break;
            }
            ceiling.edges[1].Add(new Tuple<int, int>(i - 1, i));
        }
        //loop through the rest of those verts and create side face edges initialize some variables as a sanity check
        half = (ceiling.verts.Count / 2) - 1;
        max = ceiling.verts.Count - 1;
        for (int i = 0; i <= half; i++)
        {
            if (i == half)
            {
                //Create side verts first
                ceiling.verts.Add(new Vector3(ceiling.verts[i].x, ceiling.verts[i].y, ceiling.verts[i].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[max - i].x, ceiling.verts[max - i].y, ceiling.verts[max - i].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[max].x, ceiling.verts[max].y, ceiling.verts[max].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[0].x, ceiling.verts[0].y, ceiling.verts[0].z));
            }
            else
            {
                //Create side verts first
                ceiling.verts.Add(new Vector3(ceiling.verts[i].x, ceiling.verts[i].y, ceiling.verts[i].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[max - i].x, ceiling.verts[max - i].y, ceiling.verts[max - i].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[max - i - 1].x, ceiling.verts[max - i - 1].y, ceiling.verts[max - i - 1].z));
                ceiling.verts.Add(new Vector3(ceiling.verts[i + 1].x, ceiling.verts[i + 1].y, ceiling.verts[i + 1].z));
            }
            //add the edges
            ceiling.edges.Add(new List<Tuple<int, int>>());
            ceiling.edges[2 + i].Add(new Tuple<int, int>(ceiling.verts.Count - 4, ceiling.verts.Count - 3));
            ceiling.edges[2 + i].Add(new Tuple<int, int>(ceiling.verts.Count - 3, ceiling.verts.Count - 2));
            ceiling.edges[2 + i].Add(new Tuple<int, int>(ceiling.verts.Count - 2, ceiling.verts.Count - 1));
            ceiling.edges[2 + i].Add(new Tuple<int, int>(ceiling.verts.Count - 1, ceiling.verts.Count - 4));
        }
        //create faces from edges
        for (int i=0; i<floor.edges.Count; i++)
        {
            ceiling.faces.Add(new List<int>());
            for(int j = 0; j<floor.edges[i].Count; j++)
            {
                ceiling.faces[i].Add(floor.edges[i][j].Item1);
            }
        }

        //Generate the Bottom Mesh
        string output = "Verts: ";
        for (int i = 0; i < ceiling.verts.Count; i++)
        {
            output += "\n\t" + ceiling.verts[i];
        }
        Debug.Log(output);

        output = "Edges: ";
        for (int i = 0; i < ceiling.edges.Count; i++)
        {
            for (int j = 0; j < ceiling.edges[i].Count; j++)
            {
                output += "\n\t" + ceiling.edges[i][j];
            }
        }
        Debug.Log(output);

        output = "Faces: ";
        for (int i = 0; i < ceiling.faces.Count; i++)
        {
            for (int j = 0; j < ceiling.faces[i].Count; j++)
            {
                output += "\n\t" + ceiling.faces[i][j];
            }
        }
        Debug.Log(output);

        utility.mat = ceiling_mat;
        GameObject ceiling_object = utility.GenerateBrush(ceiling);
        ceiling_object.transform.parent = sector_geo.transform;
    }
}
