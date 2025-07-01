using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCMR : MonoBehaviour
{
    public enum MouseState
    {
        None,
        MidMouseBtn,
        LeftMouseBtn
    }



    private MouseState mMouseState = MouseState.None;
    private Camera mCamera;


    private void Awake()
    {
        mCamera = GetComponent<Camera>();
        if (mCamera == null)
        {
            Debug.LogError(GetType() + "camera Get Error ……");
        }

        GetDefaultFov();
    }

    private void LateUpdate()
    {
        CameraRotate();

        CameraFOV();

        CameraMove();
    }

    #region Camera Rotation

    //旋转最大角度
    public int yRotationMinLimit = -20;
    public int yRotationMaxLimit = 80;
    //旋转速度
    public float xRotationSpeed = 250.0f;
    public float yRotationSpeed = 120.0f;
    //旋转角度
    private float xRotation = 0.0f;
    private float yRotation = 0.0f;

    /// <summary>
    /// 鼠标移动进行旋转
    /// </summary>
    void CameraRotate()
    {
        if (mMouseState == MouseState.None)
        {

            //Input.GetAxis("MouseX")获取鼠标移动的X轴的距离
            xRotation -= Input.GetAxis("Mouse X") * xRotationSpeed * 0.02f;
            yRotation += Input.GetAxis("Mouse Y") * yRotationSpeed * 0.02f;

            yRotation = ClampValue(yRotation, yRotationMinLimit, yRotationMaxLimit);//这个函数在结尾
                                                                                    //欧拉角转化为四元数
            Quaternion rotation = Quaternion.Euler(-yRotation, -xRotation, 0);
            transform.rotation = rotation;

        }
    }

    #endregion

    #region Camera fov

    //fov 最大最小角度
    public int fovMinLimit = 25;
    public int fovMaxLimit = 75;
    //fov 变化速度
    public float fovSpeed = 50.0f;
    //fov 角度
    private float fov = 0.0f;

    void GetDefaultFov()
    {
        fov = mCamera.fieldOfView;
    }

    /// <summary>
    /// 滚轮控制相机视角缩放
    /// </summary>
    public void CameraFOV()
    {
        //获取鼠标滚轮的滑动量
        fov += Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 100 * fovSpeed;

        // fov 限制修正
        fov = ClampValue(fov, fovMinLimit, fovMaxLimit);

        //改变相机的 fov
        mCamera.fieldOfView = (fov);
    }

    #endregion


    #region Camera Move

    float _mouseX = 0;
    float _mouseY = 0;
    public float moveSpeed = 1;
    /// <summary>
    /// 中键控制拖动
    /// </summary>
    public void CameraMove()
    {
        if (Input.GetMouseButton(2))
        {
            _mouseX = Input.GetAxis("Mouse X");
            _mouseY = Input.GetAxis("Mouse Y");

            //相机位置的偏移量（Vector3类型，实现原理是：向量的加法）
            Vector3 moveDir = (_mouseX * -transform.right + _mouseY * -transform.forward);

            //限制y轴的偏移量
            moveDir.y = 0;
            transform.position += moveDir * 0.5f * moveSpeed;
        }
        else if (Input.GetMouseButtonDown(2))
        {
            mMouseState = MouseState.MidMouseBtn;
            Debug.Log(GetType() + "mMouseState = " + mMouseState.ToString());
        }
        else if (Input.GetMouseButtonUp(2))
        {
            mMouseState = MouseState.None;
            Debug.Log(GetType() + "mMouseState = " + mMouseState.ToString());
        }

    }

    #endregion

    #region tools ClampValue

    //值范围值限定
    float ClampValue(float value, float min, float max)//控制旋转的角度
    {
        if (value < -360)
            value += 360;
        if (value > 360)
            value -= 360;
        return Mathf.Clamp(value, min, max);//限制value的值在min和max之间， 如果value小于min，返回min。 如果value大于max，返回max，否则返回value
    }


    #endregion
}