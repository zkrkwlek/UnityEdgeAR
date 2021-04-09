using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;

[ExecuteInEditMode]
public class UVRSystem : MonoBehaviour
{

    EditorCoroutine editorCorotine;
    static GCHandle webCamHandle;
    static WebCamTexture webCamTexture;
    [HideInInspector]
    static public Color[] webCamColorData;
    static IntPtr webCamPtr;

    public RawImage background;
    int mnFrameID = 0; //start 시에 초기화 시키기?
    
    public int nImgFrameIDX = 3;

    public int nTargetID = -1;
    public int nRefID = -1;

    DateTime baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();

    public void Init()
    {
        mnFrameID = 0;

        bProcessThread = false;
        SystemManager.Instance.Start = false;
        bWaitThread = true;

        sw = new Stopwatch();
    }
    public GameObject targetObj = null;
    public void Connect()
    {
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        targetObj = Instantiate(Bullet);
    }

    public void Disconnect()
    {
        ////reset or 따로
        //bMappingThread = false;
        //nImgFrameIDX = 3;
        //mnFrameID = 0;
        //mbSended = true;

        string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User + "&mapName=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("Disconnect!!" + addr);

        DestroyImmediate(targetObj);
        targetObj = null;
    }

    public void Reset()
    {
        Init();

        string addr = SystemManager.Instance.ServerAddr + "/reset?map=" + SystemManager.Instance.Map;
        Debug.Log("Reset:" + addr);
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }

    public void SaveMap()
    {
        string addr = SystemManager.Instance.ServerAddr + "/RequestSaveMap?map=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("save map " + addr);
    }

    public void LoadMap()
    {
        string addr = SystemManager.Instance.ServerAddr + "/LoadMap?map=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }

    //여기 아래를 이제 쓰레드에서 동작하도록 변경하기

    static bool bMappingThread = false;
    static bool bMappingThreadStart = false;


    public string[] imageData;
    public string imagePath;
    public int nMaxImageIndex;

    public void MapGeneration()
    {
        bMappingThread = !bMappingThread;
        imageData = SystemManager.Instance.ImageData;
        imagePath = SystemManager.Instance.ImagePath;
        Debug.Log(imagePath + " " + bMappingThread);
        if (!bMappingThreadStart)
        {
            tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
            bMappingThreadStart = true;
        }
    }
    
    public Texture2D tex;
    
    private Thread thread;
    public bool bProcessThread = false;
    //public bool bThreadStart = false;
    public bool bWaitThread = true;

    private void Run()
    {
        while (bProcessThread)
        {
            if (!SystemManager.Instance.Connect)
                continue;
            if (bWaitThread)
            {
                continue;
            }
            if (nImgFrameIDX >= nMaxImageIndex)
            {
                Init();
                break;
            }
            Thread.Sleep(33);
            EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
            EditorCoroutineUtility.StartCoroutine(GetReferenceInfoCoroutine(), this);
        }

    }
    public GameObject Bullet;
    
    public Vector3 Center = new Vector3(0f, 0f, 0f);
    public Vector3 DIR = new Vector3(0f, 0f, 0f);
    int prevID = -1;
    IEnumerator GetReferenceInfoCoroutine()
    {
        yield return null;
        string addr = SystemManager.Instance.ServerAddr + "/SendData?map=" + SystemManager.Instance.Map + "&attr=Users&id=" + SystemManager.Instance.User + "&key=refid";
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (!request.downloadHandler.isDone)
        {
            yield return new WaitForFixedUpdate();
        }
        nRefID = BitConverter.ToInt32(request.downloadHandler.data, 0);
        if (nRefID != -1 && nRefID != prevID)
        {
            string addr2 = SystemManager.Instance.ServerAddr + "/SendData?map=" + SystemManager.Instance.Map + "&id=" + nRefID + "&key=bpose";
            request = new UnityWebRequest(addr2);
            request.method = "POST";
            request.downloadHandler = new DownloadHandlerBuffer();
            res = request.SendWebRequest();
            while (!request.downloadHandler.isDone)
            {
                yield return new WaitForFixedUpdate();
            }

            byte[] bdata = request.downloadHandler.data;
            float[] framepose = new float[bdata.Length / 4];
            Buffer.BlockCopy(bdata, 0, framepose, 0, bdata.Length);
            //Debug.Log(framedata.Length+" "+framepose.Length);

            ////카메라 자세 획득 및 카메라 위치 추정
            Matrix3x3 R = new Matrix3x3(framepose[0], framepose[1], framepose[2], framepose[3], framepose[4], framepose[5], framepose[6], framepose[7], framepose[8]);

            Vector3 t = new Vector3(framepose[9], framepose[10], framepose[11]);
            Debug.Log(prevID + "::" + t.ToString());
            Center = -(R.Transpose() * t);

            ////업데이트 카메라 포즈
            Vector3 mAxis = R.LOG();
            DIR = mAxis;

            float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
            mAxis = mAxis.normalized;
            Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
            targetObj.transform.SetPositionAndRotation(Center, rotation);
            prevID = nRefID;
        }
    }
    Stopwatch sw;

    IEnumerator MappingCoroutine()
    {
        //yield return new WaitForSecondsRealtime(0.033f);
        yield return null;
        string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
        ++mnFrameID;
        string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        tex.LoadImage(byteTexture);
        if (mnFrameID % 3 == 0)
        {

            sw.Start();
            byte[] webCamByteData = tex.EncodeToJPG(90);
            string addr = SystemManager.Instance.ServerAddr + "/ReceiveAndDetect?user=" + SystemManager.Instance.User + "&map=" + SystemManager.Instance.Map + "&id=" + ts;
            UnityWebRequest request = new UnityWebRequest(addr);
            request.method = "POST";
            UploadHandlerRaw uH = new UploadHandlerRaw(webCamByteData);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
            request.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation res = request.SendWebRequest();
            while (request.uploadHandler.progress < 1f)
            {
                yield return new WaitForFixedUpdate();
                //progress = request.uploadHandler.progress;
            }
            //while(!request.downloadHandler.isDone)
            //{
            //    yield return new WaitForFixedUpdate();
            //}
            nTargetID = -1;//BitConverter.ToInt32(request.downloadHandler.data, 0);//Convert.ToInt32(request.downloadHandler.data);
            sw.Stop();
            Debug.Log("time = " + mnFrameID + "::" + sw.ElapsedMilliseconds.ToString() + "ms");
            sw.Reset();
            
        }
        
    }

    ///
    public void ThreadStart()
    {
        Debug.Log("thread start!!");
        if (!SystemManager.Instance.Start)
        {
            bProcessThread = true;
            bWaitThread = false;
            SystemManager.Instance.Start = true;
            thread = new Thread(Run);
            thread.Start();
            //EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
        }
    }
    public void ThreadPause()
    {
        bWaitThread = !bWaitThread;
    }
    public void ThreadStop()
    {
        Debug.Log("thread stop!!");
        Init();
        thread.Join();
    }

    public void GetModel()
    {
        string addr = SystemManager.Instance.ServerAddr + "/SendData?map=" + SystemManager.Instance.Map + "&attr=Models&id=1&key=bregion";
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (!request.downloadHandler.isDone)
        {
            continue;
        }

        float[] pdata = new float[12];
        Buffer.BlockCopy(request.downloadHandler.data, 0, pdata, 0, request.downloadHandler.data.Length);
        Debug.Log(pdata[0] + " " + pdata[1]);
        Vector3[] points = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 temp = new Vector3(pdata[3 * i], pdata[3 * i + 1], pdata[3 * i + 2]);
            points[i] = temp;
            Debug.Log(points[i]);
        }
        createPlane(points, "plane1", new Color(1.0f, 0.0f, 0.0f, 0.6f), 0, 1, 2, 3);
    }

