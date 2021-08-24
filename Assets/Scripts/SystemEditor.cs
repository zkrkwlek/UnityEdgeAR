#if(UNITY_EDITOR_WIN)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[ExecuteInEditMode]
[CustomEditor(typeof(CloudSystem))]
public class SystemEditor : Editor
{
    public CloudSystem mSystem;
    //public MapManager mMapManager = new MapManager();

    private void OnEnable()
    {
        if (AssetDatabase.Contains(target))
        {
            mSystem = null;
        }
        else
        {
            mSystem = (CloudSystem)target;
        }
    }
    float fx, fy, fz;
    float nContentID = 0f;

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
        SystemManager.Instance.IsServerMapping = EditorGUILayout.Toggle(SystemManager.Instance.IsServerMapping, GUILayout.Width(30f));
        EditorGUILayout.LabelField("Manager", GUILayout.Width(50f));
        SystemManager.Instance.Manager = EditorGUILayout.Toggle(SystemManager.Instance.Manager, GUILayout.Width(30f));
        GUILayout.EndHorizontal();

        //mSystem.runInEditMode = EditorGUILayout.TextField("Server Address", SystemManager.Instance.ServerAddr);
        //base.OnInspectorGUI();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("File Select", GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanel("Parameter file", Application.persistentDataPath, "txt");
            Debug.Log(path);
            SystemManager.Instance.LoadParameter(path);
            mSystem.tex = new Texture2D(SystemManager.Instance.ImageWidth, SystemManager.Instance.ImageHeight, TextureFormat.RGB24, false);
            mSystem.nImgFrameIDX = 3;

            mSystem.imageData = SystemManager.Instance.ImageData;
            mSystem.imagePath = SystemManager.Instance.ImagePath;
            mSystem.nMaxImageIndex = mSystem.imageData.Length - 1;
            mSystem.Init();
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

        GUILayout.BeginHorizontal();

        //if (SystemManager.Instance.Start)
        //{
        //    if (GUILayout.Button("Stop", GUILayout.Width(100)))
        //    {
        //        //selected.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
        //        mSystem.ThreadStop();
        //    }
        //}
        //else
        //{
        //    if (GUILayout.Button("Start", GUILayout.Width(100)))
        //    {
        //        //selected.transform.localScale = Vector3.one * Random.Range(0.5f, 1f);
        //        mSystem.ThreadStart();
        //    }
        //}
        string btnname = "STOP";
        if (!SystemManager.Instance.Start)
        {
            btnname = "START";
        }

        if (GUILayout.Button(btnname, GUILayout.Width(100)))
        {
            //mSystem.ThreadPause();
            Debug.Log("test = " + SystemManager.Instance.Start);
            SystemManager.Instance.Start = !SystemManager.Instance.Start;
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

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Content ID = ", GUILayout.Width(75f));
        nContentID = EditorGUILayout.FloatField(nContentID, GUILayout.Width(75f));
        if (GUILayout.Button("Echo test : send", GUILayout.Width(100)))
        {
            //float[] fdata = new float[8];
            //int nIdx = 0;
            //fdata[nIdx++] = 2f;         //type : 나중에 다른 것이 추가될 수 있기 떄문에. 콘텐츠만이 아닌 다른 사용자의 위치를 전달할 때 등
            //fdata[nIdx++] = nContentID; //content id : ray 뿐만이 아닌 배치 이동 등을 수행할 때 구분하기 위함.
            //if (mSystem.nFirstKey != -1)
            //{
            //    Transform tran = mSystem.mConnectedDevices[mSystem.nFirstKey].transform;
            //    tran.ro
            //}
            //fdata[nIdx++] = mSystem.Center.x;
            //fdata[nIdx++] = mSystem.Center.y;
            //fdata[nIdx++] = mSystem.Center.z;
            //fdata[nIdx++] = mSystem.DIR.x;
            //fdata[nIdx++] = mSystem.DIR.y;
            //fdata[nIdx++] = mSystem.DIR.z;
            //byte[] bdata = new byte[fdata.Length*4];
            //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
            //mSystem.mListUDPs[0].udp.Send(bdata, bdata.Length);
            ////AsyncSocketReceiver.Instance.SendData(bdata);//"143.248.6.143", 35001, 
            ////메세지 보낸 후 받은 곳에서 생성하기
        }
        GUILayout.EndHorizontal();
    }
}
#endif