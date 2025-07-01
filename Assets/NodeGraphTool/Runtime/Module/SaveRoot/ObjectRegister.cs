using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 存储状态
/// 数值类
/// </summary>
[Serializable]
public class GameObjectState
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
    public bool active;

    public GameObjectState(Vector3 position, Quaternion rotation, Vector3 loaclScale, bool active)
    {
        this.position = position;
        this.rotation = rotation;
        this.localScale = loaclScale;
        this.active = active;
    }
}

/// <summary>
/// 注册组件
/// </summary>
public class ObjectRegister : MonoBehaviour
{
    [Header("初始状态设置")]
    [SerializeField]
    private bool useCustomInitialState = false;
    
    [SerializeField]
    private Vector3 customInitialPosition;
    
    [SerializeField]
    private Quaternion customInitialRotation;
    
    [SerializeField]
    private Vector3 customInitialScale;
    
    [SerializeField]
    private bool customInitialActive = true;

    // 保存的初始状态
    private GameObjectState initialState;

    private void Awake()
    {
        // 在Awake中保存初始状态
        SaveInitialState();
    }

    /// <summary>
    /// 保存物件的初始状态
    /// </summary>
    private void SaveInitialState()
    {
        if (useCustomInitialState)
        {
            // 使用自定义初始状态
            initialState = new GameObjectState(
                customInitialPosition,
                customInitialRotation,
                customInitialScale,
                customInitialActive
            );
        }
        else
        {
            // 使用当前状态作为初始状态
            initialState = new GameObjectState(
                transform.position,
                transform.rotation,
                transform.localScale,
                gameObject.activeSelf
            );
        }
    }

    /// <summary>
    /// 注册当前状态
    /// </summary>
    public GameObjectState Regist()
    {
        GameObjectState gameObjectState = new GameObjectState(
             this.transform.position,
            this.transform.rotation,
             this.transform.localScale,
            this.gameObject.activeSelf
            );
        return gameObjectState;
    }

    /// <summary>
    /// 登录到指定状态
    /// </summary>
    public void Login(GameObjectState objectRegister)
    {
        this.transform.position = objectRegister.position; 
        this.transform.rotation = objectRegister.rotation;
        this.transform.localScale = objectRegister.localScale;
        this.gameObject.SetActive(objectRegister.active);
    }

    /// <summary>
    /// 重置物件到初始状态
    /// </summary>
    public void ResetToInitialState()
    {
        if (initialState == null)
        {
            // 如果初始状态为空，重新保存
            SaveInitialState();
        }

        // 重置到初始状态
        transform.position = initialState.position;
        transform.rotation = initialState.rotation;
        transform.localScale = initialState.localScale;
        gameObject.SetActive(initialState.active);

        Debug.LogFormat("<color=blue>【物件重置】物件 {0} 已重置到初始状态</color>", gameObject.name);
    }

    /// <summary>
    /// 获取初始状态
    /// </summary>
    public GameObjectState GetInitialState()
    {
        if (initialState == null)
        {
            SaveInitialState();
        }
        return initialState;
    }

    /// <summary>
    /// 设置自定义初始状态
    /// </summary>
    public void SetCustomInitialState(Vector3 position, Quaternion rotation, Vector3 scale, bool active)
    {
        useCustomInitialState = true;
        customInitialPosition = position;
        customInitialRotation = rotation;
        customInitialScale = scale;
        customInitialActive = active;
        
        // 更新初始状态
        SaveInitialState();
    }

    /// <summary>
    /// 使用当前状态作为初始状态
    /// </summary>
    public void UseCurrentAsInitial()
    {
        useCustomInitialState = false;
        SaveInitialState();
        Debug.LogFormat("<color=blue>【物件重置】物件 {0} 已将当前状态设为初始状态</color>", gameObject.name);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中的重置按钮
    /// </summary>
    [ContextMenu("重置到初始状态")]
    private void ResetToInitialStateEditor()
    {
        ResetToInitialState();
    }

    /// <summary>
    /// 编辑器中的设置初始状态按钮
    /// </summary>
    [ContextMenu("设置当前为初始状态")]
    private void SetCurrentAsInitialEditor()
    {
        UseCurrentAsInitial();
    }
#endif
}
