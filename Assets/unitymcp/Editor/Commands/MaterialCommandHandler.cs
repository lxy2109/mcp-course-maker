using UnityEngine;                       // 导入Unity引擎核心功能命名空间
using Newtonsoft.Json.Linq;              // 导入JSON处理库命名空间
using UnityEngine.Rendering.Universal;   // 导入通用渲染管线命名空间
using UnityEngine.Rendering;             // 导入Unity渲染功能命名空间
using UnityEditor;                       // 导入Unity编辑器功能命名空间
using System.IO;                         // 导入文件和目录操作命名空间

namespace UnityMCP.Editor.Commands
{
    /// <summary>
    /// 处理材质相关命令
    /// </summary>
    public static class MaterialCommandHandler
    {
        /// <summary>
        /// 设置或修改对象上的材质
        /// </summary>
        public static object SetMaterial(JObject @params)
        {
            string objectName = (string)@params["object_name"] ?? throw new System.Exception("Parameter 'object_name' is required.");  // 获取对象名称，如果未提供则抛出异常
            var obj = GameObject.Find(objectName) ?? throw new System.Exception($"Object '{objectName}' not found.");                 // 查找指定名称的游戏对象，如果找不到则抛出异常
            var renderer = obj.GetComponent<Renderer>() ?? throw new System.Exception($"Object '{objectName}' has no renderer.");     // 获取对象上的渲染器组件，如果不存在则抛出异常

            // 检查是否使用了通用渲染管线(URP)
            bool isURP = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset;  // 通过检查当前渲染管线类型判断是否使用URP

            Material material = null;                                     // 声明材质变量并初始化为null
            string materialName = (string)@params["material_name"];       // 从参数中获取材质名称
            bool createIfMissing = (bool)(@params["create_if_missing"] ?? true);  // 获取材质不存在时是否创建的标志，默认为true
            string materialPath = null;                                   // 声明材质资源路径变量并初始化为null

            // 如果指定了材质名称，尝试查找或创建它
            if (!string.IsNullOrEmpty(materialName))                      // 检查材质名称是否有效
            {
                // 确保Materials文件夹存在
                const string materialsFolder = "Assets/Materials";         // 定义材质文件夹路径常量
                if (!Directory.Exists(materialsFolder))                    // 检查材质文件夹是否存在
                {
                    Directory.CreateDirectory(materialsFolder);            // 如果文件夹不存在，创建它
                }

                materialPath = $"{materialsFolder}/{materialName}.mat";    // 构建材质资产完整路径
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);  // 尝试从指定路径加载材质资产

                if (material == null && createIfMissing)                   // 如果材质不存在且设置了缺失时创建
                {
                    // 创建具有适当着色器的新材质
                    material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));  // 根据渲染管线类型选择适当的着色器创建材质
                    material.name = materialName;                          // 设置材质名称

                    // 保存材质资产
                    AssetDatabase.CreateAsset(material, materialPath);     // 将材质保存为项目资产
                    AssetDatabase.SaveAssets();                            // 保存所有未保存的资产更改
                }
                else if (material == null)                                 // 如果材质不存在且不允许创建
                {
                    throw new System.Exception($"Material '{materialName}' not found and create_if_missing is false.");  // 抛出异常说明情况
                }
            }
            else  // 如果未指定材质名称
            {
                // 如果未指定名称，创建临时材质
                material = new Material(isURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));  // 根据渲染管线类型创建临时材质
            }

            // 如果指定了颜色，应用颜色
            if (@params.ContainsKey("color"))                              // 检查参数中是否包含颜色信息
            {
                var colorArray = (JArray)@params["color"];                 // 获取颜色数组
                if (colorArray.Count < 3 || colorArray.Count > 4)          // 验证颜色数组的长度
                    throw new System.Exception("Color must be an array of 3 (RGB) or 4 (RGBA) floats.");  // 如果长度不正确，抛出异常

                Color color = new(                                         // 创建新的颜色对象
                    (float)colorArray[0],                                  // R分量
                    (float)colorArray[1],                                  // G分量
                    (float)colorArray[2],                                  // B分量
                    colorArray.Count > 3 ? (float)colorArray[3] : 1.0f     // A分量，如未提供则默认为1.0f
                );
                material.color = color;                                    // 设置材质的颜色

                // 如果这是已保存的材质，确保保存颜色更改
                if (!string.IsNullOrEmpty(materialPath))                   // 检查材质是否已经保存为资产
                {
                    EditorUtility.SetDirty(material);                      // 标记材质为已修改
                    AssetDatabase.SaveAssets();                            // 保存所有未保存的资产更改
                }
            }

            // 将材质应用到渲染器
            renderer.material = material;                                  // 设置渲染器的材质为我们处理的材质

            return new { material_name = material.name, path = materialPath };  // 返回包含材质名称和路径的匿名对象作为结果
        }
    }
}