# 🎯 ModelParameterLib - Unity GLB模型参数化导入系统

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-lightgrey.svg)](README.md)

ModelParameterLib是一个专为Unity设计的高级3D模型导入和参数化管理系统，特别优化了GLB格式模型的批量处理工作流程。

## 🌟 核心特性

- **🚀 智能批量导入**: 自动扫描、分析和导入GLB模型文件
- **📊 复杂度智能分析**: 基于数据库的模型复杂度自动评估
- **📏 精确比例管理**: 多学科标准比例数据库支持
- **⚙️ 自动配置生成**: 根据模型特征自动生成最优导入参数
- **🎭 预制件自动创建**: 批量生成带组件的Unity预制件
- **🔄 错误恢复机制**: 完善的异常处理和重试机制
- **📈 性能监控**: 实时进度追踪和性能分析
- **🎨 可视化界面**: 直观的Unity Editor集成界面

## 📁 系统架构

```
ModelParameterLib/
├── 📂 Core/                    # 核心功能模块
│   ├── FileScanner.cs         # 文件扫描和分析
│   ├── ConfigGenerator.cs     # 配置生成引擎
│   ├── PrefabGenerator.cs     # 预制件生成器
│   └── BatchImportManager.cs  # 批量导入管理器
├── 📂 Data/                    # 数据结构和模型
│   ├── GLBFileInfo.cs         # GLB文件信息模型
│   ├── ModelConfig.cs         # 模型配置数据结构
│   └── Settings/              # 各种设置类
├── 📂 Database/                # 数据库系统
│   ├── ComplexityDatabase.cs  # 复杂度数据库
│   └── ScaleDatabase.cs       # 比例数据库
├── 📂 Editor/                  # Unity Editor工具
│   ├── ModelParameterLibWindow.cs    # 主界面窗口
│   ├── ModelParameterLibDemo.cs      # 演示工具
│   └── BatchImportWindow.cs          # 批量导入窗口
├── 📂 Utils/                   # 实用工具
│   ├── FileUtils.cs           # 文件操作工具
│   ├── DatabaseUtils.cs       # 数据库工具
│   └── ValidationUtils.cs     # 验证工具
└── 📊 数据库文件
    ├── ModelComplexityDatabase.json  # 模型复杂度数据库
    └── ModelScaleDatabase.json       # 模型比例数据库
```

## 🚀 快速开始

### 1. 安装系统
将`ModelParameterLib`文件夹复制到项目的`Assets`目录中。

### 2. 打开主界面
在Unity菜单栏选择：`Tools > ModelParameterLib > 系统管理界面`

### 3. 配置设置
```csharp
// 基本扫描设置
var scanSettings = new FileScanSettings
{
    targetDirectory = "Assets/Models/GLB/",
    includeSubdirectories = true,
    fileFilter = "*.glb"
};

// 配置生成设置
var configSettings = new ConfigGenerationSettings
{
    useComplexityDatabase = true,
    useScaleDatabase = true,
    autoDetectCategory = true
};
```

### 4. 开始批量导入
点击"开始批量导入"按钮，系统将自动处理所有GLB文件。

## 📊 数据库系统

### 复杂度数据库 (ModelComplexityDatabase.json)
支持多学科领域的模型复杂度智能分析：

```json
{
  "complexity_levels": {
    "S级-超精密": {
      "score_range": [90, 100],
      "target_faces": "15000-25000",
      "examples": ["显微镜", "精密天平", "光谱仪"]
    },
    "A级-精密设备": {
      "score_range": [70, 89],
      "target_faces": "8000-15000",
      "examples": ["分光光度计", "色谱仪", "离心机"]
    }
  },
  "domain_specific_models": {
    "化学实验": {
      "紫外可见光分光光度计": {
        "complexity_score": 78.5,
        "functional_parts": ["显示屏", "操作按钮", "样品室"],
        "processing_priority": "high"
      }
    }
  }
}
```

### 比例数据库 (ModelScaleDatabase.json)
涵盖8大学科领域的标准物品尺寸：

```json
{
  "domains": {
    "化学": {
      "items": {
        "实验桌": {
          "standard_size": {"width": 1.5, "height": 0.85, "depth": 0.75},
          "tolerance": 0.1,
          "importance": "critical"
        },
        "紫外可见光分光光度计": {
          "standard_size": {"width": 0.55, "height": 0.35, "depth": 0.50},
          "tolerance": 0.05,
          "importance": "high"
        }
      }
    }
  }
}
```

## 🎯 使用示例

