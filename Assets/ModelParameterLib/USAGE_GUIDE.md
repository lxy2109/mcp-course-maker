# 🎯 ModelParameterLib 使用指南

本指南将详细介绍如何使用ModelParameterLib系统进行GLB模型的批量导入和处理。

## 📋 目录

1. [系统概述](#系统概述)
2. [环境准备](#环境准备)
3. [基础使用流程](#基础使用流程)
4. [高级功能详解](#高级功能详解)
5. [数据库配置](#数据库配置)
6. [常见应用场景](#常见应用场景)
7. [故障排除](#故障排除)
8. [最佳实践](#最佳实践)

## 🌟 系统概述

ModelParameterLib是一个完整的GLB模型处理系统，主要包含：

- **文件扫描器**: 自动发现和分析GLB文件
- **配置生成器**: 基于数据库生成最优导入配置
- **预制件生成器**: 批量创建Unity预制件
- **批量管理器**: 协调整个处理流程

## 🔧 环境准备

### 1. 系统要求
- Unity 2021.3 LTS 或更高版本
- .NET Framework 4.7.1 或更高版本
- 至少 4GB 可用内存（推荐 8GB+）
- 至少 1GB 可用磁盘空间

### 2. 安装步骤
1. 将 `ModelParameterLib` 文件夹复制到项目的 `Assets` 目录
2. 等待Unity自动导入和编译
3. 验证安装：菜单栏应该出现 `Tools > ModelParameterLib` 选项

### 3. 初次配置
打开 `Tools > ModelParameterLib > 系统管理界面`，检查系统状态：
- ✅ 数据库文件完整性
- ✅ 缓存目录权限
- ✅ 临时文件夹设置

## 🚀 基础使用流程

### 步骤1: 准备GLB文件
```
推荐的文件组织结构：
Assets/
└── Models/
    └── GLB/
        ├── 化学实验/
        │   ├── 紫外分光光度计.glb
        │   ├── 显微镜.glb
        │   └── 烧杯.glb
        ├── 物理实验/
        │   ├── 天平.glb
        │   └── 弹簧测力计.glb
        └── 生物实验/
            ├── 解剖刀.glb
            └── 培养皿.glb
```

### 步骤2: 打开批量导入界面
- 菜单栏选择：`Tools > ModelParameterLib > 批量导入工具`
- 或使用快捷键：`Ctrl+Shift+M`

### 步骤3: 配置导入设置
```
基础设置：
✅ 源目录: Assets/Models/GLB/
✅ 输出目录: Assets/Prefabs/Generated/
✅ 包含子目录: 是
✅ 文件过滤: *.glb

高级设置：
✅ 使用复杂度数据库: 是
✅ 使用比例数据库: 是
✅ 自动类别检测: 是
✅ 最大并发数: 4
```

### 步骤4: 开始批量处理
1. 点击 `🔍 扫描文件` 按钮
2. 检查扫描结果列表
3. 调整必要的设置
4. 点击 `🚀 开始导入` 按钮
5. 监控进度和结果

## 🎯 高级功能详解

### 1. 智能复杂度分析

系统会自动分析每个模型的复杂度并分配处理策略：

```csharp
// 查看某个模型的复杂度分析结果
var analyzer = new ComplexityAnalyzer();
var result = analyzer.AnalyzeModel("紫外分光光度计.glb");

Debug.Log($"复杂度等级: {result.level}");
Debug.Log($"几何复杂度: {result.geometricScore}");
Debug.Log($"功能复杂度: {result.functionalScore}");
Debug.Log($"视觉重要性: {result.visualScore}");
Debug.Log($"建议面数: {result.recommendedFaceCount}");
```

#### 复杂度等级说明：
- **S级-超精密**: 显微镜、精密天平等（15000-25000面）
- **A级-精密设备**: 分光光度计、离心机等（8000-15000面）
- **B级-标准设备**: 加热板、pH计等（4000-8000面）
- **C级-简单器材**: 烧杯、试管架等（1500-4000面）
- **D级-辅助物品**: 试管、玻璃棒等（500-1500面）

### 2. 精确比例管理

系统内置多学科标准尺寸数据库：

```csharp
// 查询标准尺寸
var scaleManager = new ScaleManager();
var standardSize = scaleManager.GetStandardSize("实验桌", "化学");

Debug.Log($"标准尺寸: {standardSize.width}m × {standardSize.height}m × {standardSize.depth}m");
Debug.Log($"容差范围: ±{standardSize.tolerance * 100}%");
```

#### 支持的学科领域：
- 🧪 **化学**: 实验桌、分光仪、烧杯、试管等
- 🔬 **生物**: 显微镜、培养皿、解剖器具等
- ⚡ **物理**: 测力计、电表、光学器件等
- 🏗️ **工程**: 机械零件、测量工具等
- 🏥 **医学**: 手术器具、诊断设备等
- 🔌 **电子**: 电路元件、测试设备等
- 🏠 **日常**: 家具、文具、生活用品等
- 🏢 **建筑**: 建筑材料、施工工具等

### 3. 自动配置生成

基于模型特征自动生成最优导入参数：

```csharp
// 自定义配置生成规则
var configSettings = new ConfigGenerationSettings
{
    // 基础设置
    useComplexityDatabase = true,
    useScaleDatabase = true,
    autoDetectCategory = true,
    
    // 性能优化
    targetPerformanceLevel = PerformanceLevel.High,
    maxTextureSize = 2048,
    compressionQuality = TextureCompressionQuality.High,
    
    // 特殊处理
    preserveAnimations = true,
    optimizeMaterials = true,
    generateLODs = false,
    
    // 验证设置
    enableQualityCheck = true,
    strictValidation = false
};
```

### 4. 预制件自动创建

系统会为每个GLB文件创建对应的Unity预制件：

```csharp
// 预制件生成设置
var prefabSettings = new PrefabGenerationSettings
{
    // 输出设置
    outputDirectory = "Assets/Prefabs/Generated/",
    useModelNameAsPrefix = true,
    overwriteExisting = false,
    
    // 组件设置
    addDefaultCollider = true,
    colliderType = ColliderType.MeshCollider,
    addRigidbody = false,
    
    // 材质设置
    preserveOriginalMaterials = true,
    convertToUnityMaterials = true,
    optimizeTextures = true,
    
    // 缓存设置
    useCache = true,
    cacheDirectory = "Temp/ModelParameterLib/Cache/"
};
```

#### 自动添加的组件：
- **MeshRenderer** + **MeshFilter**: 渲染网格
- **Collider**: 根据设置添加碰撞体
- **ObjectRegister**: 用于课程系统的物体注册
- **MaterialOptimizer**: 材质优化组件（可选）

## 📊 数据库配置

### 1. 复杂度数据库自定义

编辑 `ModelComplexityDatabase.json` 添加新的模型类型：

```json
{
  "domain_specific_models": {
    "你的领域": {
      "新设备名称": {
        "complexity_score": 75.0,
        "geometric_complexity": 80,
        "functional_complexity": 70,
        "visual_importance": 75,
        "functional_parts": ["部件1", "部件2"],
        "processing_priority": "high",
        "special_requirements": {
          "preserve_detail_level": 0.9,
          "min_face_count": 5000,
          "texture_resolution": 2048
        }
      }
    }
  }
}
```

### 2. 比例数据库扩展

编辑 `ModelScaleDatabase.json` 添加新的物品尺寸：

```json
{
  "domains": {
    "你的学科": {
      "items": {
        "新物品": {
          "standard_size": {
            "width": 0.5,
            "height": 0.3,
            "depth": 0.2
          },
          "size_unit": "meters",
          "tolerance": 0.05,
          "importance": "high",
          "category": "设备",
          "reference_source": "国家标准GB/T xxxx-xxxx",
          "notes": "特殊说明"
        }
      }
    }
  }
}
```

### 3. 验证数据库完整性

使用系统提供的验证工具：

```csharp
// 验证复杂度数据库
var complexityValidator = new ComplexityDatabaseValidator();
var complexityResult = complexityValidator.ValidateDatabase();

if (!complexityResult.isValid)
{
    foreach (var error in complexityResult.errors)
    {
        Debug.LogError($"复杂度数据库错误: {error}");
    }
}

// 验证比例数据库
var scaleValidator = new ScaleDatabaseValidator();
var scaleResult = scaleValidator.ValidateDatabase();

if (!scaleResult.isValid)
{
    foreach (var error in scaleResult.errors)
    {
        Debug.LogError($"比例数据库错误: {error}");
    }
}
```

## 🎬 常见应用场景

### 场景1: 化学实验课程模型导入

```csharp
// 专门针对化学实验的导入设置
var chemistrySettings = new BatchImportSettings
{
    sourceDirectory = "Assets/Models/化学实验/",
    outputDirectory = "Assets/Prefabs/化学课程/",
    
    // 化学器材特殊设置
    defaultCategory = "化学实验",
    enforceGlassTransparency = true,
    preserveChemicalSafety = true,
    
    // 性能优化（化学器材通常比较简单）
    targetPerformanceLevel = PerformanceLevel.Medium,
    maxFaceCountPerModel = 8000,
    
    // 比例严格控制（化学实验对比例要求较高）
    enforceStrictScaling = true,
    scaleTolerancePercent = 5.0f
};
```

### 场景2: 大批量模型快速处理

```csharp
// 高性能批量处理设置
var batchSettings = new BatchImportSettings
{
    sourceDirectory = "Assets/Models/BatchImport/",
    outputDirectory = "Assets/Prefabs/Batch/",
    
    // 性能优化
    maxConcurrentOperations = Environment.ProcessorCount,
    enableMemoryManagement = true,
    maxMemoryUsageMB = 4096,
    
    // 质量vs速度平衡
    enableQuickMode = true,
    skipDetailedAnalysis = false,
    useProgressiveProcessing = true,
    
    // 错误处理
    enableErrorRecovery = true,
    maxRetryAttempts = 3,
    continueOnError = true
};
```

### 场景3: 高质量模型精确处理

```csharp
// 高质量处理设置
var highQualitySettings = new BatchImportSettings
{
    sourceDirectory = "Assets/Models/HighQuality/",
    outputDirectory = "Assets/Prefabs/Premium/",
    
    // 质量优先
    targetPerformanceLevel = PerformanceLevel.Ultra,
    preserveOriginalQuality = true,
    enableDetailedAnalysis = true,
    
    // 材质和纹理
    maxTextureSize = 4096,
    compressionQuality = TextureCompressionQuality.Uncompressed,
    preserveAlphaChannels = true,
    
    // 几何体处理
    preserveOriginalTopology = true,
    enableSmoothing = false,
    generateLightmapUVs = true
};
```

## 🐛 故障排除

### 问题1: 文件扫描失败

**症状**: 扫描器无法找到GLB文件
```
错误信息: "目录访问被拒绝" 或 "未找到指定文件"
```

**解决方案**:
1. 检查文件路径是否正确
2. 确保Unity有读取权限
3. 验证GLB文件完整性
```csharp
// 手动验证文件
var fileInfo = new FileInfo(glbFilePath);
Debug.Log($"文件存在: {fileInfo.Exists}");
Debug.Log($"文件大小: {fileInfo.Length} bytes");
Debug.Log($"文件权限: {fileInfo.Attributes}");
```

### 问题2: 配置生成错误

**症状**: 无法为某个模型生成配置
```
错误信息: "数据库查询失败" 或 "未找到匹配项"
```

**解决方案**:
1. 检查数据库文件完整性
2. 验证JSON格式
3. 添加缺失的模型条目
```csharp
// 手动验证数据库
var validator = new DatabaseValidator();
var result = validator.ValidateComplexityDatabase();
if (!result.isValid)
{
    Debug.LogError($"数据库错误: {string.Join(", ", result.errors)}");
}
```

### 问题3: 预制件创建失败

**症状**: GLB导入到Unity后无法创建预制件
```
错误信息: "材质丢失" 或 "网格损坏"
```

**解决方案**:
1. 检查GLB文件格式
2. 验证材质和纹理路径
3. 重新导入模型
```csharp
// 重新导入单个文件
var importer = AssetImporter.GetAtPath(glbPath) as ModelImporter;
if (importer != null)
{
    importer.SaveAndReimport();
    Debug.Log($"重新导入完成: {glbPath}");
}
```

### 问题4: 内存不足

**症状**: 处理大文件时Unity崩溃或卡死
```
错误信息: "OutOfMemoryException" 或 Unity无响应
```

**解决方案**:
1. 减少并发处理数量
2. 启用内存管理
3. 分批处理大文件
```csharp
// 内存优化设置
var settings = new BatchImportSettings
{
    maxConcurrentOperations = 2,
    enableMemoryManagement = true,
    maxMemoryUsageMB = 2048,
    forceGCAfterEachFile = true,
    enableProgressiveProcessing = true
};
```

## ✨ 最佳实践

### 1. 文件组织最佳实践

```
推荐的目录结构：
Assets/
├── Models/
│   ├── Source/           # 原始GLB文件
│   │   ├── Chemistry/
│   │   ├── Physics/
│   │   └── Biology/
│   └── Processed/        # 处理后的文件
│       ├── Optimized/
│       └── Archives/
├── Prefabs/
│   ├── Generated/        # 自动生成的预制件
│   ├── Course/           # 课程专用预制件
│   └── Templates/        # 预制件模板
└── Materials/
    ├── Generated/        # 自动生成的材质
    └── Custom/           # 自定义材质
```

### 2. 性能优化最佳实践

```csharp
// 推荐的性能设置
var optimizedSettings = new BatchImportSettings
{
    // 根据硬件调整
    maxConcurrentOperations = Mathf.Min(Environment.ProcessorCount, 6),
    
    // 内存管理
    enableMemoryManagement = true,
    maxMemoryUsageMB = Mathf.Min(SystemInfo.systemMemorySize / 2, 4096),
    
    // 缓存策略
    useFileCache = true,
    cacheExpirationHours = 24,
    cleanCacheOnStartup = false,
    
    // 进度反馈
    progressUpdateInterval = 100, // 每100ms更新一次
    enableDetailedProgress = true
};
```

### 3. 质量控制最佳实践

```csharp
// 质量检查流程
public class QualityController
{
    public void ValidateImportResults(List<ImportResult> results)
    {
        foreach (var result in results)
        {
            // 检查预制件完整性
            ValidatePrefabIntegrity(result.prefab);
            
            // 检查材质正确性
            ValidateMaterials(result.prefab);
            
            // 检查比例准确性
            ValidateScale(result.prefab, result.config);
            
            // 检查性能指标
            ValidatePerformance(result.prefab);
        }
    }
    
    private void ValidatePrefabIntegrity(GameObject prefab)
    {
        // 检查必要组件
        Assert.IsNotNull(prefab.GetComponent<MeshRenderer>());
        Assert.IsNotNull(prefab.GetComponent<MeshFilter>());
        
        // 检查网格数据
        var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        Assert.IsTrue(mesh.vertexCount > 0);
        Assert.IsTrue(mesh.triangles.Length > 0);
    }
}
```

### 4. 错误恢复最佳实践

```csharp
// 错误恢复策略
public class ErrorRecoveryManager
{
    public ErrorRecoveryAction HandleError(ImportError error, ImportContext context)
    {
        switch (error.type)
        {
            case ErrorType.FileNotFound:
                // 文件不存在 - 跳过
                LogWarning($"跳过缺失文件: {context.fileName}");
                return ErrorRecoveryAction.Skip;
                
            case ErrorType.OutOfMemory:
                // 内存不足 - 清理后重试
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                return ErrorRecoveryAction.Retry;
                
            case ErrorType.InvalidFormat:
                // 格式错误 - 尝试修复后重试
                if (TryRepairFile(context.filePath))
                    return ErrorRecoveryAction.Retry;
                else
                    return ErrorRecoveryAction.Skip;
                
            case ErrorType.DatabaseError:
                // 数据库错误 - 使用默认配置继续
                context.useDefaultConfig = true;
                return ErrorRecoveryAction.Retry;
                
            default:
                // 未知错误 - 记录并跳过
                LogError($"未知错误: {error.message}");
                return ErrorRecoveryAction.Skip;
        }
    }
}
```

### 5. 监控和调试最佳实践

```csharp
// 性能监控
public class PerformanceMonitor
{
    private Dictionary<string, float> metrics = new Dictionary<string, float>();
    
    public void StartMonitoring()
    {
        // 监控CPU使用率
        InvokeRepeating(nameof(UpdateCPUUsage), 1f, 1f);
        
        // 监控内存使用
        InvokeRepeating(nameof(UpdateMemoryUsage), 1f, 1f);
        
        // 监控处理速度
        InvokeRepeating(nameof(UpdateProcessingSpeed), 5f, 5f);
    }
    
    private void UpdateMemoryUsage()
    {
        var memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f;
        metrics["MemoryUsageMB"] = memoryUsage;
        
        if (memoryUsage > 3072) // 超过3GB时警告
        {
            Debug.LogWarning($"内存使用过高: {memoryUsage:F1}MB");
        }
    }
}
```

---

通过遵循这些指南和最佳实践，您可以充分发挥ModelParameterLib系统的能力，实现高效、稳定的GLB模型批量处理工作流程。

如果遇到问题，请参考故障排除章节，或查看系统日志获取更详细的错误信息。 