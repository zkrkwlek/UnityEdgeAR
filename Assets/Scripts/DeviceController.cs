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

//public class InitConnectData{
//    public InitConnectData() { }
//    public InitConnectData(string _userID, string _mapName , bool _bMapping, float _fx, float _fy, float _cx, float _cy, int _w, int _h)
//    {
//        userID = _userID;
//        mapName = _mapName;
//        bMapping = _bMapping;
//        fx = _fx;
//        fy = _fy;
//        cx = _cx;
//        cy = _cy;
//        w  = _w;
//        h  = _h;
//    }
//    public string userID, mapName;
//    public float fx, fy, cx, cy;
//    public int w, h;
//    public bool bMapping;
//}

//public class JsonDeepData
//{
//    public JsonDeepData() { }
//    public JsonDeepData(int _id, string a, int _w, int _h, int _c)
//    {
//        id = _id;
//        w = _w;
//        h = _h;
//        c = _c;
//        img = a;
//    }
//    public JsonDeepData(int _id)
//    {
//        id = _id;
//    }
//    public int id;
//    public int w;
//    public int h;
//    public int c;
//    public int n;
//    public string img;
//    public string depth;
//}

//public class JsonSLAMData
//{
//    public JsonSLAMData() { }
//    public JsonSLAMData(int _id1, int _id2)
//    {
//        id1 = _id1;
//        id2 = _id2;
//    }
//    public JsonSLAMData(int _id1)
//    {
//        id1 = _id1;
//    }

//    public JsonSLAMData(int _id2, bool b)
//    {
//        id2 = _id2;
//        init = b;
//    }
//    public JsonSLAMData(int _id2, bool b, string _pose, string _key, string _map)
//    {
//        id2 = _id2;
//        init = b;
//        pose = _pose;
//        keypoints = _key;
//        mappoints = _map;
//    }

//    public int id1; //reference image : 초기화시에는 initframe1, 트래킹시에는 refrence하는 이미지
//    public int id2; //target image : 디텍션을 요청하는 이미지, 초기화시에는 id1로 변화되지 않으나, tracking시에는 거의 id1로 return이 됨.
//    public int n;
//    public bool init;
//    public string pose, keypoints, mappoints;
//    //public List<float> pose;
//    //public List<float> keypoints;
//    //public List<float> mappoints;
//}

