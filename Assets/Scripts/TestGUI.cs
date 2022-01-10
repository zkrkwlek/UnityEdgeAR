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

    public InputField ifUser, ifMap, ifJPEG, ifSkip, ifFeature, ifPyramid, ifKFs, ifKeyword, ifEx;

    public RawImage ResultImage;
    public Dropdown drop;
    public Dropdown drop2;
    public Dropdown drop3;

    public GameObject Canvas;
    public AspectRatioFitter fitter;
    CanvasScaler Scaler;

    public Button btnConnect;
    public Button btnSend;
    public Button btnOption;
    public Button btnClose;

    public Toggle toggleCam, toggleIMU, toggleMapping, toggleTracking, toggleTest, toggleVis, toggleSave;

    // Start is called before the first frame update
    void Start()
    {
       
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

        foreach (string dataset in SystemManager.Instance.MapNameList)
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
        toggleTest.isOn = SystemManager.Instance.User.ModeAsyncQualityTest;
        toggleVis.isOn = SystemManager.Instance.User.bVisualizeFrame;
        toggleSave.isOn = SystemManager.Instance.User.bSaveTrajectory;
        SetUI();
        /////Initialize

        ////Add Listener
        ////Input Field
        ifUser.text = SystemManager.Instance.User.UserName;
        ifUser.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.UserName = ifUser.text;
            
        });
        
        ifMap.text = SystemManager.Instance.User.MapName;
        ifMap.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.MapName = ifMap.text;
        });
        ifKeyword.text = SystemManager.Instance.User.Keywords;
        ifKeyword.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.Keywords = ifKeyword.text;
        });
        ifEx.text = SystemManager.Instance.User.Experiments;
        ifEx.onEndEdit.AddListener(delegate {
            SystemManager.Instance.User.Experiments = ifEx.text;

            string[] strExs = SystemManager.Instance.User.Experiments.Split(',');
            string expath = Application.persistentDataPath + "/Experiment/";
            //SystemManager.Instance.Experi = new Dictionary<string, ExperimentData>(strExs.Length);
            foreach (string str in strExs)
            {
                if (!File.Exists(expath + str+".json"))

                {
                    SystemManager.ExperimentData ex = new SystemManager.ExperimentData(str);
                    ex.Init();
                    SystemManager.Instance.Experiments.Add(str, ex);
                    File.WriteAllText(expath + str + ".json", JsonUtility.ToJson(ex));
                }
            }
        });
        
        ifJPEG.text = Convert.ToString(SystemManager.Instance.AppData.JpegQuality);
        ifJPEG.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.JpegQuality = Convert.ToInt32(ifJPEG.text);
            
        });
        ifSkip.text = Convert.ToString(SystemManager.Instance.AppData.numSkipFrames);
        ifSkip.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.numSkipFrames = Convert.ToInt32(ifSkip.text);

        });
        ifFeature.text = Convert.ToString(SystemManager.Instance.AppData.numFeatures);
        ifFeature.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.numFeatures = Convert.ToInt32(ifFeature.text);

        });
        ifPyramid.text = Convert.ToString(SystemManager.Instance.AppData.numPyramids);
        ifPyramid.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.numPyramids = Convert.ToInt32(ifPyramid.text);

        });
        ifKFs.text = Convert.ToString(SystemManager.Instance.AppData.numLocalKeyFrames);
        ifKFs.onValueChanged.AddListener(delegate {
            SystemManager.Instance.AppData.numLocalKeyFrames = Convert.ToInt32(ifKFs.text);

        });
        ////Input Field
        drop.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numCameraParam = drop.value;
            SetUI();
        });
        drop2.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numDataset = drop2.value;
            if (!SystemManager.Instance.User.UseCamera)
            {
                ifMap.text = SystemManager.Instance.MapNameList[drop2.value];
            }
        });
        drop3.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numDatasetFileName = drop3.value;
        });

        toggleCam.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.UseCamera = toggleCam.isOn;
            if (!SystemManager.Instance.User.UseCamera)
            {
                ifMap.text = SystemManager.Instance.MapNameList[SystemManager.Instance.User.numDataset];
            }
            else
            {
                ifMap.text = "TestMap";
            }
        });
        toggleIMU.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.UseGyro = toggleIMU.isOn;
        });
        toggleMapping.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeMapping = toggleMapping.isOn;
        });
        toggleTracking.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeTracking = toggleTracking.isOn;
        });
        toggleTest.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.ModeAsyncQualityTest = toggleTest.isOn;
        });
        toggleVis.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.bVisualizeFrame = toggleVis.isOn;
        });
        toggleSave.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.bSaveTrajectory = toggleSave.isOn;
        });

        btnClose.onClick.AddListener(delegate {
            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));
            File.WriteAllText(Application.persistentDataPath + "/Data/AppData.json", JsonUtility.ToJson(SystemManager.Instance.AppData));
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

                string[] keywords = SystemManager.Instance.User.Keywords.Split(',');

                for(int i = 0; i < keywords.Length; i += 2)
                {
                    UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, keywords[i], "connect", keywords[i+1]);
                }

                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "ReferenceFrame", "connect", "single");
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "ObjectDetection", "connect", "single");
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "Content", "connect", "all");

            }
            else
            {
                btnConnect.GetComponentInChildren<Text>().text = "Connect";
                SystemManager.Instance.Connect = false;
                ////
                //개별 컨트롤러 해제 처리
                Tracker.Instance.Disconnect();
                ////
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "ReferenceFrame", "disconnect", "single");
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "ObjectDetection", "disconnect", "single");
                //UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, "Content", "disconnect", "all");
                string[] keywords = SystemManager.Instance.User.Keywords.Split(',');

                for (int i = 0; i < keywords.Length; i += 2)
                {
                    UdpAsyncHandler.Instance.Send(SystemManager.Instance.User.UserName, keywords[i], "disconnect", keywords[i + 1]);
                }
                UdpAsyncHandler.Instance.UdpSocketClose();
                File.WriteAllLines(Application.persistentDataPath + "/Data/Trajectory.txt", SystemManager.Instance.Trajectory);
            }

            Dictionary<string, SystemManager.ExperimentData>.ValueCollection values = SystemManager.Instance.Experiments.Values;
            SystemManager.ExperimentData[] datas = new SystemManager.ExperimentData[values.Count];
            int idx = 0;
            foreach(SystemManager.ExperimentData data in values)
            {
                data.Update();
                File.WriteAllText(Application.persistentDataPath + "/Experiment/" + data.name + ".json", JsonUtility.ToJson(data));
            }
            
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

        Scaler = Canvas.GetComponentInChildren<CanvasScaler>();
        Scaler.matchWidthOrHeight = 1f;
        Scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
        
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = ((float)SystemManager.Instance.ImageWidth) / SystemManager.Instance.ImageHeight;

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

        RectTransform rtBtn1 = btnConnect.GetComponent<RectTransform>();
        RectTransform rtBtn2 = btnSend.GetComponent<RectTransform>();
        RectTransform rtBtn3 = btnOption.GetComponent<RectTransform>();

        rtBtn1.anchoredPosition = new Vector3((Width + rtBtn1.sizeDelta.x + 10f) / 2f, rtBtn1.anchoredPosition.y);
        rtBtn2.anchoredPosition = new Vector3((Width + rtBtn1.sizeDelta.x + 10f) / 2f, rtBtn2.anchoredPosition.y);
        rtBtn3.anchoredPosition = new Vector3((Width + rtBtn1.sizeDelta.x + 10f) / 2f, rtBtn3.anchoredPosition.y);
        
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
