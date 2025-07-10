using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using ModelParameterLib.Data;
using System.Threading;
using System.IO;
using System.Linq;


namespace ModelParameterLib.Module
{
    /// <summary>
    /// GLB文件处理器，负责导入、预制体生成、材质优化等
    /// </summary>
    public class GLBFileProcessor
    {
        public async Task ProcessFiles(List<GLBFileInfo> files, EditorWindow mainWindow, ConversionStats stats, JsonScaleDataLoader jsonLoader)
        {
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                await CreatePrefabFromGLB(file, jsonLoader);
                // 可扩展：更新进度、统计等
            }
        }

        public class GLBProcessOptions
        {
            public bool applyCollider = true;
            public bool applyRigidbody = true;
            public bool applyGlassMaterial = true;
            public bool applyScaleFromLibrary = true;
            public float scaleMultiplier = 50f; // 新增：缩放倍数，默认50
        }

        public async Task<bool> CreatePrefabFromGLB(GLBFileInfo file, JsonScaleDataLoader jsonLoader, GLBProcessOptions options = null, string outputFolder = null)
        {
            Debug.Log($"[转换] 开始处理: {file.fileName}, 路径: {file.relativePath}, fullPath: {file.fullPath}");
            if (options == null) options = new GLBProcessOptions();
            // 1. 导入模型
            var prefabInstance = await ImportModelAndInstantiate(file);
            if (prefabInstance == null)
            {
                Debug.LogError($"[转换失败] 导入模型失败: {file.fileName} ({file.relativePath})");
                return false;
            }
            // 2. 应用JSON配置（带scale*10）
            if (options.applyScaleFromLibrary && file.jsonScaleData != null)
                ApplyJsonConfigToPrefabWithScale10(prefabInstance, file.jsonScaleData);
            // 2.5. 应用scaleMultiplier到Prefab本体
            if (Mathf.Abs(options.scaleMultiplier - 1f) > 0.0001f)
                prefabInstance.transform.localScale *= options.scaleMultiplier;
            // 3. 添加碰撞体和刚体
            if (options.applyCollider || options.applyRigidbody)
                AddColliderAndRigidbodySelective(prefabInstance, options.applyCollider, options.applyRigidbody);
            // 3. 添加 ObjectRegister
            if (prefabInstance.GetComponent<ObjectRegister>() == null)
                prefabInstance.AddComponent<ObjectRegister>();

            // 4. 添加 Outlinable
            if (prefabInstance.GetComponent<EPOOutline.Outlinable>() == null)
            {
                prefabInstance.AddComponent<EPOOutline.Outlinable>();
                var outlinable = prefabInstance.GetComponent<EPOOutline.Outlinable>();
                outlinable.OutlineParameters.BlurShift = 0;
            }

            // 5. 添加 Clickable 组件（不赋值uiPrefab，在场景实例化时赋值）
            if (prefabInstance.GetComponent<Clickable>() == null)
            {
                prefabInstance.AddComponent<Clickable>();
                // 移除uiPrefab赋值，改为在场景实例化时处理
            }

            CreateSubObjects(); 
  
            // 6. 判断是否为玻璃材质
            bool isGlass = false;
            if (options.applyGlassMaterial)
                isGlass = await IsGlassMaterial(file);
            // 7. 赋值玻璃材质
            if (options.applyGlassMaterial && isGlass)
                AssignGlassMaterial(prefabInstance, file.fullPath);
            // 8. 保存预制体
            bool saved = SaveAndCleanupPrefab(prefabInstance, file, outputFolder);
            if (!saved)
            {
                Debug.LogError($"[转换失败] 保存Prefab失败: {file.fileName}");
            }
            return saved;
        }

