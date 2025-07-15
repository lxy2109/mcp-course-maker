using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine.Events;
using Object = UnityEngine.Object;


//[Serializable]
//public class TimelineWithEndAction
//{
//    public TimelineAsset timeline;
//    public UnityEvent unityEvent;
//}

public class TimelineManager : SceneSingleton<TimelineManager>
{
   
   [TabGroup("基础设置")]
    [Header("指定的PlayableDirector，建议唯一")]
    [Tooltip("若未指定会自动选择当前物体的PlayableDirector")]
    [LabelText("目标timeline")]
    public PlayableDirector playableDirector;
    [TabGroup("基础设置")]
    [LabelText("是否需要实例化")]
    //[ReadOnly]
    [Tooltip("若需要播放多个Timeline，请勾选")]
    public bool needInstance = true;
    //[TabGroup("Timeline")]
    //public TimelineWithEndAction[] timelines;

    private PlayableDirector tempPlaybleDirector;
    private List<PlayableDirector> currentPlayers = new List<PlayableDirector>();

    protected override void  Awake()
    {
        base.Awake();
        if (!playableDirector)
        {
            playableDirector = GetComponent<PlayableDirector>();
        }
        tempPlaybleDirector = playableDirector;
    }


    //[FoldoutGroup("Debug")]
    //[Button]
    //public void PlayTimelineByIndex(int index)
    //{
    //    PlayTimeline(timelines[index].timeline, timelines[index].unityEvent);
    //}


    /// <summary>
    /// 播放单个timeline并执行回调action()
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="action"></param>
    public void PlayTimeline(TimelineAsset asset, Action action = null)
    {
        if (needInstance)
        {
            if (asset)
            {
                var playableDirectorGameObject = new GameObject(asset.name);
                var new_playableDirector = playableDirectorGameObject.AddComponent<PlayableDirector>();//播放Timeline时会临时添加一个PlaybleDirector
                new_playableDirector.extrapolationMode = DirectorWrapMode.Hold; //初始化
                new_playableDirector.playOnAwake = false;
                StartCoroutine(WaitTimelinePlay(asset, new_playableDirector, action));
            }
            else
                action?.Invoke();
        }
        else
        {
            if (asset)
            {
                playableDirector.extrapolationMode = DirectorWrapMode.Hold; //初始化
                playableDirector.playOnAwake = false;
                playableDirector.Play(asset);
                StartCoroutine(ReachEndPoint(playableDirector, action));
            }
            else
                action?.Invoke();

        }
    }


    /// <summary>
    /// 播放多个timeline并执行回调action()
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="action"></param>
    public void PlayTimelines(TimelineAsset[] timelineAssets, Action action = null)
    {
        if (timelineAssets.Length > 0)
        {
            StartCoroutine(WaitTimelinesPlay(timelineAssets, action));
        }
    }

    /// <summary>
    /// 删除所有目前播放的PlayableDirectors
    /// </summary>
    public void KillCurrentPlayingTimelines()
    {
        StopAllCoroutines();
        foreach (PlayableDirector player in currentPlayers)
            if (player.gameObject != this.gameObject)
            {
                player.RebuildGraph();
                Destroy(player.gameObject);
            }
    }

    /// <summary>
    /// 强制停止所有正在播放的Timeline（基于实际机制）
    /// </summary>
    public void ForceStopAllTimelines()
    {
        Debug.Log("【TimelineManager】开始强制停止所有Timeline");

        // 1. 停止所有协程（关键）
        StopAllCoroutines();


        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in allGameObjects)
        {
            if (go != null && go != this.gameObject)
            {
                PlayableDirector director = go.GetComponent<PlayableDirector>();
                if (director != null && director.playableAsset != null)
                {
                    // 将Timeline跳转到结束位置，这样物体会直接到达最终位置
                    director.time = director.playableAsset.duration;
                    director.Evaluate(); // 立即评估并应用最终状态

                    Debug.Log($"【TimelineManager】已将Timeline {director.playableAsset.name} 跳转到最终位置");

                    // 延迟一帧销毁，确保最终状态已应用
                    DestroyImmediate(go);
                }
            }
        }

        currentPlayers.Clear();
        // 5. 重置临时变量

