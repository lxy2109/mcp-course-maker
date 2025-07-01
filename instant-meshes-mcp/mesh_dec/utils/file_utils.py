"""
file_utils.py
文件操作、下载、复制、临时文件、清理等工具函数。
"""
# ... 迁移自 server.py ...
# get_temp_file, download_to_temp, copy_obj_package_to_temp, copy_folder_to_temp, move_and_cleanup, clean_temp_directory, safe_copy

import os
import shutil
from typing import Any, List, Optional
import urllib.parse
import requests
from .common_utils import get_temp_file, is_texture_file
from ..config import TEMP_DIR


def download_to_temp(url: str) -> str:
    """
    下载远程文件到本地temp文件夹，返回临时文件路径。
    只保留主文件名和后缀，去除URL参数，避免Windows非法字符。
    Args:
        url (str): 远程文件URL
    Returns:
        str: 本地临时文件路径
    Raises:
        requests.RequestException: 下载失败时抛出
    """
    import time
    
    parsed = urllib.parse.urlparse(url)
    base = os.path.basename(parsed.path)  # 只取路径部分
    suffix = os.path.splitext(base)[-1] if '.' in base else ''
    temp_path = get_temp_file(suffix)
    
    # 设置请求头，模拟常见浏览器请求
    headers = {
        'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
        'Accept': '*/*',
        'Accept-Language': 'en-US,en;q=0.9',
        'Accept-Encoding': 'gzip, deflate, br',
        'Connection': 'keep-alive',
        'Upgrade-Insecure-Requests': '1'
    }
    
    # 添加重试机制
    max_retries = 3
    retry_delay = 2  # 秒
    
    for attempt in range(max_retries):
        try:
            response = requests.get(url, stream=True, headers=headers, timeout=30)
            response.raise_for_status()
            
            with open(temp_path, 'wb') as tmp:
                for chunk in response.iter_content(chunk_size=8192):
                    tmp.write(chunk)
            return temp_path
            
        except requests.exceptions.HTTPError as e:
            if e.response.status_code == 403:
                error_msg = f"Access forbidden (403) for URL: {url}"
                if attempt < max_retries - 1:
                    error_msg += f" - Retrying in {retry_delay} seconds... (attempt {attempt + 1}/{max_retries})"
                    time.sleep(retry_delay)
                    retry_delay *= 1.5  # 指数退避
                    continue
                else:
                    # 提供详细的403错误信息和建议
                    detailed_error = f"""
Access Forbidden (403) Error for URL: {url}

This error typically occurs when:
1. The download link has expired (common with AI-generated model services)
2. The resource requires authentication or special permissions
3. The URL is private or has been revoked
4. Rate limiting is in effect

Suggested solutions:
1. Check if you have a fresh/valid download link
2. For Meshy AI models: Try regenerating the download link from your dashboard
3. For private repositories: Ensure you have proper access credentials
4. Download the file manually and use a local path instead

If this is a Meshy AI URL, the download link may have expired. Please:
- Log into your Meshy AI account
- Navigate to your model
- Generate a new download link
- Use the fresh URL or download the file locally
"""
                    raise RuntimeError(detailed_error)
            elif e.response.status_code == 404:
                raise RuntimeError(f"File not found (404) for URL: {url}. The resource may have been moved or deleted.")
            elif e.response.status_code in [429, 503]:
                if attempt < max_retries - 1:
                    time.sleep(retry_delay)
                    retry_delay *= 2
                    continue
                else:
                    raise RuntimeError(f"Service temporarily unavailable ({e.response.status_code}) for URL: {url}")
            else:
                raise RuntimeError(f"HTTP error {e.response.status_code} for URL: {url}")
                
        except requests.exceptions.Timeout:
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                continue
            else:
                raise RuntimeError(f"Download timeout for URL: {url}")
                
        except requests.exceptions.ConnectionError as e:
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                continue
            else:
                detailed_error = f"""
Connection Error for URL: {url}

This error typically occurs when:
1. Network connectivity issues (check your internet connection)
2. The remote server is temporarily unavailable
3. DNS resolution problems
4. Firewall or proxy blocking the connection
5. The service (Meshy AI) may be experiencing downtime

Suggested solutions:
1. Check your internet connection
2. Try again in a few minutes (server may be temporarily down)
3. For Meshy AI models: Try downloading the file manually from your browser
4. Use a local file path instead of URL:
   - Download the GLB file to your computer
   - Use the local path like: "C:/path/to/your/model.glb"
   
Alternative workflow:
1. Open the URL in your browser: {url}
2. Save the GLB file to your local drive
3. Call process_model with the local file path instead

Original error: {str(e)}
"""
                raise RuntimeError(detailed_error)
                
        except Exception as e:
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
                continue
            else:
                raise RuntimeError(f"Failed to download from URL {url}: {e}")
    
    # 如果所有重试都失败，抛出最终错误
    raise RuntimeError(f"Failed to download after {max_retries} attempts: {url}")

