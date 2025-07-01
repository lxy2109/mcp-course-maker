# 百度图像识别 MCP 工具

基于百度智能云图像识别API的MCP (Model Context Protocol) 工具，支持多种图像识别功能。

## 功能特性

- 🔍 **多种识别类型**：支持13种不同的图像识别功能
- 🌐 **灵活输入**：支持本地文件路径和网络图片URL
- ⚡ **高性能**：异步处理，访问令牌自动缓存
- 🛡️ **错误处理**：完善的错误处理和日志记录
- 📝 **类型安全**：使用Pydantic进行数据验证

## 支持的识别类型

| 类型 | 名称 | 描述 |
|------|------|------|
| `general_basic` | 通用物体识别（标准版） | 识别图片中的物体，返回物体名称及置信度 |
| `general` | 通用物体识别（高精度版） | 高精度识别图片中的物体，准确率更高 |
| `advanced_general` | 通用物体和场景识别 | 识别图片中的物体和场景，包含位置信息 |
| `object_detect` | 图像主体检测 | 检测并定位图片中的主体物体 |
| `animal` | 动物识别 | 识别图片中的动物，支持8000+动物种类 |
| `plant` | 植物识别 | 识别图片中的植物，支持2万+植物种类 |
| `logo` | Logo识别 | 识别图片中的品牌Logo，支持数万种Logo |
| `dish` | 菜品识别 | 识别图片中的菜品，返回菜品名称及卡路里信息 |
| `car` | 车型识别 | 识别图片中的车辆型号、品牌、年份等信息 |
| `landmark` | 地标识别 | 识别图片中的地标建筑，返回地标名称及位置 |
| `ingredient` | 果蔬识别 | 识别图片中的水果和蔬菜种类 |
| `red_wine` | 红酒识别 | 识别图片中的红酒品牌和信息 |
| `currency` | 货币识别 | 识别图片中的货币面额和类型 |

## 安装配置

### 1. 安装依赖

```bash
python install.py
```

或者手动安装：

```bash
pip install -r requirements.txt
```

### 2. 配置MCP

在Cursor的MCP配置文件中（通常位于 `%USERPROFILE%\.cursor\mcp.json`）添加以下配置：

```json
{
  "baidu-image-recognition": {
    "command": "python",
    "args": [
      "path_to_your_server.py"
      ],
    "env": {
      "BAIDU_API_KEY": "your_api_key",
      "BAIDU_SECRET_KEY": "your_secret_key"
    }
  }
}
```

注意：
- `cwd` 路径需要使用正斜杠 `/` 而不是反斜杠 `\`
- 请将 `your_api_key` 和 `your_secret_key` 替换为您的实际密钥

### 3. 获取百度API密钥

1. 注册百度智能云账号：https://cloud.baidu.com/
2. 进入控制台 -> 人工智能 -> 图像识别
3. 创建应用，获取API Key和Secret Key

## 使用方法

### 工具调用

#### 1. 获取识别类型列表

```python
result = await get_recognition_types()
```

#### 2. 执行图像识别

```python
# 使用本地文件
result = await baidu_image_recognition({
    "image_path": "/path/to/image.jpg",
    "recognition_type": "general_basic",
    "top_num": 5
})

# 使用网络URL
result = await baidu_image_recognition({
    "image_path": "https://example.com/image.jpg",
    "recognition_type": "animal",
    "top_num": 3,
    "baike_num": 1,
    "with_face": True  # 动物识别特有参数
})
```

## API响应格式

### 成功响应

```json
{
    "success": true,
    "data": {
        "log_id": 123456789,
        "result_num": 1,
        "result": [{
            "keyword": "猫",
            "score": 0.95,
            "root": "动物"
        }]
    },
    "recognition_type": "animal",
    "image_info": {
        "path": "/path/to/image.jpg",
        "size": 1024
    }
}
```

### 错误响应

```json
{
    "success": false,
    "error": "图片文件不存在",
    "recognition_type": "general_basic",
    "image_info": {
        "path": "/invalid/path.jpg"
    }
}
```

## 错误处理

常见错误及解决方案：

- **API密钥错误**：检查MCP配置中的环境变量是否正确
- **文件不存在**：确保图片路径正确且文件存在
- **网络错误**：检查网络连接和URL是否有效
- **图片格式错误**：支持JPG、PNG、BMP格式，大小不超过4MB
- **配额限制**：检查百度API调用次数是否超限

## 项目结构

```
baidu-image-recognition-mcp/
├── server.py          # 主服务文件
├── example.py         # 使用示例
├── install.py         # 安装脚本
├── requirements.txt   # 依赖列表
├── README.md         # 文档
└── QUICKSTART.md     # 快速入门指南
```

## 许可证

MIT License

## 贡献

欢迎提交Issue和Pull Request来改进本项目。 