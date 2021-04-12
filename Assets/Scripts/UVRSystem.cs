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

public struct UdpState
{
    public UdpClient udp;
    public IPEndPoint rep, lep;
}
public class UdpEventArgs : EventArgs
{
    public byte[] bdata { get; set; }
}

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
        SystemManager.Instance.Start = false;
        bWaitThread = true;
        sw = new Stopwatch();
    }
    /////////////////////
    ///UDP 변경
    /////////////////////

    public List<UdpState> mListUDPs = new List<UdpState>();
    public UdpState UdpConnect(int port) {
        UdpState stat = new UdpState();
        IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        UdpClient udp = new UdpClient(localEP);
        stat.udp = udp;
        stat.lep = localEP;
        stat.rep = remoteEP;
        //mListUDPs.Add(stat);
        udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        return stat;
    }
    public UdpState UdpConnect(string rermoteip, int remoteport, int localport)
    {
        UdpState stat = new UdpState();
        IPEndPoint localEP = new IPEndPoint(IPAddress.Any, localport);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        UdpClient udp = new UdpClient(localEP);
        udp.Connect(IPAddress.Parse(rermoteip), remoteport);
        stat.udp = udp;
        stat.lep = localEP;
        stat.rep = remoteEP;
        //mListUDPs.Add(stat);
        udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        return stat;
    }
    public void SendData(UdpState stat,byte[] data)
    {
        stat.udp.Send(data, data.Length);
    }
    public void UdpDisconnect()
    {
        foreach(UdpState stat in mListUDPs)
        {
            stat.udp.Close();
        }
        mListUDPs.Clear();
    }
    public void ReceiveCallback(IAsyncResult ar)
    {
        UdpClient udp = ((UdpState)(ar.AsyncState)).udp;
        IPEndPoint remoteEP = ((UdpState)(ar.AsyncState)).rep;
        byte[] receiveBytes = udp.EndReceive(ar, ref remoteEP);
        if (receiveBytes.Length > 0)
        {
            UdpEventArgs args = new UdpEventArgs();
            args.bdata = receiveBytes;
            OnUdpDataReceived(args);
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), (UdpState)ar.AsyncState);
        }
    }
    public event EventHandler<UdpEventArgs> UdpDataReceived;
    public virtual void OnUdpDataReceived(UdpEventArgs e)
    {
        EventHandler<UdpEventArgs> handler = UdpDataReceived;
        if (handler != null)
        {
            handler(this, e);
        }
    }
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
    int nFirstKey = -1;

    void Start() {
        //while (true)
        //{
        //    if (!SystemManager.Instance.Connect)
        //        return;
        //    Debug.Log("Queue = " + MapManager.Instance.MessageQueue.Count);
        //}
    }

    void Update() {
        
    }

    void DeviceDataReceivedProcess(object sender, DeviceEventArgs e)
    {
        
        int size = e.bdata.Length;
        float[] fdata = new float[size / 4];
        Buffer.BlockCopy(e.bdata, 0, fdata, 0, size);
        cq.Enqueue(fdata);
        int id = (int)fdata[1];
        
    }


    public GameObject targetObj = null;
    public void Connect()
    {
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        string msg2 = "/Connect";
        if (SystemManager.Instance.Manager)
            msg2 += "?port=40000";

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + msg2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        ////thread start
        ThreadStart();

        //regist event handler
        UdpDataReceived += UdpDataReceivedProcess;

        //content server
        try
        {
            UdpState cstat = UdpConnect("143.248.6.143", 35001, 40001);
            mListUDPs.Add(cstat);
            //connect contetn server
            float[] fdata = new float[1];
            fdata[0] = 10000f;
            byte[] bdata2 = new byte[4];
            Buffer.BlockCopy(fdata, 0, bdata2, 0, bdata2.Length);
            cstat.udp.Send(bdata2, bdata2.Length);
        }
        catch(Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        

        //AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 

        //targetObj = Instantiate(Bullet);

        MapManager.Instance.DeviceDataReceived += DeviceDataReceivedProcess;
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
        ThreadStop();

        //disconnect contents echo server
        float[] fdata = new float[1];
        fdata[0] = 10001f;
        byte[] bdata = new byte[4];
        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
        mListUDPs[0].udp.Send(bdata, 4);
        UdpDisconnect();

        //remove event handler
        UdpDataReceived -= UdpDataReceivedProcess;
        MapManager.Instance.DeviceDataReceived -= DeviceDataReceivedProcess;

        ////모든 기기 오브젝트 삭제
        for(int i = 0, iend = Devices.transform.childCount; i < iend; i++)
        {
            DestroyImmediate(Devices.transform.GetChild(i).gameObject);
        }
        Devices.transform.DetachChildren();
        mConnectedDevices.Clear();

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
            tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
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
            if (bWaitThread)
                continue;
            
            if (nImgFrameIDX >= nMaxImageIndex)
            {
                Init();
                break;
            }
            EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
            //EditorCoroutineUtility.StartCoroutine(GetReferenceInfoCoroutine(), this);
        }

    }
    public GameObject Devices;
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

    IEnumerator DeviceControl()
    {
        float[] fdata;
        while(cq.TryDequeue(out fdata))
        {
            Debug.Log(fdata[0] + " " + fdata[1] + " " + fdata[2]);
            yield return new WaitForFixedUpdate();
            if (fdata[0] == 2f)
            {
                //Debug.Log(rdata[1] + "=" + rdata[2] + " " + rdata[3] + " " + rdata[4] + ":" + rdata[3] + " " + rdata[4] + " " + rdata[5]);
                int nContentID = (int)fdata[1];
                int nIDX = 2;
                Vector3 pos = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                Vector3 rot = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                EditorCoroutineUtility.StartCoroutine(TestCR(pos, rot, 100f), this);
            }else if(fdata[0] == 1f)
            {
                int id = (int)fdata[2];

                if (fdata[1] == 1f)
                {
                    if (mConnectedDevices.Count == 0)
                        nFirstKey = id;
                    mConnectedDevices[id] = Instantiate(Bullet);
                    mConnectedDevices[id].transform.SetParent(Devices.transform);
                }
                else if (fdata[1] == 2f)
                {
                    DestroyImmediate(mConnectedDevices[id]);
                    mConnectedDevices[id] = null;
                }
                else if (fdata[1] == 3f)
                {
                    GameObject obj = mConnectedDevices[id];
                    int nIDX = 3;
                    Matrix3x3 R = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    Vector3 t = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    Vector3 pos = -(R.Transpose() * t);

                    ////업데이트 카메라 포즈
                    Vector3 mAxis = R.LOG();
                    float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
                    mAxis = mAxis.normalized;
                    Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
                    obj.transform.SetPositionAndRotation(pos, rotation);
                }
            }
            
            //try
            //{
                
            //}
            //catch (Exception ex)
            //{
            //    Debug.Log(ex.ToString());
            //}
        }
        
    }
    ///
    public void ThreadStart()
    {
        if (SystemManager.Instance.Connect)
        {
            bWaitThread = true;
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
        if (SystemManager.Instance.Connect)
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
        socthread.Join();
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