def copy_obj_package_to_temp(obj_path: str, additional_files: list = None) -> str:
    """
    将OBJ文件及其相关文件（MTL、贴图）复制到temp目录。
    Args:
        obj_path (str): 主OBJ文件路径
        additional_files (list): 额外的文件路径列表（MTL、贴图等）
    Returns:
        str: temp目录中的OBJ文件路径
    Raises:
        RuntimeError: 复制失败时抛出
    """
    if not os.path.exists(obj_path):
        raise RuntimeError(f"OBJ file not found: {obj_path}")
    
    # 复制主OBJ文件到temp
    obj_basename = os.path.basename(obj_path)
    temp_obj_path = os.path.join(TEMP_DIR, obj_basename)
    shutil.copy2(obj_path, temp_obj_path)
    
    # 自动查找并复制相关文件
    obj_dir = os.path.dirname(obj_path)
    obj_name = os.path.splitext(obj_basename)[0]
    
    # 查找同名MTL文件
    mtl_path = os.path.join(obj_dir, f"{obj_name}.mtl")
    if os.path.exists(mtl_path):
        temp_mtl_path = os.path.join(TEMP_DIR, f"{obj_name}.mtl")
        shutil.copy2(mtl_path, temp_mtl_path)
        
        # 从MTL文件中提取贴图文件引用并复制
        try:
            with open(mtl_path, 'r', encoding='utf-8') as f:
                for line in f:
                    line_lower = line.lower().strip()
                    if line_lower.startswith(('map_kd', 'map_ka', 'map_ks', 'map_ns', 'map_bump', 'map_d', 'map_normal', 'map_normalgl', 'map_orm', 'map_roughness', 'map_metallic', 'map_ao', 'map_emissive', 'map_opacity', 'map_displacement', 'map_height')):
                        parts = line.split()
                        if len(parts) > 1:
                            tex_file = parts[-1]  # 取最后一个部分作为文件名
                            # 处理可能的路径分隔符
                            tex_file = tex_file.replace('\\', '/').split('/')[-1]
                            orig_tex_path = os.path.join(obj_dir, tex_file)
                            if os.path.exists(orig_tex_path):
                                temp_tex_path = os.path.join(TEMP_DIR, tex_file)
                                shutil.copy2(orig_tex_path, temp_tex_path)
        except Exception:
            pass
    
    # 复制额外指定的文件
    if additional_files:
        for file_path in additional_files:
            if os.path.exists(file_path):
                filename = os.path.basename(file_path)
                temp_file_path = os.path.join(TEMP_DIR, filename)
                shutil.copy2(file_path, temp_file_path)
    
    # 查找OBJ文件中引用的MTL文件
    try:
        with open(obj_path, 'r', encoding='utf-8') as f:
            for line in f:
                if line.lower().startswith('mtllib'):
                    parts = line.split()
                    if len(parts) > 1:
                        referenced_mtl = parts[1]
                        # 处理可能的路径分隔符
                        referenced_mtl = referenced_mtl.replace('\\', '/').split('/')[-1]
                        referenced_mtl_path = os.path.join(obj_dir, referenced_mtl)
                        if os.path.exists(referenced_mtl_path):
                            temp_referenced_mtl = os.path.join(TEMP_DIR, referenced_mtl)
                            if not os.path.exists(temp_referenced_mtl):
                                shutil.copy2(referenced_mtl_path, temp_referenced_mtl)
                            
                            # 从引用的MTL文件中复制贴图
                            try:
                                with open(referenced_mtl_path, 'r', encoding='utf-8') as mtl_f:
                                    for mtl_line in mtl_f:
                                        mtl_line_lower = mtl_line.lower().strip()
                                        if mtl_line_lower.startswith(('map_kd', 'map_ka', 'map_ks', 'map_ns', 'map_bump', 'map_d', 'map_normal', 'map_normalgl', 'map_orm', 'map_roughness', 'map_metallic', 'map_ao', 'map_emissive', 'map_opacity', 'map_displacement', 'map_height')):
                                            mtl_parts = mtl_line.split()
                                            if len(mtl_parts) > 1:
                                                tex_file = mtl_parts[-1]
                                                tex_file = tex_file.replace('\\', '/').split('/')[-1]
                                                orig_tex_path = os.path.join(obj_dir, tex_file)
                                                if os.path.exists(orig_tex_path):
                                                    temp_tex_path = os.path.join(TEMP_DIR, tex_file)
                                                    if not os.path.exists(temp_tex_path):
                                                        shutil.copy2(orig_tex_path, temp_tex_path)
                            except Exception:
                                pass
                        break
    except Exception:
        pass
    
    # 智能识别并复制可能的贴图文件（基于命名约定）
    try:
        for filename in os.listdir(obj_dir):
            file_path = os.path.join(obj_dir, filename)
            if os.path.isfile(file_path) and is_texture_file(filename):
                temp_tex_path = os.path.join(TEMP_DIR, filename)
                if not os.path.exists(temp_tex_path):  # 避免重复复制
                    try:
                        shutil.copy2(file_path, temp_tex_path)
                    except Exception:
                        continue
    except Exception:
        pass
    
    return temp_obj_path

