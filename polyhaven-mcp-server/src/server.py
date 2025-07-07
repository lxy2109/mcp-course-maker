"""
Poly Haven MCP Server - 提供与Poly Haven 3D资产API交互的工具
"""
import os
import json
import httpx
import asyncio
from pathlib import Path
from typing import List, Optional, Dict, Any, Union, Literal
from urllib.parse import urljoin, urlparse
from dotenv import load_dotenv
from pydantic import BaseModel, Field
from mcp.server.fastmcp import FastMCP

# 加载环境变量
load_dotenv()

# 初始化MCP服务器
mcp = FastMCP("Poly Haven MCP Server", log_level="INFO")

# Poly Haven API配置
BASE_URL = "https://api.polyhaven.com"
API_KEY = os.getenv("POLYHAVEN_API_KEY")
DOWNLOAD_PATH = os.getenv("DOWNLOAD_PATH", "./downloads")

# 确保下载目录存在
Path(DOWNLOAD_PATH).mkdir(parents=True, exist_ok=True)

# 支持的资产类型
ASSET_TYPES = ["models", "materials", "hdris", "textures"]

# 支持的文件格式
MODEL_FORMATS = ["glb", "fbx", "obj", "blend"]
MATERIAL_FORMATS = ["jpg", "png", "exr"]
HDRI_FORMATS = ["jpg", "png", "exr"]
TEXTURE_FORMATS = ["jpg", "png", "exr"]

# 支持的分辨率
RESOLUTIONS = ["1k", "2k", "4k", "8k"]

# -------------------- 数据模型定义 --------------------

class SearchParams(BaseModel):
    type: Literal["models", "materials", "hdris", "textures"] = Field(..., description="资产类型")
    q: Optional[str] = Field(None, description="搜索关键词")
    categories: Optional[List[str]] = Field(None, description="分类过滤")
    tags: Optional[List[str]] = Field(None, description="标签过滤")
    limit: Optional[int] = Field(10, description="返回结果数量限制")
    offset: Optional[int] = Field(0, description="结果偏移量")

class AssetInfoParams(BaseModel):
    type: Literal["models", "materials", "hdris", "textures"] = Field(..., description="资产类型")
    slug: str = Field(..., description="资产唯一标识符")

class DownloadParams(BaseModel):
    type: Literal["models", "materials", "hdris", "textures"] = Field(..., description="资产类型")
    slug: str = Field(..., description="资产唯一标识符")
    format: Optional[str] = Field(None, description="文件格式")
    resolution: Optional[str] = Field(None, description="分辨率")

class CategoriesParams(BaseModel):
    type: Literal["models", "materials", "hdris", "textures"] = Field(..., description="资产类型")

class AssetFilesParams(BaseModel):
    type: Literal["models", "materials", "hdris", "textures"] = Field(..., description="资产类型")
    slug: str = Field(..., description="资产唯一标识符")

# -------------------- 工具实现 --------------------

def get_headers() -> Dict[str, str]:
    """获取请求头"""
    headers = {"Accept": "application/json"}
    if API_KEY:
        headers["Authorization"] = f"Bearer {API_KEY}"
    return headers

