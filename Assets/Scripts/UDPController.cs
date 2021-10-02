using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

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
    private static extern void SetDataFromUnity(IntPtr addr, char[] keyword, int len);

#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetDataFromUnity(IntPtr addr, char[] keyword, int len);
    
#endif


    public UnityEngine.UI.Text StatusTxt;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine("MessageParsing");
    }

    // Update is called once per frame
    void Update()
    {

    }

    IEnumerator MessageParsing()
    {

        while (true)
        {
            //yield return null;
            //yield return new WaitForFixedUpdate();
            UdpData data;
            if(UdpAsyncHandler.Instance.DataQueue.TryDequeue(out data))
            {

                if(data.keyword == "ReferenceFrame" && SystemManager.Instance.IsDeviceTracking)
                {

                    DateTime t1 = DateTime.Now;
                    //yield return StartCoroutine(GetData("ReferenceFrame", data.id));
                    //yield return StartCoroutine(GetData("ReferenceFrameDesc", data.id));
                    //yield return StartCoroutine(GetData("LocalMap", data.id));
                    //yield return StartCoroutine(GetData("LocalMapScales", data.id));
                    //yield return StartCoroutine(GetData("LocalMapAngles", data.id));
                    //yield return StartCoroutine(GetData("LocalMapPoints", data.id));

                    UnityWebRequest req1 = GetRequest("ReferenceFrame", data.id);
                    UnityWebRequest req2 = GetRequest("ReferenceFrameDesc", data.id);
                    UnityWebRequest req3 = GetRequest("LocalMap", data.id);
                    UnityWebRequest req4 = GetRequest("LocalMapScales", data.id);
                    UnityWebRequest req5 = GetRequest("LocalMapAngles", data.id);
                    UnityWebRequest req6 = GetRequest("LocalMapPoints", data.id);

                    while (!req1.downloadHandler.isDone || !req2.downloadHandler.isDone || !req3.downloadHandler.isDone || !req4.downloadHandler.isDone || !req5.downloadHandler.isDone || !req6.downloadHandler.isDone)
                    {
                        yield return null;
                    }
                    SetDataToDevice(req1, "ReferenceFrame");
                    SetDataToDevice(req2, "ReferenceFrameDesc");
                    SetDataToDevice(req3, "LocalMap");
                    SetDataToDevice(req4, "LocalMapScales");
                    SetDataToDevice(req5, "LocalMapAngles");
                    SetDataToDevice(req6, "LocalMapPoints");

                    //Tracker.Instance.CreateReferenceFrame();
                    DateTime t2 = DateTime.Now;
                    TimeSpan time1 = t2 - t1;
                    float temp = (float)time1.Milliseconds;
                    StatusTxt.text = "time = " + temp;

                }
            }
            yield return null;
        }
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

    void SetDataToDevice(UnityWebRequest req, string keyword)
    {
        try
        {
            GCHandle handle1 = GCHandle.Alloc(req.downloadHandler.data, GCHandleType.Pinned);
            IntPtr ptr1 = handle1.AddrOfPinnedObject();
            SetDataFromUnity(ptr1, keyword.ToCharArray(), req.downloadHandler.data.Length);
            handle1.Free();
        }
        catch (Exception e)
        {
            StatusTxt.text = e.ToString();
        }
    }

    IEnumerator GetData(string keyword, int id)
    {
        string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();
        if (request.downloadHandler.isDone)
        {
            try
            {
                GCHandle handle1 = GCHandle.Alloc(request.downloadHandler.data, GCHandleType.Pinned);
                IntPtr ptr1 = handle1.AddrOfPinnedObject();
                SetDataFromUnity(ptr1, keyword.ToCharArray(), request.downloadHandler.data.Length);
            }catch(Exception e)
            {
                StatusTxt.text = e.ToString();
            }
            

            //yield return request.downloadHandler.data;
            yield return null;
            //set data 맵에 키워드로 넣기
            //길이와 타입 데이ㅌㅓ
           
        }
    }
    //IEnumerator CreateReference(UdpData data)
    //{
    //    byte[] res1 = GetData("Pose", data.id);
    //    byte[] res2 = GetData("LocalMap", data.id);
    //    byte[] res3 = GetData("LocalMapScales", data.id);
    //    byte[] res4 = GetData("LocalMapPoints", data.id);
    //}

    //public byte[] GetData(string keyword, int id)
    //{
    //    string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword=+" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.UserName;
    //    UnityWebRequest request = new UnityWebRequest(addr2);
    //    request.method = "POST";
    //    request.downloadHandler = new DownloadHandlerBuffer();

    //    UnityWebRequestAsyncOperation res = request.SendWebRequest();
    //    while (!request.downloadHandler.isDone)
    //    {
    //        continue;
    //    }
    //    return request.downloadHandler.data;
    //}
}
