using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SetPosition : MonoBehaviour
{
    public Transform targetTrans;
    public float minDis;
    public UnityEvent OnSetSucceed;
    public UnityEvent OnSetFailed;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPositionAndRotation()
    {
        float dis = Vector3.Distance(transform.position, targetTrans.position);
        Debug.LogFormat("<color=red>Setposition_Dis:{0}</color>",dis);
        if (dis < minDis && targetTrans.gameObject.activeSelf) 
        {
            transform.SetPositionAndRotation(targetTrans.position,targetTrans.rotation);
            OnSetSucceed?.Invoke();
        }
        else 
        {
            OnSetFailed?.Invoke();
        }
    }
}
