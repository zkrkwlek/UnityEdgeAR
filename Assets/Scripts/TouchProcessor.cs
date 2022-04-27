using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class TouchProcessor : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)
    [DllImport("UnityLibrary")]
    private static extern void TestUploaddata(byte[] data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
    [DllImport("UnityLibrary")]
    private static extern void TouchProcessInit(int id, float x, float y, double ts);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void TouchProcessInit(int id, float x, float y, double ts);
    [DllImport("edgeslam")]
    private static extern void TestUploaddata(byte[] data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
#endif
    // Start is called before the first frame update
    void Start()
    {
        sender = new DataTransfer();
    }
    DataTransfer sender;
    int mnTouchID = 0;
    // Update is called once per frame
    void Update()
    {
        try {
            ////touch 
            bool bTouch = false;
            Vector2 touchPos = Vector2.zero;

#if UNITY_EDITOR_WIN

#elif UNITY_ANDROID
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchPos = touch.position;
                bTouch = true;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                touchPos = touch.position;
                bTouch = true;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                touchPos = touch.position;
                bTouch = true;
            }
#endif

            if (bTouch && Tracker.Instance.BackgroundRect.Contains(touchPos))
            {

                var timeSpan = DateTime.UtcNow - SystemManager.Instance.StartTime;
                ++mnTouchID;

                float[] fdata = new float[3];
                float scale = Tracker.Instance.Scale;
                float width = Tracker.Instance.Diff.x;
                float height = Tracker.Instance.Diff.y;
                fdata[0] = (touchPos.x - width) / scale;
                fdata[1] = (height - touchPos.y) / scale;
                fdata[2] = 1.0f;
                TouchProcessInit(mnTouchID, fdata[0], fdata[1], timeSpan.TotalMilliseconds);

                ////임시 사용
                float[] fPoseData = Tracker.Instance.PoseData;
                byte[] bdata = new byte[(fPoseData.Length + fdata.Length) * 4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, fdata.Length * 4);
                Buffer.BlockCopy(fPoseData, 0, bdata, fdata.Length * 4, fPoseData.Length * 4);

                UdpData data = new UdpData("ContentGeneration", SystemManager.Instance.User.UserName, mnTouchID, bdata, timeSpan.TotalMilliseconds);
                data.sendedTime = DateTime.Now;
                DataQueue.Instance.Add(data);
                ////임시 사용
                //string keyword = "ContentGeneration";
                ////TestUploaddata(bdata, bdata.Length, mnTouchID, keyword.ToCharArray(), keyword.Length, SystemManager.Instance.User.UserName.ToCharArray(), SystemManager.Instance.User.UserName.Length, 0.0);
                //StartCoroutine(sender.SendData(data));
            }
            ////touch 
        }
        catch (Exception e)
        {
            
        }
        
    }

}
