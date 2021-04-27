﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SystemManager {

    public class InitConnectData
    {
        public InitConnectData() { }
        public InitConnectData(string _userID, string _mapName, bool _bMapping, bool _bManager,
            float _fx, float _fy, float _cx, float _cy,
            float _d1, float _d2, float _d3, float _d4, int _w, int _h)
        {
            userID = _userID;
            mapName = _mapName;
            bMapping = _bMapping;
            bManager = _bManager;
            fx = _fx;
            fy = _fy;
            cx = _cx;
            cy = _cy;
            d1 = _d1;
            d2 = _d2;
            d3 = _d3;
            d4 = _d4;
            w = _w;
            h = _h;
        }
        public string userID, mapName;
        public float fx, fy, cx, cy;
        public float d1, d2, d3, d4;
        public int w, h;
        public bool bMapping, bManager;
    }

    private static int w, h;
    private static float fx, fy, cx, cy;
    float d1, d2, d3, d4;
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

    static private Matrix3x3 k;
    public Matrix3x3 K
    {
        get
        {
            return k;
        }
        set
        {
            k = value;
        }
    }
    static private float dis_scale = 1f;
    public float DisplayScale
    {
        get
        {
            return dis_scale;
        }
        set
        {
            dis_scale = value;
        }
    }

    private static string strUserID, strMapName;
    public string User
    {
        get
        {
            return strUserID;
        }
        set
        {
            strUserID = value;
        }
    }
    public string Map
    {
        get
        {
            return strMapName;
        }
        set
        {
            strMapName = value;
        }
    }
    private static string serveraddr;
    public string ServerAddr
    {
        get
        {
            return serveraddr;
        }
        set
        {
            serveraddr = value;
        }
    }
    static string[] imgFileLIst;
    public string[] ImageData
    {
        get
        {
            return imgFileLIst;
        }
    }
    static string imgPathTxt;
    public string ImagePath
    {
        get
        {
            return imgPathTxt;
        }
    }

    static bool bconnect = false;
    public bool Connect
    {
        get
        {
            return bconnect;
        }
        set
        {
            bconnect = value;
        }
    }

    static bool bstart = false;
    public bool Start {
        get
        {
            return bstart;
        }
        set
        {
            bstart = value;
        }
    }

    static bool bCam = false;
    public bool Cam
    {
        get
        {
            return bCam;
        }
    }

    static bool bMapping;
    public bool Mapping
    {
        get
        {
            return bMapping;
        }
        set
        {
            bMapping = value;
        }
    }
    static bool bManagerMode = true;
    public bool Manager
    {
        get
        {
            return bManagerMode;
        }
        set
        {
            bManagerMode = value;
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
    
    public void LoadParameter(string path)
    {
        string[] paramText = File.ReadAllLines(path);
        int nUserData = 0;
        strUserID = (paramText[nUserData++].Split('=')[1]);
        serveraddr = (paramText[nUserData++].Split('=')[1]);
        bool bMapLoad = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        bool bMapReset = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        bMapping = Convert.ToBoolean(paramText[nUserData++].Split('=')[1]);
        strMapName = (paramText[nUserData++].Split('=')[1]);
        string datafile = (paramText[nUserData++].Split('=')[1]);
        string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기
        int numLine = 0;
        if (datafile == "/File/cam.txt")
        {
            bCam = true;
            numLine = 2;
        }

        if (!bCam)
        {
            string imgFileTxt = Application.persistentDataPath + Convert.ToString(dataText[numLine++].Split('=')[1]);
            imgFileLIst = File.ReadAllLines(imgFileTxt);
            Debug.Log("Load Datase = " + (imgFileLIst.Length - 3));
            imgPathTxt = Convert.ToString(dataText[numLine++].Split('=')[1]);
            if (Application.platform == RuntimePlatform.Android)
                imgPathTxt = Application.persistentDataPath + imgPathTxt;
        }     
                
        fx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        fy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        cx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        cy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        w = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
        h = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
        d1 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        d2 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        d3 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        d4 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
        Debug.Log(d1 + " " + d2);
        k = new Matrix3x3(fx, 0f, cx, 0f, fy, cy, 0f, 0f, 1f);
    }
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
        return new InitConnectData(strUserID, strMapName, bMapping, bManagerMode, fx, fy, cx, cy, d1, d2, d3, d4, w, h);
    }
}
