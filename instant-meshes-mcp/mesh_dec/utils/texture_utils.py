"""
texture_utils.py
贴图相关的收集、识别、复制、增强、归档等工具函数。
"""
# ... 迁移自 server.py ...
# enhanced_is_texture_file, is_texture_file, collect_texture_files_from_directory, collect_all_texture_files, collect_texture_files, ensure_textures_in_obj_dir

import os
import shutil
import re
from .common_utils import get_temp_file, is_url, enhanced_is_texture_file, is_texture_file
from ..config import TEMP_DIR

def collect_texture_files_from_directory(directory: str, collected_files: list = None) -> list:
    """
    从指定目录收集所有贴图文件。
    Args:
        directory (str): 要搜索的目录路径
        collected_files (list): 已收集的文件列表，用于去重
    Returns:
        list: 贴图文件路径列表
    """
    if collected_files is None:
        collected_files = []
    
    texture_files = []
    
    if not os.path.exists(directory) or not os.path.isdir(directory):
        return texture_files
    
    try:
        for filename in os.listdir(directory):
            file_path = os.path.join(directory, filename)
            if os.path.isfile(file_path) and is_texture_file(filename):
                # 避免重复收集同名文件
                if file_path not in collected_files:
                    texture_files.append(file_path)
                    collected_files.append(file_path)
    except Exception:
        pass
    
    return texture_files

def collect_all_texture_files(input_model: str, additional_files: list = None) -> list:
    """
    从输入模型和相关目录收集所有贴图文件。
    Args:
        input_model (str): 输入模型路径
        additional_files (list): 额外文件列表
    Returns:
        list: 所有贴图文件路径列表
    """
    all_texture_files = []
    collected_paths = []  # 用于去重
    
    # 1. 从输入模型目录收集贴图
    if os.path.isdir(input_model):
        # 输入是文件夹
        texture_files = collect_texture_files_from_directory(input_model, collected_paths)
        all_texture_files.extend(texture_files)
    elif os.path.isfile(input_model) and not is_url(input_model):
        # 输入是本地文件
        input_dir = os.path.dirname(input_model)
        texture_files = collect_texture_files_from_directory(input_dir, collected_paths)
        all_texture_files.extend(texture_files)
    
    # 2. 从additional_files中收集贴图
    if additional_files:
        for file_path in additional_files:
            if os.path.isfile(file_path) and is_texture_file(os.path.basename(file_path)):
                if file_path not in collected_paths:
                    all_texture_files.append(file_path)
                    collected_paths.append(file_path)
    
    # 3. 从temp目录收集贴图（处理过程中复制的文件）
    if os.path.exists(TEMP_DIR):
        texture_files = collect_texture_files_from_directory(TEMP_DIR, collected_paths)
        all_texture_files.extend(texture_files)
    
    return all_texture_files

def collect_texture_files(model_dir: str, model_name: str, input_path: str = None) -> list:
    """
    收集模型相关的贴图文件，支持GLB文件的贴图提取。
    Args:
        model_dir (str): 模型所在目录
        model_name (str): 模型文件名（不含扩展名）
        input_path (str): 原始输入文件路径（用于GLB贴图提取）
    Returns:
        list: 贴图文件路径列表
    """
    texture_files = []

    from .mesh_convert import glb_to_obj_with_textures
    
    # 如果输入是GLB文件，尝试提取嵌入的贴图
    if input_path and input_path.lower().endswith('.glb'):
        try:
            # 创建临时OBJ文件用于贴图提取
            temp_obj = get_temp_file('.obj')
            extracted_textures = glb_to_obj_with_textures(input_path, temp_obj)
            
            # 将提取的贴图复制到模型目录
            for texture_path in extracted_textures:
                if os.path.exists(texture_path):
                    texture_name = os.path.basename(texture_path)
                    dest_path = os.path.join(model_dir, texture_name)
                    try:
                        shutil.copy2(texture_path, dest_path)
                        texture_files.append(dest_path)
                        # 记录到日志文件而不是控制台
                        pass
                    except Exception as e:
                        # 记录到日志文件而不是控制台
                        pass
            
            # 清理临时文件
            try:
                if os.path.exists(temp_obj):
                    os.remove(temp_obj)
                # 清理临时MTL文件
                temp_mtl = temp_obj.replace('.obj', '.mtl')
                if os.path.exists(temp_mtl):
                    os.remove(temp_mtl)
            except Exception:
                pass
                
        except Exception as e:
            # 记录到日志文件而不是控制台
            pass
    
    # 在模型目录中搜索贴图文件
    if os.path.exists(model_dir):
        for filename in os.listdir(model_dir):
            file_path = os.path.join(model_dir, filename)
            if os.path.isfile(file_path) and enhanced_is_texture_file(filename):
                # 检查是否与模型相关（文件名匹配或通用贴图）
                lower_filename = filename.lower()
                lower_model_name = model_name.lower()
                
                # 直接匹配模型名称或包含通用贴图关键词
                if (lower_model_name in lower_filename or 
                    any(keyword in lower_filename for keyword in [
                        'diffuse', 'albedo', 'basecolor', 'base_color', 'color',
                        'normal', 'bump', 'roughness', 'metallic', 'specular',
                        'ambient', 'ao', 'emission', 'opacity', 'alpha',
                        'material', 'texture', 'tex', 'map'
                    ])):
                    texture_files.append(file_path)
    
    # 在父目录中搜索相关贴图
    parent_dir = os.path.dirname(model_dir) if model_dir != os.path.dirname(model_dir) else model_dir
    if os.path.exists(parent_dir) and parent_dir != model_dir:
        for filename in os.listdir(parent_dir):
            file_path = os.path.join(parent_dir, filename)
            if os.path.isfile(file_path) and enhanced_is_texture_file(filename):
                lower_filename = filename.lower()
                lower_model_name = model_name.lower()
                
                # 只收集明确与模型名称匹配的贴图
                if lower_model_name in lower_filename:
                    texture_files.append(file_path)
    
    # 去重并返回
    unique_textures = []
    seen_names = set()
    for texture_path in texture_files:
        texture_name = os.path.basename(texture_path).lower()
        if texture_name not in seen_names:
            unique_textures.append(texture_path)
            seen_names.add(texture_name)
    
    return unique_textures

