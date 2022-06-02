﻿using System;
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
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale, int nskip, int nKFs);
    [DllImport("UnityLibrary")]
    private static extern void SetUserName(char[] src, int len);
    [DllImport("UnityLibrary")]
    private static extern void ConnectDevice();
    [DllImport("UnityLibrary")]
    private static extern void DisconnectDevice();
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale, int nskip, int nKFs);    
    [DllImport("edgeslam")]
    private static extern void SetUserName(char[] src, int len);    
    [DllImport("edgeslam")]
    private static extern void ConnectDevice();
    [DllImport("edgeslam")]
    private static extern void DisconnectDevice();
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

    public Rect BackgroundRect
    {
        set 
        {
            backgroundrect = value;
        }
        get 
        {
            return backgroundrect;
        }
    }
    static private Rect backgroundrect;

    public Vector2 Diff
    {
        set
        {
            diff = value;
        }
        get
        {
            return diff;
        }
    }
    static private Vector2 diff;

    public float Scale
    {
        set
        {
            scale = value;
        }
        get 
        {
            return scale;
        }
    }
    static private float scale;

    public int SkipFrame
    {
        get
        {
            return nSkip;
        }
        set 
        {
            nSkip = value;
        }
    }
    static private int nSkip;

    public int ImageQuality
    {
        get
        {
            return nQuality;
        }
        set
        {
            nQuality = value;
        }
    }
    static private int nQuality;

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
        ////맵 이름을 여기서 수정하도록 변경.
        if (!SystemManager.Instance.User.UseCamera)
        {
            string imgFileTxt = SystemManager.Instance.DataLists[SystemManager.Instance.User.numDataset] + SystemManager.Instance.DataFile; //"rgb.txt";
            imgfilelist = File.ReadAllLines(imgFileTxt);
        }

        //여기서 서버에 커넥트시 전송할 스트링을 생성함. 그런데 이게 아직 이용되는거?
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.AppData.Address + "/Connect?port=40003");
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
        string addr2 = SystemManager.Instance.AppData.Address + "/Store?keyword=DeviceConnect&id=0&src=" + SystemManager.Instance.User.UserName;
        string msg2 = SystemManager.Instance.User.UserName + "," + SystemManager.Instance.User.MapName;
        byte[] bdatab = System.Text.Encoding.UTF8.GetBytes(msg2);
        float[] fdataa = SystemManager.Instance.IntrinsicData;
        int nByte = 5;
        byte[] bdata2 = new byte[nByte + fdataa.Length * 4 + bdatab.Length];
        bdata2[fdataa.Length * 4] = SystemManager.Instance.User.ModeMapping ? (byte)1 : (byte)0;
        bdata2[fdataa.Length * 4 + 1] = SystemManager.Instance.User.ModeTracking ? (byte)1 : (byte)0;
        bdata2[fdataa.Length * 4 + 2] = SystemManager.Instance.User.UseGyro ? (byte)1 : (byte)0;
        bdata2[fdataa.Length * 4 + 3] = SystemManager.Instance.User.bSaveTrajectory ? (byte)1 : (byte)0;
        bdata2[fdataa.Length * 4 + 4] = SystemManager.Instance.User.ModeAsyncQualityTest ? (byte)1 : (byte)0;
        Buffer.BlockCopy(fdataa, 0, bdata2, 0, fdataa.Length * 4);
        Buffer.BlockCopy(bdatab, 0, bdata2, fdataa.Length * 4+nByte, bdatab.Length);

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
                        SystemManager.Instance.AppData.numFeatures,SystemManager.Instance.AppData.numPyramids, 1.2f, SystemManager.Instance.AppData.numSkipFrames, SystemManager.Instance.AppData.numLocalKeyFrames);
        SetUserName(SystemManager.Instance.User.UserName.ToCharArray(), SystemManager.Instance.User.UserName.Length);

        tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.ARGB32, false);//BGRA32
        
        if (SystemManager.Instance.User.UseCamera)
        {
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

        nSkip = SystemManager.Instance.AppData.numSkipFrames;
        nQuality = SystemManager.Instance.AppData.JpegQuality;

        ConnectDevice();
    }

    public void Disconnect()
    {
        if (bAccessCamera)
        {
            webCamTexture.Stop();
        }

        ////Device & Map store
        string addr2 = SystemManager.Instance.AppData.Address + "/Store?keyword=DeviceDisconnect&id=0&src=" + SystemManager.Instance.User.UserName;
        string msg2 = SystemManager.Instance.User.UserName + "," + SystemManager.Instance.User.MapName;
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);

        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        string addr3 = SystemManager.Instance.AppData.Address + "/Disconnect?src=" + SystemManager.Instance.User.UserName+"&type=device";
        UnityWebRequest request3 = new UnityWebRequest(addr3);
        request3.method = "POST";
        //UploadHandlerRaw uH3 = new UploadHandlerRaw(bdata3);
        //uH3.contentType = "application/json";
        //request3.uploadHandler = uH3;
        request3.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res3 = request3.SendWebRequest();

        while (!request.downloadHandler.isDone)//&& !request3.downloadHandler.isDone
        {
            continue;
        }

        DisconnectDevice();
        UnityEngine.Object.DestroyImmediate(tex);
        
    }

    public void LoadWebcamTextre()
    {
        Color[] cdata = webCamTexture.GetPixels();
        tex.SetPixels(cdata);
        tex.Apply();
    }

    public void LoadRawTextureData(IntPtr ptr, int size) {
        tex.LoadRawTextureData(ptr, size);
        tex.Apply();
    }

    ////PoseData
    public float[] PoseData
    {
        set
        {
            pose = value;
        }
        get
        {
            return pose;
        }
    }
    static private float[] pose;

}



