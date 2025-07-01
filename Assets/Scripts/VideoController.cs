using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public UnityEvent OnVideoEnd;
    public VideoPlayer videoPlayer;
    // Start is called before the first frame update
    public void StartVideo()
    {
        videoPlayer.loopPointReached += EndWithVideoPlayer;
    }

    // Update is called once per frame
    public void EndWithVideoPlayer(VideoPlayer vp)
    {
        OnVideoEnd?.Invoke();
    }
}
