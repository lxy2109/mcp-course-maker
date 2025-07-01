using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationClipGroup : ScriptableObject
{
    public string message = "";

#if UNITY_EDITOR
    public void AddAnimationClip(AnimationClip original)
    {
        AnimationClip clip = Instantiate(original);
        clip.name = original.name;

        AssetDatabase.AddObjectToAsset(clip, this);
        string path = AssetDatabase.GetAssetPath(this);
        AssetDatabase.ImportAsset(path);
    }
#endif
}

