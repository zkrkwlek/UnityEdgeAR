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

    //맵 추가하기
}

    public class DataSender : MonoBehaviour
{
    UnityWebRequest SetRequest(string keyword, byte[] data, int id)
    {
        string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=" + keyword + "&id=" + id + "&src=" + SystemManager.Instance.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(data);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        return request;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(DataQueue.Instance.SendingQueue.Count > 0)
        {
            UdpData data = DataQueue.Instance.SendingQueue.Dequeue();
            StartCoroutine(SendData(data));
        }
    }

    IEnumerator SendData(UdpData data)
    {
        UnityWebRequest req = SetRequest(data.keyword, data.data, data.id);
        req.SendWebRequest();
        yield break;
    }
}
