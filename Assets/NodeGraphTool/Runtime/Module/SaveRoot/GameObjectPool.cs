using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Sirenix.OdinInspector;
using TemplateModule.Attribute;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif



[Serializable]
public struct GameObjectStateKey
{
    public GameObject obj;
    public string eventName;
}

public class GameObjectPool : SceneSingleton<GameObjectPool>
{

    [TabGroup("设置")]
    [Header("是否自动保存")]
    public bool autoSave=false;    
    [TabGroup("Debug")]
  [SerializeField]
    private Dictionary<GameObjectStateKey, GameObjectState> pool=new Dictionary<GameObjectStateKey, GameObjectState>();
    [TabGroup("Debug")]
    //执行过的events
    public List<string> allActivedEvents;

    [InfoBox("请将流程中受Event影响的物体放在该Root下，并添加object register")]
    [InfoBox("$noneRegisterNames",InfoMessageType.Error)]
    [SerializeField]
    [ReadOnly]
    private List<Transform> children=new List<Transform>();
    public Dictionary<string , GameObject> allNeedObject = new Dictionary<string ,GameObject>();
    private string noneRegisterNames="";


#if UNITY_EDITOR
    public void GetChildrenRoot()
    {
        children.Clear();
        for (int i = 0; i < this.transform.childCount; i++)
        {
            children.Add(this.transform.GetChild(i));
            allNeedObject.Add(this.transform.GetChild(i).name, this.transform.GetChild(i).gameObject);
        }
    }
    
    private void OnValidate()
    {
        GetChildrenRoot();
        CheckChildrenResgiter();
    }




    public void CheckChildrenResgiter()
    {
        if (children == null) return;
        noneRegisterNames = "以下子物体节点未添加object register : ";
        for (int i = 0; i < children.Count; i++)
        {
            if (!children[i].GetComponent<ObjectRegister>())
            {
                noneRegisterNames += children[i].name + " , ";
            }
        }
    }


#endif


    protected void Start()
    {
      if(pool==null) pool = new Dictionary<GameObjectStateKey, GameObjectState>();


        LoadAllevents();
        UpdatePoolElements();


    }