public class DeviceController : MonoBehaviour
{
    [DllImport("edgeslam")]
    private static extern void SetTargetFrame(IntPtr addr, char[] timestamp, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetTrackingFrame(IntPtr addr, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetParam(float fx, float fy, float cx, float cy);
    
    /// <summary>
    //코드 재정리
    //21.04.08
    private ContentEchoServer mEchoServer;
    private int rectWidth = 200;
    private int rectHeight = 80;
    void OnGUI()
    {
        if (SystemManager.Instance.Connect)
        {
            if (GUI.Button(new Rect(60, 60, rectWidth, rectHeight), "Disconnect"))
            {
                SystemManager.Instance.Connect = false;
                Disconnect();
                //////Disconnect Echo Server
                float[] fdata = new float[1];
                fdata[0] = 10001f;
                byte[] bdata = new byte[4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 
                AsyncSocketReceiver.Instance.Disconnect();
                //////Disconnect Echo Server
                mEchoServer.bConnect = false;
                mEchoServer.StopEchoClient();
            }
        }
        else
        {
            if (GUI.Button(new Rect(60, 60, rectWidth, rectHeight), "Connect"))
            {
                SystemManager.Instance.Connect = true;
                Connect();
                mEchoServer.bConnect = true;
                mEchoServer.StartEchoClient();
                //////Connect Echo Server
                AsyncSocketReceiver.Instance.Connect("143.248.6.143", 35001);
                float[] fdata = new float[1];
                fdata[0] = 10000f;
                byte[] bdata = new byte[4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 
                //////Connect Echo Server
            }
        }
        if (SystemManager.Instance.Start)
        {
            if (GUI.Button(new Rect(120+ rectWidth, 60, rectWidth, rectHeight), "Stop"))
            {
                SystemManager.Instance.Start = false;
            }
        }
        else
        {
            if (GUI.Button(new Rect(120+ rectWidth, 60, rectWidth, rectHeight), "Start"))
            {
                SystemManager.Instance.Start = true;
            }
        }

        if (GUI.Button(new Rect(180 + rectWidth*2, 60, rectWidth, rectHeight), "Load Model"))
        {
            
        }
    }

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
        string path = Application.persistentDataPath + "/param.txt";
        
        SystemManager.Instance.LoadParameter(path);
        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
        nImgFrameIDX = 3;

        bCam = SystemManager.Instance.Cam;
        Debug.Log("cam = " + bCam);
        if (!bCam)
        {
            imageData = SystemManager.Instance.ImageData;
            Debug.Log(imageData.ToString());
            imagePath = SystemManager.Instance.ImagePath;
            //nMaxImageIndex = mSystem.imageData.Length - 1;
        }
        else {

        }
        Debug.Log(gameObject.name);
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

        mEchoServer = FindObjectOfType<ContentEchoServer>();
        sw = new Stopwatch();
        //try
        //{
        //    ////유저 데이터 열기
        //    string strParameterFilePath = Application.persistentDataPath + "/param.txt";
        //    string[] paramText = File.ReadAllLines(strParameterFilePath);
        //    int nUserData = 0;
        //    strUserID = (paramText[nUserData++].Split('=')[1]);
        //    serveraddr = (paramText[nUserData++].Split('=')[1]);
        //    bool bMapLoad = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        //    bool bMapReset = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        //    bMapping = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        //    strMapName = (paramText[nUserData++].Split('=')[1]);
        //    string datafile = (paramText[nUserData++].Split('=')[1]);
        //    Debug.Log(strUserID + ":" + bMapping+"::"+ datafile);

        //    if (!bMapping)
        //        mnFrameID = 10000;

        //    ////////데이터 파일 읽기
        //    //string strFilePath = Application.persistentDataPath + "/data.txt";
        //    //string[] tempText = File.ReadAllLines(strFilePath); //실제 데이터 파일의 위치.
        //    //string datafile = Convert.ToString(tempText[0].Split('=')[1]);
        //    string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기

        //    Debug.Log(Application.persistentDataPath + datafile+"::"+dataText.Length);

        //    //strMapName = datafile.Split('/')[2].Split('.')[0];
        //    //Debug.Log(strMapName);

        //    if (datafile == "/File/cam.txt")
        //    {
        //        bCam = true;
        //    }

        //    int numLine = 0;
        //    if (!bCam)
        //    {
        //        string imgFileTxt = Application.persistentDataPath + Convert.ToString(dataText[numLine++].Split('=')[1]);
        //        imgFileLIst = File.ReadAllLines(imgFileTxt);
        //        Debug.Log("Load Datase = "+(imgFileLIst.Length-3));
        //        imgPathTxt = Convert.ToString(dataText[numLine++].Split('=')[1]);
        //        if (Application.platform == RuntimePlatform.Android)
        //            imgPathTxt = Application.persistentDataPath + imgPathTxt;
        //    }
        //    else
        //    {
        //        numLine = 2;
        //    }

        //    fx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        //    fy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        //    cx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        //    cy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        //    w = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
        //    h = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
        //    Debug.Log(fx + " " + fy);
        //    DepthSource.Width = w;
        //    DepthSource.Height = h;

        //    if (bCam)
        //    {
        //        //#if UNITY_ANDROID
        //        //            w = 640;
        //        //            h = 360;
        //        //#endif
        //        //#if UNITY_EDITOR_WIN
        //        //            w = 320*2;
        //        //            h = 240*2;
        //        //#endif
        //        WebCamDevice[] devices = WebCamTexture.devices;
        //        for (int i = 0; i < devices.Length; i++)
        //        {
        //            if(Application.platform == RuntimePlatform.Android && !devices[i].isFrontFacing)
        //            {
        //                webCamTexture = new WebCamTexture(devices[i].name, w, h, 30);
        //                break;
        //            }
        //            else if (Application.platform == RuntimePlatform.WindowsEditor && devices[i].isFrontFacing)
        //            {

        //                webCamTexture = new WebCamTexture(devices[i].name, w, h, 30);
        //                Debug.Log(devices[i].name + "::" + webCamTexture.requestedWidth + ", " + webCamTexture.requestedHeight);
        //                break;
        //            }
        //        }
        //        webCamTexture.Play();

        //        ////webcam image pointer 연결
        //        webCamColorData = new Color[w * h];
        //        //webCamHandle = default(GCHandle);
        //        webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
        //        webCamPtr = webCamHandle.AddrOfPinnedObject();
        //        background.texture = webCamTexture;
        //    }

        //    tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        //    mMessageQueue = new ConcurrentQueue<Tuple<string, byte[], string, int>>();
        //    mHashIDSet = new HashSet<int>();
        //    ////reset
        //    string addr3 = serveraddr + "/Connect";
        //    InitConnectData data3 = new InitConnectData(strUserID, strMapName, bMapping, fx, fy, cx, cy, w, h);
        //    string msg3 = JsonUtility.ToJson(data3);
        //    byte[] bdata3 = System.Text.Encoding.UTF8.GetBytes(msg3);
        //    Debug.Log(msg3);
        //    Tuple<string, byte[], string, int> connect = new Tuple<string, byte[], string, int>(addr3, bdata3, "POST", 0);

        //    string addr1 = serveraddr + "/reset?map=" + strMapName;
        //    JsonDeepData data1 = new JsonDeepData(0);
        //    string msg1 = JsonUtility.ToJson(data1);
        //    byte[] bdata1 = System.Text.Encoding.UTF8.GetBytes(msg1);
        //    Tuple<string, byte[], string, int> reset = new Tuple<string, byte[], string, int>(addr1, bdata1, "POST", 0);

        //    //string addr2 = slamip + slamport + "/reset";
        //    //Tuple<string, byte[], string, int> b = new Tuple<string, byte[], string, int>(addr2, bdata1, "POST", 0);

        //    //string addr_connect = deepip + deepport + "/Connect?fx="+fx;
        //    //Tuple<string, byte[], string, int> data_connect = new Tuple<string, byte[], string, int>(addr_connect, bdata1, "GET", 0);

        //    string addr4 = serveraddr + "/LoadMap?map="+strMapName;
        //    JsonDeepData data4 = new JsonDeepData(0);
        //    string msg4 = JsonUtility.ToJson(data4);
        //    byte[] bdata4 = System.Text.Encoding.UTF8.GetBytes(msg4);
        //    Tuple<string, byte[], string, int> load = new Tuple<string, byte[], string, int>(addr4, bdata4, "POST", 0);

        //    mMessageQueue.Enqueue(connect);
        //    if (bMapReset) { 
        //        mMessageQueue.Enqueue(reset);
        //    }
        //    if(bMapLoad)
        //        mMessageQueue.Enqueue(load);

        //    //mMessageQueue.Enqueue(b);
        //    //mMessageQueue.Enqueue(data_connect);

        //    ////초기 시간 설정
        //    baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();
        //    if (Application.platform == RuntimePlatform.Android)
        //        SetParam(fx, fy, cx, cy);
        //}
        //catch (Exception e)
        //{
        //    //text.text = e.ToString();
        //}
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
                if (mbSended && mnFrameID % 3 == 0)
                {
                    mbSended = false;
                    StartCoroutine("MappingCoroutine");
                }
            }
        }
        
        bool bTouch = false;
        Ray ray = new Ray();
        if (Application.platform == RuntimePlatform.Android)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                ray = Camera.main.ScreenPointToRay(touch.position);
                bTouch = true;
            }
        }
        else {
            if (Input.GetMouseButtonDown(0))
            {
                try {
                    ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    bTouch = true;
                } catch(Exception e)
                {
                    Debug.Log(e.ToString());
                }
                //Debug.Log("mouse : " + Input.mousePosition.x + " " + Input.mousePosition.y);
            }
        }

