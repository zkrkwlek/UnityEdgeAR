using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class StateObject {

}

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

    static ConcurrentQueue<byte[]> cq = new ConcurrentQueue<byte[]>();
    public ConcurrentQueue<byte[]> MessageQueue
    {
        get
        {
            return cq;
        }
    }

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
        //UdpClient u = ((UdpState)(ar.AsyncState)).u;
        IPEndPoint e = (IPEndPoint)ar.AsyncState;

        byte[] receiveBytes = udp.EndReceive(ar, ref e);

        //Debug.Log("received = " + receiveBytes.Length);

        //string receiveString = Encoding.ASCII.GetString(receiveBytes);

        //Console.WriteLine($"Received: {receiveString}");
        //messageReceived = true;
        if (receiveBytes.Length > 0)
        {

            //cq.Enqueue(receiveBytes);
            DeviceEventArgs args = new DeviceEventArgs();
            args.bdata = receiveBytes;
            OnDataReceived(args);

            udp.BeginReceive(new AsyncCallback(ReceiveCallback), e);
        }
    }

    //void DeviceControl(byte[] bdata)
    //{
    //    int size = bdata.Length;
    //    float[] fdata = new float[size / 4];
    //    Buffer.BlockCopy(bdata, 0, fdata, 0, size);

    //    if (fdata[0] == 1f)
    //    {
    //        Instantiate(deviceObj);
    //    }
    //    else if (fdata[0] == 2f) {

    //    }
    //    else if(fdata[0] == 3f)
    //    {

    //    }

    //}


    /*
    public void Connect() {
        try {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(new IPEndPoint(IPAddress.Any, 30000));

            //join
            multicastIP = IPAddress.Parse("235.26.17.10");
            //udp.JoinMulticastGroup(multicastIP);
            //remoteEP = new IPEndPoint(IPAddress.Any, 0);
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastIP, IPAddress.Any));
            ep = new IPEndPoint(IPAddress.Any, 0);

            StateObject so = new StateObject();
            //sock.BeginConnect(ep, new AsyncCallback(connectCallback), sock);
            sock.BeginReceiveFrom(buff, 0, buffSize, SocketFlags.None, ref ep, new AsyncCallback(receiveCallback), so);
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
        
    }

    public void Disconnect()
    {
        sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(multicastIP, IPAddress.Any));
        sock.Close();
    }

    private void connectCallback(IAsyncResult ar)
    {
        sock = (Socket)ar.AsyncState;

        try
        {
            //sock.EndConnect(ar);
            sock.BeginReceiveFrom(buff, 0, buffSize, SocketFlags.None, ref ep, new AsyncCallback(receiveCallback), buff);
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }

    private void receiveCallback(IAsyncResult iar)
    {
        try
        {
            //Socket remote = (Socket)iar.AsyncState;
            StateObject so = (StateObject)iar.AsyncState;
            int recvSize = sock.EndReceiveFrom(iar, ref ep);
            
            if (recvSize > 0)
            {
                Debug.Log("asdf : " + recvSize);
                sock.BeginReceiveFrom(buff, 0, buffSize, SocketFlags.None, ref ep, new AsyncCallback(receiveCallback), so);
            }
            else
            {
                Disconnect();
                Debug.Log("1818181");
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
    }

    public void ThreadStart()
    {
        if (!bThreadStart)
        {
            //binding
            //udp = new UdpClient();
            //IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 35000);
            //udp.Client.Bind(localEP);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //sock.Bind(new IPEndPoint(IPAddress.Any, 30000));

            //join
            multicastIP = IPAddress.Parse("235.26.17.10");
            //udp.JoinMulticastGroup(multicastIP);
            //remoteEP = new IPEndPoint(IPAddress.Any, 0);
            sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastIP, IPAddress.Any));
            ep = new IPEndPoint(IPAddress.Any, 0);

            //thread
            bThreadStart = true;
            thread = new Thread(Run);
            thread.Start();
        }
    }
    public void ThreadStop()
    {
        //socket 종료
        //udp.DropMulticastGroup(multicastIP);
        //udp.Close();
        sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, new MulticastOption(multicastIP, IPAddress.Any));
        sock.Close();

        //thread 종료
        bThreadStart = false;
        thread.Join();
    }
    
    private void Run()
    {
        while (bThreadStart)
        {
            //Debug.Log("Map Manager thread");
            try {
                //byte[] bmsg = udp.Receive(ref remoteEP);
                int n = sock.ReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref ep);
                Debug.Log("map manager = " + 0);
            }
            catch(Exception e)
            {
                Debug.Log(e.ToString());
            }
            
        }
    }
    */
}
