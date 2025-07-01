import os
import tempfile
from ..config import TEMP_DIR

def enhanced_is_texture_file(filename: str) -> bool:
    """
    增强版贴图文件识别函数，支持更多贴图类型和命名约定。
    Args:
        filename (str): 文件名
    Returns:
        bool: 是否为贴图文件
    """
    # 标准贴图扩展名（包括更多格式）
    texture_extensions = [
        '.jpg', '.jpeg', '.png', '.bmp', '.tga', '.tiff', '.tif',
        '.dds', '.hdr', '.exr', '.webp', '.ktx', '.ktx2', '.basis',
        '.psd', '.targa', '.sgi', '.pic', '.iff', '.ppm', '.pgm', '.pbm'
    ]
    
    lower_name = filename.lower()
    
    # 检查扩展名
    if not any(lower_name.endswith(ext) for ext in texture_extensions):
        return False
    
    # 排除明显的非贴图关键词
    non_texture_keywords = [
        'screenshot', 'capture', 'icon', 'logo', 'banner', 'thumb', 'preview',
        'ui', 'gui', 'button', 'menu', 'cursor', 'font'
    ]
    for non_keyword in non_texture_keywords:
        if non_keyword in lower_name:
            return False
    
    # 检查常见的贴图命名约定（扩展列表，特别针对Meshy AI等AI生成模型）
    texture_keywords = [
        # 基础贴图
        'diffuse', 'albedo', 'basecolor', 'base_color', 'color', 'col', 'diff',
        'base', 'main', 'primary',
        
        # 法线贴图
        'normal', 'normalgl', 'norm', 'nrm', 'bump', 'height', 'disp', 'displacement',
        
        # 材质属性贴图
        'roughness', 'rough', 'rgh', 'gloss', 'glossiness',
        'metallic', 'metal', 'met', 'metalness',
        'specular', 'spec', 'reflection', 'refl',
        
        # 环境贴图
        'ambient', 'ao', 'occlusion', 'cavity',
        
        # 发光贴图
        'emission', 'emissive', 'emit', 'glow', 'light',
        
        # 透明度贴图
        'opacity', 'alpha', 'transparency', 'mask',
        
        # 组合贴图
        'orm',  # Occlusion-Roughness-Metallic
        'rma',  # Roughness-Metallic-AO
        'arm',  # AO-Roughness-Metallic
        
        # 细节贴图
        'detail', 'micro', 'fine', 'secondary',
        
        # 其他常见类型
        'subsurface', 'sss', 'transmission', 'clearcoat',
        'anisotropy', 'sheen', 'iridescence',
        
        # 通用标识
        'texture', 'tex', 'map', 'material', 'mat', 'surface', 'skin',
        
        # 数字编号（material_0, texture_1等）
        'material_', 'texture_', 'tex_', 'mat_', 'img_', 'image_',
        
        # Meshy AI 和其他AI建模工具的常见命名模式
        'meshy', 'mesh_', 'generated_', 'ai_', 'model_', 'asset_',
        
        # 数字后缀模式（如 texture0, texture1, texture2）
        'texture0', 'texture1', 'texture2', 'texture3', 'texture4',
        'material0', 'material1', 'material2', 'material3', 'material4',
        'img0', 'img1', 'img2', 'img3', 'img4'
    ]
    
    # 如果文件名包含任何贴图关键词，认为是贴图文件
    for keyword in texture_keywords:
        if keyword in lower_name:
            return True
    
    # 检查数字模式（如material0, tex1, image2等）
    import re
    number_patterns = [
        r'material\d+', r'texture\d+', r'tex\d+', r'mat\d+', 
        r'img\d+', r'image\d+', r'map\d+', r'surface\d+'
    ]
    for pattern in number_patterns:
        if re.search(pattern, lower_name):
            return True
    
    # 如果文件名很短且是图片格式，很可能是贴图
    name_without_ext = os.path.splitext(lower_name)[0]
    if len(name_without_ext) <= 12 and any(lower_name.endswith(ext) for ext in texture_extensions):
        return True
    
    # 如果是常见的图片格式且文件名不包含明显的非贴图词汇，也认为是贴图
    common_image_exts = ['.jpg', '.jpeg', '.png', '.bmp', '.tga', '.tiff']
    if any(lower_name.endswith(ext) for ext in common_image_exts):
        return True
    
    return False

# 更新原有的is_texture_file函数
def is_texture_file(filename: str) -> bool:
    """
    判断文件是否为贴图文件，基于文件扩展名和命名约定。
    支持标准贴图扩展名以及现代PBR工作流中的命名约定。
    Args:
        filename (str): 文件名
    Returns:
        bool: 是否为贴图文件
    """
    return enhanced_is_texture_file(filename)

def is_url(path: str) -> bool:
    """判断路径是否为URL"""
    return path.startswith("http://") or path.startswith("https://")

def get_temp_file(suffix: str) -> str:
    """在temp目录下创建唯一临时文件名，不创建文件，仅返回路径"""
    fd, path = tempfile.mkstemp(suffix=suffix, dir=TEMP_DIR)
    os.close(fd)
    return path
