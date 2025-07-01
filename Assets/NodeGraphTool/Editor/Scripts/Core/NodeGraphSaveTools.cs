

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public static class NodeGraphSaveTools
    {
        /// <summary>
        /// 自动将链接combine节点的node-设置为HoldForCombine
        /// </summary>
        /// <param name="window"></param>
        public static void UpdateCombineSettings(NodeGraphWindow window)
        {
            var view = window.NodeGraphView;
            var edges = view.edges.ToList();
            var nodes = view.nodes.ToList();

            var connectedPorts = edges;
            if (connectedPorts != null)
            {
                for (int i = 0; i < connectedPorts.Count(); i++)
                {
                    if (connectedPorts[i].output == null || connectedPorts[i].input == null)
                    {
                        return;
                    }

                    var outputNode = connectedPorts[i].output.node;
                    var inputNode = connectedPorts[i].input.node;


                    if (outputNode == null || inputNode == null) continue;

                    if (inputNode.GetType() == typeof(CombineNode))
                    {
                        if (outputNode.GetType() == typeof(FlowEventNode))
                        {
                            ((FlowEventNode)outputNode).endActionEnum = EventEndAction.HoldForCombine;
                            ((FlowEventNode)outputNode).endActionEnumField.SetValueWithoutNotify(((FlowEventNode)outputNode).endActionEnum);
                            //SetValueWithoutNotify
                        }
                    }

                }
            }
        }


        public static void UpdateCompareSettings(NodeGraphWindow window)
        {
            var view = window.NodeGraphView;
            var edges = view.edges.ToList();
            var nodes = view.nodes.ToList();

            var connectedPorts = edges;
            if (connectedPorts != null)
            {
                for (int i = 0; i < connectedPorts.Count(); i++)
                {
                    if (connectedPorts[i].output == null || connectedPorts[i].input == null)
                    {
                        return;
                    }

                    var outputNode = connectedPorts[i].output.node;
                    var inputNode = connectedPorts[i].input.node;


                    if (outputNode == null || inputNode == null) continue;

                    if ( connectedPorts[i].output.portType==typeof(float)&& connectedPorts[i].input.portType == typeof(float))
                    {
                       if(outputNode.GetType() == typeof(EventNode) && inputNode.GetType() == typeof(CompareNode) )
                        ((CompareNode)inputNode).title ="Compare  "+ connectedPorts[i].output.portName;
                    }
  
                }
            }
        }

        #region Save

        public static void Save(NodeGraphWindow window)
        {
            var view = window.NodeGraphView;
            var edges = view.edges.ToList();
            var nodes = view.nodes.ToList();
            var groups=view.groupDataList.ToList();
            //view.g
            
            var so = window.Data;
            so.ClearAllNodeDatas();
            
            var runtimeAssembly = typeof(NodeGraph).Assembly;
            
            //保存node
            foreach (var node in nodes)
            {
                var data = NodeToData(node, runtimeAssembly, out var dataTypeName);
                so.AddNodeToListFromTypeName(dataTypeName, data);
            }

            //保存选择组
            foreach (var group in groups)
            {
                //检测group title
                if(group.group!=null)
                group.title = group.group.title;

                GroupSavedData data = new GroupSavedData(group.GUID, group.title, group.posX, group.posY,group.width,group.height, group.nodeGuids);
                so.AddGroup(data);
            }

                //保存link
                var connectedPorts = edges?.Where(x=>x.input.node!=null)?.ToArray();
            if (connectedPorts != null)
            {
                for (int i = 0; i < connectedPorts.Count(); i++)
                {
                    var outputNode = connectedPorts[i].output.node;
                    var inputNode = connectedPorts[i].input.node;

                    var outputNodeType = outputNode.GetType();
                    var outputGuidField = outputNodeType.GetField("GUID");
                    var targetNodeNodeType = inputNode.GetType();
                    var targetGuidField = targetNodeNodeType.GetField("GUID");

                    so.Links.Add(new NodeLinkData
                    {
                        BaseNodeGUID = outputGuidField.GetValue(outputNode).ToString(),
                        OutputPortName = connectedPorts[i].output.portName,
                        BaseNodeType = outputNodeType.Name,
                        TargetNodeGUID = targetGuidField.GetValue(inputNode).ToString(),
                        TargetPortName = connectedPorts[i].input.portName,
                        TargetNodeType = targetNodeNodeType.Name,
                    });
                }
            }




            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static object NodeToData(Node node, Assembly runtimeAssembly, out string dataTypeName)
        {
            var nodeType = node.GetType();
            var nodeNameField = nodeType.GetField("NodeName");
            var nodeGuidField = nodeType.GetField("GUID");

        //    Debug.Log(nodeNameField.GetValue(node));

            if (IsStringFieldValid(nodeNameField) && IsStringFieldValid(nodeGuidField))
            {
                string nodeTypeName = nodeNameField.GetValue(node).ToString();
                dataTypeName = $"{nodeTypeName}Data";
                var data = runtimeAssembly.CreateInstance(dataTypeName);
                CopyData(node, data);
                FillPosition(node, data);
                return data;
            }

            dataTypeName = string.Empty;
            return null;
        }

        private static bool IsStringFieldValid(FieldInfo field)
        {
            return field != null && field.FieldType.Name.StartsWith("String");
        }

        private static void FillPosition(Node node, object data)
        {
            //position
            var dataSourceType = data.GetType();
            var dataPositionField = dataSourceType.GetField("Position");
            var rect = node.GetPosition();
            dataPositionField.SetValue(data, rect);
        }
        #endregion

        #region Load

        public static void Load(NodeGraphWindow window, NodeGraph data)
        {
            var view = window.NodeGraphView;
            
            //加载节点
            var nodeDatas = data.GetAllNodeDatas();
            foreach (var nodeData in nodeDatas)
            {
                var node = LoadNode(nodeData, nodeData.NodeDataName, view);
                view.AddElement((GraphElement) node);
            }

            //加载link
            var nodes = view.nodes.ToList();
            nodes.ForEach(node =>
            {
                var nodeType = node.GetType();
                var nodeGuidField = nodeType.GetField("GUID");
                var guid = nodeGuidField.GetValue(node).ToString();
                var connections = data.Links?.Where(x => String.Equals(x.BaseNodeGUID, guid))?.ToList();
                if (connections != null)
                {
                    connections.ForEach(connection =>
                    {
                        var targetNodeGuid = connection.TargetNodeGUID;
                        var targetNode = nodes.First(x =>
                        {
                            var xType = x.GetType();
                            var xGuidField = xType.GetField("GUID");
                            var xGuid = xGuidField.GetValue(x).ToString();
                            return String.Equals(xGuid, targetNodeGuid);
                        });
                        var outputPort = node.outputContainer.Query<Port>().ToList().First(p =>
                            p.portName.Equals(connection.OutputPortName));
                        var targetPort = targetNode.inputContainer.Query<Port>().ToList().First(p =>
                            p.portName.Equals(connection.TargetPortName));
                        LinkNodes(view, outputPort, targetPort);
                       // Debug.Log("Link");
                    });
                }
            });







            var groupSavedDatas = data.GetAllGroup();
            foreach (var groupSavedData in groupSavedDatas)
            {
                var group = new Group
                {
                    title = groupSavedData.title,
                };
                group.SetPosition(new Rect(groupSavedData.posX, groupSavedData.posY, groupSavedData.width, groupSavedData.height));

                foreach (var node in nodes)
                {
                    Type t = node.GetType();
                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                    string fieldValue="";
                    foreach (FieldInfo field in t.GetFields(bindingFlags))
                    {
                        if (field.FieldType == typeof(string) && field.Name == "GUID")
                        {
                            fieldValue = (string)field.GetValue(node);
                        }
                            
                    }
   
                    if (groupSavedData.nodeGuids.Contains(fieldValue))
                    {
                        group.AddElement(node);
                    }
                }


                view.AddElement(group);
                EditorApplication.delayCall += () =>
                {
                    Vector2 groupPosition = group.GetPosition().position;

                    var groupData = new GroupData(groupSavedData.GUID, group.title, group.GetPosition().x, group.GetPosition().y
                        , group.GetPosition().width, group.GetPosition().height, groupSavedData.nodeGuids);
                    groupData.group = group;
                    view.groupDataList.Add(groupData);
                };
            }
        }
        
        private static void LinkNodes(GraphView view, Port outputSocket, Port inputSocket)
        {
            var tempEdge = new Edge()
            {
                output = outputSocket,
                input = inputSocket
            };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            view.Add(tempEdge);
        }

        private static object LoadNode(object data,string dataTypeName, NodeGraphView view)
        {
            var editorAssembly = typeof(NodeBase<>).Assembly;
            var node = editorAssembly.CreateInstance(((NodeBaseData) data).NodeName);
            CopyData(data, node);
            
            var nodeType = node.GetType();
            var onCreated = nodeType.GetMethod("OnCreated");
            var onLoadad = nodeType.GetMethod("OnLoadad");

            onCreated?.Invoke(node, new[] {view});
            onLoadad?.Invoke(node, new[] { view });
            SetPosition((Node)node, data);
            return node;
        }

        private static void SetPosition(Node node, object data)
        {
            var dataSourceType = data.GetType();
            var dataPositionField = dataSourceType.GetField("Position");
            var rect = dataPositionField.GetValue(data);
            node.SetPosition((Rect)rect);
        }
        
        #endregion
        
        private static void CopyData(object dataSource, object dataTarget)
        {
            var dataSourceType = dataSource.GetType(); 
            var dataTargetType = dataTarget.GetType();
            var dataSourceFields = dataSourceType.GetFields().ToDictionary<FieldInfo, object>(p => p.Name);
            var dataTargetFields = dataTargetType.GetFields().ToDictionary<FieldInfo, object>(p => p.Name);
            foreach (var sourceField in dataSourceFields)
            {

                //Debug.Log(sourceField.Value.FieldType);
                if (dataTargetFields.ContainsKey(sourceField.Key))
                {
                    dataTargetFields[sourceField.Key].SetValue(dataTarget, sourceField.Value.GetValue(dataSource));
                    //if (sourceField.Value.FieldType.IsGenericType)
                    //{
                    //    string className = sourceField.Value.FieldType.GetGenericArguments()[0].Name;
                    //    var subObj = sourceField.Value.GetValue(dataSource);
                    //    var targetObj = dataTargetFields[sourceField.Key].GetValue(dataTarget);

                    //    targetObj.GetType().GetMethod("Clear")?.Invoke(targetObj, null);

                    //    if (subObj != null)
                    //    {
                    //        int count = Convert.ToInt32(subObj.GetType().GetProperty("Count").GetValue(subObj, null));
                    //        for (int i = 0; i < count; i++)
                    //        {
                    //            object item = subObj.GetType().GetProperty("Item").GetValue(subObj, new object[] { i });
                    //            Debug.Log(dataSource.GetType().Name + "         " + item);
                    //            targetObj.GetType().GetMethod("Add").Invoke(targetObj, new object[] { item });
                    //        }
                    //    }

                    //}

                }
            }
        }
    }
}