using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchProcessor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    int mnTouchID = 0;
    // Update is called once per frame
    void Update()
    {
        ////touch 
        bool bTouch = false;
        Vector2 touchPos = Vector2.zero;
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

        if (bTouch && Tracker.Instance.BackgroundRect.Contains(touchPos))
        {
            ++mnTouchID;
            ////이 부분 어떻게든 바꾸고 싶다.
            ////데이터 보낼 때 시간을 기록
            SystemManager.ExperimentMap[] maps = SystemManager.Instance.ExperimentMaps;
            maps[3].Set(mnTouchID, DateTime.Now);
            SystemManager.Instance.ExperimentMaps = maps;
            ////이 부분 어떻게든 바꾸고 싶다.
            ////데이터 보낼 때 시간을 기록

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
            DataQueue.Instance.SendingQueue.Enqueue(data);
        }

        ////touch 
    }
}
