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
public class DeviceOrientationChangeEventArgs : EventArgs
{
    ////screen과 image scale
    //public float Scale;
    ////버튼 생성시 추가
    //public Rect rect1, rect2, rect3, rect4;
    ////백그라운드 image의 위치 조정
    //int additional;
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

    public event EventHandler<DeviceOrientationChangeEventArgs> OrientationChanged;
    public virtual void OnOrientationChanged(DeviceOrientationChangeEventArgs ev) {
        EventHandler<DeviceOrientationChangeEventArgs> handler = OrientationChanged;
        if (handler != null)
            handler(this, ev);
    }
    public void OrientationUI()
    {
        if (Screen.orientation == ScreenOrientation.Landscape)
        {
            Scale = Screen.height / SystemManager.Instance.ImageHeight;
            
            Scaler.matchWidthOrHeight = 1f;
            float Width = (SystemManager.Instance.ImageWidth * Scale);
            BackGroundRect = new Rect(0f, 0f, Width, Screen.height);
            background.rectTransform.anchoredPosition = new Vector2(BackGroundRect.width * 0.5f, BackGroundRect.height * 0.5f);
            rect1 = new Rect(Width, 60f, rectWidth, rectHeight);
            rect2 = new Rect(Width, 60f + (60f + rectHeight), rectWidth, rectHeight);
            rect3 = new Rect(Width, 60f + (60f + rectHeight) * 2, rectWidth, rectHeight);
            rect4 = new Rect(Width, 60f + (60f + rectHeight) * 3, rectWidth, rectHeight);
        }
        else if (Screen.orientation == ScreenOrientation.Portrait)
        {//
            Scale = Screen.width / SystemManager.Instance.ImageWidth;
            Scaler.matchWidthOrHeight = 0f;
            //Debug.Log("scale " + Scale+"::"+ SystemManager.Instance.ImageHeight+", "+Screen.width);
            float Height = (SystemManager.Instance.ImageWidth * Scale);
            BackGroundRect = new Rect(0f, Screen.height, Screen.width, Height);
            background.rectTransform.anchoredPosition = new Vector2(BackGroundRect.width * 0.5f, Screen.height - Height * 0.5f);

            rect1 = new Rect(10, Height + 60f, rectWidth, rectHeight);
            rect2 = new Rect(10, Height + 60f + (60f + rectHeight), rectWidth, rectHeight);
            rect3 = new Rect(10, Height + 60f + (60f + rectHeight) * 2, rectWidth, rectHeight);
            rect4 = new Rect(10, Height + 60f + (60f + rectHeight) * 3, rectWidth, rectHeight);
        }
    }
    public void OrientationChangeProcess(object sender, DeviceOrientationChangeEventArgs e)
    {
        Debug.Log("as;dlkajsdf");
        OrientationUI();
    }

    ///////////Click Event Handler
    public event EventHandler<UIClickEventArgs> UIClicked;
    public virtual void OnUIClicked(UIClickEventArgs e)
    {
        EventHandler<UIClickEventArgs> handler = UIClicked;
        if (handler != null)
            handler(this, e);
    }

    bool bClickUI = false;
    public void UIClickProcess(object sender, UIClickEventArgs e)
    {
        Debug.Log("ui click!!");
        bClickUI = true;

    }
    ///////////Click Event Handler

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

        //Debug.Log("Touch!!" + e.ray.origin.x + " " + e.ray.origin.y + " " + e.ray.origin.z);
        //if (!bClickUI && SystemManager.Instance.Connect)
        //{

        //} else
        //{
        //    bClickUI = false;
        //}

