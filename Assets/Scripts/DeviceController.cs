﻿using System;
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
    public int type; //begin, move, end
    public Vector2 pos;
    public Ray ray;
}

public class DeviceController : MonoBehaviour
{
    //https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=hope0510&logNo=220079329877
    Color[] resized;
#if(UNITY_EDITOR_WIN)
    [DllImport("UnityLibrary")]
    private static extern char[] SetInit(byte[] vocName, int len, int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);

    [DllImport("UnityLibrary")]
    private static extern int SetFrameByFile(byte[] name, int len, int id, double ts, ref float t1, ref float t2);
    [DllImport("UnityLibrary")]
    private static extern int SetFrameByImage(byte[] raw, int len, int id, double ts, ref float t1, ref float t2);
    [DllImport("UnityLibrary")]
    private static extern int SetFrame(IntPtr ptr, int id, double ts, ref float t1, ref float t2);
    [DllImport("UnityLibrary")]
    private static extern float SetReferenceFrame(int id, float[] data);

    [DllImport("UnityLibrary")]
    private static extern int Track(ref int nmatch);
#elif(UNITY_ANDROID)
    [DllImport("edgeslam2")]
    private static extern char[] SetInit(char[] vocName, int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);
    [DllImport("edgeslam2")]
    private static extern int SetFrameByFile(char[] name, int id, double ts, ref float t1, ref float t2);
    [DllImport("edgeslam2")]
    private static extern int SetFrameByImage(byte[] raw, int len, int id, double ts, ref float t1, ref float t2);
    [DllImport("edgeslam2")]
    private static extern int SetFrame(IntPtr ptr, int id, double ts, ref float t1, ref float t2);
    [DllImport("edgeslam2")]
    private static extern float SetReferenceFrame(int id, float[] data);
    [DllImport("edgeslam2")]
    private static extern int Track(ref int nmatch);
#endif
    //private static extern void ResizeImage(Color[] raw, ref Color[] resized, int w, int h, int rw, int rh);

    //[DllImport("HololensLibrary")]
    //private static extern void ResizeImage(Color[] raw, ref Color[] resized, int w, int h, int rw, int rh);

    //[DllImport("edgeslam")]
    //private static extern void SetTargetFrame(IntPtr addr, char[] timestamp, int w, int h);
    //[DllImport("edgeslam")]
    //private static extern void SetTrackingFrame(IntPtr addr, int w, int h);
    //[DllImport("edgeslam")]
    //private static extern void SetParam(float fx, float fy, float cx, float cy);

    //// Calculate time
    Dictionary<int, DateTime> mapImageTime = new Dictionary<int, DateTime>();
    float fSumImages = 0.0f;
    float fSumImages_2 = 0.0f;
    int   nTotalImages = 0;
    //// Calculate time

    ////Orientation event
    float Scale, Width, Height;
    int additional;
    Rect BackGroundRect;
    GameObject Canvas;
    CanvasScaler Scaler;
    float CheckOrientationDelay = 0.5f;
    bool isAlive = true;
    private int rectWidth = 160;
    private int rectHeight = 80;
    Rect rect1, rect2, rect3, rect4;

    Matrix3x3 InvK;
    Matrix3x3 ScaleMat, InvScaleMat;
    Matrix3x3 Mcfromd; // display에서 camera 좌표계로 변환
    Vector2 DiffScreen = Vector2.zero;

