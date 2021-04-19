using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class UIClickEventArgs : EventArgs
{
}
public class TouchEventArgs : EventArgs
{
    public Vector2 pos;
    public Ray ray;
}

public class DeviceController : MonoBehaviour
{
    [DllImport("edgeslam")]
    private static extern void SetTargetFrame(IntPtr addr, char[] timestamp, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetTrackingFrame(IntPtr addr, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetParam(float fx, float fy, float cx, float cy);

    ////Orientation event
    float Scale;
    int additional;
    Rect BackGroundRect;
    GameObject Canvas;
    CanvasScaler Scaler;
    float CheckOrientationDelay = 0.5f;
    bool isAlive = true;
    private int rectWidth = 200;
    private int rectHeight = 80;
    Rect rect1, rect2, rect3, rect4;
        
    public void OrientationUI()
    {
        Scale = ((float)Screen.height) / SystemManager.Instance.ImageHeight;
        Scaler.matchWidthOrHeight = 1f;
        SystemManager.Instance.K = new Matrix3x3(
            SystemManager.Instance.FocalLengthX*Scale, 0f, SystemManager.Instance.PrincipalPointX * Scale, 
            0f, SystemManager.Instance.FocalLengthY * Scale, SystemManager.Instance.PrincipalPointY * Scale, 
            0f, 0f, 1f);

        Debug.Log(SystemManager.Instance.K.ToString());
        float Width = (SystemManager.Instance.ImageWidth * Scale);
        float Height = (SystemManager.Instance.ImageHeight * Scale);
        Debug.Log(Screen.width + " " + Screen.height+"::"+SystemManager.Instance.ImageWidth);
        Scaler.referenceResolution = new Vector2(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight); //(Width, Height);
        Debug.Log("width = " + Width+"::Scale"+Scale);
        BackGroundRect = new Rect(0f, 0f, Width, Height);
        
        rect1 = new Rect(Width, 60f, rectWidth, rectHeight);
        rect2 = new Rect(Width, 60f + (60f + rectHeight), rectWidth, rectHeight);
        rect3 = new Rect(Width, 60f + (60f + rectHeight) * 2, rectWidth, rectHeight);
        rect4 = new Rect(Width, 60f + (60f + rectHeight) * 3, rectWidth, rectHeight);
    }
    
    ///////////Touch Event Handler
    public Rect screenRect;
    public event EventHandler<TouchEventArgs> Touched;
    TouchEventArgs touchEvent = new TouchEventArgs();
    public virtual void OnTouched(TouchEventArgs e)
    {
        EventHandler<TouchEventArgs> handler = Touched;
        if (handler != null)
            handler(this, e);
    }

    //bool bClickUI = false;
    public void TouchProcess(object sender, TouchEventArgs e)
    {
        RaycastHit hit;
        if (Physics.Raycast(e.ray, out hit))
        {
            
            Debug.Log("Hit!!!");

            float[] fdata = new float[9];
            int nIDX = 0;
            float modelID = 2f;
            float contentID = 1f;
            fdata[nIDX++] = 2f; //method type = 1 : manager, 2 = content
            fdata[nIDX++] = contentID; //content id : 레이, 생성, 삭제 등
            fdata[nIDX++] = modelID; //model id
            
            if (contentID == 1f)
            {
                Debug.DrawRay(e.ray.origin, e.ray.direction * 100f, Color.blue, 1000f, false);
                Debug.DrawRay(e.ray.origin, e.ray.direction * hit.distance, Color.red, 1000f, false);
                Vector3 pos = hit.distance * e.ray.direction + e.ray.origin;//Camera.main.ViewportToWorldPoint(e.ray.origin + e.ray.direction * hit.distance);
                fdata[nIDX++] = pos.x;
                fdata[nIDX++] = pos.y;
                fdata[nIDX++] = pos.z;
                fdata[nIDX++] = 0f;
                fdata[nIDX++] = 0f;
                fdata[nIDX++] = 0f;
            }
            else
            {
                fdata[nIDX++] = Center.x;
                fdata[nIDX++] = Center.y; //ray.origin.y;//Center.y;
                fdata[nIDX++] = Center.z; //ray.origin.z;//Center.z;
                fdata[nIDX++] = e.ray.direction.x;
                fdata[nIDX++] = e.ray.direction.y;
                fdata[nIDX++] = e.ray.direction.z;
            }

            byte[] bdata = new byte[fdata.Length * 4];
            Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
            UdpState stat = UdpAsyncHandler.Instance.ConnectedUDPs[0];
            stat.udp.Send(bdata, bdata.Length, stat.hep);
        }
        else
        {
            Debug.Log("No Hit!!!");
        }
            
    }
    ///////////Touch Event Handler


    /// <summary>
    //코드 재정리
    //21.04.08
    //private ContentEchoServer mEchoServer; //이거 삭제 해도 될 듯

    
    void OnGUI()
    {
        if (SystemManager.Instance.Connect)
        {
            
            if (GUI.Button(rect1, "Disconnect"))
            {
                SystemManager.Instance.Connect = false;
                Disconnect();
                //////Disconnect Echo Server
                //float[] fdata = new float[1];
                //fdata[0] = 10001f;
                //byte[] bdata = new byte[4];
                //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                //UdpState stat = UdpAsyncHandler.Instance.ConnectedUDPs[0];
                //stat.udp.Send(bdata, 4, stat.hep);
                UdpAsyncHandler.Instance.UdpDisconnect();
                //////Disconnect Echo Server
                //remove event handler
                UdpAsyncHandler.Instance.UdpDataReceived -= UdpDataReceivedProcess;
            }
        }
        else
        {
            if (GUI.Button(rect1, "Connect"))
            {
                SystemManager.Instance.Connect = true;
                Connect();
                
                //regist event handler
                UdpAsyncHandler.Instance.UdpDataReceived += UdpDataReceivedProcess;

                //connect to echo server
                UdpState cstat = UdpAsyncHandler.Instance.UdpConnect("143.248.6.143", 35001, 40003);
                //UdpState mstat = UdpAsyncHandler.Instance.UdpConnect(40010);
                UdpAsyncHandler.Instance.ConnectedUDPs.Add(cstat);
                //UdpAsyncHandler.Instance.ConnectedUDPs.Add(mstat);
                //float[] fdata = new float[1];
                //fdata[0] = 10000f;
                //byte[] bdata = new byte[4];
                //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                //cstat.udp.Send(bdata, bdata.Length, cstat.hep);
                //////Connect Echo Server
                                
            }
        }
        
        if (SystemManager.Instance.Start)
        {
            if (GUI.Button(rect2, "Stop"))
            {
                SystemManager.Instance.Start = false;
            }
        }
        else
        {

            if (GUI.Button(rect2, "Start"))
            {
                SystemManager.Instance.Start = true;
            }
        }

        if (GUI.Button(rect3, "Load Model"))
        {
            GetModel();
        }

        //GUI.Label(rect4, Screen.width + " " + Screen.height+","+touchEvent.pos.x+" "+touchEvent.pos.y);

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

    int nImgFrameIDX, nMaxImageIndex;
    string[] imageData;
    string imagePath;
    private int nTargetID = -1;
    private int nRefID = -1;
    private int prevID = -1;

    public Vector3 Center = new Vector3(0f, 0f, 0f);
    public Vector3 DIR = new Vector3(0f, 0f, 0f);
    int nUserID = -1;
    void Connect()
    {
        
        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
        nImgFrameIDX = 3;

        bCam = SystemManager.Instance.Cam;
        //Debug.Log("cam = " + bCam);
        if (!bCam)
        {
            imageData = SystemManager.Instance.ImageData;
            //Debug.Log(imageData.ToString());
            imagePath = SystemManager.Instance.ImagePath;
            //nMaxImageIndex = mSystem.imageData.Length - 1;
        }
        else {

        }
        
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect?port=40003");
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
        Debug.Log("len = " + request.downloadHandler.data.Length);
        nUserID = BitConverter.ToInt32(request.downloadHandler.data, 0);
        Debug.Log("Connect = " + nUserID);
    }
    public void Disconnect()
    {
        string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User + "&mapName=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("Disconnect!!" + addr);
    }

    /// </summary>

    GCHandle webCamHandle;
    WebCamTexture webCamTexture;
    [HideInInspector]
    public Color[] webCamColorData;
    IntPtr webCamPtr;
    public RawImage background;
    Texture2D tex, datasetImg;
    public UnityEngine.UI.Text StatusTxt;
    [HideInInspector]
    public int mnFrameID = 0;
    IEnumerator runCoroutine;
    [HideInInspector]
    
    public AspectRatioFitter fit;

    DateTime baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();
    
    bool bCam = false;
    private bool mbSended = true;
    private Stopwatch sw;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    // Start is called before the first frame update
    void Start()
    {
        sw = new Stopwatch();
        string path = Application.persistentDataPath + "/param.txt";
        SystemManager.Instance.LoadParameter(path);

        //rect1 = new Rect(0, 60, rectWidth, rectHeight);
        //rect2 = new Rect(0, 120 + rectHeight, rectWidth, rectHeight);
        //rect3 = new Rect(0, 180 + rectHeight * 2, rectWidth, rectHeight);
        //rect4 = new Rect(0, 240 + rectHeight * 3, rectWidth, rectHeight);
        Canvas = GameObject.Find("Canvas");
        Scaler = Canvas.GetComponentInChildren<CanvasScaler>();
        //Scaler.referenceResolution = new Vector2(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight);
        
        OrientationUI();
        Touched += TouchProcess;
    }

    // Update is called once per frame
    void Update()
    {
        if (SystemManager.Instance.Start)
        {
            //StartCoroutine("GetReferenceInfoCoroutine");

            if (bCam)
            {

            }
            else
            {
                string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
                ++mnFrameID;
                byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                tex.LoadImage(byteTexture);
                background.texture = tex;
                background.SetNativeSize();
                background.rectTransform.anchoredPosition = new Vector2(SystemManager.Instance.ImageWidth*0.5f, SystemManager.Instance.ImageHeight*0.5f);//(Width * 0.5f, Height * 0.5f);
                if (mbSended && mnFrameID % 3 == 0)
                {
                    mbSended = false;
                    StartCoroutine("MappingCoroutine");
                }
            }
        }
        
        bool bTouch = false;
        Ray ray = new Ray();
        Vector2 touchPos = Vector2.zero;        
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.touchCount > 0)
            {
                Touch[] touches = Input.touches;
                Touch touch = touches[0];//Input.GetTouch(0);
                ray = Camera.main.ScreenPointToRay(touch.position);
                touchPos = touch.position;
                bTouch = true;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                try
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    touchPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    Debug.Log(ray.origin.ToString() + Center.ToString());
                    bTouch = true;
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.ToString());
                }
            }
        }
        
