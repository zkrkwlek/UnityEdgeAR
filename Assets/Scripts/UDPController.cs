using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UDPProcessor
{
    static private UDPProcessor m_pInstance = null;

    static public UDPProcessor Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new UDPProcessor();
            }
            return m_pInstance;
        }
    }

    void Connect()
    {

    }

    void Disconnect() { 
    
    }

    //여기에 처리 모듈을 등록
}

public class UDPController : MonoBehaviour
{
#if UNITY_EDITOR_WIN
    [DllImport("UnityLibrary")]
    private static extern void SetDataFromUnity(IntPtr addr, char[] keyword, int len, int strlen);
    [DllImport("UnityLibrary")]
    private static extern void AddContentInfo(int id, float x, float y, float z);
    [DllImport("UnityLibrary")]
    private static extern void AddObjectInfos();
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetDataFromUnity(IntPtr addr, char[] keyword, int len, int strlen);
    [DllImport("edgeslam")]
    private static extern void AddContentInfo(int id, float x, float y, float z);
    [DllImport("edgeslam")]
    private static extern void AddObjectInfos();
#endif

    public RawImage ResultImage;
    public UnityEngine.UI.Text StatusTxt;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        while (DataQueue.Instance.ReceivingQueue.Count > 0)
        {
            UdpData data = DataQueue.Instance.ReceivingQueue.Dequeue();
            StartCoroutine(MessageParsing(data));
        }
    }

    IEnumerator MessageParsing(UdpData data) {

        TimeSpan QueueTimeSpan = DateTime.Now - data.receivedTime;
        SystemManager.Instance.Experiments["ReceivingQueue"].Update((float)QueueTimeSpan.Milliseconds);

        if (data.keyword == "ReferenceFrame" )
        {
             UnityWebRequest req1 = GetRequest("ReferenceFrame", data.id);
           

            DateTime t1 = DateTime.Now;
            while (!req1.downloadHandler.isDone)
            {
                //yield return null;
                yield return new WaitForFixedUpdate();
            }
            TimeSpan Dtimespan = DateTime.Now - t1;
            SystemManager.Instance.Experiments["DownloadTime"].Update((float)Dtimespan.Milliseconds);

            SetDataToDevice(req1, "ReferenceFrame");
            //SetDataToDevice(req2, "ReferenceFrameDesc");

            ////update 시간
            UdpData data2 = DataQueue.Instance.Get("Image"+ data.id);
            int n1 = req1.downloadHandler.data.Length;//+req2.downloadHandler.data.Length;
            SystemManager.Instance.Experiments["ReferenceTraffic"].Update(n1);
            TimeSpan time2 = DateTime.Now - data2.sendedTime;
            SystemManager.Instance.Experiments["ReferenceReturnTime"].Update((float)time2.Milliseconds);
            ////update 시간

            float[] a = new float[4];
            Buffer.BlockCopy(req1.downloadHandler.data, 0, a, 0, 4);
            int n = (int)a[0];
            if (n > 30)
            {
                ResultImage.color = new Color(0.0f, 1.0f, 0.0f, 0.4f);
            }
            else
            {
                ResultImage.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
            }

            ////레퍼런스 프레임 설정
            //Tracker.Instance.CreateReferenceFrame();

            //UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
            //Trajectory upadte
            //string t = data2.timestamp + " " + a[1] + " " + a[2] + " " + a[3] + " " + a[4] + " " + a[5] + " " + a[6] + " " + a[7];
            //SystemManager.Instance.Trajectory.Add(t);
        }
        else if (data.keyword == "Content")
        {
            UnityWebRequest req1 = GetRequest("Content", data.id, data.src);
            DateTime t1 = DateTime.Now;
            while (!req1.downloadHandler.isDone)
            {
                //yield return null;
                yield return new WaitForFixedUpdate();
            }
            TimeSpan Dtimespan = DateTime.Now - t1;
            SystemManager.Instance.Experiments["DownloadTime"].Update((float)Dtimespan.Milliseconds);
            DateTime t2 = DateTime.Now;

            float[] fdata = new float[req1.downloadHandler.data.Length / 4];
            Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
            AddContentInfo(data.id, fdata[0], fdata[1], fdata[2]);

            if (data.type2 == SystemManager.Instance.UserName)
            {
                ////update 시간
                //SystemManager.ExperimentData[] exdatas = SystemManager.Instance.Experiments;
                UdpData data2 = DataQueue.Instance.Get("ContentGeneration" + data.id);
                //SystemManager.ExperimentMap[] maps = SystemManager.Instance.ExperimentMaps;

                TimeSpan time2 = DateTime.Now - data2.sendedTime;
                SystemManager.Instance.Experiments["ContentReturnTime"].Update((float)time2.Milliseconds);
            }

            //SystemManager.Instance.Experiments = exdatas;
            //SystemManager.Instance.ExperimentMaps = maps;
            ////update 시간
        }
        else if(data.keyword == "ObjectDetection")
        {
            UnityWebRequest req1 = GetRequest("ObjectDetection", data.id, data.src);
            DateTime t1 = DateTime.Now;
            while (!req1.downloadHandler.isDone)
            {
                //yield return null;
                yield return new WaitForFixedUpdate();
            }
            UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
            TimeSpan time2 = DateTime.Now - data2.sendedTime;
            SystemManager.Instance.Experiments["ObjectDetectionTime"].Update((float)time2.Milliseconds);

            SetDataToDevice(req1, "ObjectDetection");
            AddObjectInfos();
        }
        yield break;
    }

    UnityWebRequest GetRequest(string keyword, int id)
    {
        string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SendWebRequest();
        return request;
    }
    UnityWebRequest GetRequest(string keyword, int id, string src)
    {
        string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + src;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SendWebRequest();
        return request;
    }

    void SetDataToDevice(UnityWebRequest req, string keyword)
    {
        try
        {
            GCHandle handle1 = GCHandle.Alloc(req.downloadHandler.data, GCHandleType.Pinned);
            IntPtr ptr1 = handle1.AddrOfPinnedObject();
            SetDataFromUnity(ptr1, keyword.ToCharArray(), req.downloadHandler.data.Length, keyword.Length);
            handle1.Free();
        }
        catch (Exception e)
        {
            StatusTxt.text = e.ToString();
        }
    }
    
}
