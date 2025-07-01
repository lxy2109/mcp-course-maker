import os
import time
import zipfile
from fastmcp import FastMCP
import mcp.types as types
from typing import Literal, Optional, List

# =========================================
# 文件自动重命名和压缩解压服务（FastMCP工具）
# 规范：
# 1. 只重命名files参数指定的文件。
# 2. 重命名优先用输入关键词或用户指定名，无则用当前时间，遇重名自动递增序号（如_1、_2）。
# 3. 文件扩展名与实际类型一致。
# 4. 整个流程自动化，无需人工干预。
# 5. 支持zip文件解压到指定目录。
# =========================================


mcp = FastMCP("file-rename-server")

PROJECT_ROOT = os.getenv("PROJECT_ROOT")
if not PROJECT_ROOT:
    # 可选：自动推断或报错
    PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))

def get_unique_name(folder, base_name, ext):
    """
    在folder下查找不重复的文件名，返回base_name, base_name_1, base_name_2 ...
    """
    candidate = f"{base_name}{ext}"
    idx = 1
    while os.path.exists(os.path.join(folder, candidate)):
        candidate = f"{base_name}_{idx}{ext}"
        idx += 1
    return candidate

@mcp.tool("rename_files")
async def rename_files(
    folder: str,
    file_type: Literal["audio", "image", "model", "all"],
    files: List[str],  # 必填，指定要重命名的文件名（相对folder）
    new_name: Optional[str] = None,
    keyword: Optional[str] = None,
    ext: Optional[str] = None
) -> list[types.TextContent]:
    """
    只重命名files参数指定的文件。
    Args:
        folder: 目标文件夹路径
        file_type: 文件类型(audio/image/model/all)
        files: 需要重命名的文件名列表（相对folder）
        new_name: 直接命名（如"光度计.jpg"）
        keyword: 关键词命名（如"光度计"）
        ext: 指定后缀（如".mp3"）
    Returns:
        List[TextContent]，包含重命名结果
    """
    audio_exts = ['.mp3', '.wav']
    image_exts = ['.jpg', '.jpeg', '.png']
    model_exts = ['.glb', '.obj', '.fbx', '.stl', '.3mf']
    renamed = []

    if not os.path.exists(folder):
        return [types.TextContent(type="text", text="文件夹不存在: {}".format(folder))]

    for filename in files:
        old_path = os.path.join(folder, filename)
        if not os.path.isfile(old_path):
            continue
        ext0 = os.path.splitext(filename)[1].lower()
        if (file_type == "audio" and ext0 not in audio_exts) or \
           (file_type == "image" and ext0 not in image_exts) or \
           (file_type == "model" and ext0 not in model_exts):
            continue
        if file_type == "all" and ext0 not in audio_exts + image_exts + model_exts:
            continue
        # 直接命名
        if new_name:
            base, ext2 = os.path.splitext(new_name)
            use_ext = ext2 if ext2 else ext0
            new_name_final = base + use_ext
            new_path = os.path.join(folder, new_name_final)
            if os.path.exists(new_path):
                # 自动递增序号
                i = 1
                while True:
                    new_name_final = f"{base}_{i}{use_ext}"
                    new_path = os.path.join(folder, new_name_final)
                    if not os.path.exists(new_path):
                        break
                    i += 1
            os.rename(old_path, new_path)
            renamed.append(f"{filename} -> {new_name_final}")
        else:
            # 关键词重命名
            base, ext2 = os.path.splitext(keyword or filename)
            use_ext = ext2 if ext2 else ext0
            new_name_final = base + use_ext
            new_path = os.path.join(folder, new_name_final)
            if os.path.exists(new_path):
                i = 1
                while True:
                    new_name_final = f"{base}_{i}{use_ext}"
                    new_path = os.path.join(folder, new_name_final)
                    if not os.path.exists(new_path):
                        break
                    i += 1
            os.rename(old_path, new_path)
            renamed.append(f"{filename} -> {new_name_final}")

    if renamed:
        return [types.TextContent(type="text", text="重命名成功: " + ", ".join(renamed))]
    else:
        return [types.TextContent(type="text", text="未找到可重命名的文件或类型不匹配")]

