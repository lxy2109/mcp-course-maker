using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ModelParameterLib.Data;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ModelParameterLib.Module
{
    /// <summary>
    /// 比例JSON数据加载与模糊匹配工具
    /// </summary>
    public class JsonScaleDataLoader
    {
        private Dictionary<string, ScaleData> modelScaleDict = new Dictionary<string, ScaleData>();
        private string lastLoadedCourseFolder = null;
        private Dictionary<string, Dictionary<string, Dictionary<string, ScaleData>>> allLoadedDictionaries = new Dictionary<string, Dictionary<string, Dictionary<string, ScaleData>>>();
        private Dictionary<string, (ScaleData data, string category, string dbPath)> allModelNameMap = new Dictionary<string, (ScaleData, string, string)>();

        /// <summary>
        /// 加载指定课程目录下的比例JSON数据
        /// </summary>
        public void LoadScaleDataForCourse(string courseFolder)
        {
            modelScaleDict.Clear();
            lastLoadedCourseFolder = courseFolder;
            string[] jsonFiles = Directory.GetFiles(courseFolder, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var dict = JsonUtility.FromJson<ScaleDataDictWrapper>(json);
                    if (dict != null && dict.models != null)
                    {
                        foreach (var kv in dict.models)
                        {
                            if (!modelScaleDict.ContainsKey(kv.Key))
                                modelScaleDict.Add(kv.Key, kv.Value);
                        }
                    }
                }
                catch { /* 忽略单个文件错误 */ }
            }
        }

        /// <summary>
        /// 获取指定模型名的比例数据，支持模糊匹配
        /// </summary>
        public ScaleData GetScaleDataForModel(string modelName)
        {
            if (modelScaleDict.TryGetValue(modelName, out var data))
                return data;
            // 尝试模糊匹配
            string fuzzy = TryFuzzyMatchModelName(modelName);
            if (fuzzy != null && modelScaleDict.TryGetValue(fuzzy, out var fuzzyData))
                return fuzzyData;
            return null;
        }

        // 字符串相似度算法（Levenshtein归一化）
        private float CalcSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0f;
            int len1 = s1.Length, len2 = s2.Length;
            int[,] dp = new int[len1 + 1, len2 + 1];
            for (int i = 0; i <= len1; i++) dp[i, 0] = i;
            for (int j = 0; j <= len2; j++) dp[0, j] = j;
            for (int i = 1; i <= len1; i++)
                for (int j = 1; j <= len2; j++)
                    dp[i, j] = s1[i - 1] == s2[j - 1] ? dp[i - 1, j - 1] : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i, j - 1], dp[i - 1, j]));
            int dist = dp[len1, len2];
            int maxLen = Math.Max(len1, len2);
            return maxLen == 0 ? 1f : 1f - (float)dist / maxLen;
        }

        // 高相似度模糊匹配
        public string TryFuzzyMatchModelName(string modelName, float threshold = 0.75f)
        {
            string bestKey = null;
            float bestScore = 0f;
            foreach (var key in modelScaleDict.Keys)
            {
                float score = CalcSimilarity(modelName, key);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestKey = key;
                }
            }
            if (bestScore >= threshold)
                return bestKey;
            // 兼容原有Contains逻辑
            foreach (var key in modelScaleDict.Keys)
                if (key.Contains(modelName) || modelName.Contains(key))
                    return key;
            return null;
        }

        /// <summary>
        /// 加载多个比例库（全局/标准库），合并到modelScaleDict
        /// </summary>
        public void LoadAllScaleData(string[] jsonPaths)
        {
            modelScaleDict.Clear();
            allLoadedDictionaries.Clear();
            allModelNameMap.Clear();
            foreach (var jsonPath in jsonPaths)
            {
                if (!File.Exists(jsonPath)) continue;
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var root = JObject.Parse(json);
                    bool isStandard = false;
                    foreach (var prop in root.Properties())
                    {
                        if (prop.Value.Type == JTokenType.Object)
                        {
                            var firstObj = (JObject)prop.Value;
                            foreach (var item in firstObj.Properties())
                            {
                                if (item.Value.Type == JTokenType.Object)
                                {
                                    isStandard = true;
                                    break;
                                }
                            }
                        }
                        if (isStandard) break;
                    }
                    if (isStandard)
                    {
                        var dict = new Dictionary<string, Dictionary<string, ScaleData>>();
                        foreach (var cat in root)
                        {
                            var catDict = new Dictionary<string, ScaleData>();
                            var items = cat.Value as JObject;
                            if (items == null) continue;
                            foreach (var obj in items)
                            {
                                var itemObj = obj.Value as JObject;
                                if (itemObj == null) continue;
                                var realSize = new Dictionary<string, float>();
                                var sizeObj = itemObj["真实尺寸"] as JObject;
                                if (sizeObj != null)
                                {
                                    foreach (var prop in sizeObj)
                                    {
                                        if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                                            realSize[prop.Key] = prop.Value.Value<float>();
                                    }
                                }
                                float? volume = null;
                                if (itemObj["体积"] != null &&
                                    (itemObj["体积"].Type == JTokenType.Float || itemObj["体积"].Type == JTokenType.Integer))
                                {
                                    volume = itemObj["体积"].Value<float>();
                                }
                                float? scale = null;
                                if (itemObj["scale"] != null &&
                                    (itemObj["scale"].Type == JTokenType.Float || itemObj["scale"].Type == JTokenType.Integer))
                                {
                                    scale = itemObj["scale"].Value<float>();
                                }
                                var scaleData = new ScaleData
                                {
                                    RealWorldSizeDict = realSize,
                                    Volume = volume,
                                    ScaleValue = scale,
                                    Category = cat.Key
                                };
                                catDict[obj.Key] = scaleData;
                                if (!modelScaleDict.ContainsKey(obj.Key))
                                    modelScaleDict.Add(obj.Key, scaleData);
                                // 哈希表填充
                                if (!allModelNameMap.ContainsKey(obj.Key))
                                    allModelNameMap[obj.Key] = (scaleData, cat.Key, jsonPath);
                            }
                            dict[cat.Key] = catDict;
                        }
                        allLoadedDictionaries[jsonPath] = dict;
                        continue;
                    }
                    else if (root["models"] != null && root["models"].Type == JTokenType.Object)
                    {
                        var flat = new Dictionary<string, Dictionary<string, ScaleData>>();
                        var modelsObj = root["models"] as JObject;
                        var modelsDict = new Dictionary<string, ScaleData>();
                        foreach (var obj in modelsObj)
                        {
                            var itemObj = obj.Value as JObject;
                            if (itemObj == null) continue;
                            var realSize = new Dictionary<string, float>();
                            var sizeObj = itemObj["真实尺寸"] as JObject;
                            if (sizeObj != null)
                            {
                                foreach (var prop in sizeObj)
                                {
                                    if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
                                        realSize[prop.Key] = prop.Value.Value<float>();
                                }
                            }
                            float? volume = null;
                            if (itemObj["体积"] != null &&
                                (itemObj["体积"].Type == JTokenType.Float || itemObj["体积"].Type == JTokenType.Integer))
                            {
                                volume = itemObj["体积"].Value<float>();
                            }
                            float? scale = null;
                            if (itemObj["scale"] != null &&
                                (itemObj["scale"].Type == JTokenType.Float || itemObj["scale"].Type == JTokenType.Integer))
                            {
                                scale = itemObj["scale"].Value<float>();
                            }
                            var scaleData = new ScaleData
                            {
                                RealWorldSizeDict = realSize,
                                Volume = volume,
                                ScaleValue = scale,
                                Category = "默认"
                            };
                            modelsDict[obj.Key] = scaleData;
                            if (!modelScaleDict.ContainsKey(obj.Key))
                                modelScaleDict.Add(obj.Key, scaleData);
                            // 哈希表填充
                            if (!allModelNameMap.ContainsKey(obj.Key))
                                allModelNameMap[obj.Key] = (scaleData, "默认", jsonPath);
                        }
                        flat["默认"] = modelsDict;
                        allLoadedDictionaries[jsonPath] = flat;
                    }
                }
                catch 
                {
                    // 忽略或可选日志
                }
            }
        }

        /// <summary>
        /// 在所有已加载的比例库中查找模型，返回 (ScaleData, 来源库路径/标识)
        /// </summary>
        public (ScaleData, string) GetScaleDataForModelAll(string modelName)
        {
            foreach (var db in allLoadedDictionaries)
            {
                foreach (var category in db.Value)
                {
                    foreach (var item in category.Value)
                    {
                        if (item.Key == modelName)
                            return (item.Value, db.Key); // db.Key为库路径或标识
                    }
                }
            }
            return (null, null);
        }

        /// <summary>
        /// 获取指定课程目录下的比例库json路径（如：{courseFolder}/ModelScale.json）
        /// </summary>
        public static string GetScaleDbPathForCourse(string courseFolder)
        {
            return Path.Combine(courseFolder, "ModelScale.json");
        }

        /// <summary>
        /// 在所有已加载的比例库（支持多级分类）中查找模型，支持模糊匹配，返回(ScaleData, 来源库路径/标识, 命中物品名, 相似度)
        /// </summary>
        public (ScaleData, string, string, float) GetScaleDataForModelAllFuzzy(string modelName, float threshold = 0.75f)
        {
            // 先去除末尾数字
            string normalizedName = Regex.Replace(modelName, @"\d+$", "");
            normalizedName = normalizedName.Trim();
            // 1. 先查哈希表
            if (allModelNameMap.TryGetValue(normalizedName, out var tuple))
            {
                return (tuple.data, tuple.dbPath, normalizedName, 1.0f);
            }
            // 2. 再做模糊匹配
            ScaleData bestData = null;
            string bestDb = null;
            string bestItemName = null;
            string bestCategory = null;
            float bestScore = 0f;
            foreach (var kv in allModelNameMap)
            {
                float score = CalcSimilarity(normalizedName, kv.Key);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestData = kv.Value.data;
                    bestDb = kv.Value.dbPath;
                    bestItemName = kv.Key;
                    bestCategory = kv.Value.category;
                }
            }
            if (bestScore >= threshold)
            {
                return (bestData, bestDb, bestItemName, bestScore);
            }
            return (null, null, null, bestScore);
        }

        [System.Serializable]
        private class ScaleDataDictWrapper
        {
            public Dictionary<string, ScaleData> models;
        }
    }
} 