# Tripo3D MCP 工具集成

本项目基于 Tripo3D 官方 API，封装了文本/图片/多视图转3D、贴图、动画、风格化、格式转换、余额查询等主流能力，适用于本地开发、CLI、Cursor 场景。

---

## 特性
- 文本/图片/多视图转3D模型
- 模型贴图、动画绑定与重定向
- 风格化、格式转换、余额查询
- 全参数建模，自动类型校验
- 异步支持，易于扩展
- 适配本地 CLI/Cursor 环境

## 目录结构
```
tripo-mcp/
├── src/
│   ├── main.py           # 工具服务主程序
│   ├── tripo_api.py      # Tripo3D API 封装
│   ├── models.py         # Pydantic 参数建模
│   ├── config.py         # API 配置
│   └── __init__.py
├── requirements.txt      # 依赖包列表
├── pyproject.toml        # Python 项目元数据
├── mcp.json.example      # MCP 本地服务配置示例
├── .gitignore
└── README.md
```

## 环境要求
- 操作系统：Windows、Linux 或 macOS
- Python：3.8 及以上
- 依赖包：见 requirements.txt 或 pyproject.toml

## 安装
```bash
git clone https://github.com/lxy2109/tripo-mcp.git
cd src
pip install -r requirements.txt
```

## 配置
1. 在项目根目录创建 `.env` 文件，内容如下：
   ```
   TRIPO_API_KEY=你的Tripo3D_API_Key
   ```
2. 或在 `mcp.json` 的 `env` 字段中配置。

## mcp.json 示例
如需在 Cursor 或本地自动运行服务，项目根目录新建 `mcp.json`，内容如下（可参考 mcp.json.example）：
```json
{
  "mcpServers": {
    "tripo-mcp": {
      "command": "python",
      "args": [
        "你的绝对路径/src/main.py"
      ],
      "env": {
        "TRIPO_API_KEY": "你的Tripo3D_API_Key"
      },
      "url": "http://localhost:5001/mcp"
    }
  }
}
```

## 启动
```bash
python src/main.py
```
或通过 Cursor/CLI 工具自动调用（mcp.json 配置好后，Cursor 会自动启动服务）。

## 用法示例（自然语言）
- "用文本生成一个卡通小猫的3D模型"
- "将这张图片转成3D模型，风格为写实"
- "查询当前账户的 Tripo3D API 余额"
- "把模型转换为 FBX 格式并下载"
- "给这个模型加上动画骨骼并导出"
- "查询任务ID为 xxx 的处理进度"

只需用中文或英文描述你的目标，工具会自动解析并调用对应的 Tripo3D 能力。

## 常见问题
- **API Key 未生效**：请检查 .env 文件或 mcp.json 中的环境变量是否正确加载。
- **依赖安装失败**：建议使用 Python 3.8+，并确保 pip 源可用。
- **网络异常**：请确保本地网络可访问 Tripo3D 官方 API。

## 参考
- [Tripo3D 官方文档](https://platform.tripo3d.ai/docs)

---

如有问题欢迎提 Issue 或 PR！ 