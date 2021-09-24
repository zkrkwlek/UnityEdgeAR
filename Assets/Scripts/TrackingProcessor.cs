using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class Tracker
{
#if UNITY_EDITOR_WIN
    [DllImport("UnityLibrary")]
    private static extern void ConnectDevice();
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void ConnectDevice();
#endif

    static private Tracker m_pInstance = null;

    static public Tracker Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new Tracker();
            }
            return m_pInstance;
        }
    }
    //파일 읽을 때 이용
    static private int nimageFrameIndex;
    public int ImageFrameIndexX
    {
        get
        {
            return nimageFrameIndex;
        }
        set
        {
            nimageFrameIndex = value;
        }
    }

    static private string[] imgfilelist;
    public string[] ImageList
    {
        get
        {
            return imgfilelist;
        }
    }
    

    public void Connect()
    {
        nimageFrameIndex = 3;

        if (!SystemManager.Instance.Cam)
        {
            string imgFileTxt = SystemManager.Instance.ImagePath + "rgb.txt";
            imgfilelist = File.ReadAllLines(imgFileTxt);
        }

        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr + "/Connect?port=40003");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        while (!request.downloadHandler.isDone)
        {
            continue;
        }

        ////Device & Map store
        string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=DeviceConnect&id=0&src=" + SystemManager.Instance.UserName;
        string msg2 = SystemManager.Instance.UserName + "," + SystemManager.Instance.MapName;
        byte[] bdatab = System.Text.Encoding.UTF8.GetBytes(msg2);
        float[] fdataa = SystemManager.Instance.IntrinsicData;
        byte[] bdata2 = new byte[2 + fdataa.Length * 4 + bdatab.Length];
        bdata2[40] = SystemManager.Instance.IsServerMapping ? (byte)1 : (byte)0;
        bdata2[41] = SystemManager.Instance.UseGyro ? (byte)1 : (byte)0;
        Buffer.BlockCopy(fdataa, 0, bdata2, 0, 40);
        Buffer.BlockCopy(bdatab, 0, bdata2, 42, bdatab.Length);

        //Debug.Log(msg2+" "+ bdatab.Length);

        request = new UnityWebRequest(addr2);
        request.method = "POST";
        uH = new UploadHandlerRaw(bdata2);//webCamByteData);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        res = request.SendWebRequest();

        ConnectDevice();
    }

    public void Disconnect()
    {
        ////Device & Map store
        string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=DeviceDisconnect&id=0&src=" + SystemManager.Instance.UserName;
        string msg2 = SystemManager.Instance.UserName + "," + SystemManager.Instance.MapName;
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);

        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        while (!request.downloadHandler.isDone)
        {
            continue;
        }
    }
}



public class TrackingProcessor : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)
    [DllImport("UnityLibrary")]
    private static extern void SetPath(byte[] name, int len);
    [DllImport("UnityLibrary")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);

    [DllImport("UnityLibrary")]
    private static extern int SetFrame(int id, double ts, ref float t1, ref float t2);
    [DllImport("UnityLibrary")]
    private static extern void SetReferenceFrame(int id, float[] data);
    
    [DllImport("UnityLibrary")]
    private static extern bool GrabImage(IntPtr addr, int id);
    [DllImport("UnityLibrary")]
    private static extern void ReleaseImage();
    [DllImport("UnityLibrary")]
    private static extern bool GetMatchingImage(IntPtr data);
    [DllImport("UnityLibrary")]
    private static extern bool Track(IntPtr posePtr);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetPath(char[] path);
    [DllImport("edgeslam")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4);
    [DllImport("edgeslam")]
    private static extern void SetIMUAddress(IntPtr addr, bool bIMU);
    [DllImport("edgeslam")]
    private static extern bool GrabImage(IntPtr addr, int id);
    [DllImport("edgeslam")]
    private static extern void ReleaseImage();
    [DllImport("edgeslam")]
    private static extern int SetFrame(int id, double ts, ref float t1, ref float t2);
    [DllImport("edgeslam")]
    private static extern void SetReferenceFrame(int id, float[] data);
    [DllImport("edgeslam")]
    private static extern bool Track(IntPtr posePtr);
    [DllImport("edgeslam")]
    private static extern bool GetMatchingImage(IntPtr data);
