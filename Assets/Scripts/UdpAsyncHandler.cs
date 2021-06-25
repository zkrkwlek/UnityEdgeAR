using System;
using System.Collections;
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

                //LoadParameter();
            }
            return m_pInstance;
        }
    }

    public UdpState UdpConnect(int port)
    {
        UdpState stat = new UdpState();
        try
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            UdpClient udp = new UdpClient(localEP);
            stat.udp = udp;
            stat.lep = localEP;
            stat.rep = remoteEP;
            //mListUDPs.Add(stat);
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
        
        
        return stat;
    }
    public UdpState UdpConnect(string rermoteip, int remoteport, int localport)
    {
        UdpState stat = new UdpState();
        IPEndPoint localEP = new IPEndPoint(IPAddress.Any, localport);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        IPEndPoint hostEP = new IPEndPoint(IPAddress.Parse(rermoteip), remoteport);
        UdpClient udp = new UdpClient(localEP);
        //udp.Connect(IPAddress.Parse(rermoteip), remoteport);
        stat.udp = udp;
        stat.lep = localEP;
        stat.rep = remoteEP;
        stat.hep = hostEP;
        //mListUDPs.Add(stat);

        ////Connect
        //float[] fdata = new float[1];
        //fdata[0] = 10000f;
        //byte[] bdata = new byte[4];
        //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
        
        SystemManager.EchoData jdata = new SystemManager.EchoData("Pose","connect", SystemManager.Instance.User);
        jdata.type2 = "signle";
        string msg = JsonUtility.ToJson(jdata);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);
        stat.udp.Send(bdata, bdata.Length, stat.hep);
        ////Connect

        udp.BeginReceive(new AsyncCallback(ReceiveCallback), stat);
        return stat;
    }

    private void EchoData(string v1, string v2)
    {
        throw new NotImplementedException();
    }

    public void UdpDisconnect()
    {
        foreach (UdpState stat in mListUDPs)
        {
            stat.udp.Close();
        }
        mListUDPs.Clear();
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
    private static List<UdpState> mListUDPs = new List<UdpState>();
    public List<UdpState> ConnectedUDPs
    {
        get
        {
            return mListUDPs;
        }
    }
}
