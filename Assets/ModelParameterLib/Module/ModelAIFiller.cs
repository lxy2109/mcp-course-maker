using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_2019_1_OR_NEWER
using Unity.EditorCoroutines.Editor;
#endif

namespace ModelParameterLib.Module
{
    /// <summary>
    /// AI尺寸补全服务，负责调用AI接口补全模型类别和尺寸
    /// </summary>
    public class ModelAIFiller
    {
        private const string apiUrl = "https://api.deepseek.com/v1/chat/completions";

        #if UNITY_EDITOR
        private const string DeepSeekApiKeyPref = "DeepSeekApiKey";
        public static string DeepSeekApiKey
        {
            get => EditorPrefs.GetString(DeepSeekApiKeyPref, "");
            set => EditorPrefs.SetString(DeepSeekApiKeyPref, value ?? "");
        }
        #endif

        private string BuildPrompt(string itemName)
        {
            // 要求AI返回中文大类，禁止英文和物品名
            return $@"请根据下述物品名称，推断其所属的标准中文大类
            (如""理化实验器材""、""生物实验器材""、""医学护理器材""、""工程物理器材""、""日常与辅助用品""、""食品与厨房用具""、""建筑与材料""、""电子与元器件""、""交通与车辆""、""环境与安全""、""运动与体育器材""、""文具与学习用品""等)，
            并给出标准3D尺寸(单位：厘米)。只允许返回标准JSON，且category字段必须为中文上一级分类，不能与物品名称相同，也不能用""物品""、""器材""等泛用词。禁止输出英文。
            只返回JSON，例如: ""{{""category"":""实验玻璃器皿"",""length"":8,""width"":5,""height"":3}}""。物品名称: {itemName}";
        }

        private string BuildMaterialPrompt(string itemName)
        {
            // 要求AI只返回标准材质类型，禁止英文和物品名
            return $"请根据下述物品名称，推断其主要材质类型（如\"玻璃\"、\"金属\"、\"塑料\"、\"陶瓷\"、\"木材\"、\"纸张\"、\"橡胶\"、\"织物\"、\"复合材料\"等），只允许返回标准JSON，且material字段必须为中文材质类型，不能与物品名称相同，也不能用\"物品\"、\"器材\"等泛用词。禁止输出英文。\n只返回JSON，例如: {{\"material\":\"玻璃\"}}。物品名称: {itemName}";
        }

        private string ExtractJson(string aiResult)
        {
            if (string.IsNullOrEmpty(aiResult)) return null;
            // 尝试直接解析
            aiResult = aiResult.Trim();
            if (aiResult.StartsWith("{") && aiResult.EndsWith("}")) return aiResult;
            // 用正则提取第一个JSON对象
            var match = System.Text.RegularExpressions.Regex.Match(aiResult, "{[\\s\\S]*}");
            if (match.Success) return match.Value;
            return null;
        }

        /// <summary>
        /// 单个物品AI尺寸补全
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <param name="categoryHint">类别提示</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="onResult">回调，参数为(success, jsonResult, errorMsg)</param>
        public void FillSizeAsync(string itemName, string categoryHint, string apiKey, Action<bool, string, string> onResult)
        {
            Debug.Log($"[ModelAISizeFiller] FillSizeAsync called. itemName: {itemName}, categoryHint: {categoryHint}, apiKey empty: {string.IsNullOrEmpty(apiKey)}");
#if UNITY_2019_1_OR_NEWER
            EditorCoroutineUtility.StartCoroutineOwnerless(FillSizeInternalCoroutine(itemName, apiKey, onResult));
#else
            EditorApplication.update += EditorCoroutineHelper(FillSizeInternalCoroutine(itemName, apiKey, onResult));
#endif
        }

