using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UVRSystem : MonoBehaviour
{
        
    static GCHandle webCamHandle;
    static WebCamTexture webCamTexture;
    [HideInInspector]
    static public Color[] webCamColorData;
    static IntPtr webCamPtr;
    
    [MenuItem("UVRSystem/Connect")]
    static void Connect()
    {
        //id, map, mapping 여부, fx, fy, cx, cy, w, h
        SystemManager.Instance.LoadParameter();
        SystemManager.InitConnectData data = SystemManager.Instance.GetConnectData();
        string msg = JsonUtility.ToJson(data);
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

        UnityWebRequest request = new UnityWebRequest(SystemManager.Instance.ServerAddr+"/Connect");
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }

    [MenuItem("UVRSystem/Disconnect")]
    static void Disconnect()
    {
        string addr = SystemManager.Instance.ServerAddr + "/Disconnect?userID=" + SystemManager.Instance.User;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
        Debug.Log("Disconnect!!"+ addr);
    }

    [MenuItem("UVRSystem/Reset")]
    static void Reset()
    {
        Debug.Log("Doing Something...");
    }

    [MenuItem("UVRSystem/Save Map")]
    static void SaveMap()
    {
        Debug.Log("Doing Something...");
    }

    [MenuItem("UVRSystem/Load Map")]
    static void LoadMap()
    {
        string addr = SystemManager.Instance.ServerAddr + "/LoadMap?map=" + SystemManager.Instance.Map;
        UnityWebRequest request = new UnityWebRequest(addr);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();
    }
}
