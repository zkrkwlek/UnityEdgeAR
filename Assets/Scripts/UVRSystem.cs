using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UVRSystem : MonoBehaviour
{
    
    EditorCoroutine editorCorotine;
    static GCHandle webCamHandle;
    static WebCamTexture webCamTexture;
    [HideInInspector]
    static public Color[] webCamColorData;
    static IntPtr webCamPtr;

    public RawImage background;
    int mnFrameID = 0; //start 시에 초기화 시키기?
    bool mbSended = true; //start 시에 초기화 시키기?
    public int nImgFrameIDX = 3;
    

    DateTime baseTime = new DateTime(2021, 1, 1, 0, 0, 0, 0).ToLocalTime();

    public void Init()
    {
        mnFrameID = 0;
        mbSended = true;

        bProcessThread = false;
        bThreadStart = false;
        bWaitThread = true;

        sw = new Stopwatch();
    }

    public void Connect()
    {
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }

    public void Disconnect()
    {
        ////reset or 따로
        //bMappingThread = false;
        //nImgFrameIDX = 3;
        //mnFrameID = 0;
        //mbSended = true;

        string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("Disconnect!!" + addr);
    }
    
    //[MenuItem("UVRSystem/Reset")]
    //static void Reset()
    //{
    //    bMappingThread = false;
    //    nImgFrameIDX = 3;
    //    mnFrameID = 0;
    //    mbSended = true;

    //    string addr = SystemManager.Instance.ServerAddr + "/reset?map=" + SystemManager.Instance.Map;
    //    Debug.Log("Reset:" + addr);
    //    UnityWebRequest request = new UnityWebRequest(addr);
    //    request.method = "POST";
    //    request.downloadHandler = new DownloadHandlerBuffer();
    //    UnityWebRequestAsyncOperation res = request.SendWebRequest();
    //}

    public void SaveMap()
    {
        string addr = SystemManager.Instance.ServerAddr + "/RequestSaveMap?map=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("save map " + addr);
    }

    public void LoadMap()
    {
        string addr = SystemManager.Instance.ServerAddr + "/LoadMap?map=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }

    //여기 아래를 이제 쓰레드에서 동작하도록 변경하기

    static bool bMappingThread = false;
    static bool bMappingThreadStart = false;

    
    public string[] imageData;
    public string imagePath;
    public int nMaxImageIndex;

    public void MapGeneration()
    {
        bMappingThread = !bMappingThread;
        imageData = SystemManager.Instance.ImageData;
        imagePath = SystemManager.Instance.ImagePath;
        Debug.Log(imagePath+" "+ bMappingThread);
        if (!bMappingThreadStart)
        {
            tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
            bMappingThreadStart = true;
        }
    }
        
    void Update()
    {
        if (bMappingThread)
        {
            //if (SystemManager.Instance.ImageData.Length == nImgFrameIDX)
            //{
            //    Debug.Log("END!!!!!!!");
            //    bMappingThread = false;
            //}
            //string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
            //++mnFrameID;
            //Debug.Log(imgFile);
            //if (mbSended && mnFrameID % 3 == 0)
            //{
            //    mbSended = false;
            //    string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
            //    StartCoroutine(Mapping(imgFile, ts));
            //}
        }        
    }

    public Texture2D tex;
    IEnumerator Mapping(string imgFile,string ts)
    {
        //yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();
        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        tex.LoadImage(byteTexture);
        background.texture = tex;
        byte[] webCamByteData = tex.EncodeToJPG(90);

        string addr = SystemManager.Instance.ServerAddr + "/ReceiveAndDetect?user=" + SystemManager.Instance.User + "&map=" + SystemManager.Instance.Map + "&id=" + ts;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(webCamByteData);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();

        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (request.uploadHandler.progress < 1f)
        {
            yield return new WaitForFixedUpdate();
            //progress = request.uploadHandler.progress;
        }
        mbSended = true;
        
    }
    IEnumerator Test2(string imgFile) {
        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        tex.LoadImage(byteTexture);
        background.texture = tex;
        //Debug.Log(imgFile);
        yield return null;
    }
    private Thread thread;
    public bool bProcessThread = false;
    public bool bThreadStart = false;
    public bool bWaitThread = true;

    private float fTime = 0.0f;

    private void Run()
    {
        while (bProcessThread)
        {
            if (!SystemManager.Instance.Connect)
                continue;
            if (bWaitThread) { 
                continue;
            }
            if (nImgFrameIDX >= nMaxImageIndex) {
                Init();
                break;
            }
            Thread.Sleep(33);
            EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
            //string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
            //++mnFrameID;
            
            //if (mbSended && mnFrameID % 3 == 0)
            //{
            //    mbSended = false;
            //    string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
            //    EditorCoroutineUtility.StartCoroutine(Mapping(imgFile, ts), this);                
            //}
        }
        
    }

    Stopwatch sw;

    IEnumerator MappingCoroutine()
    {
        //yield return new WaitForSecondsRealtime(0.033f);
        yield return null;
        string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
        ++mnFrameID;
        string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();
        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        tex.LoadImage(byteTexture);
        background.texture = tex;
        if (mnFrameID % 3 == 0)
        {
            mbSended = false;

            sw.Start();
            byte[] webCamByteData = tex.EncodeToJPG(90);
            string addr = SystemManager.Instance.ServerAddr + "/ReceiveAndDetect?user=" + SystemManager.Instance.User + "&map=" + SystemManager.Instance.Map + "&id=" + ts;
            UnityWebRequest request = new UnityWebRequest(addr);
            request.method = "POST";
            UploadHandlerRaw uH = new UploadHandlerRaw(webCamByteData);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
            request.downloadHandler = new DownloadHandlerBuffer();

            UnityWebRequestAsyncOperation res = request.SendWebRequest();
            while (request.uploadHandler.progress < 1f)
            {
                yield return new WaitForFixedUpdate();
                //progress = request.uploadHandler.progress;
            }
            sw.Stop();
            Debug.Log("time = " +mnFrameID+"::"+ sw.ElapsedMilliseconds.ToString() + "ms");
            sw.Reset();
            mbSended = true;
        }
        

        //while (bProcessThread) {
        //    if (bWaitThread) {
        //        yield return null;
        //        continue;
        //    }
        //    if (nImgFrameIDX >= nMaxImageIndex)
        //    {
        //        Init();
        //        break;
        //    }
        //    yield return new WaitForSecondsRealtime(0.033f);
        //    Debug.Log(Time.deltaTime);
        //    fTime += Time.deltaTime;

        //    string imgFile = imagePath + Convert.ToString(imageData[nImgFrameIDX++].Split(' ')[1]);
        //    ++mnFrameID;

        //    if (mnFrameID % 3 == 0)
        //    {
        //        string ts = (DateTime.Now.ToLocalTime() - baseTime).ToString();

        //        byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
        //        tex.LoadImage(byteTexture);
        //        background.texture = tex;
        //        byte[] webCamByteData = tex.EncodeToJPG(90);

        //        string addr = SystemManager.Instance.ServerAddr + "/ReceiveAndDetect?user=" + SystemManager.Instance.User + "&map=" + SystemManager.Instance.Map + "&id=" + ts;
        //        UnityWebRequest request = new UnityWebRequest(addr);
        //        request.method = "POST";
        //        UploadHandlerRaw uH = new UploadHandlerRaw(webCamByteData);
        //        uH.contentType = "application/json";
        //        request.uploadHandler = uH;
        //        request.downloadHandler = new DownloadHandlerBuffer();

        //        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        //        while (request.uploadHandler.progress < 1f)
        //        {
        //            yield return new WaitForFixedUpdate();
        //            //progress = request.uploadHandler.progress;
        //        }
        //    }

        //}
        //Debug.Log("thread end!! " + fTime);
    }

    ///
    public void ThreadStart()
    {
        Debug.Log("thread start!!");
        if (!bThreadStart)
        {
            bProcessThread = true;
            bWaitThread = false;
            bThreadStart = true;
            thread = new Thread(Run);
            thread.Start();
            //EditorCoroutineUtility.StartCoroutine(MappingCoroutine(), this);
        }
    }
    public void ThreadPause()
    {
        bWaitThread = !bWaitThread;
    }
    public void ThreadStop()
    {
        Debug.Log("thread stop!!");
        Init();
        thread.Join();
    }

}
