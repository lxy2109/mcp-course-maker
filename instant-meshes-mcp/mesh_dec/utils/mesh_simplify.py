"""
mesh_simplify.py
减面、重拓扑、Instant Meshes相关工具函数。
"""
# ... 迁移自 mesh_processing.py ...
# simplify_with_uv_preservation, run_instant_meshes, auto_simplify_mesh, force_triangle_simplify, progressive_simplify, high_quality_simplify

import os
import datetime
import time
import subprocess
import psutil
import pymeshlab
from typing import Any, Dict, Optional
from .model_analysis import calculate_edge_length
from ..config import INSTANT_MESHES_PATH, LOG_DIR

def simplify_with_uv_preservation(input_path: str, target_faces: int, preserve_boundaries: bool = True) -> str:
    """
    专门针对带贴图的模型进行减面，特别保护UV坐标。
    Args:
        input_path (str): 输入OBJ文件路径
        target_faces (int): 目标面数
        preserve_boundaries (bool): 是否保持边界特征
    Returns:
        str: 简化后的OBJ文件路径
    """
    ms = pymeshlab.MeshSet()
    try:
        ms.load_new_mesh(input_path)
    except Exception:
        # 如果加载失败，返回原始路径
        return input_path
    
    original_faces = ms.current_mesh().face_number()
    
    if original_faces <= target_faces:
        return input_path
    
    # 检查是否有UV坐标
    try:
        has_texcoords = ms.current_mesh().has_face_tex_coord() or ms.current_mesh().has_vert_tex_coord()
    except Exception:
        has_texcoords = False
    
    if has_texcoords:
        # 对有UV的模型使用更保守的参数
        reduction_ratio = target_faces / original_faces
        
        if reduction_ratio < 0.3:
            # 极大减面，需要非常保守
            intermediate_target = int(original_faces * 0.5)
            
            # 第一步：减少到50%
            try:
                ms.meshing_decimation_quadric_edge_collapse(
                    targetfacenum=intermediate_target,
                    preserveboundary=True,
                    preservenormal=True,
                    preservetopology=True,
                    optimalplacement=True,
                    planarquadric=False,
                    qualityweight=False,
                    autoclean=False,
                    boundaryweight=3.0,  # 增强边界保护
                    selected=False
                )
            except Exception:
                # 如果第一步失败，尝试更保守的参数
                try:
                    ms.meshing_decimation_quadric_edge_collapse(
                        targetfacenum=target_faces,
                        preserveboundary=True,
                        preservenormal=True,
                        preservetopology=False,  # 降低要求
                        optimalplacement=True,
                        planarquadric=False,
                        qualityweight=False,
                        autoclean=True,
                        boundaryweight=2.0,
                        selected=False
                    )
                except Exception:
                    # 如果仍然失败，返回原始路径
                    return input_path
            else:
                # 第二步：减少到目标面数
                try:
                    ms.meshing_decimation_quadric_edge_collapse(
                        targetfacenum=target_faces,
                        preserveboundary=True,
                        preservenormal=True,
                        preservetopology=True,
                        optimalplacement=True,
                        planarquadric=False,
                        qualityweight=False,
                        autoclean=True,
                        boundaryweight=3.0,
                        selected=False
                    )
                except Exception:
                    # 如果第二步失败，使用当前结果
                    pass
        else:
            # 中等减面，一次性完成
            try:
                ms.meshing_decimation_quadric_edge_collapse(
                    targetfacenum=target_faces,
                    preserveboundary=preserve_boundaries,
                    preservenormal=True,
                    preservetopology=True,
                    optimalplacement=True,
                    planarquadric=False,
                    qualityweight=False,
                    autoclean=True,
                    boundaryweight=2.0,
                    selected=False
                )
            except Exception:
                # 如果简化失败，返回原始路径
                return input_path
    else:
        # 没有UV坐标，使用标准简化
        return progressive_simplify(input_path, target_faces, preserve_boundaries)
    
    simplified_path = input_path.replace('.obj', '_uv_simplified.obj')
    try:
        ms.save_current_mesh(simplified_path)
    except Exception:
        # 如果保存失败，返回原始路径
        return input_path
    
    return simplified_path

