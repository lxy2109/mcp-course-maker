using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System;
using TMPro;

[Flags]
public enum AsyncUIElements
{
    Text=1,
    FillImage=2,
    Slider=4,
}

public class AsyncLoadManager : Singleton<AsyncLoadManager>
{
    [TabGroup("数据设置")]
    [LabelText("需要加载的场景名称")]
    public string nextScene;

    [TabGroup("数据设置")]
    [LabelText("加载场景结束执行的Unity Event")]
    public UnityEvent @event;

    [TabGroup("UI设置项")]
    [LabelText("Main UI")]
    [Header("默认为Resoures的AsyncLoadingCanvas")]
    public GameObject asyncCanvas;
    [TabGroup("UI设置项")]
    public AsyncUIElements asyncUIElements;


    private GameObject runtimeCanvas=null;

    private TextMeshProUGUI text;
    private Image fillImage;
    private Slider slider;

    [TabGroup("测试项")]
    [SerializeField]
    [ProgressBar(0,1)]
    private float progress;

    private float dampprogress = 0;

    private AsyncOperation async;


    public void InitCanvas()
    {
        if (runtimeCanvas == null)
        {
            runtimeCanvas=GameObject.Instantiate(asyncCanvas);
        }

        #region Find Property

        if (GameObject.Find("ProgressText"))
        {
            text= GameObject.Find("ProgressText").GetComponent<TextMeshProUGUI>();
        }
        if (GameObject.Find("ProgressSlider"))
        {
            slider = GameObject.Find("ProgressSlider").GetComponent<Slider>();
        }
        if (GameObject.Find("ProgressImage"))
        {
            fillImage = GameObject.Find("ProgressImage").GetComponent<Image>();
        }
        #endregion

        #region Check Flags
        if (!asyncUIElements.HasFlag(AsyncUIElements.Text))
        {
            if(text)text.gameObject.SetActive(false);
        }
        if (!asyncUIElements.HasFlag(AsyncUIElements.Slider))
        {
            if (slider) slider.gameObject.SetActive(false);
        }
        if (!asyncUIElements.HasFlag(AsyncUIElements.FillImage))
        {
            if (fillImage) fillImage.gameObject.SetActive(false);
        }
        #endregion

    }

    public void LoadScene(string sceneName)
    {
        InitCanvas();
        nextScene = sceneName;
        StartCoroutine(ReturnAsync());
        StartCoroutine(LoadSceneCallback());
    }
    [Button]
    public void LoadScene(string sceneName,Action callback=null)
    {
        InitCanvas();
        nextScene =sceneName;
        StartCoroutine(ReturnAsync());
        StartCoroutine(LoadSceneCallback(callback));
      
    }



    public IEnumerator LoadSceneCallback(Action callback =null)
    {
        if (callback == null)
        {
            callback = new Action(() =>
              {
                  @event?.Invoke();
              });
        }

        if (async == null)
        {
            Debug.Log("Start Looping");
            yield return 1;
        }
            

        while(progress<1)
        {
            progress = async.progress;
            Debug.Log(progress);
            UpdatePrgressUI();

            //非真实加载速度 
            //若需要真实加载速度 将条件修改为
            //progress>=0.9f
            if (dampprogress >= 0.85f)
            {
                progress = 1;
                UpdatePrgressUI();
                callback?.Invoke();
                CloseLoading();
                async.allowSceneActivation = true;
              
                yield break;
            }
            yield return 0;
        }


       

    }

    private void CloseLoading()
    {
        //StopCoroutine("ReturnAsync");
        text = null;
        slider = null;
        fillImage = null;
       // async = null;
        progress = 0;
        dampprogress = 0;
        Destroy(runtimeCanvas);
    }

    private void UpdatePrgressUI()
    {
        dampprogress = Mathf.Lerp(dampprogress, progress, Time.deltaTime*5f);
        if (text)
        {
            text.text = (Mathf.FloorToInt(dampprogress * 100)).ToString() + "%";
        }
        if (fillImage)
        {
            fillImage.fillAmount = dampprogress;
        }
        if (slider)
        {
            slider.value = dampprogress;
        }

    }
    private IEnumerator ReturnAsync()
    {
        async = SceneManager.LoadSceneAsync(nextScene);
        async.allowSceneActivation = false;
        yield return async;

    }
}
