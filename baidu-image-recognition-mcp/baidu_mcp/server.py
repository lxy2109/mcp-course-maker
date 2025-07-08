# -*- coding: utf-8 -*-
"""
百度图像识别 MCP 服务
支持通用物体识别、场景识别、动物识别、植物识别等多种图像识别功能
"""

import os
import base64
import json
import logging
from typing import Optional, Dict, Any, List, Union, Literal
from pydantic import BaseModel, Field
from fastmcp import FastMCP
from dotenv import load_dotenv
import httpx
import hashlib
import time
import urllib.parse
from datetime import datetime, timedelta
from urllib.parse import urlparse
import requests

# 加载环境变量
load_dotenv()

# 配置
BAIDU_API_KEY = os.getenv("BAIDU_API_KEY")
BAIDU_SECRET_KEY = os.getenv("BAIDU_SECRET_KEY")
BAIDU_BASE_URL = "https://aip.baidubce.com"

# 访问令牌缓存
access_token_cache = {
    "token": None,
    "expires_at": None
}

# -------------------- 类型定义 --------------------

class BaiduImageRecognitionParams(BaseModel):
    """百度图像识别参数"""
    image_path: str = Field(..., description="图片文件路径或图片URL")
    recognition_type: Literal[
        "general_basic",  # 通用物体识别（标准版）
        "general",        # 通用物体识别（高精度版）
        "advanced_general", # 通用物体和场景识别（高精度含位置版）
        "object_detect",  # 图像主体检测
        "animal",         # 动物识别
        "plant",          # 植物识别
        "logo",           # logo识别
        "dish",           # 菜品识别
        "car",            # 车型识别
        "landmark",       # 地标识别
        "ingredient",     # 果蔬识别
        "red_wine",       # 红酒识别
        "currency",       # 货币识别
    ] = Field("general_basic", description="识别类型")
    
    # 通用参数
    top_num: Optional[int] = Field(None, description="返回预测得分top结果数，默认为5")
    baike_num: Optional[int] = Field(None, description="返回百科信息的结果数，默认不返回")
    
    # 动物识别特有参数
    with_face: Optional[bool] = Field(None, description="是否显示动物的脸部信息，默认false")
    
    # 植物识别特有参数  
    baike_version: Optional[str] = Field(None, description="百科版本号，植物识别专用")

class BaiduRecognitionResponse(BaseModel):
    """百度识别响应"""
    success: bool
    data: Optional[Dict[str, Any]] = None
    error: Optional[str] = None
    recognition_type: str
    image_info: Optional[Dict[str, Any]] = None

class BaiduImageRecognitionResult(BaseModel):
    """百度图像识别结果"""
    log_id: int
    result_num: int
    result: List[Dict[str, Any]]

class BaiduErrorResponse(BaseModel):
    """百度API错误响应"""
    error: str
    error_description: str
    error_code: int

class RecognitionType(BaseModel):
    """识别类型定义"""
    type: str = Field(..., description="识别类型代码")
    name: str = Field(..., description="识别类型名称")
    description: str = Field(..., description="识别类型描述")
    endpoint: str = Field(..., description="API端点")

# 创建FastMCP实例
mcp = FastMCP(
    "百度图像识别 MCP Server", 
    log_level="INFO",
    offerings=[{
        "name": "baidu_image_recognition",
        "description": "百度图像识别工具，支持多种类型的图像识别，包括通用物体识别、动物识别、植物识别、logo识别等。",
        "parameters": BaiduImageRecognitionParams
    }, {
        "name": "get_recognition_types",
        "description": "获取百度图像识别支持的所有识别类型及其说明",
        "parameters": None
    }]
)

# 配置日志
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# -------------------- 识别类型配置 --------------------

