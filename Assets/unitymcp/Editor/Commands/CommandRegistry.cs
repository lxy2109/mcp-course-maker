using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Registry for all MCP command handlers
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, Func<JObject, object>> _handlers = new()
        {
            // Scene management commands
            { "GET_SCENE_INFO", _ => SceneCommandHandler.GetSceneInfo() },
            { "OPEN_SCENE", parameters => SceneCommandHandler.OpenScene(parameters) },
            { "SAVE_SCENE", _ => SceneCommandHandler.SaveScene() },
            { "NEW_SCENE", parameters => SceneCommandHandler.NewScene(parameters) },
            { "CHANGE_SCENE", parameters => SceneCommandHandler.ChangeScene(parameters) },

            // Asset management commands
            { "IMPORT_ASSET", parameters => AssetCommandHandler.ImportAsset(parameters) },
            { "INSTANTIATE_PREFAB", parameters => AssetCommandHandler.InstantiatePrefab(parameters) },
            { "CREATE_PREFAB", parameters => AssetCommandHandler.CreatePrefab(parameters) },
            { "APPLY_PREFAB", parameters => AssetCommandHandler.ApplyPrefab(parameters) },
            { "GET_ASSET_LIST", parameters => AssetCommandHandler.GetAssetList(parameters) },
            
            // glb转预制件并自动AI补全比例
            { "GLB_BATCH_CONVERT", parameters => ModelParameterCommandHandler.GLBBatchConvert(parameters) },

            // Object management commands
            { "GET_OBJECT_PROPERTIES", parameters => ObjectCommandHandler.GetObjectProperties(parameters) },
            { "CREATE_OBJECT", parameters => ObjectCommandHandler.CreateObject(parameters) },
            { "GET_COMPONENT_PROPERTIES", parameters => ObjectCommandHandler.GetComponentProperties(parameters) },
            { "FIND_OBJECTS_BY_NAME", parameters => ObjectCommandHandler.FindObjectsByName(parameters) },
            { "FIND_OBJECTS_BY_TAG", parameters => ObjectCommandHandler.FindObjectsByTag(parameters) },
            { "GET_HIERARCHY", _ => ObjectCommandHandler.GetHierarchy() },
            { "SELECT_OBJECT", parameters => ObjectCommandHandler.SelectObject(parameters) },
            { "GET_SELECTED_OBJECT", _ => ObjectCommandHandler.GetSelectedObject() },
            { "MODIFY_OBJECT", parameters => ObjectCommandHandler.ModifyObject(parameters) },
            { "DELETE_OBJECT", parameters => ObjectCommandHandler.DeleteObject(parameters) },
            { "GET_OBJECT_INFO", parameters => ObjectCommandHandler.GetObjectInfo(parameters) },
            { "EXECUTE_CONTEXT_MENU_ITEM", parameters => ObjectCommandHandler.ExecuteContextMenuItem(parameters) },
            { "FIND_CAMERA_OBJECTS", _ => ObjectCommandHandler.FindCameraObjects() },
            { "FIND_OBJECTS_BY_NAME_PATTERN", parameters => ObjectCommandHandler.FindObjectsByNamePattern(parameters) },
            { "GET_ALL_SCENE_OBJECTS", _ => ObjectCommandHandler.GetAllSceneObjects() },
            { "GET_OBJECT_TRANSFORM_INFO", parameters => ObjectCommandHandler.GetObjectTransformInfo(parameters) },

            // Generate management commands
            { "GENERATE_MODEL", parameters => GenerateContentCommandHandler.GenerateModel(parameters) },
            
            // Animation commands
            { "CREATE_MOVEMENT_ANIMATION", parameters => CreateAnimationCommandHandler.CreateMovementAnimation(parameters) },
            { "GET_TIMELINE_ASSET_PATH", parameters => CreateAnimationCommandHandler.GetTimelineAssetPath(parameters) },
            { "VERIFY_TIMELINE_ASSET_EXISTS", parameters => CreateAnimationCommandHandler.VerifyTimelineAssetExists(parameters) },
            { "CREATE_SEPARATE_TIMELINES", parameters => CreateAnimationCommandHandler.CreateSeparateTimelines(parameters) },
            { "CREATE_COMBINED_TIMELINE", parameters => CreateAnimationCommandHandler.CreateCombinedTimeline(parameters) },

            // ScriptCommandHandler
            { "VIEW_SCRIPT", parameters => ScriptCommandHandler.ViewScript(parameters) },
            { "CREATE_SCRIPT", parameters => ScriptCommandHandler.CreateScript(parameters) },
            { "UPDATE_SCRIPT", parameters => ScriptCommandHandler.UpdateScript(parameters) },
            { "LIST_SCRIPTS", parameters => ScriptCommandHandler.ListScripts(parameters) },
            { "ATTACH_SCRIPT", parameters => ScriptCommandHandler.AttachScript(parameters) },
            


            // UI management commands
            //{ "CREATE_UI_BUTTON", UICommandHandler.CreateUIButton },
            //{ "CREATE_UI_TEXT", UICommandHandler.CreateUIText },

            // Editor control commands
            { "EDITOR_CONTROL", parameters => EditorControlHandler.HandleEditorControl(parameters) },

            // MaterialCommandHandler
            { "SET_MATERIAL", parameters => MaterialCommandHandler.SetMaterial(parameters) },
            
            // NodeGraphTool commands
            // CreateEmptyNodeGraph
            { "CREATE_EMPTY_NODEGRAPH",  parameters => NodeGraphCommandHandler.CreateEmptyNodeGraph(parameters) },
            // GetNodeGraphInfo
            { "GET_NODEGRAPH_INFO", parameters => NodeGraphCommandHandler.GetNodeGraphInfo(parameters) },
            // ImportExcelToNodeGraph
            { "IMPORT_EXCEL_TO_NODEGRAPH", parameters => NodeGraphCommandHandler.ImportExcelToNodeGraph(parameters) },
            // GetFlowEventNodes
            { "GET_FLOW_EVENT_NODES", parameters => NodeGraphCommandHandler.GetFlowEventNodes(parameters) },
            // GetFlowEventNodeNames
            { "GET_FLOW_EVENT_NODE_NAMES", parameters => NodeGraphCommandHandler.GetFlowEventNodeNames(parameters) },
            // GetFlowEventNodeByName
            { "GET_FLOW_EVENT_NODE_BY_NAME", parameters => NodeGraphCommandHandler.GetFlowEventNodeByName(parameters) },
            // UpdateFlowEventNodeTimelineAssets
            { "UPDATE_FLOW_EVENT_NODE_TIMELINE_ASSETS", parameters => NodeGraphCommandHandler.UpdateFlowEventNodeTimelineAssets(parameters) },
            // SaveNodeGraphChangesS
            { "SAVE_NODEGRAPH_CHANGES", parameters => NodeGraphCommandHandler.SaveNodeGraphChanges(parameters) },
                        
            
            // 周佳铭的
            //EventObject control commands
            { "FLOW_EVENT_FORTH",parameters => EventCommandHandler.FlowEventForth(parameters)},
            {"ADD_EVENT_OBJECT", parameters => EventCommandHandler.AddEventObject(parameters)},
            {"CREATE_BASE" , parameters => EventCommandHandler.CreateBase(parameters)},
            //Event control commands
            { "ADD_GRAPH_POOL", parameters => NodeGraphCommandHandler.AddGraphPool(parameters)},
            // { "ADD_EVENT", _ => EventCommandHandler.AddEvent()},
            { "SET_TRANSFORM_POSITION", parameters => ObjectCommandHandler.SetTransformPosition(parameters)},
            { "SET_TRANSFORM_ROTATION", parameters => ObjectCommandHandler.SetTransformRotation(parameters)},
            { "SET_TRANSFORM_SCALE", parametrs => ObjectCommandHandler.SetTransformScale(parametrs)},
            
        };

        /// <summary>
        /// Gets a command handler by name
        /// </summary>
        /// <param name="commandName">Name of the command to get</param>
        /// <returns>The command handler function if found, null otherwise</returns>
        public static Func<JObject, object> GetHandler(string commandName)
        {
            return _handlers.TryGetValue(commandName, out var handler) ? handler : null;
        }
    }
}
