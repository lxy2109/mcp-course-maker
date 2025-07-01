"""
Meshy AI MCP Server - Provides tools for interacting with Meshy AI's 3D generation API
"""
import os
print("MESHY_API_KEY:", os.getenv("MESHY_API_KEY"))
print("当前工作目录：", os.getcwd())
print("server.py 所在目录：", os.path.dirname(__file__))
import json
import httpx
import asyncio
from dotenv import load_dotenv
from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any, Union, Literal
from mcp.server.fastmcp import FastMCP

# Load environment variables from .env file
load_dotenv()

# Initialize the MCP server
mcp = FastMCP("Meshy AI MCP Server", log_level="ERROR")

# Get API key from environment
MESHY_API_KEY = os.getenv("MESHY_API_KEY")
if not MESHY_API_KEY:
    raise ValueError("MESHY_API_KEY environment variable is not set")

# Define Pydantic models for request/response validation
class TextTo3DTaskRequest(BaseModel):
    mode: str = Field(..., description="Task mode: 'preview' or 'refine'")
    prompt: str = Field(..., description="Text prompt describing the 3D model to generate")
    art_style: str = Field("realistic", description="Art style for the 3D model")
    should_remesh: bool = Field(True, description="Whether to remesh the model after generation")

class RemeshTaskRequest(BaseModel):
    input_task_id: str = Field(..., description="ID of the input task to remesh")
    target_formats: List[str] = Field(["glb", "fbx"], description="Target formats for the remeshed model")
    topology: str = Field("quad", description="Topology type: 'quad' or 'triangle'")
    target_polycount: int = Field(50000, description="目标面数，用于后期减面处理，可根据需要调整（100-300000）")
    resize_height: float = Field(1.0, description="Resize height for the remeshed model")
    origin_at: str = Field("bottom", description="Origin position: 'bottom', 'center', etc.")

class ImageTo3DTaskRequest(BaseModel):
    image_url: str = Field(..., description="提供一张用于模型生成的图像。支持公网可访问URL或Data URI，支持.jpg、.jpeg、.png格式")
    ai_model: Optional[str] = Field(None, description="要使用的模型ID，可选值：'meshy-4', 'meshy-5'，默认'meshy-4'")
    topology: Optional[str] = Field(None, description="生成模型的拓扑结构，可选值：'quad', 'triangle'，默认'triangle'")
    target_polycount: Optional[int] = Field(None, description="目标面数，100-300000，默认300000（最大值以保证模型完整性和水密性）")
    symmetry_mode: Optional[str] = Field(None, description="对称性控制，可选值：'off', 'auto', 'on'，默认'auto'")
    should_remesh: Optional[bool] = Field(None, description="是否启用重建网格阶段，默认True")
    should_texture: Optional[bool] = Field(None, description="是否生成贴图，默认True")
    enable_pbr: Optional[bool] = Field(None, description="是否生成PBR贴图，仅ai_model=meshy-4时支持，默认False")
    texture_prompt: Optional[str] = Field(None, description="贴图生成文本提示，最多600字符。should_texture为True时生效，与texture_image_url互斥")
    texture_image_url: Optional[str] = Field(None, description="贴图引导图片，公网直链或Data URI，仅ai_model=meshy-4时支持，should_texture为True时生效，与texture_prompt互斥")
    prompt: Optional[str] = Field(None, description="可选的文本提示，辅助生成（兼容旧参数）")

class TextToTextureTaskRequest(BaseModel):
    model_url: str = Field(..., description="URL of the 3D model to texture")
    object_prompt: str = Field(..., description="Text prompt describing the object")
    style_prompt: Optional[str] = Field(None, description="Text prompt describing the style")
    enable_original_uv: bool = Field(True, description="Whether to use original UV mapping")
    enable_pbr: bool = Field(True, description="Whether to enable PBR textures")
    resolution: str = Field("1024", description="Texture resolution")
    negative_prompt: Optional[str] = Field(None, description="Negative prompt to guide generation")
    art_style: str = Field("realistic", description="Art style for the texture")

class MultiImageTo3DTaskRequest(BaseModel):
    image_urls: List[str] = Field(..., description="1-4张图片URL或Data URI")
    ai_model: Optional[str] = Field("meshy-5", description="'meshy-5'，默认'meshy-5'")
    topology: Optional[str] = Field("triangle", description="'quad'/'triangle'，默认'triangle'")
    target_polycount: Optional[int] = Field(300000, description="100-300000，默认300000（最大值以保证模型完整性和水密性）")
    symmetry_mode: Optional[str] = Field("auto", description="'off'/'auto'/'on'，默认'auto'")
    should_remesh: Optional[bool] = Field(True, description="是否重建网格，默认true")
    should_texture: Optional[bool] = Field(True, description="是否生成贴图，默认true")
    texture_prompt: Optional[str] = Field(None, description="贴图文本提示")