        if (bTouch)
        {
            try {
                //Debug.Log("dir = "+touchDir.x+" "+touchDir.y+ " "+touchDir.z);
                float[] fdata = new float[8];
                int nIDX = 0;
                fdata[nIDX++] = 2f; //method type = 1 : manager, 2 = content
                fdata[nIDX++] = 0f; //content id
                fdata[nIDX++] = Center.x; //ray.origin.x;//Center.x;
                fdata[nIDX++] = Center.y; //ray.origin.y;//Center.y;
                fdata[nIDX++] = Center.z; //ray.origin.z;//Center.z;
                fdata[nIDX++] = ray.direction.x;
                fdata[nIDX++] = ray.direction.y;
                fdata[nIDX++] = ray.direction.z;
                byte[] bdata = new byte[fdata.Length*4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
                AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 
            } catch(Exception e)
            {
                Debug.Log(e.ToString());
            }
            
        }

        //if (bCam)
        //{
        //    ////카메라 버전은 아직 수정을 안함
        //    if (webCamTexture.didUpdateThisFrame)
        //    {
        //        webCamColorData = webCamTexture.GetPixels();
        //       ++mnFrameID;

        //        //if (mnFrameID % 3 == 0 && mbSended)
        //        //{
        //        //    mbSended = false;
        //        //    StartCoroutine("Test1");
        //        //}
        //    }

        //    float ratio = (float)webCamTexture.width / (float)webCamTexture.height;
        //    fit.aspectRatio = ratio;
        //    float scaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;
        //    background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
        //    int orient = -webCamTexture.videoRotationAngle;
        //    background.rectTransform.localEulerAngles = new Vector3(0f, 0f, orient);
        //}
        //else{
        //    if(imgFileLIst.Length == nImgFrameIDX)
        //    {
        //        Debug.Log("END!!!!!!!");
        //        return;
        //    }
        //    ////데이터셋 버전 수정 중
        //    string imgFile = imgPathTxt + Convert.ToString(imgFileLIst[nImgFrameIDX++].Split(' ')[1]);
        //    //if(Application.platform == RuntimePlatform.Android)
        //    {
        //        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        //        tex.LoadImage(byteTexture);
        //        webCamColorData = tex.GetPixels();
        //        background.texture = tex;
        //    }
        //    ++mnFrameID;

        //    //if (mbSended && mnFrameID % 3 == 0)
        //    //{
        //    //    mbSended = false;
        //    //    StartCoroutine("Test1");
        //    //}
        //}

        //GCHandle gch = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
        //IntPtr addr = gch.AddrOfPinnedObject();
        //if (mbSended && mnFrameID % 3 == 0)
        //{
        //    mbSended = false;
        //    string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
        //    try {
        //        if (Application.platform == RuntimePlatform.Android)
        //            SetTargetFrame(addr, ts.ToCharArray(), w, h);
        //    } catch(Exception e)
        //    {
        //        StatusTxt.text = e.ToString();
        //    }

        //    StartCoroutine("Test1", ts);
        //}else
        //{
        //    try
        //    {
        //        if (Application.platform == RuntimePlatform.Android)
        //            SetTrackingFrame(addr, w, h);
        //    }
        //    catch (Exception e)
        //    {
        //        StatusTxt.text = e.ToString();
        //    }
        //}

        //try
        //{
        //    gch.Free();
        //}
        //catch (Exception e)
        //{
        //    StatusTxt.text = e.ToString();
        //}


        //else if (mbSended && mnFrameID % 9 == 2)
        //{
        //    mbSended = false;
        //    StartCoroutine("Test2");
        //}
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
            //Debug.Log(prevID + "::" + t.ToString());
            Center = -(R.Transpose() * t);

            ////업데이트 카메라 포즈
            Vector3 mAxis = R.LOG();
            DIR = mAxis;

            float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
            mAxis = mAxis.normalized;
            Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
            Quaternion q = Matrix3x3.RotToQuar(R);
            gameObject.transform.SetPositionAndRotation(Center, q);
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














    //IEnumerator Test1(string ts)
    //{
    //    yield return new WaitForEndOfFrame();
    //    if(!bconnect)
    //        bconnect = true;
    //    //Debug.Log((DateTime.Now.ToLocalTime() - baseTime).ToString());
    //    //mnFrameID2 = (int)(DateTime.Now.ToLocalTime() - baseTime).Ticks;

    //    if (bCam)
    //        tex.SetPixels(webCamColorData);
    //    byte[] webCamByteData = tex.EncodeToJPG(90);
    //    //string encodedText = Convert.ToBase64String(webCamByteData);
    //    string addr1 = serveraddr + "/ReceiveAndDetect?user="+strUserID+"&map="+strMapName+"&id="+ts;
    //    //JsonDeepData data1 = new JsonDeepData(mnFrameID2, encodedText, w, h, 3);
    //    //string msg1 = JsonUtility.ToJson(data1);
    //    Tuple<string, byte[], string, int> a = new Tuple<string, byte[], string, int>(addr1, webCamByteData, "POST", 1);
    //    mMessageQueue.Enqueue(a);

    //    //yield return new WaitUntil(()=>mbSended);
    //}

    //IEnumerator Test2()
    //{
    //    yield return new WaitForEndOfFrame();
    //    string addr1 = serveraddr + "/SendDepth";
    //    JsonDeepData data1 = new JsonDeepData(mnFrameID2, "", w, h, 3);
    //    string msg1 = JsonUtility.ToJson(data1);
    //    byte[] bdata1 = System.Text.Encoding.UTF8.GetBytes(msg1);
    //    Tuple<string, byte[], string, int> a = new Tuple<string, byte[], string, int>(addr1, bdata1, "POST", 2);
    //    mMessageQueue.Enqueue(a);
    //}













    //IEnumerator Test2()
    //{
    //    StartCoroutine(Test1());
    //    yield return new WaitUntil(() => mHashIDSet.Contains(mnFrameID2));
    //    string addr2 = ip1 + port2 + "/detect";
    //    JsonSLAMData data2 = new JsonSLAMData(mnFrameID2, mbInitialized);
    //    string msg2 = JsonUtility.ToJson(data2);
    //    Tuple<string, string, int> b = new Tuple<string, string, int>(addr2, msg2, 2);
    //    //string addr2 = ip1 + port1 + "/detect";
    //    //JsonDeepData data2 = new JsonDeepData(mnFrameID2);
    //    //string msg2 = JsonUtility.ToJson(data2);
    //    //Tuple<string, string, int> b = new Tuple<string, string, int>(addr2, msg2, 2);
    //    mMessageQueue.Enqueue(b);
    //}



    //    IEnumerator CameraManage()
    //    {
    //        while (true) { 
    //            yield return new WaitForFixedUpdate();


    //            if (mnFrameID % 3 == 0)
    //            {
    //                tex.SetPixels(webCamColorData);
    //                //tex.Apply();
    //                byte[] webCamByteData = tex.EncodeToJPG(90);
    //                string encodedText = Convert.ToBase64String(webCamByteData);

    //                string addr1 = ip1 + port1 + "/receiveimage";
    //                JsonDeepData data1 = new JsonDeepData(mnFrameID, encodedText, w, h, 3);
    //                string msg1 = JsonUtility.ToJson(data1);
    //                Tuple<string, string, int> a = new Tuple<string, string, int>(addr1, msg1, 1);
    //                mMessageQueue.Enqueue(a);
    //                string addr2 = ip1 + port1 + "/detect";
    //                JsonDeepData data2 = new JsonDeepData(mnFrameID);
    //                string msg2 = JsonUtility.ToJson(data2);
    //                Tuple<string, string, int> b = new Tuple<string, string, int>(addr2, msg2, 1);
    //                mMessageQueue.Enqueue(b);
    //                //string addr3 = ip1 + port2 + "/initialization";
    //                //JsonSLAMData data3 = new JsonDeepData(mnFrameID);
    //                //string msg2 = JsonUtility.ToJson(data2);
    //                //Tuple<string, string> b = new Tuple<string, string>(addr2, msg2);

    //                mnFrameID2 = mnFrameID;
    //                if (!mbInitialized && mnFrameID1 < mnFrameID2)
    //                {
    //                    JsonSLAMData data3 = new JsonSLAMData(mnFrameID1, mnFrameID2);
    //                    string jsonStr3 = JsonUtility.ToJson(data3);
    //                    string addr3 = ip1 + port2 + "/initialization";
    //                    string msg3 = JsonUtility.ToJson(data3);
    //                    Tuple<string, string, int> c = new Tuple<string, string, int>(addr3, msg3, 2);
    //                    mMessageQueue.Enqueue(c);
    //                }
    //                else if(mbInitialized)
    //                {
    //                    JsonSLAMData data3 = new JsonSLAMData(mnFrameID2);
    //                    string jsonStr3 = JsonUtility.ToJson(data3);
    //                    string addr3 = ip1 + port2 + "/tracking";
    //                    string msg3 = JsonUtility.ToJson(data3);
    //                    Tuple<string, string, int> c = new Tuple<string, string, int>(addr3, msg3, 2);
    //                    mMessageQueue.Enqueue(c);
    //                }

    //#if UNITY_EDITOR_WIN
    //                //JsonSLAMData data2 = new JsonSLAMData(0,3);
    //                //string jsonStr2 = JsonUtility.ToJson(data2);
    //                //Debug.Log(jsonStr2);
    //                //{
    //                //    addr1 = ip1 + port2 + "/receiveimage";
    //                //    data1 = new JsonDeepData(mnFrameID, encodedText, w, h, 3);
    //                //    msg1 = JsonUtility.ToJson(data1);
    //                //    a = new Tuple<string, string>(addr1, msg1);
    //                //    mMessageQueue.Enqueue(a);
    //                //}

    //#endif

    //                //Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    //                //tex.SetPixels32(webCamColorData);
    //                //byte[] webCamByteData = tex.EncodeToJPG();
    //                //DestroyImmediate(tex);
    //                //string encodedText = Convert.ToBase64String(webCamByteData);
    //                //ImageData data = new ImageData(mnFrameID, encodedText, w, h, 3);
    //                //string jsonStr = JsonUtility.ToJson(data);
    //                //byte[] abytes = System.Text.Encoding.UTF8.GetBytes(jsonStr);

    //                //UnityWebRequest request = new UnityWebRequest("http://143.248.96.81:35005/receiveimage");
    //                ////request.SetRequestHeader("Content-Type", "application/json");
    //                //request.method = "POST";
    //                //UploadHandlerRaw uH = new UploadHandlerRaw(abytes);
    //                //uH.contentType = "application/json";
    //                //request.uploadHandler = uH;
    //                //request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    //                //yield return request.SendWebRequest();

    //                //if (request.isNetworkError || request.isHttpError)
    //                //{
    //                //    StatusTxt.text = "send::error=" + mnFrameID;
    //                //}
    //                //else
    //                //{
    //                //    StatusTxt.text = "send::complete=" + mnFrameID;
    //                //}
    //            }
    //            //mbSendImage = true;
    //        }
    //        //try {

    //        //    webCamTexture.GetPixels32(webCamColorData);
    //        //    if (mnFrameID % 3 == 0)
    //        //    {
    //        //        //StatusTxt.text = "send image = " + mnFrameID;
    //        //        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    //        //        tex.SetPixels32(webCamColorData);
    //        //        webCamByteData = tex.EncodeToJPG();
    //        //        string encodedText = Convert.ToBase64String(webCamByteData);
    //        //        ImageData data = new ImageData(mnFrameID, encodedText, w, h, 3);
    //        //        string jsonStr = JsonUtility.ToJson(data);
    //        //        StartCoroutine("SendImage", jsonStr);
    //        //        DestroyImmediate(tex);
    //        //    }
    //        //}
    //        //catch(Exception e)
    //        //{
    //        //    StatusTxt.text = e.ToString();
    //        //}
    //    }

    //IEnumerator SendImage(string jsonStr)
    //{
    //    using (UnityWebRequest request = UnityWebRequest.Post("http://143.248.96.81:35005/receiveimage", jsonStr))
    //    {
    //        request.SetRequestHeader("Content-Type", "application/json");
    //        request.SendWebRequest();
    //        yield return new WaitForSeconds(0.01f);
    //    }
    //    //using (UnityWebRequest request = new UnityWebRequest("http://143.248.96.81:35005/receiveimage"))
    //    //{
    //    //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonStr);
    //    //    //request.SetRequestHeader("Content-Type", "application/json");
    //    //    request.method = "POST";
    //    //    UploadHandlerRaw uH = new UploadHandlerRaw(bytes);
    //    //    uH.contentType = "application/json";
    //    //    request.uploadHandler = uH;
    //    //    request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    //    //    yield return request.SendWebRequest();
    //    //    if (request.isNetworkError || request.isHttpError)
    //    //    {
    //    //        StatusTxt.text = "send::error="+jsonStr;
    //    //    }
    //    //    else
    //    //    {
    //    //        StatusTxt.text = "send::complete";
    //    //    }
    //    //}
    //}

    //void OnApplicationPause()
    //{
    //    Debug.Log("pause");
    //    Disconnect();
    //}

}
