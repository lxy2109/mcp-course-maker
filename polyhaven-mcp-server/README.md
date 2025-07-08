# Poly Haven MCP Server

这是一个用于与 [Poly Haven API](https://api.polyhaven.com/) 交互的模型上下文协议 (MCP) 服务器。它提供了搜索、获取和下载3D资产、材质、HDRI等功能的工具。

## 功能特性

- 搜索3D模型、材质、HDRI等资产
- 获取资产详细信息
- 下载资产文件
- 获取资产分类信息
- 支持多种文件格式（GLB、FBX、OBJ等）
- 支持多种分辨率下载

## 安装

1. 克隆此仓库：
   ```bash
   git clone https://github.com/yourusername/polyhaven-mcp-server
   cd polyhaven-mcp-server
   ```

2. 设置虚拟环境：
   ```bash
   python -m venv .venv
   # Windows
   .\.venv\Scripts\activate
   # macOS/Linux
   source .venv/bin/activate
   ```

3. 安装依赖：
   ```bash
   pip install -e .
   ```

## 使用方法

### 启动服务器

直接使用Python启动：
```bash
python src/server.py
```

或使用MCP CLI：
```bash
mcp run config.json
```

### 编辑器配置

将此MCP服务器配置添加到你的Cursor/VS Code设置中（例如 `.vscode/settings.json` 或用户设置）：

```json
{
  "mcpServers": {
    "polyhaven": {
      "command": "polyhaven-mcp-server",
      "disabled": false,
      "env":[
        "DOWNLOAD_PATH":"YOUR DOWNLOAD PATH"
      ]
    }
  }
}
```

## 环境变量

- `POLYHAVEN_API_KEY`: Poly Haven API密钥（可选，用于高级功能）
- `DOWNLOAD_PATH`: 下载文件保存路径（默认：./downloads）

## 示例

### 搜索3D模型

```python
from mcp.client import MCPClient

client = MCPClient()
result = client.use_tool(
    "polyhaven",
    "search_assets",
    {
        "type": "models",
        "q": "chair",
        "categories": ["furniture"],
        "limit": 10
    }
)
print(f"找到 {len(result['results'])} 个模型")
```

### 获取资产详情

```python
result = client.use_tool(
    "polyhaven",
    "get_asset_info",
    {
        "type": "models",
        "slug": "chair_01"
    }
)
print(f"资产名称: {result['name']}")
```

### 下载资产

```python
result = client.use_tool(
    "polyhaven",
    "download_asset",
    {
        "type": "models",
        "slug": "chair_01",
        "format": "glb",
        "resolution": "2k"
    }
)
print(f"下载路径: {result['file_path']}")
```

## API参考

### 工具列表

- `search_assets`: 搜索资产
- `get_asset_info`: 获取资产详细信息
- `download_asset`: 下载资产文件
- `get_categories`: 获取分类信息
- `get_asset_files`: 获取资产可用文件列表

### 支持的资产类型

- `models`: 3D模型
- `materials`: 材质
- `hdris`: HDRI环境贴图
- `textures`: 纹理

### 支持的文件格式

- `glb`: GLB格式（推荐）
- `fbx`: FBX格式
- `obj`: OBJ格式
- `blend`: Blender文件

## 许可证

本项目采用MIT许可证 - 详见LICENSE文件。

## 贡献

欢迎提交问题报告和拉取请求！ 