    public void UpdatePool()
    {
       
        if (allActivedEvents.Contains(EventManager.instance.GetCurrentEventName()))
        {
            Debug.Log("RefreshPool");
            RefreshPool();

        }
        else
        {
            Debug.Log("UpdatePoolElements");
            UpdatePoolElements();
#if UNITY_EDITOR
            if (autoSave) Serialize();
#endif
        }
        RegisterEventName();
    }


#if UNITY_EDITOR
    [HorizontalGroup("保存")]
    [Button("保存所有事件")]
    private void SaveAllEvents()
    {
        SerializeActivedEvents events= ScriptableObject.CreateInstance<SerializeActivedEvents>();
        string path = "Assets/NodeGraphTool/Resources/Module/Saver/" + SceneManager.GetActiveScene().name + ".asset";
        events.SetAllEvents(allActivedEvents);
        UnityEditor.AssetDatabase.CreateAsset(events, path);
        EditorUtility.SetDirty(events);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }

#endif





#if UNITY_EDITOR
    [HorizontalGroup("保存")]
    [Button("保存该步骤初始状态)")]
    private void Serialize()
    {
        Debug.Log("SaveFile  "+ "Assets/NodeGraphTool/Resources/Module/Saver/" + SceneManager.GetActiveScene().name + "_" + EventManager.instance.GetCurrentEventName() + ".asset");
        GameObjectRootAsset tempObj = Resources.Load<GameObjectRootAsset>("Assets/NodeGraphTool/Resources/Module/Saver/" +
            SceneManager.GetActiveScene().name + "_" + EventManager.instance.GetCurrentEventName());
        if (tempObj != null)
        {
            Debug.Log("Contains File");
            return;
        }


        string path = "Assets/NodeGraphTool/Resources/Module/Saver/" + SceneManager.GetActiveScene().name + "_" + EventManager.instance.GetCurrentEventName() + ".asset";
        GameObjectRootAsset rootAsset =ScriptableObject.CreateInstance<GameObjectRootAsset>();
        foreach (KeyValuePair<GameObjectStateKey, GameObjectState> item in pool)
        {
            rootAsset.objectNames.Add(item.Key.obj.name);
            rootAsset.eventNames.Add(item.Key.eventName);
            rootAsset.gameObjectStates.Add(item.Value);
        }
        UnityEditor.AssetDatabase.CreateAsset(rootAsset, path);
        EditorUtility.SetDirty(rootAsset);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
#endif

    private void LoadAllevents()
    {
        SerializeActivedEvents tempObj = Resources.Load<SerializeActivedEvents>("Module/Saver/" +
                SceneManager.GetActiveScene().name );
        Debug.Log("SerializeActivedEvents   "+tempObj);
        if (!tempObj) return;
        allActivedEvents=tempObj.GetAllEvents();
    }




    private void DeSerialize()
    {
        //如果存在本地文件加载本地文件，否则加载历史数据
        GameObjectRootAsset tempObj = Resources.Load<GameObjectRootAsset>("Module/Saver/" +
            SceneManager.GetActiveScene().name + "_" + EventManager.instance.GetCurrentEventName());

        if (tempObj != null)
        {
            Debug.Log("Local File  " + tempObj.name);
            pool.Clear();
            foreach (var item in tempObj.ContactKeyValuePair())
            {
                pool.Add(item.Key, item.Value);
              
            }
            Debug.Log(pool.Count);
        }
        else
        {
            Debug.Log("History Data");
        }
    }

 //   [Button]
    public void UpdatePoolElements()
    {
        //0==self
        Transform[] objs=this.gameObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in objs)
        {
           if (t.gameObject.GetComponent<ObjectRegister>())
            {
            
                RegisterPool(t.gameObject);
            }
           
        }
    }
   // [Button]
    public void RefreshPool()
    {
        //如果存在本地文件加载本地文件，否则加载历史数据
        DeSerialize();

        Transform[] objs = this.gameObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in objs)
        {
            GameObjectStateKey key = new GameObjectStateKey();
            key.obj = t.gameObject;
            key.eventName = EventManager.instance.GetCurrentEventName();

            if (pool.ContainsKey(key))
            {
             t.gameObject.GetComponent<ObjectRegister>().Login(pool[key]);
            }             
        }
    }


    public void RegisterEventName()
    {
        if (!allActivedEvents.Contains(EventManager.instance.GetCurrentEventName()))
        {
            allActivedEvents.Add(EventManager.instance.GetCurrentEventName());
        }
    }

    public void RegisterPool(GameObject obj)
    {
        GameObjectStateKey key=new GameObjectStateKey();
        key.obj = obj;
        key.eventName = EventManager.instance.GetCurrentEventName();
       

        if (pool.ContainsKey(key)) return;

        ObjectRegister register;
        if (!obj.TryGetComponent<ObjectRegister>(out register)) return;
        GameObjectState state= register.Regist();

        pool.Add(key, state);
    }

    /// <summary>
    /// 重置物件池到初始状态
    /// </summary>
    [TabGroup("Debug")]
    [Button("重置物件池")]
    public void ResetGameObjectPool()
    {
        Debug.LogFormat("<color=red>【物件池】开始重置物件池</color>");
        
        // 1. 清空已激活事件列表
        allActivedEvents.Clear();
        
        // 2. 清空物件状态池
        pool.Clear();
        
        // 3. 重置所有物件的状态到初始状态
        ResetAllObjectsToInitialState();
        
        // 4. 重新加载初始事件列表（如果有的话）
        LoadAllevents();
        
        Debug.LogFormat("<color=green>【物件池】物件池重置完成</color>");
    }

    /// <summary>
    /// 重置所有物件到初始状态
    /// </summary>
    private void ResetAllObjectsToInitialState()
    {
        Debug.LogFormat("<color=blue>【物件池】重置所有物件到初始状态</color>");
        
        Transform[] objs = this.gameObject.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform t in objs)
        {
            if (t.gameObject.GetComponent<ObjectRegister>() != null)
            {
                // 获取物件的ObjectRegister组件
                ObjectRegister register = t.gameObject.GetComponent<ObjectRegister>();
                
                // 重置物件到初始状态
                if (register != null)
                {
                    register.ResetToInitialState();
                }
            }
        }
        
        Debug.LogFormat("<color=blue>【物件池】物件初始状态重置完成</color>");
    }

    /// <summary>
    /// 清空所有保存的状态文件（编辑器模式）
    /// </summary>
    #if UNITY_EDITOR
    [TabGroup("Debug")]
    [Button("清空保存文件")]
    public void ClearAllSavedStates()
    {
        Debug.LogFormat("<color=red>【物件池】开始清空所有保存的状态文件</color>");
        
        // 清空已激活事件保存文件
        string eventsPath = "Assets/NodeGraphTool/Resources/Module/Saver/" + SceneManager.GetActiveScene().name + ".asset";
        if (System.IO.File.Exists(eventsPath))
        {
            UnityEditor.AssetDatabase.DeleteAsset(eventsPath);
            Debug.LogFormat("<color=blue>【物件池】删除已激活事件文件: {0}</color>", eventsPath);
        }
        
        // 清空所有物件状态保存文件
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:GameObjectRootAsset");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(SceneManager.GetActiveScene().name))
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
                Debug.LogFormat("<color=blue>【物件池】删除物件状态文件: {0}</color>", path);
            }
        }
        
        UnityEditor.AssetDatabase.Refresh();
        Debug.LogFormat("<color=green>【物件池】所有保存文件清空完成</color>");
    }
    #endif

}
