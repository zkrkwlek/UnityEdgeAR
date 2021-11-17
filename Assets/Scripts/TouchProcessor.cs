using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TouchProcessor : MonoBehaviour
{
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
            ++mnTouchID;
            
            float[] fdata = new float[3];
            float scale = Tracker.Instance.Scale;
            float width = Tracker.Instance.Diff.x;
            float height = Tracker.Instance.Diff.y;
            fdata[0] = (touchPos.x - width) / scale;
            fdata[1] = (height - touchPos.y) / scale;
            fdata[2] = 1.0f;

            float[] fPoseData = Tracker.Instance.PoseData;

            byte[] bdata = new byte[(fPoseData.Length + fdata.Length) * 4];
            Buffer.BlockCopy(fdata, 0, bdata, 0, fdata.Length * 4);
            Buffer.BlockCopy(fPoseData, 0, bdata, fdata.Length * 4, fPoseData.Length * 4);
            
            UdpData data = new UdpData("ContentGeneration", SystemManager.Instance.UserName, mnTouchID, bdata);
            data.sendedTime = DateTime.Now;
            DataQueue.Instance.Add(data);
            //DataQueue.Instance.SendingQueue.Enqueue(data);
            StartCoroutine(sender.SendData(data));
        }
        ////touch 
    }

}
