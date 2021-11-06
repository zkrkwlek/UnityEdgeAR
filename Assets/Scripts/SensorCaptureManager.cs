
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorCaptureManager : MonoBehaviour
{
    
    private AndroidJavaObject plugin;
    public UnityEngine.UI.Text StatusTxt;

    public void Init()
    {
        try
        {

            plugin = new AndroidJavaClass("com.uvr.sensorplugin.SensorActivity").CallStatic<AndroidJavaObject>("GetInstance");
            plugin.Call("addSensor", 1, "SensorCapture", "RawGyroResult");
            //SensorManager.Instance.Plugin.Call("addSensor", 2, "SensorCapture", "RawAccResult");

            //plugin.Call("addSensor", 3, "SensorCapture", "GraResult");

            //plugin.Call("addSensor", 4, "SensorCapture", "RawMagResult");
            //plugin.Call("addSensor", 5, "SensorCapture", "RawLinearAccResult");

            //plugin.Call("addSensor", 4, "SensorCapture", "PoseResult");
            //plugin.Call("addSensor", 5, "SensorCapture", "StepResult");
            //plugin.Call("addSensor", 6, "SensorCapture", "StationaryResult");

            //SENSOR_DELAY_FASTEST 0
            //SENSOR_DELAY_GAME 1
            //SENSOR_DELAY_UI 2
            //SENSOR_DELAY_NORMAL 3 
            plugin.Call("startSensorListening", SystemManager.Instance.SensorSpeed);
            
        }
        catch(System.Exception e)
        {
            StatusTxt.text = e.ToString();
        }
        
    }

    // Start is called before the first frame update
    void Start()
    {

#if UNITY_EDITOR_WIN
#elif UNITY_ANDROID
        Init();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //public void GraResult(string res)
    //{
    //    //graTxt.text = "And::gra::" + res;
    //    string[] strs = res.Split(' ');
    //    Vector3 vGra = new Vector3(Convert.ToSingle(strs[0]), Convert.ToSingle(strs[1]), Convert.ToSingle(strs[2]));
    //    graDt = Convert.ToSingle(strs[3]);
    //    long timestamp = Convert.ToInt64(strs[4]);
    //    SensorData tempSensorData = new SensorData(vGra, graDt, timestamp);

    //    //if (VIOManager.mbSensor)
    //    //{
    //    //    GravityProcessor.SetCameraInitFromGravity(vGra);
    //    //    GravityProcessor.SetRawGravity(vGra);
    //    //}

    //    //SensorPoseManager.SetCameraPoseFromGravity(vGra);

    //    SensorPoseManager.SetGra(tempSensorData);
    //}


    public void RawAccResult(string res)
    {
        string[] strs = res.Split(' ');
        Vector3 vAcc = new Vector3(System.Convert.ToSingle(strs[0]), System.Convert.ToSingle(strs[1]), System.Convert.ToSingle(strs[2]));
        //가장 최근 이용
        //Vector3 vAcc = new Vector3(Convert.ToSingle(strs[1]), Convert.ToSingle(strs[0]), Convert.ToSingle(strs[2]));
        //190822 부호 확인 및 android와 camera간의 코디네잇을 맞출 필요가 있음.
        //Vector3 vAcc = new Vector3(-Convert.ToSingle(strs[1]), -Convert.ToSingle(strs[0]), -Convert.ToSingle(strs[2]));

        float dt = System.Convert.ToSingle(strs[3]);
        long timestamp = System.Convert.ToInt64(strs[4]);
        SensorData tempSensorData = new SensorData(vAcc, dt, timestamp);
        //AppDataManager.SetTxt("TimeStamp=" + timestamp, AppDataManager.ErrTxt);
        //SensorPoseManager.SetLinearAcc(vAcc, dt);
        
        SensorManager.Instance.SetAcc(tempSensorData);
        
    }
    public void RawGyroResult(string res)
    {
        string[] strs = res.Split(' ');
        
        //아예 변경하지 않은 것
        //유니티?
        //Vector3 gyroRes = new Vector3(System.Convert.ToSingle(strs[0]), System.Convert.ToSingle(strs[1]), System.Convert.ToSingle(strs[2]));
        //가장 최근 이용
        Vector3 gyroRes = new Vector3(System.Convert.ToSingle(strs[1]), System.Convert.ToSingle(strs[0]), System.Convert.ToSingle(strs[2]));
        //190822 부호 확인 및 android와 camera간의 코디네잇을 맞출 필요가 있음.
        //Vector3 gyroRes = new Vector3(-Convert.ToSingle(strs[1]), Convert.ToSingle(strs[0]), -Convert.ToSingle(strs[2]));


        float dt = System.Convert.ToSingle(strs[3]);
        long timestamp = System.Convert.ToInt64(strs[4]);
        SensorData tempSensorData = new SensorData(gyroRes, dt, timestamp);
        //SensorPoseManager.SetGyro(gyroRes, gyroDt);
        SensorManager.Instance.SetGyro(tempSensorData);
        
    }
}