def copy_folder_to_temp(folder_path: str) -> str:
    """
    将包含OBJ文件包的文件夹复制到temp目录，并返回主OBJ文件路径。
    Args:
        folder_path (str): 包含OBJ文件包的文件夹路径
    Returns:
        str: temp目录中的主OBJ文件路径
    Raises:
        RuntimeError: 复制失败或找不到OBJ文件时抛出
    """
    if not os.path.exists(folder_path) or not os.path.isdir(folder_path):
        raise RuntimeError(f"Folder not found or not a directory: {folder_path}")
    
    # 查找文件夹中的OBJ文件
    obj_files = []
    for filename in os.listdir(folder_path):
        if filename.lower().endswith('.obj'):
            obj_files.append(filename)
    
    if not obj_files:
        raise RuntimeError(f"No OBJ files found in folder: {folder_path}")
    
    if len(obj_files) > 1:
        # 如果有多个OBJ文件，选择第一个，但记录警告
        main_obj = obj_files[0]
        print(f"Warning: Multiple OBJ files found, using: {main_obj}")
    else:
        main_obj = obj_files[0]
    
    # 复制文件夹中的所有文件到temp目录
    copied_files = []
    try:
        for filename in os.listdir(folder_path):
            src_path = os.path.join(folder_path, filename)
            if os.path.isfile(src_path):
                dst_path = os.path.join(TEMP_DIR, filename)
                shutil.copy2(src_path, dst_path)
                copied_files.append(dst_path)
        
        # 返回主OBJ文件在temp目录中的路径
        temp_obj_path = os.path.join(TEMP_DIR, main_obj)
        return temp_obj_path
        
    except Exception as e:
        # 清理已复制的文件
        for copied_file in copied_files:
            if os.path.exists(copied_file):
                try:
                    os.remove(copied_file)
                except Exception:
                    pass
        raise RuntimeError(f"Failed to copy folder to temp: {e}")


def move_and_cleanup(src: str, dst: str) -> None:
    """
    移动文件到目标并删除源文件。
    Args:
        src (str): 源文件路径
        dst (str): 目标文件路径
    """
    shutil.move(src, dst)
    if os.path.exists(src):
        os.remove(src)

def clean_temp_directory() -> None:
    """
    彻底清空temp目录中的所有文件和子目录。
    """
    if not os.path.exists(TEMP_DIR):
        return
    
    try:
        for filename in os.listdir(TEMP_DIR):
            file_path = os.path.join(TEMP_DIR, filename)
            try:
                if os.path.isfile(file_path) or os.path.islink(file_path):
                    os.unlink(file_path)
                elif os.path.isdir(file_path):
                    shutil.rmtree(file_path)
            except Exception:
                continue
    except Exception:
        pass

def safe_copy(src, dst_dir):
    import os, shutil
    dst = os.path.join(dst_dir, os.path.basename(src))
    if os.path.abspath(src) != os.path.abspath(dst):
        shutil.copy(src, dst)
    return dst
