using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[CustomEditor(typeof(UVRSystem))]
public class SystemEditor : Editor
{
    public UVRSystem mSystem;


    private void OnEnable()
    {
        if (AssetDatabase.Contains(target))
        {
            mSystem = null;
        }
        else
        {
            mSystem = (UVRSystem)target;
        }
    }
    bool bConnect = false;
    public override void OnInspectorGUI()
    {
        SystemManager.Instance.User = EditorGUILayout.TextField("User Name", SystemManager.Instance.User);
        SystemManager.Instance.Map = EditorGUILayout.TextField("Map Name", SystemManager.Instance.Map);
        SystemManager.Instance.ServerAddr = EditorGUILayout.TextField("Server Address", SystemManager.Instance.ServerAddr);
        EditorGUILayout.TextField("Dataset Path", SystemManager.Instance.ImagePath);
        mSystem.nImgFrameIDX = EditorGUILayout.IntField("Image Index", mSystem.nImgFrameIDX);

        GUILayout.BeginHorizontal("toggle1");
        SystemManager.Instance.Connect = EditorGUILayout.Toggle("Connect", SystemManager.Instance.Connect);
        SystemManager.Instance.Mapping = EditorGUILayout.Toggle("Mapping", SystemManager.Instance.Mapping);
        GUILayout.EndHorizontal();

        //mSystem.runInEditMode = EditorGUILayout.TextField("Server Address", SystemManager.Instance.ServerAddr);
        //base.OnInspectorGUI();

        GUILayout.BeginHorizontal("button1");
        if (GUILayout.Button("File Select", GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanel("Parameter file", "C:/Users/UVR-KAIST/AppData/LocalLow/DefaultCompany/EdgeSLAM_19_1", "txt");
            SystemManager.Instance.LoadParameter(path);
            mSystem.background = (RawImage)GameObject.Find("background").GetComponent<RawImage>();
            mSystem.tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
            mSystem.nImgFrameIDX = 3;

            mSystem.imageData = SystemManager.Instance.ImageData;
            mSystem.imagePath = SystemManager.Instance.ImagePath;
            mSystem.nMaxImageIndex = mSystem.imageData.Length;
            Debug.Log(mSystem.nMaxImageIndex);
        }

        if (SystemManager.Instance.Connect)
        {
            if (GUILayout.Button("Disconnect", GUILayout.Width(100)))
            {
                SystemManager.Instance.Connect = false;
                mSystem.Disconnect();
            }
        }
        else
        {
            if (GUILayout.Button("Connect", GUILayout.Width(100)))
            {
                SystemManager.Instance.Connect = true;
                mSystem.Connect();
            }
        }

        if (GUILayout.Button("Save Map", GUILayout.Width(100)))
        {
            mSystem.SaveMap();
        }

        if (GUILayout.Button("Load Map", GUILayout.Width(100)))
        {
            mSystem.LoadMap();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal("button2");

        if (mSystem.bThreadStart)
        {
            if (GUILayout.Button("Stop", GUILayout.Width(100)))
            {
                //selected.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
                mSystem.ThreadStop();
            }
        }
        else
        {
            if (GUILayout.Button("Start", GUILayout.Width(100)))
            {
                //selected.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
                mSystem.ThreadStart();
            }
        }
        string btnname = "Pause";
        if (mSystem.bWaitThread)
        {
            btnname = "Resume";
        }
        
        if (GUILayout.Button(btnname, GUILayout.Width(100)))
        {
            mSystem.ThreadPause();
        }
        GUILayout.EndHorizontal();
    }
}