@mcp.tool(description="搜索Poly Haven资产，支持按类型、关键词、分类等条件过滤（本地二次筛选，规避API搜索bug）")
async def search_assets(params: SearchParams) -> Dict[str, Any]:
    """
    搜索Poly Haven资产（本地二次筛选）
    1. 仅用type参数获取全部数据（limit=10000）
    2. 本地遍历筛选：先按categories（用get_categories获取合法分类），再按tags
    """
    url = f"{BASE_URL}/assets"
    # 只用type参数，limit设大，不传categories
    query_params = {
        "type": params.type,
        "limit": 10000,
        "offset": 0
    }

    async with httpx.AsyncClient() as client:
        response = await client.get(url, headers=get_headers(), params=query_params)
        response.raise_for_status()
        all_assets = response.json()

    # 获取合法分类
    valid_categories = None
    if params.categories:
        # 动态import防止循环依赖
        import sys
        import importlib
        module = sys.modules[__name__]
        get_categories_func = getattr(module, 'get_categories')
        cats = await get_categories_func(CategoriesParams(type=params.type))
        valid_categories = set(cats.keys()) if isinstance(cats, dict) else set()

    # 本地二次筛选
    results = []
    for slug, asset in all_assets.items():
        # 先按categories
        if params.categories:
            asset_cats = set(asset.get("categories", []))
            if not set(params.categories).issubset(asset_cats):
                continue
            if valid_categories and not asset_cats.intersection(valid_categories):
                continue
        # 再按tags
        if params.tags:
            if not set(params.tags).issubset(set(asset.get("tags", []))):
                continue
        # 再按q（关键词，查name/description/tags）
        if params.q:
            q_lower = params.q.lower()
            name = asset.get("name", "").lower()
            desc = asset.get("description", "").lower() if asset.get("description") else ""
            tags = [t.lower() for t in asset.get("tags", [])]
            if not (q_lower in name or q_lower in desc or q_lower in tags):
                continue
        results.append({"slug": slug, **asset})
        if params.limit and len(results) >= params.limit:
            break
    return {"results": results}

@mcp.tool(description="获取指定资产的详细信息")
async def get_asset_info(params: AssetInfoParams) -> Dict[str, Any]:
    """
    获取资产详细信息
    
    Args:
        params: 包含资产类型和slug的参数
        
    Returns:
        资产的详细信息
    """
    url = f"{BASE_URL}/assets/{params.type}/{params.slug}"
    
    async with httpx.AsyncClient() as client:
        response = await client.get(url, headers=get_headers())
        response.raise_for_status()
        return response.json()

@mcp.tool(description="下载指定资产的文件到本地（仅支持Poly Haven官方下载直链）")
async def download_asset(params: DownloadParams) -> Dict[str, Any]:
    """
    仅通过拼接Poly Haven官方下载直链下载资产文件
    Args:
        params: 包含资产类型、slug、格式、分辨率等参数
    Returns:
        包含下载文件路径的对象
    """
    # 仅支持HDRI、材质、纹理、模型的官方下载直链
    if params.type == "hdris":
        format_type = params.format or "exr"
        resolution = params.resolution or "4k"
        download_url = f"https://dl.polyhaven.org/file/ph-assets/HDRIs/{format_type}/{resolution}/{params.slug}_{resolution}.{format_type}"
    elif params.type == "models":
        format_type = params.format or "glb"
        resolution = params.resolution or "2k"
        download_url = f"https://dl.polyhaven.org/file/ph-assets/3D%20Models/{format_type}/{resolution}/{params.slug}_{resolution}.{format_type}"
    elif params.type == "materials":
        format_type = params.format or "jpg"
        resolution = params.resolution or "4k"
        download_url = f"https://dl.polyhaven.org/file/ph-assets/Materials/{format_type}/{resolution}/{params.slug}_{resolution}.{format_type}"
    elif params.type == "textures":
        format_type = params.format or "jpg"
        resolution = params.resolution or "4k"
        download_url = f"https://dl.polyhaven.org/file/ph-assets/Textures/{format_type}/{resolution}/{params.slug}_{resolution}.{format_type}"
    else:
        raise ValueError("不支持的资产类型")

    # 直接通过download_by_url工具下载
    return await download_by_url(download_url)

@mcp.tool(description="获取指定资产类型的所有分类信息")
async def get_categories(params: CategoriesParams) -> Dict[str, Any]:
    """
    获取分类信息
    
    Args:
        params: 包含资产类型的参数
        
    Returns:
        分类信息列表
    """
    url = f"{BASE_URL}/categories/{params.type}"
    
    async with httpx.AsyncClient() as client:
        response = await client.get(url, headers=get_headers())
        response.raise_for_status()
        return response.json()

