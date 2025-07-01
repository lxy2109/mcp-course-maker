# 复合Timeline生成功能使用指南

本文档介绍如何使用新增的复合timeline生成功能，该功能支持两种模式：分离式和组合式timeline生成。

## 功能概述

### 输入参数
- **镜头timeline名称**: 相机动画的timeline资产名称
- **镜头timeline内容**: 描述相机动画的自然语言文本
- **物品timeline名称**: 物体动画的timeline资产名称  
- **物体timeline内容**: 描述物体动画的自然语言文本

### 生成方式

#### 方式1：分离式Timeline生成
分别生成两个独立的timeline文件：
1. 根据镜头信息生成相机timeline
2. 根据物体信息生成物体timeline
3. 两个timeline可以独立使用或组合使用

#### 方式2：组合式Timeline生成  
生成一个包含3个clip的复杂timeline：
1. **Clip 1**: 相机从初始位置移动到物体前
2. **Clip 2**: 物体执行指定动画
3. **Clip 3**: 相机从物体前移回初始位置

## Python API使用示例

### 1. 分离式Timeline生成

```python
# 使用 generate_separate_timelines 函数
result = mcp_unityMCP_generate_separate_timelines(
    camera_timeline_name="CameraMovement",
    camera_timeline_content="镜头环视四周，俯视20度，持续10秒",
    object_timeline_name="ObjectRotation", 
    object_timeline_content="立方体旋转90度",
    target_object_name="Cube",
    camera_name="Main Camera"
)
```

### 2. 组合式Timeline生成

```python
# 使用 generate_combined_timeline 函数
result = mcp_unityMCP_generate_combined_timeline(
    timeline_name="ComplexAnimation",
    camera_timeline_content="镜头移动到物体前进行特写",
    object_timeline_content="球体向前移动5米",
    target_object_name="Sphere",
    camera_name="Main Camera",
    clip_duration=5.0
)
```

## 自然语言描述支持

### 相机动画描述
- **环视**: "镜头环视四周"、"相机360度旋转"、"环绕视角"
- **扫视**: "镜头左右扫视"、"摆动拍摄"、"左右摇摆"
- **特写**: "镜头拉近特写"、"靠近拍摄"、"接近物体"
- **围绕**: "围绕物体旋转"、"绕着目标转圈"

### 物体动画描述
- **移动**: "向前移动5米"、"移动到左侧"、"上下移动"
- **旋转**: "旋转90度"、"左转"、"转身"
- **组合**: "移动到前方并旋转180度"

### 参数提取
系统会自动从描述中提取：
- **时间**: "持续10秒"、"5秒内完成"
- **角度**: "俯视20度"、"旋转90度"  
- **距离**: "移动5米"、"距离3单位"
- **方向**: "向前"、"左侧"、"上方"

## Unity C# 实现细节

### 分离式Timeline处理
```csharp
public static object CreateSeparateTimelines(JObject @params)
{
    // 1. 解析镜头和物体参数
    // 2. 分别调用 CreateMovementAnimation 创建两个timeline
    // 3. 返回两个timeline的路径信息
}
```

### 组合式Timeline处理
```csharp
public static object CreateCombinedTimeline(JObject @params)
{
    // 1. 创建包含多轨道的单个Timeline资产
    // 2. 添加相机移动轨道（2个clip：去程+回程）
    // 3. 添加物体动画轨道（1个clip：物体动画）
    // 4. 设置时间衔接和同步
}
```

## 实际应用场景

### 场景1：实验演示
```python
# 生成实验器材操作的完整动画
generate_combined_timeline(
    timeline_name="ExperimentDemo",
    camera_timeline_content="镜头特写显微镜，俯视30度",
    object_timeline_content="显微镜向右移动3米到实验台",
    target_object_name="Microscope"
)
```

### 场景2：产品展示
```python
# 分别生成相机和产品动画，后续可组合使用
generate_separate_timelines(
    camera_timeline_name="ProductShowCamera",
    camera_timeline_content="镜头环视产品，持续15秒",
    object_timeline_name="ProductRotation",
    object_timeline_content="产品旋转360度展示",
    target_object_name="Product"
)
```

## 错误处理

系统提供完整的错误处理：
- 物体不存在时的提示
- 无法解析自然语言时的默认行为
- Timeline创建失败时的回滚机制
- 详细的错误日志和调试信息

## 扩展功能

### 自定义动画函数支持
可以轻松添加新的动画类型：
```python
def execute_animation_function(ctx: Context, params: Dict[str, Any]) -> str:
    function_name = params.get('function')
    
    if function_name == 'custom_animation':
        return custom_animation_function(ctx, **params)
    # ... 其他动画函数
```

### 复杂路径规划
支持多点路径和贝塞尔曲线：
```python
# 多点路径
points = [
    {"position": {"x": 0, "y": 0, "z": 0}, "time": 0},
    {"position": {"x": 5, "y": 2, "z": 3}, "time": 2.5},
    {"position": {"x": 0, "y": 5, "z": 0}, "time": 5.0}
]
```

这些新功能为Unity中的自动化timeline生成提供了强大而灵活的解决方案。 