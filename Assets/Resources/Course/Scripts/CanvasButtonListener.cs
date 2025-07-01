using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CanvasButtonListener : MonoBehaviour
{
    private EventManager eventManager;
    private Button[] button = new Button[3];
    private Camera camera;
    private Vector3 cameraOriginalPosition;
    private Quaternion cameraOriginalRotation;
    


    private void Awake()
    {
        button[0] = transform.GetChild(0).GetChild(0).GetComponent<Button>();
        button[1] = transform.GetChild(0).GetChild(1).GetComponent<Button>();
        button[2] = transform.GetChild(1).GetComponentInChildren<Button>();

        camera = Camera.main;
        
        // 保存摄像机的初始位置和旋转值
        if (camera != null)
        {
            cameraOriginalPosition = camera.transform.position;
            cameraOriginalRotation = camera.transform.rotation;
            Debug.Log($"摄像机初始位置已保存: {cameraOriginalPosition}");
        }
       
    }

    void Start()
    {
        eventManager = FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            Debug.LogError("EventManager not found in scene!");
            return;
        }

        if (button == null)
        {
            Debug.LogError("No Button found in children!");
            return;
        }

        button[0].onClick.AddListener(CameraReSet);
        button[1].onClick.AddListener(ReStartExp);
        button[2].onClick.AddListener(StartExp);
    }

    /// <summary>
    /// 按钮点击事件处理,自身panel消失，且自动加载当前nodegraph的第一个node
    /// </summary>
    private void StartExp()
    {
        transform.GetChild(1).gameObject.SetActive(false);
        if (eventManager != null)
        {
            eventManager.LoadFlowEvent(0);
        }
    }

    private void CameraReSet()
    {
        if (camera != null)
        {
            camera.transform.position = cameraOriginalPosition;
            camera.transform.rotation = cameraOriginalRotation;
            Debug.Log($"摄像机已重置到初始位置: {cameraOriginalPosition}");
        }
        else
        {
            Debug.LogError("Camera is null!");
        }
    }

    private void ReStartExp()
    {
        EventManager.instance.ResetAndRestartExperiment();
    }
}
