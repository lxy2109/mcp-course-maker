using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public class NodeCreateWindowContent : ScriptableObject, ISearchWindowProvider
    {
        private EditorWindow m_Window;
        private NodeGraphView m_NodeGraphView;

        private Texture2D _indentationIcon;
        private Type[] nodeTypes;

        private Type[] valueNodeTypes;
        private Type[] baseNodeTypes;
        private Type[] floweventNodeTypes;
        
        public void Configure(EditorWindow window, NodeGraphView graphView)
        {
            m_Window = window;
            m_NodeGraphView = graphView;

            var editorAssembly = typeof(NodeBase<>).Assembly;
            editorAssembly.GetTypes();
            
            nodeTypes = editorAssembly.GetTypes()
                .Where(a => a.GetInterfaces().Contains(typeof(INode)))
                .ToArray();

            valueNodeTypes = editorAssembly.GetTypes()
                .Where(a => a.GetInterfaces().Contains(typeof(IValueNode)))
                .ToArray();
            baseNodeTypes = editorAssembly.GetTypes()
            .Where(a => a.GetInterfaces().Contains(typeof(IBaseNode)))
            .ToArray();
            floweventNodeTypes = editorAssembly.GetTypes()
.Where(a => a.GetInterfaces().Contains(typeof(IEventNode)))
.ToArray();
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
               // new SearchTreeGroupEntry(new GUIContent("Value"), 1),
            };
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Base"), 1));
            if (baseNodeTypes != null && baseNodeTypes.Length > 0)
            {
                for (int i = 0; i < baseNodeTypes.Length; i++)
                {
                    var entry = new SearchTreeEntry(new GUIContent(baseNodeTypes[i].FullName.Replace("Node", "")))
                    {
                        level = 2,
                        userData = baseNodeTypes[i].FullName
                    };

                    tree.Add(entry);
                }
            }
            tree.Add(new SearchTreeGroupEntry(new GUIContent("FlowEvent"), 1));
            if (floweventNodeTypes != null && floweventNodeTypes.Length > 0)
            {
                for (int i = 0; i < floweventNodeTypes.Length; i++)
                {
                    var entry = new SearchTreeEntry(new GUIContent(floweventNodeTypes[i].FullName.Replace("Node", "")))
                    {
                        level = 2,
                        userData = floweventNodeTypes[i].FullName
                    };
                    tree.Add(entry);
                }
            }


            tree.Add(new SearchTreeGroupEntry(new GUIContent("Value"), 1));
            if (valueNodeTypes != null && valueNodeTypes.Length > 0)
            {
                for (int i = 0; i < valueNodeTypes.Length; i++)
                {
                    var entry = new SearchTreeEntry(new GUIContent(valueNodeTypes[i].FullName.Replace("Node","")))
                    {
                        level = 2,
                        userData = valueNodeTypes[i].FullName
                    };
                    tree.Add(entry);
                }
            }
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Example"), 1));
            if (nodeTypes != null && nodeTypes.Length > 0)
            {
                for (int i = 0; i < nodeTypes.Length; i++)
                {
                    var entry = new SearchTreeEntry(new GUIContent(nodeTypes[i].FullName.Replace("Node", "")))
                    {
                        level = 2,
                        userData = nodeTypes[i].FullName
                    };

                    tree.Add(entry);
                }
            }
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            //Editor window-based mouse position
            var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
                context.screenMousePosition - m_Window.position.position);
            var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);
            
            var editorAssembly = typeof(NodeBase<>).Assembly;
            var typeName = (string) SearchTreeEntry.userData;
           // Debug.Log(typeName);
            var newNode = editorAssembly.CreateInstance(typeName);
            var nodeType = newNode.GetType();
            var onCreated = nodeType.GetMethod("OnCreated");
            onCreated?.Invoke(newNode, new[] {m_NodeGraphView});
            var node = newNode as Node;
            node.SetPosition(new Rect(graphMousePosition, node.GetPosition().size));
            m_NodeGraphView.AddElement(node);
            
            return true;
        }


        //public Node CreatNode(string nodeTypeName )
        //{
        //    var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
        //       m_Window.position.position);
        //    var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);

        //    var editorAssembly = typeof(NodeBase<>).Assembly;
        //    var typeName = nodeTypeName;
        //    var newNode = editorAssembly.CreateInstance(typeName);
        //    var nodeType = newNode.GetType();
        //    var onCreated = nodeType.GetMethod("OnCreated");
        //    onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
        //    var node = newNode as Node;
        //    node.SetPosition(new Rect(graphMousePosition+new Vector2(400,0), node.GetPosition().size));
        //    m_NodeGraphView.AddElement(node);
        //    return node;
        //}


        //public Node CreatNode(string nodeTypeName,string eventName,int index=0)
        //{
        //    var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
        //       m_Window.position.position);
        //    var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);

        //    var editorAssembly = typeof(NodeBase<>).Assembly;
        //    var typeName = nodeTypeName;
        //    var newNode = editorAssembly.CreateInstance(typeName);
        //    var nodeType = newNode.GetType();

        //    if (nodeType == typeof(FlowEventNode))
        //    {
        //        FlowEventNode temp =newNode as FlowEventNode;
        //        temp.eventName = eventName;
        //    }


        //    var onCreated = nodeType.GetMethod("OnCreated");
        //    onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
        //    var node = newNode as Node;
        //    node.SetPosition(new Rect(graphMousePosition + new Vector2(400*(index+1), 0), node.GetPosition().size));
        //    m_NodeGraphView.AddElement(node);
        //    return node;
        //}

        public Node CreatNode(string nodeTypeName)
        {
            var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
               m_Window.position.position);
            var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);

            var editorAssembly = typeof(NodeBase<>).Assembly;
            var typeName = nodeTypeName;
            var newNode = editorAssembly.CreateInstance(typeName);
            var nodeType = newNode.GetType();
            var onCreated = nodeType.GetMethod("OnCreated");
            onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
            var node = newNode as Node;
            node.SetPosition(new Rect(graphMousePosition + new Vector2(50 , 50), node.GetPosition().size));
            m_NodeGraphView.AddElement(node);
            return node;
        }
        public Node CreatNode(string nodeTypeName, Vector2 pos)
        {
            var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
               m_Window.position.position);
            var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);

            var editorAssembly = typeof(NodeBase<>).Assembly;
            var typeName = nodeTypeName;
            var newNode = editorAssembly.CreateInstance(typeName);
            var nodeType = newNode.GetType();
            var onCreated = nodeType.GetMethod("OnCreated");
            onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
            var node = newNode as Node;
            node.SetPosition(new Rect(pos, node.GetPosition().size));
            m_NodeGraphView.AddElement(node);
            return node;
        }
        
        public Node CreatNode(string nodeTypeName, FlowNodeTempAsset asset, AudioClip clip, int index = 0)
        {
            var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
               m_Window.position.position);
            var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);
        
            var editorAssembly = typeof(NodeBase<>).Assembly;
            var typeName = nodeTypeName;
            var newNode = editorAssembly.CreateInstance(typeName);
            var nodeType = newNode.GetType();
        
            if (nodeType == typeof(FlowEventNode))
            {
                FlowEventNode temp = newNode as FlowEventNode;
                // 设置基本属性
                temp.flowGraph = asset.flowGraph;
                temp.eventName = asset.eventName;
                temp.eventContent = asset.eventContent;
                
                // 设置事件相关属性
                temp.enterEventName = asset.enterEventName;
                temp.enterEventContent = asset.enterEventContent;
                temp.exitEventName = asset.exitEventName;
                temp.exitEventContent = asset.exitEventContent;
                
                // 设置音频相关属性
                temp.inAudioClip = clip;
                // 可以保存音频名称和内容作为引用
                // temp.voiceName = asset.voiceName; // 如果FlowEventNode添加了此属性
                // temp.voiceContent = asset.voiceContent; // 如果FlowEventNode添加了此属性
                
                // 设置时间线相关属性
                temp.cameraTimelineName = asset.cameraTimelineName;
                temp.cameraTimelineContent = asset.cameraTimelineContent;
                temp.objectTimelineName = asset.objectTimelineName;
                temp.objectTimelineContent = asset.objectTimelineContent;
                
                // 如果handTip是要高亮的物体名称，可以在这里进行初始化处理
                // 例如查找场景中相应的物体并添加到selectableObects列表中
                // GameObject obj = GameObject.Find(asset.handTip);
                // if(obj != null) {
                //     if(temp.selectableObects == null) temp.selectableObects = new List<GameObject>();
                //     temp.selectableObects.Add(obj);
                // }
            }
        
            var onCreated = nodeType.GetMethod("OnCreated");
            onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
            var node = newNode as Node;
            node.SetPosition(new Rect(Vector2.zero + new Vector2(300 * (index % 5), 300 * Mathf.Floor((float)index / 5.0f)), node.GetPosition().size));
            m_NodeGraphView.AddElement(node);
            return node;
        }
        
        public Node CreatNode(string nodeTypeName, FlowNodeTempAsset asset, AudioClip clip, Vector2 pos)
        {
            var mousePosition = m_Window.rootVisualElement.ChangeCoordinatesTo(m_Window.rootVisualElement.parent,
               m_Window.position.position);
            var graphMousePosition = m_NodeGraphView.contentViewContainer.WorldToLocal(mousePosition);
        
            var editorAssembly = typeof(NodeBase<>).Assembly;
            var typeName = nodeTypeName;
            var newNode = editorAssembly.CreateInstance(typeName);
            var nodeType = newNode.GetType();
        
            if (nodeType == typeof(FlowEventNode))
            {
                FlowEventNode temp = newNode as FlowEventNode;
                
                // 设置基本属性
                temp.flowGraph = asset.flowGraph;
                temp.eventName = asset.eventName;
                temp.eventContent = asset.eventContent;
                
                // 设置事件相关属性
                temp.enterEventName = asset.enterEventName;
                temp.enterEventContent = asset.enterEventContent;
                temp.exitEventName = asset.exitEventName;
                temp.exitEventContent = asset.exitEventContent;
                
                // 设置音频相关属性
                temp.inAudioClip = clip;
                // 可以保存音频名称和内容作为引用
                // temp.voiceName = asset.voiceName; // 如果FlowEventNode添加了此属性
                // temp.voiceContent = asset.voiceContent; // 如果FlowEventNode添加了此属性
                
                // 设置时间线相关属性
                temp.cameraTimelineName = asset.cameraTimelineName;
                temp.cameraTimelineContent = asset.cameraTimelineContent;
                temp.objectTimelineName = asset.objectTimelineName;
                temp.objectTimelineContent = asset.objectTimelineContent;
                
                // 如果handTip是要高亮的物体名称，可以在这里进行初始化处理
                // 例如查找场景中相应的物体并添加到selectableObects列表中
                // GameObject obj = GameObject.Find(asset.handTip);
                // if(obj != null) {
                //     if(temp.selectableObects == null) temp.selectableObects = new List<GameObject>();
                //     temp.selectableObects.Add(obj);
                // }
            }
        
            var onCreated = nodeType.GetMethod("OnCreated");
            onCreated?.Invoke(newNode, new[] { m_NodeGraphView });
            var node = newNode as Node;
            node.SetPosition(new Rect(pos, node.GetPosition().size));
            m_NodeGraphView.AddElement(node);
            return node;
        }

    }


}