### 基本文件扫描
```csharp
var fileScanner = new FileScanner();
var settings = new FileScanSettings
{
    targetDirectory = "Assets/Models/",
    includeSubdirectories = true
};

var files = fileScanner.ScanDirectory(settings);
foreach (var file in files)
{
    Debug.Log($"发现GLB文件: {file.fileName} ({file.fileSizeBytes / 1024}KB)");
}
```

### 智能配置生成
```csharp
var configGenerator = new ConfigGenerator();
var settings = new ConfigGenerationSettings
{
    useComplexityDatabase = true,
    useScaleDatabase = true,
    autoDetectCategory = true
};

foreach (var file in files)
{
    var config = configGenerator.GenerateConfig(file, settings);
    Debug.Log($"为 {file.fileName} 生成配置:");
    Debug.Log($"  - 类别: {config.category}");
    Debug.Log($"  - 复杂度等级: {config.complexityLevel}");
    Debug.Log($"  - 目标面数: {config.targetFaceCount}");
    Debug.Log($"  - 建议缩放: {config.suggestedScale}");
}
```

### 批量预制件创建
```csharp
var prefabGenerator = new PrefabGenerator();
var settings = new PrefabGenerationSettings
{
    outputDirectory = "Assets/Prefabs/Generated/",
    addDefaultCollider = true,
    useCache = true
};

var prefabs = new List<GameObject>();
foreach (var file in files)
{
    var prefab = prefabGenerator.GeneratePrefab(file, settings);
    prefabs.Add(prefab);
    Debug.Log($"✅ 创建预制件: {prefab.name}");
}
```

### 完整批量处理工作流程
```csharp
var batchManager = new BatchImportManager();
var settings = new BatchImportSettings
{
    sourceDirectory = "Assets/Models/GLB/",
    outputDirectory = "Assets/Prefabs/Imported/",
    maxConcurrentOperations = 4,
    enableErrorRecovery = true,
    generateReports = true
};

// 设置进度回调
batchManager.OnProgressUpdated += (current, total) =>
{
    Debug.Log($"导入进度: {current}/{total} ({(float)current/total*100:F1}%)");
};

// 设置完成回调
batchManager.OnBatchCompleted += (results) =>
{
    Debug.Log($"批量导入完成! 成功: {results.successCount}, 失败: {results.failureCount}");
};

// 开始批量处理
await batchManager.StartBatchImport(settings);
```

## 🔧 高级配置

### 自定义复杂度分析器
```csharp
public class CustomComplexityAnalyzer : IComplexityAnalyzer
{
    public ComplexityResult AnalyzeModel(GLBFileInfo fileInfo)
    {
        // 自定义复杂度分析逻辑
        var geometricScore = AnalyzeGeometry(fileInfo);
        var functionalScore = AnalyzeFunctionality(fileInfo);
        var visualScore = AnalyzeVisualImportance(fileInfo);
        
        return new ComplexityResult
        {
            overallScore = (geometricScore + functionalScore + visualScore) / 3,
            level = DetermineComplexityLevel(overallScore),
            recommendations = GenerateRecommendations(fileInfo)
        };
    }
}
```

### 自定义比例验证器
```csharp
public class CustomScaleValidator : IScaleValidator
{
    public ScaleValidationResult ValidateScale(GameObject prefab, ModelConfig config)
    {
        var currentBounds = prefab.GetComponent<Renderer>().bounds;
        var expectedSize = config.targetSize;
        
        var scaleDifference = Vector3.Distance(currentBounds.size, expectedSize);
        var tolerance = config.scaleTolerance;
        
        return new ScaleValidationResult
        {
            isValid = scaleDifference <= tolerance,
            actualSize = currentBounds.size,
            expectedSize = expectedSize,
            scaleDifference = scaleDifference,
            suggestedCorrection = expectedSize.x / currentBounds.size.x
        };
    }
}
```

## 📈 性能优化

### 并行处理配置
```csharp
var settings = new BatchImportSettings
{
    // 根据CPU核心数调整并发数
    maxConcurrentOperations = Mathf.Min(Environment.ProcessorCount, 8),
    
    // 启用内存管理
    enableMemoryManagement = true,
    maxMemoryUsageMB = 2048,
    
    // 启用缓存系统
    useFileCache = true,
    cacheDirectory = "Temp/ModelParameterLib/Cache/"
};
```

### 大文件处理优化
```csharp
var largeFileSettings = new ConfigGenerationSettings
{
    // 大文件专用设置
    enableStreamingLoad = true,
    loadInBackground = true,
    progressiveProcessing = true,
    
    // 内存优化
    enableGCManagement = true,
    forceGCAfterEachFile = true
};
```

