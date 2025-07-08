# instant-meshes-mcp

高效3D模型自动减面与重拓扑服务，基于 Instant Meshes、pymeshlab、Blender 3.6，支持 MCP 协议批量处理 OBJ/GLB/FBX 模型，自动归档、材质贴图保留、质量分析。

---

## 1. 环境配置

### 1.1 系统要求

- **操作系统**：Windows 10/11
- **Python**：3.8 及以上
- **Blender**：3.6（必须安装，建议官方版或便携版）
- **Instant Meshes**：需将 `Instant Meshes.exe` 放在项目根目录

### 1.2 依赖安装

```bash
cd instant-meshes-mcp
pip install -e .
```

### 1.3 Blender 3.6 配置

- 推荐安装路径：  
  `C:\Program Files\Blender Foundation\Blender 3.6\blender.exe`
- 便携版/自定义路径：  
  `xxx\Blender\3.6\blender.exe`
- Steam版：  
  `C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe`

**环境变量设置（如自动检测失败）：**

```bash
set BLENDER_EXECUTABLE=xxx\Blender\3.6\blender.exe
```

---

## 2. 快速使用

### 2.1 启动服务

```bash
instant-meshes-mcp
# 或
python -m mesh_dec.server
```

### 2.2 MCP 客户端调用示例

```json
{
  "method": "tools/call",
  "params": {
    "name": "process_model",
    "arguments": {
      "input_model": "https://example.com/model.glb",
      "target_faces": 5000,
      "operation": "auto",
      "create_archive": true
    }
  }
}
```

### 2.3 mcp.json 配置示例

```json
{
  "mcpServers": {
    "instant-meshes-mcp": {
      "command": "instant-meshes-mcp",
      "env": {
        "PYTHONUNBUFFERED": "1",
        "BLENDER_PATH": "your_blender3.6.abs_dir"
      }
    }
  }
}
```

---

## 3. 主要功能

- **智能减面/重拓扑**：自动选择最佳方式，保护UV和贴图
- **批量处理**：支持文件夹、URL、归档管理
- **材质贴图保留**：自动处理MTL和贴图，兼容GLB/OBJ
- **质量分析**：面数、拓扑、破洞、建议等
- **自动归档**：结构化存储，便于追溯

---

## 4. 常见问题

### 4.1 Blender 检测失败

- 检查 Blender 3.6 是否安装，路径是否正确
- 设置 `BLENDER_EXECUTABLE` 环境变量
- 检查可执行文件权限

### 4.2 依赖安装失败

- 确认 Python 版本 >=3.8
- 使用国内镜像源加速 pip 安装

### 4.3 命令行无法启动

- 确认 `pip install -e .` 后 `instant-meshes-mcp` 命令已加入 PATH
- 或用 `python -m instant_meshes_mcp.server` 启动

### 4.4 归档/输出找不到

- 默认归档在 `archives/` 目录，输出在 `output/` 目录
- 日志在 `logs/`，临时文件自动清理

---

---

---

## 6. 依赖说明

- **trimesh**：3D模型格式转换和几何处理
- **pymeshlab**：网格简化和修复
- **requests**：远程文件下载
- **psutil**：进程管理
- **mcp**：MCP协议支持

---

## 7. 其他说明

- 仅支持 Windows 平台
- 所有临时文件自动清理，日志详细
- 支持自定义归档、批量处理、自动参数推荐
- 如遇问题请查阅日志或提交 issue
