using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Sirenix.OdinInspector;

[RequireComponent(typeof(PlayableDirector))]
public class PlayableDirectorPlayer : MonoBehaviour
{

    [SerializeField]
    [Header("选择控制的Timeline，默认为自己")]
    private PlayableDirector director;
    public enum HandleType
    { 
        slider,
        fillimage
    }

    [Header("播放UI")]
    public GameObject handleCanvas;

    private GameObject m_canvas;


    [SerializeField]
    [Header("控制UI类型")]
    public HandleType handleType;

    [ShowIf("handleType", HandleType.slider)]
    [SerializeField]
    [Header("带柄滑条")]
    private Slider slider;
    [ShowIf("handleType", HandleType.fillimage)]

    [SerializeField]
    [Header("滑条")]
    private Image fillamge;
    [Header("是否开始控制")]
    public bool inControlling=false;
    [Header("进度")]
    [ProgressBar(0,1)]
    public float percent;

    private void Start()
    {
        if (handleCanvas == null)
        {
            handleCanvas = Resources.Load<GameObject>("Module/UIPrefab/TimelineHandleCanvas");
        }
        if (!GameObject.Find("TimelineHandleCanvas"))
        {
            m_canvas=GameObject.Instantiate(handleCanvas);
            m_canvas.name.Replace("(Clone)", "");
        }
        slider = m_canvas.GetComponentInChildren<Slider>();
        fillamge = GameObject.Find("Handle_Image").GetComponentInChildren<Image>();
        switch (handleType)
        {
            case HandleType.slider:
                fillamge.gameObject.SetActive(false);
                break;
            case HandleType.fillimage:
                slider.gameObject.SetActive(false);
                break;
        }



        if (director==null)
        director=this.GetComponent<PlayableDirector>();
        StartCoroutine(NormalWithHandle());
    }

    private IEnumerator NormalWithHandle()
    {
        while (!inControlling)
        {
            percent =(float) (director.time / director.duration);
            switch (handleType)
            {
                case HandleType.slider:
                     slider.value= percent;
                    break;
                case HandleType.fillimage:
                     fillamge.fillAmount=percent;
                    break;
            }
            yield return 0;
        }
        yield break;
    }



    private IEnumerator PlayWithHandle()
    {
        director.Pause();
        while (inControlling)
        {
            switch (handleType)
            {
                case HandleType.slider:
                    percent = slider.value;
                    break;
                case HandleType.fillimage:
                    percent = fillamge.fillAmount;
                    break;
            }
            director.time = percent * director.duration;
            director.Evaluate();
            yield return 0;
        }
       yield break;
    }

    [Button]
    public void HandleControlling()
    {
        inControlling = true;
    StartCoroutine(PlayWithHandle());
    }

    [Button]
    public void Restore()
    {
        inControlling = false;
        PlayFromDurationPercent();
        StartCoroutine(NormalWithHandle());
    }

    [Button]
    public void PlayFromDurationPercent()
    {
        if (inControlling)
        {
            inControlling = false;
            StartCoroutine(NormalWithHandle());
        }
        director.Pause();
        director.time=percent*director.duration;
        director.Evaluate();
        director.Play();
    }


    //public void PlayWithHandle()
    //{
    //    director.Pause();
    //    director.initialTime = percent * director.duration;
    //}

    



}
