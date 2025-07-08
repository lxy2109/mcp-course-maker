# Picui 图床 MCP Server

本项目是基于 [PICUI 图床](https://picui.cn/) 的多功能 API 封装，支持用户资料、策略列表、Token 生成、图片上传、图片列表、删除图片、相册列表、删除相册等操作，适用于 MCP 智能体工具集成。

## 依赖环境
- Python 3.8+
- 依赖包见 `requirements.txt`

## 快速开始
1. 安装依赖：
   ```bash
   pip install -r requirements.txt
   ```
2. 配置 `mcpjson`（见下方示例）
3. 运行服务：
   ```bash
   python server.py
   ```

## mcpjson 配置示例
见 `mcpjson.example` 文件：
```json
{
    "mcpServers":{
        "Picui": {
            "command": "picui-mcp-server",
            "env": {
                "PICUI_TOKEN": "your_token"
            }
        }
    }
}
```
- `PICUI_TOKEN`：你的 Bearer Token，可在 Picui 个人中心获取。

## 工具功能
- 所有功能统一通过 `picui_api` 工具入口调用，action 参数区分操作类型。
- 支持：
  - 用户资料查询（get_profile）
  - 策略列表（get_strategies）
  - Token 生成（generate_token）
  - 图片上传（upload_image，支持本地文件）
  - 图片列表（list_images）
  - 删除图片（delete_image）
  - 相册列表（list_albums）
  - 删除相册（delete_album）

## 参数说明

`picui_api` 工具参数为 `PicuiApiParams`，主要字段如下：

| 参数名                | 类型      | 说明                                               |
|----------------------|-----------|----------------------------------------------------|
| action               | str       | 操作类型，见下表                                   |
| file_path            | str       | 本地图片路径（upload_image 必填，File 类型）        |
| token                | str       | 临时上传 Token（可选）                             |
| permission           | int       | 权限，1=公开，0=私有（可选）                       |
| strategy_id          | int       | 储存策略ID（可选）                                 |
| album_id             | int       | 相册ID（可选）                                     |
| expired_at           | str       | 图片过期时间(yyyy-MM-dd HH:mm:ss)（可选）          |
| num                  | int       | 生成Token数量（generate_token 必填）               |
| seconds              | int       | Token有效期(秒)（generate_token 必填）             |
| key                  | str       | 图片唯一密钥（delete_image 必填）                  |
| album_id_to_delete   | int       | 要删除的相册ID（delete_album 必填）                |
| 其它                 |           | 详见 server.py 注释                                |

**action 可选值及功能：**
- get_profile：获取用户资料
- get_strategies：获取策略列表
- generate_token：生成临时上传Token
- upload_image：上传图片（本地文件）
- list_images：获取图片列表
- delete_image：删除图片
- list_albums：获取相册列表
- delete_album：删除相册

## 使用示例

### 上传本地图片
```python
from server import picui_api, PicuiApiParams
import asyncio

params = PicuiApiParams(
    action='upload_image',
    file_path='test.jpg',  # 本地图片路径
    permission=1           # 可选，1=公开，0=私有
)
result = asyncio.run(picui_api(params))
print(result)
# result.data['links']['url'] 即为公网访问url
```

### 生成Token
```python
params = PicuiApiParams(action='generate_token', num=1, seconds=3600)
result = asyncio.run(picui_api(params))
print(result)
```

### 获取图片列表
```python
params = PicuiApiParams(action='list_images', page=1, order='newest')
result = asyncio.run(picui_api(params))
print(result)
```

### 删除图片
```python
params = PicuiApiParams(action='delete_image', key='图片key')
result = asyncio.run(picui_api(params))
print(result)
```

### 其它操作
其它 action 用法类似，详见 `server.py` 注释和类型定义。

## 返回值类型
所有返回值均为 Pydantic 模型对象，字段与 Picui 官方 API 保持一致，详见 `server.py` 类型定义。

## 许可证
MIT 