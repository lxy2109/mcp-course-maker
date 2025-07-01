using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ModelParameterLib.Data;

namespace ModelParameterLib.Module
{
    /// <summary>
    /// 课程文件夹和GLB文件扫描管理器
    /// </summary>
    public class GLBFileScanner
    {
        public List<string> ScanForCourseFolders()
        {
            var result = new List<string>();
            try
            {
                string[] allDirectories = Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories);
                foreach (string directory in allDirectories)
                {
                    string dirName = Path.GetFileName(directory).ToLower();
                    string[] excludeKeywords = {
                        "samples", "plugins", "editor", "resources", "streamingassets",
                        "gizmos", "webgl", "android", "ios", "materials", "shader",
                        "scripts", "textures", "models", "prefabs", "sounds", "music",
                        "animations", "fonts", "settings", "trilib", "amplify", "mirror",
                        "dreamos", "textmesh", "tutorial", ".cursor"
                    };
                    bool shouldExclude = false;
                    foreach (var keyword in excludeKeywords)
                    {
                        if (dirName.Contains(keyword)) { shouldExclude = true; break; }
                    }
                    if (shouldExclude) continue;
                    string[] courseKeywords = {
                        "course", "课程", "lesson", "实验", "experiment",
                        "lab", "demo", "示例", "测试"
                    };
                    bool isCourseFolder = false;
                    foreach (var keyword in courseKeywords)
                    {
                        if (dirName.Contains(keyword)) { isCourseFolder = true; break; }
                    }
                    if (isCourseFolder)
                    {
                        string modelsPath = Path.Combine(directory, "Models");
                        if (Directory.Exists(modelsPath))
                        {
                            result.Add(directory);
                        }
                    }
                }
                result.Sort();
            }
            catch { }
            return result;
        }

        public (string sourceFolder, string outputFolder) GetCourseModelAndPrefabFolders(string courseFolder)
        {
            if (string.IsNullOrEmpty(courseFolder)) return ("", "");
            string source = Path.Combine(courseFolder, "Models");
            string output = Path.Combine(courseFolder, "Prefabs");
            return (source, output);
        }

        public List<GLBFileInfo> ScanForGLBFiles(string sourceFolder, string outputFolder)
        {
            var glbFiles = new List<GLBFileInfo>();
            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder)) return glbFiles;
            string[] glbFilePaths = Directory.GetFiles(sourceFolder, "*.glb", SearchOption.AllDirectories);
            foreach (string filePath in glbFilePaths)
            {
                var fileInfo = new GLBFileInfo();
                fileInfo.fileName = Path.GetFileName(filePath);
                fileInfo.fullPath = filePath;
                fileInfo.relativePath = filePath;
                var fileInfoSystem = new FileInfo(filePath);
                fileInfo.fileSize = fileInfoSystem.Length;
                fileInfo.lastModified = fileInfoSystem.LastWriteTime;
                string configPath = Path.ChangeExtension(filePath, ".json");
                fileInfo.hasConfig = File.Exists(configPath);
                fileInfo.configPath = configPath;
                string prefabPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(fileInfo.fileName) + ".prefab");
                prefabPath = prefabPath.Replace("\\", "/");
                fileInfo.hasPrefab = File.Exists(prefabPath);
                glbFiles.Add(fileInfo);
            }
            return glbFiles;
        }
    }
} 