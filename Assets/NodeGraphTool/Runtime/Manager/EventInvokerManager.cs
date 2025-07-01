using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using UnityEngine.Windows;

public class EventInvokerManager : SceneSingleton<EventInvokerManager>
{
    [LabelText("事件")]
    [SerializeField]
    private Dictionary<string, UnityEvent> allEvents=new Dictionary<string, UnityEvent>();


    [Button]
    public void InvokeEvent(string eventName)
    {
        if (String.IsNullOrEmpty(eventName)) return;
        if (allEvents.ContainsKey(eventName))
        {
            Debug.Log("Find Key   "+eventName);
            allEvents[eventName]?.Invoke();
        }
        else
        {
            Debug.LogError("Can't Find Key" + eventName);
            return;
        }
    }
    
    public Dictionary<string, UnityEvent> GetDic() {
        return allEvents;
    }
}
