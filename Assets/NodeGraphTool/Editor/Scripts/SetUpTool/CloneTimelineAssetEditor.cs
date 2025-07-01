using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(CloneTimelineAsset))]
[CanEditMultipleObjects]
public class CloneTimelineAssetEditor : Editor
{
    CloneTimelineAsset cloneTimeline;
    private SerializedObject obj;

    void OnEnable()
    {
        obj = new SerializedObject(target);
    }
    public override void OnInspectorGUI()
    {
        cloneTimeline = (CloneTimelineAsset)target;
        obj.Update();
     
        DrawDefaultInspector();
        if (GUILayout.Button("复制",GUILayout.Width(80)))
        {
            cloneTimeline.GetAssetInEditor();
        }
        EditorGUILayout.HelpBox("是这样，动画应该在一个timeline上完成，才能复制\n选择timeline点击复制即可，默认在根目录", MessageType.Info);
        obj.ApplyModifiedProperties();
    }
}
