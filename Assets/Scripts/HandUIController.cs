using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandUIController : MonoBehaviour
{
    public Animator animator;
    public List<GameObject> handUIs;
    // Start is called before the first frame update
    void Start()
    {
        animator = transform.GetComponent<Animator>();
    }

   public void SetUIState(int i)
    {
        animator.SetInteger("UIState",i);
    }

    public void SetHandUI(int i)
    {
        for(int j = 0; j < handUIs.Count; j++) 
        {
            if (j == i) 
            {
                handUIs[j].SetActive(true);
            }
            else
            {
                handUIs[j].SetActive(false);
            }
        }
    }
}
