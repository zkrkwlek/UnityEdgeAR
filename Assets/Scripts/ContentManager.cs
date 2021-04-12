using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentManager
{
    static private ContentManager m_pInstance = null;
    static public ContentManager Instance
    {
        get
        {
            if (m_pInstance == null)
            {
                m_pInstance = new ContentManager();
                LoadContentNames();
            }
            return m_pInstance;
        }
    }

    static void LoadContentNames() {
        objs.Add("Prefab/Device.prefab");
        objs.Add("Prefab/Bullet.prefab");
        objs.Add("Prefab/GrassBlock.prefab");
    }

    public enum ContentIDs :int
    {
        Device = 0,
        Bullet = 1,
        GrassBlock = 2
    }

    static private List<string> objs = new List<string>();
    public List<string> ContentNames
    {
        get
        {
            return objs;
        }
    }

}
