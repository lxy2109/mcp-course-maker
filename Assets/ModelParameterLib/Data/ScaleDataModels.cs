using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModelParameterLib.Data
{
    /// <summary>
    /// 📐 比例数据类
    /// 存储模型的比例和尺寸信息
    /// </summary>
    [System.Serializable]
    public class ScaleData
    {
        public Vector3 Scale = Vector3.one;              // 标准化缩放比例
        public Vector3 RealWorldSize = Vector3.zero;     // 标准化长宽高（米）
        public Dictionary<string, float> RealWorldSizeDict = new Dictionary<string, float>(); // 原始尺寸字段，如length/width/height/diameter等
        public string Unit = "cm"; // 尺寸单位，默认cm
        public float? Volume = null; // 体积
        public float? ScaleValue = null; // scale字段
        public string Category = ""; // 专业类别（如"理化实验器材"）
    }

    /// <summary>
    /// 💾 智能比例数据融合系统 - 数据结构定义
    /// </summary>
    [System.Serializable]
    public class ScaleDataSource
    {
        public string Name;              // 数据源名称
        public string Path;              // 数据源路径
        public int Priority;             // 优先级（0-100，数值越高优先级越高）
        public ScaleData Data;           // 比例数据
        public bool IsAvailable;         // 是否可用
        public string Description;       // 数据源描述
        public System.DateTime LastModified; // 最后修改时间
    }

    /// <summary>
    /// 📋 JSON解析结果类
    /// </summary>
    [System.Serializable]
    public class JsonParseResult<T>
    {
        public bool Success;                    // 解析是否成功
        public T Data;                         // 解析结果数据
        public string ErrorMessage;            // 错误消息
        public string SourcePath;             // 源文件路径
        public string FormatType;              // 检测到的格式类型
        public DateTime ParseTime;            // 解析时间
        public float ParseTimeMs;              // 解析耗时（毫秒）
        public bool FromCache;                 // 是否来自缓存
        public List<string> Warnings = new List<string>(); // 警告信息
        
        public JsonParseResult()
        {
            Success = false;
            Data = default(T);
            ErrorMessage = "";
            SourcePath = "";
            FormatType = "";
            ParseTime = DateTime.Now;
            ParseTimeMs = 0f;
            FromCache = false;
        }
    }

    /// <summary>
    /// 🗂️ 缓存条目类
    /// </summary>
    [System.Serializable]
    public class CacheEntry<T>
    {
        public T Value;                  // 缓存的值
        public DateTime CacheTime;      // 缓存时间
        public string FilePath;         // 文件路径
        public long FileSize;           // 文件大小
        public DateTime FileModifyTime; // 文件修改时间
        public string FileHash;         // 文件哈希值
        public float ExpirationHours = 1.0f; // 缓存过期时间（小时）
        
        // 默认构造函数
        public CacheEntry()
        {
            Value = default(T);
            FilePath = "";
            CacheTime = DateTime.Now;
            FileSize = 0;
            FileModifyTime = DateTime.Now;
            FileHash = "";
        }
        
        public CacheEntry(T value, string filePath)
        {
            Value = value;
            FilePath = filePath;
            CacheTime = DateTime.Now;
            
            if (System.IO.File.Exists(filePath))
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                FileSize = fileInfo.Length;
                FileModifyTime = fileInfo.LastWriteTime;
                // 简单的文件哈希计算
                FileHash = $"{FileSize}_{FileModifyTime.Ticks}";
            }
        }
        
        /// <summary>
        /// 检查缓存是否仍然有效
        /// </summary>
        public bool IsValid()
        {
            if (!System.IO.File.Exists(FilePath))
                return false;
                
            var fileInfo = new System.IO.FileInfo(FilePath);
            bool sizeMatches = fileInfo.Length == FileSize;
            bool timeMatches = fileInfo.LastWriteTime == FileModifyTime;
            bool notExpired = (DateTime.Now - CacheTime).TotalHours <= ExpirationHours;
            
            return sizeMatches && timeMatches && notExpired;
        }
        
        /// <summary>
        /// 检查缓存是否已过期
        /// </summary>
        public bool IsExpired => (DateTime.Now - CacheTime).TotalHours > ExpirationHours;
    }

    /// <summary>
    /// 💾 JSON解析器配置
    /// </summary>
    [System.Serializable]
    public class JsonParserConfig
    {
        public bool enableCaching = true;           // 启用缓存
        public bool enableValidation = true;        // 启用验证
        public bool enableFallback = true;          // 启用降级处理
        public float cacheExpirationHours = 1.0f;  // 缓存过期时间（小时）
        public int maxRetryAttempts = 3;            // 最大重试次数
    }

    [System.Serializable]
    public class OriginalDimensions
    {
        public float width;
        public float height;
        public float depth;
        public string unit;
    }
} 