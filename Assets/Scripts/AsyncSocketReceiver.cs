using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class AsyncSocketReceiver
{
    //참조 https://gist.github.com/louis-e/888d5031190408775ad130dde353e0fd
    private EndPoint lep, rep;
    private Socket socket;
    private UdpState stat;

    private const int bufSize = 8 * 1024;

    public EndPoint RemoteEP
    {
        get
        {
            return rep;
        }
        set {
            rep = value;
        }
    }
    public Socket SOCKET
    {
        get
        {
            return socket;
        }
    }

    public byte[] BUFFER
    {
        get
        {
            return stat.buffer;
        }
    }
    public int BUFSIZE
    {
        get
        {
            return bufSize;
        }
    }

    static private AsyncSocketReceiver m_pInstance = null;
    static public AsyncSocketReceiver Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new AsyncSocketReceiver();
                
                //LoadParameter();
            }
            return m_pInstance;
        }
    }

    public class UdpState
    {
        public Socket u;
        public EndPoint e;
        public byte[] buffer = new byte[bufSize];
    }

    public void Connect(string ip, int port)
    {
        
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //lep = new IPEndPoint(IPAddress.Any, 34999); // 에코 서버의 포트
        //socket.Bind(lep);
        socket.Connect(IPAddress.Parse(ip), port);
        
        
        rep = new IPEndPoint(IPAddress.Any, 0); // 에코 서버의 포트
        stat = new UdpState();
        stat.e = rep;
        stat.u = socket;

    }
    public void Disconnect()
    {
        //SendDone.WaitOne();
        //if (Usend != null)
        //    Usend.Close();
        if (socket != null)
            socket.Close();
    }

    public void SendData(byte[] data)
    {
        socket.SendTo(data, data.Length, SocketFlags.None, socket.RemoteEndPoint);
        //try
        //{
        //    Debug.Log("Port = " + ((IPEndPoint)socket.LocalEndPoint).Port + "");
        //}
        //catch (Exception e)
        //{
        //    Debug.Log(e.ToString());
        //}
    }

    
    
}
