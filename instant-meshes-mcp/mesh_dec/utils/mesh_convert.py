"""
mesh_convert.py
格式转换相关工具函数（GLB<->OBJ、贴图提取、Blender/trimesh调用等）。
"""
# ... 迁移自 mesh_processing.py ...
# glb_to_obj_with_textures, glb_to_obj, obj_to_glb

import subprocess
from typing import List
import os
import datetime
from .file_utils import get_temp_file
from .blender_utils import get_blender_executable_with_fallback
from .blender_scripts import get_glb_to_obj_script, get_obj_to_glb_script
from ..config import LOG_DIR
from .common_utils import is_texture_file

def glb_to_obj(glb_path: str, obj_path: str) -> list:
    """
    将GLB转换为OBJ，并提取所有嵌入的贴图文件。
    优先使用Blender以更好地处理材质和贴图，如果Blender不可用则使用trimesh。
    Args:
        glb_path (str): 输入GLB文件路径
        obj_path (str): 输出OBJ文件路径
    Returns:
        list: 提取的贴图文件路径列表
    Raises:
        RuntimeError: 转换失败时抛出
    """
    import trimesh
    import time
    
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    log_file = os.path.join(LOG_DIR, f"glb_to_obj_{timestamp}.log")
    extracted_textures = []
    
    with open(log_file, "w", encoding="utf-8") as logf:
        logf.write(f"Starting GLB to OBJ conversion with texture extraction\n")
        logf.write(f"Input GLB: {glb_path}\n")
        logf.write(f"Output OBJ: {obj_path}\n")
        logf.write(f"File exists: {os.path.exists(glb_path)}\n")
        if os.path.exists(glb_path):
            logf.write(f"File size: {os.path.getsize(glb_path)} bytes\n")
    
    # 首先尝试使用Blender进行转换和贴图提取
    try:
        # 自动检测Blender 3.6可执行文件
        blender_exe = get_blender_executable_with_fallback()
        
        # 记录检测结果
        with open(log_file, "a", encoding="utf-8") as logf:
            if blender_exe:
                logf.write(f"Blender 3.6 detected at: {blender_exe}\n")
            else:
                logf.write("Blender 3.6 not found, will use fallback method\n")
        
        if blender_exe:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Using Blender: {blender_exe}\n")
            
            # 创建完成标记文件路径（确保是唯一的）
            done_flag_path = get_temp_file(".done")
            
            # 确保完成标记文件不存在（清理之前可能残留的文件）
            if os.path.exists(done_flag_path):
                try:
                    os.remove(done_flag_path)
                except Exception:
                    pass
            
            # 创建Blender调试日志文件
            blender_debug_log = os.path.join(LOG_DIR, "blender_debug.log")
            
            # 在日志文件中插入分隔标记
            with open(blender_debug_log, "a", encoding="utf-8") as debug_logf:
                debug_logf.write(f"\n===== {timestamp} Blender GLB to OBJ with Texture Extraction =====\n")
                debug_logf.write(f"Input GLB: {glb_path}\n")
                debug_logf.write(f"Output OBJ: {obj_path}\n")
                debug_logf.write(f"Blender executable: {blender_exe}\n")
                debug_logf.write(f"Done flag path: {done_flag_path}\n")
            
            # 创建输出目录
            output_dir = os.path.dirname(obj_path)
            os.makedirs(output_dir, exist_ok=True)
            
            # 使用拆分的 Blender 脚本函数
            script_content = get_glb_to_obj_script(
                glb_path=glb_path,
                obj_path=obj_path,
                output_dir=output_dir,
                done_flag_path=done_flag_path,
                blender_debug_log=blender_debug_log
            )
            
            script_path = get_temp_file(".py")
            with open(script_path, 'w', encoding='utf-8') as f:
                f.write(script_content)
            
            # 使用Windows的start命令启动Blender，让它独立运行
            cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Starting Blender process independently: {cmd}\n")
            
            # 启动Blender进程（不阻塞）
            subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            
            # 等待Blender完成，通过检查完成标记文件
            max_wait_time = 180  # 最大等待时间3分钟
            wait_interval = 1    # 检查间隔1秒
            waited_time = 0
            blender_completed = False
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Waiting for Blender to complete (max {max_wait_time}s)...\n")
                logf.write(f"Done flag path: {done_flag_path}\n")
                logf.write(f"OBJ output path: {obj_path}\n")
            
            # 记录开始等待的时间
            start_wait_time = time.time()
            
            while waited_time < max_wait_time:
                # 首先检查完成标记文件
                done_flag_exists = os.path.exists(done_flag_path)
                obj_exists = os.path.exists(obj_path)
                obj_size = os.path.getsize(obj_path) if obj_exists else 0
                
                # 记录详细的检查状态
                if waited_time % 10 == 0:  # 每10秒记录一次状态
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Wait status at {waited_time}s: done_flag={done_flag_exists}, obj_exists={obj_exists}, obj_size={obj_size}\n")
                
                # 检查完成条件：完成标记文件存在且OBJ文件有效
                if done_flag_exists and obj_exists and obj_size > 0:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Blender completed successfully after {waited_time}s\n")
                        logf.write(f"Final OBJ size: {obj_size} bytes\n")
                    blender_completed = True
                    break
                
                # 如果只有OBJ文件但没有完成标记，等待更长时间确认
                elif obj_exists and obj_size > 0 and waited_time > 30:
                    # 等待30秒后，如果OBJ文件存在且大小稳定，认为完成
                    time.sleep(2)  # 再等2秒确认文件大小稳定
                    new_obj_size = os.path.getsize(obj_path) if os.path.exists(obj_path) else 0
                    if new_obj_size == obj_size and obj_size > 0:
                        with open(log_file, "a", encoding="utf-8") as logf:
                            logf.write(f"OBJ file stable after {waited_time}s (size: {obj_size} bytes), assuming completion\n")
                        blender_completed = True
                        break
                
                time.sleep(wait_interval)
                waited_time += 1
            
            if not blender_completed:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Blender timeout after {max_wait_time}s\n")
                    # 尝试强制终止可能残留的Blender进程
                    logf.write("Attempting to terminate any remaining Blender processes...\n")
                
                # 强制终止Blender进程
                try:
                    for p in psutil.process_iter(['name', 'exe', 'cmdline']):
                        try:
                            if p.info['name'] and 'blender' in p.info['name'].lower():
                                # 检查是否是我们启动的进程（通过脚本路径）
                                if p.info['cmdline'] and script_path in ' '.join(p.info['cmdline']):
                                    p.terminate()
                                    with open(log_file, "a", encoding="utf-8") as logf:
                                        logf.write(f"Terminated Blender process: {p.pid}\n")
                        except Exception:
                            continue
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to terminate Blender processes: {e}\n")

            # 收集提取的贴图文件
            output_dir = os.path.dirname(obj_path)
            for filename in os.listdir(output_dir):
                file_path = os.path.join(output_dir, filename)
                if os.path.isfile(file_path) and is_texture_file(filename):
                    extracted_textures.append(file_path)
            
            # 清理临时文件
            try:
                os.remove(script_path)
            except Exception:
                pass
            try:
                if os.path.exists(done_flag_path):
                    os.remove(done_flag_path)
            except Exception:
                pass
            
            # 在Blender调试日志中添加结束标记
            with open(blender_debug_log, "a", encoding="utf-8") as debug_logf:
                debug_logf.write(f"===== Blender Conversion End =====\n\n")
            
            # 检查OBJ文件是否成功创建
            if os.path.exists(obj_path) and os.path.getsize(obj_path) > 0:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Blender conversion successful, extracted {len(extracted_textures)} textures\n")
                    for tex in extracted_textures:
                        logf.write(f"  - {tex}\n")
                return extracted_textures
            else:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write("Blender conversion failed, falling back to trimesh\n")
                # 如果 Blender 失败，确保强制终止任何残留进程
                try:
                    import psutil
                    for p in psutil.process_iter(['name', 'exe', 'cmdline']):
                        try:
                            if p.info['name'] and 'blender' in p.info['name'].lower():
                                if p.info['cmdline'] and script_path in ' '.join(p.info['cmdline']):
                                    p.terminate()
                                    with open(log_file, "a", encoding="utf-8") as logf:
                                        logf.write(f"Force terminated Blender process: {p.pid}\n")
                        except Exception:
                            continue
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to terminate Blender processes: {e}\n")
        else:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write("Blender not found, using trimesh\n")
    
    except Exception as e:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Blender conversion error: {e}, falling back to trimesh\n")
    
    # 使用trimesh作为备选方案（但trimesh无法提取嵌入的贴图）
    try:
        start_time = time.time()
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Loading mesh with trimesh...\n")
        
        mesh = trimesh.load(glb_path)
        
        load_time = time.time() - start_time
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Mesh loaded in {load_time:.2f} seconds\n")
            logf.write(f"Mesh type: {type(mesh)}\n")
            if hasattr(mesh, 'vertices'):
                logf.write(f"Vertices: {len(mesh.vertices)}\n")
            if hasattr(mesh, 'faces'):
                logf.write(f"Faces: {len(mesh.faces)}\n")
        
        # 导出为OBJ
        start_time = time.time()
        mesh.export(obj_path)
        export_time = time.time() - start_time
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"OBJ exported in {export_time:.2f} seconds\n")
            logf.write(f"Output file size: {os.path.getsize(obj_path)} bytes\n")
            logf.write("Trimesh conversion completed successfully (no texture extraction)\n")
        
        # trimesh无法提取嵌入贴图，返回空列表
        return []
    
    except Exception as e:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Trimesh conversion failed: {e}\n")
        raise RuntimeError(f"GLB to OBJ conversion failed: {e}")


