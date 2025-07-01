using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using Sirenix.OdinInspector;


public class ItemDragImage : MonoBehaviour
{
    
    public InventoryItem item;
    [SerializeField]
    private Transform beginParentTransform;
    [SerializeField]
    private Transform topUI;


    private InventoryItem itemInstance;

    private void Start()
    {
        if (item != null)
        {
            itemInstance = new InventoryItem(item);
        }


        if (topUI == null)
        {
            topUI = BagManager.instance.GetCanvas();
        } 
    }

    public void Begin(BaseEventData data)
    {
        if (transform.parent == topUI) return;
        beginParentTransform = transform.parent;
        transform.SetParent(topUI);
    }

    public void OnDrag(BaseEventData data)
    {
        transform.position = Input.mousePosition;
        if (transform.GetComponent<Image>().raycastTarget) transform.GetComponent<Image>().raycastTarget = false;
    }

    public void End(BaseEventData data)
    {
        PointerEventData pointerEvent = data as PointerEventData;
        GameObject go = pointerEvent.pointerCurrentRaycast.gameObject;


        if (go == null)
        {

            SetPosAndParent(transform, beginParentTransform);
            transform.GetComponent<Image>().raycastTarget = true;
            if(item) RayCast3DAction(item);

            return;
        }
        Debug.Log(go.tag + go.name);
        if (go.tag == "Grid") 
        {
            SetPosAndParent(transform, go.transform);
            transform.GetComponent<Image>().raycastTarget = true;
        }
        else if (go.tag == "ItemUI")
        {
            Debug.Log("???");
            SetPosAndParent(transform, go.transform.parent);
            go.transform.SetParent(topUI);

            go.transform.position=beginParentTransform.position;
            go.transform.SetParent(beginParentTransform);
            transform.GetComponent<Image>().raycastTarget = true;
        }
        else 
        {
            
            SetPosAndParent(transform, beginParentTransform);
            transform.GetComponent<Image>().raycastTarget = true;
        }


    }

    public void RightClick(BaseEventData data)
    {
        PointerEventData pointerEvent = data as PointerEventData;
        if (pointerEvent.button == PointerEventData.InputButton.Right)
        {
            if (ItemPreviewManager.instance)
            {
                ItemPreviewManager.instance.Preview(itemInstance);
            }
          
            Debug.Log("Right");
        }
    }

    private GameObject RayCast3DAction(InventoryItem item)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject target= hit.collider.gameObject;

            for (int i = 0; i < item.events.Count; i++)
            {
                switch (item.events[i].eventType)
                {
                    case InventoryEventType.Tag:
                        if (target.tag == item.events[i].tag)
                        {
                            if (EventInvokerManager.instance) { 
                                EventInvokerManager.instance.InvokeEvent(item.events[i].eventNameDragOn);
                                if (!item.isReused) { this.gameObject.SetActive(false); }
                            }
                        }
                        break;
                    case InventoryEventType.Name:
                        if (target.name == item.events[i].name)
                        {
                            if (EventInvokerManager.instance) { 
                                EventInvokerManager.instance.InvokeEvent(item.events[i].eventNameDragOn);
                                if (!item.isReused) { this.gameObject.SetActive(false); }
                            }
                        }
                        break;
                }
            }

        }
        return null;
    }






    private void SetPosAndParent(Transform t, Transform parent)
    {
        t.SetParent(parent);
        t.position = parent.position;
    }


}
