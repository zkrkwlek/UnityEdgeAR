using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SendMessage : MonoBehaviour
{
    //public UnityEngine.UI.Text StatusTxt;
    CameraManager mCamManager;
    System.Diagnostics.Stopwatch watch;
    float progress = 0f;
    int downloadprogress = 0;
    IEnumerator runCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        watch = new System.Diagnostics.Stopwatch();
        mCamManager = FindObjectOfType<CameraManager>();
        //runCoroutine = SendImageData();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (mCamManager.mMessageQueue.Count > 0)
        {
            Tuple<string, byte[], string, int> tmsg;
            bool bQueue = mCamManager.mMessageQueue.TryDequeue(out tmsg);
            if (bQueue)
            {
                StartCoroutine(SendNewImage(tmsg));
            }
        }
       
        //runCoroutine.MoveNext();
    }
    

    IEnumerator SendNewImage(Tuple<string, byte[], string, int> message)
    {
        yield return new WaitForFixedUpdate();
        
        string addr = message.Item1;
        //string msg = message.Item2;
        string method = message.Item3;
        //watch.Start();
        byte[] abytes = message.Item2;//System.Text.Encoding.UTF8.GetBytes(msg);
        UnityWebRequest request = new UnityWebRequest(addr);
        //request.SetRequestHeader("Content-Type", "application/json");
        request.method = method;
        UploadHandlerRaw uH = new UploadHandlerRaw(abytes);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        
        watch.Restart();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        //while (!res.isDone)
        //{
        //    yield return new WaitForFixedUpdate();
        //    progress = res.progress;
        //}
        //while (request.uploadProgress < 1.0f)
        while(request.uploadHandler.progress < 1f)
        {
            yield return new WaitForFixedUpdate();
            progress = request.uploadHandler.progress;
        }
        mCamManager.mbSended = true;
        watch.Stop();
        int type = message.Item4;
        if(type == 2)
        {
            //while (request.downloadProgress < 1.0f)
            while(!request.downloadHandler.isDone)
            {
                yield return new WaitForFixedUpdate();
                //downloadprogress = request.downloadProgress;
            }
            byte[] data = request.downloadHandler.data;
            downloadprogress = data.Length;
            //Debug.Log(data.ToString());

            //JsonDeepData data = JsonUtility.FromJson<JsonDeepData>(request.downloadHandler.text);
        }

        //        yield return request.SendWebRequest();
        //        int type = message.Item3;
        //        if (request.isNetworkError || request.isHttpError)
        //        {
        //            //StatusTxt.text = "send::error=";
        //        }
        //        else
        //        {

        //            if (type == 1)
        //            {
        //                try {
        //                    mCamManager.mbSended = true;
        //                    JsonDeepData data = JsonUtility.FromJson<JsonDeepData>(request.downloadHandler.text);
        //                    mCamManager.mHashIDSet.Add(data.id);
        //                    //StatusTxt.text = "send::image=" + data.id;
        ////#if UNITY_EDITOR_WIN
        ////                    Debug.Log("Results::SendImage= " + data.id + "::" + data.n);
        ////#endif
        //                }
        //                catch (Exception e)
        //                {
        //                    //StatusTxt.text = "???"+e.ToString();
        //                }

        //            }else if(type == 2)
        //            {
        //                JsonSLAMData data = JsonUtility.FromJson<JsonSLAMData>(request.downloadHandler.text);
        //                mCamManager.mbInitialized = data.init;
        //                mCamManager.mnFrameID1 = data.id1;
        //                mCamManager.mbDetected = true;
        //                if (data.init)
        //                {
        //                    byte[] bpose = Convert.FromBase64String(data.pose);
        //                    float[] fpose = new float[bpose.Length / 4];
        //                    Buffer.BlockCopy(bpose, 0, fpose, 0, bpose.Length);

        //                    byte[] bkey = Convert.FromBase64String(data.keypoints);
        //                    float[] fkey = new float[bkey.Length / 4];
        //                    Buffer.BlockCopy(bkey, 0, fkey, 0, bkey.Length);
        //#if UNITY_EDITOR_WIN
        //                    UnityEngine.Debug.Log("SLAM = " + data.id1 + "::" + fkey[4] + "::" + fkey[10]);
        //#endif
        //                }

        //                //StatusTxt.text = "send::detect=" + data.id1 + "::" + data.n;
        //            }

        //            else if(type == 3)
        //            {
        //                JsonSLAMData data = JsonUtility.FromJson<JsonSLAMData>(request.downloadHandler.text);
        //                mCamManager.mbInitialized = data.init;
        //                mCamManager.mnFrameID1 = data.id1;
        //#if UNITY_EDITOR_WIN
        //                UnityEngine.Debug.Log("SLAM ref id = " + data.id1);
        //#endif
        //            }else if(type == 0)
        //            {

        //            }

        //        }
        //        watch.Stop();
    }//send new image
    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 80;
        Rect rect = new Rect(10, style.fontSize + 5, w, h * 2 / 100);
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        string text = string.Format("Frame={0}:{1:0.0}ms,{2}bytes, MP={3}", mCamManager.mnFrameID, watch.ElapsedMilliseconds, downloadprogress, PoseOptimizer.NumMatch);
        GUI.Label(rect, text, style);
    }

    //IEnumerator SendImageData()
    //{
    //    UnityWebRequest request = new UnityWebRequest("http://143.248.96.81:35005/receiveimage");
    //    request.SetRequestHeader("Content-Type", "application/json");
    //    WaitUntil condition = new WaitUntil(()=>(mCamManager.mnFrameID > 0 && mCamManager.mnFrameID % 3 == 0));
    //    int w, h;
    //    w = 640;
    //    h = 360;
    //    Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    //    while (true)
    //    {
    //        yield return condition;
    //        int nFrame = mCamManager.mnFrameID;
    //        tex.SetPixels32(mCamManager.webCamColorData);
    //        webCamByteData = tex.EncodeToJPG();
    //        string encodedText = Convert.ToBase64String(webCamByteData);
    //        ImageData data = new ImageData(nFrame, encodedText, w, h, 3);
    //        string jsonStr = JsonUtility.ToJson(data);

    //        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonStr);
    //        using (UploadHandlerRaw uH = new UploadHandlerRaw(bytes))
    //        {
    //            //request.SetRequestHeader("Content-Type", "application/json");
    //            request.method = "POST";
    //            //uH.contentType = "application/json";
    //            request.uploadHandler = uH;
    //            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    //            yield return request.SendWebRequest();
    //            if (request.isNetworkError || request.isHttpError)
    //            {
    //                StatusTxt.text = "send::error="+nFrame;
    //            }
    //            else
    //            {
    //                StatusTxt.text = "send::complete=" + nFrame;
    //            }

    //        }
    //    }
    //}
}