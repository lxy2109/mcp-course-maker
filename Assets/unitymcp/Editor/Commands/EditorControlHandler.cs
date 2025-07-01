using UnityEngine;                        // 导入Unity核心功能命名空间
using UnityEditor;                        // 导入Unity编辑器功能命名空间
using UnityEditor.Build.Reporting;        // 导入编辑器构建报告功能命名空间
using Newtonsoft.Json.Linq;               // 导入JSON处理库命名空间
using System;                             // 导入基础系统功能命名空间
using System.Reflection;                  // 导入反射功能命名空间，用于动态调用方法和访问属性
using System.Collections.Generic;         // 导入集合类型命名空间
using System.Linq;                        // 导入LINQ查询功能命名空间

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理编辑器控制命令，如撤销、重做、播放、暂停、停止和构建操作。
    /// </summary>
    public static class EditorControlHandler
    {
        /// <summary>
        /// 处理编辑器控制命令
        /// </summary>
        /// <param name="params">包含命令及其参数的JSON对象</param>
        /// <returns>命令执行结果</returns>
        public static object HandleEditorControl(JObject @params)
        {
            string command = (string)@params["command"];            // 从参数中获取命令名称
            JObject commandParams = (JObject)@params["params"];     // 从参数中获取命令参数对象

            // 使用命令名称（转为大写以忽略大小写）来决定执行哪个处理方法
            return command.ToUpper() switch
            {
                "UNDO" => HandleUndo(),                              // 处理撤销命令
                "REDO" => HandleRedo(),                              // 处理重做命令
                "PLAY" => HandlePlay(),                              // 处理播放命令
                "PAUSE" => HandlePause(),                            // 处理暂停命令
                "STOP" => HandleStop(),                              // 处理停止命令
                "BUILD" => HandleBuild(commandParams),               // 处理构建命令，传入具体参数
                "EXECUTE_COMMAND" => HandleExecuteCommand(commandParams), // 处理执行特定命令
                "READ_CONSOLE" => ReadConsole(commandParams),        // 处理读取控制台日志命令
                "GET_AVAILABLE_COMMANDS" => GetAvailableCommands(),  // 获取可用命令列表
                _ => new { error = $"Unknown editor control command: {command}" }, // 处理未知命令，返回错误
            };
        }

        /// <summary>
        /// 处理撤销操作
        /// </summary>
        /// <returns>操作结果</returns>
        private static object HandleUndo()
        {
            Undo.PerformUndo();                                     // 执行Unity编辑器的撤销操作
            return new { message = "Undo performed successfully" };  // 返回成功消息
        }

        /// <summary>
        /// 处理重做操作
        /// </summary>
        /// <returns>操作结果</returns>
        private static object HandleRedo()
        {
            Undo.PerformRedo();                                     // 执行Unity编辑器的重做操作
            return new { message = "Redo performed successfully" };  // 返回成功消息
        }

        /// <summary>
        /// 处理播放模式切换
        /// </summary>
        /// <returns>操作结果</returns>
        private static object HandlePlay()
        {
            if (!EditorApplication.isPlaying)                        // 检查当前是否已在播放模式
            {
                EditorApplication.isPlaying = true;                  // 设置编辑器进入播放模式
                return new { message = "Entered play mode" };        // 返回进入播放模式的消息
            }
            return new { message = "Already in play mode" };         // 返回已经在播放模式的消息
        }

        /// <summary>
        /// 处理暂停/恢复操作
        /// </summary>
        /// <returns>操作结果</returns>
        private static object HandlePause()
        {
            if (EditorApplication.isPlaying)                        // 检查当前是否在播放模式
            {
                EditorApplication.isPaused = !EditorApplication.isPaused;  // 切换暂停状态
                return new { message = EditorApplication.isPaused ? "Game paused" : "Game resumed" }; // 返回当前状态消息
            }
            return new { message = "Not in play mode" };             // 返回当前不在播放模式的消息
        }

        /// <summary>
        /// 处理停止播放模式
        /// </summary>
        /// <returns>操作结果</returns>
        private static object HandleStop()
        {
            if (EditorApplication.isPlaying)                        // 检查当前是否在播放模式
            {
                EditorApplication.isPlaying = false;                 // 设置编辑器退出播放模式
                return new { message = "Exited play mode" };         // 返回退出播放模式的消息
            }
            return new { message = "Not in play mode" };             // 返回当前不在播放模式的消息
        }

        /// <summary>
        /// 处理构建操作
        /// </summary>
        /// <param name="params">包含构建参数的JSON对象</param>
        /// <returns>构建结果</returns>
        private static object HandleBuild(JObject @params)
        {
            string platform = (string)@params["platform"];          // 从参数中获取目标平台
            string buildPath = (string)@params["buildPath"];        // 从参数中获取构建输出路径

            try
            {
                BuildTarget target = GetBuildTarget(platform);         // 获取对应平台的BuildTarget枚举值
                if ((int)target == -1)
                {
                    return new { error = $"Unsupported platform: {platform}" }; // 返回不支持的平台错误
                }

                // 设置构建选项
                BuildPlayerOptions buildPlayerOptions = new()
                {
                    scenes = GetEnabledScenes(),                        // 获取所有启用的场景
                    target = target,                                    // 设置目标平台
                    locationPathName = buildPath                        // 设置构建输出路径
                };

                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions); // 执行构建操作
                return new
                {
                    message = "Build completed successfully",            // 返回构建成功消息
                    report.summary                                      // 返回构建报告摘要
                };
            }
            catch (Exception e)
            {
                return new { error = $"Build failed: {e.Message}" };  // 返回构建失败错误
            }
        }

        /// <summary>
        /// 处理执行特定命令操作
        /// </summary>
        /// <param name="params">包含命令名称的JSON对象</param>
        /// <returns>执行结果</returns>
        private static object HandleExecuteCommand(JObject @params)
        {
            string commandName = (string)@params["commandName"];    // 从参数中获取要执行的命令名称
            try
            {
                EditorApplication.ExecuteMenuItem(commandName);        // 执行Unity编辑器菜单命令
                return new { message = $"Executed command: {commandName}" }; // 返回执行成功消息
            }
            catch (Exception e)
            {
                return new { error = $"Failed to execute command: {e.Message}" }; // 返回执行失败错误
            }
        }

        /// <summary>
        /// 读取控制台日志
        /// </summary>
        /// <param name="params">包含过滤选项的参数</param>
        /// <returns>按类型过滤的控制台消息对象</returns>
        public static object ReadConsole(JObject @params)
        {
            // 默认的日志显示标志
            bool showLogs = true;
            bool showWarnings = true;
            bool showErrors = true;
            string searchTerm = string.Empty;                        // 默认空的搜索词

            // 获取过滤参数（如果提供了的话）
            if (@params != null)
            {
                if (@params["show_logs"] != null) showLogs = (bool)@params["show_logs"];                // 是否显示日志
                if (@params["show_warnings"] != null) showWarnings = (bool)@params["show_warnings"];    // 是否显示警告
                if (@params["show_errors"] != null) showErrors = (bool)@params["show_errors"];            // 是否显示错误
                if (@params["search_term"] != null) searchTerm = (string)@params["search_term"];            // 搜索词
            }

            try
            {
                // 通过反射获取所需的类型和方法
                Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                Type logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor");

                if (logEntriesType == null || logEntryType == null)
                    return new { error = "Could not find required Unity logging types", entries = new List<object>() };

                // 获取必要的方法
                MethodInfo getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo getEntryMethod = logEntriesType.GetMethod("GetEntryAt", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                                            logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (getCountMethod == null || getEntryMethod == null)
                    return new { error = "Could not find required Unity logging methods", entries = new List<object>() };

                // 获取堆栈跟踪方法（如果可用）
                MethodInfo getStackTraceMethod = logEntriesType.GetMethod("GetEntryStackTrace", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null) ?? logEntriesType.GetMethod("GetStackTrace", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(int) }, null);

                // 获取日志条目数并准备结果列表
                int count = (int)getCountMethod.Invoke(null, null);
                var entries = new List<object>();

                // 创建LogEntry实例以填充
                object logEntryInstance = Activator.CreateInstance(logEntryType);

                // 在LogEntry类型上查找属性
                PropertyInfo modeProperty = logEntryType.GetProperty("mode") ?? logEntryType.GetProperty("Mode");
                PropertyInfo messageProperty = logEntryType.GetProperty("message") ?? logEntryType.GetProperty("Message");

                // 解析搜索词（如果提供了的话）
                string[] searchWords = !string.IsNullOrWhiteSpace(searchTerm) ?
                    searchTerm.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : null;

                // 处理每个日志条目
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        // 获取索引为i的日志条目
                        var methodParams = getEntryMethod.GetParameters();
                        if (methodParams.Length == 2 && methodParams[1].ParameterType == logEntryType)
                        {
                            getEntryMethod.Invoke(null, new object[] { i, logEntryInstance });
                        }
                        else if (methodParams.Length >= 1 && methodParams[0].ParameterType == typeof(int))
                        {
                            var parameters = new object[methodParams.Length];
                            parameters[0] = i;
                            for (int p = 1; p < parameters.Length; p++)
                            {
                                parameters[p] = methodParams[p].ParameterType.IsValueType ?
                                    Activator.CreateInstance(methodParams[p].ParameterType) : null;
                            }
                            getEntryMethod.Invoke(null, parameters);
                        }
                        else continue;

                        // 提取日志数据
                        int logType = modeProperty != null ?
                            Convert.ToInt32(modeProperty.GetValue(logEntryInstance) ?? 0) : 0;

                        string message = messageProperty != null ?
                            (messageProperty.GetValue(logEntryInstance)?.ToString() ?? "") : "";

                        // 如果消息为空，尝试通过字段获取
                        if (string.IsNullOrEmpty(message))
                        {
                            var msgField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (msgField != null)
                            {
                                object msgValue = msgField.GetValue(logEntryInstance);
                                message = msgValue != null ? msgValue.ToString() : "";
                            }

                            // 如果仍然为空，尝试使用控制台窗口的替代方法
                            if (string.IsNullOrEmpty(message))
                            {
                                // 访问ConsoleWindow及其数据
                                Type consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                                if (consoleWindowType != null)
                                {
                                    try
                                    {
                                        // 获取Console窗口实例
                                        var getWindowMethod = consoleWindowType.GetMethod("GetWindow",
                                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                            null, new[] { typeof(bool) }, null) ??
                                            consoleWindowType.GetMethod("GetConsoleWindow",
                                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                                        if (getWindowMethod != null)
                                        {
                                            object consoleWindow = getWindowMethod.Invoke(null,
                                                getWindowMethod.GetParameters().Length > 0 ? new object[] { false } : null);

                                            if (consoleWindow != null)
                                            {
                                                // 尝试查找日志条目集合
                                                foreach (var prop in consoleWindowType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                                {
                                                    if (prop.PropertyType.IsArray ||
                                                       (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                                                    {
                                                        try
                                                        {
                                                            var logItems = prop.GetValue(consoleWindow);
                                                            if (logItems != null)
                                                            {
                                                                if (logItems.GetType().IsArray && i < ((Array)logItems).Length)
                                                                {
                                                                    var entry = ((Array)logItems).GetValue(i);
                                                                    if (entry != null)
                                                                    {
                                                                        var entryType = entry.GetType();
                                                                        var entryMessageProp = entryType.GetProperty("message") ??
                                                                                             entryType.GetProperty("Message");
                                                                        if (entryMessageProp != null)
                                                                        {
                                                                            object value = entryMessageProp.GetValue(entry);
                                                                            if (value != null)
                                                                            {
                                                                                message = value.ToString();
                                                                                break;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch
                                                        {
                                                            // 忽略此备用方法中的错误
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // 忽略此备用方法中的错误
                                    }
                                }
                            }

                            // 如果仍然为空，尝试最后一种方法获取日志文件
                            if (string.IsNullOrEmpty(message))
                            {
                                // 这是我们的最后手段 - 尝试从最近的Unity日志文件中获取日志消息
                                try
                                {
                                    string logPath = string.Empty;

                                    // 根据平台确定日志文件路径
                                    if (Application.platform == RuntimePlatform.WindowsEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                            "Unity", "Editor", "Editor.log");
                                    }
                                    else if (Application.platform == RuntimePlatform.OSXEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                            "Library", "Logs", "Unity", "Editor.log");
                                    }
                                    else if (Application.platform == RuntimePlatform.LinuxEditor)
                                    {
                                        logPath = System.IO.Path.Combine(
                                            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                            ".config", "unity3d", "logs", "Editor.log");
                                    }

                                    if (!string.IsNullOrEmpty(logPath) && System.IO.File.Exists(logPath))
                                    {
                                        // 从日志文件中读取最后几行
                                        var logLines = ReadLastLines(logPath, 100);
                                        if (logLines.Count > i)
                                        {
                                            message = logLines[logLines.Count - 1 - i];
                                        }
                                    }
                                }
                                catch
                                {
                                    // 忽略此备用方法中的错误
                                }
                            }
                        }

                        // 获取堆栈跟踪（如果方法可用）
                        string stackTrace = "";
                        if (getStackTraceMethod != null)
                        {
                            stackTrace = getStackTraceMethod.Invoke(null, new object[] { i })?.ToString() ?? "";
                        }

                        // 按类型过滤
                        bool typeMatch = (logType == 0 && showLogs) ||
                                        (logType == 1 && showWarnings) ||
                                        (logType == 2 && showErrors);
                        if (!typeMatch) continue;

                        // 按搜索词过滤
                        bool searchMatch = true;
                        if (searchWords != null && searchWords.Length > 0)
                        {
                            string lowerMessage = message.ToLower();
                            string lowerStackTrace = stackTrace.ToLower();

                            foreach (string word in searchWords)
                            {
                                if (!lowerMessage.Contains(word) && !lowerStackTrace.Contains(word))
                                {
                                    searchMatch = false;
                                    break;
                                }
                            }
                        }
                        if (!searchMatch) continue;

                        // 将匹配的条目添加到结果中
                        string typeStr = logType == 0 ? "Log" : logType == 1 ? "Warning" : "Error";
                        entries.Add(new
                        {
                            type = typeStr,
                            message,
                            stackTrace
                        });
                    }
                    catch (Exception)
                    {
                        // 跳过导致错误的条目
                        continue;
                    }
                }

                // 返回过滤后的结果
                return new
                {
                    message = "Console logs retrieved successfully",
                    entries,
                    total_entries = count,
                    filtered_count = entries.Count,
                    show_logs = showLogs,
                    show_warnings = showWarnings,
                    show_errors = showErrors
                };
            }
            catch (Exception e)
            {
                return new
                {
                    error = $"Failed to read console logs: {e.Message}",
                    entries = new List<object>()
                };
            }
        }

        /// <summary>
        /// 查找方法的辅助函数
        /// </summary>
        /// <param name="type">要查找方法的类型</param>
        /// <param name="methodNames">可能的方法名称列表</param>
        /// <returns>找到的方法信息</returns>
        private static MethodInfo FindMethod(Type type, string[] methodNames)
        {
            foreach (var methodName in methodNames)
            {
                var method = type.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method;
            }
            return null;
        }

        /// <summary>
        /// 获取目标平台对应的BuildTarget枚举值
        /// </summary>
        /// <param name="platform">平台名称</param>
        /// <returns>对应的BuildTarget值</returns>
        private static BuildTarget GetBuildTarget(string platform)
        {
            BuildTarget target;
            switch (platform.ToLower())
            {
                case "windows": target = BuildTarget.StandaloneWindows64; break;
                case "mac": target = BuildTarget.StandaloneOSX; break;
                case "linux": target = BuildTarget.StandaloneLinux64; break;
                case "android": target = BuildTarget.Android; break;
                case "ios": target = BuildTarget.iOS; break;
                case "webgl": target = BuildTarget.WebGL; break;
                default: target = (BuildTarget)(-1); break; // 无效的目标
            }
            return target;
        }

        /// <summary>
        /// 获取所有启用的场景
        /// </summary>
        /// <returns>启用场景的路径数组</returns>
        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].enabled)
                {
                    scenes.Add(EditorBuildSettings.scenes[i].path);
                }
            }
            return scenes.ToArray();
        }

        /// <summary>
        /// Helper method to get information about available properties and fields in a type
        /// </summary>
        private static Dictionary<string, object> GetTypeInfo(Type type)
        {
            var result = new Dictionary<string, object>();

            // 获取所有公共和非公共属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Static | BindingFlags.Instance);
            var propList = new List<string>();
            foreach (var prop in properties)
            {
                propList.Add($"{prop.PropertyType.Name} {prop.Name}");
            }
            result["Properties"] = propList;

            // 获取所有公共和非公共字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Static | BindingFlags.Instance);
            var fieldList = new List<string>();
            foreach (var field in fields)
            {
                fieldList.Add($"{field.FieldType.Name} {field.Name}");
            }
            result["Fields"] = fieldList;

            // 获取所有公共和非公共方法
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Static | BindingFlags.Instance);
            var methodList = new List<string>();
            foreach (var method in methods)
            {
                if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                {
                    var parameters = string.Join(", ", method.GetParameters()
                        .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    methodList.Add($"{method.ReturnType.Name} {method.Name}({parameters})");
                }
            }
            result["Methods"] = methodList;

            return result;
        }

        /// <summary>
        /// Helper method to get all property and field values from an object
        /// </summary>
        private static Dictionary<string, string> GetObjectValues(object obj)
        {
            if (obj == null) return new Dictionary<string, string>();

            var result = new Dictionary<string, string>();
            var type = obj.GetType();

            // 获取所有属性值
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    result[$"Property:{prop.Name}"] = value?.ToString() ?? "null";
                }
                catch (Exception)
                {
                    result[$"Property:{prop.Name}"] = "ERROR";
                }
            }

            // 获取所有字段值
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    result[$"Field:{field.Name}"] = value?.ToString() ?? "null";
                }
                catch (Exception)
                {
                    result[$"Field:{field.Name}"] = "ERROR";
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the last N lines from a file
        /// </summary>
        private static List<string> ReadLastLines(string filePath, int lineCount)
        {
            var result = new List<string>();

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            using (var reader = new System.IO.StreamReader(stream))
            {
                string line;
                var circularBuffer = new List<string>(lineCount);
                int currentIndex = 0;

                // Read all lines keeping only the last N in a circular buffer
                while ((line = reader.ReadLine()) != null)
                {
                    if (circularBuffer.Count < lineCount)
                    {
                        circularBuffer.Add(line);
                    }
                    else
                    {
                        circularBuffer[currentIndex] = line;
                        currentIndex = (currentIndex + 1) % lineCount;
                    }
                }

                // Reorder the circular buffer so that lines are returned in order
                if (circularBuffer.Count == lineCount)
                {
                    for (int i = 0; i < lineCount; i++)
                    {
                        result.Add(circularBuffer[(currentIndex + i) % lineCount]);
                    }
                }
                else
                {
                    result.AddRange(circularBuffer);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a comprehensive list of available Unity commands, including editor menu items,
        /// internal commands, utility methods, and other actionable operations that can be executed.
        /// </summary>
        /// <returns>Object containing categorized lists of available command paths</returns>
        private static object GetAvailableCommands()
        {
            var menuCommands = new HashSet<string>();
            var utilityCommands = new HashSet<string>();
            var assetCommands = new HashSet<string>();
            var sceneCommands = new HashSet<string>();
            var gameObjectCommands = new HashSet<string>();
            var prefabCommands = new HashSet<string>();
            var shortcutCommands = new HashSet<string>();
            var otherCommands = new HashSet<string>();

            // 添加一个我们知道会工作的简单命令以供测试
            menuCommands.Add("Window/Unity MCP");

            Debug.Log("Starting command collection...");

            try
            {
                // 添加所有EditorApplication静态方法 - 这些是保证可用的
                Debug.Log("Adding EditorApplication methods...");
                foreach (MethodInfo method in typeof(EditorApplication).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"EditorApplication.{method.Name}");
                }
                Debug.Log($"Added {utilityCommands.Count} EditorApplication methods");

                // 直接添加内置菜单命令 - 这些是应该始终可用的常用命令
                Debug.Log("Adding built-in menu commands...");
                string[] builtInMenus = new[] {
                    "File/New Scene",
                    "File/Open Scene",
                    "File/Save",
                    "File/Save As...",
                    "Edit/Undo",
                    "Edit/Redo",
                    "Edit/Cut",
                    "Edit/Copy",
                    "Edit/Paste",
                    "Edit/Duplicate",
                    "Edit/Delete",
                    "GameObject/Create Empty",
                    "GameObject/3D Object/Cube",
                    "GameObject/3D Object/Sphere",
                    "GameObject/3D Object/Capsule",
                    "GameObject/3D Object/Cylinder",
                    "GameObject/3D Object/Plane",
                    "GameObject/Light/Directional Light",
                    "GameObject/Light/Point Light",
                    "GameObject/Light/Spotlight",
                    "GameObject/Light/Area Light",
                    "Component/Mesh/Mesh Filter",
                    "Component/Mesh/Mesh Renderer",
                    "Component/Physics/Rigidbody",
                    "Component/Physics/Box Collider",
                    "Component/Physics/Sphere Collider",
                    "Component/Physics/Capsule Collider",
                    "Component/Audio/Audio Source",
                    "Component/Audio/Audio Listener",
                    "Window/General/Scene",
                    "Window/General/Game",
                    "Window/General/Inspector",
                    "Window/General/Hierarchy",
                    "Window/General/Project",
                    "Window/General/Console",
                    "Window/Analysis/Profiler",
                    "Window/Package Manager",
                    "Assets/Create/Material",
                    "Assets/Create/C# Script",
                    "Assets/Create/Prefab",
                    "Assets/Create/Scene",
                    "Assets/Create/Folder",
                };

                foreach (string menuItem in builtInMenus)
                {
                    menuCommands.Add(menuItem);
                }
                Debug.Log($"Added {builtInMenus.Length} built-in menu commands");

                // 从MenuItem属性获取菜单命令 - 包装在单独的try块中
                Debug.Log("Searching for MenuItem attributes...");
                try
                {
                    int itemCount = 0;
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.IsDynamic) continue;

                        try
                        {
                            foreach (Type type in assembly.GetExportedTypes())
                            {
                                try
                                {
                                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                    {
                                        try
                                        {
                                            object[] attributes = method.GetCustomAttributes(typeof(UnityEditor.MenuItem), false);
                                            if (attributes != null && attributes.Length > 0)
                                            {
                                                foreach (var attr in attributes)
                                                {
                                                    var menuItem = attr as UnityEditor.MenuItem;
                                                    if (menuItem != null && !string.IsNullOrEmpty(menuItem.menuItem))
                                                    {
                                                        menuCommands.Add(menuItem.menuItem);
                                                        itemCount++;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception methodEx)
                                        {
                                            Debug.LogWarning($"Error getting menu items for method {method.Name}: {methodEx.Message}");
                                            continue;
                                        }
                                    }
                                }
                                catch (Exception typeEx)
                                {
                                    Debug.LogWarning($"Error processing type: {typeEx.Message}");
                                    continue;
                                }
                            }
                        }
                        catch (Exception assemblyEx)
                        {
                            Debug.LogWarning($"Error examining assembly {assembly.GetName().Name}: {assemblyEx.Message}");
                            continue;
                        }
                    }
                    Debug.Log($"Found {itemCount} menu items from attributes");
                }
                catch (Exception menuItemEx)
                {
                    Debug.LogError($"Failed to get menu items: {menuItemEx.Message}");
                }

                // 添加EditorUtility方法作为命令
                Debug.Log("Adding EditorUtility methods...");
                foreach (MethodInfo method in typeof(EditorUtility).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"EditorUtility.{method.Name}");
                }
                Debug.Log($"Added {typeof(EditorUtility).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} EditorUtility methods");

                // 添加AssetDatabase方法作为命令
                Debug.Log("Adding AssetDatabase methods...");
                foreach (MethodInfo method in typeof(AssetDatabase).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    assetCommands.Add($"AssetDatabase.{method.Name}");
                }
                Debug.Log($"Added {typeof(AssetDatabase).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} AssetDatabase methods");

                // 添加EditorSceneManager方法作为命令
                Debug.Log("Adding EditorSceneManager methods...");
                Type sceneManagerType = typeof(UnityEditor.SceneManagement.EditorSceneManager);
                if (sceneManagerType != null)
                {
                    foreach (MethodInfo method in sceneManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        sceneCommands.Add($"EditorSceneManager.{method.Name}");
                    }
                    Debug.Log($"Added {sceneManagerType.GetMethods(BindingFlags.Public | BindingFlags.Static).Length} EditorSceneManager methods");
                }

                // 添加GameObject操作命令
                Debug.Log("Adding GameObject methods...");
                foreach (MethodInfo method in typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    gameObjectCommands.Add($"GameObject.{method.Name}");
                }
                Debug.Log($"Added {typeof(GameObject).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} GameObject methods");

                // 添加与Selection相关的命令
                Debug.Log("Adding Selection methods...");
                foreach (MethodInfo method in typeof(Selection).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    gameObjectCommands.Add($"Selection.{method.Name}");
                }
                Debug.Log($"Added {typeof(Selection).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} Selection methods");

                // 添加PrefabUtility方法作为命令
                Debug.Log("Adding PrefabUtility methods...");
                Type prefabUtilityType = typeof(UnityEditor.PrefabUtility);
                if (prefabUtilityType != null)
                {
                    foreach (MethodInfo method in prefabUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        prefabCommands.Add($"PrefabUtility.{method.Name}");
                    }
                    Debug.Log($"Added {prefabUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static).Length} PrefabUtility methods");
                }

                // 添加与Undo相关的方法
                Debug.Log("Adding Undo methods...");
                foreach (MethodInfo method in typeof(Undo).GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    utilityCommands.Add($"Undo.{method.Name}");
                }
                Debug.Log($"Added {typeof(Undo).GetMethods(BindingFlags.Public | BindingFlags.Static).Length} Undo methods");

                // 其余的命令收集可以尝试但可能不是关键
                try
                {
                    // 从Unity的内部命令系统获取命令
                    Debug.Log("Trying to get internal CommandService commands...");
                    Type commandServiceType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.CommandService");
                    if (commandServiceType != null)
                    {
                        Debug.Log("Found CommandService type");
                        PropertyInfo instanceProperty = commandServiceType.GetProperty("Instance",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                        if (instanceProperty != null)
                        {
                            Debug.Log("Found Instance property");
                            object commandService = instanceProperty.GetValue(null);
                            if (commandService != null)
                            {
                                Debug.Log("Got CommandService instance");
                                MethodInfo findAllCommandsMethod = commandServiceType.GetMethod("FindAllCommands",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                if (findAllCommandsMethod != null)
                                {
                                    Debug.Log("Found FindAllCommands method");
                                    var commandsResult = findAllCommandsMethod.Invoke(commandService, null);
                                    if (commandsResult != null)
                                    {
                                        Debug.Log("Got commands result");
                                        var commandsList = commandsResult as System.Collections.IEnumerable;
                                        if (commandsList != null)
                                        {
                                            int commandCount = 0;
                                            foreach (var cmd in commandsList)
                                            {
                                                try
                                                {
                                                    PropertyInfo nameProperty = cmd.GetType().GetProperty("name") ??
                                                                             cmd.GetType().GetProperty("path") ??
                                                                             cmd.GetType().GetProperty("commandName");
                                                    if (nameProperty != null)
                                                    {
                                                        string commandName = nameProperty.GetValue(cmd)?.ToString();
                                                        if (!string.IsNullOrEmpty(commandName))
                                                        {
                                                            otherCommands.Add(commandName);
                                                            commandCount++;
                                                        }
                                                    }
                                                }
                                                catch (Exception cmdEx)
                                                {
                                                    Debug.LogWarning($"Error processing command: {cmdEx.Message}");
                                                    continue;
                                                }
                                            }
                                            Debug.Log($"Added {commandCount} internal commands");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("FindAllCommands returned null");
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning("FindAllCommands method not found");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("CommandService instance is null");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Instance property not found on CommandService");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CommandService type not found");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to get internal Unity commands: {e.Message}");
                }

                // 其他额外的命令来源可以尝试
                // ... other commands ...
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Unity commands: {e.Message}\n{e.StackTrace}");
            }

            // 创建命令类别字典以供结果使用
            var commandCategories = new Dictionary<string, List<string>>
            {
                { "MenuCommands", menuCommands.OrderBy(x => x).ToList() },
                { "UtilityCommands", utilityCommands.OrderBy(x => x).ToList() },
                { "AssetCommands", assetCommands.OrderBy(x => x).ToList() },
                { "SceneCommands", sceneCommands.OrderBy(x => x).ToList() },
                { "GameObjectCommands", gameObjectCommands.OrderBy(x => x).ToList() },
                { "PrefabCommands", prefabCommands.OrderBy(x => x).ToList() },
                { "ShortcutCommands", shortcutCommands.OrderBy(x => x).ToList() },
                { "OtherCommands", otherCommands.OrderBy(x => x).ToList() }
            };

            // 计算总命令数
            int totalCount = commandCategories.Values.Sum(list => list.Count);

            Debug.Log($"Command retrieval complete. Found {totalCount} total commands.");

            // 创建一个简化的响应，只包含基本数据
            // 复杂的对象结构可能会导致序列化问题
            var allCommandsList = commandCategories.Values.SelectMany(x => x).OrderBy(x => x).ToList();

            // 使用简单的字符串数组而不是JArray以便更好的序列化
            string[] commandsArray = allCommandsList.ToArray();

            // 验证数组大小
            Debug.Log($"Final commands array contains {commandsArray.Length} items");

            try
            {
                // 返回一个简单的对象，只包含命令数组和计数
                var result = new
                {
                    commands = commandsArray,
                    count = commandsArray.Length
                };

                // 验证结果是否可以正确序列化
                var jsonTest = JsonUtility.ToJson(new { test = "This is a test" });
                Debug.Log($"JSON serialization test successful: {jsonTest}");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating response: {ex.Message}");

                // 最终的后备方案 - 不使用任何JObject/JArray
                return new
                {
                    message = $"Found {commandsArray.Length} commands",
                    firstTen = commandsArray.Take(10).ToArray(),
                    count = commandsArray.Length
                };
            }
        }
    }
}