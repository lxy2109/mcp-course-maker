using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Linq;

public class TTSUtillity 
{
   // private string url = "http://nls-meta.cn-shanghai.aliyuncs.com/";
    private static string usertokenid = "09be41ce1e9049dc9d99ea54da5924a0";
    private static string appkey = "dmUpTMXx6sevgibu";


    public static  IEnumerator TTS(string content,string fileName="tts",string fold="voice")
    {
        string url = string.Format("https://nls-gateway.cn-shanghai.aliyuncs.com/stream/v1/tts?appkey={0}&token={1}&text={2}&format=wav&sample_rate=16000&voice=voice-6711010&speech_rate=117", appkey, usertokenid, content);
        Uri uri = new Uri(url);

        UnityWebRequest WebRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);

        yield return WebRequest.SendWebRequest();
        if (WebRequest.result == UnityWebRequest.Result.ConnectionError || WebRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(WebRequest.error);
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(WebRequest);

            string savepath = Application.dataPath+"/Resources/Lesson";
           Debug.Log(savepath);
            WavUtility.FromAudioClip(clip, out savepath, true, fold, fileName);
            UnityEditor.AssetDatabase.Refresh();

        }

    }

    public static IEnumerator TTS(List<FlowNodeTempAsset> assets,Action callback)
    {
        EditorUtility.DisplayProgressBar("从Excel中生成节点中", "创建音频中", 0);
        for (int i = 0; i < assets.Count; i++)
        {
      
            if (string.IsNullOrEmpty(assets[i].voiceContent))  continue;

            EditorUtility.DisplayProgressBar("从Excel中生成节点中", "创建音频" + assets[i].voiceName, i / assets.Count);

            string url = string.Format("https://nls-gateway.cn-shanghai.aliyuncs.com/stream/v1/tts?appkey={0}&token={1}&text={2}&format=wav&sample_rate=16000", appkey, usertokenid, assets[i].voiceContent);
            Uri uri = new Uri(url);

            UnityWebRequest WebRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);

            yield return WebRequest.SendWebRequest();
            if (WebRequest.result == UnityWebRequest.Result.ConnectionError || WebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(WebRequest.error);
                EditorUtility.ClearProgressBar();
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(WebRequest);

                string savepath="";
                Debug.Log(i.ToString() + "_" + assets[i].eventName);
                Debug.Log(assets[i].voiceName);
                WavUtility.FromAudioClip(clip, out savepath, true, i.ToString() + "_" + assets[i].eventName, assets[i].voiceName);
            
             
                yield return 1;
            }

           
        }
        UnityEditor.AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        callback?.Invoke();

    }

}
