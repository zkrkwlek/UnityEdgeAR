using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SystemManager {

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

    private static int w, h;
    private static float fx, fy, cx, cy;
    public int ImageWidth
    {
        get
        {
            return w;
        }
    }
    public int ImageHeight
    {
        get
        {
            return h;
        }
    }
    public float FocalLengthX
    {
        get
        {
            return fx;
        }
    }
    public float FocalLengthY
    {
        get
        {
            return fy;
        }
    }
    public float PrincipalPointX
    {
        get
        {
            return cx;
        }
    }
    public float PrincipalPointY
    {
        get
        {
            return cy;
        }
    }
    private static string strUserID, strMapName;
    public string User
    {
        get
        {
            return strUserID;
        }
    }
    public string Map
    {
        get
        {
            return strMapName;
        }
    }
    private static string serveraddr;
    public string ServerAddr
    {
        get
        {
            return serveraddr;
        }
    }

    static private SystemManager m_pInstance = null;
    static public SystemManager Instance
    {
        get{
            if (m_pInstance == null)
            {
                m_pInstance = new SystemManager();
                //LoadParameter();
            }
            return m_pInstance;
        }
    }

    static bool bconnect = false;
    static string[] imgFileLIst;
    static string imgPathTxt;
    static int nImgFrameIDX = 3;
    static bool bCam = false;
    static bool bMapping;

    public void LoadParameter()
    {
        Debug.Log("Load Parameter!!");
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

        string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기

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
        
        ////이건 뎁스소스에서 가져가도록 해야 함
        //DepthSource.Width = w;
        //DepthSource.Height = h;
    }

    public InitConnectData GetConnectData()
    {
        return new InitConnectData(strUserID, strMapName, bMapping, fx, fy, cx, cy, w, h);
    }
}
