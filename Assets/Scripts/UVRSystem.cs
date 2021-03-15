using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UVRSystem : MonoBehaviour
{
    //// Start is called before the first frame update
    //void Start()
    //{

    //}

    //// Update is called once per frame
    //void Update()
    //{

    //}
    static string[] imgFileLIst;
    static string imgPathTxt;
    static int nImgFrameIDX = 3;
    static bool bCam = false;
    static bool bMapping;

    private static string serveraddr;
    public static string ServerAddr
    {
        get
        {
            return serveraddr;
        }
    }
    
    private static bool bconnect = false;
    //public static bool Connect
    //{
    //    get
    //    {
    //        return bconnect;
    //    }
    //}

    private static string strUserID, strMapName;
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
    private static int w, h;
    private static float fx, fy, cx, cy;
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
        get
        {
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

    public class InitConnectData
    {
        public InitConnectData() { }
        public InitConnectData(string _userID, string _mapName, bool _bMapping, float _fx, float _fy, float _cx, float _cy, int _w, int _h)
        {
            userID = _userID;
            mapName = _mapName;
            bMapping = _bMapping;
            fx = _fx;
            fy = _fy;
            cx = _cx;
            cy = _cy;
            w = _w;
            h = _h;
        }
        public string userID, mapName;
        public float fx, fy, cx, cy;
        public int w, h;
        public bool bMapping;
    }

    static GCHandle webCamHandle;
    static WebCamTexture webCamTexture;
    [HideInInspector]
    static public Color[] webCamColorData;
    static IntPtr webCamPtr;

    public RawImage background;

    //[MenuItem("UVRSystem/Load Parameter")]
    static void LoadParam()
    {
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
        Debug.Log(strUserID + ":" + bMapping + "::" + datafile);

        string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기

        Debug.Log(Application.persistentDataPath + datafile + "::" + dataText.Length);

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
            Debug.Log("Load Datase = " + (imgFileLIst.Length - 3));
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
                if (Application.platform == RuntimePlatform.Android && !devices[i].isFrontFacing)
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
            //webCamTexture.Play();
            //////webcam image pointer 연결
            //webCamColorData = new Color[w * h];
            ////webCamHandle = default(GCHandle);
            //webCamHandle = GCHandle.Alloc(webCamColorData, GCHandleType.Pinned);
            //webCamPtr = webCamHandle.AddrOfPinnedObject();
            //background.texture = webCamTexture;
        }

    }

    [MenuItem("UVRSystem/Connect")]
    static void Connect()
    {
        //id, map, mapping 여부, fx, fy, cx, cy, w, h
        LoadParam();
    }

    [MenuItem("UVRSystem/Disconnect")]
    void Disconnect()
    {
        Debug.Log("Doing Something...");
    }

    [MenuItem("UVRSystem/Reset")]
    static void Reset()
    {
        Debug.Log("Doing Something...");
    }

    [MenuItem("UVRSystem/Save Map")]
    static void SaveMap()
    {
        Debug.Log("Doing Something...");
    }

    [MenuItem("UVRSystem/Load Map")]
    static void LoadMap()
    {
        Debug.Log("Doing Something...");
    }
}