def obj_to_glb(obj_path: str, glb_path: str) -> None:
    """
    将OBJ文件转换为GLB文件，优先使用Blender以更好地处理材质和贴图，
    如果Blender不可用则使用trimesh作为备选方案。
    """
    import trimesh
    import time
    
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    log_file = os.path.join(LOG_DIR, f"obj_to_glb_{timestamp}.log")
    
    with open(log_file, "w", encoding="utf-8") as logf:
        logf.write(f"Starting OBJ to GLB conversion\n")
        logf.write(f"Input OBJ: {obj_path}\n")
        logf.write(f"Output GLB: {glb_path}\n")
        logf.write(f"File exists: {os.path.exists(obj_path)}\n")
        if os.path.exists(obj_path):
            logf.write(f"File size: {os.path.getsize(obj_path)} bytes\n")
    
    # 首先尝试使用Blender进行转换
    try:
        # 自动检测Blender 3.6可执行文件
        blender_exe = get_blender_executable_with_fallback()
        
        # 记录检测结果
        with open(log_file, "a", encoding="utf-8") as logf:
            if blender_exe:
                logf.write(f"Blender 3.6 detected at: {blender_exe}\n")
            else:
                logf.write("Blender 3.6 not found, will use fallback method\n")
        
        if blender_exe:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Using Blender: {blender_exe}\n")
            
            # 创建完成标记文件路径（确保是唯一的）
            done_flag_path = get_temp_file(".done")
            
            # 确保完成标记文件不存在（清理之前可能残留的文件）
            if os.path.exists(done_flag_path):
                try:
                    os.remove(done_flag_path)
                except Exception:
                    pass
            
            # 创建Blender调试日志文件
            blender_debug_log = os.path.join(LOG_DIR, "blender_debug.log")
            
            # 在日志文件中插入分隔标记
            with open(blender_debug_log, "a", encoding="utf-8") as debug_logf:
                debug_logf.write(f"\n===== {timestamp} Blender OBJ to GLB Conversion =====\n")
                debug_logf.write(f"Input OBJ: {obj_path}\n")
                debug_logf.write(f"Output GLB: {glb_path}\n")
                debug_logf.write(f"Blender executable: {blender_exe}\n")
                debug_logf.write(f"Done flag path: {done_flag_path}\n")
            
            # 使用拆分的 Blender 脚本函数
            script_content = get_obj_to_glb_script(
                obj_path=obj_path,
                glb_path=glb_path,
                done_flag_path=done_flag_path,
                blender_debug_log=blender_debug_log
            )
            
            script_path = get_temp_file(".py")
            with open(script_path, 'w', encoding='utf-8') as f:
                f.write(script_content)
            
            # 使用Windows的start命令启动Blender，让它独立运行
            cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Starting Blender process independently: {cmd}\n")
            
            # 启动Blender进程（不阻塞）
            subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            
            # 等待Blender完成，通过检查完成标记文件
            max_wait_time = 180  # 最大等待时间3分钟
            wait_interval = 1    # 检查间隔1秒
            waited_time = 0
            blender_completed = False
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Waiting for Blender to complete (max {max_wait_time}s)...\n")
                logf.write(f"Done flag path: {done_flag_path}\n")
                logf.write(f"GLB output path: {glb_path}\n")
            
            # 记录开始等待的时间
            start_wait_time = time.time()
            
            while waited_time < max_wait_time:
                # 首先检查完成标记文件
                done_flag_exists = os.path.exists(done_flag_path)
                glb_exists = os.path.exists(glb_path)
                glb_size = os.path.getsize(glb_path) if glb_exists else 0
                
                # 记录详细的检查状态
                if waited_time % 10 == 0:  # 每10秒记录一次状态
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Wait status at {waited_time}s: done_flag={done_flag_exists}, glb_exists={glb_exists}, glb_size={glb_size}\n")
                
                # 检查完成条件：完成标记文件存在且GLB文件有效
                if done_flag_exists and glb_exists and glb_size > 0:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Blender completed successfully after {waited_time}s\n")
                        logf.write(f"Final GLB size: {glb_size} bytes\n")
                    blender_completed = True
                    break
                
                # 如果只有GLB文件但没有完成标记，等待更长时间确认
                elif glb_exists and glb_size > 0 and waited_time > 30:
                    # 等待30秒后，如果GLB文件存在且大小稳定，认为完成
                    time.sleep(2)  # 再等2秒确认文件大小稳定
                    new_glb_size = os.path.getsize(glb_path) if os.path.exists(glb_path) else 0
                    if new_glb_size == glb_size and glb_size > 0:
                        with open(log_file, "a", encoding="utf-8") as logf:
                            logf.write(f"GLB file stable after {waited_time}s (size: {glb_size} bytes), assuming completion\n")
                        blender_completed = True
                        break
                
                time.sleep(wait_interval)
                waited_time += 1
            
            if not blender_completed:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Blender timeout after {max_wait_time}s\n")
                    # 尝试强制终止可能残留的Blender进程
                    logf.write("Attempting to terminate any remaining Blender processes...\n")
                
                # 强制终止Blender进程
                try:
                    for p in psutil.process_iter(['name', 'exe', 'cmdline']):
                        try:
                            if p.info['name'] and 'blender' in p.info['name'].lower():
                                # 检查是否是我们启动的进程（通过脚本路径）
                                if p.info['cmdline'] and script_path in ' '.join(p.info['cmdline']):
                                    p.terminate()
                                    with open(log_file, "a", encoding="utf-8") as logf:
                                        logf.write(f"Terminated Blender process: {p.pid}\n")
                        except Exception:
                            continue
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to terminate Blender processes: {e}\n")
            
            # 清理临时文件
            try:
                os.remove(script_path)
            except Exception:
                pass
            try:
                if os.path.exists(done_flag_path):
                    os.remove(done_flag_path)
            except Exception:
                pass
            
            # 在Blender调试日志中添加结束标记
            with open(blender_debug_log, "a", encoding="utf-8") as debug_logf:
                debug_logf.write(f"===== Blender Conversion End =====\n\n")
            
            # 检查GLB文件是否成功创建
            if os.path.exists(glb_path) and os.path.getsize(glb_path) > 0:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write("Blender conversion successful\n")
                return
            else:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write("Blender conversion failed, falling back to trimesh\n")
                # 如果 Blender 失败，确保强制终止任何残留进程
                try:
                    import psutil
                    for p in psutil.process_iter(['name', 'exe', 'cmdline']):
                        try:
                            if p.info['name'] and 'blender' in p.info['name'].lower():
                                if p.info['cmdline'] and script_path in ' '.join(p.info['cmdline']):
                                    p.terminate()
                                    with open(log_file, "a", encoding="utf-8") as logf:
                                        logf.write(f"Force terminated Blender process: {p.pid}\n")
                        except Exception:
                            continue
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to terminate Blender processes: {e}\n")
        else:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write("Blender not found, using trimesh\n")
    
    except Exception as e:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Blender conversion error: {e}, falling back to trimesh\n")
    
    # 使用trimesh作为备选方案
    try:
        start_time = time.time()
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Loading mesh with trimesh...\n")
        
        mesh = trimesh.load(obj_path)
        
        load_time = time.time() - start_time
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Mesh loaded in {load_time:.2f} seconds\n")
            logf.write(f"Mesh type: {type(mesh)}\n")
            if hasattr(mesh, 'vertices'):
                logf.write(f"Vertices: {len(mesh.vertices)}\n")
            if hasattr(mesh, 'faces'):
                logf.write(f"Faces: {len(mesh.faces)}\n")
        
        # 导出为GLB
        start_time = time.time()
        mesh.export(glb_path)
        export_time = time.time() - start_time
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"GLB exported in {export_time:.2f} seconds\n")
            logf.write(f"Output file size: {os.path.getsize(glb_path)} bytes\n")
            logf.write("Trimesh conversion completed successfully\n")
    
    except Exception as e:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Trimesh conversion failed: {e}\n")
        raise RuntimeError(f"OBJ to GLB conversion failed: {e}")

