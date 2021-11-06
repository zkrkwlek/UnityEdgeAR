using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;

public class SensorData
{
    public SensorData()
    {
        data = Vector3.zero;
        dt = 0f;
        timestamp = 0;
    }
    public SensorData(Vector3 _data, float _dt, long _tp)
    {
        data = _data;
        dt = _dt;
        timestamp = _tp;
    }
    public Vector3 data;
    public float dt;
    public long timestamp;

};

public class SensorManager
{
    static Matrix3x3 R;
    static Vector3 t;
    static private AndroidJavaObject plugin;
    static private Queue<SensorData> mQueueGyro, mQueueAcc;
    static private bool bStart;
    static private SensorManager m_pInstance = null;
    static public SensorManager Instance
    {
        get {
            if (m_pInstance == null)
            {
                bStart = false;
                m_pInstance = new SensorManager();
                mQueueGyro = new Queue<SensorData>();
                mQueueAcc = new Queue<SensorData>();

                //plugin = new AndroidJavaClass("com.uvr.sensorplugin.SensorActivity").CallStatic<AndroidJavaObject>("GetInstance");
                
            }
            return m_pInstance;
        }

    }

    public AndroidJavaObject Plugin
    {
        get 
        {
            return plugin;
        }
    }

    public void Start()
    {
        if (bStart)
            return;
        bStart = true;
        plugin.Call("startSensorListening", SystemManager.Instance.SensorSpeed);
    }

    public void SetGyro(SensorData data)
    {
        //if (!mbInit)
        //    return;
        mQueueGyro.Enqueue(data);
    }
    public void SetAcc(SensorData data)
    {
        //if (!mbInit)
        //    return;
        mQueueAcc.Enqueue(data);
    }

    public Matrix3x3 DeltaRotationMatrix()
    {
        Matrix3x3 DeltaR = new Matrix3x3();

        List<SensorData> listGyro = new List<SensorData>();
        int N = mQueueGyro.Count;
        for (int i = 0; i <N; i++)
        {
            SensorData tgyroData;
            tgyroData = mQueueGyro.Dequeue();
            listGyro.Add(tgyroData);
        }

        for (int i = listGyro.Count - 1; i >= 0; i--)
        {
            Vector3 tempVec = listGyro[i].data;
            float dt = listGyro[i].dt;

            Matrix3x3 tempDeltaR = Matrix3x3.EXP(tempVec * dt);
            DeltaR = tempDeltaR * DeltaR;
        }
        return DeltaR;
    }

}