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
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale);
    [DllImport("UnityLibrary")]
    private static extern void SetReferenceFrame();
    [DllImport("UnityLibrary")]
    private static extern bool SetLocalMap();
    [DllImport("UnityLibrary")]
    private static extern void ConnectDevice();
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale);    
    [DllImport("edgeslam")]
    private static extern void SetReferenceFrame();
    [DllImport("edgeslam")]
    private static extern bool SetLocalMap();
    [DllImport("edgeslam")]
    private static extern void ConnectDevice();
#endif

    static private RawImage background;
    public RawImage Background
    {
        set
        {
            background = value;
        }
        get
        {
            return background;
        }
    }

    static private Texture2D tex;
    public Texture2D Texture
    {
        get 
        {
            return tex;
        }
    }
    static private WebCamTexture webCamTexture;
    
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

    static private bool bAccessCamera = false;
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
        byte[] bdata2 = new byte[3 + fdataa.Length * 4 + bdatab.Length];
        bdata2[40] = SystemManager.Instance.IsServerMapping ? (byte)1 : (byte)0;
        bdata2[41] = SystemManager.Instance.IsDeviceTracking ? (byte)1 : (byte)0;
        bdata2[42] = SystemManager.Instance.UseGyro ? (byte)1 : (byte)0;
        Buffer.BlockCopy(fdataa, 0, bdata2, 0, 40);
        Buffer.BlockCopy(bdatab, 0, bdata2, 43, bdatab.Length);

        //Debug.Log(msg2+" "+ bdatab.Length);

        request = new UnityWebRequest(addr2);
        request.method = "POST";
        uH = new UploadHandlerRaw(bdata2);//webCamByteData);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        res = request.SendWebRequest();

        //SystemManager.Instance.AppData.
        SetInit(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, SystemManager.Instance.FocalLengthX, SystemManager.Instance.FocalLengthY, SystemManager.Instance.PrincipalPointX, SystemManager.Instance.PrincipalPointY,
                        SystemManager.Instance.IntrinsicData[6], SystemManager.Instance.IntrinsicData[7], SystemManager.Instance.IntrinsicData[8], SystemManager.Instance.IntrinsicData[9],
                        SystemManager.Instance.AppData.numFeatures,SystemManager.Instance.AppData.numPyramids, 1.2f);

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
                    bAccessCamera = true;
                    webCamTexture = new WebCamTexture(devices[i].name, SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, 30);
                    break;
                }

            }
            webCamTexture.Play();
        }
        background.texture = tex;

        ConnectDevice();
    }

    public void Disconnect()
    {
        if (bAccessCamera)
        {
            webCamTexture.Stop();
        }


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

    public void LoadWebcamTextre()
    {
        Color[] cdata = webCamTexture.GetPixels();
        tex.SetPixels(cdata);
        tex.Apply();
    }

    public byte[] GetData(string keyword, int id)
    {
        string addr2 = SystemManager.Instance.ServerAddr + "/Load?keyword="+keyword+"&id=" + id + "&src=" + SystemManager.Instance.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();

        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (!request.downloadHandler.isDone)
        {
            continue;
        }
        return request.downloadHandler.data;
    }

    public void CreateReferenceFrame()
    {
        SetReferenceFrame();
        SetLocalMap();
        //byte[] res1 = GetData("ReferenceFrame", data.id);
        //byte[] res_desc = GetData("LocalMap", data.id);
        //byte[] res_scale = GetData("LocalMapScales", data.id);
        //byte[] res_angle = GetData("LocalMapAngles", data.id);
        //byte[] res_points = GetData("LocalMapPoints", data.id);

        //GCHandle handle1 = GCHandle.Alloc(res_points, GCHandleType.Pinned);
        //IntPtr ptr1 = handle1.AddrOfPinnedObject();
        //GCHandle handle2 = GCHandle.Alloc(res_desc, GCHandleType.Pinned);
        //IntPtr ptr2 = handle2.AddrOfPinnedObject();
        //GCHandle handle3 = GCHandle.Alloc(res_scale, GCHandleType.Pinned);
        //IntPtr ptr3 = handle3.AddrOfPinnedObject();
        //GCHandle handle4 = GCHandle.Alloc(res_angle, GCHandleType.Pinned);
        //IntPtr ptr4 = handle4.AddrOfPinnedObject();

        //SetLocalMap(ptr1, res_points.Length, ptr2, res_desc.Length, ptr3, res_scale.Length, ptr4, res_angle.Length);

        //handle1.Free();
        //handle2.Free();
        //handle3.Free();
        //handle4.Free();

        //Pose
        //keypoint(2d, angle, scale)
        //mp(3d, predicted scale, descriptor)

        //float[] fdata = new float[request.downloadHandler.data.Length / 4];
        //Buffer.BlockCopy(request.downloadHandler.data, 0, fdata, 0, request.downloadHandler.data.Length);
        ////SetReferenceFrame(data.id, fdata);

    }
}



