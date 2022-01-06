using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushUtils {
    public Material mat;
    /*simple object that holds data for a brush
        * verts holds a list of 3D points for every vertex in the brush
        * faces holds a list of list of indices pointing to verts that belong in faces of a mesh
        * edges holds a list of list of ordered pairs containing edges between the verts of a face in a mesh
        * outward holds a list of booleans determining the direction of a face in a brush
        * 
        * faces and edges MUST be parallel lists
        */
    public struct Brush {
        public string name;
        public float uv_size;
        public List<Vector3> verts;
        public List<List<int>> faces;
        public List<List<Tuple<int, int>>> edges;
        public List<bool> outward;
    }

    class UVComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;
        public UVComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }
        public int Compare(T x, T y)
        {
            return comparison(x, y);
        }
    }

    //Calculate the area of a triangle given it's 3 points
    float CalculateAreaTri(Vector3 a, Vector3 b, Vector3 c)
    {
        //Debug.Log($"A: {a}, B: {b}, C: {c}");
        Vector3 d = ((Vector3.Dot(b - a, c - a)/Mathf.Pow((c - a).magnitude,2)) * (c - a)) + a;
        float based = Vector3.Distance(c, a);
        float height = Vector3.Distance(b, d);
        float area = .5f * based * height;
        //handle rounding errors
        if (area < .01f) area = 0f;

        area = (float)Math.Round(area * 100f) / 100f;
        //Debug.Log($"Base: {based}, Height: {height}, Area: {area}");

        return area;
    }

    //returns true if point p is within triangle abc, given they are all within the same plane
    bool WithinTri(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
    {
        //get orthogonal of triangle
        Vector3 l1 = (b - a).normalized;
        Vector3 l2 = (c - a).normalized;
        Vector3 ortho = Vector3.Cross(l1, l2);

        //pick a suitable cardinal plane to project on
        Vector3 a_p;
        Vector3 b_p;
        Vector3 c_p;
        Vector3 p_p;
        //XY
        if(ortho.z != 0)
        {
            //project onto the plane
            a_p = new Vector3(a.x, a.y, 0.0f);
            b_p = new Vector3(b.x, b.y, 0.0f);
            c_p = new Vector3(c.x, c.y, 0.0f);
            p_p = new Vector3(p.x, p.y, 0.0f);
        }
        //YZ
        else if(ortho.x != 0)
        {
            //project onto the plane
            a_p = new Vector3(0.0f, a.y, a.z);
            b_p = new Vector3(0.0f, b.y, b.z);
            c_p = new Vector3(0.0f, c.y, c.z);
            p_p = new Vector3(0.0f, p.y, p.z);

        }
        //XZ
        else
        {
            //project onto the plane
            a_p = new Vector3(a.x, 0.0f, a.z);
            b_p = new Vector3(b.x, 0.0f, b.z);
            c_p = new Vector3(c.x, 0.0f, c.z);
            p_p = new Vector3(p.x, 0.0f, p.z);
        }

        //USE THE AREA TEST TO NOW SEE IF THE POINT IS WITHIN THE TRIANGLE
        //1) find the area of triangle abc        
        float triangle_area = CalculateAreaTri(a_p, b_p, c_p);

        //failsafe to see if the triangle is even worth checking as a valid triangle
        if (triangle_area == 0) return true;

        //2) find the areas of the 3 possible triangles between points abp, acp, bcp
        float abp_area = CalculateAreaTri(a_p, p_p, b_p);
        float acp_area = CalculateAreaTri(a_p, p_p, c_p);
        float bcp_area = CalculateAreaTri(b_p, p_p, c_p);

        //3) check if the 3 resultant areas equal the full triangle's area, if they do then the point lies within the triangle or on the triangle so return true
        //Debug.Log($"Reassurance: TRI {triangle_area}, ABP {abp_area}, ACP {acp_area}, BCP {bcp_area}");
        //Debug.Log(triangle_area == abp_area + acp_area + bcp_area);
        return triangle_area == abp_area + acp_area + bcp_area;
    }

    //function for triangulating a brush
    int[] TriangulateBrush(Brush brush)
    {
        List<int> exported_tris = new List<int>();
        //BEGIN TRIANGULATION ALGORITHM
        //loop through all faces in a mesh
        for (int i = 0; i < brush.faces.Count; i++)
        {
            //store all vertices in the face along with it's index in the mesh for easy access
            //also store all edges in face for easy access
            List<Tuple<int, Vector3>> face_verts = new List<Tuple<int, Vector3>>();
            List<Tuple<int, int>> face_edges = new List<Tuple<int, int>>();
            for (int j = 0; j < brush.faces[i].Count; j++) {
                face_verts.Add(new Tuple<int, Vector3>(brush.faces[i][j], brush.verts[brush.faces[i][j]]));
            }
            for(int j=0; j<brush.edges[i].Count; j++)
            {
                face_edges.Add(brush.edges[i][j]);
            }

            //while the remaining face is not a triangle
            int edge_check = 0;
            while(face_verts.Count > 3)
            {
                //1) pick an edge
                Tuple<int, int> border_edge = face_edges[edge_check];

                //2) pick a vert on that edge
                int first = border_edge.Item1;
                int chosen = border_edge.Item2;

                //3) pick any other vertex than the first 2, this vertex may not share an edge with the chosen vertex in step 2
                // no vertices may also lie within the triangle these 3 vertices form
                int third = -1;
                for(int j=0; j<face_verts.Count; j++)
                {
                    //vertex equals the first or chosen vert
                    if (face_verts[j].Item1 == first || face_verts[j].Item1 == chosen) continue;
                    
                    //if skip flag is set to true then the vertex shares an edge with the chosen vert
                    bool skip = false;
                    for(int k=0; k<face_edges.Count; k++)
                    {
                        if((face_edges[k].Item1 == chosen && face_edges[k].Item2 == face_verts[j].Item1) || 
                            (face_edges[k].Item1 == face_verts[j].Item1 && face_edges[k].Item2 == chosen))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    for (int k = 0; k < face_verts.Count; k++)
                    {
                        //make sure we're checking verts that aren't the first, chosen, or vert we're checking
                        if (face_verts[k].Item1 == first || face_verts[k].Item1 == chosen || face_verts[k].Item1 == face_verts[j].Item1) continue;
                        //Debug.Log($"Checking {face_verts[k].Item2}");
                        if (WithinTri(brush.verts[first], brush.verts[chosen], face_verts[j].Item2, face_verts[k].Item2))
                        {
                            //Debug.Log("FAILED!");
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    third = face_verts[j].Item1;
                    break;
                }
                //if the end has been reached and third is still -1 then no valid vert has been found increment edge check and continue looping that face
                if(third == -1)
                {
                    edge_check++;
                    continue;
                }

                //4) store these 3 verts as valid points in a tri
                exported_tris.Add(first);
                exported_tris.Add(chosen);
                exported_tris.Add(third);

                //5) remove vertex not picked in step 2
                for (int j = 0; j < face_verts.Count; j++)
                {
                    if (face_verts[j].Item1 == first)
                    {
                        face_verts.RemoveAt(j);
                        break;
                    }
                }

                //6) remove all edges attached to that vertex
                bool valid = true;
                do
                {
                    valid = true;
                    for(int j=0; j<face_edges.Count; j++)
                    {
                        if(face_edges[j].Item1 == first || face_edges[j].Item2 == first)
                        {
                            face_edges.RemoveAt(j);
                            valid = false;
                            break;
                        }
                    }
                } while (!valid);

                //7) add new diagonal to edges
                face_edges.Add(new Tuple<int, int>(chosen, third));
            }
            //8) add the remain vertices as a tri
            for(int j=0; j<face_verts.Count; j++)
            {
                exported_tris.Add(face_verts[j].Item1);
            }
        }
        //convert exported tris to an array and return
        int[] e_tris = exported_tris.ToArray();
        return e_tris;
    }
    
    //function takes in a brush and calculates vert uvs based on planar projection of individual faces
    Vector2[] CalculateUVS(Brush brush, ref float uv_size)
    {
        //for every vert there is a uv
        Vector2[] exported_uvs = new Vector2[brush.verts.Count];

        //perform this operation for every face in the mesh
        for(int i=0; i<brush.faces.Count; i++)
        {
            //find the longest distance between verts in the face
            float longest = 0;
            Vector3 u_vector = Vector3.zero;
            for(int j=0; j<brush.faces[i].Count; j++)
            {
                for(int k=0; k<brush.faces[i].Count; k++)
                {
                    //don't waste time checking the same verts :)
                    if (i == k) continue;

                    //calculate distance between 2 verts being checked record it if it's longer than the previous recorded distance, along with the vector they make
                    float distance = Vector3.Distance(brush.verts[brush.faces[i][j]], brush.verts[brush.faces[i][k]]);
                    if (distance > longest)
                    {
                        longest = distance;
                        u_vector = (brush.verts[brush.faces[i][k]] - brush.verts[brush.faces[i][j]]);
                        uv_size = u_vector.magnitude;
                        u_vector.Normalize();
                    }
                }
            }

            //Attempt to find the orthogonal to the U vector by first finding another vector across the polygon
            Vector3 v_vector = Vector3.zero;
            for(int j=1; j<brush.faces[i].Count; j++)
            {
                //calculate a vector between vertices if it's the same as the u_vector try again
                Vector3 temp = (brush.verts[brush.faces[i][j]] - brush.verts[brush.faces[i][0]]).normalized;

                if (temp == u_vector) continue;

                //find the v_vector by calculating 2 orthogonals
                v_vector = Vector3.Cross(u_vector, Vector3.Cross(u_vector, temp));
            }

            //create 2D coordinates with respect to the newly calculated UV vector coords for all verts in the face
            List<Vector2> face_vert_uv_coords = new List<Vector2>();
            for(int j=0; j<brush.faces[i].Count; j++)
            {
                //project the vertex onto the U an v vectors
                float u_coord = Vector3.Dot(brush.verts[brush.faces[i][j]], u_vector)/Mathf.Pow(u_vector.magnitude, 2);
                float v_coord = Vector3.Dot(brush.verts[brush.faces[i][j]], v_vector)/Mathf.Pow(v_vector.magnitude, 2);

                //record these coords
                face_vert_uv_coords.Add(new Vector2(u_coord, v_coord));
            }

            //transform the uv coords to Quadrant I of the cartesian plane (the positive one) by finding the most negative x and y points with respect to u_vector an v_vector
            float lowest_u = 0;
            float lowest_v = 0;
            for(int j=0; j<face_vert_uv_coords.Count; j++)
            {
                if (face_vert_uv_coords[j].x < lowest_u) lowest_u = face_vert_uv_coords[j].x;
                if (face_vert_uv_coords[j].y < lowest_v) lowest_v = face_vert_uv_coords[j].y;
            }

            for(int j=0; j<face_vert_uv_coords.Count; j++)
            {
                face_vert_uv_coords[j] = new Vector2(face_vert_uv_coords[j].x - lowest_u, face_vert_uv_coords[j].y - lowest_v);
            }
            
            //scale back the uv coords with respect to the longest distance calculated earlier
            for(int j=0; j<face_vert_uv_coords.Count; j++)
            {
                face_vert_uv_coords[j] = new Vector2(face_vert_uv_coords[j].x / longest, face_vert_uv_coords[j].y / longest);
            }

            //write the new uv coords to the exported uvs array
            for(int j=0; j<brush.faces[i].Count; j++)
            {
                exported_uvs[brush.faces[i][j]] = face_vert_uv_coords[j];
            }
        }
        return exported_uvs;
    }

    //given a list of vertex positions this function will generate a brush at the specified points
    public GameObject GenerateBrush(Brush brush)
    {
        //declare members to generate in mesh
        Vector3[] verts = new Vector3[brush.verts.Count];
        Vector2[] uvs = new Vector2[brush.verts.Count];

        verts = brush.verts.ToArray();

        int[] tris;
        tris = TriangulateBrush(brush);
        foreach (int vert in tris)
        {
            Debug.Log(vert);
        }
        
        uvs = CalculateUVS(brush, ref brush.uv_size);


        //instance mesh and set it's members to declared members
        Mesh mesh = new Mesh();
        mesh.name = "Generated";
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        //create an object to use the mesh with
        GameObject w_mesh = new GameObject(brush.name, typeof(MeshFilter), typeof(MeshRenderer));
        w_mesh.transform.localScale = Vector3.one;

        //set the object's mesh to the generated mesh
        w_mesh.GetComponent<MeshFilter>().mesh = mesh;
        w_mesh.GetComponent<MeshRenderer>().material = mat;
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_BaseMap", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_BumpMap", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_EmissionMap", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_MetallicGlossMap", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_OcclusionMap", new Vector2(brush.uv_size, brush.uv_size));
        w_mesh.GetComponent<MeshRenderer>().material.SetTextureScale("_SpecGlossMap", new Vector2(brush.uv_size, brush.uv_size));

        return w_mesh;
    }
}
