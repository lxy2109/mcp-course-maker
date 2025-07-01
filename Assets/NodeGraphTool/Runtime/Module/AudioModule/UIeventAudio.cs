using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using InspectorExtra;
#endif


public class UIeventAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
#if UNITY_EDITOR
    [FieldName("鼠标进入时播放音频")]
#endif
    [SerializeField]
    private AudioClip enterClip;
#if UNITY_EDITOR
    [FieldName("鼠标点击时播放音频")]
#endif
    [SerializeField]
    private AudioClip clickClip;
#if UNITY_EDITOR
    [FieldName("鼠标离开时播放音频")]
#endif
    [SerializeField]
    private AudioClip exitClip;

    private AudioSource uiAudioSource;

    void Awake()
    {
        uiAudioSource=GameObject.Find("UIEventAudio").GetComponent<AudioSource>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        this.PlayAudio(this.clickClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.PlayAudio(this.enterClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        this.PlayAudio(this.exitClip);
    }

    private void PlayAudio(AudioClip ac)
    {
        if (ac == null||uiAudioSource==null)
        {
            return;
        }
        this.uiAudioSource.PlayOneShot(ac);
    }

}
