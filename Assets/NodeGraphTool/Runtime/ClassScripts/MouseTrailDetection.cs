using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class MouseTrailDetection : MonoBehaviour
{
    public LineRenderer lineRenderer;
    [LabelText("配置结束事件")]
    public UnityEvent endCallBack;

    public float maxDistance = 100;

    public List<Vector3> points=new List<Vector3>();

    private bool isHold = false;
    private bool isFinished=false;

    public int currentIndex=0;
    void Start()
    {
        Initialize();
    }

    [Button]
    public void Initialize()
    {
        points.Clear();
        SetUpPoints();
        currentIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            isHold = true;
        }

       else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            isHold = false;
            currentIndex = 0;
        }
        CheckMouseTrail();
    }

    private void CheckMouseTrail()
    {
        if (!isHold || isFinished) return;
        if (currentIndex >= points.Count)
        {
            isFinished = true;
            endCallBack?.Invoke();
            Debug.Log("CheckMouseTrail Over");
            return;
        }
        Vector3 mousePos = Input.mousePosition;
        Debug.LogFormat("<color=yellow>{0}</color>","Distance: " + Vector3.Distance(mousePos, points[currentIndex]));
        if (Vector3.Distance(mousePos, points[currentIndex]) <= maxDistance)
        {
            currentIndex++;
        }

    }

    private void SetUpPoints()
    {
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            positions[i] = transform.TransformPoint(lineRenderer.GetPosition(i));
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(positions[i]);
            points.Add(screenPosition);
        }
    }


}