def run_instant_meshes(
    obj_in: str,
    obj_out: str,
    target_faces: int,
    extra_options: Optional[Dict[str, Any]] = None,
    mode: str = "balanced"
) -> None:
    """
    调用Instant Meshes命令行进行重拓扑，支持所有官方参数。
    自动计算合适的边长参数，减少破洞和过细网格问题。
    每次运行日志输出到 logs/instant_meshes_YYYYmmdd_HHMMSS.log，避免锁定。
    remesh完成后强制结束所有Instant Meshes.exe进程。
    Args:
        obj_in (str): 输入OBJ文件路径
        obj_out (str): 输出OBJ文件路径
        target_faces (int): 目标面数
        extra_options (dict): 其他命令行参数，如{"-d": True, "-t": 4}
        mode (str): 重拓扑模式
    Raises:
        subprocess.CalledProcessError: 命令行执行失败时抛出
    """
    # 计算合适的边长参数
    edge_length = calculate_edge_length(obj_in, target_faces)
    
    # 根据模式调整参数（注意：--scale和--faces不能同时使用）
    if mode == "fine":
        # 精细模式：使用面数控制，添加更多质量参数
        cmd = [
            INSTANT_MESHES_PATH,
            "-i", obj_in,
            "-o", obj_out,
            "--faces", str(target_faces),
            "-d",  # 确定性模式
            "-b",  # 边界对齐
            "-c"   # 纯四边形网格
        ]
    elif mode == "coarse":
        # 粗糙模式：减少目标面数
        target_faces = int(target_faces * 0.8)  # 实际目标面数减少20%
        cmd = [
            INSTANT_MESHES_PATH,
            "-i", obj_in,
            "-o", obj_out,
            "--faces", str(target_faces),
            "-d"  # 只使用确定性模式
        ]
    elif mode == "fix_holes":
        # 修复破洞模式：专注于修复拓扑
        cmd = [
            INSTANT_MESHES_PATH,
            "-i", obj_in,
            "-o", obj_out,
            "--faces", str(target_faces),
            "-d",  # 确定性模式
            "-b",  # 边界对齐
            "-s", "2"  # 更多的平滑迭代
        ]
    else:  # balanced mode (default)
        cmd = [
            INSTANT_MESHES_PATH,
            "-i", obj_in,
            "-o", obj_out,
            "--faces", str(target_faces),
            "-d",  # 确定性模式，结果更稳定
            "-b"   # 边界对齐，减少边界破洞
        ]
    
    if extra_options:
        for k, v in extra_options.items():
            if isinstance(v, bool):
                if v:
                    cmd.append(str(k))
            else:
                cmd.extend([str(k), str(v)])
    
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    log_file = os.path.join(LOG_DIR, f"instant_meshes_{timestamp}.log")
    proc = None
    try:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"\n===== {timestamp} Instant Meshes Run =====\n")
            logf.write(f"Edge length: {edge_length}\n")
            logf.write(f"Target faces: {target_faces}\n")
            logf.write(f"Command: {' '.join(cmd)}\n")
            proc = subprocess.Popen(cmd, stdout=logf, stderr=logf)
            proc.communicate()
            if proc.returncode != 0:
                raise subprocess.CalledProcessError(proc.returncode, cmd)
    finally:
        # 强制杀掉所有残留的 Instant Meshes.exe 进程
        for p in psutil.process_iter(['name', 'exe']):
            try:
                if p.info['name'] and 'Instant Meshes.exe' in p.info['name']:
                    p.kill()
            except Exception:
                pass

def auto_simplify_mesh(input_path: str, max_faces: int = 100000) -> str:
    """
    如果模型面数超过max_faces，自动用pymeshlab简化到max_faces以内，返回简化后模型路径（obj）。
    否则返回原始路径。
    """
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(input_path)
    n_faces = ms.current_mesh().face_number()
    if n_faces > max_faces:
        ms.meshing_decimation_quadric_edge_collapse(targetfacenum=max_faces)
        simplified_path = input_path.replace('.obj', '_simplified.obj').replace('.glb', '_simplified.obj')
        ms.save_current_mesh(simplified_path)
        return simplified_path
    return input_path

def force_triangle_simplify(input_path: str, target_triangles: int) -> str:
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(input_path)
    ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target_triangles)
    simplified_path = input_path.replace('.obj', '_tri.obj')
    ms.save_current_mesh(simplified_path)
    return simplified_path

