using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InitConnectData{
    public InitConnectData() { }
    public InitConnectData(string _userID, string _mapName , bool _bMapping, float _fx, float _fy, float _cx, float _cy, int _w, int _h)
    {
        userID = _userID;
        mapName = _mapName;
        bMapping = _bMapping;
        fx = _fx;
        fy = _fy;
        cx = _cx;
        cy = _cy;
        w  = _w;
        h  = _h;
    }
    public string userID, mapName;
    public float fx, fy, cx, cy;
    public int w, h;
    public bool bMapping;
}

public class JsonDeepData
{
    public JsonDeepData() { }
    public JsonDeepData(int _id, string a, int _w, int _h, int _c)
    {
        id = _id;
        w = _w;
        h = _h;
        c = _c;
        img = a;
    }
    public JsonDeepData(int _id)
    {
        id = _id;
    }
    public int id;
    public int w;
    public int h;
    public int c;
    public int n;
    public string img;
    public string depth;
}

public class JsonSLAMData
{
    public JsonSLAMData() { }
    public JsonSLAMData(int _id1, int _id2)
    {
        id1 = _id1;
        id2 = _id2;
    }
    public JsonSLAMData(int _id1)
    {
        id1 = _id1;
    }

    public JsonSLAMData(int _id2, bool b)
    {
        id2 = _id2;
        init = b;
    }
    public JsonSLAMData(int _id2, bool b, string _pose, string _key, string _map)
    {
        id2 = _id2;
        init = b;
        pose = _pose;
        keypoints = _key;
        mappoints = _map;
    }

    public int id1; //reference image : 초기화시에는 initframe1, 트래킹시에는 refrence하는 이미지
    public int id2; //target image : 디텍션을 요청하는 이미지, 초기화시에는 id1로 변화되지 않으나, tracking시에는 거의 id1로 return이 됨.
    public int n;
    public bool init;
    public string pose, keypoints, mappoints;
    //public List<float> pose;
    //public List<float> keypoints;
    //public List<float> mappoints;
}

public class CameraManager : MonoBehaviour
{
    [DllImport("edgeslam")]
    private static extern void SetTargetFrame(IntPtr addr, char[] timestamp, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetTrackingFrame(IntPtr addr, int w, int h);
    [DllImport("edgeslam")]
    private static extern void SetParam(float fx, float fy, float cx, float cy);
    

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

    DateTime baseTime;
    
    public bool mbSended = true;
    public bool mbDetected = true;
    public ConcurrentQueue<Tuple<string, byte[], string, int>> mMessageQueue;
    public HashSet<int> mHashIDSet;

    public int mnFrameID1 = 0;
    int mnFrameID2 = 0;
    public bool mbInitialized = false;
    
    bool bMapping;
    string serverip;
    int serverport;

    string[] imgFileLIst;
    string imgPathTxt;
    int nImgFrameIDX = 3;
    
    bool bCam = false;

    private static int w, h;
    private static float fx, fy, cx, cy;
    private static string strUserID, strMapName;
    
    private static bool bconnect = false;
    public static bool Connect
    {
        get
        {
            return bconnect;
        }
    }
    public static string User
    {
        get
        {
            return strUserID;
        }
    }
    public static string Map
    {
        get
        {
            return strMapName;
        }
    }

    public static int ImageWidth
    {
        get
        {
            return w;
        }
    }
    public static int ImageHeight
    {
        get
        {
            return h;
        }
    }
    public static float FocalLengthX
    {
        get {
            return fx;
        }
    }
    public static float FocalLengthY
    {
        get
        {
            return fy;
        }
    }
    public static float PrincipalPointX
    {
        get
        {
            return cx;
        }
    }
    public static float PrincipalPointY
    {
        get
        {
            return cy;
        }
    }

    private static string serveraddr;
    public static string ServerAddr
    {
        get
        {
            return serveraddr;
        }
    }

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }

