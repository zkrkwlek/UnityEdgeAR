using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentData
{
    public ContentData()
    {

    }
    public ContentData(GameObject o, Vector3 p, Vector3 r)
    {
        pos = p;
        obj = o;
        float fAngle = r.magnitude * Mathf.Rad2Deg;
        q = Quaternion.AngleAxis(fAngle, r.normalized);
    }
    public ContentData(string _name, Vector3 p, Vector3 r)
    {
        obj = Resources.Load(_name) as GameObject;
        pos = p;
        float fAngle = r.magnitude * Mathf.Rad2Deg;
        q = Quaternion.AngleAxis(fAngle, r.normalized);
    }
    
    int id; //method id
    public Vector3 pos;
    public Quaternion q;
    public GameObject obj;
}

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
        objs.Add("Prefabs/Device");
        objs.Add("Prefabs/Bullet");
        objs.Add("Prefabs/GrassBlock");
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