@mcp.tool(description="直接通过URL下载文件到本地（不经过API）")
async def download_by_url(url: str, filename: Optional[str] = None) -> Dict[str, Any]:
    """
    直接通过URL下载文件到本地DOWNLOAD_PATH目录，无需API调用。
    Args:
        url: 资源的真实下载链接
        filename: 保存到本地的文件名（可选，默认用URL中的文件名）
    Returns:
        包含本地文件路径、文件大小、下载链接等信息
    """
    # 解析文件名
    if not filename:
        filename = os.path.basename(urlparse(url).path)
    file_path = Path(DOWNLOAD_PATH) / filename

    async with httpx.AsyncClient() as client:
        response = await client.get(url)
        response.raise_for_status()
        with open(file_path, 'wb') as f:
            f.write(response.content)
    return {
        "file_path": str(file_path),
        "file_size": len(response.content),
        "download_url": url
    }

# -------------------- 资源定义 --------------------

@mcp.resource("polyhaven://assets/{asset_type}/{slug}")
async def get_asset_resource(asset_type: str, slug: str) -> Dict[str, Any]:
    """
    获取资产资源
    
    Args:
        asset_type: 资产类型
        slug: 资产标识符
        
    Returns:
        资产信息
    """
    if asset_type not in ASSET_TYPES:
        raise ValueError(f"不支持的资产类型: {asset_type}")
    
    return await get_asset_info(AssetInfoParams(type=asset_type, slug=slug))

@mcp.resource("polyhaven://categories/{asset_type}")
async def get_categories_resource(asset_type: str) -> Dict[str, Any]:
    """
    获取分类资源
    
    Args:
        asset_type: 资产类型
        
    Returns:
        分类信息
    """
    if asset_type not in ASSET_TYPES:
        raise ValueError(f"不支持的资产类型: {asset_type}")
    
    return await get_categories(CategoriesParams(type=asset_type))

# -------------------- 提示词定义 --------------------

@mcp.prompt()
def polyhaven_usage_guide() -> str:
    """Poly Haven MCP服务器使用指南"""
    return """
# Poly Haven MCP服务器使用指南

## 可用工具

### 1. 搜索资产 (`search_assets`)
搜索Poly Haven上的3D资产，支持多种过滤条件：
- **type**: 资产类型 (models, materials, hdris, textures)
- **q**: 搜索关键词
- **categories**: 分类过滤
- **tags**: 标签过滤
- **limit**: 结果数量限制
- **offset**: 结果偏移量

### 2. 获取资产信息 (`get_asset_info`)
获取指定资产的详细信息：
- **type**: 资产类型
- **slug**: 资产唯一标识符

### 3. 下载资产 (`download_asset`)
下载资产文件到本地：
- **type**: 资产类型
- **slug**: 资产唯一标识符
- **format**: 文件格式（可选）
- **resolution**: 分辨率（可选）

### 4. 获取分类 (`get_categories`)
获取指定资产类型的所有分类：
- **type**: 资产类型

### 5. 获取文件列表 (`get_asset_files`)
获取指定资产的所有可用文件：
- **type**: 资产类型
- **slug**: 资产唯一标识符

### 6. 获取API状态 (`get_api_status`)
检查Poly Haven API的状态

## 支持的资产类型

- **models**: 3D模型 (支持 glb, fbx, obj, blend 格式)
- **materials**: 材质 (支持 jpg, png, exr 格式)
- **hdris**: HDRI环境贴图 (支持 jpg, png, exr 格式)
- **textures**: 纹理 (支持 jpg, png, exr 格式)

## 支持的分辨率

- 1k, 2k, 4k, 8k

## 使用示例

1. 搜索椅子模型：
   ```python
   search_assets(type="models", q="chair", categories=["furniture"], limit=5)
   ```

2. 获取特定模型信息：
   ```python
   get_asset_info(type="models", slug="chair_01")
   ```

3. 下载GLB格式的2K分辨率模型：
   ```python
   download_asset(type="models", slug="chair_01", format="glb", resolution="2k")
   ```

## 注意事项

- 下载的文件会保存到 `DOWNLOAD_PATH` 环境变量指定的目录（默认为 `./downloads`）
- 某些资产可能需要API密钥才能访问
- 建议优先使用GLB格式，因为它包含了材质和纹理信息
"""

# -------------------- 主函数 --------------------

if __name__ == "__main__":
    print("启动Poly Haven MCP服务器...")
    mcp.run() 