#endif

    public UnityEngine.UI.Text StatusTxt;
    public RawImage background;
    Texture2D tex;
    GCHandle texHandle;
    IntPtr texPtr;
    WebCamTexture webCamTexture;

    float[] fPoseData;
    GCHandle poseHandle;
    IntPtr posePtr;

    float[] fIMUPose;
    GCHandle imuHandle;
    IntPtr imuPtr;

    byte[] resData;
    GCHandle resHandle;
    IntPtr resPtr;

    bool bTracking = false;
    bool bDoingTrack = false;
    bool bGrabImage = false;
    bool bSendImage = false;

    int mnFrameID = 0;
    double ts = 0.0;
    Matrix3x3 DeltaR = new Matrix3x3();
    Matrix3x3 DeltaFrameR;

    void Start()
    {
        //result image ptr
        resData = new byte[SystemManager.Instance.ImageWidth * SystemManager.Instance.ImageHeight * 4];
        resHandle = GCHandle.Alloc(resData, GCHandleType.Pinned);
        resPtr = resHandle.AddrOfPinnedObject();

        ////pose ptr
        fPoseData = new float[12];
        poseHandle = GCHandle.Alloc(fPoseData, GCHandleType.Pinned);
        posePtr = poseHandle.AddrOfPinnedObject();
        ////pose ptr

        ////imu ptr
        fIMUPose = new float[12];
        imuHandle = GCHandle.Alloc(fIMUPose, GCHandleType.Pinned);
        imuPtr = imuHandle.AddrOfPinnedObject();
        ////imu ptr

        StartCoroutine("ImageSendingCoroutine");

#if (UNITY_EDITOR_WIN)
        byte[] b = System.Text.Encoding.ASCII.GetBytes(Application.persistentDataPath);
        SetPath(b, b.Length);
        //SystemManager.Instance.strBytes, SystemManager.Instance.strBytes.Length, 
#elif (UNITY_ANDROID)
        SetPath(Application.persistentDataPath.ToCharArray());    
#endif
        SetInit(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, SystemManager.Instance.FocalLengthX, SystemManager.Instance.FocalLengthY, SystemManager.Instance.PrincipalPointX, SystemManager.Instance.PrincipalPointY,
                        SystemManager.Instance.IntrinsicData[6], SystemManager.Instance.IntrinsicData[7], SystemManager.Instance.IntrinsicData[8], SystemManager.Instance.IntrinsicData[9]);
        
        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.BGRA32, false);

        if (SystemManager.Instance.Cam)
        {
            //SetDeviceCamera(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, 30.0);
            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; i++)
            {

                bool bWebcamDevice = false;
#if UNITY_EDITOR_WIN
                bWebcamDevice = devices[i].isFrontFacing;
#elif UNITY_ANDROID
                bWebcamDevice = !devices[i].isFrontFacing;
#endif
                if (bWebcamDevice)
                {
                    webCamTexture = new WebCamTexture(devices[i].name, SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, 30);
                    break;
                }

            }

            webCamTexture.Play();

        }
#if (UNITY_EDITOR_WIN)

#elif (UNITY_ANDROID)
        SetIMUAddress(imuPtr, SystemManager.Instance.UseGyro);
