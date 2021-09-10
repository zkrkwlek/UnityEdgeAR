using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SystemManager {

    /// <summary>
    /// 파사드 서버에 내가 생성할 키워드를 알림.
    /// </summary>

    public class AppData {
        public bool bMapping;
        public bool bTracking;
        public bool bGyro;
        public bool bAcc;
    }

    public class ProcessTime
    {
        public int nTotal;
        public int nTotalSize;//jpeg
        public float fAvgSize;//jpeg
        public float fSum;
        public float fSum_2;
        public float fAvg;
        public float fStddev;

        public void Update(float ts)
        {
            nTotal++;
            fSum += ts;
            fSum_2 += (ts * ts);
        }

        public void Calculate() {
            if (nTotal > 2)
            {
                int N = nTotal - 1;
                fAvg = fSum / nTotal;
                fStddev = Mathf.Sqrt(fSum_2 / N - fAvg * fSum / N);
            }
        }
    }

    public class InitConnectData
    {
        public InitConnectData() { }
        public InitConnectData(string _userID, string _mapName, bool _bMapping, bool _bGyro, bool _bManager, 
            float _fx, float _fy, float _cx, float _cy, 
            float _d1, float _d2, float _d3, float _d4, int _w, int _h)
        {
            userID = _userID;
            mapName = _mapName;
            bMapping = _bMapping;
            bGyro = _bGyro;
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
            type1 = "device";
            type2 = "raw";
            //생성할 키워드
            keyword = "Image,Gyro,Accelerometer,DevicePosition,DeviceConnect,DeviceDisconnect,ContentGeneration,Map";
            src = userID;
        }
        public string type1, type2, keyword, src;
        public string userID, mapName;
        public float fx, fy, cx, cy;
        public float d1, d2, d3, d4;
        public int w, h;
        public bool bMapping, bGyro, bManager;
    }
    /// <summary>
    /// 알림 서버에 내가 받을 키워드를 알림
    /// UdpConnect와 UdpDisconnect 참조
    /// 모든 키워드를 받을지 아니면 특정 아이디만 받을지 선택이 가능함. 근데 이것을 없앨까 생각중.
    /// </summary>
     
    public class EchoData {
        public EchoData() { }
        public EchoData(string _key, string _type, string _src)
        {
            keyword = _key;
            type1 = _type;
            src = _src;
        }
        public string keyword, type1, type2, src;
        public byte[] data;
        public int id, id2;
    }

    public InitConnectData GetConnectData()
    {
        return new InitConnectData(strUserID, strMapName, bMapping, bGyro, bManagerMode, fx, fy, cx, cy, d1, d2, d3, d4, w, h);
    }

    private static float[] fdata;
    public float[] IntrinsicData {
        get
        {
            return fdata;
        } 
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
    private static int numSkipFrame;
    public int NumSkipFrame
    {
        get
        {
            return numSkipFrame;
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
    public bool IsServerMapping
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

    static bool bDeviceTracking;
    public bool IsDeviceTracking
    {
        get
        {
            return bDeviceTracking;
        }
        set
        {
            bDeviceTracking = value;
        }
    }
    static int nSensorSpeed = 0;
    public int SensorSpeed
    {
        get
        {
            return nSensorSpeed;
        }
        set
        {
            nSensorSpeed = value;
        }
    }
    static bool bGyro, bAcc;
    public bool UseAccelerometer
    {
        get
        {
            return bAcc;
        }
        set
        {
            bAcc = value;
        }
    }
    public bool UseGyro
    {
        get
        {
            return bGyro;
        }
        set
        {
            bGyro = value;
        }
    }


    static bool bManagerMode = false;
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

    public byte[] strBytes;
    public string strVocName;

    public ProcessTime ReferenceTime, TrackingTime, ContentGenerationTime, JpegTime;

    static private SystemManager m_pInstance = null;
    static public SystemManager Instance
    {
        get{
            if (m_pInstance == null)
            {
                m_pInstance = new SystemManager();
                //LoadParameter();

                try
                {
                    string strAddData = File.ReadAllText(Application.persistentDataPath + "/AppData.json");
                    AppData appData = JsonUtility.FromJson<AppData>(strAddData);
                    bMapping = appData.bMapping;
                    bDeviceTracking = appData.bTracking;
                    bGyro = appData.bGyro;
                    bAcc = appData.bAcc;
                }
                catch (FileNotFoundException fe)
                {
                    AppData appData = new AppData();
                    appData.bMapping = false;
                    appData.bTracking = true;
                    appData.bGyro = false;
                    appData.bAcc = false;
                    File.WriteAllText(Application.persistentDataPath + "/AppData.json", JsonUtility.ToJson(appData));
                }

            }
            return m_pInstance;
        }
    }
    
    public void LoadParameter(string path)
    {
        string[] paramText = File.ReadAllLines(path);
        int nUserData = 0;
        /*
        어드레스와 파일 위치만 남기고 나머지는 다 다른곳으로
        --------
        아이디
        어드레스
        맵로드, 리셋 삭제
        맵네임
        매핑
        프레임스킵
         */
        
        serveraddr = (paramText[nUserData++].Split('=')[1]);
        string datafile = (paramText[nUserData++].Split('=')[1]);

        string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기
        int numLine = 0;

        strUserID = (dataText[numLine++].Split('=')[1]);
        strMapName = (dataText[numLine++].Split('=')[1]);
        bMapping = Convert.ToBoolean(dataText[numLine++].Split('=')[1]);
        bDeviceTracking = Convert.ToBoolean(dataText[numLine++].Split('=')[1]);
        numSkipFrame = Convert.ToInt32(dataText[numLine++].Split('=')[1]);

        if (datafile == "/File/cam.txt")
        {
            bCam = true;
            //numLine = 2;
        }
        //if (!bCam)
        {
            //string imgFileTxt = Application.persistentDataPath + Convert.ToString(dataText[numLine++].Split('=')[1]);
            //imgFileLIst = File.ReadAllLines(imgFileTxt);
            
            //nMaxImageIndex = mSystem.imageData.Length - 1;
            imgPathTxt = Convert.ToString(dataText[numLine++].Split('=')[1]);
#if(UNITY_EDITOR_WIN)
            //Debug.Log(imgFileLIst.Length - 1);
            //Debug.Log("Load Datase = " + (imgFileLIst.Length - 3));
            Debug.Log(imgPathTxt);
#elif(UNITY_ANDROID)
            imgPathTxt = Application.persistentDataPath + imgPathTxt;
#endif
        }
        
        //reference
        try
        {
            string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/reference.json");
            ReferenceTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        }
        catch (FileNotFoundException fe)
        {
            ProcessTime appData = new ProcessTime();
            appData.nTotal = 0;
            appData.fSum = 0.0f;
            appData.fSum_2 = 0.0f;
            File.WriteAllText(Application.persistentDataPath + "/Time/reference.json", JsonUtility.ToJson(appData));
        }
        //tracking
        try
        {
            string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/tracking.json");
            TrackingTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        }
        catch (FileNotFoundException fe)
        {
            ProcessTime appData = new ProcessTime();
            appData.nTotal = 0;
            appData.fSum = 0.0f;
            appData.fSum_2 = 0.0f;
            File.WriteAllText(Application.persistentDataPath + "/Time/tracking.json", JsonUtility.ToJson(appData));
        }
        //content generation
        try
        {
            string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/content.json");
            ContentGenerationTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        }
        catch (FileNotFoundException fe)
        {
            ProcessTime appData = new ProcessTime();
            appData.nTotal = 0;
            appData.fSum = 0.0f;
            appData.fSum_2 = 0.0f;
            File.WriteAllText(Application.persistentDataPath + "/Time/content.json", JsonUtility.ToJson(appData));
        }
        //jpeg
        try
        {
            string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/jpeg.json");
            JpegTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        }
        catch (FileNotFoundException fe)
        {
            ProcessTime appData = new ProcessTime();
            appData.nTotal = 0;
            appData.nTotalSize = 0;
            appData.fSum = 0.0f;
            appData.fSum_2 = 0.0f;
            File.WriteAllText(Application.persistentDataPath + "/Time/jpeg.json", JsonUtility.ToJson(appData));
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

        fdata = new float[10];
        int nidx = 0;
        fdata[nidx++] = (float)w;
        fdata[nidx++] = (float)h;
        fdata[nidx++] = fx;
        fdata[nidx++] = fy;
        fdata[nidx++] = cx;
        fdata[nidx++] = cy;
        fdata[nidx++] = d1;
        fdata[nidx++] = d2;
        fdata[nidx++] = d3;
        fdata[nidx++] = d4;

        k = new Matrix3x3(fx, 0f, cx, 0f, fy, cy, 0f, 0f, 1f);
#if (UNITY_EDITOR_WIN)
        strVocName = Application.persistentDataPath + "/orbvoc.dbow3";
        strBytes=System.Text.Encoding.ASCII.GetBytes(strVocName);
#elif (UNITY_ANDROID)
        strVocName = Application.persistentDataPath + "/orbvoc.dbow3";
        strBytes=System.Text.Encoding.ASCII.GetBytes(strVocName);
#endif
    }

    
}
