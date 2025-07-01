using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PreviewObject : MonoBehaviour
{
    const float k_MouseSensitivityMultiplier = 0.01f;

    public float mouseSensitivity = 100f;
    float xRotation = 0f;

    private bool canDrag = false;


    public void SetDrag(bool value)
    {
        canDrag=value;
    }

    private void Update()
    {
        
        if (canDrag)
        {
            RotatObject();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
        }
    }

    private void RotatObject()
    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * k_MouseSensitivityMultiplier;

        this.transform.Rotate(Vector3.up * mouseX);

    }
}
