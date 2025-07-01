using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ModelParameterLib.Data;
using System;

namespace ModelParameterLib.Module
{
    /// <summary>
    /// 比例库写入与追加工具，支持自动合并与去重
    /// </summary>
    public static class ScaleDataWriter
    {
        /// <summary>
        /// 向指定比例库JSON追加或更新一条数据（自动按类别分组）
        /// </summary>
        public static void AddOrUpdateScaleData(string dbPath, string category, string itemName, ScaleData scaleData)
        {
            JObject root;
            if (File.Exists(dbPath))
            {
                root = JObject.Parse(File.ReadAllText(dbPath));
            }
            else
            {
                root = new JObject();
            }

            if (!root.ContainsKey(category))
                root[category] = new JObject();

            var catObj = (JObject)root[category];
            // 自动计算最大边
            float maxEdge = 0f;
            if (scaleData.RealWorldSizeDict != null && scaleData.RealWorldSizeDict.Count > 0)
            {
                float l = scaleData.RealWorldSizeDict.TryGetValue("length", out var v1) ? v1 : 0f;
                float w = scaleData.RealWorldSizeDict.TryGetValue("width", out var v2) ? v2 : 0f;
                float h = scaleData.RealWorldSizeDict.TryGetValue("height", out var v3) ? v3 : 0f;
                float d = scaleData.RealWorldSizeDict.TryGetValue("diameter", out var v4) ? v4 : 0f;
                maxEdge = Mathf.Max(l, w, h, d);
            }
            float scale = maxEdge > 0.01f ? (float)Math.Round(maxEdge / 100f, 4) : 0.0001f;
            JObject itemObj = new JObject
            {
                ["真实尺寸"] = JObject.FromObject(scaleData.RealWorldSizeDict ?? new Dictionary<string, float>()),
                ["体积"] = scaleData.Volume.HasValue ? Math.Round(scaleData.Volume.Value, 2) : 0,
                ["scale"] = scale
            };
            catObj[itemName] = itemObj;

            File.WriteAllText(dbPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
