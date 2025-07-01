using System;

namespace ModelParameterLib.Data
{
    /// <summary>
    /// 📊 转换统计数据类
    /// 记录转换过程中的各种统计信息
    /// </summary>
    [System.Serializable]
    public class ConversionStats
    {
        public int processedFiles = 0;          // 已处理文件数
        public int successfulConversions = 0;   // 成功转换数
        public int failedConversions = 0;       // 失败转换数
        public float totalProcessingTime = 0f;  // 总处理时间
        public DateTime startTime;              // 开始时间
        public DateTime endTime;                // 结束时间
        
        // 性能统计
        public float averageFileSize = 0f;      // 平均文件大小
        public float largestFileSize = 0f;      // 最大文件大小
        public float smallestFileSize = float.MaxValue; // 最小文件大小
        
        // 错误统计
        public int importErrors = 0;            // 导入错误数
        public int scaleErrors = 0;             // 比例错误数
        public int materialErrors = 0;          // 材质错误数
        public int prefabErrors = 0;            // 预制件创建错误数
        
        /// <summary>
        /// 📊 重置统计数据
        /// </summary>
        public void Reset()
        {
            processedFiles = 0;
            successfulConversions = 0;
            failedConversions = 0;
            totalProcessingTime = 0f;
            
            averageFileSize = 0f;
            largestFileSize = 0f;
            smallestFileSize = float.MaxValue;
            
            importErrors = 0;
            scaleErrors = 0;
            materialErrors = 0;
            prefabErrors = 0;
            
            startTime = DateTime.Now;
        }
        
        /// <summary>
        /// ⏱️ 开始计时
        /// </summary>
        public void StartTiming()
        {
            startTime = DateTime.Now;
        }
        
        /// <summary>
        /// ⏱️ 结束计时
        /// </summary>
        public void EndTiming()
        {
            endTime = DateTime.Now;
            totalProcessingTime = (float)(endTime - startTime).TotalSeconds;
        }
        
        /// <summary>
        /// 📈 更新文件大小统计
        /// </summary>
        public void UpdateFileSizeStats(float fileSize)
        {
            if (fileSize > largestFileSize)
                largestFileSize = fileSize;
            
            if (fileSize < smallestFileSize)
                smallestFileSize = fileSize;
            
            // 计算平均文件大小
            averageFileSize = (averageFileSize * processedFiles + fileSize) / (processedFiles + 1);
        }
        
        /// <summary>
        /// 📊 获取成功率
        /// </summary>
        public float GetSuccessRate()
        {
            if (processedFiles == 0) return 0f;
            return (float)successfulConversions / processedFiles * 100f;
        }
        
        /// <summary>
        /// ⚡ 获取平均处理时间
        /// </summary>
        public float GetAverageProcessingTime()
        {
            if (processedFiles == 0) return 0f;
            return totalProcessingTime / processedFiles;
        }
        
        /// <summary>
        /// 📋 生成统计摘要
        /// </summary>
        public string GetSummary()
        {
            return $"处理: {processedFiles}, 成功: {successfulConversions}, 失败: {failedConversions}, " +
                   $"成功率: {GetSuccessRate():F1}%, 平均时间: {GetAverageProcessingTime():F2}s";
        }
    }
} 