#endif

        background.texture = tex;
        
    }
    void Update()
    {
        
        if (SystemManager.Instance.UseGyro)
        {
            DeltaFrameR = SensorManager.Instance.DeltaRotationMatrix();
            DeltaR = DeltaR * DeltaFrameR;
        }

        if (SystemManager.Instance.Start && SystemManager.Instance.Connect)
        {
            try
            {

                if (SystemManager.Instance.Cam)
                {
                    Color[] cdata = webCamTexture.GetPixels();
                    tex.SetPixels(cdata);
                    tex.Apply();
                }
                else
                {
                    string file = Convert.ToString(Tracker.Instance.ImageList[Tracker.Instance.ImageFrameIndexX++].Split(' ')[1]);
                    string imgFile = SystemManager.Instance.ImagePath + file;
                    byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                    tex.LoadImage(byteTexture);
                }

                Color32[] texData = tex.GetPixels32();
                texHandle = GCHandle.Alloc(texData, GCHandleType.Pinned);
                texPtr = texHandle.AddrOfPinnedObject();

                bSendImage = false;
                bGrabImage = GrabImage(texPtr, mnFrameID);

                if (bGrabImage)
                {
                    bSendImage = true;
                    
                    if (SystemManager.Instance.IsDeviceTracking)
                    { 
                        DateTime t1 = DateTime.Now;

                        float tt1 = 0f; float tt2 = 0f;
                        SetFrame(mnFrameID, 0.0, ref tt1, ref tt2);

                        int nRes = 0;
                        if (SystemManager.Instance.UseGyro)
                            DeltaFrameR.Copy(ref fIMUPose, 0);

                        bTracking = Track(posePtr);
                        //DateTime t2 = DateTime.Now;
                        //TimeSpan time1 = t2 - t1;
                        //float temp = (float)time1.Milliseconds;
                        //SystemManager.Instance.TrackingTime.Update(temp);
                        StartCoroutine("SendPoseData");

                    }

                    //if (GetMatchingImage(resPtr))
                    //{
                    //    tex.LoadRawTextureData(resPtr, resData.Length);
                    //    tex.Apply();
                    //    //background.texture = tex;
                    //}

                    ReleaseImage();
                    ++mnFrameID;

                }

            }
            catch (Exception ex)
            {
                StatusTxt.text = ex.ToString();
            }

        }
    }

    byte[] webCamByteData;
    IEnumerator ImageSendingCoroutine()
    {
        int nSkipFrame = SystemManager.Instance.NumSkipFrame;

        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (SystemManager.Instance.Start && mnFrameID % nSkipFrame == 0 && bSendImage)
            {
                ////JPEG 압축
                DateTime t1 = DateTime.Now;
                webCamByteData = tex.EncodeToJPG(100);
                DateTime t2 = DateTime.Now;
                TimeSpan time2 = t2 - t1;
                float temp = (float)time2.Milliseconds;
                //SystemManager.Instance.JpegTime.Update(temp);
                //SystemManager.Instance.JpegTime.nTotalSize += webCamByteData.Length;
                ////JPEG 압축

                int id = mnFrameID;
                DateTime start = DateTime.Now;

                string addr = SystemManager.Instance.ServerAddr + "/Store?keyword=Image&id=" + id + "&src=" + SystemManager.Instance.UserName + "&type2=" + ts;
                //mapImageTime[id] = start;

                if (SystemManager.Instance.UseGyro)
                {
                    //StatusTxt.text = DeltaR.m00 + " " + DeltaR.m11+" "+ DeltaR.m22;
                    float[] fdata = new float[9];
                    DeltaR.Copy(ref fdata, 0);
                    byte[] bdata = new byte[(fdata.Length) * 4];
                    Buffer.BlockCopy(fdata, 0, bdata, 0, fdata.Length * 4);
                    string addr2 = SystemManager.Instance.ServerAddr + "/Store?keyword=Gyro&id=" + id + "&src=" + SystemManager.Instance.UserName;
                    UnityWebRequest request2 = new UnityWebRequest(addr2);
                    request2.method = "POST";
                    UploadHandlerRaw uH2 = new UploadHandlerRaw(bdata);
                    uH2.contentType = "application/json";
                    request2.uploadHandler = uH2;
                    request2.downloadHandler = new DownloadHandlerBuffer();
                    UnityWebRequestAsyncOperation res2 = request2.SendWebRequest();
                    DeltaR = new Matrix3x3();
                }

                UnityWebRequest request = new UnityWebRequest(addr);
                request.method = "POST";
                UploadHandlerRaw uH = new UploadHandlerRaw(webCamByteData);
                uH.contentType = "application/json";
                request.uploadHandler = uH;
                request.downloadHandler = new DownloadHandlerBuffer();

                UnityWebRequestAsyncOperation res = request.SendWebRequest();

                while (request.uploadHandler.progress < 1f)
                {
                    //continue;
                    yield return new WaitForFixedUpdate();
                }
                //while (!request.downloadHandler.isDone)
                //{
                //    yield return new WaitForFixedUpdate();
                //}

            }
        }
    }
}