    // Start is called before the first frame update
    void Start()
    {
        try
        {
            ////유저 데이터 열기
            string strParameterFilePath = Application.persistentDataPath + "/param.txt";
            string[] paramText = File.ReadAllLines(strParameterFilePath);
            int nUserData = 0;
            strUserID = (paramText[nUserData++].Split('=')[1]);
            serveraddr = (paramText[nUserData++].Split('=')[1]);
            bool bMapLoad = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
            bool bMapReset = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
            bMapping = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
            strMapName = (paramText[nUserData++].Split('=')[1]);
            string datafile = (paramText[nUserData++].Split('=')[1]);
            Debug.Log(strUserID + ":" + bMapping+"::"+ datafile);

            if (!bMapping)
                mnFrameID = 10000;

            ////////데이터 파일 읽기
            //string strFilePath = Application.persistentDataPath + "/data.txt";
            //string[] tempText = File.ReadAllLines(strFilePath); //실제 데이터 파일의 위치.
            //string datafile = Convert.ToString(tempText[0].Split('=')[1]);
            string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기

            Debug.Log(Application.persistentDataPath + datafile+"::"+dataText.Length);

            //strMapName = datafile.Split('/')[2].Split('.')[0];
            //Debug.Log(strMapName);

            if (datafile == "/File/cam.txt")
            {
                bCam = true;
            }

            int numLine = 0;
            if (!bCam)
            {
                string imgFileTxt = Application.persistentDataPath + Convert.ToString(dataText[numLine++].Split('=')[1]);
                imgFileLIst = File.ReadAllLines(imgFileTxt);
                Debug.Log("Load Datase = "+(imgFileLIst.Length-3));
                imgPathTxt = Convert.ToString(dataText[numLine++].Split('=')[1]);
                if (Application.platform == RuntimePlatform.Android)
                    imgPathTxt = Application.persistentDataPath + imgPathTxt;
            }
            else
            {
                numLine = 2;
            }

            fx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
            fy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
            cx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
            cy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
            w = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
            h = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
            Debug.Log(fx + " " + fy);
            DepthSource.Width = w;
            DepthSource.Height = h;

            if (bCam)
            {
                //#if UNITY_ANDROID
                //            w = 640;
                //            h = 360;
                //#endif
                //#if UNITY_EDITOR_WIN
                //            w = 320*2;
                //            h = 240*2;
                //#endif
                WebCamDevice[] devices = WebCamTexture.devices;
                for (int i = 0; i < devices.Length; i++)
                {
                    if(Application.platform == RuntimePlatform.Android && !devices[i].isFrontFacing)
                    {
                        webCamTexture = new WebCamTexture(devices[i].name, w, h, 30);
                        break;
                    }
                    else if (Application.platform == RuntimePlatform.WindowsEditor && devices[i].isFrontFacing)
                    {
                        
                        webCamTexture = new WebCamTexture(devices[i].name, w, h, 30);
                        Debug.Log(devices[i].name + "::" + webCamTexture.requestedWidth + ", " + webCamTexture.requestedHeight);
                        break;
                    }
                }
                webCamTexture.Play();

                ////webcam image pointer 연결
                webCamColorData = new Color[w * h];
                //webCamHandle = default(GCHandle);
                webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
                webCamPtr = webCamHandle.AddrOfPinnedObject();
                background.texture = webCamTexture;
            }

            tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            mMessageQueue = new ConcurrentQueue<Tuple<string, byte[], string, int>>();
            mHashIDSet = new HashSet<int>();
            ////reset
            string addr3 = serveraddr + "/Connect";
            InitConnectData data3 = new InitConnectData(strUserID, strMapName, bMapping, fx, fy, cx, cy, w, h);
            string msg3 = JsonUtility.ToJson(data3);
            byte[] bdata3 = System.Text.Encoding.UTF8.GetBytes(msg3);
            Debug.Log(msg3);
            Tuple<string, byte[], string, int> connect = new Tuple<string, byte[], string, int>(addr3, bdata3, "POST", 0);

            string addr1 = serveraddr + "/reset?map=" + strMapName;
            JsonDeepData data1 = new JsonDeepData(0);
            string msg1 = JsonUtility.ToJson(data1);
            byte[] bdata1 = System.Text.Encoding.UTF8.GetBytes(msg1);
            Tuple<string, byte[], string, int> reset = new Tuple<string, byte[], string, int>(addr1, bdata1, "POST", 0);
           
            //string addr2 = slamip + slamport + "/reset";
            //Tuple<string, byte[], string, int> b = new Tuple<string, byte[], string, int>(addr2, bdata1, "POST", 0);

            //string addr_connect = deepip + deepport + "/Connect?fx="+fx;
            //Tuple<string, byte[], string, int> data_connect = new Tuple<string, byte[], string, int>(addr_connect, bdata1, "GET", 0);

            string addr4 = serveraddr + "/LoadMap?map="+strMapName;
            JsonDeepData data4 = new JsonDeepData(0);
            string msg4 = JsonUtility.ToJson(data4);
            byte[] bdata4 = System.Text.Encoding.UTF8.GetBytes(msg4);
            Tuple<string, byte[], string, int> load = new Tuple<string, byte[], string, int>(addr4, bdata4, "POST", 0);

            mMessageQueue.Enqueue(connect);
            if (bMapReset) { 
                mMessageQueue.Enqueue(reset);
            }
            if(bMapLoad)
                mMessageQueue.Enqueue(load);

            //mMessageQueue.Enqueue(b);
            //mMessageQueue.Enqueue(data_connect);

            ////초기 시간 설정
            baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();
            if (Application.platform == RuntimePlatform.Android)
                SetParam(fx, fy, cx, cy);
        }
        catch (Exception e)
        {
            //text.text = e.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (bCam)
        {
            ////카메라 버전은 아직 수정을 안함
            if (webCamTexture.didUpdateThisFrame)
            {
                webCamColorData = webCamTexture.GetPixels();
               ++mnFrameID;
                
                //if (mnFrameID % 3 == 0 && mbSended)
                //{
                //    mbSended = false;
                //    StartCoroutine("Test1");
                //}
            }

            float ratio = (float)webCamTexture.width / (float)webCamTexture.height;
            fit.aspectRatio = ratio;
            float scaleY = webCamTexture.videoVerticallyMirrored ? -1f : 1f;
            background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
            int orient = -webCamTexture.videoRotationAngle;
            background.rectTransform.localEulerAngles = new Vector3(0f, 0f, orient);
        }
        else{
            if(imgFileLIst.Length == nImgFrameIDX)
            {
                Debug.Log("END!!!!!!!");
                return;
            }
            ////데이터셋 버전 수정 중
            string imgFile = imgPathTxt + Convert.ToString(imgFileLIst[nImgFrameIDX++].Split(' ')[1]);
            //if(Application.platform == RuntimePlatform.Android)
            {
                byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                tex.LoadImage(byteTexture);
                webCamColorData = tex.GetPixels();
                background.texture = tex;
            }
            ++mnFrameID;
            
            //if (mbSended && mnFrameID % 3 == 0)
            //{
            //    mbSended = false;
            //    StartCoroutine("Test1");
            //}
        }
        GCHandle gch = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
        IntPtr addr = gch.AddrOfPinnedObject();
        if (mbSended && mnFrameID % 3 == 0)
        {
            mbSended = false;
            string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
            try {
                if (Application.platform == RuntimePlatform.Android)
                    SetTargetFrame(addr, ts.ToCharArray(), w, h);
            } catch(Exception e)
            {
                StatusTxt.text = e.ToString();
            }
            
            StartCoroutine("Test1", ts);
        }else
        {
            try
            {
                if (Application.platform == RuntimePlatform.Android)
                    SetTrackingFrame(addr, w, h);
            }
            catch (Exception e)
            {
                StatusTxt.text = e.ToString();
            }
        }
        
        try
        {
            gch.Free();
        }
        catch (Exception e)
        {
            StatusTxt.text = e.ToString();
        }
        

        //else if (mbSended && mnFrameID % 9 == 2)
        //{
        //    mbSended = false;
        //    StartCoroutine("Test2");
        //}
    }

    IEnumerator Test1(string ts)
    {
        yield return new WaitForEndOfFrame();
        if(!bconnect)
            bconnect = true;
        //Debug.Log((DateTime.Now.ToLocalTime() - baseTime).ToString());
        //mnFrameID2 = (int)(DateTime.Now.ToLocalTime() - baseTime).Ticks;

        if (bCam)
            tex.SetPixels(webCamColorData);
        byte[] webCamByteData = tex.EncodeToJPG(90);
        //string encodedText = Convert.ToBase64String(webCamByteData);
        string addr1 = serveraddr + "/ReceiveAndDetect?user="+strUserID+"&map="+strMapName+"&id="+ts;
        //JsonDeepData data1 = new JsonDeepData(mnFrameID2, encodedText, w, h, 3);
        //string msg1 = JsonUtility.ToJson(data1);
        Tuple<string, byte[], string, int> a = new Tuple<string, byte[], string, int>(addr1, webCamByteData, "POST", 1);
        mMessageQueue.Enqueue(a);

        //yield return new WaitUntil(()=>mbSended);
    }
    IEnumerator Test2()
    {
        yield return new WaitForEndOfFrame();
        string addr1 = serveraddr + "/SendDepth";
        JsonDeepData data1 = new JsonDeepData(mnFrameID2, "", w, h, 3);
        string msg1 = JsonUtility.ToJson(data1);
        byte[] bdata1 = System.Text.Encoding.UTF8.GetBytes(msg1);
        Tuple<string, byte[], string, int> a = new Tuple<string, byte[], string, int>(addr1, bdata1, "POST", 2);
        mMessageQueue.Enqueue(a);
    }

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
    void OnApplicationQuit()
    {
        Disconnect();
        //Application.CancelQuit();
        
    }
    void Disconnect()
    {
        //yield return new WaitForEndOfFrame();
        string addr = serveraddr + "/Disconnect?userID="+ strUserID;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (!request.isDone)
        {
            //yield return new WaitForFixedUpdate();
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
