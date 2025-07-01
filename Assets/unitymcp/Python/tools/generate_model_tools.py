from mcp.server.fastmcp import FastMCP, Context
from typing import List, Dict, Any, Optional
import json
from unity_connection import get_unity_connection

@mcp.tool()
def generate_3d_model(
    ctx: Context,
    prompt: str,
    randomize_seed: bool = True,
    seed: int = 0,
    ss_guidance_strength: float = 7.5,
    ss_sampling_steps: int = 25,
    slat_guidance_strength: float = 7.5,
    slat_sampling_steps: int = 25,
    mesh_simplify: float = 0.95,
    texture_size: int = 1024
) -> str:
    """根据文本提示生成3D模型。

    参数：
        ctx: MCP 上下文
        prompt: 用于生成模型的文本描述
        randomize_seed: 是否使用随机种子（默认：True）
        seed: 生成固定结果的种子值（当randomize_seed=False时使用）
        ss_guidance_strength: ShapeStudio指导强度（默认：7.5）
        ss_sampling_steps: ShapeStudio采样步数（默认：25）
        slat_guidance_strength: SLAT指导强度（默认：7.5）
        slat_sampling_steps: SLAT采样步数（默认：25）
        mesh_simplify: 网格简化比例，控制模型复杂度（默认：0.95）
        texture_size: 贴图尺寸（默认：1024）

    返回值：
        str: 成功消息或错误详情
    """
    try:
        # 发送命令到Unity
        response = get_unity_connection().send_command("GENERATE_MODEL", {
            "prompt": prompt,
            "randomize_seed": randomize_seed,
            "seed": seed,
            "ss_guidance_strength": ss_guidance_strength,
            "ss_sampling_steps": ss_sampling_steps,
            "slat_guidance_strength": slat_guidance_strength,
            "slat_sampling_steps": slat_sampling_steps,
            "mesh_simplify": mesh_simplify,
            "texture_size": texture_size
        })
        
        # 处理返回结果
        status = response.get("message", "未知状态")
        result = response.get("result", "")
        
        if result:
            return f"{status}：{result}"
        return status
        
    except Exception as e:
        return f"生成3D模型时出错：{str(e)}" 