class ListTasksParams(BaseModel):
    page_size: int = Field(10, description="Number of tasks to return per page")
    page: int = Field(1, description="Page number to return")

class TaskResponse(BaseModel):
    id: str = Field(..., description="Task ID")
    result: Optional[str] = Field(None, description="Task result (if available)")

@mcp.tool(
    description="""
    通用任务创建工具，已优化纹理生成质量。支持的 task_type 及 request 参数结构如下：
    - text-to-3d: TextTo3DTaskRequest
    - image-to-3d: ImageTo3DTaskRequest（自动使用最大面数300000，优化纹理设置，默认禁用PBR避免颜色异常）
    - multi-image-to-3d: MultiImageTo3DTaskRequest（自动使用最大面数300000保证模型完整性和水密性）
    - remesh: RemeshTaskRequest（用于后期减面处理）
    - text-to-texture: TextToTextureTaskRequest（自动优化纹理设置，添加负面提示避免颜色失真）
    每种类型的参数详见各模型定义。建模任务会优先保证模型质量，纹理任务已优化避免颜色异常。
    """
)
async def meshy3d_create_task(
    task_type: str,
    request: Union[
        TextTo3DTaskRequest,
        ImageTo3DTaskRequest,
        MultiImageTo3DTaskRequest,
        RemeshTaskRequest,
        TextToTextureTaskRequest,
        dict
    ]
) -> TaskResponse:
    """
    通用任务创建工具，已集成纹理质量优化。
    Args:
        task_type (str): 任务类型。可选值：
            'text-to-3d'：文本生成3D（预览/精细化），参数见 TextTo3DTaskRequest
            'image-to-3d'：单图像转3D，参数见 ImageTo3DTaskRequest
                - 自动使用最大面数300000保证模型完整性
                - 自动禁用PBR避免纹理颜色异常
                - 使用稳定的meshy-4模型
                - 智能生成纹理提示
            'multi-image-to-3d'：多图像转3D，参数见 MultiImageTo3DTaskRequest（自动使用最大面数300000保证模型完整性）
            'remesh'：重建网格，参数见 RemeshTaskRequest（用于后期减面处理）
            'text-to-texture'：文本生成纹理，参数见 TextToTextureTaskRequest
                - 自动禁用PBR避免颜色失真
                - 保持原始UV映射
                - 添加负面提示避免颜色异常
                - 智能设置风格描述
        request: 任务参数对象，类型随 task_type 变化，详见各模型定义。
                所有任务类型都已自动优化，无需手动设置复杂参数。
    Returns:
        TaskResponse: 包含任务ID的响应对象。
    """
    headers = {"Authorization": f"Bearer {MESHY_API_KEY}"}
    url_map = {
        "text-to-3d": "https://api.meshy.ai/openapi/v2/text-to-3d",
        "image-to-3d": "https://api.meshy.ai/openapi/v1/image-to-3d",
        "multi-image-to-3d": "https://api.meshy.ai/openapi/v1/multi-image-to-3d",
        "remesh": "https://api.meshy.ai/openapi/v1/remesh",
        "text-to-texture": "https://api.meshy.ai/openapi/v1/text-to-texture"
    }
    if task_type not in url_map:
        raise ValueError(f"不支持的任务类型: {task_type}")
    url = url_map[task_type]
    # 自动将 dict 转为 Pydantic 对象
    if isinstance(request, dict):
        model_map = {
            "text-to-3d": TextTo3DTaskRequest,
            "image-to-3d": ImageTo3DTaskRequest,
            "multi-image-to-3d": MultiImageTo3DTaskRequest,
            "remesh": RemeshTaskRequest,
            "text-to-texture": TextToTextureTaskRequest
        }
        if task_type not in model_map:
            raise ValueError(f"不支持的任务类型: {task_type}")
        request = model_map[task_type](**request)
    
    # 为建模任务设置最大面数以保证模型完整性和水密性
    if hasattr(request, 'target_polycount') and task_type in ["image-to-3d", "multi-image-to-3d"]:
        if request.target_polycount is None:
            request.target_polycount = 300000  # 设置为最大值
    
    # 优化纹理生成设置以提高贴图质量
    if task_type == "image-to-3d" and hasattr(request, 'enable_pbr'):
        # 如果没有明确设置PBR，默认禁用以避免纹理问题
        if request.enable_pbr is None:
            request.enable_pbr = False
        # 确保使用更稳定的AI模型
        if request.ai_model is None:
            request.ai_model = "meshy-4"  # meshy-4在纹理方面更稳定
        # 如果没有设置纹理提示但有主提示，优化纹理生成
        if hasattr(request, 'texture_prompt') and request.texture_prompt is None and request.prompt:
            request.texture_prompt = f"high quality realistic texture, clean surface, {request.prompt}, avoid color distortion"
        # 优化对称性设置
        if request.symmetry_mode is None:
            request.symmetry_mode = "auto"
    
    # 优化text-to-texture任务的默认设置
    if task_type == "text-to-texture" and hasattr(request, 'enable_pbr'):
        # 为纹理任务禁用PBR以避免颜色异常
        if request.enable_pbr is None:
            request.enable_pbr = False
        # 确保使用原始UV映射
        if hasattr(request, 'enable_original_uv') and request.enable_original_uv is None:
            request.enable_original_uv = True
        # 如果没有提供负面提示，添加默认的负面提示
        if hasattr(request, 'negative_prompt') and request.negative_prompt is None:
            request.negative_prompt = "distorted colors, green tint, purple tint, blue tint, oversaturated, unrealistic colors, damaged surface, color artifacts"
        # 如果没有风格提示，添加默认的清洁风格
        if hasattr(request, 'style_prompt') and request.style_prompt is None:
            request.style_prompt = "clean realistic surface texture, maintain natural colors, professional appearance"
    
    body = request.model_dump(exclude_none=True) if hasattr(request, 'model_dump') else dict(request)
    
    async with httpx.AsyncClient() as client:
        try:
            response = await client.post(url, headers=headers, json=body)
            print(f"响应状态码: {response.status_code}")
            print(f"响应头: {dict(response.headers)}")
            response.raise_for_status()
            data = response.json()
            task_id = data.get("id") or data.get("result")
            if not task_id:
                raise ValueError(f"接口返回异常：{data}")
            return TaskResponse(id=task_id, result=task_id)
        except httpx.HTTPStatusError as e:
            print(f"HTTP错误: {e.response.status_code}")
            print(f"错误响应内容: {e.response.text}")
            raise

