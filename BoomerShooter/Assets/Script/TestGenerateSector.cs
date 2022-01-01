using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGenerateSector : MonoBehaviour
{
    public Material floor_mat;
    public Material ceiling_mat;

    // Start is called before the first frame update
    void Start()
    {
        List<Vector2> vertices = new List<Vector2>()
        {
            new Vector2(-1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(1f, -1f)
        };
        List<Sector.LineDev> line_devs = new List<Sector.LineDev>();
        Sector.LineDev a = new Sector.LineDev();
        Sector.LineDev b = new Sector.LineDev();
        Sector.LineDev c = new Sector.LineDev();
        Sector.LineDev d = new Sector.LineDev();
        a.edge = new System.Tuple<int, int>(0, 1);
        b.edge = new System.Tuple<int, int>(1, 2);
        c.edge = new System.Tuple<int, int>(2, 3);
        d.edge = new System.Tuple<int, int>(3, 0);
        line_devs.Add(a);
        line_devs.Add(b);
        line_devs.Add(c);
        line_devs.Add(d);

        Sector s = new Sector("Test Sector", 0f, 5f, vertices, line_devs, floor_mat, ceiling_mat);
        s.Generate();
    }
}
