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

    [Tooltip("TagΪ�����ڸ���Tag�Ķ���NameΪ������ָ��Ŀ��")]
#if UNITY_EDITOR
    [FieldName("�¼����ö�������")]
#endif
    public InventoryEventType eventType;
    [ShowIf("eventType", InventoryEventType.Tag)]
#if UNITY_EDITOR
    [FieldName("����Ŀ���ǩ")]
#endif
    public string tag;
    [ShowIf("eventType", InventoryEventType.Name)]
#if UNITY_EDITOR
    [FieldName("����Ŀ������")]
#endif
    public string name;
#if UNITY_EDITOR
    [FieldName("�Ϸ��������ϵ��¼�")]
#endif
    public string eventNameDragOn;
}

[Serializable]
public class ItemObject
{
    [AssetsOnly]
#if UNITY_EDITOR
    [FieldName("��Ӧ��prefab")]
#endif
    public GameObject itemObject;

  // [MultiLineProperty]
#if UNITY_EDITOR
    [FieldName("�����ı�")]
#endif
    public string content;
#if UNITY_EDITOR
    [FieldName("��������")]
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
    [Header("�Ƿ�����ظ�ʹ��")]
    public bool isReused = false;

    public InventoryItem(InventoryItem item)
    {
        this.itemName = item.itemName;
        this.events = item.events;  
        this.itemObject = item.itemObject;  
    }


}