        /// <summary>
        /// 批量物品AI尺寸补全（每个物品独立回调，带索引）
        /// </summary>
        /// <param name="items">物品列表，每个元素为(itemName, categoryHint)</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="onResult">回调，参数为(index, success, jsonResult, errorMsg)</param>
        public void FillSizeBatchAsync(List<(string itemName, string categoryHint)> items, string apiKey, Action<int, bool, string, string> onResult)
        {
            Debug.Log($"[ModelAISizeFiller] FillSizeBatchAsync called. items.Count: {items?.Count ?? 0}, apiKey empty: {string.IsNullOrEmpty(apiKey)}");
#if UNITY_2019_1_OR_NEWER
            EditorCoroutineUtility.StartCoroutineOwnerless(FillSizeBatchInternalCoroutine(items, apiKey, onResult));
#else
            EditorApplication.update += EditorCoroutineHelper(FillSizeBatchInternalCoroutine(items, apiKey, onResult));
#endif
        }

        /// <summary>
        /// 内部通用AI补全逻辑（协程实现）
        /// </summary>
        private System.Collections.IEnumerator FillSizeInternalCoroutine(string itemName, string apiKey, Action<bool, string, string> onResult)
        {
            var prompt = BuildPrompt(itemName);
            Debug.Log($"[ModelAISizeFiller] FillSizeInternalCoroutine: prompt=\n{prompt}");
            var body = new
            {
                model = "deepseek-chat",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.2,
                max_tokens = 512
            };
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            Debug.Log($"[ModelAISizeFiller] Request body: {jsonBody}");

            var request = new UnityEngine.Networking.UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onResult(false, null, request.error);
            }
            else
            {
                var respStr = request.downloadHandler.text;
                Debug.Log($"[ModelAISizeFiller] DeepSeek AI原始返回: {respStr}");
                if (!request.result.ToString().StartsWith("Success"))
                {
                    onResult(false, null, respStr);
                    yield break;
                }
                var respObj = JObject.Parse(respStr);
                var aiResult = respObj["choices"]?[0]?["message"]?["content"]?.ToString();
                Debug.Log($"[ModelAISizeFiller] DeepSeek AI content: {aiResult}");
                var json = ExtractJson(aiResult);
                if (json == null)
                {
                    Debug.LogError($"[ModelAISizeFiller] AI返回内容无法提取为JSON: {aiResult}");
                    onResult(false, null, "AI返回内容无法提取为JSON: " + aiResult);
                    yield break;
                }
                Debug.Log($"[ModelAISizeFiller] 提取到的JSON: {json}");
                onResult(true, json, null);
            }
        }