        if (Screen.safeArea.Contains(e.pos))
        {
            float[] fdata = new float[9];
            int nIDX = 0;
            fdata[nIDX++] = 2f; //method type = 1 : manager, 2 = content
            fdata[nIDX++] = 0f; //content id : 레이, 생성, 삭제 등
            fdata[nIDX++] = 2f; //model id
            fdata[nIDX++] = Center.x; //ray.origin.x;//Center.x;
            fdata[nIDX++] = Center.y; //ray.origin.y;//Center.y;
            fdata[nIDX++] = Center.z; //ray.origin.z;//Center.z;
            fdata[nIDX++] = e.ray.direction.x;
            fdata[nIDX++] = e.ray.direction.y;
            fdata[nIDX++] = e.ray.direction.z;
            byte[] bdata = new byte[fdata.Length * 4];
            Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
            UdpAsyncHandler.Instance.ConnectedUDPs[0].udp.Send(bdata, bdata.Length);
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
                //ui click event
                UIClickEventArgs args = new UIClickEventArgs();
                OnUIClicked(args);
                SystemManager.Instance.Connect = false;
                Disconnect();
                //////Disconnect Echo Server
                float[] fdata = new float[1];
                fdata[0] = 10001f;
                byte[] bdata = new byte[4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                UdpAsyncHandler.Instance.ConnectedUDPs[0].udp.Send(bdata, 4);
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
                //ui click event
                UIClickEventArgs args = new UIClickEventArgs();
                OnUIClicked(args);

                SystemManager.Instance.Connect = true;
                Connect();

                //background 위치를 canvas와 일치
                
                
                //ui button 설정
                rect1.x = 10;
                rect1.y = 60;
                rect2.x = 10;
                rect2.y = 120 + rectHeight;
                rect3.x = 10;
                rect3.y = 180 + rectHeight * 2;
                rect4.x = 10;
                rect4.y = 60 + (60 + rectHeight) * 3;
                
                //regist event handler
                UdpAsyncHandler.Instance.UdpDataReceived += UdpDataReceivedProcess;

                //connect to echo server
                UdpState cstat = UdpAsyncHandler.Instance.UdpConnect("143.248.6.143", 35001, 40002);
                UdpAsyncHandler.Instance.ConnectedUDPs.Add(cstat);
                float[] fdata = new float[1];
                fdata[0] = 10000f;
                byte[] bdata = new byte[4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                cstat.udp.Send(bdata, bdata.Length);
                //////Connect Echo Server
                                
            }
        }
        
        if (SystemManager.Instance.Start)
        {
            if (GUI.Button(rect2, "Stop"))
            {
                SystemManager.Instance.Start = false;
                //ui click event
                UIClickEventArgs args = new UIClickEventArgs();
                OnUIClicked(args);
            }
        }
        else
        {

            if (GUI.Button(rect2, "Start"))
            {
                SystemManager.Instance.Start = true;
                //ui click event
                UIClickEventArgs args = new UIClickEventArgs();
                OnUIClicked(args);
            }
        }

        if (GUI.Button(rect3, "Load Model"))
        {

            //ui click event
            UIClickEventArgs args = new UIClickEventArgs();
            OnUIClicked(args);
        }

        GUI.Label(rect4, Screen.width + " " + Screen.height+","+touchEvent.pos.x+" "+touchEvent.pos.y);

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

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
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
        Scaler.referenceResolution = new Vector2(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight);
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            if(Screen.width > Screen.height)
            {
                Debug.Log("be = " + Screen.orientation.ToString());
                Screen.orientation = ScreenOrientation.Landscape;
                Debug.Log("af = " + Screen.orientation.ToString()+" "+Input.deviceOrientation);
            }
            else
                Screen.orientation = ScreenOrientation.Portrait;
        }

        OrientationUI();
        Debug.Log("Start = "+Screen.orientation.ToString()+"::"+Screen.width+" "+Screen.height);
        StartCoroutine("CheckDeviceOrientation");
        
        UIClicked += UIClickProcess;
        Touched += TouchProcess;
    }

    // Update is called once per frame
    void Update()
    {
        if (SystemManager.Instance.Start)
        {
            StartCoroutine("GetReferenceInfoCoroutine");

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
                                
                //Debug.Log("Screen = " + Screen.width + ", " + Screen.height+"::"+tex.width+" "+tex.height+":"+ background.texture.dimension.ToString());
                //Debug.Log(background.rectTransform.anchoredPosition.x + " " + background.rectTransform.anchoredPosition.y +"::"+ background.transform.localPosition+" "+ background.transform.localPosition.y);

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
                    Debug.Log("mouse = " + Input.mousePosition.ToString());
                    touchPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
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
            touchEvent.ray = ray;
            touchEvent.pos = touchPos;
            OnTouched(touchEvent);
            StatusTxt.text = "Touch = " + touchPos.x + ", " + touchPos.y + "||Screen=" + Screen.width + " " + Screen.height;
        }
        
        if (bTouch && !bClickUI && SystemManager.Instance.Connect)
        {
            try
            {
                //Debug.Log("dir = "+touchDir.x+" "+touchDir.y+ " "+touchDir.z);
                

            }
            catch (Exception ex)
            {
                Debug.Log(ex.ToString());
            }
        }
        if (bClickUI)
        {
            bClickUI = false;
        }
        StartCoroutine("DeviceControl");
    }

    IEnumerator DeviceControl()
    {
        float[] fdata;
        while (cq.TryDequeue(out fdata))
        {
            yield return new WaitForFixedUpdate();
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
    IEnumerator CheckDeviceOrientation() {

        OrientationChanged += OrientationChangeProcess;
        DeviceOrientationChangeEventArgs ev = new DeviceOrientationChangeEventArgs();
        ScreenOrientation deviceOri = Screen.orientation;
        
        while (isAlive)
        {
            if (deviceOri != Screen.orientation)
            {
                OnOrientationChanged(ev);
                deviceOri = Screen.orientation;
            }
                
            yield return new WaitForSecondsRealtime(CheckOrientationDelay);
        }
    }
    void OnDestroy()
    {
        isAlive = false;
    }
}
