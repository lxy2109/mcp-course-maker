using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NodeGraph
{
    [Serializable]
    public class GroupData
    {
        public string GUID;
        public string title;
        public float posX;
        public float posY;
        public float width;
        public float height;
        public List<string> nodeGuids=new List<string>();

        public Group group=null;

        public GroupData(string gUID, string title, float posX, float posY, float width, float height, List<string> nodeGuids)
        {
            GUID = gUID;
            this.title = title;
            this.posX = posX;
            this.posY = posY;
            this.width = width;
            this.height = height;
            this.nodeGuids = nodeGuids;
        }
    }



    public class NodeGraphView : GraphView
    {
        private NodeCreateWindowContent m_NodeCreateWindow;
        public static NodeGraphView instance = null;
        public List<GroupData> groupDataList = new List<GroupData>();
        public NodeGraphView(EditorWindow window, StyleSheet styleSheet)
        {
            instance = this;
            if (styleSheet != null) styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale,ContentZoomer.DefaultMaxScale);//zoom     


          //  Insert(0,new GridBackground());

            this.AddManipulator(new ContentDragger());

            this.AddManipulator(new SelectionDragger());
            
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());
        

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddNodeCreateWindow(window);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }


        
        private void AddNodeCreateWindow(EditorWindow window)
        {
            m_NodeCreateWindow = ScriptableObject.CreateInstance<NodeCreateWindowContent>();
            m_NodeCreateWindow.Configure(window, this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), m_NodeCreateWindow);
        }
        public void CreateGroupWithSelectedNodes()
        {
            // 如果没有选定任何节点，就不创建组
            if (selection.Count == 0)
                return;

            // 创建一个新的Group并设置属性
            var group = new Group
            {
                title = "选择组"
            };

            string groupGUID = System.Guid.NewGuid().ToString();


            List<string> nodeGUIDs = new List<string>();
            // 将选定的节点添加到新组中
            foreach (var element in selection)
            {
              


                if (element is Node node )
                {
                    group.AddElement(node);

                    Type t = element.GetType();
                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                    foreach (FieldInfo field in t.GetFields(bindingFlags))
                    {
                        if (field.FieldType == typeof(string)&&field.Name=="GUID")
                        {
                            object fieldValue = field.GetValue(node);
                           // Debug.Log($"Field '{field.Name}' of type '{field.FieldType}': {fieldValue}");
                            nodeGUIDs.Add((string)fieldValue);
                        }
                    }

                    // nodeGUIDs.Add(((t.GetType())node).GUID);
                }

            }

            // 将组添加到GraphView中
            AddElement(group);
            EditorApplication.delayCall += () =>
            {
                Vector2 groupPosition = group.GetPosition().position;
      
                var groupData = new GroupData(groupGUID, group.title, group.GetPosition().x, group.GetPosition().y
                    , group.GetPosition().width, group.GetPosition().height,nodeGUIDs);
                groupData.group = group;

                groupDataList.Add(groupData);
            };

           
        }
        public void RemoveElementsFromGroup(Group group)
        {
            

            // 获取组内所有节点
            var groupNodes = group.containedElements.ToList();

            // 将节点从组中移除
            foreach (var element in groupNodes)
            {
                if (element is Node node)
                {
                    group.RemoveElement(node);
                }
            }

            groupDataList.Remove(groupDataList.Find(x => x.group == group));
        }
        public override EventPropagation DeleteSelection()
        {
            // 检查当前选定的元素
            var selectedGroups = selection.OfType<Group>().ToList();

            // 从组中移除节点
            foreach (var group in selectedGroups)
            {
                RemoveElementsFromGroup(group);
            }

            // 调用基类的DeleteSelection方法删除组
            base.DeleteSelection();
            return EventPropagation.Stop;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("创建组", action =>
            {
                CreateGroupWithSelectedNodes();
            }, DropdownMenuAction.AlwaysEnabled);
        }

    }
}