@mcp.tool()
async def meshy3d_get_balance() -> Dict[str, Any]:
    """
    获取当前Meshy AI账户余额。
    Args:
        无
    Returns:
        dict: 余额信息。
    """
    headers = {"Authorization": f"Bearer {MESHY_API_KEY}"}
    async with httpx.AsyncClient() as client:
        response = await client.get(
            "https://api.meshy.ai/openapi/v1/balance",
            headers=headers
        )
        response.raise_for_status()
        return response.json()

# 合并后的通用工具
@mcp.tool()
async def meshy3d_retrieve_task(task_type: str, task_id: str) -> Dict[str, Any]:
    """
    通用任务详情查询工具。
    Args:
        task_type (str): 任务类型，如 'text-to-3d', 'image-to-3d', 'remesh', 'text-to-texture'。
        task_id (str): 任务ID。
    Returns:
        dict: 任务详情，包括状态、结果等。
    """
    headers = {"Authorization": f"Bearer {MESHY_API_KEY}"}
    url_map = {
        "text-to-3d": "https://api.meshy.ai/openapi/v2/text-to-3d/{}",
        "image-to-3d": "https://api.meshy.ai/openapi/v1/image-to-3d/{}",
        "remesh": "https://api.meshy.ai/openapi/v1/remesh/{}",
        "text-to-texture": "https://api.meshy.ai/openapi/v1/text-to-texture/{}"
    }
    if task_type not in url_map:
        raise ValueError(f"不支持的任务类型: {task_type}")
    url = url_map[task_type].format(task_id)
    async with httpx.AsyncClient() as client:
        response = await client.get(url, headers=headers)
        response.raise_for_status()
        return response.json()

@mcp.tool()
async def meshy3d_list_tasks(task_type: str, params: Optional[ListTasksParams] = None) -> List[Dict[str, Any]]:
    """
    通用任务列表查询工具。
    Args:
        task_type (str): 任务类型，如 'text-to-3d', 'image-to-3d', 'remesh', 'text-to-texture'。
        params (ListTasksParams, optional): 查询参数。
    Returns:
        list: 任务列表。
    """
    headers = {"Authorization": f"Bearer {MESHY_API_KEY}"}
    url_map = {
        "text-to-3d": "https://api.meshy.ai/openapi/v2/text-to-3d",
        "image-to-3d": "https://api.meshy.ai/openapi/v1/image-to-3d",
        "remesh": "https://api.meshy.ai/openapi/v1/remesh",
        "text-to-texture": "https://api.meshy.ai/openapi/v1/text-to-texture"
    }
    if task_type not in url_map:
        raise ValueError(f"不支持的任务类型: {task_type}")
    url = url_map[task_type]
    query_params = params.model_dump(exclude_none=True) if params else {}
    async with httpx.AsyncClient() as client:
        response = await client.get(url, headers=headers, params=query_params)
        response.raise_for_status()
        return response.json()