    public void SetScreen() {
        ////디스플레이와 카메라 스케일 계산
        Scale = ((float)Screen.height) / SystemManager.Instance.ImageHeight;
        Scaler.matchWidthOrHeight = 1f;
        ////디스플레이와 카메라 스케일 계산

        //이미지를 스크린에 정렬
        Width = (SystemManager.Instance.ImageWidth * Scale);
        Height = (SystemManager.Instance.ImageHeight * Scale);
        Scaler.referenceResolution = new Vector2(Screen.width, Screen.height);// (SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight); //(Width, Height);

        //카메라 이미지를 스크린의 가운데 정렬 & 클릭 이벤트 감지를 위한 사각형 설정
        float diff = (Screen.width - Width) * 0.5f;
        DiffScreen = new Vector2(diff, 0f);
        BackGroundRect = new Rect(diff, 0f, Width, Height);
        float margin = DiffScreen.x / Screen.width;
        Camera.main.rect = new Rect(margin, 0f, 1 - 2f * margin, 1f);
    }
    public void SetUI() {
        ////버튼 UI 설정
        rect1 = new Rect(DiffScreen.x + Width, 60f, rectWidth, rectHeight);
        rect2 = new Rect(DiffScreen.x + Width, 60f + (60f + rectHeight), rectWidth, rectHeight);
        rect3 = new Rect(DiffScreen.x + Width, 60f + (60f + rectHeight) * 2, rectWidth, rectHeight);
        rect4 = new Rect(DiffScreen.x + Width, 60f + (60f + rectHeight) * 3, rectWidth, rectHeight);
    }

    public void OrientationUI()
    {
        
        
        

        ////디스플레이 - 카메라 좌표계 변환용
        ////스케일 변환
        //SystemManager.Instance.K = new Matrix3x3(
        //    SystemManager.Instance.FocalLengthX * Scale, 0f, SystemManager.Instance.PrincipalPointX * Scale,
        //    0f, SystemManager.Instance.FocalLengthY * Scale, SystemManager.Instance.PrincipalPointY * Scale,
        //    0f, 0f, 1f);
        //ScaleMat = new Matrix3x3(Scale, 0f, 0f, 0f, Scale, 0f, 0f, 0f, 1f);
        //InvScaleMat = new Matrix3x3(1f/Scale, 0f, 0f, 0f, 1f/Scale, 0f, 0f, 0f, 1f);
        //Mcfromd = new Matrix3x3(
        //    1f, 0f, 0f,
        //    0f, -1f, Height,
        //    0f, 0f, 1f);
        ////SystemManager.Instance.K = new Matrix3x3(
        ////   565.8f, 0f, Width*0.5f,
        ////   0f, 424.35f, Height*0.5f,
        ////   0f, 0f, 1f);
        ////SystemManager.Instance.K = new Matrix3x3(
        ////    640f, 0f, 320f,
        ////    0f, 480f, 240f,
        ////    0f, 0f, 1f);
        //////스케일 변환
        //////역행렬
        //float a = -SystemManager.Instance.K.m02 / SystemManager.Instance.K.m00;
        //float b = -SystemManager.Instance.K.m12 / SystemManager.Instance.K.m11;
        //InvK = new Matrix3x3(
        //     1f / SystemManager.Instance.K.m00, 0f, a,
        //     0f, 1f / SystemManager.Instance.K.m11, b,
        //     0f, 0f, 1f
        //    );
        ////디스플레이 - 카메라 좌표계 변환용
        ////역행렬
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
        ////스크린 터치 -> 유니티 좌표계 변환
        ////이게 뷰포트 변환인지 잘 모르겠음. 진행중
        //Vector3 a = Mcfromd * new Vector3(e.pos.x - DiffScreen.x, e.pos.y, 1f);
        //Vector3 dir = NewRotationMat.Transpose() * InvK * a;
        //Ray ray = new Ray(Center, dir);
        ////스크린 터치 -> 유니티 좌표계 변환
        
        RaycastHit hit;
        if (Physics.Raycast(e.ray, out hit))
        {

            float[] fdata = new float[9];
            int nIDX = 0;
            float modelID = 2f;
            float contentID = mnContentID;
            fdata[nIDX++] = 2f; //method type = 1 : manager, 2 = content
            fdata[nIDX++] = contentID; //content id : 레이, 생성, 삭제 등
            fdata[nIDX++] = modelID; //model id

            //항상 start와 end로 전송
            Vector3 end = hit.distance * e.ray.direction + e.ray.origin;//Camera.main.ViewportToWorldPoint(e.ray.origin + e.ray.direction * hit.distance);
            Vector3 start = Center + DIR * 2f;
            fdata[nIDX++] = start.x;
            fdata[nIDX++] = start.y;
            fdata[nIDX++] = start.z;
            fdata[nIDX++] = end.x;
            fdata[nIDX++] = end.y;
            fdata[nIDX++] = end.z;

            byte[] bdata = new byte[fdata.Length * 4];
            Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
            UdpState stat = UdpAsyncHandler.Instance.ConnectedUDPs[0];
            stat.udp.Send(bdata, bdata.Length, stat.hep);
        }
            
    }
    ///////////Touch Event Handler

