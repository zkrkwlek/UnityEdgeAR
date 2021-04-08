//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using UnityEngine;
//using UnityEngine.Networking;

//public class TrackingFrame
//{
//    public TrackingFrame() { }
//    public TrackingFrame(int _id, string _ts)
//    {
//        id = _id;
//        ts = _ts;
//    }
//    public int id;
//    public string ts;
//}

//public class TrckingManager : MonoBehaviour
//{

//    [DllImport("edgeslam")]
//    private static extern void SetTrackingInformation(char[] ts, float[] pose, float[] data, int n);

//    int nLastID;
//    bool bReceiveTracking = false;
//    public UnityEngine.UI.Text StatusTxt;

//    private static Matrix3x3 r;
//    private static Vector3 t;
//    public static Matrix3x3 R
//    {
//        get
//        {
//            return r;
//        }
//    }
//    public static Vector3 T
//    {
//        get
//        {
//            return t;
//        }
//    }

//    // Start is called before the first frame update
//    void Start()
//    {
//        nLastID = -1;
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if(!bReceiveTracking)
//            StartCoroutine("UpdateTrackingInformation");
//    }

//    IEnumerator UpdateTrackingInformation()
//    {
//        yield return new WaitForEndOfFrame();
//        string addr = CameraManager.ServerAddr + "/SendFrameID?user="+CameraManager.User+"&map="+CameraManager.Map;

//        UnityWebRequest request = new UnityWebRequest(addr);
//        request.method = "POST";
//        request.downloadHandler = new DownloadHandlerBuffer();
//        UnityWebRequestAsyncOperation res = request.SendWebRequest();
//        while (!request.isDone)
//        {
//            yield return new WaitForFixedUpdate();
//        }
//        TrackingFrame data = JsonUtility.FromJson<TrackingFrame>(request.downloadHandler.text);

//        if(data.id != nLastID)
//        {
//            bReceiveTracking = true;
//            nLastID = data.id;
//            Debug.Log("Tracking ID = " + data.id+"::"+data.ts);
//            string addr2 = CameraManager.ServerAddr + "/SendData?map=" + CameraManager.Map + "&id=" + data.id + "&key=btracking";
//            UnityWebRequest request2 = new UnityWebRequest(addr2);
//            request2.method = "POST";
//            request2.downloadHandler = new DownloadHandlerBuffer();
//            request2.SendWebRequest();

//            string addr3 = CameraManager.ServerAddr + "/SendData?map=" + CameraManager.Map + "&id=" + data.id + "&key=bpose";
//            UnityWebRequest request3 = new UnityWebRequest(addr3);
//            request3.method = "POST";
//            request3.downloadHandler = new DownloadHandlerBuffer();
//            request3.SendWebRequest();

//            while (!request2.isDone)
//            {
//                yield return new WaitForFixedUpdate();
//            }
//            byte[] bdata = request2.downloadHandler.data;
//            float[] framedata = new float[bdata.Length / 4];
//            Buffer.BlockCopy(bdata, 0, framedata, 0, bdata.Length);

//            while (!request3.isDone)
//            {
//                yield return new WaitForFixedUpdate();
//            }

//            byte[] bdata2 = request3.downloadHandler.data;
//            float[] framepose = new float[bdata2.Length / 4];
//            Buffer.BlockCopy(bdata2, 0, framepose, 0, bdata2.Length);
//            //Debug.Log(framedata.Length+" "+framepose.Length);

//            ////카메라 자세 획득 및 카메라 위치 추정
//            r = new Matrix3x3(framepose[0], framepose[1], framepose[2], framepose[3], framepose[4], framepose[5], framepose[6], framepose[7], framepose[8]);
//            t = new Vector3(framepose[9], framepose[10], framepose[11]);
//            Vector3 Center = -(r.Transpose() * t);

//            ////업데이트 카메라 포즈
//            Vector3 mAxis = r.LOG();
//            float mAngle = mAxis.magnitude * Mathf.Rad2Deg;
//            mAxis = mAxis.normalized;
//            Quaternion rotation = Quaternion.AngleAxis(mAngle, mAxis);
//            Camera.main.transform.SetPositionAndRotation(Center, rotation);

//            ////여기서 데이터 보내기. 타임스탬프, 포즈, 매칭 정보
//            if (Application.platform == RuntimePlatform.Android)
//                SetTrackingInformation(data.ts.ToCharArray(), framepose, framedata, framedata.Length);
//            bReceiveTracking = false;
//        }

//    }
//}
