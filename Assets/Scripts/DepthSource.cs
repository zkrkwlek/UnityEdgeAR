using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DepthSource : MonoBehaviour
{
    static private int lastUpdatedID = -1;
    static private bool initialized = false;
    static private int w, h;
    private static Texture2D depthTexture;
    static private bool updating = false;

    static public bool Updating
    {
        get
        {
            return updating;
        }
    }
    static public Texture2D DepthTexture
    {
        get
        {
            ////data 체크
            return depthTexture;
        }
    }
    static public bool Initialized
    {
        get
        {
            return initialized;
        }
    }

    static public int Width
    {
        get
        {
            return w;
        }
        set
        {
            w = value;
        }
    }
    static public int Height
    {
        get
        {
            return h;
        }
        set
        {
            h = value;
        }
    }

    static private bool updated;
    static public bool Updated
    {
        get
        {
            return updated;
        }
        set
        {
            updated = value;
        }
    }
    static private byte[] data;
    static public byte[] Data;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!updating)
        {
            StartCoroutine("UpdateDepthTexture");
        }
    }

    IEnumerator UpdateDepthTexture()
    {
        yield return new WaitForEndOfFrame();
        string addr = CameraManager.ServerAddr + "/GetLastFrameID?map="+CameraManager.Map+ "&key=rdepth";

        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        //UploadHandlerRaw uH = new UploadHandlerRaw(abytes);
        //uH.contentType = "application/json";
        //request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        while (!request.isDone)
        {
            yield return new WaitForFixedUpdate();
        }
        JsonDeepData data = JsonUtility.FromJson<JsonDeepData>(request.downloadHandler.text);
        if(data.n != -1 && data.n != lastUpdatedID)
        {
            updating = true;
            lastUpdatedID = data.n;
            string addr2 = CameraManager.ServerAddr + "/SendData?map="+CameraManager.Map+"&id=" + lastUpdatedID + "&key=rdepth";
            UnityWebRequest request2 = new UnityWebRequest(addr2);
            request2.method = "POST";
            request2.downloadHandler = new DownloadHandlerBuffer();
            request2.SendWebRequest();
            while (!request2.isDone)
            {
                yield return new WaitForFixedUpdate();
            }
            ////이걸 텍스쳐화 해야함.
            Data = request2.downloadHandler.data;

            if (!initialized)
            {
                depthTexture = new Texture2D(w, h, TextureFormat.RFloat, false); //나중에 Rfloat으로 교체
            }
            Debug.Log("Depth ID = " + data.n + "::" + Data.Length);
            depthTexture.LoadImage(Data);
            depthTexture.Apply();
            if (!initialized)
                initialized = true;
            updated = true;
            updating = false;
        }
    }

}
