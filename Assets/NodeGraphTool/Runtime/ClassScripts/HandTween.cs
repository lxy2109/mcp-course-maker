using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;

public class HandTween : MonoBehaviour
{
    public enum XZstate
    {
        X,
        Z
    }

    [BoxGroup("SETTINGS")]
    [LabelText("手_Root")]
    public Transform hand;
    [BoxGroup("SETTINGS")]
    [LabelText("模型X方向或Z方向")]
    public XZstate state=XZstate.X;
    [BoxGroup("SETTINGS")]
    public bool rotateY = false;

    [BoxGroup("SETTINGS")]
    [LabelText("手部接触物体时事件")]
    public UnityEvent touchEvent;
    [BoxGroup("SETTINGS")]
    [LabelText("手部归位时事件")]
    public UnityEvent endEvent;

    private Vector3 originPos;
    private Vector3 originAngles;

    private void Start()
    {
        originPos = hand.localPosition;
        originAngles = hand.localEulerAngles;
    }

    [Button]
    public void CatchItem(Transform target,Action action=null)
    {
        originPos=hand.localPosition;
        originAngles = hand.localEulerAngles;

        Collider col;

        Sequence sequence = DOTween.Sequence();
        if (target.TryGetComponent<Collider>(out col))
        {
            Bounds bounds= col.bounds;
            Vector3 boxSize=bounds.max-bounds.min;
            //Debug.Log(boxSize);
           
            if (boxSize.x > boxSize.y && boxSize.x > boxSize.y)
            {
                Tween tween = hand.DOMove(bounds.center + Vector3.up * boxSize.y * 0.5f, 0.5f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.05f).SetRelative(false);
                sequence.Append(tween);
                tween.OnComplete(() =>
                {
                    Debug.Log("touchEvent");
                    touchEvent?.Invoke();
                });
                sequence.Insert(0.25f, hand.DORotate(new Vector3(0, rotateY?-90:90, -90f), 0.25f));
                sequence.Append(hand.DOLocalMove(originPos, 0.375f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.5f).SetRelative(false));
                sequence.Insert(1.1f, hand.DOLocalRotate(originAngles, 0.25f));

                sequence.OnComplete(() =>
                {
                    endEvent?.Invoke();
                    action?.Invoke();
                });
            }
           else if (boxSize.y > boxSize.x && boxSize.y > boxSize.z)
            {
                Tween tween;
                switch (state)
                {
                    case XZstate.X:
                        tween = hand.DOMove(bounds.center + Vector3.left * boxSize.x * 0.5f , 0.5f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.05f).SetRelative(false);
                        sequence.Append(tween);
                        tween.OnComplete(() =>
                        {
                            Debug.Log("touchEvent");
                            touchEvent?.Invoke();
                        });
                        sequence.Insert(0.5f, hand.DORotate(new Vector3(0, 0, 0), 0.25f));

                        sequence.Append(hand.DOLocalMove(originPos, 0.375f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.5f).SetRelative(false));
                        sequence.Insert(1.1f, hand.DOLocalRotate(originAngles, 0.25f));
                        break;
                    case XZstate.Z:
                         tween = hand.DOMove(bounds.center + Vector3.right * boxSize.x * 0.5f, 0.5f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.05f).SetRelative(false);
                        sequence.Append(tween);
                        tween.OnComplete(() =>
                        {
                            Debug.Log("touchEvent");
                            touchEvent?.Invoke();
                        });
                        sequence.Insert(0.5f, hand.DORotate(new Vector3(0, 0, 0), 0.25f));

                        sequence.Append(hand.DOLocalMove(originPos, 0.375f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.5f).SetRelative(false));
                        sequence.Insert(1.1f, hand.DOLocalRotate(originAngles, 0.25f));
                        break;
                }
                sequence.OnComplete(() =>
                {
                    endEvent?.Invoke();
                    action?.Invoke();
                });
            }

        }
        else
        {
            sequence.Append(hand.DOMove(target.position, 1).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(0.1f).SetRelative(false));
            sequence.Append(hand.DOMove(originPos, 0.75f).SetRecyclable(true).SetEase(Ease.InOutSine).SetDelay(1f).SetRelative(false));

            sequence.OnComplete(() =>
            {
                endEvent?.Invoke();
                action?.Invoke();
            });
        }
    }


    public void HoldItem(Transform target)
    {
        Collider col;
        if (target.TryGetComponent<Collider>(out col))
        {
            Bounds bounds = col.bounds;
            Vector3 boxSize = bounds.max - bounds.min;
            //Debug.Log(boxSize);

            if (boxSize.x > boxSize.y && boxSize.x > boxSize.y)
            {
                hand.transform.position = bounds.center + Vector3.up * boxSize.y * 0.5f;
                hand.transform.eulerAngles = new Vector3(0, rotateY ? -90 : 90, -90f);
            }
            else if (boxSize.y > boxSize.x && boxSize.y > boxSize.z)
            {
                switch (state)
                {
                    case XZstate.X:
                        hand.transform.position = bounds.center + Vector3.left * boxSize.x * 0.5f;
                        hand.transform.eulerAngles = new Vector3(0, rotateY ? -90 : 90, 0);
                        break;
                    case XZstate.Z:
                        hand.transform.position = bounds.center + Vector3.right * boxSize.x * 0.5f;
                        hand.transform.eulerAngles = new Vector3(0, rotateY ? -90 : 90, 0);
                        break;
                }
            }

        }
    }


    public void Restore (){
        hand.transform.localPosition = originPos;
        hand.transform.localEulerAngles = originAngles;
    }
}
