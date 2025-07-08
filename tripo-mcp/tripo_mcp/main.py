import os
import json
import httpx
from dotenv import load_dotenv
from pydantic import BaseModel, Field
from typing import Optional
import asyncio
try:
    from mcp import FastMCP
except ImportError:
    from mcp.server.fastmcp import FastMCP
from .models import (
    TextToModelRequest, ImageToModelRequest, MultiviewToModelRequest, TextureModelRequest,
    RefineModelRequest, AnimatePrerigcheckRequest, AnimateRigRequest, AnimateRetargetRequest,
    StylizeModelRequest, ConvertModelRequest, TaskIdRequest, UploadImageRequest
)
from .tripo_api import (
    text_to_model, image_to_model, multiview_to_model, texture_model, refine_model,
    animate_prerigcheck, animate_rig, animate_retarget, stylize_model, convert_model,
    get_task_status, upload_image, get_balance
)

# 加载环境变量
load_dotenv()

TRIPO_API_KEY = os.getenv("TRIPO_API_KEY")
if not TRIPO_API_KEY:
    raise ValueError("TRIPO_API_KEY 环境变量未设置")

mcp = FastMCP("Tripo3D MCP Server", log_level="ERROR")

class CreateTaskRequest(BaseModel):
    prompt: str = Field(..., description="3D模型描述")
    face_limit: int = Field(8000, description="面数限制")

class TaskStatusRequest(BaseModel):
    task_id: str = Field(..., description="任务ID")

class TaskResponse(BaseModel):
    id: str = Field(..., description="任务ID")
    result: Optional[dict] = Field(None, description="任务结果")

