using UnityEngine;
using System;

namespace ModelParameterLib
{
    /// <summary>
        /// 🏷️ 模型信息组件 - 存储GLB模型的元数据
        /// </summary>
        [System.Serializable]
        public class ModelInfo : MonoBehaviour
        {
            [Header("📐 尺寸信息")]
            public Vector3 realWorldSize = Vector3.zero;
            public Vector3 originalScale = Vector3.one;
            
            [Header("📝 描述信息")]
            public string category = "";
            public string description = "";
            public float confidenceLevel = 1.0f;
            
            [Header("📊 数据源")]
            public string scaleSource = "";
            public string realWorldSizeSource = "";
            public string jsonFilePath = "";
            
            [Header("⏰ 时间信息")]
            public string createdTime = "";
            public string lastModified = "";
            
            private void Awake()
            {
                if (string.IsNullOrEmpty(createdTime))
                {
                    createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            
            /// <summary>
            /// 获取模型信息摘要
            /// </summary>
            public string GetSummary()
            {
                return $"类别: {category}\n" +
                       $"描述: {description}\n" +
                       $"真实尺寸: {realWorldSize}\n" +
                       $"缩放比例: {transform.localScale}\n" +
                       $"置信度: {confidenceLevel:P0}\n" +
                       $"数据源: {scaleSource}";
            }
            
            /// <summary>
            /// 重置到原始缩放
            /// </summary>
            public void ResetToOriginalScale()
            {
                if (originalScale != Vector3.zero)
                {
                    transform.localScale = originalScale;
                }
            }
            
            /// <summary>
            /// 应用推荐缩放
            /// </summary>
            public void ApplyRecommendedScale(Vector3 recommendedScale)
            {
                originalScale = transform.localScale;
                transform.localScale = recommendedScale;
                lastModified = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
} 