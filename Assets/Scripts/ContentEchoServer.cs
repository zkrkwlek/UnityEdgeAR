using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ContentEchoServer : MonoBehaviour
{
    [HideInInspector]
    public bool bConnect = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    private Thread socthread;
    public void StartEchoClient()
    {
        socthread = new Thread(SocRun);
        socthread.Start();
    }
    public void StopEchoClient()
    {
        socthread.Join();
    }

    private void SocRun()
    {
        Debug.Log("UDP Client Receive Thread Start");
        while (bConnect)
        {
            try
            {
                EndPoint ep = AsyncSocketReceiver.Instance.RemoteEP;
                int bytes = AsyncSocketReceiver.Instance.SOCKET.ReceiveFrom(AsyncSocketReceiver.Instance.BUFFER, AsyncSocketReceiver.Instance.BUFSIZE, SocketFlags.None, ref ep);
                AsyncSocketReceiver.Instance.RemoteEP = ep;
                float[] rdata = new float[bytes / 4];
                Buffer.BlockCopy(AsyncSocketReceiver.Instance.BUFFER, 0, rdata, 0, bytes);
                Debug.Log("Received = "+rdata[0] + " " + rdata[1] + " " + rdata[2] + ":" + rdata[3] + " " + rdata[4] + " " + rdata[5]);
                Vector3 pos = new Vector3(rdata[0], rdata[1], rdata[2]);
                Vector3 rot = new Vector3(rdata[3], rdata[4], rdata[5]);
                Debug.Log("asdf");
                StartCoroutine("CretaeContentTest");
                //StartCoroutine(CretaeContent(pos, rot, 100f));
                Debug.Log("defgh");
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }
    }
    IEnumerator CretaeContentTest()
    {
        Debug.Log("Create Content");
        yield return null;
    }
    IEnumerator CretaeContent(Vector3 pos, Vector3 rot, float dist)
    {
        Debug.Log("Create Content");
        try {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            float fAngle = rot.magnitude * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);

            go.transform.SetPositionAndRotation(pos, q);

            Transform tr = go.transform;
            Vector3 spawnPoint = tr.position;
        } catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
       

        //bool bDist = (spawnPoint - tr.position).sqrMagnitude < dist;
        //while (bDist)
        //{
        //    yield return new WaitForSeconds(0.001f);
        //    tr.Translate(Vector3.forward * Time.deltaTime * 10f);
        //    bDist = (spawnPoint - tr.position).sqrMagnitude < dist;
        //    if (!bDist)
        //    {
        //        //Destroy(Bullet);
        //        try
        //        {
        //            StartCoroutine(DestroyBullet(go));
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.Log(e.ToString());
        //        }

        //    }
        //}
        yield return null;
    }
    IEnumerator DestroyBullet(GameObject obj)
    {
        DestroyImmediate(obj);
        yield return null;
    }
}
