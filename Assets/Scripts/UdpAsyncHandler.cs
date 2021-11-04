using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public struct UdpState
{
    public UdpClient udp;
    public IPEndPoint rep, lep, hep;
}
public class UdpEventArgs : EventArgs
{
    public byte[] bdata { get; set; }
}

public class UdpData
{
    public UdpData() { }

    public UdpData(string _key, string _type, string _src)
    {
        keyword = _key;
        type1 = _type;
        src = _src;
    }
    public UdpData(string _key, string _src, int _id, byte[] _data)
    {
        keyword = _key;
        src = _src;
        id = _id;
        data = _data;
    }
    public UdpData(string _key, string _src, int _id, byte[] _data, double _ts)
    {
        keyword = _key;
        src = _src;
        id = _id;
        data = _data;
        timestamp = _ts;
    }
    public string keyword, type1, type2, src;
    public byte[] data;
    public int id, id2;
    public double timestamp;
    public DateTime sendedTime, receivedTime;
}

public class UdpAsyncHandler
{
    static private UdpAsyncHandler m_pInstance = null;
    static public UdpAsyncHandler Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new UdpAsyncHandler();
            }
            return m_pInstance;
        }
    }

    static private UdpState stat;
    public UdpState Status
    {
        get
        {
            return stat;
        }
    }

    public UdpState UdpSocketBegin(int port)
    {
        stat = new UdpState();
        try
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            UdpClient udp = new UdpClient(localEP);
            stat.udp = udp;
            stat.lep = localEP;
            stat.rep = remoteEP;
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        return stat;
    }

    public UdpState UdpSocketBegin(string rermoteip, int remoteport, int localport)
    {
        UdpDataReceived += UdpDataReceivedProcess;

        stat = new UdpState();
        IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 0);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        IPEndPoint hostEP = new IPEndPoint(IPAddress.Parse(rermoteip), remoteport);
        UdpClient udp = new UdpClient(localEP);
        
        stat.udp = udp;
        stat.lep = localEP;
        stat.rep = remoteEP;
        stat.hep = hostEP;

        udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        
        return stat;
    }

    public void UdpSocketClose()
    {
        stat.udp.Close();
        UdpDataReceived -= UdpDataReceivedProcess;
    }

    public void Send(string src, string keyword, string method, string type)
    {
        UdpData data = new UdpData(keyword, method, src);
        data.type2 = type;
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);
        stat.udp.Send(bdata, bdata.Length, stat.hep);
                
    }
    
    /*
      ////Connect Pose
        
        
        jdata = new SystemManager.EchoData("Content", "connect", SystemManager.Instance.UserName);
        jdata.type2 = "all";
        msg = JsonUtility.ToJson(jdata);
        bdata = System.Text.Encoding.UTF8.GetBytes(msg);
        stat.udp.Send(bdata, bdata.Length, stat.hep);
        ////Connect


     */

    /*
    ////Connect Pose
            SystemManager.EchoData jdata = new SystemManager.EchoData("Pose", "disconnect", SystemManager.Instance.UserName);
            jdata.type2 = "single";
            string msg = JsonUtility.ToJson(jdata);
            byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);
            stat.udp.Send(bdata, bdata.Length, stat.hep);
            //////Connect Image
            //jdata = new SystemManager.EchoData("Image", "disconnect", SystemManager.Instance.User);
            //jdata.type2 = "single";
            //msg = JsonUtility.ToJson(jdata);
            //bdata = System.Text.Encoding.UTF8.GetBytes(msg);
            //stat.udp.Send(bdata, bdata.Length, stat.hep);
            ////Connect Image
            jdata = new SystemManager.EchoData("Content", "disconnect", SystemManager.Instance.UserName);
            jdata.type2 = "all";
            msg = JsonUtility.ToJson(jdata);
            bdata = System.Text.Encoding.UTF8.GetBytes(msg);
            stat.udp.Send(bdata, bdata.Length, stat.hep);
            ////Connect
     */


    private void EchoData(string v1, string v2)
    {
        throw new NotImplementedException();
    }

    public void SendData(UdpState stat, byte[] data)
    {
        stat.udp.Send(data, data.Length, stat.hep);
    }
    public void ReceiveCallback(IAsyncResult ar)
    {

        UdpClient udp = ((UdpState)(ar.AsyncState)).udp;
        IPEndPoint remoteEP = ((UdpState)(ar.AsyncState)).rep;
        byte[] receiveBytes = udp.EndReceive(ar, ref remoteEP);
        if (receiveBytes.Length > 0)
        {
            UdpEventArgs args = new UdpEventArgs();
            args.bdata = receiveBytes;
            OnUdpDataReceived(args);
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), (UdpState)ar.AsyncState);
        }
        else
        {
            Debug.Log("?????????");
        }
    }
    public virtual void OnUdpDataReceived(UdpEventArgs e)
    {
        EventHandler<UdpEventArgs> handler = UdpDataReceived;
        if (handler != null)
        {
            handler(this, e);
        }
    }

    public event EventHandler<UdpEventArgs> UdpDataReceived;

    //private static List<UdpState> mListUDPs = new List<UdpState>();
    //public List<UdpState> ConnectedUDPs
    //{
    //    get
    //    {
    //        return mListUDPs;
    //    }
    //}

    //ConcurrentQueue
    //static private Queue<UdpData> dataQueue = new Queue<UdpData>();
    //public Queue<UdpData> DataQueue
    //{
    //    get
    //    {
    //        return dataQueue;
    //    }
    //}
    
    void UdpDataReceivedProcess(object sender, UdpEventArgs e)
    {
        int size = e.bdata.Length;
        string msg = System.Text.Encoding.Default.GetString(e.bdata);
        UdpData data = JsonUtility.FromJson<UdpData>(msg);
        data.receivedTime = DateTime.Now;

        ////데이터 처리용
        //if (data.keyword == "Pose")
        //{
        //    try
        //    {
        //        SystemManager.Instance.ReferenceTime.Update(temp);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Log("err = " + ex.ToString());
        //    }

        //}
        //else if (data.keyword == "Content" && data.type2 == SystemManager.Instance.UserName)
        //{
        //    DateTime end = DateTime.Now;
        //    TimeSpan time = end - mapContentTime[data.id2];

        //    float temp = (float)time.Milliseconds;
        //    SystemManager.Instance.ContentGenerationTime.Update(temp);
        //}
        //else
        //{

        //}

        //dataQueue.Enqueue(data);
        DataQueue.Instance.ReceivingQueue.Enqueue(data);
        
    }
    //IEnumerable Test() {
    //    yield return null;
    //}
    //큐에서 
}