    /// <summary>
    //코드 재정리
    //21.04.08
    //private ContentEchoServer mEchoServer; //이거 삭제 해도 될 듯


    int tabIndex;
    string[] tabSubject = {"ray", "object"};

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
        
        tabIndex = GUI.Toolbar(rect3, tabIndex, tabSubject);
        switch (tabIndex)
        {
            case 0:
                mnContentID = 0;
                break;
            case 1:
                mnContentID = 1;
                break;
            case 2:
                break;
            default:
                break;
        }
        //GUI.Label(rect4, Screen.width + " " + Screen.height+","+touchEvent.pos.x+" "+touchEvent.pos.y);


        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(10, 40, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 40;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float avg = 0.0f;
        float stddev = 0.0f;
        int N = nTotalImages - 1;
        if (nTotalImages > 1)
        {
            avg = fSumImages / nTotalImages;
            stddev = Mathf.Sqrt(fSumImages_2 / N - avg * fSumImages / N);
        }
        string text = string.Format("avg : {0:0.0} ms, stddev : ({1:0.0} )", avg, stddev);
        GUI.Label(rect, text, style);

    }

    void UdpDataReceivedProcess(object sender, UdpEventArgs e)
    {
        int size = e.bdata.Length;
        string msg = System.Text.Encoding.Default.GetString(e.bdata);
        SystemManager.EchoData data = JsonUtility.FromJson<SystemManager.EchoData>(msg);
        if(data.keyword == "Pose")
        {
            try
            {
                DateTime end = DateTime.Now;
                TimeSpan time = end - mapImageTime[data.id];
                
                float temp = (float)time.Milliseconds;
                fSumImages += temp;
                fSumImages_2 += (temp * temp);
                nTotalImages++;
                cq.Enqueue(data);
            }
            catch (Exception ex)
            {
                Debug.Log("err = " + ex.ToString());
            }
            
        }
        
        ////DateTime end = DateTime.Now;

        //////Image

        //if(data.keyword == "Pose")
        //{
        //    Debug.Log("??????"+data.id);
        //    ////Load Pose
        //    string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=Pose&id="+data.id+"&src=" + SystemManager.Instance.User;
        //    //string msg2 = SystemManager.Instance.User + "," + SystemManager.Instance.Map;
        //    //byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);
        //    Debug.Log(addr2);
        //    try
        //    {
        //        UnityWebRequest request = new UnityWebRequest(addr2);
        //        Debug.Log("1111");
        //        request.method = "POST";

        //        //UploadHandlerRaw uH = new UploadHandlerRaw();
        //        //uH.contentType = "application/json";
        //        //request.uploadHandler = uH;
        //        request.downloadHandler = new DownloadHandlerBuffer();

        //        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        //        Debug.Log("11112222");
        //        while (!request.downloadHandler.isDone)
        //        {
        //            continue;
        //        }
        //        float[] fdata = new float[request.downloadHandler.data.Length / 4];
        //        Buffer.BlockCopy(request.downloadHandler.data, 0, fdata, 0, fdata.Length * 4);
        //    }
        //    catch(Exception ex)
        //    {
        //        Debug.Log(ex.ToString());
        //    }
           

        //    ////Load Pose

        //    //Debug.Log(fdata[0]);
        //    TimeSpan time = end - mapImageTime[data.id];
        //    Debug.Log("Ref Time = "+ mnLastFrameID+", "+data.id+"::" + String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));

        //    float temp = (float)time.Milliseconds;
        //    fSumImages += temp;
        //    fSumImages_2 += (temp * temp);
        //    nTotalImages++;
        //}

        //if (!mapImageTime.ContainsKey(data.id))
        //{
        //    mapImageTime.Add(data.id, end);
        //}
        
        ////float[] fdata = new float[size / 4];
        ////Buffer.BlockCopy(e.bdata, 0, fdata, 0, size);
        ////cq.Enqueue(fdata);
    }