def progressive_simplify(input_path: str, target_faces: int, preserve_boundaries: bool = True) -> str:
    """
    渐进式简化，避免一次性大幅减面导致模型破碎。
    Args:
        input_path (str): 输入OBJ文件路径
        target_faces (int): 目标面数
        preserve_boundaries (bool): 是否保持边界特征
    Returns:
        str: 简化后的OBJ文件路径
    """
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(input_path)
    
    original_faces = ms.current_mesh().face_number()
    
    # 如果已经小于目标面数，直接返回
    if original_faces <= target_faces:
        return input_path
    
    reduction_ratio = target_faces / original_faces
    
    # 如果减面比例超过50%，进行渐进式简化
    if reduction_ratio < 0.5:
        # 分步简化，每次最多减少50%
        current_faces = original_faces
        step = 1
        max_steps = 10  # 最大步数限制，防止无限循环
        
        # 记录处理过程到日志
        timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
        log_file = os.path.join(LOG_DIR, f"progressive_simplify_{timestamp}.log")
        with open(log_file, "w", encoding="utf-8") as logf:
            logf.write(f"Progressive simplification started\n")
            logf.write(f"Original faces: {original_faces}\n")
            logf.write(f"Target faces: {target_faces}\n")
            logf.write(f"Reduction ratio: {reduction_ratio:.2%}\n")
        
        start_time = time.time()
        timeout_seconds = 300  # 5分钟超时
        
        while current_faces > target_faces * 1.1 and step <= max_steps:  # 留10%缓冲，限制最大步数
            # 检查超时
            if time.time() - start_time > timeout_seconds:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Breaking: timeout after {timeout_seconds} seconds\n")
                break
            prev_faces = current_faces  # 记录上一次的面数
            next_target = max(target_faces, int(current_faces * 0.6))  # 每步减少40%
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Step {step}: current_faces={current_faces}, next_target={next_target}\n")
            
            # 预处理：修复和清理网格
            if step == 1:
                try:
                    ms.meshing_remove_duplicate_vertices()
                    ms.meshing_remove_duplicate_faces()
                    ms.meshing_remove_null_faces()
                except Exception:
                    pass
            
            # 保守的简化参数
            try:
                ms.meshing_decimation_quadric_edge_collapse(
                    targetfacenum=next_target,
                    preserveboundary=preserve_boundaries,
                    preservenormal=True,
                    preservetopology=True,
                    optimalplacement=True,
                    planarquadric=False,  # 关闭平面二次型，更保守
                    qualityweight=False,  # 关闭质量权重，更稳定
                    autoclean=False,  # 手动控制清理
                    selected=False,
                    boundaryweight=2.0 if preserve_boundaries else 1.0  # 增加边界权重
                )
            except Exception as e:
                # 如果减面失败，直接跳出循环
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Step {step} failed: {e}\n")
                break
            
            current_faces = ms.current_mesh().face_number()
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Step {step} result: {current_faces} faces\n")
            
            step += 1
            
            # 严格的安全检查：
            # 1. 如果面数没有明显减少（少于5%），跳出循环
            if current_faces >= prev_faces * 0.95:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Breaking: insufficient reduction {current_faces}/{prev_faces}\n")
                break
            # 2. 如果面数异常（为0或负数），跳出循环
            if current_faces <= 0:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Breaking: abnormal face count {current_faces}\n")
                break
            # 3. 如果已经接近目标，跳出循环
            if current_faces <= target_faces * 1.05:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Breaking: close to target {current_faces}/{target_faces}\n")
                break
        
        # 最后一次精确简化到目标面数
        if current_faces > target_faces:
            try:
                ms.meshing_decimation_quadric_edge_collapse(
                    targetfacenum=target_faces,
                    preserveboundary=preserve_boundaries,
                    preservenormal=True,
                    preservetopology=True,
                    optimalplacement=True,
                    planarquadric=False,
                    qualityweight=False,
                    autoclean=True,
                    boundaryweight=2.0 if preserve_boundaries else 1.0
                )
            except Exception:
                # 如果最终简化失败，使用当前结果
                pass
    else:
        # 减面比例较小，直接一次性简化
        try:
            ms.meshing_decimation_quadric_edge_collapse(
                targetfacenum=target_faces,
                preserveboundary=preserve_boundaries,
                preservenormal=True,
                preservetopology=True,
                optimalplacement=True,
                planarquadric=False,
                qualityweight=False,
                autoclean=True,
                boundaryweight=2.0 if preserve_boundaries else 1.0
            )
        except Exception:
            # 如果简化失败，返回原始路径
            return input_path
    
    simplified_path = input_path.replace('.obj', '_simplified.obj')
    ms.save_current_mesh(simplified_path)
    
    return simplified_path

def high_quality_simplify(input_path: str, target_faces: int, preserve_boundaries: bool = True) -> str:
    """
    使用pymeshlab进行高质量减面，保持边缘清晰和网格完整性。
    现在使用渐进式简化避免模型破碎。
    Args:
        input_path (str): 输入OBJ文件路径
        target_faces (int): 目标面数
        preserve_boundaries (bool): 是否保持边界特征
    Returns:
        str: 简化后的OBJ文件路径
    """
    return progressive_simplify(input_path, target_faces, preserve_boundaries)