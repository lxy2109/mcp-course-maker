using System;
using UnityEngine;
using System.Collections.Generic;

namespace ModelParameterLib.Data
{
    /// <summary>
    /// 📁 GLB文件信息数据结构
    /// 存储单个GLB文件的详细信息
    /// </summary>
    [System.Serializable]
    public class GLBFileInfo
    {
        #region 📁 基本信息
        
        [SerializeField]
        public string fileName = "";
        
        [SerializeField]
        public string fullPath = "";
        
        [SerializeField]
        public string relativePath = "";
        
        [SerializeField]
        public long fileSize = 0;
        
        [SerializeField]
        public DateTime lastModified = DateTime.MinValue;
        
        [SerializeField]
        public string courseName = "";
        
        [SerializeField]
        public bool selected = true;
        
        [SerializeField]
        public List<string> tags = new List<string>();
        
        [SerializeField]
        public bool hasJsonData = false;
        
        [SerializeField]
        public ScaleData jsonScaleData = null;
        
        [SerializeField]
        public string jsonPath = "";
        
        #endregion
        
        #region 🔍 状态信息
        
        [SerializeField]
        public bool hasPrefab = false;
        
        [SerializeField]
        public bool hasConfig = false;
        
        [SerializeField]
        public string configPath = "";
        
        [SerializeField]
        public ProcessingStatus status = ProcessingStatus.Pending;
        
        [SerializeField]
        public string lastError = "";
        
        [SerializeField]
        public float processingProgress = 0f;
        
        #endregion
        
        #region 📊 计算属性
        
        /// <summary>
        /// 文件大小（MB）
        /// </summary>
        public float sizeMB => fileSize / (1024.0f * 1024.0f);
        
        /// <summary>
        /// 文件名（不含扩展名）
        /// </summary>
        public string fileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(fileName);
        
        /// <summary>
        /// 目录名
        /// </summary>
        public string directoryName => System.IO.Path.GetDirectoryName(relativePath);
        
        /// <summary>
        /// 是否为新文件（24小时内修改）
        /// </summary>
        public bool isNewFile => lastModified > DateTime.Now.AddDays(-1);
        
        /// <summary>
        /// 是否为大文件（超过50MB）
        /// </summary>
        public bool isLargeFile => sizeMB > 50f;
        
        /// <summary>
        /// 状态描述
        /// </summary>
        public string statusDescription
        {
            get
            {
                switch (status)
                {
                    case ProcessingStatus.Pending:
                        return "等待处理";
                    case ProcessingStatus.Processing:
                        return $"处理中 ({processingProgress:P0})";
                    case ProcessingStatus.Completed:
                        return "已完成";
                    case ProcessingStatus.Failed:
                        return "处理失败";
                    case ProcessingStatus.Skipped:
                        return "已跳过";
                    default:
                        return "未知状态";
                }
            }
        }
        
        /// <summary>
        /// 状态颜色
        /// </summary>
        public Color statusColor
        {
            get
            {
                switch (status)
                {
                    case ProcessingStatus.Pending:
                        return Color.gray;
                    case ProcessingStatus.Processing:
                        return Color.yellow;
                    case ProcessingStatus.Completed:
                        return Color.green;
                    case ProcessingStatus.Failed:
                        return Color.red;
                    case ProcessingStatus.Skipped:
                        return Color.cyan;
                    default:
                        return Color.white;
                }
            }
        }
        
        /// <summary>
        /// 文件字节数（兼容fileSizeBytes调用）
        /// </summary>
        public long fileSizeBytes => fileSize;
        
        #endregion
        
        #region 🛠️ 方法
        
        /// <summary>
        /// 🔄 重置处理状态
        /// </summary>
        public void ResetProcessingStatus()
        {
            status = ProcessingStatus.Pending;
            processingProgress = 0f;
            lastError = "";
        }
        
        /// <summary>
        /// ✅ 设置处理完成
        /// </summary>
        public void SetCompleted()
        {
            status = ProcessingStatus.Completed;
            processingProgress = 1f;
            lastError = "";
        }
        
        /// <summary>
        /// ❌ 设置处理失败
        /// </summary>
        public void SetFailed(string error)
        {
            status = ProcessingStatus.Failed;
            lastError = error;
            processingProgress = 0f;
        }
        
