using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SceneSingletonReplace<T> : SerializedMonoBehaviour where T : SceneSingletonReplace<T>
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
            Destroy(instance.gameObject);
            instance = (T)this;
        }
    }
}
