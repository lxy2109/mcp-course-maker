from mcp.server.fastmcp import FastMCP
import subprocess
import base64
import os

mcp = FastMCP("realesrgan-mcp-server")

@mcp.tool(
    annotations={
        "title": "Real-ESRGAN 超分",
        "readOnlyHint": False,
        "openWorldHint": True
    }
)
async def realesrgan_upscale(input_path: str, output_path: str = None, scale: int = 2) -> dict:
    """
    使用 Real-ESRGAN 对图片进行超分辨率处理，提升到4K等高分辨率。
    Args:
        input_path: 输入图片的完整路径
        output_path: 输出图片的完整路径（可选，默认自动加_4k后缀）
        scale: 放大倍数（2、3、4，默认2）
    Returns:
        包含输出图片路径和 base64 编码的字典
    """
    if not output_path:
        root, ext = os.path.splitext(input_path)
        output_path = f"{root}_4k{ext}"
    cmd = [
        r"E:\Tools\realesrgan-ncnn-vulkan-20220424-windows\realesrgan-ncnn-vulkan.exe",
        "-i", input_path,
        "-o", output_path,
        "-s", str(scale)
    ]
    subprocess.run(cmd, check=True)
    with open(output_path, "rb") as f:
        img_b64 = base64.b64encode(f.read()).decode()
    return {
        "output_path": output_path,
        "image_base64": img_b64
    }

if __name__ == "__main__":
    mcp.run()
