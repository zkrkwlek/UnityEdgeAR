using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataQueue
{
    static private DataQueue m_pInstance = null;
    static public DataQueue Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new DataQueue();
                map = new Dictionary<string, UdpData>();
                bSending = false;
            }
            return m_pInstance;
        }
    }
    static private Queue<UdpData> squeue = new Queue<UdpData>();
    public Queue<UdpData> SendingQueue
    {
        get
        {
            return squeue;
        }
    }

    static private Queue<UdpData> rqueue = new Queue<UdpData>();
    public Queue<UdpData> ReceivingQueue
    {
        get
        {
            return rqueue;
        }
    }
    
    public void Add(UdpData data)
    {
        string key = data.keyword + data.id;
        map[key] = data;
    }

    public UdpData Get(string key)
    {
        return map[key];
    }
    public bool Sending
    {
        set
        {
            bSending = value;
        }
        get
        {
            return bSending;
        }
    }
    static private bool bSending;
    static private Dictionary<string, UdpData> map;
    //맵 추가하기
}

public class DataTransfer {

    UnityWebRequest SetRequest(string keyword, byte[] data, int id, double ts)
    {
        string addr2 = SystemManager.Instance.AppData.Address + "/Store?keyword=" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.User.UserName;
        if (ts > 0.0)
            addr2 += "&type2=" + ts;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        if (data.Length > 0)
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(data);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    public IEnumerator SendData(UdpData data)
    {
        UnityWebRequest req = SetRequest(data.keyword, data.data, data.id, data.timestamp);
        yield return req.SendWebRequest();

        //DateTime t1 = DateTime.Now;
        //while (req.uploadHandler.progress < 1f)
        //{
        //    yield return new WaitForFixedUpdate();
        //    //yield return new WaitForSecondsRealtime(0.001f);
        //}
        //TimeSpan SnedingTimeSpan = DateTime.Now - t1;
        //SystemManager.Instance.Experiments["UploadTime"+data.keyword].Update((float)SnedingTimeSpan.Milliseconds);
        //yield break;
    }
}
    public class DataSender : MonoBehaviour
{
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //while(DataQueue.Instance.SendingQueue.Count > 0)
        //{
        //    UdpData data = DataQueue.Instance.SendingQueue.Dequeue();
        //    StartCoroutine(SendData(data));
        //}
    }

}
