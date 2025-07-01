using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class SceneSingleton<T> : SerializedMonoBehaviour where T: SceneSingleton<T>
{
   public static T instance { get; private set; }
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = (T)this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static T GetInstanceInEditor()
    {
#if UNITY_EDITOR
        if(instance == null)
        {
            // 尝试查找场景中的 EventManager
            instance = GameObject.FindObjectOfType<T>();
            if (instance == null)
            {
                // 如果没有，创建一个新的 GameObject 并挂载
                GameObject go = new GameObject(typeof(T).Name);
                instance = go.AddComponent<T>();
            }
        }       
#endif
        return instance;
    }

}
