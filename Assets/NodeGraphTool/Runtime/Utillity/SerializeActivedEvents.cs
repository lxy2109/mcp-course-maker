using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class SerializeActivedEvents : SerializedScriptableObject
{
    [SerializeField]
    private List<string> allActivedEvents = new List<string>();

    public List<string> GetAllEvents()
    {
        List<string> temp=new List<string>();
        for (int i = 0; i < allActivedEvents.Count; i++)
        {
            temp.Add(allActivedEvents[i]);
        }
        return temp;
    }

    public void SetAllEvents(List<string> events)
    {
        allActivedEvents=new List<string>();
        for (int i = 0; i < events.Count; i++)
        {
            allActivedEvents.Add(events[i]);
        }
    }
    }
