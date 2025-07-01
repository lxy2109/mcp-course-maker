using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderSwap : MonoBehaviour
{
    public UIPanelManager panelManager;

    public Slider slider;
    public List<string> contents=new List<string>();
    // Start is called before the first frame update
    void Start()
    {
        slider.maxValue=contents.Count-1;
    }

    // Update is called once per frame
    void Update()
    {
        float tempValue=slider.value;
        slider.value = Mathf.Floor(tempValue);
    }

    public void UpdatePage()
    {
        panelManager.UpdateInfoText(contents[Mathf.FloorToInt(slider.value)]);
       // Debug.Log(Mathf.FloorToInt(slider.value));
    }

    public void SetPageValue(float f)
    {
        slider.value = f;
    }
}
