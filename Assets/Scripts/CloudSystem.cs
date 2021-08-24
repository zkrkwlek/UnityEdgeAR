#if(UNITY_EDITOR_WIN)
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
using System.Collections.Concurrent;

[ExecuteInEditMode]
public class CloudSystem : MonoBehaviour
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
        SystemManager.Instance.Start = false;
        bWaitThread = true;
        sw = new Stopwatch();
    }
    /////////////////////
    ///UDP 변경
    /////////////////////

    void UdpDataReceivedProcess(object sender, UdpEventArgs e)
    {
        int size = e.bdata.Length;
        float[] fdata = new float[size / 4];
        Buffer.BlockCopy(e.bdata, 0, fdata, 0, size);
        cq.Enqueue(fdata);
    }


    /// <summary>
    /// 접속 devices 관리 용
    /// </summary>
    ConcurrentQueue<float[]> cq = new ConcurrentQueue<float[]>();
    public Dictionary<int, GameObject> mConnectedDevices = new Dictionary<int, GameObject>();

    void Start()
    {

    }


    void Update()
    {

    }
    bool bCam = false;
    int mnLastFrameID;
    int nDurationSendFrame;
    public void Connect()
    {
        SystemManager.Instance.Connect = true;
        Contents = GameObject.Find("Contents");
        Devices = GameObject.Find("Devices");
        bFloor = false;

        mnLastFrameID = 0;
        nImgFrameIDX = 3;
        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGBA32, false);

        bCam = SystemManager.Instance.Cam;
        //Debug.Log("cam = " + bCam);
        if (!bCam)
        {
            imageData = SystemManager.Instance.ImageData;
            //Debug.Log(imageData.ToString());
            imagePath = SystemManager.Instance.ImagePath;
            //nMaxImageIndex = mSystem.imageData.Length - 1;
        }
        else
        {

        }

        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect?port=40001");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        while (!request.downloadHandler.isDone)
        {
            continue;
        }

        //Debug.Log("len = " + request.downloadHandler.data.Length);
        //nUserID = BitConverter.ToInt32(request.downloadHandler.data, 0);
        //Debug.Log("Connect = " + nUserID);

        //SystemManager.EchoData jdata = new SystemManager.EchoData("Image", "notification", SystemManager.Instance.User);
        //jdata.data = webCamByteData;
        //string msg = JsonUtility.ToJson(jdata);
        //byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        ////Device & Map store
        string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=DeviceConnect&id=0&src=" + SystemManager.Instance.User;
        string msg2 = SystemManager.Instance.User + "," + SystemManager.Instance.Map;
        byte[] bdatab = System.Text.Encoding.UTF8.GetBytes(msg2);
        float[] fdataa = SystemManager.Instance.IntrinsicData;
        byte[] bdata2 = new byte[1 + fdataa.Length * 4 + bdatab.Length];
        bdata2[40] = SystemManager.Instance.IsServerMapping ? (byte)1 : (byte)0;
        Buffer.BlockCopy(fdataa, 0, bdata2, 0, 40);
        Buffer.BlockCopy(bdatab, 0, bdata2, 41, bdatab.Length);

        //Debug.Log(msg2+" "+ bdatab.Length);

        request = new UnityWebRequest(addr2);
        request.method = "POST";
        uH = new UploadHandlerRaw(bdata2);//webCamByteData);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        res = request.SendWebRequest();


        UdpAsyncHandler.Instance.UdpDataReceived += UdpDataReceivedProcess;

        //connect to echo server
        UdpState cstat = UdpAsyncHandler.Instance.UdpConnect("143.248.6.143", 35001, 40001);
        UdpAsyncHandler.Instance.ConnectedUDPs.Add(cstat);

        nDurationSendFrame = SystemManager.Instance.NumSkipFrame;
        ThreadStart();

        //SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        //string msg = JsonUtility.ToJson(data);
        //byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        //string msg2 = "/Connect?port=40001";

        //UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + msg2);
        //request.method = "POST";
        //UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        //uH.contentType = "application/json";
        //request.uploadHandler = uH;
        //request.downloadHandler = new DownloadHandlerBuffer();
        //UnityWebRequestAsyncOperation res = request.SendWebRequest();

        //////thread start
        //

        ////regist event handler
        //UdpAsyncHandler.Instance.UdpDataReceived += UdpDataReceivedProcess;

        ////content server
        //try
        //{
        //    UdpState cstat = UdpAsyncHandler.Instance.UdpConnect("143.248.6.143", 35001, 40001);
        //    //UdpState mstat = UdpAsyncHandler.Instance.UdpConnect(40001);
        //    UdpAsyncHandler.Instance.ConnectedUDPs.Add(cstat);
        //    //UdpAsyncHandler.Instance.ConnectedUDPs.Add(mstat);
        //    //connect contetn server
        //    float[] fdata = new float[1];
        //    fdata[0] = 10000f;
        //    byte[] bdata2 = new byte[4];
        //    Buffer.BlockCopy(fdata, 0, bdata2, 0, bdata2.Length);
        //    cstat.udp.Send(bdata2, bdata2.Length);
        //}
        //catch(Exception ex)
        //{
        //    Debug.Log(ex.ToString());
        //}
    }

    public void Disconnect()
    {
        SystemManager.Instance.Connect = false;
        string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=DeviceDisconnect&id=0&src=" + SystemManager.Instance.User;
        string msg2 = SystemManager.Instance.User + "," + SystemManager.Instance.Map;
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);

        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        while (!request.downloadHandler.isDone)
        {
            continue;
        }

        UdpAsyncHandler.Instance.UdpDisconnect();
        UdpAsyncHandler.Instance.UdpDataReceived -= UdpDataReceivedProcess;

        ThreadStop();

        //////reset or 따로
        ////bMappingThread = false;
        ////nImgFrameIDX = 3;
        ////mnFrameID = 0;
        ////mbSended = true;

        //string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User + "&mapName=" + SystemManager.Instance.Map;
        //UnityWebRequest request = new UnityWebRequest(addr);
        //request.method = "POST";
        //request.downloadHandler = new DownloadHandlerBuffer();
        //UnityWebRequestAsyncOperation res = request.SendWebRequest();
        //Debug.Log("Disconnect!!" + addr);
        //ThreadStop();

        ////disconnect contents echo server
        ////float[] fdata = new float[1];
        ////fdata[0] = 10001f;
        ////byte[] bdata = new byte[4];
        ////Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
        ////UdpAsyncHandler.Instance.ConnectedUDPs[0].udp.Send(bdata, 4);
        //UdpAsyncHandler.Instance.UdpDisconnect();

        ////remove event handler
        //UdpAsyncHandler.Instance.UdpDataReceived -= UdpDataReceivedProcess;

        ////모든 기기 오브젝트 삭제
        try
        {

            Dictionary<int, GameObject>.KeyCollection keys = mConnectedDevices.Keys;
            Dictionary<int, GameObject>.ValueCollection values = mConnectedDevices.Values;

            foreach (int key in keys)
            {
                GameObject go = mConnectedDevices[key];
                mConnectedDevices.Remove(key);
                DestroyImmediate(go);
            }

            ////foreach (GameObject go in mConnectedDevices.Values)
            ////{
            ////    if (go != null)
            ////    {
            ////        DestroyImmediate(go);
            ////    }
            ////}

            ////for (int i = 0, iend = Devices.transform.childCount; i < iend; i++)
            ////{
            ////    GameObject go = Devices.transform.GetChild(i).gameObject;
            ////    if (go != null)
            ////        DestroyImmediate(Devices.transform.GetChild(i).gameObject);
            ////}

            ////Devices.transform.DetachChildren();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        mConnectedDevices.Clear();

        ////현재 남아있는 모든 콘텐츠 삭제
        for (int i = 0, iend = Contents.transform.childCount; i < iend; i++)
        {
            GameObject go = Contents.transform.GetChild(i).gameObject;
            if (go != null)
                DestroyImmediate(go);
        }
        Contents.transform.DetachChildren();
        //DestroyImmediate(targetObj);
        //targetObj = null;
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
            tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGBA32, false);
            bMappingThreadStart = true;
        }
    }

    public Texture2D tex;
    private Thread thread;
    public bool bWaitThread = true;

    private void Run()
    {
        while (SystemManager.Instance.Connect)
        {

            Thread.Sleep(33);
            EditorCoroutineUtility.StartCoroutine(DeviceControl(), this);
            if (!SystemManager.Instance.Start)
                continue;

            if (nImgFrameIDX >= nMaxImageIndex)
            {
                Init();
                break;
            }
            EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
        }

    }
    public GameObject Devices, Contents;

    public Vector3 Center = new Vector3(0f, 0f, 0f);
    public Vector3 DIR = new Vector3(0f, 0f, 0f);
    int prevID = -1;

    Stopwatch sw;

    IEnumerator MappingCoroutine()
    {
        //yield return new WaitForSecondsRealtime(0.033f);
        //yield return null;
        string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
        ++mnFrameID;

        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        tex.LoadImage(byteTexture);

        if (mnFrameID % nDurationSendFrame == 0)
        {

            int id = mnLastFrameID;
            byte[] webCamByteData = tex.EncodeToJPG(100);
            string addr = SystemManager.Instance.ServerAddr + "/Store?keyword=Image&id=" + id + "&src=" + SystemManager.Instance.User;
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
            while (!request.downloadHandler.isDone)
            {
                continue;
                //yield return new WaitForFixedUpdate();
            }
            //while(!request.downloadHandler.isDone)
            //{
            //    yield return new WaitForFixedUpdate();
            //}

        }
        mnLastFrameID++;
    }
    /// <summary>
    /// 인덱스 0 = 전체 메소드= 1:접속 기기 관리, 2: 컨텐츠
    /// ##접속기기 관리
    /// 1 = 메소드 = 1:등록, 2:삭제, 3:갱신
    /// 이후는 필요한 추가 데이터
    /// ##콘텐츠 관리
    /// 1 = 메소드 = 정리 필요
    /// 2 = 모델ID = 0:기기, 1:불렛, 2:블락, 이후 추가
    /// 이후는 필요한 추가 데이터
    /// </summary>
    /// <returns></returns>
    bool bFloor = false;
    GameObject FloorObject;
    Matrix3x3 FloorRotationMat = new Matrix3x3();
    Vector4 FloorParam = Vector4.zero;
    IEnumerator DeviceControl()
    {
        float[] fdata;
        while (cq.TryDequeue(out fdata))
        {
            //Debug.Log(fdata[0] + " " + fdata[1] + " " + fdata[2]);
            yield return new WaitForFixedUpdate();
            if (fdata[0] == 2f)
            {
                //Debug.Log(rdata[1] + "=" + rdata[2] + " " + rdata[3] + " " + rdata[4] + ":" + rdata[3] + " " + rdata[4] + " " + rdata[5]);
                int nContentID = (int)fdata[1];
                int nModelID = (int)fdata[2];
                int nIDX = 3;
                Vector3 start = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                Vector3 end = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                if (nContentID == 0f)
                {
                    DrawLine(start, end, Color.cyan, 100f);
                }
                else
                {
                    Vector3 dir = end - start;
                    ContentData c = new ContentData(ContentManager.Instance.ContentNames[nModelID], end, Vector3.zero);
                    GameObject go = Instantiate(c.obj, c.pos, c.q);
                    go.transform.SetParent(Contents.transform);
                }
                //Vector3 pos = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                //Vector3 rot = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                //ContentData c = new ContentData(ContentManager.Instance.ContentNames[nModelID],pos, rot);


            }
            else if (fdata[0] == 1f)
            {
                int id = (int)fdata[2];

                if (fdata[1] == 1f)
                {
                    ContentData c = new ContentData(ContentManager.Instance.ContentNames[0], Vector3.zero, Vector3.zero);
                    mConnectedDevices[id] = Instantiate(c.obj);
                    mConnectedDevices[id].transform.SetParent(Devices.transform);
                }
                else if (fdata[1] == 2f)
                {
                    DestroyImmediate(mConnectedDevices[id]);
                    mConnectedDevices.Remove(id);
                    //mConnectedDevices[id] = null;
                }
                else if (fdata[1] == 3f)
                {
                    GameObject obj = mConnectedDevices[id];
                    int nIDX = 3;
                    Matrix3x3 R = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    Vector3 t = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    Vector3 pos = -(R.Transpose() * t);
                    pos = FloorRotationMat * pos;
                    pos.y *= -1f;
                    Vector3 dir = R.row3 * FloorRotationMat.Transpose();
                    dir.y *= -1f;
                    Vector3 vel = Vector3.zero;
                    obj.transform.position = Vector3.Lerp(obj.transform.position, pos, 0.1f);//pos;
                    obj.transform.forward = dir;
                }
            }
            else if (fdata[0] == 3f && fdata[1] == 1f)
            {
                if (fdata[2] == 1f)
                {
                    int nIDX = 3;
                    FloorRotationMat = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                }
                else if (fdata[2] == 2f)
                {
                    int nIDX = 3;
                    FloorParam = new Vector4(fdata[nIDX++], -fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    float y = -FloorParam.w;

                    Vector3[] points = new Vector3[4];
                    float val = 100f;
                    points[0] = new Vector3(val, y, val);
                    points[1] = new Vector3(val, y, -val);
                    points[2] = new Vector3(-val, y, -val);
                    points[3] = new Vector3(-val, y, val);

                    if (bFloor)
                    {
                        //변경
                        FloorObject.GetComponent<MeshFilter>().mesh.vertices = points;
                    }
                    else
                    {
                        //생성
                        bFloor = true;
                        FloorObject = createPlane(points, "Floor", new Color(1.0f, 0.0f, 0.0f, 0.6f), 0, 1, 2, 3);
                    }
                }

            }

        }

    }
    ///
    public void ThreadStart()
    {
        if (SystemManager.Instance.Connect)
        {
            bWaitThread = true;
            //SystemManager.Instance.Start = true;
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
        if (SystemManager.Instance.Connect && SystemManager.Instance.Start)
        {
            Init();
            thread.Join();
        }
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

        MeshCollider mc = go.AddComponent(typeof(MeshCollider)) as MeshCollider;
        mc.sharedMesh = m;
        return go;
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
                try
                {
                    EditorCoroutineUtility.StartCoroutine(DestroyBullet(go), this);
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }

            }
        }
        yield return null;
    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }

    IEnumerator DestroyBullet(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }
}
#endif