public class TrackingProcessor : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)
    [DllImport("UnityLibrary")]
    private static extern int SetFrame(IntPtr data,int id, double ts, ref float t1, ref float t2);
    [DllImport("UnityLibrary")]
    private static extern bool GrabImage(IntPtr addr, int id);
    [DllImport("UnityLibrary")]
    private static extern bool GetMatchingImage(IntPtr data);
    [DllImport("UnityLibrary")]
    private static extern bool Track(IntPtr posePtr);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetIMUAddress(IntPtr addr, bool bIMU);
    [DllImport("edgeslam")]
    private static extern bool GrabImage(IntPtr addr, int id);
    [DllImport("edgeslam")]
    private static extern int SetFrame(IntPtr data, int id, double ts, ref float t1, ref float t2);
    
    [DllImport("edgeslam")]
    private static extern bool Track(IntPtr posePtr);
    [DllImport("edgeslam")]
    private static extern bool GetMatchingImage(IntPtr data);
#endif

    public UnityEngine.UI.Text StatusTxt;
    public RawImage ResultImage;
    public RawImage background;
    GCHandle texHandle;
    IntPtr texPtr;
    
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

        Tracker.Instance.Background = background;
        StartCoroutine("ImageSendingCoroutine");


        
#if (UNITY_EDITOR_WIN)

#elif (UNITY_ANDROID)
        SetIMUAddress(imuPtr, SystemManager.Instance.UseGyro);
#endif
        
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
                    Tracker.Instance.LoadWebcamTextre();
                }
                else
                {
                    string file = Convert.ToString(Tracker.Instance.ImageList[Tracker.Instance.ImageFrameIndexX++].Split(' ')[1]);
                    string imgFile = SystemManager.Instance.ImagePath + file;
                    byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                    Tracker.Instance.Texture.LoadImage(byteTexture);
                }

                if (SystemManager.Instance.IsDeviceTracking)
                {
                    Color32[] texData = Tracker.Instance.Texture.GetPixels32();
                    GCHandle texHandle = GCHandle.Alloc(texData, GCHandleType.Pinned);
                    IntPtr texPtr = texHandle.AddrOfPinnedObject();
                    //bGrabImage = GrabImage(texPtr, mnFrameID);
                    float tt1 = 0f; float tt2 = 0f;
                    SetFrame(texPtr, mnFrameID, 0.0, ref tt1, ref tt2);
                    if (SystemManager.Instance.UseGyro)
                        DeltaFrameR.Copy(ref fIMUPose, 0);
                    bool bTrack = Track(posePtr);
                    if (bTrack)
                    {
                        ResultImage.color = new Color(0.0f, 1.0f, 0.0f, 4.0f);
                    }
                    else {
                        ResultImage.color = new Color(1.0f, 0.0f, 0.0f, 4.0f);
                    }
                    texHandle.Free();
                }                
                

                //texHandle.Free();
                //Marshal.FreeHGlobal(texPtr);
                /*
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
                    //}
                }
                ReleaseImage();
                */
                ++mnFrameID;
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
            if (SystemManager.Instance.Start && mnFrameID % nSkipFrame == 0)
            {
                ////JPEG 압축
                DateTime t1 = DateTime.Now;
                webCamByteData = Tracker.Instance.Texture.EncodeToJPG(100);
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
                    //yield return new WaitForFixedUpdate();
                    yield return null;
                }
                //while (!request.downloadHandler.isDone)
                //{
                //    yield return new WaitForFixedUpdate();
                //}

            }
            yield return null;
        }
    }
}