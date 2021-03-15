using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PoseOptimizer : MonoBehaviour
{
    [DllImport("edgeslam")]
    private static extern int OpticalFlowMatching();
    [DllImport("edgeslam")]
    private static extern int PoseEstimation();

    public UnityEngine.UI.Text StatusTxt;

    private static int mnMatch = 0;
    public static int NumMatch
    {
        get
        {
            return mnMatch;
        }
    }
    bool bDoingProcess = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!bDoingProcess)
        {
            StartCoroutine("PoseOptimization");
        }
    }

    IEnumerator PoseOptimization()
    {
        bDoingProcess = true;
        yield return new WaitForFixedUpdate();

        try
        {
            if (Application.platform == RuntimePlatform.Android)
                mnMatch = OpticalFlowMatching();
        }
        catch (Exception e)
        {
            StatusTxt.text = e.ToString();
        }
        yield return new WaitForFixedUpdate();
        try
        {
            if (Application.platform == RuntimePlatform.Android)
                mnMatch = PoseEstimation();
        }
        catch (Exception e)
        {
            StatusTxt.text = e.ToString();
        }
        bDoingProcess = false;
    }
}