public class TrackingProcessor : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)

    [DllImport("UnityLibrary")]
    private static extern bool Localization(IntPtr texdata, IntPtr posedata, int id, double ts, int nQuality, bool bTracking, bool bVisualization);
    [DllImport("UnityLibrary")]
    private static extern void TestUploaddata(byte[] data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetIMUAddress(IntPtr addr, bool bIMU);
    [DllImport("edgeslam")]
    private static extern bool Localization(IntPtr texdata, IntPtr posedata, int id, double ts, int nQuality, bool bTracking, bool bVisualization);
    [DllImport("edgeslam")]
    private static extern void TestUploaddata(byte[] data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
#endif

    public UnityEngine.UI.Text StatusTxt;
    public RawImage ResultImage;
    public RawImage background;
            
    GCHandle poseHandle;
    IntPtr posePtr;

    float[] fIMUPose;
    GCHandle imuHandle;
    IntPtr imuPtr;

    byte[] resData;
    GCHandle resHandle;
    IntPtr resPtr;

    int mnFrameID = 0;
    double ts = 1.0;
    Matrix3x3 DeltaR = new Matrix3x3();
    Matrix3x3 DeltaFrameR;

    DataTransfer sender;

    void Delay() {
        //yield return new WaitForSecondsRealtime(0.033333333333333333f);
    }

    void Start()
    {
        //result image ptr
        resData = new byte[SystemManager.Instance.ImageWidth * SystemManager.Instance.ImageHeight * 4];
        resHandle = GCHandle.Alloc(resData, GCHandleType.Pinned);
        resPtr = resHandle.AddrOfPinnedObject();

        ////pose ptr
        Tracker.Instance.PoseData = new float[12];
        poseHandle = GCHandle.Alloc(Tracker.Instance.PoseData, GCHandleType.Pinned);
        posePtr = poseHandle.AddrOfPinnedObject();
        ////pose ptr

        ////imu ptr
        fIMUPose = new float[12];
        imuHandle = GCHandle.Alloc(fIMUPose, GCHandleType.Pinned);
        imuPtr = imuHandle.AddrOfPinnedObject();
        ////imu ptr

        Tracker.Instance.Background = background;

        UnityEngine.Profiling.Profiler.enabled = true;

#if (UNITY_EDITOR_WIN)

#elif (UNITY_ANDROID)
        SetIMUAddress(imuPtr, SystemManager.Instance.User.UseGyro);
#endif
        sender = new DataTransfer();
    }

    void Update()
    {
        try
        {
            var A = DateTime.Now;
            ////자이로 센서 설정
            if (SystemManager.Instance.User.UseGyro)
            {
                DeltaFrameR = SensorManager.Instance.DeltaRotationMatrix();
                DeltaR = DeltaR * DeltaFrameR;
            }

            if (SystemManager.Instance.Start && SystemManager.Instance.Connect)
            {

                ////웹캠 카메라 텍스쳐 올리기
                if (SystemManager.Instance.User.UseCamera)
                {
                    Tracker.Instance.LoadWebcamTextre();
                    var timeSpan = DateTime.UtcNow - SystemManager.Instance.StartTime;
                    ts = timeSpan.TotalMilliseconds;
                }
                ////이미지 텍스쳐에 올리기
                else
                {
                    if(Tracker.Instance.ImageFrameIndexX >= Tracker.Instance.ImageList.Length)
                    {
                        Tracker.Instance.ImageFrameIndexX = 3;
                        SystemManager.Instance.Start = false;
                        return;
                    }
                    string[] strsplit = Tracker.Instance.ImageList[Tracker.Instance.ImageFrameIndexX++].Split(' ');
                    ts = Convert.ToDouble(strsplit[0]);
                    string file = Convert.ToString(strsplit[1]);
                    string imgFile = SystemManager.Instance.DataLists[SystemManager.Instance.User.numDataset] + file;
                    byte[] byteTexture = System.IO.File.ReadAllBytes(imgFile);
                    Tracker.Instance.Texture.LoadImage(byteTexture);
                }

                ////텍스쳐 정보 NDK에 올리기
                Color32[] texData = Tracker.Instance.Texture.GetPixels32();
                GCHandle texHandle = GCHandle.Alloc(texData, GCHandleType.Pinned);
                IntPtr texPtr = texHandle.AddrOfPinnedObject();

                if (SystemManager.Instance.User.UseGyro)
                    DeltaFrameR.Copy(ref fIMUPose, 0);
                var B = DateTime.Now;
                TimeSpan time2 = B - A;
                //StatusTxt.text = "\t\t\t\t\t Reference = " + time2.TotalMilliseconds;
                bool bSuccessTracking = Localization(texPtr, posePtr, ++mnFrameID, ts, Tracker.Instance.ImageQuality, SystemManager.Instance.User.ModeTracking, SystemManager.Instance.User.bVisualizeFrame);
                //if (SystemManager.Instance.User.ModeTracking)
                //{
                //    var C = DateTime.Now;
                //    TimeSpan time3 = C - B;
                //    //StatusTxt.text = StatusTxt.text + "  " + time3.TotalMilliseconds;// + "==" + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                //}
                if (SystemManager.Instance.User.bVisualizeFrame)
                    Tracker.Instance.LoadRawTextureData(texPtr, texData.Length * 4);
                
                if (bSuccessTracking)
                {
                    //////포즈 정보 획득 후 카메라 위치 변경
                    //{
                    //    var pdata = Tracker.Instance.PoseData;
                    //    Matrix3x3 R = new Matrix3x3(pdata[0], pdata[1], pdata[2], pdata[3], pdata[4], pdata[5], pdata[6], pdata[7], pdata[8]);
                    //    var mAxis = R.LOG(); mAxis.y = -mAxis.y;
                    //    float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
                    //    mAxis = mAxis.normalized;
                    //    Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
                    //    var Center = new Vector3(pdata[9], -pdata[10], pdata[11]);
                    //    //var Center = -(R.Transpose() * t);
                    //    ////이스크립터는 엣지 슬램 오브젝트와 연결되어 있음.

                    //    //gameObject.transform.forward = R.row3;
                    //    //gameObject.transform.position = Center;
                    //    gameObject.transform.rotation = rotation;
                    //    gameObject.transform.position = Center;

                    //    ////업데이트 카메라 포즈
                    //    //Vector3 mAxis = R.LOG();
                    //    //var DIR = mAxis;
                    //    //float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
                    //    //mAxis = mAxis.normalized;
                    //    //Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
                    //    //Quaternion q = Matrix3x3.RotToQuar(R);
                    //    //StatusTxt.text = "\t\t\t\t\t Camera Center = " + Center.x+" "+Center.y+" "+Center.z+q.ToString();
                    //}
                    //Camera.main.transform.rotation = 
                    ResultImage.color = new Color(0.0f, 1.0f, 0.0f, 4.0f);
                }
                else
                {
                    ResultImage.color = new Color(1.0f, 0.0f, 0.0f, 4.0f);
                }

                texHandle.Free();
                ////텍스쳐 정보 NDK에 올리기

                if (mnFrameID % Tracker.Instance.SkipFrame == 0) {

                    string src = SystemManager.Instance.User.UserName;
                    UdpData data = new UdpData("Image", src, mnFrameID, ts);
                    data.sendedTime = A;
                        DataQueue.Instance.Add(data);
                    
                    if (SystemManager.Instance.User.UseGyro)
                    {
                        //StatusTxt.text = DeltaR.m00 + " " + DeltaR.m11+" "+ DeltaR.m22;
                        float[] fdata = new float[9];
                        DeltaR.Copy(ref fdata, 0);
                        byte[] bdata = new byte[(fdata.Length) * 4];
                        Buffer.BlockCopy(fdata, 0, bdata, 0, fdata.Length * 4);

                        UdpData gdata = new UdpData("Gyro", src, mnFrameID, bdata, ts);
                        gdata.sendedTime = DateTime.Now;

                        //DataQueue.Instance.SendingQueue.Enqueue(gdata);
                        StartCoroutine(sender.SendData(gdata));
                        //전송할때까지 다시 자이로 값을 누적함.
                        DeltaR = new Matrix3x3();
                    }
                    
                }
                //var timeSpan2 = DateTime.Now - A;
                //StatusTxt.text = "\t\t\t\t\t" + timeSpan2.TotalMilliseconds;
                return;
            }
        }
        catch (Exception ex)
        {
            StatusTxt.text = ex.ToString();
        }
    }

}