@mcp.tool("extract_zip")
async def extract_zip(
    zip_path: str,
    extract_to: Optional[str] = None,
    password: Optional[str] = None
) -> list[types.TextContent]:
    """
    解压zip文件到指定目录。
    Args:
        zip_path: zip文件的完整路径
        extract_to: 解压到的目标目录，如果不指定则解压到zip文件同级目录
        password: zip文件密码（如果有的话）
    Returns:
        List[TextContent]，包含解压结果和解压的文件列表
    """
    if not os.path.exists(zip_path):
        return [types.TextContent(type="text", text=f"zip文件不存在: {zip_path}")]
    
    if not zip_path.lower().endswith('.zip'):
        return [types.TextContent(type="text", text="文件不是zip格式")]
    
    # 如果没有指定解压目录，使用zip文件同级目录
    if extract_to is None:
        extract_to = os.path.dirname(zip_path)
        # 创建以zip文件名命名的子目录
        zip_name = os.path.splitext(os.path.basename(zip_path))[0]
        extract_to = os.path.join(extract_to, zip_name)
    
    # 确保解压目录存在
    os.makedirs(extract_to, exist_ok=True)
    
    try:
        extracted_files = []
        with zipfile.ZipFile(zip_path, 'r') as zip_ref:
            # 如果有密码，使用密码解压
            if password:
                zip_ref.setpassword(password.encode('utf-8'))
            
            # 获取zip文件中的文件列表
            file_list = zip_ref.namelist()
            
            # 解压所有文件
            zip_ref.extractall(extract_to)
            
            for file_name in file_list:
                extracted_path = os.path.join(extract_to, file_name)
                if os.path.exists(extracted_path):
                    extracted_files.append(file_name)
        
        result_text = f"成功解压到: {extract_to}\n"
        result_text += f"解压文件数量: {len(extracted_files)}\n"
        result_text += "解压的文件:\n" + "\n".join(extracted_files)
        
        return [types.TextContent(type="text", text=result_text)]
        
    except zipfile.BadZipFile:
        return [types.TextContent(type="text", text="无效的zip文件格式")]
    except RuntimeError as e:
        if "Bad password" in str(e):
            return [types.TextContent(type="text", text="zip文件密码错误")]
        else:
            return [types.TextContent(type="text", text=f"解压时发生错误: {str(e)}")]
    except Exception as e:
        return [types.TextContent(type="text", text=f"解压失败: {str(e)}")]

@mcp.tool("to_absolute_path")
async def to_absolute_path(
    relative_path: str
) -> list[types.TextContent]:
    """
    将项目内的相对路径（如Assets/xxx、Output/xxx等）转换为绝对路径。
    Args:
        relative_path: 项目内相对路径，必须以项目根目录下的一级文件夹为根
    Returns:
        List[TextContent]，包含绝对路径或错误信息
    """
    if not relative_path or not isinstance(relative_path, str):
        return [types.TextContent(type="text", text="路径不能为空且必须为字符串")]
    allowed_roots = [
        "Assets/", "Output/", "instant-meshes-mcp/", "image-gen-server-main/", "file-simp-server/", "elevenlabs-mcp/", "baidu-image-recognition-mcp/", "doubao-tts-mcp/", "tripo-mcp/", "picui-image-upload-mcp/", "meshy-ai-mcp-server/", "环境配置图示/"
    ]
    if not any(relative_path.startswith(root) for root in allowed_roots):
        return [types.TextContent(type="text", text=f"路径必须以{allowed_roots}之一开头: {relative_path}")]
    abs_path = os.path.abspath(os.path.join(PROJECT_ROOT, relative_path))
    return [types.TextContent(type="text", text=abs_path)]

if __name__ == "__main__":
    mcp.run() 