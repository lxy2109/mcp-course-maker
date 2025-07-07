using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理天空盒相关命令
    /// </summary>
    public static class SkyboxCommandHandler
    {
        /// <summary>
        /// 设置当前场景的天空盒材质
        /// </summary>
        /// <param name="@params">包含材质路径的参数</param>
        /// <returns>操作结果</returns>
        public static object SetSkybox(JObject @params)
        {
            try
            {
                string materialPath = (string)@params["material_path"];
                if (string.IsNullOrEmpty(materialPath))
                    return new { success = false, error = "Material path cannot be empty" };

                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                    return new { success = false, error = $"Material not found at: {materialPath}" };

                RenderSettings.skybox = material;
                // 通知编辑器刷新
                EditorUtility.SetDirty(RenderSettings.skybox);
                return new { success = true, message = $"Skybox set to material: {material.name}" };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = $"Failed to set skybox: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// 清除当前场景的天空盒（设为null）
        /// </summary>
        /// <returns>操作结果</returns>
        public static object ClearSkybox(JObject _)
        {
            try
            {
                RenderSettings.skybox = null;
                return new { success = true, message = "Skybox cleared (set to null)" };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = $"Failed to clear skybox: {e.Message}", stackTrace = e.StackTrace };
            }
        }

        /// <summary>
        /// 由HDR/EXR图片创建天空盒材质球
        /// </summary>
        /// <param name="@params">图片路径、输出材质路径、天空盒类型</param>
        /// <returns>操作结果</returns>
        public static object CreateSkyboxMaterial(JObject @params)
        {
            try
            {
                string imagePath = (string)@params["image_path"];
                string materialPath = (string)@params["material_path"];
                string skyboxType = (string)@params["skybox_type"] ?? "Panoramic";
                if (string.IsNullOrEmpty(imagePath) || string.IsNullOrEmpty(materialPath))
                    return new { success = false, error = "image_path和material_path不能为空" };

                // 加载贴图，支持hdr/exr/jpg/png
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(imagePath);
                if (tex == null)
                    return new { success = false, error = $"未找到贴图: {imagePath}" };

                // 判断图片格式
                string ext = System.IO.Path.GetExtension(imagePath).ToLower();
                bool isLDR = ext == ".jpg" || ext == ".jpeg" || ext == ".png";
                bool isHDR = ext == ".hdr" || ext == ".exr";

                // 选择shader
                string shaderName = skyboxType switch
                {
                    "Panoramic" => "Skybox/Panoramic",
                    "6 Sided" => "Skybox/6 Sided",
                    "Cubemap" => "Skybox/Cubemap",
                    _ => "Skybox/Panoramic"
                };
                var shader = Shader.Find(shaderName);
                if (shader == null)
                    return new { success = false, error = $"未找到shader: {shaderName}" };

                // 创建材质
                var mat = new Material(shader);
                mat.name = System.IO.Path.GetFileNameWithoutExtension(materialPath);
                // 赋值贴图属性
                if (shaderName == "Skybox/Panoramic")
                    mat.SetTexture("_MainTex", tex);
                else if (shaderName == "Skybox/Cubemap")
                    mat.SetTexture("_Tex", tex);
                else if (shaderName == "Skybox/6 Sided")
                    mat.SetTexture("_FrontTex", tex); // 这里只赋一面，用户需后续补全

                // 保存材质
                AssetDatabase.CreateAsset(mat, materialPath);
                AssetDatabase.SaveAssets();
                return new { success = true, message = $"天空盒材质已创建: {materialPath}", material = materialPath };
            }
            catch (System.Exception e)
            {
                return new { success = false, error = $"创建天空盒材质失败: {e.Message}", stackTrace = e.StackTrace };
            }
        }
    }
} 