# 紫外可见光光度计实验 - Timeline动作分类系统

## 📋 系统概述

这是一个基于自然语言处理的智能Timeline动作分类系统，专门为紫外可见光光度计测量实验设计。系统能够解析FlowEventNode中的自然语言描述，自动识别动作类型，并生成对应的Unity Timeline动画。

## 🏗️ 系统架构

### 核心组件

1. **TimelineActionClassifier** - 主要分类器
2. **ActionResult** - 动作识别结果数据结构
3. **实验专用模式库** - 针对光度计实验的特殊动作识别

## 🎯 动作类型分类

### 📹 镜头动作类型 (Camera Actions)

| 动作类型 | MCP函数 | 应用场景 | 关键词 |
|---------|---------|----------|--------|
| **camera_panorama** | `camera_panorama_animation` | 全景观察实验环境 | 环视、360度、全景、旋转一周 |
| **camera_sweep** | `camera_sweep_animation` | 左右扫视检查器材 | 扫视、左右扫描、水平摆动 |
| **camera_closeup** | `camera_closeup_animation` | 特写操作细节 | 特写、聚焦、近距离、细节观察 |
| **camera_orbit** | `rotate_around_target_animation` | 环绕设备拍摄 | 围绕旋转、环绕拍摄、轨道运动 |
| **camera_move** | `create_multipoint_animation` | 镜头位置移动 | 平移、移动到、推进、拉远 |

### 📦 物体动作类型 (Object Actions)

| 动作类型 | MCP函数 | 应用场景 | 关键词 |
|---------|---------|----------|--------|
| **object_multipoint** | `create_multipoint_animation` | 物体位置移动 | 移动到、平移至、拿取、放置 |
| **object_rotation** | `create_multipoint_animation` | 旋转操作 | 旋转、转动、打开、关闭 |
| **object_orbit** | `rotate_around_target_animation` | 环绕运动 | 围绕移动、圆周运动 |
| **object_sequence** | `create_multipoint_animation` | 复杂动作序列 | 依次、顺序、先后 |

## 🧪 实验专用动作模式

### 仪器检查类 (Instrument Inspection)
```python
"keywords": [
    "检查.*外观", "检查.*电源", "检查.*样品室", 
    "观察.*状态", "确认.*连接", "验证.*功能"
]
"camera_action": "camera_closeup"
"object_action": None
```

### 按钮操作类 (Button Operation)
```python
"keywords": [
    "按下.*按钮", "按.*键", "点击.*按钮", 
    "按压.*开关", "操作.*控制", "启动.*按钮"
]
"camera_action": "camera_closeup"
"object_action": "object_rotation"
```

### 样品操作类 (Sample Handling)
```python
"keywords": [
    "放入.*比色皿", "取出.*样品", "更换.*溶液", 
    "清洗.*器皿", "倒入.*溶液", "倒掉.*液体"
]
"camera_action": "camera_closeup"
"object_action": "object_multipoint"
```

### 设备连接类 (Equipment Connection)
```python
"keywords": [
    "连接.*电源", "插入.*插座", "连接.*线缆", "接通.*电源"
]
"camera_action": "camera_closeup"
"object_action": "object_multipoint"
```

### 数据记录类 (Data Recording)
```python
"keywords": [
    "记录.*数值", "观察.*读数", "读取.*数据", "记录.*结果"
]
"camera_action": "camera_closeup"
"object_action": None
```

### 清理整理类 (Cleanup Organization)
```python
"keywords": [
    "擦拭.*表面", "清洁.*部件", "整理.*设备", 
    "收拾.*器具", "扔掉.*废料", "清理.*残留"
]
"camera_action": "camera_sweep"
"object_action": "object_multipoint"
```

## 🔧 实验器材识别

### 主要设备
- **紫外可见光分光仪** - 主要实验设备
- **比色皿1/2/3** - 样品容器
- **电源线** - 电源连接
- **废液烧杯** - 废料收集
- **擦拭棉球** - 清洁工具
- **塑料洗瓶** - 清洗工具

### 仪器部件
- **样品室** - 样品放置区域
- **电源按钮** - 设备开关
- **吸光度按钮** - 测量模式选择
- **调零按钮** - 校准功能
- **样品杆** - 样品切换机构

## 📊 参数提取功能

### 时间参数
- `(\d+\.?\d*)秒` → `duration`
- `用时(\d+\.?\d*)秒` → `duration`
- `持续(\d+\.?\d*)秒` → `duration`

### 角度参数
- `(\d+)度` → `angle`
- `旋转(\d+)度` → `rotation_angle`
- `俯仰(\d+)度` → `pitch_angle`

### 距离参数
- `距离(\d+\.?\d*)米` → `distance`
- `半径(\d+\.?\d*)米` → `radius`
- `高度(\d+\.?\d*)米` → `height`

## 🎬 典型动作示例

### 镜头动作示例

#### 1. 全景观察
```
输入: "镜头环视四周"
输出:
- 动作类型: camera_panorama
- MCP函数: camera_panorama_animation
- 参数: {camera_name: "Main Camera", pitch_angle: -20, duration: 10, steps: 24}
```

