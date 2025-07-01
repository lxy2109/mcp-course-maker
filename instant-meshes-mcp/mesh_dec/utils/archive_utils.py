"""
archive_utils.py
归档、日志、归档管理等工具函数。
"""
# ... 迁移自 server.py ...
# create_model_archive, clean_old_archives, manage_archives

import datetime
import os
import shutil
import json
from typing import Any, Dict
from ..config import ARCHIVE_DIR, LOG_DIR, TEMP_DIR
from .texture_utils import collect_texture_files_from_directory

def create_model_archive(
    model_path: str, 
    original_input: str, 
    processing_info: Dict[str, Any],
    processing_log_file: str = None,
    temp_files: list = None,
    texture_files: list = None
) -> str:
    """
    为处理后的模型创建归档文件夹，包含模型文件、材质、贴图和处理信息。
    Args:
        model_path (str): 输出模型文件路径
        original_input (str): 原始输入模型路径/URL
        processing_info (dict): 处理信息（面数、参数等）
        processing_log_file (str): 本次处理的日志文件路径
        temp_files (list): 临时文件列表，用于查找贴图文件
        texture_files (list): 要归档的贴图文件路径列表
    Returns:
        str: 归档文件夹路径
    Raises:
        RuntimeError: 归档创建失败时抛出
    """
    try:
        timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
        model_name = os.path.splitext(os.path.basename(model_path))[0]
        archive_name = f"{model_name}_{timestamp}"
        archive_path = os.path.join(ARCHIVE_DIR, archive_name)
        
        # 记录开始信息到单独的归档日志
        archive_log = os.path.join(LOG_DIR, f"archive_{timestamp}.log")
        with open(archive_log, "w", encoding="utf-8") as logf:
            logf.write(f"Starting archive creation: {archive_name}\n")
            logf.write(f"Model path: {model_path}\n")
            logf.write(f"Archive path: {archive_path}\n")
        
        # 创建归档文件夹结构
        os.makedirs(archive_path, exist_ok=True)
        model_dir = os.path.join(archive_path, "model")
        textures_dir = os.path.join(archive_path, "textures")
        logs_dir = os.path.join(archive_path, "logs")
        
        os.makedirs(model_dir, exist_ok=True)
        os.makedirs(textures_dir, exist_ok=True)
        os.makedirs(logs_dir, exist_ok=True)
        
        with open(archive_log, "a", encoding="utf-8") as logf:
            logf.write("Directory structure created.\n")
        
        # 1. 复制主模型文件
        if os.path.exists(model_path):
            with open(archive_log, "a", encoding="utf-8") as logf:
                logf.write(f"Copying main model file: {model_path}\n")
            shutil.copy2(model_path, model_dir)
            with open(archive_log, "a", encoding="utf-8") as logf:
                logf.write("Main model file copied.\n")
        else:
            with open(archive_log, "a", encoding="utf-8") as logf:
                logf.write(f"WARNING: Main model file not found: {model_path}\n")

        # 2. 复制所有收集到的贴图文件
        copied_textures = []
        
        with open(archive_log, "a", encoding="utf-8") as logf:
            logf.write("Starting texture file copying...\n")
            logf.write(f"Total texture files to copy: {len(texture_files) if texture_files else 0}\n")
        
        if texture_files:
            for texture_path in texture_files:
                if os.path.exists(texture_path) and os.path.isfile(texture_path):
                    try:
                        filename = os.path.basename(texture_path)
                        # 避免重复复制同名文件
                        if filename not in copied_textures:
                            with open(archive_log, "a", encoding="utf-8") as logf:
                                logf.write(f"Copying texture: {filename} from {texture_path}\n")
                            shutil.copy2(texture_path, textures_dir)
                            copied_textures.append(filename)
                            with open(archive_log, "a", encoding="utf-8") as logf:
                                logf.write(f"Texture copied: {filename}\n")
                        else:
                            with open(archive_log, "a", encoding="utf-8") as logf:
                                logf.write(f"Skipping duplicate texture: {filename}\n")
                    except Exception as e:
                        with open(archive_log, "a", encoding="utf-8") as logf:
                            logf.write(f"Failed to copy texture {texture_path}: {e}\n")
                        continue
        
        # 备用方案：如果没有收集到贴图文件，从temp和模型目录搜索
        if not copied_textures:
            with open(archive_log, "a", encoding="utf-8") as logf:
                logf.write("No textures copied from provided list, searching directories...\n")
            
            # 搜索temp目录
            if os.path.exists(TEMP_DIR):
                temp_textures = collect_texture_files_from_directory(TEMP_DIR)
                for texture_path in temp_textures:
                    try:
                        filename = os.path.basename(texture_path)
                        if filename not in copied_textures:
                            with open(archive_log, "a", encoding="utf-8") as logf:
                                logf.write(f"Copying texture from temp: {filename}\n")
                            shutil.copy2(texture_path, textures_dir)
                            copied_textures.append(filename)
                    except Exception as e:
                        with open(archive_log, "a", encoding="utf-8") as logf:
                            logf.write(f"Failed to copy temp texture {texture_path}: {e}\n")
            
            # 搜索模型目录
            if model_path.lower().endswith('.obj'):
                model_dir = os.path.dirname(model_path)
                model_textures = collect_texture_files_from_directory(model_dir)
                for texture_path in model_textures:
                    try:
                        filename = os.path.basename(texture_path)
                        if filename not in copied_textures:
                            with open(archive_log, "a", encoding="utf-8") as logf:
                                logf.write(f"Copying texture from model dir: {filename}\n")
                            shutil.copy2(texture_path, textures_dir)
                            copied_textures.append(filename)
                    except Exception as e:
                        with open(archive_log, "a", encoding="utf-8") as logf:
                            logf.write(f"Failed to copy model dir texture {texture_path}: {e}\n")
        
        with open(archive_log, "a", encoding="utf-8") as logf:
            logf.write(f"Texture copying completed. Total copied: {len(copied_textures)}\n")

        # 3. 复制MTL文件（如果是OBJ模型）
        if model_path.lower().endswith('.obj'):
            source_dir = os.path.dirname(model_path)
            model_base = os.path.splitext(os.path.basename(model_path))[0]
            
            # 查找并复制MTL文件
            possible_mtl = os.path.join(source_dir, f"{model_base}.mtl")
            if os.path.exists(possible_mtl):
                try:
                    shutil.copy2(possible_mtl, model_dir)
                    with open(archive_log, "a", encoding="utf-8") as logf:
                        logf.write(f"MTL file copied: {os.path.basename(possible_mtl)}\n")
                except Exception as e:
                    with open(archive_log, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to copy MTL file: {e}\n")
            else:
                # 尝试查找temp目录中的MTL文件
                temp_mtl = os.path.join(TEMP_DIR, f"{model_base}.mtl")
                if os.path.exists(temp_mtl):
                    try:
                        shutil.copy2(temp_mtl, model_dir)
                        with open(archive_log, "a", encoding="utf-8") as logf:
                            logf.write(f"MTL file copied from temp: {os.path.basename(temp_mtl)}\n")
                    except Exception as e:
                        with open(archive_log, "a", encoding="utf-8") as logf:
                            logf.write(f"Failed to copy MTL file from temp: {e}\n")

        # 4. 创建处理信息文件
        info_data = {
            "archive_created": timestamp,
            "original_input": original_input,
            "output_model": os.path.basename(model_path),
            "processing_info": processing_info,
            "copied_textures": copied_textures,
            "file_structure": {
                "model/": "主模型文件和材质文件",
                "textures/": "从temp目录和模型目录复制的贴图文件",
                "logs/": "本次处理的日志文件",
                "info.json": "处理信息和元数据"
            }
        }
        
        info_json_path = os.path.join(archive_path, "info.json")
        with open(info_json_path, 'w', encoding='utf-8') as f:
            json.dump(info_data, f, indent=2, ensure_ascii=False)
        
        # 5. 复制本次处理的日志文件
        if processing_log_file and os.path.exists(processing_log_file):
            try:
                with open(archive_log, "a", encoding="utf-8") as logf:
                    logf.write(f"Copying processing log: {processing_log_file}\n")
                shutil.copy2(processing_log_file, logs_dir)
                log_filename = os.path.basename(processing_log_file)
                info_data["processing_log"] = log_filename
                # 更新info.json
                with open(info_json_path, 'w', encoding='utf-8') as f:
                    json.dump(info_data, f, indent=2, ensure_ascii=False)
                with open(archive_log, "a", encoding="utf-8") as logf:
                    logf.write("Processing log copied and info.json updated.\n")
            except Exception as e:
                with open(archive_log, "a", encoding="utf-8") as logf:
                    logf.write(f"Failed to copy processing log: {e}\n")
        
        with open(archive_log, "a", encoding="utf-8") as logf:
            logf.write("Archive creation completed successfully.\n")
        
        return archive_path
    
    except Exception as e:
        # 如果归档创建过程中出现任何错误，记录并抛出异常
        error_log = os.path.join(LOG_DIR, f"archive_error_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
        with open(error_log, "w", encoding="utf-8") as logf:
            logf.write(f"Archive creation failed: {e}\n")
            logf.write(f"Model path: {model_path}\n")
            logf.write(f"Original input: {original_input}\n")
        raise RuntimeError(f"Failed to create archive: {e}")

def clean_old_archives(days_to_keep: int = 30) -> int:
    """
    清理旧的归档文件夹，保留指定天数内的文件夹。
    Args:
        days_to_keep (int): 保留的天数
    Returns:
        int: 删除的文件夹数量
    """
    if not os.path.exists(ARCHIVE_DIR):
        return 0
    
    cutoff_time = datetime.datetime.now() - datetime.timedelta(days=days_to_keep)
    deleted_count = 0
    
    for filename in os.listdir(ARCHIVE_DIR):
        file_path = os.path.join(ARCHIVE_DIR, filename)
        if os.path.isdir(file_path):
            try:
                file_time = datetime.datetime.fromtimestamp(os.path.getmtime(file_path))
                if file_time < cutoff_time:
                    shutil.rmtree(file_path)
                    deleted_count += 1
            except Exception:
                continue
    
    return deleted_count