        tempPlaybleDirector = playableDirector;
        
    }

    #region HELPER FUNCTION

    /// <summary>
    /// Event调用播放多个timeline的协程
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="new_playableDirector">临时创建的新playable director</param>
    /// <param name="action">回调执行的委托</param>
    /// <returns></returns>
    private IEnumerator WaitTimelinesPlay(TimelineAsset[] timelineAssets, Action action)
    {
        foreach (var timeline in timelineAssets)
        {
            var playableDirectorGameObject = new GameObject(timeline.name);
            var new_playableDirector = playableDirectorGameObject.AddComponent<PlayableDirector>();//播放Timeline时会临时添加一个PlaybleDirector
            new_playableDirector.extrapolationMode = DirectorWrapMode.None; //初始化
            new_playableDirector.playOnAwake = false;
            ResetTimelineBinding(timeline, new_playableDirector);
            new_playableDirector.Play(timeline);
            //while (new_playableDirector.state.Equals(PlayState.Playing))
            //    yield return null;

            yield return new WaitForSeconds((float)new_playableDirector.playableAsset.duration);

            if (new_playableDirector != null)
            {
                Debug.Log("Play Finished"+timeline.name);
                if (new_playableDirector.gameObject.activeInHierarchy)//创建的新PlayableDirector物体在场景中激活才会执行回调
                    action?.Invoke();
                Destroy(new_playableDirector.gameObject); //播放完毕后销毁创建的新物体
            }
        }
        //action?.Invoke();
    }

    /// <summary>
    /// Event调用播放单个timeline的协程
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="new_playableDirector">临时创建的新playable director</param>
    /// <param name="action">回调执行的委托</param>
    /// <returns></returns>
    private IEnumerator WaitTimelinePlay(TimelineAsset asset, PlayableDirector new_playableDirector, Action action)
    {
        ResetTimelineBinding(asset, new_playableDirector);
        new_playableDirector.Play(asset);
        currentPlayers.Add(new_playableDirector);

        yield return new WaitForSeconds((float)new_playableDirector.playableAsset.duration);

        //while (new_playableDirector.state.Equals(PlayState.Playing))
        //    yield return null;

        if (new_playableDirector != null)
        {
            if (new_playableDirector.gameObject.activeInHierarchy)//创建的新PlayableDirector物体在场景中激活才会执行回调
                action?.Invoke();
            currentPlayers.Remove(new_playableDirector);
            Destroy(new_playableDirector.gameObject); //播放完毕后销毁创建的新物体
        }
    }

    /// <summary>
    /// timeline End Action 
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="new_playableDirector">临时创建的新playable director</param>
    /// <param name="action">回调执行的委托</param>
    /// <returns></returns>
    private IEnumerator ReachEndPoint(PlayableDirector director, Action action)
    {
        yield return new WaitForSeconds((float)director.playableAsset.duration);
        if (director != null)
        {
            if (director.gameObject.activeInHierarchy)
                action?.Invoke();
        }
    }


    /// <summary>
    /// 获取原PlayableDirector上tracks和object的bindings并复制到新的Director上
    /// </summary>
    /// <param name="timelineAsset"></param>
    /// <param name="new_playableDirector">临时创建的新playable director</param>
    private void ResetTimelineBinding(TimelineAsset timelineAsset, PlayableDirector new_playableDirector)
    {
        tempPlaybleDirector.playableAsset = timelineAsset;
        new_playableDirector.playableAsset = timelineAsset;


        List<PlayableBinding> newBindingList = new List<PlayableBinding>();
        List<PlayableBinding> oldBindingList = new List<PlayableBinding>();

        foreach (PlayableBinding pb in tempPlaybleDirector.playableAsset.outputs)
        {
            oldBindingList.Add(pb);
        }

        foreach (PlayableBinding pb in new_playableDirector.playableAsset.outputs)
        {
            newBindingList.Add(pb);
        }

        new_playableDirector.playableAsset = timelineAsset;

        for (int i = 0; i < oldBindingList.Count; i++)
        {
            new_playableDirector.SetGenericBinding(newBindingList[i].sourceObject, tempPlaybleDirector.GetGenericBinding(oldBindingList[i].sourceObject));
        }

     

        //绑定
        PlayableDirectorPlayer player = null;
        if (this.gameObject.TryGetComponent<PlayableDirectorPlayer>(out player))
        {
            PlayableDirectorPlayer new_player = null;
            new_player = new_playableDirector.gameObject.AddComponent<PlayableDirectorPlayer>();
            new_player.handleType = player.handleType;
        }

        //绑定 signal
        SignalReceiver receiver = null;
        if (this.gameObject.TryGetComponent<SignalReceiver>(out receiver))
        {
            SignalReceiver new_reciver;
            new_reciver = new_playableDirector.gameObject.AddComponent<SignalReceiver>();
            List<SignalAsset> signalAssets = new List<SignalAsset>();
            List<UnityEvent> events = new List<UnityEvent>();
            for (int i = 0; i < receiver.Count(); i++)
            {
                new_reciver.AddReaction(receiver.GetSignalAssetAtIndex(i), receiver.GetReactionAtIndex(i) != null ? receiver.GetReactionAtIndex(i) : null);
            }
        }

        List<Object> tempCams = new List<Object>();
        List<PropertyName> exposedNames = new List<PropertyName>();
        for (int i = 0; i < timelineAsset.outputTrackCount; i++)
        {
            //bug.Log(timelineAsset.GetRootTrack(i).GetType());
            if (timelineAsset.GetRootTrack(i).GetType() == typeof(Timeline.Extra.VideoTrack))
            {
                foreach (var clip in timelineAsset.GetRootTrack(i).GetClips())
                {
                    Timeline.Extra.VideoPlayableAsset asset = (Timeline.Extra.VideoPlayableAsset)(clip.asset);
                    exposedNames.Add(asset.targetCamera.exposedName);
                    bool idValid = false;
                    var target= playableDirector.GetReferenceValue(asset.targetCamera.exposedName,out idValid);
                    tempCams.Add(idValid == true ? target:null);
                }
            }
        }
        for (int i = 0; i < exposedNames.Count; i++)
        {
            new_playableDirector.SetReferenceValue(exposedNames[i], tempCams[i]);
        }



        tempPlaybleDirector.playableAsset = null;
    }

    #endregion HELPER FUNCTION

}