@mcp.tool()
async def meshy3d_stream_task(task_type: str, task_id: str, timeout: int = 300) -> Dict[str, Any]:
    """
    通用流式任务进度查询工具。
    Args:
        task_type (str): 任务类型，如 'text-to-3d', 'image-to-3d', 'remesh', 'text-to-texture'。
        task_id (str): 任务ID。
        timeout (int, optional): 超时时间（秒），默认300。
    Returns:
        dict: 任务最终状态和结果。
    """
    headers = {"Authorization": f"Bearer {MESHY_API_KEY}", "Accept": "text/event-stream"}
    url_map = {
        "text-to-3d": "https://api.meshy.ai/openapi/v2/text-to-3d/{}/stream",
        "image-to-3d": "https://api.meshy.ai/openapi/v1/image-to-3d/{}/stream",
        "remesh": "https://api.meshy.ai/openapi/v1/remesh/{}/stream",
        "text-to-texture": "https://api.meshy.ai/openapi/v1/text-to-texture/{}/stream"
    }
    if task_type not in url_map:
        raise ValueError(f"不支持的任务类型: {task_type}")
    url = url_map[task_type].format(task_id)
    try:
        async with httpx.AsyncClient() as client:
            async with client.stream("GET", url, headers=headers, timeout=timeout) as response:
                response.raise_for_status()
                final_data = None
                async for line in response.aiter_lines():
                    if line.startswith("data:"):
                        data = json.loads(line[5:])
                        final_data = data
                        if data.get("status") in ["SUCCEEDED", "FAILED", "CANCELED"]:
                            break
                return final_data or {"error": "未收到完整流数据，可能任务异常中断"}
    except httpx.IncompleteRead:
        return {"error": "流式连接被提前关闭（incomplete chunked read），任务可能失败或被取消。建议用 meshy3d_retrieve_task 查询任务状态。"}
    except Exception as e:
        return {"error": f"发生异常：{str(e)}"}

@mcp.resource("health://status")
def health_check() -> Dict[str, str]:
    """Health check endpoint"""
    return {
        "status": "ok",
        "api_key_configured": bool(MESHY_API_KEY)
    }

# Add task resources for each task type
@mcp.resource("task://text-to-3d/{task_id}")
async def get_text_to_3d_task(task_id: str) -> Dict[str, Any]:
    """
    Get a Text to 3D task by its ID.
    
    This resource allows you to access task details and results.
    """
    headers = {
        "Authorization": f"Bearer {MESHY_API_KEY}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"https://api.meshy.ai/openapi/v2/text-to-3d/{task_id}",
            headers=headers
        )
        response.raise_for_status()
        return response.json()

@mcp.resource("task://image-to-3d/{task_id}")
async def get_image_to_3d_task(task_id: str) -> Dict[str, Any]:
    """
    Get an Image to 3D task by its ID.
    
    This resource allows you to access task details and results.
    """
    headers = {
        "Authorization": f"Bearer {MESHY_API_KEY}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"https://api.meshy.ai/openapi/v1/image-to-3d/{task_id}",
            headers=headers
        )
        response.raise_for_status()
        return response.json()

@mcp.resource("task://remesh/{task_id}")
async def get_remesh_task(task_id: str) -> Dict[str, Any]:
    """
    Get a Remesh task by its ID.
    
    This resource allows you to access task details and results.
    """
    headers = {
        "Authorization": f"Bearer {MESHY_API_KEY}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"https://api.meshy.ai/openapi/v1/remesh/{task_id}",
            headers=headers
        )
        response.raise_for_status()
        return response.json()

@mcp.resource("task://text-to-texture/{task_id}")
async def get_text_to_texture_task(task_id: str) -> Dict[str, Any]:
    """
    Get a Text to Texture task by its ID.
    
    This resource allows you to access task details and results.
    """
    headers = {
        "Authorization": f"Bearer {MESHY_API_KEY}"
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"https://api.meshy.ai/openapi/v1/text-to-texture/{task_id}",
            headers=headers
        )
        response.raise_for_status()
        return response.json()

if __name__ == "__main__":
    # Start the MCP server
    mcp.run()