using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject CreatePlane(Vector3[] points, string oname, Color acolor, int idx1, int idx2, int idx3, int idx4)
    {
        GameObject go = new GameObject("Plane");
        go.name = oname;
        MeshFilter mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer mr = go.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        Mesh m = new Mesh();

        m.vertices = new Vector3[] {
            points [idx1],
            points [idx2],
            points [idx3],
            points [idx4]
        };

        m.uv = new Vector2[] {
            new Vector2 (0, 0),
            new Vector2 (0, 1),
            new Vector2 (1, 1),
            new Vector2 (1, 0)
        };
        m.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        
        mf.mesh = m;
        m.RecalculateBounds();
        m.RecalculateNormals();
        mr.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        mr.sharedMaterial.color = acolor;
        return go;

    }
}
