using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UICatchInstance : MonoBehaviour
{
    public static UICatchInstance instance;
    public List<GameObject> UICatchGOs;
    public int curShowIndex = 0;
    public Transform scenePoint2;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void ShowOBJByIndex(int index)
    {
        curShowIndex = index;
        UICatchGOs[curShowIndex].SetActive(true);
        if (index > 6)
        {
            UICatchGOs[curShowIndex].transform.SetPositionAndRotation(scenePoint2.position,scenePoint2.rotation);
        }
    }
}

