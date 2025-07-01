# 导入 FastMCP 和 Context 类，用于创建和管理 MCP 工具
from mcp.server.fastmcp import FastMCP, Context
# 导入类型提示相关模块，用于提高代码可读性和类型安全性
from typing import List, Dict, Any, Optional
# 导入 json 模块，用于处理 JSON 格式数据
import json
# 导入自定义模块中的函数，用于获取与 Unity 的连接
from unity_connection import get_unity_connection

def register_generate_tools(mcp):
    """注册3D模型生成相关工具"""
    
    @mcp.tool()  # 使用装饰器将函数注册为 MCP 工具
    def generate_3d_model(
        ctx: Context,  # MCP 上下文参数，包含调用工具的环境信息
        prompt: str,   # 必需参数：用于描述要生成的 3D 模型的文本提示
        randomize_seed: bool = True,  # 控制是否使用随机种子
        seed: int = 0,  # 当不使用随机种子时的固定种子值
        ss_guidance_strength: float = 7.5,  # ShapeStudio 生成算法的指导强度参数
        ss_sampling_steps: int = 25,  # ShapeStudio 生成过程的采样步数
        slat_guidance_strength: float = 7.5,  # SLAT 算法的指导强度参数
        slat_sampling_steps: int = 25,  # SLAT 算法的采样步数
        mesh_simplify: float = 0.95,  # 控制生成模型几何复杂度的参数，值越小模型越简化
        texture_size: int = 1024  # 生成的纹理贴图分辨率大小
    ) -> str:  # 函数返回值类型为字符串
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
            # 发送命令到Unity，调用 Unity 端的 3D 模型生成功能
            response = get_unity_connection().send_command("GENERATE_MODEL", {
                "prompt": prompt,  # 传递用户提供的文本提示
                "randomize_seed": randomize_seed,  # 控制是否使用随机种子
                "seed": seed,  # 固定种子值（当不使用随机种子时）
                "ss_guidance_strength": ss_guidance_strength,  # ShapeStudio 的指导强度参数
                "ss_sampling_steps": ss_sampling_steps,  # ShapeStudio 的采样步数
                "slat_guidance_strength": slat_guidance_strength,  # SLAT 的指导强度参数
                "slat_sampling_steps": slat_sampling_steps,  # SLAT 的采样步数
                "mesh_simplify": mesh_simplify,  # 网格简化率
                "texture_size": texture_size  # 纹理贴图尺寸
            })
            
            # 处理返回结果
            # 获取返回消息，如果不存在则使用"未知状态"作为默认值
            status = response.get("message", "未知状态")
            # 获取生成结果的详细信息，如果不存在则使用空字符串作为默认值
            result = response.get("result", "")
            
            # 如果存在详细结果，则返回状态和结果的组合信息
            if result:
                return f"{status}：{result}"
            # 否则只返回状态信息
            return status
            
        except Exception as e:
            # 捕获并处理可能发生的任何异常，返回友好的错误信息
            return f"生成3D模型时出错：{str(e)}"