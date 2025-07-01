"""
model_analysis.py
模型分析、包验证、分析主流程等工具函数。
"""
# ... 迁移自 server.py ...
# analyze_obj_folder, validate_obj_package_internal, analyze_model

import os
from typing import Any, Dict
from .common_utils import is_texture_file
import trimesh

def analyze_obj_folder(folder_path: str) -> Dict[str, Any]:
    """
    分析包含OBJ文件包的文件夹，返回文件清单和关系。
    Args:
        folder_path (str): 文件夹路径
    Returns:
        dict: 包含文件分析结果的字典
    """
    if not os.path.exists(folder_path) or not os.path.isdir(folder_path):
        return {"error": f"Folder not found or not a directory: {folder_path}"}
    
    result = {
        "folder_path": folder_path,
        "obj_files": [],
        "mtl_files": [],
        "texture_files": [],
        "other_files": [],
        "relationships": [],
        "warnings": [],
        "errors": []
    }
    
    # 分类文件
    texture_extensions = ['.jpg', '.jpeg', '.png', '.bmp', '.tga', '.tiff', '.dds', '.hdr', '.exr', '.webp', '.ktx', '.ktx2', '.basis']
    
    for filename in os.listdir(folder_path):
        file_path = os.path.join(folder_path, filename)
        if os.path.isfile(file_path):
            lower_name = filename.lower()
            if lower_name.endswith('.obj'):
                result["obj_files"].append(filename)
            elif lower_name.endswith('.mtl'):
                result["mtl_files"].append(filename)
            elif is_texture_file(filename):
                result["texture_files"].append(filename)
            else:
                result["other_files"].append(filename)
    
    # 分析文件关系
    for obj_file in result["obj_files"]:
        obj_path = os.path.join(folder_path, obj_file)
        relationship = {
            "obj_file": obj_file,
            "referenced_mtl": [],
            "missing_mtl": [],
            "available_textures": [],
            "missing_textures": []
        }
        
        # 读取OBJ文件中的MTL引用
        try:
            with open(obj_path, 'r', encoding='utf-8') as f:
                for line in f:
                    if line.lower().startswith('mtllib'):
                        parts = line.split()
                        if len(parts) > 1:
                            mtl_name = parts[1].replace('\\', '/').split('/')[-1]
                            relationship["referenced_mtl"].append(mtl_name)
                            
                            # 检查MTL文件是否存在
                            if mtl_name not in result["mtl_files"]:
                                relationship["missing_mtl"].append(mtl_name)
        except Exception:
            result["errors"].append(f"Failed to read OBJ file: {obj_file}")
            continue
        
        # 分析MTL文件中的贴图引用
        for mtl_file in result["mtl_files"]:
            if mtl_file in relationship["referenced_mtl"]:
                mtl_path = os.path.join(folder_path, mtl_file)
                try:
                    with open(mtl_path, 'r', encoding='utf-8') as f:
                        for line in f:
                            line_lower = line.lower().strip()
                            if line_lower.startswith(('map_kd', 'map_ka', 'map_ks', 'map_ns', 'map_bump', 'map_d', 'map_normal', 'map_normalgl', 'map_orm', 'map_roughness', 'map_metallic', 'map_ao', 'map_emissive', 'map_opacity', 'map_displacement', 'map_height')):
                                parts = line.split()
                                if len(parts) > 1:
                                    tex_name = parts[-1].replace('\\', '/').split('/')[-1]
                                    if tex_name in result["texture_files"]:
                                        if tex_name not in relationship["available_textures"]:
                                            relationship["available_textures"].append(tex_name)
                                    else:
                                        if tex_name not in relationship["missing_textures"]:
                                            relationship["missing_textures"].append(tex_name)
                except Exception:
                    result["errors"].append(f"Failed to read MTL file: {mtl_file}")
        
        result["relationships"].append(relationship)
    
    # 生成警告
    if not result["obj_files"]:
        result["warnings"].append("No OBJ files found in folder")
    elif len(result["obj_files"]) > 1:
        result["warnings"].append(f"Multiple OBJ files found: {result['obj_files']}")
    
    if not result["mtl_files"]:
        result["warnings"].append("No MTL files found in folder")
    
    if not result["texture_files"]:
        result["warnings"].append("No texture files found in folder")
    
    return result

def validate_obj_package_internal(obj_file: str, mtl_files: list = None, texture_files: list = None) -> Dict[str, Any]:
    """
    内部使用的OBJ包验证函数
    """
    result = {
        "obj_file": {
            "path": obj_file,
            "exists": os.path.exists(obj_file),
            "valid": False
        },
        "referenced_mtl_files": [],
        "provided_mtl_files": [],
        "missing_mtl_files": [],
        "texture_files": [],
        "missing_textures": [],
        "warnings": [],
        "errors": []
    }
    
    if not os.path.exists(obj_file):
        result["errors"].append(f"OBJ file not found: {obj_file}")
        return result
    
    result["obj_file"]["valid"] = True
    
    # 检查OBJ文件中引用的MTL文件
    try:
        with open(obj_file, 'r', encoding='utf-8') as f:
            for line in f:
                if line.lower().startswith('mtllib'):
                    parts = line.split()
                    if len(parts) > 1:
                        referenced_mtl = parts[1].replace('\\', '/').split('/')[-1]
                        result["referenced_mtl_files"].append(referenced_mtl)
    except Exception as e:
        result["errors"].append(f"Failed to read OBJ file: {e}")
        return result
    
    # 检查MTL文件
    obj_dir = os.path.dirname(obj_file)
    for ref_mtl in result["referenced_mtl_files"]:
        if not os.path.exists(os.path.join(obj_dir, ref_mtl)):
            result["missing_mtl_files"].append(ref_mtl)
    
    return result

def get_model_scale(obj_path: str) -> float:
    """
    获取模型的包围盒对角线长度，用于计算合适的边长参数。
    Args:
        obj_path (str): OBJ文件路径
    Returns:
        float: 包围盒对角线长度
    """
    try:
        mesh = trimesh.load(obj_path, force='mesh')
        bounds = mesh.bounds
        diagonal = ((bounds[1] - bounds[0]) ** 2).sum() ** 0.5
        return diagonal
    except Exception:
        return 1.0  # 默认值

def calculate_edge_length(obj_path: str, target_faces: int) -> float:
    """
    根据模型尺寸和目标面数计算合适的边长。
    Args:
        obj_path (str): OBJ文件路径
        target_faces (int): 目标面数
    Returns:
        float: 推荐的边长
    """
    scale = get_model_scale(obj_path)
    # 根据目标面数估算合适的边长
    # 假设四面体网格，边长与面数平方根成反比
    edge_length = scale / (target_faces ** 0.5 * 10)
    # 限制边长范围，避免过大或过小
    edge_length = max(0.001, min(edge_length, scale * 0.1))
    return edge_length