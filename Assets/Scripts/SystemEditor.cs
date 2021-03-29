using System;
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
    float fx, fy, fz;
    bool bConnect = false;
    public override void OnInspectorGUI()
    {
        SystemManager.Instance.User = EditorGUILayout.TextField("User Name", SystemManager.Instance.User);
        SystemManager.Instance.Map = EditorGUILayout.TextField("Map Name", SystemManager.Instance.Map);
        SystemManager.Instance.ServerAddr = EditorGUILayout.TextField("Server Address", SystemManager.Instance.ServerAddr);
        EditorGUILayout.TextField("Dataset Path", SystemManager.Instance.ImagePath);
        mSystem.nImgFrameIDX = EditorGUILayout.IntField("Image Index", mSystem.nImgFrameIDX);
        EditorGUILayout.IntField("Target Frame ID", mSystem.nTargetID);
        EditorGUILayout.IntField("Reference Frame ID", mSystem.nRefID);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Connect", GUILayout.Width(50f));
        SystemManager.Instance.Connect = EditorGUILayout.Toggle(SystemManager.Instance.Connect, GUILayout.Width(30f));
        EditorGUILayout.LabelField("Mapping", GUILayout.Width(50f));
        SystemManager.Instance.Mapping = EditorGUILayout.Toggle(SystemManager.Instance.Mapping, GUILayout.Width(30f));
        EditorGUILayout.LabelField("Manager", GUILayout.Width(50f));
        SystemManager.Instance.Manager = EditorGUILayout.Toggle(SystemManager.Instance.Manager, GUILayout.Width(30f));
        GUILayout.EndHorizontal();

        //mSystem.runInEditMode = EditorGUILayout.TextField("Server Address", SystemManager.Instance.ServerAddr);
        //base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("File Select", GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanel("Parameter file", "C:/Users/UVR-KAIST/AppData/LocalLow/DefaultCompany/EdgeSLAM_19_1", "txt");
            SystemManager.Instance.LoadParameter(path);
            mSystem.background = (RawImage)GameObject.Find("background").GetComponent<RawImage>();
            mSystem.tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
            mSystem.nImgFrameIDX = 3;

            mSystem.imageData = SystemManager.Instance.ImageData;
            mSystem.imagePath = SystemManager.Instance.ImagePath;
            mSystem.nMaxImageIndex = mSystem.imageData.Length-1;
            mSystem.Init();
        }

        if (SystemManager.Instance.Connect)
        {
            if (GUILayout.Button("Disconnect", GUILayout.Width(100)))
            {
                SystemManager.Instance.Connect = false;
                mSystem.Disconnect();
                mSystem.SocThreadStop();
                //AsyncSocketReceiver.Instance.SendMessage("Disconnect");
                AsyncSocketReceiver.Instance.Disconnect();
                
            }
        }
        else
        {
            if (GUILayout.Button("Connect", GUILayout.Width(100)))
            {
                SystemManager.Instance.Connect = true;
                AsyncSocketReceiver.Instance.Connect("143.248.6.143", 35001);
                mSystem.SocThreadStart();
                //AsyncSocketReceiver.Instance.ReceiveMessage();
                //AsyncSocketReceiver.Instance.SendMessage("Connect");//"143.248.6.143", 35001, 
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

        GUILayout.BeginHorizontal();

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

        if (GUILayout.Button("Reset", GUILayout.Width(100)))
        {
            mSystem.Reset();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Get Model", GUILayout.Width(100)))
        {
            mSystem.GetModel();
            //mSystem.ThreadStop();
        }

        mSystem.Bullet = (GameObject)EditorGUILayout.ObjectField("오브젝트", mSystem.Bullet, typeof(GameObject), true);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("POS", GUILayout.Width(75f));
        EditorGUILayout.FloatField(mSystem.Center.x, GUILayout.Width(75f));
        EditorGUILayout.FloatField(mSystem.Center.y, GUILayout.Width(75f));
        EditorGUILayout.FloatField(mSystem.Center.z, GUILayout.Width(75f));
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("DIR", GUILayout.Width(75f));
        mSystem.DIR.x = EditorGUILayout.FloatField(mSystem.DIR.x, GUILayout.Width(75f));
        mSystem.DIR.y = EditorGUILayout.FloatField(mSystem.DIR.y, GUILayout.Width(75f));
        mSystem.DIR.z = EditorGUILayout.FloatField(mSystem.DIR.z, GUILayout.Width(75f));
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Echo test : send", GUILayout.Width(100)))
        {
            float[] fdata = new float[6];
            fdata[0] = mSystem.Center.x;
            fdata[1] = mSystem.Center.y;
            fdata[2] = mSystem.Center.z;
            fdata[3] = mSystem.DIR.x;
            fdata[4] = mSystem.DIR.y;
            fdata[5] = mSystem.DIR.z;
            byte[] bdata = new byte[24];
            Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);

            AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 
            //메세지 보낸 후 받은 곳에서 생성하기
        }
    }
}
