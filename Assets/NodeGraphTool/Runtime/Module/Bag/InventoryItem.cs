using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

using System;

#if UNITY_EDITOR
using UnityEditor;
using InspectorExtra;
#endif
public enum InventoryEventType
{
    Tag,
    Name,
}
[Serializable]
public struct EventAffectGrop
{

    [Tooltip("Tag为作用于该种Tag的对象，Name为作用于指定目标")]
#if UNITY_EDITOR
    [FieldName("事件作用对象类型")]
#endif
    public InventoryEventType eventType;
    [ShowIf("eventType", InventoryEventType.Tag)]
#if UNITY_EDITOR
    [FieldName("作用目标标签")]
#endif
    public string tag;
    [ShowIf("eventType", InventoryEventType.Name)]
#if UNITY_EDITOR
    [FieldName("作用目标名称")]
#endif
    public string name;
#if UNITY_EDITOR
    [FieldName("拖放在物体上的事件")]
#endif
    public string eventNameDragOn;
}

[Serializable]
public class ItemObject
{
    [AssetsOnly]
#if UNITY_EDITOR
    [FieldName("对应的prefab")]
#endif
    public GameObject itemObject;

  // [MultiLineProperty]
#if UNITY_EDITOR
    [FieldName("介绍文本")]
#endif
    public string content;
#if UNITY_EDITOR
    [FieldName("介绍语音")]
#endif
    public AudioClip descriptionAudio;
}

[CreateAssetMenu(menuName ="Item",fileName ="Item",order =0)]
public class InventoryItem : SerializedScriptableObject
{
    [Sirenix.OdinInspector.FilePath]
    public string itemName;

    public Sprite itemImage;

    public List<EventAffectGrop>  events=new List<EventAffectGrop>();

    public ItemObject itemObject;
    [Header("是否可以重复使用")]
    public bool isReused = false;

    public InventoryItem(InventoryItem item)
    {
        this.itemName = item.itemName;
        this.events = item.events;  
        this.itemObject = item.itemObject;  
    }


}
