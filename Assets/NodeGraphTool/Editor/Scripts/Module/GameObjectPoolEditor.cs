using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(GameObjectPool))]
public class GameObjectPoolEditor : OdinEditor
{
   public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GameObjectPool gameObjectPool = (GameObjectPool)target;
        if (Application.isPlaying) return;
        gameObjectPool.GetChildrenRoot();
        gameObjectPool.CheckChildrenResgiter();

    }

}
