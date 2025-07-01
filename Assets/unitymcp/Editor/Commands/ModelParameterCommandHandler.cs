using UnityEngine;                  // 导入Unity引擎核心功能
using UnityEditor;                  // 导入Unity编辑器功能
using System.IO;                    // 导入文件和目录操作功能
using Newtonsoft.Json.Linq;         // 导入JSON处理库，用于解析JSON对象
using System.Collections.Generic;   // 导入集合类，如List和Dictionary

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理模型参数相关命令
    /// </summary>
    public static class ModelParameterCommandHandler
    {
        /// <summary>
        /// 批量GLB转Prefab并自动AI补全比例
        /// </summary>
        public static object GLBBatchConvert(JObject @params)
        {
            Debug.Log("[MCP] GLBBatchConvert called");
            try
            {
                string courseFolder = (string)@params["course_folder"];
                Debug.Log($"[MCP] course_folder param: {courseFolder}");
                if (string.IsNullOrEmpty(courseFolder))
                {
                    Debug.LogError("[MCP] course_folder参数不能为空");
                    return new { success = false, error = "course_folder参数不能为空" };
                }

                // 异步启动，不阻塞主线程
                _ = ModelParameterLib.Editor.GLBToPrefabBatchConverter.RunBatchConvert(courseFolder);

                Debug.Log("[MCP] RunBatchConvert started (async)");
                return new { success = true, message = "批量转换已异步启动，请前往untiy查看结果。" };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCP] GLBBatchConvert Exception: {ex.Message}\n{ex.StackTrace}");
                return new { success = false, error = ex.Message };
            }
        }
    }
}