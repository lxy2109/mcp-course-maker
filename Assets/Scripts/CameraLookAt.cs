using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    [SerializeField]
    private Transform target; // 目标物体（立方体）
    
    void Start()
    {
        // 如果没有手动设置目标，尝试查找名为"立方体"的对象
        if (target == null)
        {
            GameObject cubeObj = GameObject.Find("立方体");
            if (cubeObj != null)
            {
                target = cubeObj.transform;
                Debug.Log("已自动设置目标为立方体");
            }
            else
            {
                Debug.LogWarning("未找到立方体对象，请手动设置目标");
            }
        }
    }
    
    // 每帧更新
    void LateUpdate()
    {
        if (target != null)
        {
            // 摄像机始终朝向目标
            transform.LookAt(target);
        }
    }
}