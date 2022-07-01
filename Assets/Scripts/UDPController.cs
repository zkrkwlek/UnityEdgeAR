﻿using System;
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
    private static extern void TestDownloaddata(int id, char[] keyword, int len1, char[] src, int len2, bool bTracking);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void TestDownloaddata(int id, char[] keyword, int len1, char[] src, int len2, bool bTracking);
#endif
    public PlaneManager mPlaneManager;
    public RawImage ResultImage;
    public UnityEngine.UI.Text StatusTxt;
    // Start is called before the first frame update
    void Start()
    {
        UdpAsyncHandler.Instance.UdpDataReceived += Process;
    }
    void Process(object sender, UdpEventArgs e) {
        try {
            int size = e.bdata.Length;
            string msg = System.Text.Encoding.Default.GetString(e.bdata);
            UdpData data = JsonUtility.FromJson<UdpData>(msg);
            data.receivedTime = DateTime.Now;
            TestDownloaddata(data.id, data.keyword.ToCharArray(), data.keyword.Length, data.src.ToCharArray(), data.src.Length, SystemManager.Instance.User.ModeTracking);
            //StartCoroutine(MessageParsing(data));
            /*
            var timeSpan = DateTime.UtcNow - SystemManager.Instance.StartTime;
            double a = timeSpan.TotalMilliseconds-data.ts;
            UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
            //StatusTxt.text = "aaaaaaaaaaaaaaaaaaaaaaaaa = " + msg+"\n\t\t\t\t"+data.ts + "                  "+a+"             "+data2.ts+" "+data.src;//data.ts;
            SystemManager.Instance.Experiments[data.keyword].Update("latency", (float)a);
            //SystemManager.Instance.Experiments[data.keyword].Update("download", (float)Dtimespan.Milliseconds);
            */
        }
        catch(Exception ex)
        {
            StatusTxt.text = ex.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    HashSet<int> cids = new HashSet<int>();

    IEnumerator MessageParsing(UdpData data) {
        
        //try
        //{
        if (data.keyword == "LocalContent")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();


            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);

                ////가상 객체 등록
                //int N = (int)fdata[0];
                //int idx = 1;
                //for(int i = 0; i < N; i++)
                //{
                //    int id = (int)fdata[i*4+1];
                //    if (!cids.Contains(id))
                //    {
                //        cids.Add(id);
                //        Vector3 pos = new Vector3(fdata[i*4+2], -fdata[i * 4 + 3], fdata[i * 4 + 4]);
                //        Vector3 rot = new Vector3(0.0f, 0.0f, 0.0f);
                //        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                //        float fAngle = rot.magnitude * Mathf.Rad2Deg;
                //        Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
                //        go.transform.SetPositionAndRotation(pos, q);
                //    }
                //}
                ////가상 객체 등록

                //int idx = 1 + (N - 1) * 4;
                //int cid = (int)fdata[idx++];
                //StatusTxt.text = "\t\t\t\t\t" + cid + " " + N+", "+idx+" "+fdata.Length;
                //Vector3 pos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                ////Vector3 rot = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
                //Vector3 rot = new Vector3(0.0f, 0.0f, 0.0f);
                //string contentname = "CO" + cid;
                //var co = GameObject.Find(contentname);
                //if (co)
                //{
                //    StatusTxt.text = "\t\t\t\t already Create " + contentname + co.transform.position.x + " " + co.transform.position.y + " " + co.transform.position.z + "::" + pos.x + " " + pos.y + " " + pos.z;
                //}
                //else
                //{
                //    //StatusTxt.text = "\t\t\t\t Create " + contentname + "::" + pos.x + " " + pos.y + " " + pos.z;
                //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //    go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //    float fAngle = rot.magnitude * Mathf.Rad2Deg;
                //    Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
                //    go.transform.SetPositionAndRotation(pos, q);
                //    go.name = data.type2 + (int)data.ts;
                //    //StatusTxt.text = "\t\t\t Create asdfasdf aaaaaaa " + pos.ToString() + Camera.main.transform.position.ToString()+" "+fdata[0];
                //}

                //시각화

                //AddContentInfo(data.id, fdata[0], fdata[1], fdata[2]);


                //if (data.type2 == SystemManager.Instance.User.UserName)
                //{
                //    ////update 시간
                //    int id = (int)data.ts;
                //    //StatusTxt.text = "\t\t\t\t\t asdfasdfasdfasdfasdfasdfasdf = " +id;
                //    UdpData data2 = DataQueue.Instance.Get("ContentGeneration" + id);
                //    TimeSpan time2 = data.receivedTime - data2.sendedTime;
                //    SystemManager.Instance.Experiments["Content"].Update("latency", (float)time2.Milliseconds);
                //}
            }


        }
        else {
            if (data.keyword == "ReferenceFrame")
            {
                ////update 시간
                UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
                TimeSpan time2 = data.receivedTime - data2.sendedTime;
                SystemManager.Instance.Experiments["ReferenceFrame"].Update("latency", (float)time2.Milliseconds);
                //StatusTxt.text = "\t\t\t\t\t Reference = " + time2.TotalMilliseconds;
                ////update 시간

            }
            else if (data.keyword == "VO.Registration")
            {
                //수행
            }
            else if (data.keyword == "ObjectDetection")
            {
                UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
                TimeSpan time2 = data.receivedTime - data2.sendedTime;
                SystemManager.Instance.Experiments["ObjectDetection"].Update("latency", (float)time2.Milliseconds);
                //SystemManager.Instance.Experiments["ObjectDetection"].Update("traffic", n1);
                //SystemManager.Instance.Experiments["ObjectDetection"].Update("download", (float)Dtimespan.Milliseconds);

            }
            else if (data.keyword == "Segmentation")
            {
                UdpData data2 = DataQueue.Instance.Get("Image" + data.id);
                TimeSpan time2 = data.receivedTime - data2.sendedTime;
                SystemManager.Instance.Experiments["Segmentation"].Update("latency", (float)time2.Milliseconds);
            }
        }
            
        //}
        //catch (Exception e)
        //{
        //    StatusTxt.text = "MessageParsing=" + e.ToString();
        //}
        yield break;
    }

    UnityWebRequest GetRequest(string keyword, int id)
    {
        string addr2 = SystemManager.Instance.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.User.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }
    UnityWebRequest GetRequest(string keyword, int id, string src)
    {
        string addr2 = SystemManager.Instance.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + src;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }
}
