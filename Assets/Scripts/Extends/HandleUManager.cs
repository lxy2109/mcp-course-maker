using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

[System.Serializable]
public class HandStructPage
{
    public string title;
    public List<string> tips;
    public List<bool> isDone;
}
public class HandleUManager : MonoBehaviour
{
    public List<GameObject> tipObjs=new List<GameObject>();
    public TextMeshProUGUI title;



    [SerializeField]
    public List<HandStructPage> pages;
    public int index = -1;


    [Button]
    public void SetUpPage(int index)
    {
        this.index = index;
        foreach (var obj in tipObjs)
        {
            obj.SetActive(false);
        }

        title.text=pages[index].title;

        for (int i = 0; i < pages[index].tips.Count; i++)
        {
            tipObjs[i].SetActive(true);
            tipObjs[i].transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = pages[index].tips[i];
            tipObjs[i].transform.GetChild(2).GetComponent<Image>().color = pages[index].isDone[i] ? Color.green : Color.white;
        }

    }

    [Button]
    public void ActiveTip(int index)
    {
        pages[this.index].isDone[index] =true;
        tipObjs[index].transform.GetChild(2).GetComponent<Image>().color = pages[this.index].isDone[index] ? Color.green : Color.white;
    }

    [Button]
    public void DisActiveTip(int index)
    {
        pages[this.index].isDone[index] = false;
        tipObjs[index].transform.GetChild(2).GetComponent<Image>().color = pages[this.index].isDone[index] ? Color.green : Color.white;
    }
}
