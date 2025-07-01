using UnityEngine;
using UnityEditor;
using NodeGraph;
using System.IO;

public class NodeGraphCreator : EditorWindow
{
    private string savePath = "Assets/Resources/NodeGraphs";
    private string graphName = "NewNodeGraph";
    
    [MenuItem("工具/创建NodeGraph")]
    public static void ShowWindow()
    {
        GetWindow<NodeGraphCreator>("NodeGraph创建工具");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("NodeGraph创建工具", EditorStyles.boldLabel);
        
        graphName = EditorGUILayout.TextField("节点图名称:", graphName);
        savePath = EditorGUILayout.TextField("保存路径:", savePath);
        
        if (GUILayout.Button("选择路径"))
        {
            string path = EditorUtility.OpenFolderPanel("选择保存路径", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对于Assets的路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                savePath = path;
            }
        }
        
        if (GUILayout.Button("创建NodeGraph"))
        {
            CreateNodeGraph();
        }
    }
    
    private void CreateNodeGraph()
    {
        // 确保目录存在
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        
        // 创建NodeGraph资产
        NodeGraph.NodeGraph nodeGraph = ScriptableObject.CreateInstance<NodeGraph.NodeGraph>();
        
        // 构建完整路径
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/{graphName}.asset");
        
        // 创建资产文件
        AssetDatabase.CreateAsset(nodeGraph, assetPathAndName);
        AssetDatabase.SaveAssets();
        
        // 聚焦到新创建的资产
        Selection.activeObject = nodeGraph;
        EditorGUIUtility.PingObject(nodeGraph);
        
        Debug.Log($"NodeGraph创建成功: {assetPathAndName}");
        
        // 可选：立即打开NodeGraph编辑器
        // NodeGraphWindow.OpenNodeGraphWindow(graphName, nodeGraph);
    }
}