using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public  class ExtraMenu:Editor
{
    [MenuItem("GameObject/模板/创建轨道")]
    private static void CreatTrack()
    {
        GameObject path = new GameObject("TrackPath");
        path.AddComponent<TrackPath>();

        GameObject cart = new GameObject("TrackCart");
        cart.AddComponent<TrackCart>();
        cart.GetComponent<TrackCart>().path=path.GetComponent<TrackPath>();
    }

/*    [MenuItem("GameObject/模板/创建人物")]
    private static void CreatCharacter()
    {
        GameObject chararcter =(GameObject) PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Character"));
        chararcter.transform.parent=GameObject.Find("GameObjectRoot").transform;
        if (!GameObject.FindObjectOfType<LocalNavMeshBuilder>())
        {
            GameObject meshBaker = new GameObject("Baker");
            meshBaker.AddComponent<LocalNavMeshBuilder>();
        }
    }*/

    [MenuItem("GameObject/模板/创建文字转语音")]
    private static void CreatTextToAudio()
    {
        //GameObject textAudioPlayer = new GameObject("TextAudioPlayer");
        //textAudioPlayer.AddComponent<AudioSource>();
        //textAudioPlayer.AddComponent<XunFeiSpeech.TTS.TextAudioBehaiver>();
        //GameObject textToAudio = new GameObject("TextToAudio");
        //textToAudio.AddComponent<TextToAudio>();
    }

    [MenuItem("GameObject/模板/创建滑动事件")]
    private static void CreatMouseTrail()
    {
        GameObject mouseTrail = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("ClassPrefab/MouseTrail"));
        mouseTrail.transform.SetAsLastSibling();
    }
    [MenuItem("GameObject/模板/创建手部")]
    private static void CreatHandTween()
    {
        GameObject handTween = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("ClassPrefab/HandTween"));
        handTween.transform.SetAsLastSibling();
    }
}
