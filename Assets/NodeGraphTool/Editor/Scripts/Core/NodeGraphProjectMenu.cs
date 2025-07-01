using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

namespace NodeGraph
{
    public class NodeGraphProjectMenu : MonoBehaviour
    {
        [MenuItem("Assets/Open NodeGraph", true)]
        private static bool ValidateOpenNodeGraph()
        {
            if (Selection.activeObject is NodeGraph)
            {
                return true; //返回true菜单可用，返回false菜单灰色不可用
            }
            return false;
        }

        [MenuItem("Assets/Open NodeGraph", false, 1)]
        private static void OpenNodeGraph()
        {
            if (Selection.activeObject is NodeGraph)
            {
                NodeGraphWindow.OpenNodeGraphWindow(Selection.activeObject.name, Selection.activeObject as NodeGraph);
            }
        }

        [OnOpenAssetAttribute(1)]
        public static bool CallOpenNodeGraph(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj is NodeGraph)
            {
                NodeGraphWindow.OpenNodeGraphWindow(Selection.activeObject.name, obj as NodeGraph);
                return true;
            }
            return false;
        }
    }
}