    /// <summary>
    /// 접속 devices 관리 용
    /// </summary>
    ConcurrentQueue<SystemManager.EchoData> cq = new ConcurrentQueue<SystemManager.EchoData>();

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
        ////Reset
        fSumImages = 0.0f;
        fSumImages_2 = 0.0f;
        nTotalImages = 0;
        mapImageTime.Clear();
        mnLastFrameID = 0;
        ////Reset

        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGBA32, false);

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
        byte[] bdata2 = new byte[1 + fdataa.Length*4+bdatab.Length];
        bdata2[40] = SystemManager.Instance.Mapping ? (byte)1 : (byte)0;
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

        nDurationSendFrame = SystemManager.Instance.NumSkipFrame;

        ////Device & Map store
    }
    public void Disconnect()
    {
        ////Device & Map store
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

        //string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User + "&mapName=" + SystemManager.Instance.Map;
        //UnityWebRequest request = new UnityWebRequest(addr);
        //request.method = "POST";
        //request.downloadHandler = new DownloadHandlerBuffer();
        //UnityWebRequestAsyncOperation res = request.SendWebRequest();
        //Debug.Log("Disconnect!!" + addr);
    }

    /// </summary>

    GCHandle webCamHandle;
    WebCamTexture webCamTexture;
    [HideInInspector]
    public Color32[] webCamColorData;
    IntPtr webCamPtr;
    public RawImage background;
    Texture2D tex, datasetImg;
    public UnityEngine.UI.Text StatusTxt;
    [HideInInspector]
    public int mnFrameID = 0;
    public int mnLastFrameID = 0;
    IEnumerator runCoroutine;
    [HideInInspector]
    
    public AspectRatioFitter fit;

    DateTime baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();
    
    bool bCam = false;
    private bool mbSended = true;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }


    int nDurationSendFrame;
    // Start is called before the first frame update
    void Start()
    {
        string path = Application.persistentDataPath + "/param.txt";
        SystemManager.Instance.LoadParameter(path);
        
        Canvas = GameObject.Find("Canvas");
        Scaler = Canvas.GetComponentInChildren<CanvasScaler>();
        AspectRatioFitter fitter = GameObject.Find("background").GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = ((float)SystemManager.Instance.ImageWidth) / SystemManager.Instance.ImageHeight;

        SetScreen();
        SetUI();
        //OrientationUI();
        Touched += TouchProcess;

        webCamColorData = new Color32[SystemManager.Instance.ImageWidth * SystemManager.Instance.ImageHeight];
        webCamHandle = default(GCHandle);
        webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
        webCamPtr = webCamHandle.AddrOfPinnedObject();
        

        ////
        //dll or so library
        try
        {
            Debug.Log(SystemManager.Instance.strBytes[0]+" "+ SystemManager.Instance.strBytes[1]+" "+ SystemManager.Instance.strBytes[2]);
            SetInit(SystemManager.Instance.strBytes, SystemManager.Instance.strBytes.Length, SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, SystemManager.Instance.FocalLengthX, SystemManager.Instance.FocalLengthY, SystemManager.Instance.PrincipalPointX, SystemManager.Instance.PrincipalPointY,
                        SystemManager.Instance.IntrinsicData[6], SystemManager.Instance.IntrinsicData[7], SystemManager.Instance.IntrinsicData[8], SystemManager.Instance.IntrinsicData[9]);
            StatusTxt.text = "Init::Success";
        }
        catch (Exception ex)
        {
            StatusTxt.text = "err=" + ex.ToString();
        }
        StartCoroutine("DeviceControl");

        //resized = new Color[SystemManager.Instance.ImageWidth*SystemManager.Instance.ImageHeight/4];
        ////
    }

    // Update is called once per frame
    void Update()
    {
        
        if (SystemManager.Instance.Start)
        {

            if (bCam)
            {

            }
            else
            {
                string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
                ++mnFrameID;
                byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                tex.LoadImage(byteTexture);
                byte[] webCamByteData = tex.EncodeToJPG(100);
                
                background.texture = tex;
                                
                if (!SystemManager.Instance.Mapping) {
                    StartCoroutine("TrackingCoroutine", imgFile);
                }

                if (mbSended && mnFrameID % nDurationSendFrame == 0)
                {
                    mbSended = false;
                    StartCoroutine("MappingCoroutine", webCamByteData);
                }

                
                ////library test
                //Debug.Log("1");
                //ResizeImage(tex.GetPixels(), ref resized, SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, SystemManager.Instance.ImageWidth / 2, SystemManager.Instance.ImageHeight / 2);
                //Debug.Log("2");
                ////library test
            }
            mnLastFrameID++;
        }
        
        bool bTouch = false;
        Ray ray = new Ray();
        Vector2 touchPos = Vector2.zero;        
        if (Application.platform == RuntimePlatform.Android)
        {
            
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                //StatusTxt.text = "Began";
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                //StatusTxt.text = "Moved";
            }else if(touch.phase == TouchPhase.Ended)
            {
                //StatusTxt.text = "Ended";
            }
            //StatusTxt.text = Screen.width+" "+Screen.height+"=" + Application.persistentDataPath;
            ray = Camera.main.ScreenPointToRay(touch.position);
            touchPos = touch.position;
            bTouch = true;

            //if (Input.touchCount > 0)
            //{
            //    //Touch[] touches = Input.touches;
            //    //Touch touch = touches[0];//Input.GetTouch(0);
            //    //if(touch.phase == TouchPhase.Began)
            //    //{
            //    //    StatusTxt.text = "Began";
            //    //}else if(touch.phase)
            //    //ray = Camera.main.ScreenPointToRay(touch.position);
            //    //touchPos = touch.position;
            //    //bTouch = true;
            //}
        }
        else
        {
            
            if (Input.GetMouseButtonDown(0))
            {
                //Debug.Log("Button Down");
                try
                {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    touchPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    bTouch = true;
                    Vector3 view = Camera.main.ScreenToViewportPoint(Input.mousePosition);
                    //Debug.Log(Input.mousePosition.ToString() + ":" + view.ToString());
                }
                catch (Exception ex)
                {
                    Debug.Log(ex.ToString());
                }
            }else if (Input.GetMouseButtonUp(0))
            {
                //Debug.Log("Button Up");
            }else if (Input.GetMouseButton(0))
            {
                //Moved
                //Debug.Log("Button");
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
       
        
    }

    int mnContentID;
    Matrix3x3 NewRotationMat = new Matrix3x3();
    Matrix3x3 Runityfromslam = new Matrix3x3(1f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, 1f);
    Matrix3x3 FloorRotationMat = new Matrix3x3();
    Vector4 FloorParam = Vector4.zero;
    GameObject FloorObject;
    Vector3 vel = Vector3.zero;
    bool bFloor = false;
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

    IEnumerator DeviceControl()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();
            SystemManager.EchoData data;
            if (cq.TryDequeue(out data))
            {
                if (data.keyword == "Pose"&& !SystemManager.Instance.Mapping)
                {
                    string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=Pose&id=" + data.id + "&src=" + SystemManager.Instance.User;
                    UnityWebRequest request = new UnityWebRequest(addr2);
                    request.method = "POST";
                    //UploadHandlerRaw uH = new UploadHandlerRaw();
                    //uH.contentType = "application/json";
                    //request.uploadHandler = uH;
                    request.downloadHandler = new DownloadHandlerBuffer();

                    UnityWebRequestAsyncOperation res = request.SendWebRequest();
                    while (!request.downloadHandler.isDone)
                    {
                        continue;
                    }
                    float[] fdata = new float[request.downloadHandler.data.Length / 4];
                    Buffer.BlockCopy(request.downloadHandler.data, 0, fdata, 0, request.downloadHandler.data.Length);
                    try
                    {
                        float a = SetReferenceFrame(data.id, fdata);
                        //StatusTxt.text = "MP = " + a;
                    }
                    catch (Exception ex)
                    {
                        StatusTxt.text = ex.ToString();
                    }

                    //DateTime end = DateTime.Now;
                    //TimeSpan time = end - mapImageTime[data.id];
                    //Debug.Log("Ref Time = " + mnLastFrameID + ", " + data.id + "::" + String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
                }
            }
            
        }
        
        
        
        
            //    try
            //    {
            //float[] fdata;
            //while (cq.TryDequeue(out fdata))
            //{
            //    yield return new WaitForFixedUpdate();

            //    try
            //    {
            //        if (fdata[0] == 2f)
            //        {
            //            int nContentID = (int)fdata[1];
            //            int nModelID = (int)fdata[2];
            //            int nIDX = 3;
            //            Vector3 start = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //            Vector3 end = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //            Vector3 viewEnd = Camera.main.WorldToViewportPoint(end);
            //            //Debug.Log("line test = " + start.ToString() + "::" + end.ToString()+"::"+viewEnd);

            //            if(nContentID == 0f)
            //            {
            //                DrawLine(start, end, Color.cyan, 100f);
            //            }
            //            else
            //            {
            //                Vector3 dir = end - start;
            //                ContentData c = new ContentData(ContentManager.Instance.ContentNames[nModelID], end, Vector3.zero);
            //                Instantiate(c.obj, c.pos, c.q);
            //            }
            //        }
            //        else if (fdata[0] == 1f && fdata[1] == 3f) {

            //            ////center와 dir로 변경하기
            //            int nIDX = 3;
            //            Matrix3x3 R = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //            Vector3 t = new Vector3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //            Vector3 pos = -(R.Transpose() * t);
            //            pos = FloorRotationMat * pos;
            //            pos.y *= -1f;
            //            DIR = R.row3 * FloorRotationMat.Transpose();
            //            DIR.y *= -1f;
            //            Center = pos;

            //            gameObject.transform.position = Vector3.SmoothDamp(gameObject.transform.position, pos, ref vel, 0.1f);//pos;
            //            //Debug.Log(vel.ToString());
            //            gameObject.transform.forward = DIR;

            //            //Rotation Test
            //            NewRotationMat = R * FloorRotationMat.Transpose()* Runityfromslam;
            //            //Vector3 testVec = (-1f * NewRotationMat.Transpose() * t);
            //            //Debug.Log("TEST1 = " + testVec.x + " " + testVec.y + " " + testVec.z + "::" + Center.x + " " + Center.y + " " + Center.z);
            //            //Debug.Log("TEST2 = " + NewRotationMat.row3.x + " " + NewRotationMat.row3.y + " " + NewRotationMat.row3.z + "::" + dir.x + " " + dir.y + " " + dir.z);

            //        }
            //        else if (fdata[0] == 3f && fdata[1] == 1f)
            //        {
            //            ////평면 모델 관련
            //            if (fdata[2] == 1f)
            //            {
            //                int nIDX = 3;
            //                FloorRotationMat = new Matrix3x3(fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //            }
            //            else if (fdata[2] == 2f)
            //            {
            //                int nIDX = 3;
            //                FloorParam = new Vector4(fdata[nIDX++], -fdata[nIDX++], fdata[nIDX++], fdata[nIDX++]);
            //                float y = -FloorParam.w;

            //                Vector3[] points = new Vector3[4];
            //                float val = 100f;
            //                points[0] = new Vector3(val, y, val);
            //                points[1] = new Vector3(val, y, -val);
            //                points[2] = new Vector3(-val, y, -val);
            //                points[3] = new Vector3(-val, y, val);

            //                if (bFloor)
            //                {
            //                    //변경
            //                    FloorObject.GetComponent<MeshFilter>().mesh.vertices = points;
            //                }else
            //                {
            //                    //생성
            //                    bFloor = true;
            //                    FloorObject = createPlane(points, "plane1", new Color(1.0f, 0.0f, 0.0f, 0.6f), 0, 1, 2, 3);
            //                }

            //            }

            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Debug.Log(e.ToString());
            //    }
            //}
    }

    IEnumerable TrackingCoroutine(string file)//byte[] data)
    {
        
        try
        {
            

            int id = mnLastFrameID;
            DateTime t1 = DateTime.Now;
            float tt1 = 0.0f;
            float tt2 = 0.0f;

            //webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
            //webCamPtr = webCamHandle.AddrOfPinnedObject();

            //IntPtr unmanagedPointer = Marshal.AllocHGlobal(data.Length);
            //Marshal.Copy(data, 0, unmanagedPointer, data.Length);

            //Color32[] cdata = tex.GetPixels32();
            //webCamHandle = GCHandle.Alloc(cdata, GCHandleType.Pinned);
            //webCamPtr = webCamHandle.AddrOfPinnedObject();
            //IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            byte[] b = System.Text.Encoding.ASCII.GetBytes(file);
            int N = SetFrameByFile(b, b.Length, id, 0.0, ref tt1, ref tt2);//tex.GetPixels32()
            //int N = SetFrameByFile(file.ToCharArray(), id, 0.0, ref tt1, ref tt2);//tex.GetPixels32()

            //int N = SetFrameByImage(data, data.Length, id, 0.0, ref tt1,ref tt2);//tex.GetPixels32()
            //int N = SetFrame(webCamPtr, id, 0.0, ref tt1, ref tt2);//tex.GetPixels32()

            //StatusTxt.text = "SetFrame = " + N;
            DateTime t2 = DateTime.Now;
            int nMatch = -2;

            int nRes = 0;
            nRes = Track(ref nMatch);
            Debug.Log("track = " + nRes);
            DateTime t3 = DateTime.Now;
            TimeSpan time1 = t2 - t1;
            TimeSpan time2 = t3 - t2;
            StatusTxt.text = "Tracking = " + nMatch + ", " + nRes + "::" + tt1 + ", " + tt2 + "::::" + time1.Milliseconds.ToString().PadLeft(3, '0') + ", " + time2.Milliseconds.ToString().PadLeft(3, '0');
        }
        catch(Exception e)
        {
            StatusTxt.text = e.ToString();
        }
        //Marshal.FreeHGlobal(webCamPtr);
        //webCamHandle.Free();
        //pinnedArray.Free();
        return null;

        //try {
        //    //int id = mnLastFrameID;
        //    //DateTime t1 = DateTime.Now;
        //    //float tt1 = 0.0f;
        //    //float tt2 = 0.0f;

        //    ////IntPtr unmanagedPointer = Marshal.AllocHGlobal(data.Length);
        //    ////Marshal.Copy(data, 0, unmanagedPointer, data.Length);

        //    //yield return new WaitForFixedUpdate();
        //    //GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
        //    //IntPtr pointer = pinnedArray.AddrOfPinnedObject();
        //    ////int N = SetFrameByImage(data, data.Length, id, 0.0, ref tt1,ref tt2);//tex.GetPixels32()
        //    //int N = SetFrame(pointer, id, 0.0, ref tt1, ref tt2);//tex.GetPixels32()
        //    //pinnedArray.Free();
        //    ////Marshal.FreeHGlobal(unmanagedPointer);

        //    ////StatusTxt.text = "SetFrame = " + N;
        //    //DateTime t2 = DateTime.Now;
        //    //int nMatch = -2;
        //    ////int nRes = Track(ref nMatch);
        //    //int nRes = 0;
        //    //DateTime t3 = DateTime.Now;
        //    //TimeSpan time1 = t2 - t1;
        //    //TimeSpan time2 = t3 - t2;
        //    ////Debug.Log("Time = "+nID +" = " + String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        //    ////Debug.Log("id = " + id + "   feature = " + N + " Match = " + Nmatch + "::" + time.Milliseconds.ToString().PadLeft(3, '0'));
        //    //StatusTxt.text = "Tracking = " + nMatch+", "+ nRes +"::"+tt1+", "+tt2+ "::::" + time1.Milliseconds.ToString().PadLeft(3, '0')+", "+ time2.Milliseconds.ToString().PadLeft(3, '0');

        //}
        //catch(Exception e)
        //{
        //    StatusTxt.text = e.ToString();
        //}
        //return null;
    }
    IEnumerator MappingCoroutine(byte[] data)
    {
        int id = mnLastFrameID;
        DateTime start = DateTime.Now;

        //Color[] atemp = tex.GetPixels();
        //Color[] btemp = new Color[320 * 240];
        //ResizeImage(atemp, ref btemp, 640, 480, 320, 240);
        //Debug.Log("Resize = " + btemp[0] +" " + btemp[1]);

        
        //webCamColorData = tex.GetPixels();
        ////Buffer.BlockCopy(colors, 0, webCamColorData, 0, colors.Length);
        //webCamHandle = default(GCHandle);
        //webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
        //webCamPtr = webCamHandle.AddrOfPinnedObject();
        //byte[] bdata = new byte[webCamColorData.Length*3];
        //Marshal.Copy(webCamPtr, bdata, 0, bdata.Length);
        //byte[] webCamByteData = tex.EncodeToJPG(100); //tex.GetRawTextureData();//tex.EncodeToJPG(90);

        string addr = SystemManager.Instance.ServerAddr + "/Store?keyword=Image&id="+ id + "&src="+SystemManager.Instance.User;
        mapImageTime[id] = start;

        //SystemManager.EchoData jdata = new SystemManager.EchoData("Image", "notification", SystemManager.Instance.User);
        //jdata.data = webCamByteData;
        //string msg = JsonUtility.ToJson(jdata);
        //byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);


        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(data);//webCamByteData);
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

        //nTargetID = -1;//BitConverter.ToInt32(request.downloadHandler.data, 0);//Convert.ToInt32(request.downloadHandler.data);
        //sw.Stop();
        ////Debug.Log("time = " + mnFrameID + "::" + sw.ElapsedMilliseconds.ToString() + "ms");
        //sw.Reset();

        //int nID = Convert.ToInt32(System.Text.Encoding.Default.GetString(request.downloadHandler.data));
        //mnLastFrameID = nID;
        
        //if (mapImageTime.ContainsKey(nID))
        //{
        //    DateTime end = mapImageTime[nID];
        //    mapImageTime[nID] = start;

        //    TimeSpan time = end - start;
        //    //Debug.Log("Time = "+nID +" = " + String.Format("{0}.{1}", time.Seconds, time.Milliseconds.ToString().PadLeft(3, '0')));
        //}
        mbSended = true;
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
