using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
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
        float triangle_area = .5f * Vector3.Distance(c_p, a_p) * Vector3.Distance(b_p, a_p + Vector3.Dot(b_p - a_p, c_p - a_p) * (c_p - a_p));

        //2) find the areas of the 3 possible triangles between points abp, acp, bcp
        float abp_area = .5f * Vector3.Distance(a_p, b_p) * Vector3.Distance(p_p, b_p + Vector3.Dot(p_p - b_p, a_p - b_p) * (a_p - b_p));
        float acp_area = .5f * Vector3.Distance(b_p, c_p) * Vector3.Distance(p_p, c_p + Vector3.Dot(p_p - c_p, b_p - c_p) * (b_p - c_p));
        float bcp_area = .5f * Vector3.Distance(c_p, a_p) * Vector3.Distance(p_p, a_p + Vector3.Dot(p_p - a_p, c_p - a_p) * (c_p - a_p));

        //3) check if the 3 resultant areas equal the full triangle's area, if they do then the point lies within the triangle or on the triangle so return true
        return triangle_area == abp_area + acp_area + bcp_area;
    }

    //function for triangulating a brush
    public int[] TriangulateBrush(Brush brush)
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
            while(face_verts.Count > 3)
            {
                //1) pick an edge
                Tuple<int, int> border_edge = face_edges[0];

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
                        if (WithinTri(brush.verts[first], brush.verts[chosen], face_verts[j].Item2, face_verts[k].Item2))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;

                    third = face_verts[j].Item1;
                    break;
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
    /*
    //function takes in a brush and calculates vert uvs based on planar projection of individual faces
    public Vector2[] CalculateUVS(Brush brush)
    {
        //initialize all uvs at (0,0)
        List<Vector2> e_uvs = new List<Vector2>();

        //loop through all faces
        for(int i=0; i<brush.faces.Count; i++)
        {
            //first find the longest distance between vertices in the face
            SortedSet<Tuple<Tuple<int, int>, float>> point_distance = new SortedSet<Tuple<Tuple<int, int>, float>>(new UVComparer<Tuple<Tuple<int,int>,float>>((a, b) => a.Item2 > b.Item2 ? -1 : a.Item2 < b.Item2 ? 1 : 0));
            for(int j=0; j<brush.faces[i].Count; j++)
            {
                for(int k=0; k<brush.faces[i].Count; k++)
                {
                    if(brush.faces[i][j] != brush.faces[i][k])
                    {
                        point_distance.Add(new Tuple<Tuple<int, int>, float>(new Tuple<int, int>(brush.faces[i][j], brush.faces[i][k]), Vector3.Distance(brush.verts[brush.faces[i][j]], brush.verts[brush.faces[i][k]])));
                    }
                }
            }


        }

    }
    */

    //given a list of vertex positions this function will generate a brush at the specified points
    public void GenerateBrush(Brush brush)
    {
        //declare members to generate in mesh
        Vector3[] verts = new Vector3[brush.verts.Count];
        Vector2[] uvs = new Vector2[brush.verts.Count];

        verts = brush.verts.ToArray();

        int[] tris;
        tris = TriangulateBrush(brush);
        
        //temporary UV function
        for(int i=0; i<uvs.Length; i++) { uvs[i] = new Vector2(0, 0); }

        //instance mesh and set it's members to declared members
        Mesh mesh = new Mesh();
        mesh.name = "Generated";
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;

        //create an object to use the mesh with
        GameObject w_mesh = new GameObject("Name", typeof(MeshFilter), typeof(MeshRenderer));
        w_mesh.transform.localScale = Vector3.one;

        //set the object's mesh to the generated mesh
        w_mesh.GetComponent<MeshFilter>().mesh = mesh;
        w_mesh.GetComponent<MeshRenderer>().material = mat;
    }

    // Start is called before the first frame update
    void Start()
    {
        //generate a test brush to test generation function
        Brush test = new Brush();
        test.name = "test";
        test.verts = new List<Vector3>()
        {
            new Vector3(-1f, -1f, -1f), new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f),
            new Vector3(1f, -1f, -1f), new Vector3(-1f, 1f, -1f), new Vector3(-1f, 1f, 1f),
            new Vector3(1f, 1f, 1f), new Vector3(1f, 1f, -1f)
        };
        test.edges = new List<List<Tuple<int, int>>>()
        {
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(3, 2),
                new Tuple<int, int>(2, 1),
                new Tuple<int, int>(1, 0),
                new Tuple<int, int>(0, 3)
            },
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(0, 1),
                new Tuple<int, int>(1, 5),
                new Tuple<int, int>(5, 4),
                new Tuple<int, int>(4, 0)
            },
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(0, 4),
                new Tuple<int, int>(4, 7),
                new Tuple<int, int>(7, 3),
                new Tuple<int, int>(3, 0)
            },
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(3, 7),
                new Tuple<int, int>(7, 6),
                new Tuple<int, int>(6, 2),
                new Tuple<int, int>(2, 3)
            },
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(2, 6),
                new Tuple<int, int>(6, 5),
                new Tuple<int, int>(5, 1),
                new Tuple<int, int>(1, 2)
            },
            new List<Tuple<int, int>>()
            {
                new Tuple<int, int>(4, 5),
                new Tuple<int, int>(5, 6),
                new Tuple<int, int>(6, 7),
                new Tuple<int, int>(7, 4)
            }
        };
        test.faces = new List<List<int>>()
        {
            new List<int>(){3, 2, 1, 0},
            new List<int>(){0, 1, 5, 4},
            new List<int>(){0, 4, 7, 3},
            new List<int>(){3, 7, 6, 2},
            new List<int>(){2, 6, 5, 1},
            new List<int>(){4, 5, 6, 7}
        };

        GenerateBrush(test);
    }
}
