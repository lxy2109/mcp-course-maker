using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.EventSystems;

public class NotebookManager : SceneSingleton<NotebookManager>
{
    public GameObject notetBook;
    public bool isShowing = true;
    private bool locked = false;


    private Vector3 showPos = new Vector3(-0.317f,- 0.0340001f,0.7249997f);
    private Vector3 hidePos = new Vector3(-0.543f, -0.456f, 0.725f);

    private void Update()
    {   
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
                CheckRayCast();
        }
    }

    public void SetUp(bool value)
    {
        locked = true;
        isShowing = value;
        if (isShowing)
        {
            notetBook.transform.DOLocalMove(showPos, 0.5f).OnComplete(() => {
                locked = false;
            });
        }
        else
        {
            notetBook.transform.DOLocalMove(hidePos, 0.5f).OnComplete(() => {
                locked = false;
            });
        }
    }

    private bool CheckRayCast()
    {
        if (locked) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == notetBook)
            {
                isShowing=!isShowing;
                locked=true;
                if (isShowing)
                {
                    notetBook.transform.DOLocalMove(showPos, 0.5f).OnComplete(() => {
                        locked=false;
                    });
                }
                else
                {
                    notetBook.transform.DOLocalMove(hidePos, 0.5f).OnComplete(() => {
                        locked = false;
                    });
                }

                return true;
                
            }

        }
        return false;
    }

}
