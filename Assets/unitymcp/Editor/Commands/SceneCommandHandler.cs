using UnityEngine.SceneManagement;    // 导入Unity场景管理命名空间，用于场景操作
using System.Linq;                    // 导入LINQ命名空间，用于集合查询操作
using System;                         // 导入基础系统命名空间
using Newtonsoft.Json.Linq;           // 导入JSON处理库命名空间
using UnityEditor;                    // 导入Unity编辑器命名空间
using UnityEditor.SceneManagement;    // 导入Unity编辑器场景管理命名空间，提供编辑器下的场景操作功能

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理MCP服务器的场景相关命令
    /// </summary>
    public static class SceneCommandHandler
    {
        /// <summary>
        /// 获取当前场景信息
        /// </summary>
        /// <returns>包含场景名称和根级游戏对象的场景信息</returns>
        public static object GetSceneInfo()
        {
            var scene = SceneManager.GetActiveScene();          // 获取当前激活的场景
            var rootObjects = scene.GetRootGameObjects().Select(o => o.name).ToArray();  // 获取场景中所有根级游戏对象的名称并转换为数组
            return new { sceneName = scene.name, rootObjects }; // 返回包含场景名称和根级对象的匿名对象
        }

        /// <summary>
        /// 在Unity编辑器中打开指定的场景
        /// </summary>
        /// <param name="params">包含场景路径的参数</param>
        /// <returns>操作结果</returns>
        public static object OpenScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];  // 从参数中获取场景路径
                if (string.IsNullOrEmpty(scenePath))               // 检查场景路径是否为空
                    return new { success = false, error = "Scene path cannot be empty" };  // 如果为空，返回错误信息

                if (!System.IO.File.Exists(scenePath))              // 检查场景文件是否存在
                    return new { success = false, error = $"Scene file not found: {scenePath}" };  // 如果不存在，返回错误信息

                EditorSceneManager.OpenScene(scenePath);           // 使用编辑器场景管理器打开指定场景
                return new { success = true, message = $"Opened scene: {scenePath}" };  // 返回成功信息
            }
            catch (Exception e)                                    // 捕获任何异常
            {
                return new { success = false, error = $"Failed to open scene: {e.Message}", stackTrace = e.StackTrace };  // 返回错误信息和堆栈跟踪
            }
        }

        /// <summary>
        /// 保存当前场景
        /// </summary>
        /// <returns>操作结果</returns>
        public static object SaveScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();          // 获取当前激活的场景
                EditorSceneManager.SaveScene(scene);               // 使用编辑器场景管理器保存场景
                return new { success = true, message = $"Saved scene: {scene.path}" };  // 返回成功信息和场景路径
            }
            catch (Exception e)                                    // 捕获任何异常
            {
                return new { success = false, error = $"Failed to save scene: {e.Message}", stackTrace = e.StackTrace };  // 返回错误信息和堆栈跟踪
            }
        }

        /// <summary>
        /// 创建一个新的空场景
        /// </summary>
        /// <param name="params">包含新场景路径的参数</param>
        /// <returns>操作结果</returns>
        public static object NewScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];   // 从参数中获取场景路径
                if (string.IsNullOrEmpty(scenePath))                // 检查场景路径是否为空
                    return new { success = false, error = "Scene path cannot be empty" };  // 如果为空，返回错误信息

                // 创建新场景
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);  // 创建一个空场景

                // 确保场景已加载并激活
                if (!scene.isLoaded)                               // 检查场景是否已加载
                {
                    EditorSceneManager.LoadScene(scenePath);       // 如果未加载，则加载场景
                }

                // 保存场景
                EditorSceneManager.SaveScene(scene, scenePath);    // 将场景保存到指定路径

                // 强制刷新场景视图
                EditorApplication.ExecuteMenuItem("Window/General/Scene");  // 通过菜单项打开场景视图窗口

                return new { success = true, message = $"Created new scene at: {scenePath}" };  // 返回成功信息
            }
            catch (Exception e)                                    // 捕获任何异常
            {
                return new { success = false, error = $"Failed to create new scene: {e.Message}", stackTrace = e.StackTrace };  // 返回错误信息和堆栈跟踪
            }
        }

        /// <summary>
        /// 切换到不同的场景，可选择是否保存当前场景
        /// </summary>
        /// <param name="params">包含目标场景路径和保存选项的参数</param>
        /// <returns>操作结果</returns>
        public static object ChangeScene(JObject @params)
        {
            try
            {
                string scenePath = (string)@params["scene_path"];      // 从参数中获取场景路径
                bool saveCurrent = @params["save_current"]?.Value<bool>() ?? false;  // 获取是否保存当前场景的标志，默认为false

                if (string.IsNullOrEmpty(scenePath))                   // 检查场景路径是否为空
                    return new { success = false, error = "Scene path cannot be empty" };  // 如果为空，返回错误信息

                if (!System.IO.File.Exists(scenePath))                 // 检查场景文件是否存在
                    return new { success = false, error = $"Scene file not found: {scenePath}" };  // 如果不存在，返回错误信息

                // 如果请求，保存当前场景
                if (saveCurrent)                                       // 检查是否需要保存当前场景
                {
                    var currentScene = SceneManager.GetActiveScene();   // 获取当前激活的场景
                    EditorSceneManager.SaveScene(currentScene);        // 保存当前场景
                }

                // 打开新场景
                EditorSceneManager.OpenScene(scenePath);               // 使用编辑器场景管理器打开指定场景
                return new { success = true, message = $"Changed to scene: {scenePath}" };  // 返回成功信息
            }
            catch (Exception e)                                        // 捕获任何异常
            {
                return new { success = false, error = $"Failed to change scene: {e.Message}", stackTrace = e.StackTrace };  // 返回错误信息和堆栈跟踪
            }
        }
    }
}