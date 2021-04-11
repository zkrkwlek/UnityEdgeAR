using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class DeviceEventArgs : EventArgs
{
    public byte[] bdata { get; set; }
}

public class MapManager
{

    /// <summary>
    /// 데이터 전송받았을 때 메세지 큐에 담고 다른 곳에서 처리하도록 하기 위한 이벤트 발생
    /// </summary>
    
    public event EventHandler<DeviceEventArgs> DeviceDataReceived;
    public virtual void OnDataReceived(DeviceEventArgs e)
    {
        EventHandler<DeviceEventArgs> handler = DeviceDataReceived;
        if(handler != null)
        {
            handler(this, e);
        }
    }

    UdpClient udp;
    Socket sock;
    IPAddress multicastIP;
    IPEndPoint remoteEP, localEP;
    EndPoint ep;
    
    private Thread thread;
    public bool bThreadStart = false;

    byte[] buff = new byte[1024];
    int buffSize = 1024;

    static private MapManager m_pInstance = null;
    static public MapManager Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new MapManager();
            }
            return m_pInstance;
        }
    }
    public void Connect() {
        localEP = new IPEndPoint(IPAddress.Any, 40000);
        remoteEP = new IPEndPoint(IPAddress.Any, 0);
        udp = new UdpClient(localEP);
        udp.BeginReceive(new AsyncCallback(ReceiveCallback), remoteEP);
    }
    public void Disconnect()
    {
        udp.Close();
    }

    public void ReceiveCallback(IAsyncResult ar)
    {
        IPEndPoint e = (IPEndPoint)ar.AsyncState;
        byte[] receiveBytes = udp.EndReceive(ar, ref e);
        if (receiveBytes.Length > 0)
        {
            DeviceEventArgs args = new DeviceEventArgs();
            args.bdata = receiveBytes;
            OnDataReceived(args);
            udp.BeginReceive(new AsyncCallback(ReceiveCallback), e);
        }
    }
    
}
