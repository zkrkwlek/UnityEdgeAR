using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ContentData
{
    public ContentData()
    {

    }
    public ContentData(GameObject o, Vector3 p, Vector3 r)
    {
        pos = p;
        obj = o;
        float fAngle = r.magnitude * Mathf.Rad2Deg;
        q = Quaternion.AngleAxis(fAngle, r.normalized);
    }

    int id;
    public Vector3 pos;
    public Quaternion q;
    public GameObject obj;
}

public class ContentEchoServer : MonoBehaviour
{
    [HideInInspector]
    public bool bConnect = false;
    public GameObject rayPrefab;

    ConcurrentQueue<ContentData> cq;

    // Start is called before the first frame update
    void Start()
    {
        cq = new ConcurrentQueue<ContentData>();
    }

    // Update is called once per frame
    void Update()
    {
        ContentData cd;
        if(cq.TryDequeue(out cd))
        {
            Instantiate(cd.obj, cd.pos, cd.q);
        }
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
                int nIDX = 2;
                Vector3 pos = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
                Vector3 rot = new Vector3(rdata[nIDX++], rdata[nIDX++], rdata[nIDX++]);
                cq.Enqueue(new ContentData(rayPrefab, pos, rot));
                //float fAngle = rot.magnitude * Mathf.Rad2Deg;
                //Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
                ////StartCoroutine("CretaeContentTest");
                //Instantiate(rayPrefab, pos, q);
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
