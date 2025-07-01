using UnityEngine;                   // 导入Unity引擎核心功能命名空间
using UnityEditor;                   // 导入Unity编辑器功能命名空间
using System;                        // 导入基础系统功能命名空间
using System.IO;                     // 导入文件和目录操作功能命名空间
using System.Text;                   // 导入文本处理功能命名空间
using System.Linq;                   // 导入LINQ查询功能命名空间
using Newtonsoft.Json.Linq;          // 导入JSON处理库命名空间

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理Unity脚本相关命令
    /// </summary>
    public static class ScriptCommandHandler
    {
        /// <summary>
        /// 查看Unity脚本文件的内容
        /// </summary>
        public static object ViewScript(JObject @params)
        {
            string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");  // 从参数中获取脚本路径，如果不存在则抛出异常
            bool requireExists = (bool?)@params["require_exists"] ?? true;   // 获取是否要求文件必须存在的标志，默认为true

            // 正确处理路径以避免"Assets"文件夹重复问题
            string relativePath;
            if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))  // 检查路径是否已经以"Assets/"开头（忽略大小写）
            {
                // 如果路径已经以Assets/开头，为本地路径操作移除它
                relativePath = scriptPath.Substring(7);   // 移除前7个字符("Assets/")
            }
            else
            {
                relativePath = scriptPath;   // 使用原始路径
            }

            string fullPath = Path.Combine(Application.dataPath, relativePath);  // 组合完整路径，Application.dataPath已经指向Assets文件夹

            if (!File.Exists(fullPath))   // 检查文件是否存在
            {
                if (requireExists)   // 如果要求文件必须存在
                {
                    throw new Exception($"Script file not found: {scriptPath}");  // 抛出文件不存在的异常
                }
                else
                {
                    return new { exists = false, message = $"Script file not found: {scriptPath}" };  // 返回文件不存在的信息
                }
            }

            return new { exists = true, content = File.ReadAllText(fullPath) };  // 返回文件存在标志和文件内容
        }

        /// <summary>
        /// 确保项目中存在Scripts文件夹
        /// </summary>
        private static void EnsureScriptsFolderExists()
        {
            // 不要创建"Assets"文件夹，因为它是项目根目录
            // 而是在现有的Assets文件夹中创建"Scripts"
            string scriptsFolderPath = Path.Combine(Application.dataPath, "Scripts");  // 组合Scripts文件夹的完整路径
            if (!Directory.Exists(scriptsFolderPath))  // 检查Scripts文件夹是否存在
            {
                Directory.CreateDirectory(scriptsFolderPath);  // 创建Scripts文件夹
                AssetDatabase.Refresh();  // 刷新资产数据库以识别新文件夹
            }
        }

        /// <summary>
        /// 在指定文件夹中创建新的Unity脚本文件
        /// </summary>
        public static object CreateScript(JObject @params)
        {
            string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");  // 获取脚本名称，如果不存在则抛出异常
            string scriptType = (string)@params["script_type"] ?? "MonoBehaviour";  // 获取脚本类型，默认为MonoBehaviour
            string namespaceName = (string)@params["namespace"];  // 获取命名空间名称
            string template = (string)@params["template"];  // 获取模板
            string scriptFolder = (string)@params["script_folder"];  // 获取脚本文件夹路径
            string content = (string)@params["content"];  // 获取脚本内容
            bool overwrite = (bool?)@params["overwrite"] ?? false;  // 获取是否覆盖现有文件的标志，默认为false

            // 确保脚本名称以.cs结尾
            if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))  // 检查脚本名称是否以.cs结尾（忽略大小写）
                scriptName += ".cs";  // 如果不是，添加.cs扩展名

            // 确保scriptName不包含路径分隔符 - 提取基本名称
            scriptName = Path.GetFileName(scriptName);  // 提取文件名部分

            // 确定脚本路径
            string scriptPath;

            // 处理脚本文件夹参数
            if (string.IsNullOrEmpty(scriptFolder))  // 检查是否提供了脚本文件夹
            {
                // 默认使用Assets中的Scripts文件夹
                scriptPath = "Scripts";  // 设置默认路径为Scripts
                EnsureScriptsFolderExists();  // 确保Scripts文件夹存在
            }
            else
            {
                // 使用提供的文件夹路径
                scriptPath = scriptFolder;  // 使用指定的脚本文件夹

                // 如果scriptFolder以"Assets/"开头，为本地路径操作移除它
                if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))  // 检查是否以Assets/开头
                {
                    scriptPath = scriptPath.Substring(7);  // 移除前7个字符("Assets/")
                }
            }

            // 创建完整的目录路径，避免Assets/Assets问题
            string folderPath = Path.Combine(Application.dataPath, scriptPath);  // 组合文件夹的完整路径

            // 如果目录不存在，创建它
            if (!Directory.Exists(folderPath))  // 检查文件夹是否存在
            {
                try
                {
                    Directory.CreateDirectory(folderPath);  // 创建文件夹
                    AssetDatabase.Refresh();  // 刷新资产数据库
                }
                catch (Exception ex)  // 捕获目录创建异常
                {
                    throw new Exception($"Failed to create directory '{scriptPath}': {ex.Message}");  // 抛出创建目录失败的异常
                }
            }

            // 检查脚本是否已存在
            string fullFilePath = Path.Combine(folderPath, scriptName);  // 组合脚本文件的完整路径
            if (File.Exists(fullFilePath) && !overwrite)  // 检查文件是否存在且不允许覆盖
            {
                throw new Exception($"Script file '{scriptName}' already exists in '{scriptPath}' and overwrite is not enabled.");  // 抛出文件已存在且不允许覆盖的异常
            }

            try
            {
                // 如果提供了内容，直接使用
                if (!string.IsNullOrEmpty(content))  // 检查是否提供了内容
                {
                    // 使用提供的内容创建脚本文件
                    File.WriteAllText(fullFilePath, content);  // 将内容写入文件
                }
                else
                {
                    // 否则根据模板和参数生成内容
                    StringBuilder contentBuilder = new();  // 创建用于构建内容的StringBuilder

                    // 添加using指令
                    contentBuilder.AppendLine("using UnityEngine;");  // 添加UnityEngine命名空间引用
                    contentBuilder.AppendLine();  // 添加空行

                    // 如果指定了命名空间，添加命名空间
                    if (!string.IsNullOrEmpty(namespaceName))  // 检查是否指定了命名空间
                    {
                        contentBuilder.AppendLine($"namespace {namespaceName}");  // 添加命名空间声明
                        contentBuilder.AppendLine("{");  // 添加命名空间开始花括号
                    }

                    // 根据命名空间添加带缩进的类定义
                    string indent = string.IsNullOrEmpty(namespaceName) ? "" : "    ";  // 根据是否有命名空间确定缩进
                    contentBuilder.AppendLine($"{indent}public class {Path.GetFileNameWithoutExtension(scriptName)} : {scriptType}");  // 添加类定义
                    contentBuilder.AppendLine($"{indent}{{");  // 添加类开始花括号

                    // 根据脚本类型添加默认的Unity方法
                    if (scriptType == "MonoBehaviour")  // 如果是MonoBehaviour类型
                    {
                        contentBuilder.AppendLine($"{indent}    private void Start()");  // 添加Start方法
                        contentBuilder.AppendLine($"{indent}    {{");  // 添加方法开始花括号
                        contentBuilder.AppendLine($"{indent}        // Initialize your component here");  // 添加注释
                        contentBuilder.AppendLine($"{indent}    }}");  // 添加方法结束花括号
                        contentBuilder.AppendLine();  // 添加空行
                        contentBuilder.AppendLine($"{indent}    private void Update()");  // 添加Update方法
                        contentBuilder.AppendLine($"{indent}    {{");  // 添加方法开始花括号
                        contentBuilder.AppendLine($"{indent}        // Update your component here");  // 添加注释
                        contentBuilder.AppendLine($"{indent}    }}");  // 添加方法结束花括号
                    }
                    else if (scriptType == "ScriptableObject")  // 如果是ScriptableObject类型
                    {
                        contentBuilder.AppendLine($"{indent}    private void OnEnable()");  // 添加OnEnable方法
                        contentBuilder.AppendLine($"{indent}    {{");  // 添加方法开始花括号
                        contentBuilder.AppendLine($"{indent}        // Initialize your ScriptableObject here");  // 添加注释
                        contentBuilder.AppendLine($"{indent}    }}");  // 添加方法结束花括号
                    }

                    // 关闭类
                    contentBuilder.AppendLine($"{indent}}}");  // 添加类结束花括号

                    // 如果指定了命名空间，关闭命名空间
                    if (!string.IsNullOrEmpty(namespaceName))  // 检查是否指定了命名空间
                    {
                        contentBuilder.AppendLine("}");  // 添加命名空间结束花括号
                    }

                    // 将生成的内容写入文件
                    File.WriteAllText(fullFilePath, contentBuilder.ToString());  // 将构建的内容写入文件
                }

                // 刷新AssetDatabase以识别新脚本
                AssetDatabase.Refresh();  // 刷新资产数据库

                // 返回相对路径以便更容易引用
                string relativePath = scriptPath.Replace('\\', '/');  // 将反斜杠替换为正斜杠
                if (!relativePath.StartsWith("Assets/"))  // 检查是否以Assets/开头
                {
                    relativePath = $"Assets/{relativePath}";  // 如果不是，添加Assets/前缀
                }

                return new
                {
                    message = $"Created script: {Path.Combine(relativePath, scriptName).Replace('\\', '/')}",  // 返回创建成功的消息
                    script_path = Path.Combine(relativePath, scriptName).Replace('\\', '/')  // 返回脚本的路径
                };
            }
            catch (Exception ex)  // 捕获任何异常
            {
                Debug.LogError($"Failed to create script: {ex.Message}\n{ex.StackTrace}");  // 记录错误日志
                throw new Exception($"Failed to create script '{scriptName}': {ex.Message}");  // 抛出创建脚本失败的异常
            }
        }

        /// <summary>
        /// 更新现有Unity脚本的内容
        /// </summary>
        public static object UpdateScript(JObject @params)
        {
            string scriptPath = (string)@params["script_path"] ?? throw new Exception("Parameter 'script_path' is required.");  // 获取脚本路径，如果不存在则抛出异常
            string content = (string)@params["content"] ?? throw new Exception("Parameter 'content' is required.");  // 获取脚本内容，如果不存在则抛出异常
            bool createIfMissing = (bool?)@params["create_if_missing"] ?? false;  // 获取如果文件不存在是否创建的标志，默认为false
            bool createFolderIfMissing = (bool?)@params["create_folder_if_missing"] ?? false;  // 获取如果文件夹不存在是否创建的标志，默认为false

            // 正确处理路径以避免"Assets"文件夹重复
            string relativePath;
            if (scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))  // 检查路径是否已经以"Assets/"开头
            {
                // 如果路径已经以Assets/开头，为本地路径操作移除它
                relativePath = scriptPath.Substring(7);  // 移除前7个字符("Assets/")
            }
            else
            {
                relativePath = scriptPath;  // 使用原始路径
            }

            string fullPath = Path.Combine(Application.dataPath, relativePath);  // 组合脚本文件的完整路径
            string directory = Path.GetDirectoryName(fullPath);  // 获取目录路径

            // 检查文件是否存在，如果请求则创建
            if (!File.Exists(fullPath))  // 检查文件是否存在
            {
                if (createIfMissing)  // 如果允许在文件不存在时创建
                {
                    // 如果需要且请求了，创建目录
                    if (!Directory.Exists(directory) && createFolderIfMissing)  // 检查目录是否存在且允许创建
                    {
                        Directory.CreateDirectory(directory);  // 创建目录
                    }
                    else if (!Directory.Exists(directory))  // 如果目录不存在且不允许创建
                    {
                        throw new Exception($"Directory does not exist: {Path.GetDirectoryName(scriptPath)}");  // 抛出目录不存在的异常
                    }

                    // 创建带内容的文件
                    File.WriteAllText(fullPath, content);  // 将内容写入文件
                    AssetDatabase.Refresh();  // 刷新资产数据库
                    return new { message = $"Created script: {scriptPath}" };  // 返回创建成功的消息
                }
                else
                {
                    throw new Exception($"Script file not found: {scriptPath}");  // 抛出文件不存在的异常
                }
            }

            // 更新现有脚本
            File.WriteAllText(fullPath, content);  // 将内容写入文件

            // 刷新AssetDatabase
            AssetDatabase.Refresh();  // 刷新资产数据库

            return new { message = $"Updated script: {scriptPath}" };  // 返回更新成功的消息
        }

        /// <summary>
        /// 列出指定文件夹中的所有脚本文件
        /// </summary>
        public static object ListScripts(JObject @params)
        {
            string folderPath = (string)@params["folder_path"] ?? "Assets";  // 获取文件夹路径，默认为"Assets"

            // 对"Assets"路径进行特殊处理，因为它已经是根目录
            string fullPath;
            if (folderPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))  // 检查是否就是"Assets"路径
            {
                fullPath = Application.dataPath;  // 直接使用Application.dataPath
            }
            else if (folderPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))  // 检查是否以"Assets/"开头
            {
                // 从路径中移除"Assets/"，因为Application.dataPath已经指向它
                string relativePath = folderPath.Substring(7);  // 移除前7个字符("Assets/")
                fullPath = Path.Combine(Application.dataPath, relativePath);  // 组合完整路径
            }
            else
            {
                // 假设它是Assets的相对路径
                fullPath = Path.Combine(Application.dataPath, folderPath);  // 组合完整路径
            }

            if (!Directory.Exists(fullPath))  // 检查文件夹是否存在
                throw new Exception($"Folder not found: {folderPath}");  // 抛出文件夹不存在的异常

            string[] scripts = Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories)  // 递归获取所有.cs文件
                .Select(path => path.Replace(Application.dataPath, "Assets"))  // 将绝对路径转换为相对于Assets的路径
                .ToArray();  // 转换为数组

            return new { scripts };  // 返回脚本列表
        }

        /// <summary>
        /// 将脚本组件附加到GameObject
        /// </summary>
        public static object AttachScript(JObject @params)
        {
            string objectName = (string)@params["object_name"] ?? throw new Exception("Parameter 'object_name' is required.");  // 获取对象名称，如果不存在则抛出异常
            string scriptName = (string)@params["script_name"] ?? throw new Exception("Parameter 'script_name' is required.");  // 获取脚本名称，如果不存在则抛出异常
            string scriptPath = (string)@params["script_path"];  // 获取脚本路径（可选）

            // 查找目标对象
            GameObject targetObject = GameObject.Find(objectName);  // 在场景中查找指定名称的游戏对象
            if (targetObject == null)  // 检查对象是否存在
                throw new Exception($"Object '{objectName}' not found in scene.");  // 抛出对象不存在的异常

            // 确保脚本名称以.cs结尾
            if (!scriptName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))  // 检查脚本名称是否以.cs结尾
                scriptName += ".cs";  // 如果不是，添加.cs扩展名

            // 如果scriptName包含路径分隔符，移除路径部分
            string scriptFileName = Path.GetFileName(scriptName);  // 获取文件名部分
            string scriptNameWithoutExtension = Path.GetFileNameWithoutExtension(scriptFileName);  // 获取不带扩展名的文件名

            // 查找脚本资产
            string[] guids;

            if (!string.IsNullOrEmpty(scriptPath))  // 检查是否提供了特定路径
            {
                // 如果提供了特定路径，首先尝试该路径
                if (File.Exists(Path.Combine(Application.dataPath, scriptPath.Replace("Assets/", ""))))  // 检查文件是否存在
                {
                    // 如果文件存在，使用直接路径
                    MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);  // 加载脚本资产
                    if (scriptAsset != null)  // 检查脚本资产是否存在
                    {
                        Type scriptType = scriptAsset.GetClass();  // 获取脚本类型
                        if (scriptType != null)  // 检查类型是否存在
                        {
                            try
                            {
                                // 尝试添加组件
                                Component component = targetObject.AddComponent(scriptType);  // 向目标对象添加组件
                                if (component != null)  // 检查组件是否添加成功
                                {
                                    return new
                                    {
                                        message = $"Successfully attached script '{scriptFileName}' to object '{objectName}'",  // 返回添加成功的消息
                                        component_type = scriptType.Name  // 返回组件类型名称
                                    };
                                }
                            }
                            catch (Exception ex)  // 捕获任何异常
                            {
                                Debug.LogError($"Error attaching script component: {ex.Message}");  // 记录错误日志
                                throw new Exception($"Failed to add component: {ex.Message}");  // 抛出添加组件失败的异常
                            }
                        }
                    }
                }
            }

            // 如果直接路径不起作用，使用文件名进行搜索
            guids = AssetDatabase.FindAssets(scriptNameWithoutExtension + " t:script");  // 搜索指定名称的脚本资产

            if (guids.Length == 0)  // 如果没有找到精确匹配
            {
                // 如果精确匹配失败，尝试更广泛的搜索
                guids = AssetDatabase.FindAssets(scriptNameWithoutExtension);  // 搜索包含指定名称的任何资产

                if (guids.Length == 0)  // 如果仍然没有找到
                    throw new Exception($"Script '{scriptFileName}' not found in project.");  // 抛出脚本未找到的异常
            }

            // 检查每个潜在脚本，直到找到一个可以附加的
            foreach (string guid in guids)  // 遍历每个找到的GUID
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);  // 获取GUID对应的资产路径

                // 只考虑.cs文件
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))  // 检查是否是.cs文件
                    continue;  // 如果不是，跳过

                // 再次检查文件名，以避免错误匹配
                string foundFileName = Path.GetFileName(path);  // 获取找到文件的文件名
                if (!string.Equals(foundFileName, scriptFileName, StringComparison.OrdinalIgnoreCase) &&  // 检查文件名是否匹配
                    !string.Equals(Path.GetFileNameWithoutExtension(foundFileName), scriptNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                    continue;  // 如果不匹配，跳过

                MonoScript scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);  // 加载脚本资产
                if (scriptAsset == null)  // 检查脚本资产是否存在
                    continue;  // 如果不存在，跳过

                Type scriptType = scriptAsset.GetClass();  // 获取脚本类型
                if (scriptType == null || !typeof(MonoBehaviour).IsAssignableFrom(scriptType))  // 检查类型是否存在且是否继承自MonoBehaviour
                    continue;  // 如果不是，跳过

                try
                {
                    // 检查组件是否已经附加
                    if (targetObject.GetComponent(scriptType) != null)  // 检查目标对象是否已经有该类型的组件
                    {
                        return new
                        {
                            message = $"Script '{scriptNameWithoutExtension}' is already attached to object '{objectName}'",  // 返回组件已存在的消息
                            component_type = scriptType.Name  // 返回组件类型名称
                        };
                    }

                    // 添加组件
                    Component component = targetObject.AddComponent(scriptType);  // 向目标对象添加组件
                    if (component != null)  // 检查组件是否添加成功
                    {
                        return new
                        {
                            message = $"Successfully attached script '{scriptFileName}' to object '{objectName}'",  // 返回添加成功的消息
                            component_type = scriptType.Name,  // 返回组件类型名称
                            script_path = path  // 返回脚本路径
                        };
                    }
                }
                catch (Exception ex)  // 捕获任何异常
                {
                    Debug.LogError($"Error attaching script '{path}': {ex.Message}");  // 记录错误日志
                    // 继续尝试其他匹配，而不是立即失败
                }
            }

            // 如果尝试了所有可能性但没有一个成功
            throw new Exception($"Could not attach script '{scriptFileName}' to object '{objectName}'. No valid script found or component creation failed.");  // 抛出无法附加脚本的异常
        }
    }
}