        if(bTouch && SystemManager.Instance.Connect)
        {
            if (BackGroundRect.Contains(touchPos)) { 
                touchEvent.ray = ray;
                touchEvent.pos = touchPos;
                OnTouched(touchEvent);
            }
            //StatusTxt.text = "Touch = " + touchPos.x + ", " + touchPos.y + "||Screen=" + Screen.width + " " + Screen.height+":"+Scale+"::"+BackGroundRect.ToString();
        }
       
        StartCoroutine("DeviceControl");
    }

    Matrix3x3 FloorRotationMat = new Matrix3x3();
    Vector4 FloorParam = Vector4.zero;
    GameObject FloorObject;
    bool bFloor = false;
    IEnumerator DeviceControl()
    {
        float[] fdata;
        while (cq.TryDequeue(out fdata))
        {
            yield return new WaitForFixedUpdate();
            try
            {
                if (fdata[0] == 2f)
                {
                    int nContentID = (int)fdata[1];
                    int nModelID = (int)fdata[2];
                    int nIDX = 3;
                    Vector3 pos = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    Vector3 rot = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                    ContentData c = new ContentData(ContentManager.Instance.ContentNames[nModelID], pos, rot);
                    Instantiate(c.obj, c.pos, c.q);
                }
                else if (fdata[0] == 1f && fdata[1] == 3f) {
               
                        ////center와 dir로 변경하기
                        int nIDX = 3;
                        Matrix3x3 R = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                        Vector3 t = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
                        Vector3 pos = -(R.Transpose() * t);
                        pos = FloorRotationMat * pos;
                        pos.y *= -1f;
                        Vector3 dir = R.row3 * FloorRotationMat.Transpose();
                        dir.y *= -1f;
                        Center = pos;

                        gameObject.transform.position = pos;
                        gameObject.transform.forward = dir;
                
                }
                else if (fdata[0] == 3f && fdata[1] == 1f)
                {
                    ////평면 모델 관련
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
                        }else
                        {
                            //생성
                            bFloor = true;
                            FloorObject = createPlane(points, "plane1", new Color(1.0f, 0.0f, 0.0f, 0.6f), 0, 1, 2, 3);
                        }

                    }

                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
    }

    IEnumerator GetReferenceInfoCoroutine()
    {
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
            Center = -(R.Transpose() * t);
            gameObject.transform.forward = R.row3;
            gameObject.transform.position = Center;
            ////업데이트 카메라 포즈
            prevID = nRefID;
        }
    }

    IEnumerator MappingCoroutine()
    {
        sw.Start();
        string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
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

        //nTargetID = -1;//BitConverter.ToInt32(request.downloadHandler.data, 0);//Convert.ToInt32(request.downloadHandler.data);
        sw.Stop();
        //Debug.Log("time = " + mnFrameID + "::" + sw.ElapsedMilliseconds.ToString() + "ms");
        sw.Reset();

        mbSended = true;
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


    void OnDestroy()
    {
        isAlive = false;
    }
}