@mcp.tool(description=
    """
    文本转3D模型（text_to_model）。
    参数列表：
        - prompt (str, 必填)：3D模型描述，最大1024字符，支持多语言，不支持emoji和部分特殊字符。
        - negative_prompt (str, 可选)：反向描述，辅助生成与prompt相反的内容，最大255字符。
        - model_version (str, 可选，默认v2.5-20250123)：模型版本，可选：v2.5-20250123、v2.0-20240919、v1.4-20240625。
        - face_limit (int, 可选)：输出模型面数上限，未设置时自适应，仅v2.0及以上版本有效。
        - texture (bool, 可选，默认True)：是否生成贴图，仅v2.0及以上版本有效。
        - pbr (bool, 可选，默认True)：是否生成PBR贴图，若为True，texture自动为True。
        - image_seed (int, 可选)：prompt生成过程的随机种子。
        - model_seed (int, 可选)：模型生成随机种子，仅v2.0及以上版本有效。
        - texture_seed (int, 可选)：贴图生成随机种子，仅v2.0及以上版本有效。
        - texture_quality (str, 可选，默认standard)：贴图质量，可选：standard、detailed。
        - style (str, 可选)：风格化类型，详见官方文档。
        - auto_size (bool, 可选，默认False)：是否自动缩放到真实世界尺寸（米）。
        - quad (bool, 可选，默认False)：是否输出四边面网格，若为True且未设置face_limit，默认10000。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_text_to_model(request: TextToModelRequest):
    return await text_to_model(request)

@mcp.tool(description=
    """
    图片转3D模型（image_to_model）。
    参数列表：
        - file (object, 必填)：图片文件描述对象，包含：
            - type (str, 必填)：图片类型（如jpeg、png、webp）。
            - file_token (str, 可选)：图片上传后返回的token。与url、object互斥。
            - url (str, 可选)：图片公网直链（JPEG/PNG，最大20MB）。与file_token、object互斥。
            - object (dict, 可选)：STS上传信息。与url、file_token互斥。
        - model_version (str, 可选，默认v2.5-20250123)：模型版本。
        - face_limit (int, 可选)：输出模型面数上限。
        - texture (bool, 可选，默认True)：是否生成贴图。
        - pbr (bool, 可选，默认True)：是否生成PBR贴图。
        - model_seed (int, 可选)：模型生成随机种子。
        - texture_seed (int, 可选)：贴图生成随机种子。
        - texture_quality (str, 可选，默认standard)：贴图质量，可选：standard、detailed。
        - texture_alignment (str, 可选，默认original_image)：贴图对齐方式，可选：original_image、geometry。
        - style (str, 可选)：风格化类型。
        - auto_size (bool, 可选，默认False)：是否自动缩放到真实世界尺寸。
        - orientation (str, 可选，默认default)：模型朝向，可选：align_image、default。
        - quad (bool, 可选，默认False)：是否输出四边面网格。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_image_to_model(request: ImageToModelRequest):
    return await image_to_model(request)

@mcp.tool(description=
    """
    多视图转3D模型（multiview_to_model）。
    参数列表：
        - files (list, 必填)：长度为4的图片描述对象数组，顺序为[front, left, back, right]，每个对象：
        - type (str, 必填)：图片类型。
        - file_token (str, 必填)：图片上传后返回的token。
        - mode (str, 可选)：LEFT或RIGHT。
        - model_version (str, 可选，默认v2.5-20250123)：模型版本。
        - orthographic_projection (bool, 可选，默认False)：正交投影。
        - face_limit (int, 可选)：输出模型面数上限。
        - texture (bool, 可选，默认True)：是否生成贴图。
        - pbr (bool, 可选，默认True)：是否生成PBR贴图。
        - model_seed (int, 可选)：模型生成随机种子。
        - texture_seed (int, 可选)：贴图生成随机种子。
        - texture_quality (str, 可选，默认standard)：贴图质量，可选：standard、detailed。
        - texture_alignment (str, 可选，默认original_image)：贴图对齐方式，可选：original_image、geometry。
        - auto_size (bool, 可选，默认False)：是否自动缩放到真实世界尺寸。
        - orientation (str, 可选，默认default)：模型朝向，可选：align_image、default。
        - quad (bool, 可选，默认False)：是否输出四边面网格。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_multiview_to_model(request: MultiviewToModelRequest):
    return await multiview_to_model(request)

@mcp.tool(description=
    """
    模型贴图（texture_model）。
    主要参数：
        - original_model_task_id (str): 原始模型任务ID。
        - texture (bool, 可选): 是否生成贴图。
        - pbr (bool, 可选): 是否生成PBR贴图。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_texture_model(request: TextureModelRequest):
    return await texture_model(request)

@mcp.tool(description=
    """
    模型精修（refine_model）。
    主要参数：
        - draft_model_task_id (str): 草稿模型任务ID。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_refine_model(request: RefineModelRequest):
    return await refine_model(request)

@mcp.tool(description=
    """
    动画预检查（animate_prerigcheck）。
    主要参数：
        - original_model_task_id (str): 原始模型任务ID。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_animate_prerigcheck(request: AnimatePrerigcheckRequest):
    return await animate_prerigcheck(request)

@mcp.tool(description=
    """
    动画骨骼绑定（animate_rig）。
    主要参数：
        - original_model_task_id (str): 原始模型任务ID。
        - out_format (str, 可选): 输出文件格式。
        - topology (str, 可选): 骨骼拓扑结构。
        - spec (str, 可选): 骨骼规格。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_animate_rig(request: AnimateRigRequest):
    return await animate_rig(request)

@mcp.tool(description=
    """
    动画重定向（animate_retarget）。
    主要参数：
        - original_model_task_id (str): 骨骼绑定任务ID。
        - animation (str): 动画类型。
        - out_format (str, 可选): 输出文件格式。
        - bake_animation (bool, 可选): 是否烘焙动画。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_animate_retarget(request: AnimateRetargetRequest):
    return await animate_retarget(request)

@mcp.tool(description=
    """
    模型风格化。
    主要参数：
        - original_model_task_id (str): 原始模型任务ID。
        - style (str): 风格类型。
        - block_size (int, 可选): 网格大小，仅minecraft风格有效。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_stylize_model(request: StylizeModelRequest):
    return await stylize_model(request)

@mcp.tool(description=
    """
    模型格式转换（convert_model）。
    主要参数：
        - original_model_task_id (str): 原始模型任务ID。
        - format (str): 目标格式。
        - quad (bool, 可选): 是否四边面重拓扑。
        - face_limit (int, 可选): 输出模型面数上限。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_convert_model(request: ConvertModelRequest):
    return await convert_model(request)

@mcp.tool(description=
    """
    查询任务状态（get_task_status）。
    主要参数：
        - task_id (str): 任务ID。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_get_task_status(request: TaskIdRequest):
    return await get_task_status(request)

@mcp.tool(description=
    """
    上传图片，返回image_token（upload_image）。
    主要参数：
        - file_path (str): 本地图片路径。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_upload_image(request: UploadImageRequest):
    return await upload_image(request)

@mcp.tool(description=
    """
    查询API余额（get_balance）。
    无需参数。
    返回余额和冻结金额。
    详见Tripo3D官方文档。
    """
    )
async def tripo3d_get_balance():
    return await get_balance()

def main():
    #print("MCP server starting...")
    mcp.run()
if __name__ == "__main__":
    main()