#### 2. 设备检查
```
输入: "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周"
输出:
- 动作类型: camera_orbit
- MCP函数: rotate_around_target_animation
- 参数: {moving_object_name: "Main Camera", target_object_name: "紫外可见光分光仪", radius: 8, height: 3, duration: 12, look_at_target: True}
```

#### 3. 特写观察
```
输入: "镜头聚焦到电源按钮进行特写观察"
输出:
- 动作类型: camera_closeup
- MCP函数: camera_closeup_animation
- 参数: {camera_name: "Main Camera", target_object_name: "电源按钮", closeup_distance: 5, pitch_angle: 10, horizontal_angle: 60, duration: 10, move_speed: 5}
```

### 物体动作示例

#### 1. 电源连接
```
输入: "电源线平移至插排电源口处并插入"
输出:
- 动作类型: object_multipoint
- MCP函数: create_multipoint_animation
- 参数: {name: "电源线", duration: 5, path_type: "linear", include_rotation: False}
```

#### 2. 按钮操作
```
输入: "按下电源按钮，按钮向下按压"
输出:
- 动作类型: object_rotation
- MCP函数: create_multipoint_animation
- 参数: {name: "电源按钮", duration: 3, path_type: "linear", include_rotation: True}
```

#### 3. 样品操作
```
输入: "比色皿1移动到样品室内部"
输出:
- 动作类型: object_multipoint
- MCP函数: create_multipoint_animation
- 参数: {name: "比色皿1", duration: 5, path_type: "linear", include_rotation: False}
```

## 🔄 完整实验流程示例

### 步骤1: 实验准备阶段
- **镜头动作**: "镜头环视四周检查实验环境"
  - 类型: camera_panorama
  - MCP: camera_panorama_animation
- **物体动作**: 无

### 步骤2: 设备检查
- **镜头动作**: "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周"
  - 类型: camera_orbit
  - MCP: rotate_around_target_animation
- **物体动作**: 无

### 步骤3: 电源连接
- **镜头动作**: "镜头聚焦到电源插口进行特写"
  - 类型: camera_closeup
  - MCP: camera_closeup_animation
- **物体动作**: "电源线平移至插排电源口处并插入"
  - 类型: object_multipoint
  - MCP: create_multipoint_animation

### 步骤4: 开机操作
- **镜头动作**: "镜头特写电源按钮"
  - 类型: camera_closeup
  - MCP: camera_closeup_animation
- **物体动作**: "按下电源按钮，按钮向下按压"
  - 类型: object_rotation
  - MCP: create_multipoint_animation

### 步骤5: 样品放入
- **镜头动作**: "镜头跟随样品移动到样品室"
  - 类型: camera_move
  - MCP: create_multipoint_animation
- **物体动作**: "比色皿1移动到样品室内部"
  - 类型: object_multipoint
  - MCP: create_multipoint_animation

### 步骤6: 测量操作
- **镜头动作**: "镜头聚焦到显示屏读取数值"
  - 类型: camera_closeup
  - MCP: camera_closeup_animation
- **物体动作**: "样品杆向左拉动切换样品位置"
  - 类型: object_multipoint
  - MCP: create_multipoint_animation

### 步骤7: 清理维护
- **镜头动作**: "镜头扫视整个清理过程"
  - 类型: camera_sweep
  - MCP: camera_sweep_animation
- **物体动作**: "擦拭棉球在样品室内部环绕擦拭一圈"
  - 类型: object_orbit
  - MCP: rotate_around_target_animation

## 🎯 系统优势

1. **智能识别**: 基于关键词和正则表达式的双重识别机制
2. **实验专用**: 针对紫外可见光光度计实验的专门优化
3. **参数提取**: 自动从描述中提取数值参数
4. **MCP映射**: 直接映射到对应的Unity MCP函数
5. **扩展性强**: 易于添加新的动作类型和实验场景

## 🚀 使用方法

### 1. 创建分类器实例
```python
from timeline_action_classifier import TimelineActionClassifier
classifier = TimelineActionClassifier()
```

### 2. 分析单个动作
```python
result = classifier.classify_timeline_action("镜头环视四周", "camera")
print(f"动作类型: {result.action_type}")
print(f"MCP函数: {result.mcp_function}")
print(f"参数: {result.extracted_params}")
```

### 3. 分析节点数据
```python
camera_result, object_result = classifier.analyze_experiment_timelines(node_data)
```

## 📝 注意事项

1. **置信度阈值**: 系统会计算识别置信度，建议置信度低于0.5时进行人工检查
2. **参数验证**: 提取的参数需要在Unity中验证有效性
3. **物体存在性**: 确保场景中存在识别出的物体名称
4. **Timeline兼容性**: 生成的Timeline需要与Unity版本兼容

## 🔮 未来扩展

1. **机器学习**: 集成深度学习模型提高识别准确率
2. **多实验支持**: 扩展到其他类型的实验场景
3. **参数优化**: 基于实际效果自动调整动画参数
4. **可视化编辑**: 提供图形界面进行动作编辑和预览 