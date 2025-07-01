using UnityEngine;                  // 导入Unity引擎核心功能
using UnityEditor;                  // 导入Unity编辑器功能
using System.IO;                    // 导入文件和目录操作功能
using Newtonsoft.Json.Linq;         // 导入JSON处理库，用于解析JSON对象
using System.Collections.Generic;   // 导入集合类，如List和Dictionary

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// Handles asset-related commands for the MCP Server
    /// </summary>
    /// <summary>
    /// 处理MCP服务器的资产相关命令
    /// </summary>
    public static class AssetCommandHandler
    {
        /// <summary>
        /// Imports an asset into the project
        /// </summary>
        /// <summary>
        /// 将资产导入到项目中
        /// </summary>
        public static object ImportAsset(JObject @params)
        {
            try
            {
                string sourcePath = (string)@params["source_path"]; // 从参数中获取源文件路径
                string targetPath = (string)@params["target_path"]; // 从参数中获取目标文件路径

                if (string.IsNullOrEmpty(sourcePath))
                    return new { success = false, error = "Source path cannot be empty" }; // 如果源路径为空，返回错误

                if (string.IsNullOrEmpty(targetPath))
                    return new { success = false, error = "Target path cannot be empty" }; // 如果目标路径为空，返回错误

                if (!File.Exists(sourcePath))
                    return new { success = false, error = $"Source file not found: {sourcePath}" }; // 如果源文件不存在，返回错误

                // Ensure target directory exists
                // 确保目标目录存在
                string targetDir = Path.GetDirectoryName(targetPath); // 获取目标路径的目录部分
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir); // 如果目标目录不存在，创建它
                }

                // Copy file to target location
                // 将文件复制到目标位置
                File.Copy(sourcePath, targetPath, true); // 复制文件，true表示覆盖已存在的文件
                AssetDatabase.Refresh(); // 刷新Unity资产数据库，使导入的资产在编辑器中可见

                return new
                {
                    success = true, // 返回成功状态
                    message = $"Successfully imported asset to {targetPath}", // 成功信息
                    path = targetPath // 返回目标路径
                };
            }
            catch (System.Exception e) // 捕获任何异常
            {
                return new
                {
                    success = false, // 返回失败状态
                    error = $"Failed to import asset: {e.Message}", // 错误信息包含异常消息
                    stackTrace = e.StackTrace // 返回堆栈跟踪信息用于调试
                };
            }
        }

        /// <summary>
        /// Instantiates a prefab in the current scene
        /// </summary>
        /// <summary>
        /// 在当前场景中实例化预制体
        /// </summary>
        public static object InstantiatePrefab(JObject @params)
        {
            try
            {
                string prefabPath = (string)@params["prefab_path"]; // 从参数中获取预制体路径

                if (string.IsNullOrEmpty(prefabPath))
                    return new { success = false, error = "Prefab path cannot be empty" }; // 如果预制体路径为空，返回错误

                Vector3 position = new(
                    (float)@params["position_x"], // 从参数中获取X轴位置
                    (float)@params["position_y"], // 从参数中获取Y轴位置
                    (float)@params["position_z"]  // 从参数中获取Z轴位置
                ); // 创建表示实例化位置的Vector3
                
                Vector3 rotation = new(
                    (float)@params["rotation_x"], // 从参数中获取X轴旋转
                    (float)@params["rotation_y"], // 从参数中获取Y轴旋转
                    (float)@params["rotation_z"]  // 从参数中获取Z轴旋转
                ); // 创建表示实例化旋转的Vector3

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath); // 从指定路径加载预制体GameObject
                if (prefab == null)
                {
                    return new { success = false, error = $"Prefab not found at path: {prefabPath}" }; // 如果找不到预制体，返回错误
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); // 实例化预制体到场景中
                if (instance == null)
                {
                    return new { success = false, error = $"Failed to instantiate prefab: {prefabPath}" }; // 如果实例化失败，返回错误
                }

                instance.transform.position = position; // 设置实例的位置
                instance.transform.rotation = Quaternion.Euler(rotation); // 将欧拉角转换为四元数并设置实例的旋转

                return new
                {
                    success = true, // 返回成功状态
                    message = "Successfully instantiated prefab", // 成功信息
                    instance_name = instance.name // 返回实例的名称
                };
            }
            catch (System.Exception e) // 捕获任何异常
            {
                return new
                {
                    success = false, // 返回失败状态
                    error = $"Failed to instantiate prefab: {e.Message}", // 错误信息包含异常消息
                    stackTrace = e.StackTrace // 返回堆栈跟踪信息用于调试
                };
            }
        }

        /// <summary>
        /// Creates a new prefab from a GameObject in the scene
        /// </summary>
        /// <summary>
        /// 从场景中的游戏对象创建新的预制体
        /// </summary>
        public static object CreatePrefab(JObject @params)
        {
            try
            {
                string objectName = (string)@params["object_name"]; // 从参数中获取游戏对象名称
                string prefabPath = (string)@params["prefab_path"]; // 从参数中获取预制体保存路径

                if (string.IsNullOrEmpty(objectName))
                    return new { success = false, error = "GameObject name cannot be empty" }; // 如果游戏对象名称为空，返回错误

                if (string.IsNullOrEmpty(prefabPath))
                    return new { success = false, error = "Prefab path cannot be empty" }; // 如果预制体路径为空，返回错误

                // Ensure prefab has .prefab extension
                // 确保预制体有.prefab扩展名
                if (!prefabPath.ToLower().EndsWith(".prefab"))
                    prefabPath = $"{prefabPath}.prefab"; // 如果路径没有.prefab后缀，添加它

                GameObject sourceObject = GameObject.Find(objectName); // 在场景中查找指定名称的游戏对象
                if (sourceObject == null)
                {
                    return new { success = false, error = $"GameObject not found in scene: {objectName}" }; // 如果找不到游戏对象，返回错误
                }

                // Ensure target directory exists
                // 确保目标目录存在
                string targetDir = Path.GetDirectoryName(prefabPath); // 获取预制体路径的目录部分
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir); // 如果目标目录不存在，创建它
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath); // 将游戏对象保存为预制体资产
                if (prefab == null)
                {
                    return new { success = false, error = "Failed to create prefab. Verify the path is writable." }; // 如果创建失败，返回错误
                }

                return new
                {
                    success = true, // 返回成功状态
                    message = $"Successfully created prefab at {prefabPath}", // 成功信息
                    path = prefabPath // 返回预制体路径
                };
            }
            catch (System.Exception e) // 捕获任何异常
            {
                return new
                {
                    success = false, // 返回失败状态
                    error = $"Failed to create prefab: {e.Message}", // 错误信息包含异常消息
                    stackTrace = e.StackTrace, // 返回堆栈跟踪信息用于调试
                    sourceInfo = $"Object: {@params["object_name"]}, Path: {@params["prefab_path"]}" // 返回源信息用于调试
                };
            }
        }

        /// <summary>
        /// Applies changes from a prefab instance back to the original prefab asset
        /// </summary>
        /// <summary>
        /// 将预制体实例的更改应用回原始预制体资产
        /// </summary>
        public static object ApplyPrefab(JObject @params)
        {
            string objectName = (string)@params["object_name"]; // 从参数中获取游戏对象名称

            GameObject instance = GameObject.Find(objectName); // 在场景中查找指定名称的游戏对象
            if (instance == null)
            {
                return new { error = $"GameObject not found in scene: {objectName}" }; // 如果找不到游戏对象，返回错误
            }

            Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(instance); // 获取实例对应的原始预制体资产
            if (prefabAsset == null)
            {
                return new { error = "Selected object is not a prefab instance" }; // 如果对象不是预制体实例，返回错误
            }

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction); // 将实例更改应用回预制体，使用自动模式
            return new { message = "Successfully applied changes to prefab asset" }; // 返回成功信息
        }

        /// <summary>
        /// Gets a list of assets in the project, optionally filtered by type
        /// </summary>
        /// <summary>
        /// 获取项目中的资产列表，可选择按类型过滤
        /// </summary>
        public static object GetAssetList(JObject @params)
        {
            string type = (string)@params["type"]; // 从参数中获取资产类型过滤条件
            string searchPattern = (string)@params["search_pattern"] ?? "*"; // 从参数中获取搜索模式，默认为"*"（全部）
            string folder = (string)@params["folder"] ?? "Assets"; // 从参数中获取搜索文件夹，默认为"Assets"

            var guids = AssetDatabase.FindAssets(searchPattern, new[] { folder }); // 使用搜索模式和文件夹查找资产GUID
            var assets = new List<object>(); // 创建资产对象列表

            foreach (var guid in guids) // 遍历每个找到的GUID
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); // 将GUID转换为资产路径
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path); // 获取路径上主资产的类型

                // Skip if type filter is specified and doesn't match
                // 如果指定了类型过滤且不匹配则跳过
                if (!string.IsNullOrEmpty(type) && assetType?.Name != type)
                    continue; // 如果指定了类型且不匹配，跳过这个资产

                assets.Add(new
                {
                    name = Path.GetFileNameWithoutExtension(path), // 获取不带扩展名的文件名
                    path, // 资产路径
                    type = assetType?.Name ?? "Unknown", // 资产类型，如果不能确定则为"Unknown"
                    guid // 资产GUID
                });
            }

            return new { assets }; // 返回资产列表
        }
    }
}