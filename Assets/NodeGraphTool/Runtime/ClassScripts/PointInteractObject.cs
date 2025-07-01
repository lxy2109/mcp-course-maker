using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PointInteractObject : MonoBehaviour
{
    public UnityEvent pointEvent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
             CheckRayCast();
            //if (!EventSystem.current.())
            //    CheckRayCast();
            //else
            //{
            //    Debug.Log(EventSystem.current.currentSelectedGameObject);
            //}
        }

    }

    private bool CheckRayCast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == this.gameObject)
            {
                //Debug.Log(hit.collider.gameObject);
                pointEvent?.Invoke();
                return true;
                
            }

        }
        return false;
    }
}
