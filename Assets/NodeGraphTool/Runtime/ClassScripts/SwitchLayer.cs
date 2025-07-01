using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SwitchLayer : MonoBehaviour
{
   public List<GameObject> objects = new List<GameObject>();

    public void SetLayer(int layerInt)
    {
        foreach (GameObject obj in objects)
        {
            obj.layer = layerInt;
        }
    }
}
