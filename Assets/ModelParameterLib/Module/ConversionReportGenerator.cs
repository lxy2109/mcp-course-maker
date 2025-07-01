using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using ModelParameterLib.Data;

namespace ModelParameterLib.Module
{
    /// <summary>
    /// 转换报告生成器，负责生成和导出批量转换统计报告
    /// </summary>
    public class ConversionReportGenerator
    {
        /// <summary>
        /// 生成并导出转换报告
        /// </summary>
        public void GenerateReport(ConversionStats stats, List<GLBFileInfo> glbFiles, string courseFolderPath)
        {
            string report = BuildReport(stats, glbFiles);
            string path = SaveReportToFile(report, courseFolderPath);
            Debug.Log($"[报告] 转换报告已导出: {path}");
        }

        /// <summary>
        /// 构建报告内容
        /// </summary>
        public string BuildReport(ConversionStats stats, List<GLBFileInfo> glbFiles)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("==== GLB批量转换报告 ====");
            sb.AppendLine($"总文件数: {glbFiles.Count}");
            sb.AppendLine($"总用时: {stats.totalProcessingTime:F2}秒");
            sb.AppendLine("");
            foreach (var file in glbFiles)
            {
                sb.AppendLine($"- {file.fileName} | 大小: {file.fileSize / 1024}KB | Prefab: {(file.hasPrefab ? "是" : "否")}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 保存报告到文件
        /// </summary>
        public string SaveReportToFile(string report, string courseFolderPath)
        {
            // 课程文件夹下的Report目录
            string folder = Path.Combine(courseFolderPath, "Report");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, $"GLB转换报告_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(filePath, report);
            return filePath;
        }
    }
} 