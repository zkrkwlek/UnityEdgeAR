using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthMeshGenerator : MonoBehaviour
{
    private const float k_TriangleConnectivityCutOff = 0.5f;
    private static readonly Vector3 k_DefaultMeshOffset = new Vector3(-100, -100, -100);
    private static readonly string k_VertexModelTransformPropertyName = "_VertexModelTransform";

    private Mesh m_Mesh;
    private bool m_FreezeMesh = false;
    private bool m_Initialized = false;
    private Texture2D m_StaticDepthTexture = null;

    float fx, fy, cx, cy;

    // Start is called before the first frame update
    void Start()
    {
        fx = CameraManager.FocalLengthX;
        fy = CameraManager.FocalLengthY;
        cx = CameraManager.PrincipalPointX;
        cy = CameraManager.PrincipalPointY;
    }

    private static int[] GenerateTriangles(int width, int height)
    {
        int[] indices = new int[(height - 1) * (width - 1) * 6];
        int idx = 0;
        for (int y = 0; y < (height - 1); y++)
        {
            for (int x = 0; x < (width - 1); x++)
            {
                //// Unity has a clockwise triangle winding order.
                //// Upper quad triangle
                //// Top left
                int idx0 = (y * width) + x;
                //// Top right
                int idx1 = idx0 + 1;
                //// Bottom left
                int idx2 = idx0 + width;

                //// Lower quad triangle
                //// Top right
                int idx3 = idx1;
                //// Bottom right
                int idx4 = idx2 + 1;
                //// Bottom left
                int idx5 = idx2;

                indices[idx++] = idx0;
                indices[idx++] = idx1;
                indices[idx++] = idx2;
                indices[idx++] = idx3;
                indices[idx++] = idx4;
                indices[idx++] = idx5;
            }
        }

        return indices;
    }

    private void InitializeMesh()
    {
        // Create template vertices.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        // Create template vertices for the mesh object.
        for (int y = 0; y < DepthSource.Height; y++)
        {
            for (int x = 0; x < DepthSource.Width; x++)
            {
                Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, 0);// + k_DefaultMeshOffset;
                vertices.Add(v);
                normals.Add(Vector3.back);
            }
        }
        Debug.Log("Initialize Meth = "+DepthSource.Width + " " + DepthSource.Height);
        // Create template triangle list.
        int[] triangles = GenerateTriangles(DepthSource.Width, DepthSource.Height);

        // Create the mesh object and set all template data.
        m_Mesh = new Mesh();
        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.SetVertices(vertices);
        m_Mesh.SetNormals(normals);
        m_Mesh.SetTriangles(triangles, 0);
        m_Mesh.bounds = new Bounds(Vector3.zero, new Vector3(50, 50, 50));
        //m_Mesh.UploadMeshData(true);
        //m_Mesh.RecalculateNormals();
        //m_Mesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = m_Mesh;

        //// Sets camera intrinsics for depth reprojection.
        Material material = GetComponent<MeshRenderer>().material;
        material.SetTexture("_CurrentDepthTexture", DepthSource.DepthTexture);
        material.SetFloat("_FocalLengthX", CameraManager.FocalLengthX);
        material.SetFloat("_FocalLengthY", CameraManager.FocalLengthY);
        material.SetFloat("_PrincipalPointX", CameraManager.PrincipalPointX);
        material.SetFloat("_PrincipalPointY", CameraManager.PrincipalPointY);
        material.SetInt("_ImageDimensionsX", CameraManager.ImageWidth);
        material.SetInt("_ImageDimensionsY", CameraManager.ImageWidth);
        material.SetFloat("_TriangleConnectivityCutOff", k_TriangleConnectivityCutOff);
        m_Initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_FreezeMesh)
        {
            //추후 카메라 자세로 변경하기
            Material material = GetComponent<Renderer>().material;
            //material.SetMatrix(k_VertexModelTransformPropertyName, DepthSource.LocalToWorldMatrix);
        }
        if (!m_Initialized && DepthSource.Initialized)
        {
            InitializeMesh();
        }

        Matrix3x3 R = TrckingManager.R;
        Vector3 T = TrckingManager.T;

        if (m_Initialized && DepthSource.Updated && !DepthSource.Updating)
        {
            //Material material = GetComponent<Renderer>().material;
            //material.SetTexture("_CurrentDepthTexture", DepthSource.DepthTexture);
            Vector3[] vertices = new Vector3[DepthSource.Height * DepthSource.Width];
            byte[] data = DepthSource.Data;
            float[] fdata = new float[data.Length / 4];
            Buffer.BlockCopy(data, 0, fdata, 0, data.Length);

            //m_Mesh.GetVertices(vertices);
            for (int y = 0; y < DepthSource.Height; y++)
            {
                for (int x = 0; x < DepthSource.Width; x++)
                {
                    int idx = DepthSource.Width * y + x;
                    float val = fdata[idx];

                    Vector3 v = new Vector3((x - cx) / fx, (y - cy) / fy, 1.0f) * val;
                    v = R.Transpose() * (v - T);

                    //Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, (float)val);// + k_DefaultMeshOffset;
                    vertices[idx] = v;
                    //Debug.Log(DepthSource.DepthTexture.GetPixel(x, y).r);
                    //Vector3 v = new Vector3(x * 0.01f, -y * 0.01f, x);// + k_DefaultMeshOffset;
                    //vertices.Add(v);
                    //normals.Add(Vector3.back);
                }
            }
            if (m_Mesh.isReadable)
            {
                m_Mesh.vertices = vertices;
                Debug.Log("true");
            }
            else
                Debug.Log("False");
            DepthSource.Updated = false;
        }

    }
}
