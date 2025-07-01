# 快速入门指南

## 1. 安装

### 自动安装（推荐）

```bash
python install.py
```

### 手动安装

```bash
# 安装依赖
pip install -r requirements.txt

# 复制配置文件
cp env.example .env
cp mcp.json.example mcp.json
```

## 2. 配置API密钥

编辑 `.env` 文件：

```env
BAIDU_API_KEY=your_actual_api_key
BAIDU_SECRET_KEY=your_actual_secret_key
```

### 获取API密钥步骤：

1. 注册百度智能云账号：https://cloud.baidu.com/
2. 进入控制台 → 人工智能 → 图像识别
3. 创建应用，获取API Key和Secret Key

## 3. 测试服务

```bash
python test_server.py
```

成功输出示例：
```
✅ API凭据配置
✅ 获取识别类型  
✅ URL图片识别
✅ 本地文件识别
✅ 不同识别类型

🎉 所有测试通过！服务可以正常使用
```

## 4. 启动MCP服务

```bash
python server.py
```

## 5. 快速使用示例

### 通用物体识别
```python
result = await baidu_image_recognition(
    image_input="/path/to/image.jpg",
    recognition_type="general_basic",
    top_num=5
)
```

### 动物识别
```python
result = await baidu_image_recognition(
    image_input="https://example.com/animal.jpg",
    recognition_type="animal",
    top_num=3,
    baike_num=1
)
```

### 获取所有识别类型
```python
types = await get_recognition_types()
print(f"支持{types['total_types']}种识别类型")
```

## 6. 运行示例

```bash
python example.py
```

这将展示各种识别类型的使用方法。

## 7. 配置MCP客户端

在你的MCP客户端配置中添加：

```json
{
  "mcpServers": {
    "baidu-image-recognition": {
      "command": "python",
      "args": ["server.py"],
      "cwd": "baidu-image-recognition-mcp",
      "env": {
        "BAIDU_API_KEY": "your_actual_api_key",
        "BAIDU_SECRET_KEY": "your_actual_secret_key"
      }
    }
  }
}
```

## 常见问题

### Q: API密钥错误
A: 检查 `.env` 文件中的密钥是否正确，确保没有多余的空格或引号

### Q: 网络连接失败
A: 检查网络连接，确认能访问百度API服务

### Q: 图片识别失败
A: 确保图片格式为JPG/PNG/BMP，大小不超过4MB

### Q: 模块导入失败
A: 运行 `pip install -r requirements.txt` 安装所有依赖

## 更多信息

- 完整文档：[README.md](README.md)
- 百度API文档：https://cloud.baidu.com/doc/IMAGERECOGNITION/
- MCP协议：https://mcp-docs.cn/ 