RECOGNITION_TYPES = {
    "general_basic": RecognitionType(
        type="general_basic",
        name="通用物体识别（标准版）",
        description="识别图片中的物体，返回物体名称及置信度",
        endpoint="/rest/2.0/image-classify/v2/advanced_general"
    ),
    "general": RecognitionType(
        type="general",
        name="通用物体识别（高精度版）",
        description="高精度识别图片中的物体，准确率更高",
        endpoint="/rest/2.0/image-classify/v2/advanced_general"
    ),
    "advanced_general": RecognitionType(
        type="advanced_general",
        name="通用物体和场景识别（高精度含位置版）",
        description="识别图片中的物体和场景，包含位置信息",
        endpoint="/rest/2.0/image-classify/v2/advanced_general"
    ),
    "object_detect": RecognitionType(
        type="object_detect",
        name="图像主体检测",
        description="检测并定位图片中的主体物体",
        endpoint="/rest/2.0/image-classify/v1/object_detect"
    ),
    "animal": RecognitionType(
        type="animal",
        name="动物识别",
        description="识别图片中的动物，支持8000+动物种类",
        endpoint="/rest/2.0/image-classify/v1/animal"
    ),
    "plant": RecognitionType(
        type="plant",
        name="植物识别",
        description="识别图片中的植物，支持2万+植物种类",
        endpoint="/rest/2.0/image-classify/v1/plant"
    ),
    "logo": RecognitionType(
        type="logo",
        name="Logo识别",
        description="识别图片中的品牌Logo，支持数万种Logo",
        endpoint="/rest/2.0/image-classify/v2/logo"
    ),
    "dish": RecognitionType(
        type="dish",
        name="菜品识别",
        description="识别图片中的菜品，返回菜品名称及卡路里信息",
        endpoint="/rest/2.0/image-classify/v2/dish"
    ),
    "car": RecognitionType(
        type="car",
        name="车型识别",
        description="识别图片中的车辆型号、品牌、年份等信息",
        endpoint="/rest/2.0/image-classify/v1/car"
    ),
    "landmark": RecognitionType(
        type="landmark",
        name="地标识别",
        description="识别图片中的地标建筑，返回地标名称及位置",
        endpoint="/rest/2.0/image-classify/v1/landmark"
    ),
    "ingredient": RecognitionType(
        type="ingredient",
        name="果蔬识别",
        description="识别图片中的水果和蔬菜种类",
        endpoint="/rest/2.0/image-classify/v1/classify/ingredient"
    ),
    "red_wine": RecognitionType(
        type="red_wine",
        name="红酒识别",
        description="识别图片中的红酒品牌和信息",
        endpoint="/rest/2.0/image-classify/v1/redwine"
    ),
    "currency": RecognitionType(
        type="currency",
        name="货币识别",
        description="识别图片中的货币面额和类型",
        endpoint="/rest/2.0/image-classify/v1/currency"
    )
}

# -------------------- 辅助函数 --------------------

async def get_access_token() -> str:
    """获取百度API访问令牌"""
    global access_token_cache
    
    # 检查缓存的令牌是否有效
    if (access_token_cache["token"] and 
        access_token_cache["expires_at"] and 
        datetime.now() < access_token_cache["expires_at"]):
        return access_token_cache["token"]
    
    if not BAIDU_API_KEY or not BAIDU_SECRET_KEY:
        raise ValueError("请设置 BAIDU_API_KEY 和 BAIDU_SECRET_KEY 环境变量")
    
    url = f"{BAIDU_BASE_URL}/oauth/2.0/token"
    params = {
        "grant_type": "client_credentials",
        "client_id": BAIDU_API_KEY,
        "client_secret": BAIDU_SECRET_KEY
    }
    
    async with httpx.AsyncClient() as client:
        response = await client.post(url, params=params)
        response.raise_for_status()
        
        data = response.json()
        if "access_token" in data:
            # 缓存令牌，设置过期时间为实际过期时间前5分钟
            expires_in = data.get("expires_in", 3600)
            access_token_cache["token"] = data["access_token"]
            access_token_cache["expires_at"] = datetime.now() + timedelta(seconds=expires_in - 300)
            
            logger.info(f"成功获取访问令牌，有效期: {expires_in}秒")
            return data["access_token"]
        else:
            raise RuntimeError(f"获取访问令牌失败: {data}")

def encode_image_to_base64(image_path: str) -> str:
    """将图片编码为base64格式"""
    if image_path.startswith(('http://', 'https://')):
        # 如果是URL，先下载图片
        response = requests.get(image_path)
        response.raise_for_status()
        return base64.b64encode(response.content).decode('utf-8')
    else:
        # 如果是本地文件
        if not os.path.exists(image_path):
            raise FileNotFoundError(f"图片文件不存在: {image_path}")
        
        with open(image_path, 'rb') as f:
            return base64.b64encode(f.read()).decode('utf-8')

def get_api_endpoint(recognition_type: str) -> str:
    """根据识别类型获取API端点"""
    endpoints = {
        "general_basic": "/rest/2.0/image-classify/v2/advanced_general",
        "general": "/rest/2.0/image-classify/v2/advanced_general", 
        "advanced_general": "/rest/2.0/image-classify/v2/advanced_general",
        "object_detect": "/rest/2.0/image-classify/v1/object_detect",
        "animal": "/rest/2.0/image-classify/v1/animal",
        "plant": "/rest/2.0/image-classify/v1/plant",
        "logo": "/rest/2.0/image-classify/v2/logo",
        "dish": "/rest/2.0/image-classify/v2/dish",
        "car": "/rest/2.0/image-classify/v1/car",
        "landmark": "/rest/2.0/image-classify/v1/landmark", 
        "ingredient": "/rest/2.0/image-classify/v1/classify/ingredient",
        "red_wine": "/rest/2.0/image-classify/v1/redwine",
        "currency": "/rest/2.0/image-classify/v1/currency",
    }
    return endpoints.get(recognition_type, endpoints["general_basic"])

