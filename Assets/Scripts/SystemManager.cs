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
        public float d5;
        public float w;
        public float h;
    }

    [Serializable]
    public class UserData
    {
        public int numCameraParam;
        public int numDataset;
        public int numDatasetFileName;
        public string UserName;
        public string MapName;
        public string Keywords;
        public string Experiments;
        public bool ModeMapping;
        public bool ModeTracking;
        public bool ModeMultiAgentTest;
        public bool UseCamera;
        public bool UseGyro;
        public bool UseAccelerometer;
        public bool bSaveTrajectory;
        public bool bVisualizeFrame;
        //public bool bShowLog;
    }

    [Serializable]
    public class ApplicationData {
        public string Address;
        public string UdpAddres;
        public int UdpPort;
        public int LocalPort;
        public int JpegQuality;
        public int numPyramids;
        public int numFeatures;
        public int numSkipFrames;
        public int numLocalMapPoints;
        public int numLocalKeyFrames;
        public string strBoW_database;
    }

    [Serializable]
    public class ExperimentData
    {
        public string name;
        public List<ExperimentDataElement> datas;
        public Dictionary<string, ExperimentDataElement> datas2;
        public ExperimentData(string _name)
        {
            name = _name;
            datas = new List<ExperimentDataElement>();
            datas.Add(new ExperimentDataElement("latency"));
            datas.Add(new ExperimentDataElement("traffic"));
            datas.Add(new ExperimentDataElement("download"));
        }

        public void Add(string s)
        {
            if(!datas2.ContainsKey(s))
                datas2.Add(s, new ExperimentDataElement(s));
        }
        public void Remove(string s)
        {
            if (datas2.ContainsKey(s))
                datas2.Remove(s);
        }

        public void Update(string s, float f)
        {
            datas2[s].Update(f);
        }

        public void Init()
        {
            datas2 = new Dictionary<string, ExperimentDataElement>();
            for(int i =0; i < datas.Count; i++)
            {
                datas2.Add(datas[i].name, datas[i]);
            }
        }

        public void Update()
        {
            datas = new List<ExperimentDataElement>();
            Dictionary<string, ExperimentDataElement>.ValueCollection values = datas2.Values;
            foreach(ExperimentDataElement data in values)
            {
                data.Calculate();
                datas.Add(data);
            }
        }
    }

    [Serializable]
    public class ExperimentDataElement
    {
        public string name;
        public int nTotal;
        public float fSum;
        public float fSum_2;
        public float fAvg;
        public float fStddev;

        public ExperimentDataElement(string _name)
        {
            name = _name;
            nTotal = 0;
            fSum = 0.0f;
            fSum_2 = 0.0f;
            fAvg = 0.0f;
            fStddev = 0.0f;
        }

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
            type2 = "NONE";
            //생성할 키워드
            keyword = "Image,Gyro,Accelerometer,DeviceConnect,DeviceDisconnect,ContentGeneration,DevicePosition";//,Map,
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
            fdata = new float[13];
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
            fdata[nidx++] = camParam.d5;
            fdata[nidx++] = appData.JpegQuality;
            fdata[nidx++] = appData.numSkipFrames;
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

    public string ImagePath
    {
        get
        {
            return path + datalists[userData.numDataset];
        }
    }
    public string DataFile
    {
        get{
            return filelists[userData.numDatasetFileName];
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
    static private string[] mapnamelist;
    public string[] MapNameList
    {
        get
        {
            return mapnamelist;
        }
    }

    static private string[] datalists;
    public String[] DataLists
    {
        get {
            return datalists;
        }
    }

    static private string[] filelists;
    public String[] FileLists
    {
        get
        {
            return filelists;
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

    public Dictionary<string, ExperimentData> Experiments
    {
        get
        {
            return exDatas;
        }

    }
    static private Dictionary<string, ExperimentData> exDatas;

    //public ExperimentData[] Experiments
    //{
    //    get
    //    {
    //        return exDatas;
    //    }
    //    set
    //    {
    //        exDatas = value;
    //    }
    //}
    //static private ExperimentData[] exDatas;

    public List<string> Trajectory
    {
        get
        {
            return trajectory;
        }
        set 
        {
            trajectory = value;
        }
    }
    static private List<string> trajectory;

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

    //public ProcessTime ReferenceTime, TrackingTime, ContentGenerationTime, JpegTime;

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
                    Debug.Log(Application.persistentDataPath);
                    string strIntrinsics = File.ReadAllText(Application.persistentDataPath + "/Data/CameraIntrinsics.json");
                    camParams = JsonHelper.FromJson<CameraParams>(strIntrinsics);
                }
                catch(FileNotFoundException)
                {
                    int nTotalCam = 13;
                    camParams = new CameraParams[nTotalCam];
                    int idx = 0;
                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "S20+_Camera_Portrait";
                    camParams[idx].fx = 476.6926f;
                    camParams[idx].fy = 485.7888f;
                    camParams[idx].cx = 328.5845f;
                    camParams[idx].cy = 172.9118f;
                    camParams[idx].d1 = 0.0919f;
                    camParams[idx].d2 = -0.0314f;
                    camParams[idx].d3 = -0.0242f;
                    camParams[idx].d4 = 0.0023f;
                    camParams[idx].d5 = 0.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "S20+_Camera_Landscape";
                    camParams[idx].fx = 442.6716f;
                    camParams[idx].fy = 466.0769f;
                    camParams[idx].cx = 323.6599f;
                    camParams[idx].cy = 231.4053f;
                    camParams[idx].d1 = 0.0437f;
                    camParams[idx].d2 = 0.0421f;
                    camParams[idx].d3 = -0.0050f;
                    camParams[idx].d4 = 0.0084f;
                    camParams[idx].d5 = 0.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 360f;
                    idx++;

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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
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
                    camParams[idx].d5 = 0.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 480f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "TUM3";
                    camParams[idx].fx = 535.4f;
                    camParams[idx].fy = 539.2f;
                    camParams[idx].cx = 320.1f;
                    camParams[idx].cy = 247.6f;
                    camParams[idx].d1 = 0.0f;
                    camParams[idx].d2 = 0.0f;
                    camParams[idx].d3 = 0.0f;
                    camParams[idx].d4 = 0.0f;
                    camParams[idx].d5 = 0.0f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 480f;
                    idx++;


                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "TUM2";
                    camParams[idx].fx = 520.908620f;
                    camParams[idx].fy = 521.007327f;
                    camParams[idx].cx = 325.141442f;
                    camParams[idx].cy = 249.701764f;
                    camParams[idx].d1 = 0.231222f;
                    camParams[idx].d2 = -0.784899f;
                    camParams[idx].d3 = -0.003257f;
                    camParams[idx].d4 = -0.000105f;
                    camParams[idx].d5 = 0.917205f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 480f;
                    idx++;

                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "TUM1";
                    camParams[idx].fx = 517.306408f;
                    camParams[idx].fy = 516.469215f;
                    camParams[idx].cx = 318.643040f;
                    camParams[idx].cy = 255.313989f;
                    camParams[idx].d1 = 0.262383f;
                    camParams[idx].d2 = -0.953104f;
                    camParams[idx].d3 = -0.005358f;
                    camParams[idx].d4 = 0.002628f;
                    camParams[idx].d5 = 1.163314f;
                    camParams[idx].w = 640f;
                    camParams[idx].h = 480f;
                    idx++;


                    camParams[idx] = new CameraParams();
                    camParams[idx].name = "ICL_NUIM";
                    camParams[idx].fx = 481.20f;
                    camParams[idx].fy = -480.00f;
                    camParams[idx].cx = 319.50f;
                    camParams[idx].cy = 239.5f;
                    camParams[idx].d1 = 0.0f;
                    camParams[idx].d2 = 0.0f;
                    camParams[idx].d3 = 0.0f;
                    camParams[idx].d4 = 0.0f;
                    camParams[idx].d5 = 0.0f;
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

                string[] templist;
                try
                {
                    templist = File.ReadAllLines(Application.persistentDataPath + "/Data/DataLists.json");
                    datalists = new string[templist.Length];
                    mapnamelist = new string[templist.Length];
                    for(int i = 0; i < templist.Length; i++)
                    {
                        string[] strs = templist[i].Split(',');
                        mapnamelist[i] = strs[0];
                        datalists[i] = strs[1];
                    }
                }
                catch (FileNotFoundException)
                {
                    int nDataList = 27;
                    templist = new string[nDataList];
                    int nIdx = 0;
                    templist[nIdx++] = "TEST,/KI/S21+/1/";
                    templist[nIdx++] = "TEST,/KI/S21+/5/";
                    templist[nIdx++] = "TEST,/KI/NOTE8/1/";
                    templist[nIdx++] = "tum1_desk,/TUM/TUM1/desk/";
                    templist[nIdx++] = "tum1_floor,/TUM/TUM1/floor/";
                    templist[nIdx++] = "tum1_room,/TUM/TUM1/room/";
                    templist[nIdx++] = "tum1_xyz,/TUM/TUM1/xyz/";
                    templist[nIdx++] = "tum2_xyz,/TUM/TUM2/xyz/";
                    templist[nIdx++] = "tum2_desk,/TUM/TUM2/desk/";
                    templist[nIdx++] = "tum2_desk_person,/TUM/TUM2/desk_with_person/";
                    templist[nIdx++] = "tum2_360,/TUM/TUM2/360/";
                    templist[nIdx++] = "tum2_no_loop,/TUM/TUM2/large_no_loop/";
                    templist[nIdx++] = "tum2_loop,/TUM/TUM2/large_with_loop/";
                    templist[nIdx++] = "tum3_office,/TUM/TUM3/long_office/";
                    templist[nIdx++] = "tum3_strtexfar,/TUM/TUM3/str_tex_far/";
                    templist[nIdx++] = "tum3_strtexnear,/TUM/TUM3/str_tex_near/";
                    templist[nIdx++] = "tum3_sitting_half,/TUM/TUM3/sitting_half/";
                    templist[nIdx++] = "tum3_walking_static,/TUM/TUM3/walking_static/";
                    templist[nIdx++] = "tum3_walking_xyz,/TUM/TUM3/walking_xyz/";
                    templist[nIdx++] = "lr0,/NUIM/lr0/";
                    templist[nIdx++] = "lr0n,/NUIM/lr0n/";
                    templist[nIdx++] = "lr1,/NUIM/lr1/";
                    templist[nIdx++] = "lr1n,/NUIM/lr1n/";
                    templist[nIdx++] = "lr2,/NUIM/lr2/";
                    templist[nIdx++] = "lr2n,/NUIM/lr2n/";
                    templist[nIdx++] = "lr3,/NUIM/lr3/";
                    templist[nIdx++] = "lr3n,/NUIM/lr3n/";

                    datalists = new string[templist.Length];
                    mapnamelist = new string[templist.Length];
                    for (int i = 0; i < templist.Length; i++)
                    {
                        string[] strs = templist[i].Split(',');
                        mapnamelist[i] = strs[0];
                        datalists[i] = strs[1];
                    }

                    File.WriteAllLines(Application.persistentDataPath + "/Data/DataLists.json", templist);
                }
                finally {
                    
                }

                try
                {
                    filelists = File.ReadAllLines(Application.persistentDataPath + "/Data/FileLists.json");
                }
                catch (FileNotFoundException)
                {
                    int nDataList = 9;
                    filelists = new string[nDataList];
                    int nIdx = 0;
                    filelists[nIdx++] = "rgb.txt";
                    filelists[nIdx++] = "test/scene1.txt";
                    filelists[nIdx++] = "test/scene2.txt";
                    filelists[nIdx++] = "test/scene3.txt";
                    filelists[nIdx++] = "test/scene4.txt";
                    filelists[nIdx++] = "test/scene5.txt";
                    filelists[nIdx++] = "test/scene6.txt";
                    filelists[nIdx++] = "test/scene7.txt";
                    filelists[nIdx++] = "test/scene8.txt";
                    File.WriteAllLines(Application.persistentDataPath + "/Data/FileLists.json", filelists);
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
                    userData.numCameraParam = 0;
                    userData.numDataset = 0;
                    userData.numDatasetFileName = 0;
                    userData.UserName = "zkrkwlek";
                    userData.MapName = "TestMap";
                    userData.Keywords = "ReferenceFrame,single,Content,all";
                    userData.Experiments = "ReferenceFrame,Tracking,Content,ObjectDetection,Segmentation";
                    userData.ModeMultiAgentTest = false;
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
                    appData.JpegQuality = 95;
                    appData.numSkipFrames = 3;
                    appData.numPyramids = 4;
                    appData.numFeatures = 800;
                    appData.numLocalMapPoints = 600;
                    appData.numLocalKeyFrames = 50;
                    File.WriteAllText(Application.persistentDataPath + "/Data/AppData.json", JsonUtility.ToJson(appData));
                }
                
                //try
                //{
                //    string strExperiments = File.ReadAllText(Application.persistentDataPath + "/Data/Experiment.json");
                //    ExperimentData[] temp = JsonHelper.FromJson<ExperimentData>(strExperiments);
                //    exDatas = new Dictionary<string, ExperimentData>();
                //    foreach (ExperimentData data in temp)
                //    {
                //        exDatas[data.name] = data;
                //    }
                //}
                //catch (FileNotFoundException) {

                //    //local map size, ref size, content time, local map time
                //    exDatas = new Dictionary<string, ExperimentData>();

                //    List<string> list = new List<string>();
                //    list.Add("TrackingTime");
                //    list.Add("FrameTime");
                //    list.Add("ReferenceFrameTime");
                //    list.Add("VisualizationTime");
                //    list.Add("ReferenceTraffic");
                //    list.Add("ReferenceReturnTime");
                //    list.Add("ObjectDetectionTime");
                //    list.Add("Segmentation");
                //    list.Add("ContentReturnTime");
                //    list.Add("UploadTimeImage");
                //    list.Add("UploadTimeGyro");
                //    list.Add("DownloadTimeImage");
                //    list.Add("DownloadTimeObject");

                //    foreach (string str in list)
                //    {
                //        ExperimentData data = new ExperimentData(str);
                //        data.nTotal = 0;
                //        data.fSum = 0.0f;
                //        data.fSum_2 = 0.0f;
                //        data.Calculate();
                //        exDatas[data.name] = data;
                //    }

                //    int idx = 0;
                //    Dictionary<string, SystemManager.ExperimentData>.ValueCollection values = exDatas.Values;
                //    ExperimentData[] temp = new ExperimentData[exDatas.Count];
                //    foreach(ExperimentData data in values)
                //    {
                //        temp[idx++] = data;
                //    }

                //    string camJsonStr = JsonHelper.ToJson(temp, true);
                //    File.WriteAllText(Application.persistentDataPath + "/Data/Experiment.json", camJsonStr);
                //}

                if (!Directory.Exists(Application.persistentDataPath + "/Experiment/"))
                {
                    Directory.CreateDirectory(Application.persistentDataPath + "/Experiment/");
                }
                string[] strExs = userData.Experiments.Split(',');
                string expath = Application.persistentDataPath + "/Experiment/";
                exDatas = new Dictionary<string, ExperimentData>(strExs.Length);
                foreach(string str in strExs)
                {
                    try
                    {
                        string strData = File.ReadAllText(expath + str + ".json");
                        ExperimentData ex = JsonUtility.FromJson<ExperimentData>(strData);
                        ex.Init();
                        exDatas.Add(str, ex);
                    }
                    catch (FileNotFoundException)
                    {
                        ExperimentData ex = new ExperimentData(str);
                        exDatas.Add(str, ex);
                        ex.Init();
                        File.WriteAllText(expath+str+".json", JsonUtility.ToJson(ex));
                    }
                }
                exDatas["Tracking"].Remove("latency");
                exDatas["Tracking"].Remove("traffic");
                exDatas["Tracking"].Remove("download");
                exDatas["Tracking"].Add("Frame");
                exDatas["Tracking"].Add("Tracking");
                exDatas["Tracking"].Add("Visualization");

                trajectory = new List<string>();
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
        //try
        //{
        //    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/reference.json");
        //    ReferenceTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        //}
        //catch (FileNotFoundException)
        //{
        //    ProcessTime appData = new ProcessTime();
        //    appData.nTotal = 0;
        //    appData.fSum = 0.0f;
        //    appData.fSum_2 = 0.0f;
        //    File.WriteAllText(Application.persistentDataPath + "/Time/reference.json", JsonUtility.ToJson(appData));
        //}
        ////tracking
        //try
        //{
        //    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/tracking.json");
        //    TrackingTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        //}
        //catch (FileNotFoundException)
        //{
        //    ProcessTime appData = new ProcessTime();
        //    appData.nTotal = 0;
        //    appData.fSum = 0.0f;
        //    appData.fSum_2 = 0.0f;
        //    File.WriteAllText(Application.persistentDataPath + "/Time/tracking.json", JsonUtility.ToJson(appData));
        //}
        ////content generation
        //try
        //{
        //    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/content.json");
        //    ContentGenerationTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        //}
        //catch (FileNotFoundException)
        //{
        //    ProcessTime appData = new ProcessTime();
        //    appData.nTotal = 0;
        //    appData.fSum = 0.0f;
        //    appData.fSum_2 = 0.0f;
        //    File.WriteAllText(Application.persistentDataPath + "/Time/content.json", JsonUtility.ToJson(appData));
        //}
        ////jpeg
        //try
        //{
        //    string strAddData = File.ReadAllText(Application.persistentDataPath + "/Time/jpeg.json");
        //    JpegTime = JsonUtility.FromJson<ProcessTime>(strAddData);
        //}
        //catch (FileNotFoundException)
        //{
        //    ProcessTime appData = new ProcessTime();
        //    appData.nTotal = 0;
        //    appData.nTotalSize = 0;
        //    appData.fSum = 0.0f;
        //    appData.fSum_2 = 0.0f;
        //    File.WriteAllText(Application.persistentDataPath + "/Time/jpeg.json", JsonUtility.ToJson(appData));
        //}

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
