using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.AI;
using System;
public class CharacterMovement : MonoBehaviour
{
    [FoldoutGroup("Base")]
    [ReadOnly]
    [SerializeField]
    protected NavMeshAgent agent;

    protected NavMeshPath path;

    [FoldoutGroup("Base")]
    [ReadOnly]
    [SerializeField]
    protected Animator characterAnimator;
    [FoldoutGroup("Base")]
    [ReadOnly]
    [SerializeField]
    protected Transform character;

    [FoldoutGroup("Base")]
    protected float rotateSpeed = 5;

    protected IEnumerator moveIEnumerator;
    private RaycastHit hit;

    public string eventName;

    private void Start()
    {
        Initialize();
    }

    protected virtual void Initialize()
    {
        agent = this.GetComponent<NavMeshAgent>();
        characterAnimator = this.GetComponent<Animator>();
        if(character==null) character = this.transform;
        path = new NavMeshPath();
    }



    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                MoveToPoint(hit.point, null);
            }
        }
    }


    /// <summary>
    /// 移动至路径
    /// </summary>
    /// <param name="point"></param>
    /// <param name="action"></param>
    public void MoveToPoint(Vector3 point, Action action = null)
    {
        if (moveIEnumerator != null)
        {
            StopCoroutine(moveIEnumerator);
        }

        agent.SetDestination(point);
        moveIEnumerator = Move(point, action = () =>
        {
            if (String.IsNullOrEmpty(eventName))
            {
                EventInvokerManager.instance?.InvokeEvent(eventName);
            }
           
            Debug.Log("Move Compelete");
        });
        StartCoroutine(moveIEnumerator);
    }



    /// <summary>
    /// 移动协程
    /// </summary>
    /// <param name="point"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    protected IEnumerator Move(Vector3 point, Action action)
    {
        //yield return 1;
        while (agent.path.corners.Length > 1)
        {
            characterAnimator.SetFloat("speed", agent.velocity.magnitude / agent.speed);
            yield return 1;
        }


        characterAnimator.SetFloat("speed", agent.velocity.magnitude / agent.speed);
        action?.Invoke();
        yield break;
    }
}