def build_request_params(params: BaiduImageRecognitionParams, image_base64: str) -> Dict[str, Any]:
    """构建请求参数"""
    request_params = {
        "image": image_base64
    }
    
    # 添加通用参数
    if params.top_num is not None:
        request_params["top_num"] = params.top_num
    if params.baike_num is not None:
        request_params["baike_num"] = params.baike_num
    
    # 添加特定识别类型的参数
    if params.recognition_type == "animal" and params.with_face is not None:
        request_params["with_face"] = "true" if params.with_face else "false"
    
    if params.recognition_type == "plant" and params.baike_version is not None:
        request_params["baike_version"] = params.baike_version
    
    return request_params

# -------------------- MCP工具实现 --------------------

@mcp.tool(description="百度图像识别工具，支持多种类型的图像识别，包括通用物体识别、动物识别、植物识别、logo识别等。")
async def baidu_image_recognition(params: BaiduImageRecognitionParams) -> BaiduRecognitionResponse:
    """
    百度图像识别
    
    Args:
        params: 识别参数，包含图片路径、识别类型等
        
    Returns:
        BaiduRecognitionResponse: 识别结果
    """
    try:
        logger.info(f"开始图像识别，类型: {params.recognition_type}, 图片: {params.image_path}")
        
        # 获取访问令牌
        access_token = await get_access_token()
        
        # 编码图片
        image_base64 = encode_image_to_base64(params.image_path)
        
        # 构建请求
        endpoint = get_api_endpoint(params.recognition_type)
        url = f"{BAIDU_BASE_URL}{endpoint}"
        
        request_params = build_request_params(params, image_base64)
        
        # 发送请求
        async with httpx.AsyncClient(timeout=30.0) as client:
            response = await client.post(
                url,
                params={"access_token": access_token},
                headers={"Content-Type": "application/x-www-form-urlencoded"},
                data=urllib.parse.urlencode(request_params)
            )
            response.raise_for_status()
            result = response.json()
        
        # 检查API响应
        if "error_code" in result:
            error_msg = f"百度API错误 {result['error_code']}: {result.get('error_msg', '未知错误')}"
            logger.error(error_msg)
            return BaiduRecognitionResponse(
                success=False,
                error=error_msg,
                recognition_type=params.recognition_type
            )
        
        # 获取图片信息
        image_info = None
        if os.path.exists(params.image_path):
            stat = os.stat(params.image_path)
            image_info = {
                "file_size": stat.st_size,
                "file_path": params.image_path
            }
        elif params.image_path.startswith(('http://', 'https://')):
            image_info = {
                "image_url": params.image_path
            }
        
        logger.info(f"识别成功，结果数量: {len(result.get('result', []))}")
        
        return BaiduRecognitionResponse(
            success=True,
            data=result,
            recognition_type=params.recognition_type,
            image_info=image_info
        )
        
    except Exception as e:
        error_msg = f"图像识别失败: {str(e)}"
        logger.error(error_msg)
        return BaiduRecognitionResponse(
            success=False,
            error=error_msg,
            recognition_type=params.recognition_type
        )

@mcp.tool(description="获取百度图像识别支持的所有识别类型及其说明")
async def get_recognition_types() -> Dict[str, Any]:
    """获取支持的识别类型列表"""
    types_info = []
    for type_key, config in RECOGNITION_TYPES.items():
        types_info.append({
            "type": config.type,
            "name": config.name, 
            "description": config.description,
            "endpoint": config.endpoint
        })
    
    return {
        "success": True,
        "total_types": len(RECOGNITION_TYPES),
        "recognition_types": types_info,
        "timestamp": datetime.now().isoformat()
    }

# -------------------- 主函数 --------------------
def main():
    # 启动前检查配置
    if not BAIDU_API_KEY or not BAIDU_SECRET_KEY:
        logger.warning("警告: 未设置百度API密钥，请在环境变量中设置 BAIDU_API_KEY 和 BAIDU_SECRET_KEY")
    
    logger.info("百度图像识别 MCP 服务器启动...")
    logger.info(f"支持的识别类型数量: {len(RECOGNITION_TYPES)}")
    mcp.run()

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        logger.error(f"服务器启动失败: {str(e)}")
        raise