## 🔍 监控和调试

### 性能监控
```csharp
// 启用详细日志
ModelParameterLibSettings.EnableDetailedLogging = true;
ModelParameterLibSettings.LogLevel = LogLevel.Verbose;

// 性能分析器
var profiler = new ImportProfiler();
profiler.StartProfiling();

await batchManager.StartBatchImport(settings);

var results = profiler.StopProfiling();
Debug.Log($"总处理时间: {results.totalTime}ms");
Debug.Log($"平均单文件时间: {results.averageFileTime}ms");
Debug.Log($"内存峰值: {results.peakMemoryUsage}MB");
```

### 错误诊断
```csharp
// 设置错误处理器
batchManager.OnErrorOccurred += (file, error, context) =>
{
    Debug.LogError($"处理文件 {file.fileName} 时发生错误:");
    Debug.LogError($"  错误: {error.Message}");
    Debug.LogError($"  上下文: {context}");
    
    // 错误恢复策略
    if (error is MemoryException)
    {
        // 内存不足时的处理
        GC.Collect();
        return ErrorRecoveryAction.Retry;
    }
    
    return ErrorRecoveryAction.Skip;
};
```

## 🧪 测试和验证

### 运行系统演示
在Unity菜单栏选择：`Tools > ModelParameterLib > 系统演示工具`

### 单元测试
```csharp
[Test]
public void TestFileScanning()
{
    var scanner = new FileScanner();
    var settings = new FileScanSettings
    {
        targetDirectory = "Assets/TestData/",
        includeSubdirectories = true
    };
    
    var files = scanner.ScanDirectory(settings);
    Assert.IsNotNull(files);
    Assert.IsTrue(files.Count > 0);
}

[Test]
public void TestConfigGeneration()
{
    var generator = new ConfigGenerator();
    var testFile = new GLBFileInfo
    {
        fileName = "test_model.glb",
        fileSizeBytes = 1024000
    };
    
    var config = generator.GenerateConfig(testFile, new ConfigGenerationSettings());
    Assert.IsNotNull(config);
    Assert.IsNotEmpty(config.modelName);
}
```

## 🤝 扩展开发

### 添加新的文件格式支持
```csharp
public class FBXFileInfo : IModelFileInfo
{
    // 实现FBX文件特定的属性和方法
}

// 注册新的文件类型
ModelParameterLib.RegisterFileType<FBXFileInfo>(".fbx");
```

### 自定义数据库查询器
```csharp
public class CustomDatabaseQuery : IDatabaseQuery
{
    public async Task<QueryResult> QueryAsync(string modelName, QueryContext context)
    {
        // 自定义查询逻辑
        // 可以连接外部API、数据库等
    }
}
```

## 📋 最佳实践

### 1. 文件组织
- 将GLB文件按类别组织在不同的子文件夹中
- 使用有意义的文件命名约定
- 保持源文件和生成文件分离

### 2. 性能优化
- 根据硬件配置调整并发处理数量
- 定期清理缓存文件
- 监控内存使用情况

### 3. 质量控制
- 定期更新复杂度和比例数据库
- 验证生成的预制件质量
- 建立自动化测试流程

### 4. 错误处理
- 实现完善的日志记录
- 设置合理的重试策略
- 建立错误报告机制

## 🐛 故障排除

### 常见问题

#### 1. 文件扫描失败
```
原因: 文件路径包含特殊字符或权限不足
解决: 检查文件路径，确保Unity有访问权限
```

#### 2. 配置生成错误
```
原因: 数据库文件损坏或缺失
解决: 重新导入数据库文件，检查JSON格式
```

#### 3. 预制件创建失败
```
原因: GLB文件格式不支持或损坏
解决: 验证GLB文件完整性，使用标准的GLB导出工具
```

#### 4. 内存不足
```
原因: 处理大量大文件时内存溢出
解决: 减少并发数量，启用内存管理功能
```

## 📞 支持和贡献

- **Bug报告**: 请使用GitHub Issues提交问题
- **功能请求**: 欢迎提交Enhancement建议  
- **代码贡献**: 遵循代码规范，提交Pull Request
- **文档改进**: 帮助完善文档和示例

## 📄 许可证

本项目基于MIT许可证开源。详见 [LICENSE](LICENSE) 文件。

## 🙏 致谢

感谢所有为ModelParameterLib项目做出贡献的开发者和用户！

---

**ModelParameterLib** - 让Unity 3D模型导入更智能、更高效！ 🚀 