using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SerializeSceneGameobject 
{
    public static GameObject GetGameObject(string name)
    {
      
       // Transform[] objs= root.gameObject.GetComponentsInChildren<Transform>();
        foreach (var obj in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            Debug.Log(obj.name);
            if (obj.scene.name == null) continue;
            if (obj.name == name) return obj.gameObject;
        }
        Debug.Log(name + " Not Find");
        return null;

    }

}
