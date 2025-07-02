#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ModelParameterLib.Core; // 确保命名空间正确

[InitializeOnLoad]
public class AutoPlacerHierarchyLabel
{
    static AutoPlacerHierarchyLabel()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;
        // 只为带有AutoPlacer组件的物体显示标签
        if (obj.GetComponent<AutoPlacer>() != null)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.yellow;
            string label = "[模型y轴贴合]";
            // 动态计算文本宽度
            Vector2 size = style.CalcSize(new GUIContent(label));
            float width = size.x + 10f; // 适当加padding
            EditorGUI.LabelField(
                new Rect(selectionRect.xMax - width, selectionRect.y, width, selectionRect.height),
                label, style);
        }
    }
}
#endif