        private async Task<GameObject> ImportModelAndInstantiate(GLBFileInfo file)
        {
            string modelPath = file.relativePath.Replace('\\', '/');
            string modelName = System.IO.Path.GetFileNameWithoutExtension(file.fileName);
            string prefabPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file.fullPath), modelName + ".prefab").Replace('\\', '/');
            ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.importBlendShapes = true;
                modelImporter.importVisibility = true;
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.preserveHierarchy = true;
                modelImporter.meshCompression = ModelImporterMeshCompression.Off;
                modelImporter.isReadable = false;
                modelImporter.optimizeMeshPolygons = true;
                modelImporter.optimizeMeshVertices = true;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                modelImporter.materialSearch = ModelImporterMaterialSearch.Local;
                modelImporter.animationType = ModelImporterAnimationType.None;
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }
            await Task.Delay(100);
            GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            Debug.Log($"[GLB导入] AssetDatabase.LoadAssetAtPath: {modelPath}, 结果: {(importedModel == null ? "null" : "OK")}");
            if (importedModel == null) return null;
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(importedModel) as GameObject;
            return prefabInstance;
        }

        private void AddColliderAndRigidbodySelective(GameObject go, bool addCollider, bool addRigidbody)
        {
            if (addCollider && go.GetComponent<MeshCollider>() == null)
                go.AddComponent<MeshCollider>();
            if (addRigidbody && go.GetComponent<Rigidbody>() == null)
            {
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }

        private async Task<bool> IsGlassMaterial(GLBFileInfo file)
        {
            // 先根据比例库和文件名判断
            string nameOrCat = (file.jsonScaleData?.Category ?? file.fileNameWithoutExtension ?? "");
            if (nameOrCat.Contains("玻璃") || nameOrCat.Contains("比色皿") || nameOrCat.Contains("烧杯"))
            {
                return true;
            }
            // AI分析（仅用于材质判断）
            var aiFiller = new ModelAIFiller();
            string aiMaterial = null;
            var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
            string apiKey = UnityEditor.EditorPrefs.GetString("DeepSeekApiKey", "");
            aiFiller.FillMaterialAsync(file.fileNameWithoutExtension, apiKey, (succ, json, err) =>
            {
                Debug.Log("[AI返回] json=" + json);
                if (succ && !string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var jobj = Newtonsoft.Json.Linq.JObject.Parse(json);
                        aiMaterial = jobj["material"]?.ToString();
                        if (string.IsNullOrEmpty(aiMaterial))
                            aiMaterial = jobj["材质"]?.ToString();
                        if (string.IsNullOrEmpty(aiMaterial))
                            aiMaterial = jobj["type"]?.ToString();
                        if (string.IsNullOrEmpty(aiMaterial))
                            aiMaterial = jobj["category"]?.ToString();
                    }
                    catch { }
                }
                tcs.SetResult(aiMaterial);
            });
            aiMaterial = await tcs.Task;
            if (!string.IsNullOrEmpty(aiMaterial) && (aiMaterial.Contains("玻璃") || aiMaterial.Contains("比色皿") || aiMaterial.Contains("烧杯")))
            {
                return true;
            }
            return false;
        }

        private void AssignGlassMaterial(GameObject prefabInstance, string glbPath)
        {
            Shader glassShader = Shader.Find("GlassUnlit");
            if (glassShader == null)
            {
                Debug.LogError("[材质错误] 未找到GlassUnlit.shader，请确保已导入并命名一致。");
                return;
            }

            // 查找GLB子资产图片
            Texture2D mainTex = null;
            List<Texture2D> textures = null;
            if (!string.IsNullOrEmpty(glbPath) && glbPath.StartsWith("Assets"))
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
                textures = allAssets.OfType<Texture2D>().ToList();
                if (textures.Count > 0)
                    mainTex = textures.OrderByDescending(t => t.width * t.height).First();
            }
            if (mainTex == null) mainTex = Texture2D.whiteTexture;

            // 获取课程文件夹路径（假设glbPath在Assets/课程名/Models/xxx.glb）
            string courseDir = "Assets";
            if (!string.IsNullOrEmpty(glbPath) && glbPath.StartsWith("Assets"))
            {
                var dir = System.IO.Path.GetDirectoryName(glbPath).Replace('\\', '/');
                // 向上查找直到遇到"Models"，其上一级为课程文件夹
                var parts = dir.Split('/');
                int modelsIdx = System.Array.FindLastIndex(parts, p => p == "Models");
                if (modelsIdx > 0)
                {
                    courseDir = string.Join("/", parts.Take(modelsIdx));
                }
            }
            string matFolder = courseDir + "/Materials";
            if (!System.IO.Directory.Exists(matFolder))
            {
                System.IO.Directory.CreateDirectory(matFolder);
                AssetDatabase.Refresh();
            }
            // 材质保存路径
            string matPath = $"{matFolder}/{prefabInstance.name}_Glass.mat";
            Material glassMatAsset = null;
            if (System.IO.File.Exists(matPath))
            {
                glassMatAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
            if (glassMatAsset == null)
            {
                glassMatAsset = new Material(glassShader);
                // 只设置 _Texture 属性，其他属性保持默认
                if (glassMatAsset.HasProperty("_Texture"))
                    glassMatAsset.SetTexture("_Texture", mainTex);
                // 设置默认玻璃参数
                if (glassMatAsset.HasProperty("_matcapStrength"))
                    glassMatAsset.SetFloat("_matcapStrength", 0.509f);
                if (glassMatAsset.HasProperty("_refractStrength"))
                    glassMatAsset.SetFloat("_refractStrength", 0.038f);
                if (glassMatAsset.HasProperty("_fresnelMin"))
                    glassMatAsset.SetFloat("_fresnelMin", 0.436f);
                if (glassMatAsset.HasProperty("_fresnelMax"))
                    glassMatAsset.SetFloat("_fresnelMax", 0.654f);
                if (glassMatAsset.HasProperty("_fresnelVal"))
                    glassMatAsset.SetFloat("_fresnelVal", 0.393f);
                if (glassMatAsset.HasProperty("_mainColor"))
                    glassMatAsset.SetColor("_mainColor", Color.white);
                if (glassMatAsset.HasProperty("_ChromaticOffset"))
                    glassMatAsset.SetFloat("_ChromaticOffset", 0.005f);
                AssetDatabase.CreateAsset(glassMatAsset, matPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[玻璃材质] 创建并保存: {matPath}");
            }
            else
            {
                // 只设置 _Texture 属性，其他属性保持默认
                if (glassMatAsset.HasProperty("_Texture"))
                    glassMatAsset.SetTexture("_Texture", mainTex);
                EditorUtility.SetDirty(glassMatAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[玻璃材质] 复用已存在: {matPath}");
            }
            // 赋值给所有Renderer
            foreach (var mr in prefabInstance.GetComponentsInChildren<MeshRenderer>(true))
            {
                var origMats = mr.sharedMaterials;
                var newMats = new Material[origMats.Length];
                for (int i = 0; i < origMats.Length; i++)
                {
                    newMats[i] = glassMatAsset;
                }
                mr.sharedMaterials = newMats;
                EditorUtility.SetDirty(mr);
            }
            // 标记材质和Prefab为Dirty，确保保存
            EditorUtility.SetDirty(prefabInstance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstance);
        }

        private bool SaveAndCleanupPrefab(GameObject prefabInstance, GLBFileInfo file, string outputFolder = null)
        {
            string modelName = System.IO.Path.GetFileNameWithoutExtension(file.fileName);
            string prefabPath;
            // 只要outputFolder是有效Unity资源路径（以Assets/开头），就用它
            if (!string.IsNullOrEmpty(outputFolder) && outputFolder.Replace('\\', '/').StartsWith("Assets/"))
            {
                prefabPath = System.IO.Path.Combine(outputFolder, modelName + ".prefab");
            }
            else
            {
                prefabPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file.fullPath), modelName + ".prefab");
            }
            prefabPath = prefabPath.Replace('\\', '/');
            Debug.Log($"[保存Prefab] 路径: {prefabPath}");
            string prefabDirectory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(prefabDirectory))
                System.IO.Directory.CreateDirectory(prefabDirectory);
            GameObject savedPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            // --- 自动修复：保存后强制刷新AssetDatabase ---
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            UnityEngine.Object.DestroyImmediate(prefabInstance);
            return savedPrefab != null;
        }

        public void ApplyJsonConfigToPrefab(GameObject prefab, ScaleData scaleData)
        {
            if (scaleData == null) return;
            if (scaleData.Scale != Vector3.zero)
                prefab.transform.localScale = scaleData.Scale;
        }

        public void ApplyMaterialOptimizations(GameObject prefab, GLBFileInfo file)
        {
            // AI分析是否为玻璃材质
            bool isGlass = false;
            string name = file.fileName + " " + (file.jsonScaleData?.Category ?? "");
            if (name.Contains("玻璃") || name.Contains("比色皿") || name.Contains("烧杯"))
                isGlass = true;
            if (isGlass)
            {
                // 查找Glass_Material.mat
                string[] guids = AssetDatabase.FindAssets("Glass_Material t:Material");
                if (guids.Length > 0)
                {
                    string matPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var glassMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (glassMat != null)
                    {
                        foreach (var mr in prefab.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            mr.sharedMaterial = glassMat;
                        }
                    }
                }
            }
        }

        private void ApplyJsonConfigToPrefabWithScale10(GameObject prefab, ScaleData scaleData)
        {
            if (scaleData == null) return;
            // 1. 统计模型包围盒最大边
            Bounds bounds = new Bounds();
            bool first = true;
            foreach (var mr in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (first) { bounds = mr.bounds; first = false; }
                else { bounds.Encapsulate(mr.bounds); }
            }
            float modelMaxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            // 2. 直接用比例库scale字段（最大边，米）
            float targetMaxSize = 1.0f; // 默认1米
            if (scaleData.ScaleValue.HasValue && scaleData.ScaleValue.Value > 0.0001f)
            {
                targetMaxSize = scaleData.ScaleValue.Value;
            }
            else if (scaleData.RealWorldSizeDict != null && scaleData.RealWorldSizeDict.Count > 0)
            {
                float l = scaleData.RealWorldSizeDict.TryGetValue("length", out var v1) ? v1 : 0f;
                float w = scaleData.RealWorldSizeDict.TryGetValue("width", out var v2) ? v2 : 0f;
                float h = scaleData.RealWorldSizeDict.TryGetValue("height", out var v3) ? v3 : 0f;
                float d = scaleData.RealWorldSizeDict.TryGetValue("diameter", out var v4) ? v4 : 0f;
                targetMaxSize = Mathf.Max(l, w, h, d) / 100f;
            }
            // 3. 计算等比缩放因子
            if (modelMaxSize > 0.0001f)
            {
                float scale = targetMaxSize / modelMaxSize;
                prefab.transform.localScale = Vector3.one * scale;
            }
        }

         public static void CreateSubObjects()
        {
            var objdic = AllUnityEvent.GetInstanceInEditor().objectpartobj;
            if (objdic == null || objdic.Count == 0)
            {
                Debug.LogWarning("objectpartobj字典为空");
                return;
            }

            foreach (var item in objdic)
            {
                var parobj = GameObjectPool.GetInstanceInEditor().allNeedObject;
                GameObject parentObj = parobj.ContainsKey(item.Key) ? parobj[item.Key] : null;
                if (parentObj == null) continue;

                string[] childNames = System.Text.RegularExpressions.Regex.Split(item.Value, "[,，]");
                foreach (string childName in childNames)
                {
                    string trimmedName = childName.Trim();
                    if (string.IsNullOrEmpty(trimmedName)) continue;
                    if (parentObj.transform.Find(trimmedName) != null) continue;

                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = trimmedName;
                    cube.transform.SetParent(parentObj.transform);
                    cube.transform.localPosition = Vector3.zero;
                    cube.transform.localRotation = Quaternion.identity;
                    cube.transform.localScale = new Vector3(.02f, .02f, .02f);
                }
            }
        }
    }
} 