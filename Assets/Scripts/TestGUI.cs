using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TestGUI : MonoBehaviour
{

    public Dropdown drop;
    public Dropdown drop2;
    GameObject Canvas;
    CanvasScaler Scaler;

    public Button btnConnect;
    public Button btnSend;

    public Toggle toggleCam, toggleIMU, toggleMapping, toggleTracking;

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

        /////Initialize
        drop.options.Clear();
        drop2.options.Clear();
        foreach(SystemManager.CameraParams cam in SystemManager.Instance.Cameras)
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

        drop.value = SystemManager.Instance.User.numCameraParam;
        drop2.value = SystemManager.Instance.User.numDataset;

        toggleCam.isOn = SystemManager.Instance.User.UseCamera;
        toggleIMU.isOn = SystemManager.Instance.User.UseGyro;
        toggleMapping.isOn = SystemManager.Instance.User.ModeMapping;
        toggleTracking.isOn = SystemManager.Instance.User.ModeTracking;

        SetUI();
        /////Initialize

        ////Add Listener
        drop.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numCameraParam = drop.value;
            SetUI();
            
        });
        drop2.onValueChanged.AddListener(delegate
        {
            SystemManager.Instance.User.numDataset = drop2.value;
        });

        toggleCam.onValueChanged.AddListener(delegate {
            SystemManager.Instance.User.UseCamera = toggleCam.isOn;
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
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "Pose", "connect", "single");
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
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "Pose", "disconnect", "single");
                UdpAsyncHandler.Instance.Send(SystemManager.Instance.UserName, "Content", "disconnect", "all");
                UdpAsyncHandler.Instance.UdpSocketClose();
            }

            File.WriteAllText(Application.persistentDataPath + "/Data/UserData.json", JsonUtility.ToJson(SystemManager.Instance.User));

        });

        btnSend.onClick.AddListener(delegate {
            bool bStart = !SystemManager.Instance.Start;
            if (bStart)
            {
                btnSend.GetComponentInChildren<Text>().text = "Stop";
                SystemManager.Instance.Start = true;
                Debug.Log("Start");
            }
            else
            {
                btnSend.GetComponentInChildren<Text>().text = "Start";
                SystemManager.Instance.Start = false;
                Debug.Log("Stop");
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

        RectTransform rt1 = drop.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        float offset = rt1.sizeDelta.x / 2f+30f;

        RectTransform rt2 = drop2.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);

        RectTransform rt3 = btnConnect.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        RectTransform rt4 = btnSend.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);

        RectTransform rt5 = toggleCam.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        RectTransform rt6 = toggleIMU.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        RectTransform rt7 = toggleMapping.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        RectTransform rt8 = toggleTracking.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);

        rt1.anchoredPosition = new Vector3(-Width / 2f - offset, 200f);
        rt2.anchoredPosition = new Vector3(-Width / 2f - offset, 150f);
        rt5.anchoredPosition = new Vector3(-Width / 2f - offset, 100f);
        rt6.anchoredPosition = new Vector3(-Width / 2f - offset, 50f);
        rt7.anchoredPosition = new Vector3(-Width / 2f - offset, 0f);
        rt8.anchoredPosition = new Vector3(-Width / 2f - offset, -50f);

        rt3.anchoredPosition = new Vector3(Width / 2f + offset, 200f);
        rt4.anchoredPosition = new Vector3(Width / 2f + offset, 150f);

        //RectTransform rt = drop.GetComponent<RectTransform>();//.anchoredPosition = new Vector3(-700, -124, 0);
        //rt.anchoredPosition = new Vector3(-700, -124, 0);
        //rt.sizeDelta = new Vector2(200f, 50f);
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
