using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemManager {
    static private SystemManager m_pInstance = null;
    static public SystemManager Instance
    {
        get{
            if (m_pInstance == null)
            {
                m_pInstance = new SystemManager();
            }
            return m_pInstance;
        }
    }
    static public void LoadParameter()
    {

    }
}
