using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class TestGUI : MonoBehaviour
{

#if UNITY_EDITOR_WIN
    [DllImport("UnityLibrary")]
    private static extern void SetPath(byte[] name, int len);
    [DllImport("UnityLibrary")]
    private static extern void LoadVocabulary();
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetPath(char[] path);
    [DllImport("edgeslam")]
    private static extern void LoadVocabulary();
#endif

    public InputField ifUser;
    public InputField ifMap;
    public InputField ifJPEG;

    public RawImage ResultImage;
    public Dropdown drop;
    public Dropdown drop2;
    public Dropdown drop3;

    GameObject Canvas;
    CanvasScaler Scaler;

    public Button btnConnect;
    public Button btnSend;
    public Button btnOption;

    public Toggle toggleCam, toggleIMU, toggleMapping, toggleTracking, toggleTest;

    // Start is called before the first frame update
    void Start()
    {
        Canvas = GameObject.Find("Canvas");
        Scaler = Canvas.GetComponentInChildren<CanvasScaler>();
        Scaler.matchWidthOrHeight = 1f;
        Scaler.referenceResolution = new Vector2(Screen.width, Screen.height);

        AspectRatioFitter fitter = GameObject.Find("background").GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = ((float)SystemManager.Instance.ImageWidth) / SystemManager.Instance.ImageHeight;

#if (UNITY_EDITOR_WIN)
        byte[] b = System.Text.Encoding.ASCII.GetBytes(Application.persistentDataPath);
        SetPath(b, b.Length);
        //SystemManager.Instance.strBytes, SystemManager.Instance.strBytes.Length, 
#elif (UNITY_ANDROID)
        SetPath(Application.persistentDataPath.ToCharArray());    
#endif
        LoadVocabulary();

        /////Initialize
        drop.options.Clear();
        drop2.options.Clear();
        drop3.options.Clear();
        foreach (SystemManager.CameraParams cam in SystemManager.Instance.Cameras)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = cam.name;
            drop.options.Add(data);
        }

        foreach (string dataset in SystemManager.Instance.DataLists)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = dataset;
            drop2.options.Add(data);
        }

        foreach (string filename in SystemManager.Instance.FileLists)
        {
            Dropdown.OptionData data = new Dropdown.OptionData();
            data.text = filename;
            drop3.options.Add(data);
        }

        drop.value = SystemManager.Instance.User.numCameraParam;
        drop2.value = SystemManager.Instance.User.numDataset;
        drop3.value = SystemManager.Instance.User.numDatasetFileName;

        toggleCam.isOn = SystemManager.Instance.User.UseCamera;
        toggleIMU.isOn = SystemManager.Instance.User.UseGyro;
        toggleMapping.isOn = SystemManager.Instance.User.ModeMapping;
        toggleTracking.isOn = SystemManager.Instance.User.ModeTracking;
        toggleTest.isOn = SystemManager.Instance.User.ModeMultiAgentTest;

        SetUI();
        /////Initialize

        ////Add Listener
        ////Input Field
        ifUser.text = SystemManager.Instance.User.UserName;
        ifUser.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.UserName = ifUser.text;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        ifMap.text = SystemManager.Instance.User.MapName;
        ifMap.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.MapName = ifMap.text;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        ifJPEG.text = Convert.ToString(SystemManager.Instance.AppData.JpegQuality);
        ifJPEG.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.JpegQuality = Convert.ToInt32(ifJPEG.text);
            File.WriteAllText(Application.persistentDataPath + "/Data/AppData.json", JsonUtility.ToJson(SystemManager.Instance.AppData));
        });

        drop.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numCameraParam = drop.value;
            SetUI();
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        drop2.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numDataset = drop2.value;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        drop3.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numDatasetFileName = drop3.value;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });

        toggleCam.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.UseCamera = toggleCam.isOn;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        toggleIMU.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.UseGyro = toggleIMU.isOn;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        toggleMapping.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeMapping = toggleMapping.isOn;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        toggleTracking.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeTracking = toggleTracking.isOn;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });
        toggleTest.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeMultiAgentTest = toggleTest.isOn;
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
        });

        ////Connect & disconnect
        btnConnect.onClick.AddListener(delegate {
            bool bConnect = !SystemManager.Instance.Connect;
            if (bConnect)
            {
                btnConnect.GetComponentInChildren<Text>().text = "Disconnect";
                SystemManager.Instance.Connect = true;
                ////
                //개별 컨트롤러 연결 처리
                Tracker.Instance.Connect();
                ////
                SystemManager.ApplicationData appData = SystemManager.Instance.AppData;
                UdpAsyncHandler.Instance.UdpSocketBegin(appData.UdpAddres, appData.UdpPort, appData.LocalPort);
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "MappingResult", "connect", "single");
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "LocalMap", "connect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "ReferenceFrame", "connect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "ObjectDetection", "connect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "Content", "connect", "all");

            }
            else
            {
                btnConnect.GetComponentInChildren<Text>().text = "Connect";
                SystemManager.Instance.Connect = false;
                ////
                //개별 컨트롤러 해제 처리
                Tracker.Instance.Disconnect();
                ////
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "ReferenceFrame", "disconnect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "ObjectDetection", "disconnect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "Content", "disconnect", "all");
                UdpAsyncHandler.Instance.UdpSocketClose();
                File.WriteAllLines(Application.persistentDataPath + "/Data/Trajectory.txt", SystemManager.Instance.Trajectory);
            }
                        
            Dictionary<string, SystemManager.ExperimentData>.ValueCollection values = SystemManager.Instance.Experiments.Values;
            SystemManager.ExperimentData[] datas = new SystemManager.ExperimentData[values.Count];
            int idx = 0;
            foreach(SystemManager.ExperimentData data in values)
            {
                data.Calculate();
                datas[idx++] = data;
            }
            File.WriteAllText(Application.persistentDataPath + "/Data/Experiment.json", JsonHelper.ToJson(datas, true));
            
        });

        btnSend.onClick.AddListener(delegate {
            bool bStart = !SystemManager.Instance.Start;
            if (bStart)
            {
                btnSend.GetComponentInChildren<Text>().text = "Stop";
                SystemManager.Instance.Start = true;
            }
            else
            {
                btnSend.GetComponentInChildren<Text>().text = "Start";
                SystemManager.Instance.Start = false;
            }
            
        });
    }

    void SetUI()
    {
        SystemManager.CameraParams cam = SystemManager.Instance.Cameras[SystemManager.Instance.User.numCameraParam];
        float Scale = ((float)Screen.height) / cam.h;
        float Width = (cam.w * Scale);
        float Height = (cam.h * Scale);
        float diff = (Screen.width - Width) * 0.5f;

        Tracker.Instance.BackgroundRect = new Rect(diff, 0f, Width, Height);
        Tracker.Instance.Scale = Scale;
        Tracker.Instance.Diff = new Vector2(diff, Height);

        RectTransform rt0 = ResultImage.GetComponent<RectTransform>();
        rt0.anchoredPosition = new Vector3(-Width/2f+25f,Height/2f-25f,0f);
        ResultImage.color = new Color(0f, 1.0f, 0f, 0.3f);

        RectTransform rtDrop1 = drop.GetComponent<RectTransform>();
        float offset = rtDrop1.sizeDelta.x / 2f+30f;
        
        RectTransform rtBtn1 = btnConnect.GetComponent<RectTransform>();
        RectTransform rtBtn2 = btnSend.GetComponent<RectTransform>();
        RectTransform rtBtn3 = btnOption.GetComponent<RectTransform>();

        float w, w2, toggleHeight, margin;
#if UNITY_EDITOR_WIN
        w = 200f;
        margin = 10;
        w2 = 80f;
        toggleHeight = 30f;
#elif UNITY_ANDROID
        w = 350f;//Width - 100f;
        margin = 20;
        w2 = 70f;
        toggleHeight = 60f;
#endif

        //rtDrop1.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + w2);
        //rtDrop2.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + w2);
        //rtDrop3.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + w2);

        /*
        rtToggle1.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + toggleHeight);
        rtToggle2.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + toggleHeight);
        rtToggle3.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + toggleHeight);
        rtToggle4.anchoredPosition = new Vector3(-Width / 2f - offset, w); w -= (margin + toggleHeight);
        rtToggle5.anchoredPosition = new Vector3(-Width / 2f - offset, w);
        */

        w = -Height/2f+100f;
        
#if UNITY_EDITOR_WIN
        w = 200f;
#elif UNITY_ANDROID
        w = 350f;//Width - 100f;
#endif

        rtBtn1.anchoredPosition = new Vector3(Width / 2f + offset, w); w -= (margin + w2);
        rtBtn2.anchoredPosition = new Vector3(Width / 2f + offset, w); w -= (margin + w2);
        rtBtn3.anchoredPosition = new Vector3(Width / 2f + offset, w);

        //rtDrop1.sizeDelta = new Vector2(200f, w2);
        //rtDrop2.sizeDelta = new Vector2(200f, w2);
        //rtDrop3.sizeDelta = new Vector2(200f, w2);
        
        rtBtn1.sizeDelta = new Vector2(200f, w2);
        rtBtn2.sizeDelta = new Vector2(200f, w2);
        rtBtn3.sizeDelta = new Vector2(200f, w2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //private int Function_Dropdown1(Dropdown select)
    //{
    //    //string op = select.options[select.value].text;
    //    //Debug.Log("Dropdown Change!\n" + select.value);
    //    //SystemManager.Instance.uu.numCameraParam = select.value;
    //    return select.value;
    //}

}
