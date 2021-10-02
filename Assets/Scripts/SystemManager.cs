using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SystemManager {

    /// <summary>
    /// 파사드 서버에 내가 생성할 키워드를 알림.
    /// </summary>
    [Serializable]
    public class CameraParams
    {
        public string name;
        public float fx;
        public float fy;
        public float cx;
        public float cy;
        public float d1;
        public float d2;
        public float d3;
        public float d4;
        public float w;
        public float h;
    }

    [Serializable]
    public class UserData
    {
        public int numCameraParam;
        public int numDataset;
        public string UserName;
        public string MapName;
        public bool ModeMapping;
        public bool ModeTracking;
        public bool UseCamera;
        public bool UseGyro;
        public bool UseAccelerometer;
    }

    [Serializable]
    public class ApplicationData {
        public string Address;
        public string UdpAddres;
        public int UdpPort;
        public int LocalPort;
        public int numPyramids;
        public int numFeatures;
        public int numSkipFrames;
        public int numLocalMapPoints;
        public int numLocalKeyFrames;
        public string strBoW_database;
    }

    [Serializable]
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
        public InitConnectData(string _userID, string _mapName, bool _bMapping, bool _bGyro, bool _bManager, bool _bDeviceTracking,
            float _fx, float _fy, float _cx, float _cy,
            float _d1, float _d2, float _d3, float _d4, int _w, int _h)
        {
            userID = _userID;
            mapName = _mapName;
            bMapping = _bMapping;
            bDeviceTracking = _bDeviceTracking;
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
        public bool bMapping, bGyro, bManager, bDeviceTracking;
    }
    /// <summary>
    /// 알림 서버에 내가 받을 키워드를 알림
    /// UdpConnect와 UdpDisconnect 참조
    /// 모든 키워드를 받을지 아니면 특정 아이디만 받을지 선택이 가능함. 근데 이것을 없앨까 생각중.
    /// </summary>

    //public class EchoData {
    //    public EchoData() { }
    //    public EchoData(string _key, string _type, string _src)
    //    {
    //        keyword = _key;
    //        type1 = _type;
    //        src = _src;
    //    }
    //    public string keyword, type1, type2, src;
    //    public byte[] data;
    //    public int id, id2;
    //    public DateTime sendedTime, receivedTime;
    //}

    public InitConnectData GetConnectData()
    {
        CameraParams camParam = camParams[userData.numCameraParam];
        return new InitConnectData(userData.UserName, userData.MapName, userData.ModeMapping, userData.UseGyro, bManagerMode, userData.ModeTracking, camParam.fx, camParam.fy, camParam.cx, camParam.cy, camParam.d1, camParam.d2, camParam.d3, camParam.d4, (int)camParam.w, (int)camParam.h);
    }

    private static float[] fdata;
    public float[] IntrinsicData {
        get
        {
            
            int nidx = 0;
            CameraParams camParam = camParams[userData.numCameraParam];
            fdata[nidx++] = (float)camParam.w;
            fdata[nidx++] = (float)camParam.h;
            fdata[nidx++] = camParam.fx;
            fdata[nidx++] = camParam.fy;
            fdata[nidx++] = camParam.cx;
            fdata[nidx++] = camParam.cy;
            fdata[nidx++] = camParam.d1;
            fdata[nidx++] = camParam.d2;
            fdata[nidx++] = camParam.d3;
            fdata[nidx++] = camParam.d4;
            return fdata;
        }
    }
    
    public int ImageWidth
    {
        get
        {
            return (int)camParams[userData.numCameraParam].w;
        }
    }
    public int ImageHeight
    {
        get
        {
            return (int)camParams[userData.numCameraParam].h;
        }
    }
    public float FocalLengthX
    {
        get
        {
            return camParams[userData.numCameraParam].fx;
        }
    }
    public float FocalLengthY
    {
        get
        {
            return camParams[userData.numCameraParam].fy;
        }
    }
    public float PrincipalPointX
    {
        get
        {
            return camParams[userData.numCameraParam].cx;
        }
    }
    public float PrincipalPointY
    {
        get
        {
            return camParams[userData.numCameraParam].cy;
        }
    }

    static private Matrix3x3 k;
    
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
    
    public int NumSkipFrame
    {
        get
        {
            return appData.numSkipFrames;
        }
    }
    
    public string UserName
    {
        get
        {
            return userData.UserName;
        }
        
    }
    public string MapName
    {
        get
        {
            return userData.MapName;
        }
    }
    public string ServerAddr
    {
        get
        {
            return appData.Address;
        }
    }

    public string ImagePath
    {
        get
        {
            return path+datalists[userData.numDataset];
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

    
    public bool Cam
    {
        get
        {
            return userData.UseCamera;
        }
    }
    
    public bool IsServerMapping
    {
        get
        {
            return userData.ModeMapping;
        }
    }

    public bool IsDeviceTracking
    {
        get
        {
            return userData.ModeTracking;
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
    
    static bool bAcc = false;
    public bool UseAccelerometer
    {
        get
        {
            return bAcc;
        }
    }
    public bool UseGyro
    {
        get
        {
            return userData.UseGyro;
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

    static private string[] datalists;
    public String[] DataLists
    {
        get {
            return datalists;
        }
    }

    public CameraParams[] Cameras
    {
        get
        {
            return camParams;
        }
    }
    static private CameraParams[] camParams;

    public UserData User
    {
        get
        {
            return userData;
        }
    }
    static private UserData userData;

    public CameraParams Camera
    {
        get {
            return camParams[userData.numCameraParam]; ;
        }
    }

    static private string path;
    public string Path
    {
        get
        {
            return path;
        }
    }

    static private ApplicationData appData;
     public ApplicationData AppData
    {
        get
        {
            return appData;
        }
    }

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
                    string strIntrinsics = File.ReadAllText(Application.persistentDataPath + "/Data/CameraIntrinsics.json");
                    camParams = JsonHelper.FromJson<CameraParams>(strIntrinsics);
                }
                catch(FileNotFoundException)
                {
                    camParams = new CameraParams[7];
                    int idx = 0;
                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "S21+_Camera";
                    camParams[idx].fx = 476.6926f;
                    camParams[idx].fy = 485.7888f;
                    camParams[idx].cx = 328.5845f;
                    camParams[idx].cy = 172.9118f;
                    camParams[idx].d1 = 0.0919f;
                    camParams[idx].d2 = -0.0314f;
                    camParams[idx].d3 = -0.0242f;
                    camParams[idx].d4 = 0.0023f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "S21+_Image";
                    camParams[idx].fx = 541.8811f;
                    camParams[idx].fy = 556.2014f;
                    camParams[idx].cx = 323.0700f;
                    camParams[idx].cy = 208.3839f;
                    camParams[idx].d1 = -0.0377f;
                    camParams[idx].d2 = 0.1729f;
                    camParams[idx].d3 = -0.0169f;
                    camParams[idx].d4 = -0.0019f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "NOTE8_Camera";
                    camParams[idx].fx = 2.0f;
                    camParams[idx].fy = 2.0f;
                    camParams[idx].cx = 2.0f;
                    camParams[idx].cy = 2.0f;
                    camParams[idx].d1 = 1.0f;
                    camParams[idx].d2 = 1.0f;
                    camParams[idx].d3 = 1.0f;
                    camParams[idx].d4 = 1.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "NOTE8_Image";
                    camParams[idx].fx = 599.5733f;
                    camParams[idx].fy = 610.5514f;
                    camParams[idx].cx = 337.6243f;
                    camParams[idx].cy = 176.1959f;
                    camParams[idx].d1 = 0.1940f;
                    camParams[idx].d2 = -0.4056f;
                    camParams[idx].d3 = -0.0190f;
                    camParams[idx].d4 = 0.0013f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "HOLOLENS2_Camera";
                    camParams[idx].fx = 2.0f;
                    camParams[idx].fy = 2.0f;
                    camParams[idx].cx = 2.0f;
                    camParams[idx].cy = 2.0f;
                    camParams[idx].d1 = 1.0f;
                    camParams[idx].d2 = 1.0f;
                    camParams[idx].d3 = 1.0f;
                    camParams[idx].d4 = 1.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "HOLOLENS2_Image";
                    camParams[idx].fx = 2.0f;
                    camParams[idx].fy = 2.0f;
                    camParams[idx].cx = 2.0f;
                    camParams[idx].cy = 2.0f;
                    camParams[idx].d1 = 1.0f;
                    camParams[idx].d2 = 1.0f;
                    camParams[idx].d3 = 1.0f;
                    camParams[idx].d4 = 1.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "Senz3D";
                    camParams[idx].fx = 2.0f;
                    camParams[idx].fy = 2.0f;
                    camParams[idx].cx = 2.0f;
                    camParams[idx].cy = 2.0f;
                    camParams[idx].d1 = 1.0f;
                    camParams[idx].d2 = 1.0f;
                    camParams[idx].d3 = 1.0f;
                    camParams[idx].d4 = 1.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 480f;
                    idx++;

                    string camJsonStr = JsonHelper.ToJson(camParams, true);
                    File.WriteAllText(Application.persistentDataPath + "/Data/CameraIntrinsics.json", camJsonStr);
                }
                
#if UNITY_EDITOR_WIN
                path = "E:/SLAM_DATASET/MY";
#elif UNITY_ANDROID
                path = Application.persistentDataPath+"/File";
#endif

                try
                {
                    datalists =  File.ReadAllLines(Application.persistentDataPath + "/Data/DataLists.json");
                    //for (int i = 0; i < datalists.Length; i++)
                    //    datalists[i] = path + datalists[i];
                }
                catch(FileNotFoundException)
                {
                    datalists = new string[3];
                    datalists[0] = "/KI/S21+/1/";
                    datalists[1] = "/KI/S21+/5/";
                    datalists[2] = "/KI/NOTE8/1/";
                    File.WriteAllLines(Application.persistentDataPath + "/Data/DataLists.json", datalists);
                }

                try
                {
                    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Data/UserData.json");
                    userData = JsonUtility.FromJson<UserData>(strAddData);
                    
                    
                    //////
                    //bMapping = appData.bMapping;
                    //bDeviceTracking = appData.bTracking;
                    //bGyro = appData.bGyro;
                    //bAcc = appData.bAcc;

                }
                catch(FileNotFoundException)
                {
                    userData = new UserData();
                    userData.numCameraParam = 1;
                    userData.numDataset = 1;
                    userData.UserName = "zkrkwlek";
                    userData.MapName = "TestMap";
                    File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(userData));
                }

                try
                {
                    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Data/AppData.json");
                    appData = JsonUtility.FromJson<ApplicationData>(strAddData);
                    
                }
                catch (FileNotFoundException)
                {
                    appData = new ApplicationData();
                    appData.Address = "http://143.248.6.143:35005";
                    appData.UdpAddres = "143.248.6.143";
                    appData.UdpPort = 35001;
                    appData.LocalPort = 40003;
                    appData.numSkipFrames = 3;
                    appData.numPyramids = 4;
                    appData.numFeatures = 800;
                    appData.numLocalMapPoints = 600;
                    appData.numLocalKeyFrames = 50;
                    File.WriteAllText(Application.persistentDataPath + "/Data/AppData.json", JsonUtility.ToJson(appData));
                }
                fdata = new float[10];

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
        
        /*
        serveraddr = (paramText[nUserData++].Split('=')[1]);
        string datafile = (paramText[nUserData++].Split('=')[1]);

        string[] dataText = File.ReadAllLines(Application.persistentDataPath + datafile); //데이터 읽기
        int numLine = 0;

        strUserID = (dataText[numLine++].Split('=')[1]);
        strMapName = (dataText[numLine++].Split('=')[1]);
        bool aMapping = Convert.ToBoolean(dataText[numLine++].Split('=')[1]);
        bool aDeviceTracking = Convert.ToBoolean(dataText[numLine++].Split('=')[1]);
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
#if (UNITY_EDITOR_WIN)
            //Debug.Log(imgFileLIst.Length - 1);
            //Debug.Log("Load Datase = " + (imgFileLIst.Length - 3));
            Debug.Log(imgPathTxt);
#elif (UNITY_ANDROID)
            imgPathTxt = Application.persistentDataPath + imgPathTxt;
#endif
        }
        */

        //reference
        try
        {
            string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/reference.json");
            ReferenceTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        }
        catch (FileNotFoundException)
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
        catch (FileNotFoundException)
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
        catch (FileNotFoundException)
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
        catch (FileNotFoundException)
        {
            ProcessTime appData = new ProcessTime();
            appData.nTotal = 0;
            appData.nTotalSize = 0;
            appData.fSum = 0.0f;
            appData.fSum_2 = 0.0f;
            File.WriteAllText(Application.persistentDataPath + "/Time/jpeg.json", JsonUtility.ToJson(appData));
        }

//        fx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        fy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        cx = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        cy = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        w = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
//        h = Convert.ToInt32(dataText[numLine++].Split('=')[1]);
//        d1 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        d2 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        d3 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);
//        d4 = Convert.ToSingle(dataText[numLine++].Split('=')[1]);

//        fdata = new float[10];
//        int nidx = 0;
//        fdata[nidx++] = (float)w;
//        fdata[nidx++] = (float)h;
//        fdata[nidx++] = fx;
//        fdata[nidx++] = fy;
//        fdata[nidx++] = cx;
//        fdata[nidx++] = cy;
//        fdata[nidx++] = d1;
//        fdata[nidx++] = d2;
//        fdata[nidx++] = d3;
//        fdata[nidx++] = d4;

//        k = new Matrix3x3(fx, 0f, cx, 0f, fy, cy, 0f, 0f, 1f);
//#if (UNITY_EDITOR_WIN)
//        strVocName = Application.persistentDataPath + "/orbvoc.dbow3";
//        strBytes=System.Text.Encoding.ASCII.GetBytes(strVocName);
//#elif (UNITY_ANDROID)
//        strVocName = Application.persistentDataPath + "/orbvoc.dbow3";
//        strBytes=System.Text.Encoding.ASCII.GetBytes(strVocName);
//#endif
    }

    
}
