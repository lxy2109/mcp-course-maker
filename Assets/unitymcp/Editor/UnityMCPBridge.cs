using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityMCP.Editor.Models;
using UnityMCP.Editor.Commands;

namespace UnityMCP.Editor
{
    [InitializeOnLoad]
    public static partial class UnityMCPBridge
    {
        private static TcpListener listener;
        private static bool isRunning = false;
        private static readonly object lockObj = new();
        private static Dictionary<string, (string commandJson, TaskCompletionSource<string> tcs)> commandQueue = new();
        private static readonly int unityPort = 6400;  // Hardcoded port

        public static bool IsRunning => isRunning;

        public static bool FolderExists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                return true;

            string fullPath = Path.Combine(Application.dataPath, path.StartsWith("Assets/") ? path.Substring(7) : path);
            return Directory.Exists(fullPath);
        }

        static UnityMCPBridge()
        {
            Start();
            EditorApplication.quitting += Stop;
        }

        public static void Start()
        {
            if (isRunning) return;
            isRunning = true;
            listener = new TcpListener(IPAddress.Loopback, unityPort);
            listener.Start();
            Debug.Log($"UnityMCPBridge started on port {unityPort}.");
            Task.Run(ListenerLoop);
            EditorApplication.update += ProcessCommands;
        }

        public static void Stop()
        {
            if (!isRunning) return;
            isRunning = false;
            listener.Stop();
            EditorApplication.update -= ProcessCommands;
            Debug.Log("UnityMCPBridge stopped.");
        }

