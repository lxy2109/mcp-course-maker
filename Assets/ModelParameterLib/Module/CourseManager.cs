using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using UnityEngine;

namespace ModelParameterLib.Module
{
    /// <summary>
    /// 📚 课程文件夹管理器
    /// 负责课程文件夹的扫描、选择和路径管理
    /// </summary>
    [System.Serializable]
    public class CourseManager
    {
        #region 📚 属性和字段
        
        [Header("📚 课程文件夹设置")]
        public string selectedCourseFolder = "";
        
        [SerializeField]
        public List<string> availableCourseFolders = new List<string>();
        
        [SerializeField]
        public int selectedCourseIndex = 0;
        
        // 路径设置
        public string sourceFolder = ""; // 自动设置为课程/Models
        public string outputFolder = ""; // 自动设置为课程/Prefabs
        
        #endregion
        
        #region 🔍 事件回调
        
        public event System.Action<string> OnCourseChanged;
        public event System.Action OnCourseFoldersUpdated;
        
        #endregion
        
        #region 🎯 核心方法
        
        /// <summary>
        /// 🔍 扫描Assets目录下的课程文件夹
        /// </summary>
        public void ScanForCourseFolders()
        {
            try
            {
                availableCourseFolders.Clear();
                
                // 获取Assets目录
                string assetsPath = Application.dataPath;
                
                if (!Directory.Exists(assetsPath))
                {
                    Debug.LogError("❌ Assets目录不存在");
                    return;
                }
                
                // 获取Assets下的所有目录
                var directories = Directory.GetDirectories(assetsPath, "*", SearchOption.TopDirectoryOnly);
                
                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir);
                    
                    // 跳过系统目录和插件目录
                    if (IsSystemOrPluginDirectory(dirName))
                        continue;
                    
                    // 检查是否为课程文件夹（包含Models或Prefabs子目录）
                    if (IsCourseDirectory(dir))
                    {
                        string relativePath = "Assets/" + dirName;
                        availableCourseFolders.Add(relativePath);
                        Debug.Log($"📁 发现课程文件夹: {relativePath}");
                    }
                }
                
                // 按名称排序
                availableCourseFolders.Sort();
                
                Debug.Log($"🎯 总共发现 {availableCourseFolders.Count} 个课程文件夹");
                
                // 如果当前选择的课程不在列表中，重置选择
                if (selectedCourseIndex >= availableCourseFolders.Count)
                {
                    selectedCourseIndex = 0;
                    selectedCourseFolder = "";
                }
                
                // 触发事件
                OnCourseFoldersUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 扫描课程文件夹失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 📝 设置选中的课程
        /// </summary>
        public void SetSelectedCourse(int index)
        {
            if (index < 0 || index >= availableCourseFolders.Count)
            {
                Debug.LogWarning($"⚠️ 无效的课程索引: {index}");
                return;
            }
            
            selectedCourseIndex = index;
            selectedCourseFolder = availableCourseFolders[index];
            
            // 更新路径
            UpdatePathsForSelectedCourse();
            
            // 触发事件
            OnCourseChanged?.Invoke(selectedCourseFolder);
            
            Debug.Log($"📚 已选择课程: {selectedCourseFolder}");
        }
        
        /// <summary>
        /// 🔄 更新选中课程的路径
        /// </summary>
        public void UpdatePathsForSelectedCourse()
        {
            if (string.IsNullOrEmpty(selectedCourseFolder))
            {
                sourceFolder = "";
                outputFolder = "";
                return;
            }
            // 自动去除selectedCourseFolder中的Models或Prefabs子目录，只保留Assets/{课程名}
            string baseCourseFolder = selectedCourseFolder.Replace("\\", "/");
            // 防御性去除多次/Models或/Prefabs后缀
            while (baseCourseFolder.EndsWith("/Models") || baseCourseFolder.EndsWith("/Prefabs"))
            {
                if (baseCourseFolder.EndsWith("/Models"))
                    baseCourseFolder = baseCourseFolder.Substring(0, baseCourseFolder.Length - "/Models".Length);
                if (baseCourseFolder.EndsWith("/Prefabs"))
                    baseCourseFolder = baseCourseFolder.Substring(0, baseCourseFolder.Length - "/Prefabs".Length);
            }
            selectedCourseFolder = baseCourseFolder;
            sourceFolder = Path.Combine(selectedCourseFolder, "Models").Replace('\\', '/');
            outputFolder = Path.Combine(selectedCourseFolder, "Prefabs").Replace('\\', '/');
            Debug.Log($"[CourseManager] 路径修正: selectedCourseFolder={selectedCourseFolder}, sourceFolder={sourceFolder}, outputFolder={outputFolder}");
        }
        