        /// <summary>
        /// ⏩ 设置跳过
        /// </summary>
        public void SetSkipped(string reason = "")
        {
            status = ProcessingStatus.Skipped;
            lastError = reason;
            processingProgress = 0f;
        }
        
        /// <summary>
        /// 🔄 设置处理中
        /// </summary>
        public void SetProcessing(float progress = 0f)
        {
            status = ProcessingStatus.Processing;
            processingProgress = Mathf.Clamp01(progress);
            lastError = "";
        }
        
        /// <summary>
        /// 📊 获取详细信息字符串
        /// </summary>
        public string GetDetailedInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"文件名: {fileName}");
            info.AppendLine($"路径: {relativePath}");
            info.AppendLine($"大小: {sizeMB:F2} MB");
            info.AppendLine($"修改时间: {lastModified:yyyy-MM-dd HH:mm:ss}");
            info.AppendLine($"状态: {statusDescription}");
            
            if (hasConfig)
            {
                info.AppendLine($"配置文件: {configPath}");
            }
            
            if (hasPrefab)
            {
                info.AppendLine("✅ 已有预制件");
            }
            
            if (!string.IsNullOrEmpty(lastError))
            {
                info.AppendLine($"错误: {lastError}");
            }
            
            return info.ToString();
        }
        
        /// <summary>
        /// 🔍 检查文件是否存在
        /// </summary>
        public bool FileExists()
        {
            return System.IO.File.Exists(fullPath);
        }
        
        /// <summary>
        /// 📄 获取预期的预制件路径
        /// </summary>
        public string GetExpectedPrefabPath()
        {
            if (string.IsNullOrEmpty(relativePath))
                return "";
            
            try
            {
                string prefabPath = relativePath.Replace("Models/", "Prefabs/").Replace("Models\\", "Prefabs\\");
                return System.IO.Path.ChangeExtension(prefabPath, ".prefab");
            }
            catch
            {
                return $"Assets/Prefabs/{fileNameWithoutExtension}.prefab";
            }
        }
        
        /// <summary>
        /// 🎯 获取Unity资源路径
        /// </summary>
        public string GetUnityAssetPath()
        {
            if (string.IsNullOrEmpty(relativePath))
                return "";
            
            return relativePath.Replace('\\', '/');
        }
        
        /// <summary>
        /// 🏷️ 从文件名推断物体类型
        /// </summary>
        public string InferObjectType()
        {
            string name = fileNameWithoutExtension.ToLower();
            
            // 化学实验设备
            if (name.Contains("光度计") || name.Contains("spectr"))
                return "分光光度计";
            if (name.Contains("烧杯") || name.Contains("beaker"))
                return "烧杯";
            if (name.Contains("试管") || name.Contains("tube"))
                return "试管";
            if (name.Contains("比色皿") || name.Contains("cuvette"))
                return "比色皿";
            if (name.Contains("移液管") || name.Contains("pipet"))
                return "移液管";
            
            // 物理实验设备
            if (name.Contains("天平") || name.Contains("balance"))
                return "天平";
            if (name.Contains("温度计") || name.Contains("thermometer"))
                return "温度计";
            if (name.Contains("显微镜") || name.Contains("microscope"))
                return "显微镜";
            
            // 通用实验设备
            if (name.Contains("桌") || name.Contains("table") || name.Contains("desk"))
                return "实验桌";
            if (name.Contains("架") || name.Contains("rack") || name.Contains("stand"))
                return "支架";
            
            return "实验器材";
        }
        
        #endregion
        
        #region 🔄 重载方法
        
        public override string ToString()
        {
            return $"{fileName} ({sizeMB:F2}MB) - {statusDescription}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is GLBFileInfo other)
            {
                return fullPath.Equals(other.fullPath, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return fullPath?.GetHashCode() ?? 0;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 📊 处理状态枚举
    /// </summary>
    public enum ProcessingStatus
    {
        Pending,     // 等待处理
        Processing,  // 处理中
        Completed,   // 已完成
        Failed,      // 处理失败
        Skipped      // 已跳过
    }
} 