using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TestBall : MonoBehaviour
{
    public bool isShow = true;
    public UnityEvent onShow;
    public void Show()
    {
        isShow = !isShow;
        transform.gameObject.SetActive(isShow);
        onShow?.Invoke();
    }
}