        private static async Task ListenerLoop()
        {
            while (isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    // Enable basic socket keepalive
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    // Set longer receive timeout to prevent quick disconnections
                    client.ReceiveTimeout = 60000; // 60 seconds

                    // Fire and forget each client connection
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    if (isRunning) Debug.LogError($"Listener error: {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                while (isRunning)
                {
                    try
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // Client disconnected

                        string commandText = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string commandId = Guid.NewGuid().ToString();
                        var tcs = new TaskCompletionSource<string>();

                        // Special handling for ping command to avoid JSON parsing
                        if (commandText.Trim() == "ping")
                        {
                            // Direct response to ping without going through JSON parsing
                            byte[] pingResponseBytes = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"success\",\"result\":{\"message\":\"pong\"}}");
                            await stream.WriteAsync(pingResponseBytes, 0, pingResponseBytes.Length);
                            continue;
                        }

                        lock (lockObj)
                        {
                            commandQueue[commandId] = (commandText, tcs);
                        }

                        string response = await tcs.Task;
                        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Client handler error: {ex.Message}");
                        break;
                    }
                }
            }
        }

        private static void ProcessCommands()
        {
            List<string> processedIds = new();
            lock (lockObj)
            {
                foreach (var kvp in commandQueue.ToList())
                {
                    string id = kvp.Key;
                    string commandText = kvp.Value.commandJson;
                    var tcs = kvp.Value.tcs;

                    try
                    {
                        // Special case handling
                        if (string.IsNullOrEmpty(commandText))
                        {
                            var emptyResponse = new
                            {
                                status = "error",
                                error = "Empty command received"
                            };
                            tcs.SetResult(JsonConvert.SerializeObject(emptyResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Trim the command text to remove any whitespace
                        commandText = commandText.Trim();

                        // Non-JSON direct commands handling (like ping)
                        if (commandText == "ping")
                        {
                            var pingResponse = new
                            {
                                status = "success",
                                result = new { message = "pong" }
                            };
                            tcs.SetResult(JsonConvert.SerializeObject(pingResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Check if the command is valid JSON before attempting to deserialize
                        if (!IsValidJson(commandText))
                        {
                            var invalidJsonResponse = new
                            {
                                status = "error",
                                error = "Invalid JSON format",
                                receivedText = commandText.Length > 50 ? commandText.Substring(0, 50) + "..." : commandText
                            };
                            tcs.SetResult(JsonConvert.SerializeObject(invalidJsonResponse));
                            processedIds.Add(id);
                            continue;
                        }

                        // Normal JSON command processing
                        var command = JsonConvert.DeserializeObject<Command>(commandText);
                        if (command == null)
                        {
                            var nullCommandResponse = new
                            {
                                status = "error",
                                error = "Command deserialized to null",
                                details = "The command was valid JSON but could not be deserialized to a Command object"
                            };
                            tcs.SetResult(JsonConvert.SerializeObject(nullCommandResponse));
                        }
                        else
                        {
                            string responseJson = ExecuteCommand(command);
                            tcs.SetResult(responseJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing command: {ex.Message}\n{ex.StackTrace}");

                        var response = new
                        {
                            status = "error",
                            error = ex.Message,
                            commandType = "Unknown (error during processing)",
                            receivedText = commandText?.Length > 50 ? commandText.Substring(0, 50) + "..." : commandText
                        };
                        string responseJson = JsonConvert.SerializeObject(response);
                        tcs.SetResult(responseJson);
                    }

                    processedIds.Add(id);
                }

                foreach (var id in processedIds)
                {
                    commandQueue.Remove(id);
                }
            }
        }

        // Helper method to check if a string is valid JSON
        private static bool IsValidJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            if ((text.StartsWith("{") && text.EndsWith("}")) || // Object
                (text.StartsWith("[") && text.EndsWith("]")))   // Array
            {
                try
                {
                    JToken.Parse(text);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static string ExecuteCommand(Command command)
        {
            try
            {
                if (string.IsNullOrEmpty(command.type))
                {
                    var errorResponse = new
                    {
                        status = "error",
                        error = "Command type cannot be empty",
                        details = "A valid command type is required for processing"
                    };
                    return JsonConvert.SerializeObject(errorResponse);
                }

                // Handle ping command for connection verification
                if (command.type == "ping")
                {
                    var pingResponse = new { status = "success", result = new { message = "pong" } };
                    return JsonConvert.SerializeObject(pingResponse);
                }

                object result = command.type switch
                {
                    "GET_SCENE_INFO" => SceneCommandHandler.GetSceneInfo(),
                    "OPEN_SCENE" => SceneCommandHandler.OpenScene(command.@params),
                    "SAVE_SCENE" => SceneCommandHandler.SaveScene(),
                    "NEW_SCENE" => SceneCommandHandler.NewScene(command.@params),
                    "CHANGE_SCENE" => SceneCommandHandler.ChangeScene(command.@params),
                    "GET_OBJECT_INFO" => ObjectCommandHandler.GetObjectInfo(command.@params),
                    "CREATE_OBJECT" => ObjectCommandHandler.CreateObject(command.@params),
                    "MODIFY_OBJECT" => ObjectCommandHandler.ModifyObject(command.@params),
                    "DELETE_OBJECT" => ObjectCommandHandler.DeleteObject(command.@params),
                    "EXECUTE_CONTEXT_MENU_ITEM" => ObjectCommandHandler.ExecuteContextMenuItem(command.@params),
                    "GET_OBJECT_PROPERTIES" => ObjectCommandHandler.GetObjectProperties(command.@params),
                    "GET_COMPONENT_PROPERTIES" => ObjectCommandHandler.GetComponentProperties(command.@params),
                    "FIND_OBJECTS_BY_NAME" => ObjectCommandHandler.FindObjectsByName(command.@params),
                    "FIND_OBJECTS_BY_TAG" => ObjectCommandHandler.FindObjectsByTag(command.@params),
                    "GET_ALL_SCENE_OBJECTS" => ObjectCommandHandler.GetAllSceneObjects(),
                    "GET_OBJECT_TRANSFORM_INFO" => ObjectCommandHandler.GetObjectTransformInfo(command.@params),
                    "FIND_CAMERA_OBJECTS" => ObjectCommandHandler.FindCameraObjects(),
                    "FIND_OBJECTS_BY_NAME_PATTERN" => ObjectCommandHandler.FindObjectsByNamePattern(command.@params),
                    "GET_HIERARCHY" => ObjectCommandHandler.GetHierarchy(),
                    "SELECT_OBJECT" => ObjectCommandHandler.SelectObject(command.@params),
                    "GET_SELECTED_OBJECT" => ObjectCommandHandler.GetSelectedObject(),
                    "SET_MATERIAL" => MaterialCommandHandler.SetMaterial(command.@params),
                    "VIEW_SCRIPT" => ScriptCommandHandler.ViewScript(command.@params),
                    "CREATE_SCRIPT" => ScriptCommandHandler.CreateScript(command.@params),
                    "UPDATE_SCRIPT" => ScriptCommandHandler.UpdateScript(command.@params),
                    "LIST_SCRIPTS" => ScriptCommandHandler.ListScripts(command.@params),
                    "ATTACH_SCRIPT" => ScriptCommandHandler.AttachScript(command.@params),
                    "IMPORT_ASSET" => AssetCommandHandler.ImportAsset(command.@params),
                    "INSTANTIATE_PREFAB" => AssetCommandHandler.InstantiatePrefab(command.@params),
                    "CREATE_PREFAB" => AssetCommandHandler.CreatePrefab(command.@params),
                    "APPLY_PREFAB" => AssetCommandHandler.ApplyPrefab(command.@params),
                    "GET_ASSET_LIST" => AssetCommandHandler.GetAssetList(command.@params),
                   
                    // glb转预制件并自动AI补全比例
                    "GLB_BATCH_CONVERT" => ModelParameterCommandHandler.GLBBatchConvert(command.@params),
                    
                    "GENERATE_MODEL"=>GenerateContentCommandHandler.GenerateModel(command.@params),
                    "CREATE_MOVEMENT_ANIMATION"=>CreateAnimationCommandHandler.CreateMovementAnimation(command.@params),
                    "GET_TIMELINE_ASSET_PATH"=>CreateAnimationCommandHandler.GetTimelineAssetPath(command.@params),
                    "VERIFY_TIMELINE_ASSET_EXISTS"=>CreateAnimationCommandHandler.VerifyTimelineAssetExists(command.@params),
                    "CREATE_SEPARATE_TIMELINES"=>CreateAnimationCommandHandler.CreateSeparateTimelines(command.@params),
                    "CREATE_COMBINED_TIMELINE"=>CreateAnimationCommandHandler.CreateCombinedTimeline(command.@params),

                    //2025.3.24 add ui
                    //"CREATE_UI_BUTTON" => UICommandHandler.CreateUIButton(command.@params),
                    //"CREATE_UI_TEXT" => UICommandHandler.CreateUIText(command.@params),

                    "EDITOR_CONTROL" => EditorControlHandler.HandleEditorControl(command.@params),
                    
                    // NodeGraphTool commands
                    // CreateEmptyNodeGraph
                    "CREATE_EMPTY_NODEGRAPH" => NodeGraphCommandHandler.CreateEmptyNodeGraph(command.@params),
                    // GetNodeGraphInfo
                    "GET_NODEGRAPH_INFO" => NodeGraphCommandHandler.GetNodeGraphInfo(command.@params),
                    // ImportExcelToNodeGraph
                    "IMPORT_EXCEL_TO_NODEGRAPH" => NodeGraphCommandHandler.ImportExcelToNodeGraph(command.@params),
                    //  GetFlowEventNodes
                    "GET_FLOW_EVENT_NODES" => NodeGraphCommandHandler.GetFlowEventNodes(command.@params),
                    // GetFlowEventNodeNames
                    "GET_FLOW_EVENT_NODE_NAMES" => NodeGraphCommandHandler.GetFlowEventNodeNames(command.@params),
                    // GetFlowEventNodeByName
                    "GET_FLOW_EVENT_NODE_BY_NAME" => NodeGraphCommandHandler.GetFlowEventNodeByName(command.@params),
                    // UpdateFlowEventNodeTimelineAssets
                    "UPDATE_FLOW_EVENT_NODE_TIMELINE_ASSETS" => NodeGraphCommandHandler.UpdateFlowEventNodeTimelineAssets(command.@params),
                    // SaveNodeGraphChanges
                    "SAVE_NODEGRAPH_CHANGES" => NodeGraphCommandHandler.SaveNodeGraphChanges(command.@params),
                    
                    
                    
                    // 周佳铭的
                    "FLOW_EVENT_FORTH" => EventCommandHandler.FlowEventForth(command.@params),
                    "ADD_GRAPH_POOL" => NodeGraphCommandHandler.AddGraphPool(command.@params),
                    "CREATE_BASE" => EventCommandHandler.CreateBase(command.@params),
                    // "ADD_EVENT" => EventCommandHandler.AddEvent(),
                    "ADD_EVENT_OBJECT" => command.@params == null ? EventCommandHandler.AddEventObject(new JObject()) : EventCommandHandler.AddEventObject(command.@params),
                    "ADD_EVENT_OBJECT_NO_PARAM" => EventCommandHandler.AddEventObject(new JObject()),
                    "SET_TRANSFORM_POSITION" => ObjectCommandHandler.SetTransformPosition(command.@params),
                    "SET_TRANSFORM_ROTATION" => ObjectCommandHandler.SetTransformRotation(command.@params),
                    "SET_TRANSFORM_SCALE" => ObjectCommandHandler.SetTransformScale(command.@params),
                    
                    "SET_SKYBOX" => SkyboxCommandHandler.SetSkybox(command.@params),
                    "CLEAR_SKYBOX" => SkyboxCommandHandler.ClearSkybox(command.@params),
                    "CREATE_SKYBOX_MATERIAL" => SkyboxCommandHandler.CreateSkyboxMaterial(command.@params),
                    
                    "GET_OBJECT_BOUNDS" => ObjectCommandHandler.GetObjectBounds(command.@params),
                    "GET_COMBINED_BOUNDS" => ObjectCommandHandler.GetCombinedBounds(command.@params),
                    "POSITION_CAMERA_TO_FRAME_OBJECTS" => ObjectCommandHandler.PositionCameraToFrameObjects(command.@params),
                    "AUTO_POSITION_CAMERA_TO_OBJECTS" => ObjectCommandHandler.AutoPositionCameraToObjects(command.@params),

                    // 兼容无参数调用
                    "CREATE_UNITY_EVENT" => command.@params == null ? EventCommandHandler.CreateUnityEvent(new JObject()) : EventCommandHandler.CreateUnityEvent(command.@params),

                    _ => throw new Exception($"Unknown command type: {command.type}")
                };

                var response = new { status = "success", result };
                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing command {command.type}: {ex.Message}\n{ex.StackTrace}");
                var response = new
                {
                    status = "error",
                    error = ex.Message,
                    command = command.type,
                    stackTrace = ex.StackTrace,
                    paramsSummary = command.@params != null ? GetParamsSummary(command.@params) : "No parameters"
                };
                return JsonConvert.SerializeObject(response);
            }
        }

        // Helper method to get a summary of parameters for error reporting
        private static string GetParamsSummary(JObject @params)
        {
            try
            {
                if (@params == null || !@params.HasValues)
                    return "No parameters";

                return string.Join(", ", @params.Properties().Select(p => $"{p.Name}: {p.Value?.ToString()?.Substring(0, Math.Min(20, p.Value?.ToString()?.Length ?? 0))}"));
            }
            catch
            {
                return "Could not summarize parameters";
            }
        }
    }
}