        /// <summary>
        /// 批量补全内部协程
        /// </summary>
        private System.Collections.IEnumerator FillSizeBatchInternalCoroutine(List<(string itemName, string categoryHint)> items, string apiKey, Action<int, bool, string, string> onResult)
        {
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                var (itemName, categoryHint) = items[i];
                Debug.Log($"[ModelAISizeFiller] Batch item {idx}: itemName={itemName}, categoryHint={categoryHint}");
                bool done = false;
                bool success = false;
                string jsonResult = null;
                string errorMsg = null;
                yield return FillSizeInternalCoroutine(itemName, apiKey, (succ, json, err) =>
                {
                    success = succ;
                    jsonResult = json;
                    errorMsg = err;
                    done = true;
                });
                while (!done) yield return null;
                onResult?.Invoke(idx, success, jsonResult, errorMsg);
            }
        }

        /// <summary>
        /// 单个物品AI材质分析
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="onResult">回调，参数为(success, jsonResult, errorMsg)</param>
        public void FillMaterialAsync(string itemName, string apiKey, Action<bool, string, string> onResult)
        {
            Debug.Log($"[ModelAIMaterialFiller] FillMaterialAsync called. itemName: {itemName}, apiKey empty: {string.IsNullOrEmpty(apiKey)}");
#if UNITY_2019_1_OR_NEWER
            EditorCoroutineUtility.StartCoroutineOwnerless(FillMaterialInternalCoroutine(itemName, apiKey, onResult));
#else
            EditorApplication.update += EditorCoroutineHelper(FillMaterialInternalCoroutine(itemName, apiKey, onResult));
#endif
        }

        /// <summary>
        /// 批量物品AI材质分析（每个物品独立回调，带索引）
        /// </summary>
        /// <param name="items">物品列表，每个元素为itemName</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="onResult">回调，参数为(index, success, jsonResult, errorMsg)</param>
        public void FillMaterialBatchAsync(List<string> items, string apiKey, Action<int, bool, string, string> onResult)
        {
            Debug.Log($"[ModelAIMaterialFiller] FillMaterialBatchAsync called. items.Count: {items?.Count ?? 0}, apiKey empty: {string.IsNullOrEmpty(apiKey)}");
#if UNITY_2019_1_OR_NEWER
            EditorCoroutineUtility.StartCoroutineOwnerless(FillMaterialBatchInternalCoroutine(items, apiKey, onResult));
#else
            EditorApplication.update += EditorCoroutineHelper(FillMaterialBatchInternalCoroutine(items, apiKey, onResult));
#endif
        }

        /// <summary>
        /// 内部通用AI材质分析逻辑（协程实现）
        /// </summary>
        private System.Collections.IEnumerator FillMaterialInternalCoroutine(string itemName, string apiKey, Action<bool, string, string> onResult)
        {
            var prompt = BuildMaterialPrompt(itemName);
            Debug.Log($"[ModelAIMaterialFiller] FillMaterialInternalCoroutine: prompt=\n{prompt}");
            var body = new
            {
                model = "deepseek-chat",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.2,
                max_tokens = 256
            };
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);
            Debug.Log($"[ModelAIMaterialFiller] Material Request body: {jsonBody}");

            var request = new UnityEngine.Networking.UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onResult(false, null, request.error);
            }
            else
            {
                var respStr = request.downloadHandler.text;
                Debug.Log($"[ModelAIMaterialFiller] DeepSeek AI原始返回(Material): {respStr}");
                if (!request.result.ToString().StartsWith("Success"))
                {
                    onResult(false, null, respStr);
                    yield break;
                }
                var respObj = JObject.Parse(respStr);
                var aiResult = respObj["choices"]?[0]?["message"]?["content"]?.ToString();
                Debug.Log($"[ModelAIMaterialFiller] DeepSeek AI content(Material): {aiResult}");
                var json = ExtractJson(aiResult);
                if (json == null)
                {
                    Debug.LogError($"[ModelAIMaterialFiller] 材质AI返回内容无法提取为JSON: {aiResult}");
                    onResult(false, null, "材质AI返回内容无法提取为JSON: " + aiResult);
                    yield break;
                }
                Debug.Log($"[ModelAIMaterialFiller] 材质AI提取到的JSON: {json}");
                onResult(true, json, null);
            }
        }

        /// <summary>
        /// 批量材质分析内部协程
        /// </summary>
        private System.Collections.IEnumerator FillMaterialBatchInternalCoroutine(List<string> items, string apiKey, Action<int, bool, string, string> onResult)
        {
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                var itemName = items[i];
                Debug.Log($"[ModelAIMaterialFiller] Batch(Material) item {idx}: itemName={itemName}");
                bool done = false;
                bool success = false;
                string jsonResult = null;
                string errorMsg = null;
                yield return FillMaterialInternalCoroutine(itemName, apiKey, (succ, json, err) =>
                {
                    success = succ;
                    jsonResult = json;
                    errorMsg = err;
                    done = true;
                });
                while (!done) yield return null;
                onResult?.Invoke(idx, success, jsonResult, errorMsg);
            }
        }

        /// <summary>
        /// AI分析模型摆放方式
        /// </summary>
        /// <param name="itemName">物品名称</param>
        /// <param name="sceneHint">场景描述（可选）</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="onResult">回调，参数为(success, jsonResult, errorMsg)</param>
        public void AnalyzePlacementAsync(string itemName, string sceneHint, string apiKey, Action<bool, string, string> onResult)
        {
            #if UNITY_2019_1_OR_NEWER
            EditorCoroutineUtility.StartCoroutineOwnerless(AnalyzePlacementInternalCoroutine(itemName, sceneHint, apiKey, onResult));
            #else
            EditorApplication.update += EditorCoroutineHelper(AnalyzePlacementInternalCoroutine(itemName, sceneHint, apiKey, onResult));
            #endif
        }

        private string BuildPlacementPrompt(string itemName, string sceneHint)
        {
            // 获取场景名（UnityEditor环境下）
            string sceneName = "";
#if UNITY_EDITOR
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            sceneName = scene.name;
#endif
            // 获取场景中所有可选父物体名和所有模型名
            var root = GameObject.Find("GameObjectRoot");
            List<string> parentNames = new List<string>();
            List<string> modelNames = new List<string>();
            if (root != null)
            {
                foreach (Transform child in root.transform)
                {
                    parentNames.Add(child.name);
                    modelNames.Add(child.name);
                }
            }
            string parentList = string.Join("、", parentNames);
            string modelList = string.Join("、", modelNames);

            string extraRules = $"\n- parent字段必须从以下物体名称中选择：[{parentList}]，不要创造新名称。\n- 当前场景包含的所有模型名称为：[{modelList}]。\n- 你只需分析parent（父物体/依附对象）、group（分组名/功能组）、layout_mode（推荐分布方式：Linear/Grid/Circle）、is_main_model（是否为主要模型，true/false），不要返回具体的position，所有空间布局将由代码自动完成。\n- \"主要模型\"定义：通常为场景核心设备、实验主对象、桌面等。请根据场景名和所有模型名，智能判断当前物体是否为主要模型。\n- 只返回JSON，不要解释。\n- 当前场景名称：{sceneName}。\n";
            string scenePart = string.IsNullOrEmpty(sceneHint) ? "" : $"\n{sceneHint}";
            string prompt =
                $"你是Unity 3D场景的智能助手。根据物品名称\"{itemName}\"{scenePart}，\n"
                + extraRules +
"请智能推荐其在虚拟实验场景中的最佳结构关系。只返回如下JSON字段：\n- parent: 推荐父物体（必须从上面列表中选）\n- group: 推荐分组名（如有，由AI智能分析赋值）\n- layout_mode: 推荐分布方式（Linear/Grid/Circle）\n- is_main_model: 是否为主要模型（true/false）\n";
            return prompt;
        }

        private System.Collections.IEnumerator AnalyzePlacementInternalCoroutine(string itemName, string sceneHint, string apiKey, Action<bool, string, string> onResult)
        {
            var prompt = BuildPlacementPrompt(itemName, sceneHint);
            var body = new
            {
                model = "deepseek-chat",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.2,
                max_tokens = 512
            };
            var jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            var request = new UnityEngine.Networking.UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onResult(false, null, request.error);
            }
            else
            {
                var respStr = request.downloadHandler.text;
                var respObj = JObject.Parse(respStr);
                var aiResult = respObj["choices"]?[0]?["message"]?["content"]?.ToString();
                var json = ExtractJson(aiResult);
                if (json == null)
                {
                    onResult(false, null, "AI返回内容无法提取为JSON: " + aiResult);
                    yield break;
                }
                onResult(true, json, null);
            }
        }

#if !UNITY_2019_1_OR_NEWER
        // 兼容旧版Unity的Editor协程辅助
        private Action EditorCoroutineHelper(System.Collections.IEnumerator routine)
        {
            return () =>
            {
                if (!routine.MoveNext())
                {
                    EditorApplication.update -= EditorCoroutineHelper(routine);
                }
            };
        }
#endif
    }
} 