def ensure_textures_in_obj_dir(obj_path: str) -> None:
    """
    确保OBJ文件引用的所有贴图都在OBJ文件的同一目录中。
    这对于GLB转换时正确嵌入贴图非常重要。
    Args:
        obj_path (str): OBJ文件路径
    """
    obj_dir = os.path.dirname(obj_path)
    obj_name = os.path.splitext(os.path.basename(obj_path))[0]
    
    # 查找MTL文件
    mtl_path = os.path.join(obj_dir, f"{obj_name}.mtl")
    if not os.path.exists(mtl_path):
        # 尝试查找OBJ文件中引用的MTL文件
        try:
            with open(obj_path, 'r', encoding='utf-8') as f:
                for line in f:
                    if line.lower().startswith('mtllib'):
                        parts = line.split()
                        if len(parts) > 1:
                            referenced_mtl = parts[1].replace('\\', '/').split('/')[-1]
                            mtl_path = os.path.join(obj_dir, referenced_mtl)
                            break
        except Exception:
            return
    
    if not os.path.exists(mtl_path):
        return
    
    # 读取MTL文件，确保所有贴图都在同一目录
    try:
        with open(mtl_path, 'r', encoding='utf-8') as f:
            mtl_lines = f.readlines()
        
        updated_lines = []
        textures_copied = []
        
        for line in mtl_lines:
            line_lower = line.lower().strip()
            if line_lower.startswith(('map_kd', 'map_ka', 'map_ks', 'map_ns', 'map_bump', 'map_d', 'map_normal', 'map_normalgl', 'map_orm', 'map_roughness', 'map_metallic', 'map_ao', 'map_emissive', 'map_opacity', 'map_displacement', 'map_height')):
                parts = line.split()
                if len(parts) > 1:
                    original_tex_path = parts[-1]
                    tex_filename = original_tex_path.replace('\\', '/').split('/')[-1]
                    target_tex_path = os.path.join(obj_dir, tex_filename)
                    
                    # 如果贴图不在OBJ目录中，尝试从temp目录复制
                    if not os.path.exists(target_tex_path):
                        temp_tex_path = os.path.join(TEMP_DIR, tex_filename)
                        if os.path.exists(temp_tex_path):
                            try:
                                shutil.copy2(temp_tex_path, target_tex_path)
                                textures_copied.append(tex_filename)
                            except Exception:
                                pass
                    
                    # 更新MTL文件中的贴图路径为相对路径
                    if os.path.exists(target_tex_path):
                        # 重写这一行，使用相对路径
                        map_type = parts[0]
                        updated_lines.append(f"{map_type} {tex_filename}\n")
                    else:
                        updated_lines.append(line)
                else:
                    updated_lines.append(line)
            else:
                updated_lines.append(line)
        
        # 如果有贴图被复制，更新MTL文件
        if textures_copied:
            with open(mtl_path, 'w', encoding='utf-8') as f:
                f.writelines(updated_lines)
                
    except Exception:
        pass
