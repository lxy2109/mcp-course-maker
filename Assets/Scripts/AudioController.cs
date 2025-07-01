using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoBehaviour
{

    public AudioMixer audioMixer;    // 控制主音量的Mixer对象
    public bool isMute = false;
    public void SetMasterVolume()     // 切换主音量静音状态
    {
        isMute = !isMute;
        if (isMute)
        {
            audioMixer.SetFloat("Master", -80);
        }
        else
        {
            audioMixer.SetFloat("Master", 0);
        }

    }

}