        /// <summary>
        /// 📊 获取当前选中课程的名称
        /// </summary>
        public string GetSelectedCourseName()
        {
            if (string.IsNullOrEmpty(selectedCourseFolder))
                return "";
            
            return Path.GetFileName(selectedCourseFolder);
        }
        
        /// <summary>
        /// 📋 获取可选课程名称列表
        /// </summary>
        public string[] GetCourseDisplayNames()
        {
            return availableCourseFolders.Select(path => 
                $"📁 {Path.GetFileName(path)}").ToArray();
        }
        
        /// <summary>
        /// ✅ 验证当前课程目录结构
        /// </summary>
        public bool ValidateCurrentCourseStructure()
        {
            if (string.IsNullOrEmpty(selectedCourseFolder))
                return false;
            
            // 检查Models目录
            string modelsPath = Path.Combine(Application.dataPath, 
                selectedCourseFolder.Replace("Assets/", ""), "Models");
            
            if (!Directory.Exists(modelsPath))
            {
                Debug.LogWarning($"⚠️ Models目录不存在: {modelsPath}");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 🏗️ 创建必要的课程目录结构
        /// </summary>
        public void EnsureCourseDirectoryStructure()
        {
            if (string.IsNullOrEmpty(selectedCourseFolder))
                return;
            
            string coursePath = Path.Combine(Application.dataPath, 
                selectedCourseFolder.Replace("Assets/", ""));
            
            // 创建必要的子目录
            string[] requiredDirs = { "Models", "Prefabs", "Textures", "Materials", "Audio" };
            
            foreach (string dirName in requiredDirs)
            {
                string dirPath = Path.Combine(coursePath, dirName);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                    Debug.Log($"📁 已创建目录: {dirPath}");
                }
            }
        }
        
        #endregion
        
        #region 🛠️ 辅助方法
        
        /// <summary>
        /// 🚫 检查是否为系统或插件目录
        /// </summary>
        private bool IsSystemOrPluginDirectory(string dirName)
        {
            var systemDirs = new HashSet<string>
            {
                "Scripts", "Editor", "Plugins", "Settings", "Scenes",
                "TextMesh Pro", "Materials", "Shaders", "Resources",
                "StreamingAssets", "Gizmos", "Standard Assets",
                "AmplifyShaderEditor", "Mirror", "NodeGraphTool",
                "TutorialInfo", "Samples", "DreamOS - Complete OS UI",
                "TriLib", "unitymcp", "ModelParameterLib", "TimeLine","excel-mcp-server",
                // 新增：忽略Timeline相关目录
                "Timeline", "TimelineAsset", "TimelineAssets",
                "Fronts", "Pictures", "Prefabs", "Models", "Textures", "Materials", "Audio"
            };
            // 忽略Timeline相关目录（不区分大小写）
            if (dirName.ToLower().Contains("timeline"))
                return true;
            return systemDirs.Contains(dirName) || dirName.StartsWith(".");
        }
        
        /// <summary>
        /// 📁 检查是否为课程目录
        /// </summary>
        private bool IsCourseDirectory(string dirPath)
        {
            // 检查是否包含Models或Prefabs子目录
            string modelsPath = Path.Combine(dirPath, "Models");
            string prefabsPath = Path.Combine(dirPath, "Prefabs");
            
            bool hasModels = Directory.Exists(modelsPath);
            bool hasPrefabs = Directory.Exists(prefabsPath);
            
            // 至少要有Models目录，或者有其他课程相关的标识
            if (hasModels || hasPrefabs)
                return true;
            
            // 检查是否包含课程相关的文件
            var files = Directory.GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file).ToLower();
                if (fileName.Contains("课程") || fileName.Contains("实验") || 
                    fileName.EndsWith(".unity") || fileName.EndsWith(".asset"))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
    }
} 