def fbx_to_obj(fbx_path: str, obj_path: str) -> list:
    """
    将FBX转换为OBJ，并提取所有嵌入的贴图文件。
    优先使用Blender以更好地处理材质和贴图。
    Args:
        fbx_path (str): 输入FBX文件路径
        obj_path (str): 输出OBJ文件路径
    Returns:
        list: 提取的贴图文件路径列表
    Raises:
        RuntimeError: 转换失败时抛出
    """
    import time
    import datetime
    import os
    from .blender_utils import get_blender_executable_with_fallback
    from .blender_scripts import get_fbx_to_obj_script
    from .file_utils import get_temp_file
    from ..config import LOG_DIR
    log_file = os.path.join(LOG_DIR, f"fbx_to_obj_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
    extracted_textures = []
    blender_exe = get_blender_executable_with_fallback()
    if not blender_exe:
        raise RuntimeError("Blender not found, cannot convert FBX to OBJ.")
    done_flag_path = get_temp_file(".done")
    if os.path.exists(done_flag_path):
        try:
            os.remove(done_flag_path)
        except Exception:
            pass
    blender_debug_log = os.path.join(LOG_DIR, "blender_debug.log")
    output_dir = os.path.dirname(obj_path)
    os.makedirs(output_dir, exist_ok=True)
    script_content = get_fbx_to_obj_script(
        fbx_path=fbx_path,
        obj_path=obj_path,
        output_dir=output_dir,
        done_flag_path=done_flag_path,
        blender_debug_log=blender_debug_log
    )
    script_path = get_temp_file(".py")
    with open(script_path, 'w', encoding='utf-8') as f:
        f.write(script_content)
    import subprocess
    cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
    with open(log_file, "a", encoding="utf-8") as logf:
        logf.write(f"Starting Blender process independently: {cmd}\n")
    subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    max_wait_time = 180
    wait_interval = 1
    waited_time = 0
    blender_completed = False
    start_wait_time = time.time()
    while waited_time < max_wait_time:
        done_flag_exists = os.path.exists(done_flag_path)
        obj_exists = os.path.exists(obj_path)
        obj_size = os.path.getsize(obj_path) if obj_exists else 0
        if waited_time % 10 == 0:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Wait status at {waited_time}s: done_flag={done_flag_exists}, obj_exists={obj_exists}, obj_size={obj_size}\n")
        if done_flag_exists and obj_exists and obj_size > 0:
            blender_completed = True
            break
        elif obj_exists and obj_size > 0 and waited_time > 30:
            time.sleep(2)
            new_obj_size = os.path.getsize(obj_path) if os.path.exists(obj_path) else 0
            if new_obj_size == obj_size and obj_size > 0:
                blender_completed = True
                break
        time.sleep(wait_interval)
        waited_time += 1
    if not blender_completed:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Blender timeout after {max_wait_time}s\n")
        raise RuntimeError("FBX to OBJ conversion timeout.")
    # 收集贴图
    for filename in os.listdir(output_dir):
        file_path = os.path.join(output_dir, filename)
        if os.path.isfile(file_path) and filename.lower().endswith(('.png', '.jpg', '.jpeg', '.tga', '.bmp')):
            extracted_textures.append(file_path)
    try:
        os.remove(script_path)
    except Exception:
        pass
    try:
        if os.path.exists(done_flag_path):
            os.remove(done_flag_path)
    except Exception:
        pass
    return extracted_textures


def fbx_to_glb(fbx_path: str, glb_path: str) -> None:
    """
    将FBX文件转换为GLB文件，优先使用Blender。
    Args:
        fbx_path (str): 输入FBX文件路径
        glb_path (str): 输出GLB文件路径
    Raises:
        RuntimeError: 转换失败时抛出
    """
    import time
    import datetime
    import os
    from .blender_utils import get_blender_executable_with_fallback
    from .blender_scripts import get_fbx_to_glb_script
    from .file_utils import get_temp_file
    from ..config import LOG_DIR
    log_file = os.path.join(LOG_DIR, f"fbx_to_glb_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}.log")
    blender_exe = get_blender_executable_with_fallback()
    if not blender_exe:
        raise RuntimeError("Blender not found, cannot convert FBX to GLB.")
    done_flag_path = get_temp_file(".done")
    if os.path.exists(done_flag_path):
        try:
            os.remove(done_flag_path)
        except Exception:
            pass
    blender_debug_log = os.path.join(LOG_DIR, "blender_debug.log")
    script_content = get_fbx_to_glb_script(
        fbx_path=fbx_path,
        glb_path=glb_path,
        done_flag_path=done_flag_path,
        blender_debug_log=blender_debug_log
    )
    script_path = get_temp_file(".py")
    with open(script_path, 'w', encoding='utf-8') as f:
        f.write(script_content)
    import subprocess
    cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
    with open(log_file, "a", encoding="utf-8") as logf:
        logf.write(f"Starting Blender process independently: {cmd}\n")
    subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    max_wait_time = 180
    wait_interval = 1
    waited_time = 0
    blender_completed = False
    start_wait_time = time.time()
    while waited_time < max_wait_time:
        done_flag_exists = os.path.exists(done_flag_path)
        glb_exists = os.path.exists(glb_path)
        glb_size = os.path.getsize(glb_path) if glb_exists else 0
        if waited_time % 10 == 0:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Wait status at {waited_time}s: done_flag={done_flag_exists}, glb_exists={glb_exists}, glb_size={glb_size}\n")
        if done_flag_exists and glb_exists and glb_size > 0:
            blender_completed = True
            break
        elif glb_exists and glb_size > 0 and waited_time > 30:
            time.sleep(2)
            new_glb_size = os.path.getsize(glb_path) if os.path.exists(glb_path) else 0
            if new_glb_size == glb_size and glb_size > 0:
                blender_completed = True
                break
        time.sleep(wait_interval)
        waited_time += 1
    if not blender_completed:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Blender timeout after {max_wait_time}s\n")
        raise RuntimeError("FBX to GLB conversion timeout.")
    try:
        os.remove(script_path)
    except Exception:
        pass
    try:
        if os.path.exists(done_flag_path):
            os.remove(done_flag_path)
    except Exception:
        pass
    return