    public GameObject createPlane(Vector3[] points, string oname, Color acolor, int idx1, int idx2, int idx3, int idx4)
    {
        GameObject go = new GameObject("Plane");
        go.name = oname;
        MeshFilter mf = go.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer mr = go.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        Mesh m = new Mesh();

        m.vertices = new Vector3[] {
			//입력의 3,4를 바꿈.
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
        /*
		m.colors = new Color[] {
			acolor, acolor, acolor, acolor
		};
		*/
        mf.mesh = m;
        m.RecalculateBounds();
        m.RecalculateNormals();
        mr.sharedMaterial = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        mr.sharedMaterial.color = acolor;
        return go;
    }
    

    private Thread socthread;
    public bool bSocThreadStart = false;
    public bool bSocDoThread = false;
    public void SocThreadStart()
    {
        Debug.Log("thread start!!");
        if (!bSocThreadStart)
        {
            bSocDoThread = true;
            bSocThreadStart = true;
            socthread = new Thread(SocRun);
            socthread.Start();
        }
    }
    public void SocThreadStop()
    {
        bSocDoThread = false;
        bSocThreadStart = false;
        socthread.Abort();
    }

    private void SocRun()
    {
        while (bSocDoThread)
        {
            try
            {
                EndPoint ep = AsyncSocketReceiver.Instance.RemoteEP;
                int bytes = AsyncSocketReceiver.Instance.SOCKET.ReceiveFrom(AsyncSocketReceiver.Instance.BUFFER, AsyncSocketReceiver.Instance.BUFSIZE, SocketFlags.None, ref ep);
                AsyncSocketReceiver.Instance.RemoteEP = ep;
                float[] rdata = new float[bytes / 4];
                Buffer.BlockCopy(AsyncSocketReceiver.Instance.BUFFER, 0, rdata, 0, bytes);
                Debug.Log(rdata[1] +"="+rdata[2] + " " + rdata[3] + " " + rdata[4] + ":" + rdata[3] + " " + rdata[4] + " " + rdata[5]);
                int nContentID = (int)rdata[1];

                int nIDX = 2;
                Vector3 pos = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
                Vector3 rot = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
                //ContentStart(pos, rot);
                //TestCoroutine(pos, rot);
                EditorCoroutineUtility.StartCoroutine(TestCR(pos, rot, 100f), this);
            }
            catch (Exception e)
            {
                e.ToString();
            }

            //ReceiveMessage();
            //receiveDone.WaitOne();
        }
    }

    IEnumerator TestCR(Vector3 pos, Vector3 rot, float dist)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        float fAngle = rot.magnitude * Mathf.Rad2Deg;
        Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);

        go.transform.SetPositionAndRotation(pos, q);

        Transform tr = go.transform;
        Vector3 spawnPoint = tr.position;

        bool bDist = (spawnPoint - tr.position).sqrMagnitude < dist;
        while (bDist)
        {
            yield return new WaitForSeconds(0.001f);
            tr.Translate(Vector3.forward * Time.deltaTime * 10f);
            bDist = (spawnPoint - tr.position).sqrMagnitude < dist;
            if (!bDist)
            {
                //Destroy(Bullet);
                try {
                    EditorCoroutineUtility.StartCoroutine(DestroyBullet(go), this);
                } catch(Exception e)
                {
                    Debug.Log(e.ToString());
                }
                
            }
        }
        yield return null;
    }
    IEnumerator DestroyBullet(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }
}
