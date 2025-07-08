import json
import shutil
from typing import Any, Dict, Optional
import os
from mcp.server.fastmcp import FastMCP
import trimesh
import pymeshlab
import urllib.parse
import datetime
from .utils.file_utils import copy_folder_to_temp, download_to_temp, copy_obj_package_to_temp, get_temp_file, clean_temp_directory, move_and_cleanup
from .utils.mesh_convert import glb_to_obj, obj_to_glb, fbx_to_obj
from .utils.mesh_repair import repair_mesh_with_pymeshlab
from .utils.mesh_simplify import simplify_with_uv_preservation, run_instant_meshes, auto_simplify_mesh, force_triangle_simplify, progressive_simplify, high_quality_simplify
from .utils.mesh_material import enhance_meshy_ai_materials, restore_obj_material, rename_tripo_textures_and_update_mtl
from .utils.mesh_quality import check_mesh_quality
from .utils.texture_utils import collect_all_texture_files, collect_texture_files_from_directory, enhanced_is_texture_file, ensure_textures_in_obj_dir
from .utils.archive_utils import create_model_archive
from .utils.model_analysis import analyze_obj_folder, validate_obj_package_internal
from .utils.blender_utils import test_blender_detection
from .utils.common_utils import is_url, enhanced_is_texture_file, is_texture_file
from .config import TEMP_DIR, OUTPUT_DIR, ARCHIVE_DIR, LOG_DIR

mcp = FastMCP("instant_meshes")

def process_obj_with_materials(obj_path: str, additional_files: list = None) -> str:
    """
    处理OBJ文件及其材质文件，确保所有相关文件都在temp目录中。
    支持单个OBJ文件、文件列表或整个文件夹。
    Args:
        obj_path (str): OBJ文件路径、文件夹路径（可以是URL或本地路径）
        additional_files (list): 额外的文件路径列表
    Returns:
        str: temp目录中的OBJ文件路径
    """
    temp_files = []
    
    try:
        # 检查是否为文件夹
        if os.path.isdir(obj_path):
            # 文件夹模式：复制整个文件夹到temp
            return copy_folder_to_temp(obj_path)
        
        # 处理主OBJ文件
        if is_url(obj_path):
            local_obj = download_to_temp(obj_path)
            temp_files.append(local_obj)
        else:
            # 本地文件，复制整个OBJ包到temp目录
            local_obj = copy_obj_package_to_temp(obj_path, additional_files)
        
        # 处理额外的文件（如果是URL）
        if additional_files:
            for file_path in additional_files:
                if is_url(file_path):
                    downloaded_file = download_to_temp(file_path)
                    temp_files.append(downloaded_file)
        
        return local_obj
        
    except Exception as e:
        # 清理已下载的临时文件
        for temp_file in temp_files:
            if os.path.exists(temp_file):
                try:
                    os.remove(temp_file)
                except Exception:
                    pass
        raise RuntimeError(f"Failed to process OBJ package: {e}")

def get_original_name(input_model: str) -> str:
    # 支持本地路径和URL
    if input_model.startswith('http://') or input_model.startswith('https://'):
        path = urllib.parse.urlparse(input_model).path
        return os.path.basename(path)
    else:
        return os.path.basename(input_model)

@mcp.tool()
async def process_model(
    input_model: str,
    additional_files: Optional[list] = None,
    target_faces: int = 5000,
    operation: str = "simplify",
    mode: str = "balanced",
    preserve_boundaries: bool = True,
    preserve_uv: bool = True,
    options: Optional[Dict[str, Any]] = None,
    create_archive: bool = True
) -> str:
    """
    统一的模型处理工具，支持减面、重拓扑等操作。
    自动识别输入类型（单文件、文件夹、URL），智能选择最佳处理方式。

    Args:
        input_model (str): 输入模型路径（支持GLB/OBJ文件、文件夹或URL）
        additional_files (list): 额外的文件路径列表（MTL、贴图等，可选）
        target_faces (int): 目标面数
        operation (str): 操作类型
            - "simplify": 纯减面，保持原有网格结构，保持边缘清晰（默认）
            - "auto": 自动选择（水密模型用simplify，有问题的用remesh）
            - "remesh": 重拓扑，修复网格问题，生成新的拓扑结构
        mode (str): 处理模式（用于remesh操作）
            - "balanced": 平衡模式，适合大多数情况（默认）
            - "fine": 精细模式，生成更均匀的网格
            - "coarse": 粗糙模式，生成更少的面数
            - "fix_holes": 专门用于修复破洞的模式
        preserve_boundaries (bool): 是否保持边界特征
        preserve_uv (bool): 是否保持UV坐标
        options (dict): 其他Instant Meshes命令行参数（如{"-d": True, "-t": 4}）
        create_archive (bool): 是否创建归档文件夹
    Returns:
        str: 输出模型路径或归档文件夹路径（如果create_archive=True）
    Raises:
        RuntimeError: 处理失败时抛出
    """
    # 开始前先清理temp目录并设置日志
    clean_temp_directory()
    temp_files = []
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    log_file = os.path.join(LOG_DIR, f"process_model_{timestamp}.log")
    
    # 在处理开始前收集所有贴图文件（支持GLB贴图提取）
    texture_files_for_archive = collect_all_texture_files(input_model, additional_files)
    
    # 如果输入是GLB文件，预先提取贴图到temp目录
    if input_model.lower().endswith('.glb') and os.path.exists(input_model):
        try:
            temp_obj_for_extraction = get_temp_file('.obj')
            extracted_textures = glb_to_obj(input_model, temp_obj_for_extraction)
            
            # 将提取的贴图添加到归档列表
            for texture_path in extracted_textures:
                if os.path.exists(texture_path) and texture_path not in texture_files_for_archive:
                    texture_files_for_archive.append(texture_path)
            
            # 清理临时OBJ文件
            try:
                if os.path.exists(temp_obj_for_extraction):
                    os.remove(temp_obj_for_extraction)
                temp_mtl = temp_obj_for_extraction.replace('.obj', '.mtl')
                if os.path.exists(temp_mtl):
                    os.remove(temp_mtl)
            except Exception:
                pass
                
        except Exception as e:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"WARNING: Failed to pre-extract GLB textures: {e}\n")
    
    try:
        with open(log_file, "w", encoding="utf-8") as logf:
            logf.write(f"Starting model processing\n")
            logf.write(f"Input: {input_model}\n")
            logf.write(f"Target faces: {target_faces}\n")
            logf.write(f"Operation: {operation}\n")
            logf.write(f"Mode: {mode}\n")
            logf.write(f"Preserve UV: {preserve_uv}\n")
            logf.write(f"Create archive: {create_archive}\n")
            logf.write(f"Initial texture files collected: {len(texture_files_for_archive)}\n")
            for tex_file in texture_files_for_archive:
                logf.write(f"  - {tex_file}\n")
            logf.write("Temp directory cleaned.\n")
        
        # 1. 处理输入模型（支持URL、多文件和文件夹）
        if os.path.isdir(input_model):
            # 文件夹模式：复制整个文件夹到temp
            obj_in = process_obj_with_materials(input_model, additional_files)
            temp_files.append(obj_in)
            # 自动处理Tripo贴图重命名和MTL修正
            model_dir = os.path.dirname(obj_in)
            rename_tripo_textures_and_update_mtl(model_dir)
        elif input_model.lower().endswith(".obj"):
            # OBJ文件，使用新的处理函数
            obj_in = process_obj_with_materials(input_model, additional_files)
            temp_files.append(obj_in)
            # 自动处理Tripo贴图重命名和MTL修正
            model_dir = os.path.dirname(obj_in)
            rename_tripo_textures_and_update_mtl(model_dir)
        elif input_model.lower().endswith(".fbx") and os.path.exists(input_model):
            # FBX文件，先转OBJ
            temp_obj_for_fbx = get_temp_file('.obj')
            extracted_textures = fbx_to_obj(input_model, temp_obj_for_fbx)
            # 将提取的贴图添加到归档列表
            for texture_path in extracted_textures:
                temp_files.append(texture_path)
            # 自动处理Tripo贴图重命名和MTL修正
            model_dir = os.path.dirname(temp_obj_for_fbx)
            rename_tripo_textures_and_update_mtl(model_dir)
            obj_in = temp_obj_for_fbx
            temp_files.append(obj_in)
        elif is_url(input_model):
            local_input = download_to_temp(input_model)
            temp_files.append(local_input)
            # 若输入为GLB，先转OBJ
            if local_input.lower().endswith(".glb"):
                obj_in = get_temp_file(".obj")
                glb_to_obj(local_input, obj_in)
                temp_files.append(obj_in)
            # 若输入为FBX，先转OBJ
            elif local_input.lower().endswith(".fbx"):
                temp_obj_for_fbx = get_temp_file('.obj')
                extracted_textures = fbx_to_obj(local_input, temp_obj_for_fbx)
                for texture_path in extracted_textures:
                    if os.path.exists(texture_path) and texture_path not in texture_files_for_archive:
                        texture_files_for_archive.append(texture_path)
                obj_in = temp_obj_for_fbx
                temp_files.append(obj_in)
            else:
                obj_in = local_input
        else:
            # 本地文件
            local_input = input_model
            # 确保使用绝对路径
            if not os.path.isabs(local_input):
                local_input = os.path.abspath(local_input)
            # 若输入为GLB，先转OBJ
            if local_input.lower().endswith(".glb"):
                obj_in = get_temp_file(".obj")
                glb_to_obj(local_input, obj_in)
                temp_files.append(obj_in)
            # 若输入为FBX，先转OBJ
            elif local_input.lower().endswith(".fbx"):
                temp_obj_for_fbx = get_temp_file('.obj')
                extracted_textures = fbx_to_obj(local_input, temp_obj_for_fbx)
                for texture_path in extracted_textures:
                    if os.path.exists(texture_path) and texture_path not in texture_files_for_archive:
                        texture_files_for_archive.append(texture_path)
                obj_in = temp_obj_for_fbx
                temp_files.append(obj_in)
            else:
                # 本地OBJ文件，复制到temp目录
                obj_in = process_obj_with_materials(local_input, additional_files)
                temp_files.append(obj_in)

        # 更新贴图文件列表，包含temp目录中新复制的文件
        temp_texture_files = collect_texture_files_from_directory(TEMP_DIR)
        for tex_file in temp_texture_files:
            if tex_file not in texture_files_for_archive:
                texture_files_for_archive.append(tex_file)
        
        # 在处理完成后，再次收集temp目录中的贴图文件，确保所有贴图都被包含
        def collect_textures_after_processing():
            final_texture_files = []
            obj_dir = os.path.dirname(obj_in) if obj_in else TEMP_DIR
            for root, dirs, files in os.walk(obj_dir):
                for file in files:
                    if enhanced_is_texture_file(file):
                        file_path = os.path.join(root, file)
                        if file_path not in final_texture_files:
                            final_texture_files.append(file_path)
            return final_texture_files

        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Input processed: {obj_in}\n")

        # 2. 分析原始模型质量
        original_quality = check_mesh_quality(obj_in)
        original_faces = original_quality.get("faces", 0)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Original faces: {original_faces}\n")
            logf.write(f"Original quality: {original_quality}\n")

        # === 自动破洞修复集成 ===
        try:
            watertight = original_quality.get("watertight", True)
            issues = original_quality.get("issues", [])
            if not watertight or (isinstance(issues, list) and any("hole" in str(issue).lower() for issue in issues)):
                repaired_obj = repair_mesh_with_pymeshlab(obj_in)
                temp_files.append(repaired_obj)
                obj_in = repaired_obj
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write("Auto-repaired mesh holes before further processing.\n")
        except Exception as e:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"WARNING: Auto hole repair failed: {e}\n")

        # 3. 智能选择操作方式
        if operation == "auto":
            # 根据模型质量自动选择处理方式
            is_watertight = original_quality.get("watertight", False)
            has_issues = len(original_quality.get("issues", [])) > 0
            
            if is_watertight and not has_issues:
                actual_operation = "simplify"
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write("Auto mode: Selected 'simplify' - model is watertight and has no issues\n")
            else:
                actual_operation = "remesh"
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Auto mode: Selected 'remesh' - watertight: {is_watertight}, issues: {original_quality.get('issues', [])}\n")
        else:
            actual_operation = operation

        # 4. 检查是否需要处理
        if original_faces <= target_faces:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write("Model already has fewer faces than target, no processing needed.\n")
            final_obj = obj_in
        else:
            # 5. 根据选择的操作进行处理
            if actual_operation == "simplify":
                # 纯减面模式
                try:
                    simplified_obj = simplify_with_uv_preservation(obj_in, target_faces, preserve_boundaries)
                    temp_files.append(simplified_obj)
                    final_obj = simplified_obj
                    
                    # 修复材质引用
                    restore_obj_material(final_obj, obj_in)
                    # 特别针对AI生成模型（如Meshy AI）增强材质处理
                    enhance_meshy_ai_materials(final_obj, log_file)
                    
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write("Simplify operation completed successfully.\n")
                        
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"ERROR: Simplify operation failed: {e}\n")
                    raise RuntimeError(f"Simplify operation failed: {e}")
            else:
                # 重拓扑模式
                try:
                    repaired_obj = repair_mesh_with_pymeshlab(obj_in)
                    temp_files.append(repaired_obj)
                    obj_in = repaired_obj
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"WARNING: Mesh repair failed: {e}\n")

                # 输出先到临时文件
                temp_output = get_temp_file(".obj")
                temp_files.append(temp_output)

                # Instant Meshes重拓扑
                run_instant_meshes(obj_in, temp_output, target_faces, extra_options=options, mode=mode)
                restore_obj_material(temp_output, obj_in)
                # 特别针对AI生成模型（如Meshy AI）增强材质处理
                enhance_meshy_ai_materials(temp_output, log_file)
                final_obj = temp_output

        # 6. 检查处理结果质量
        final_quality = check_mesh_quality(final_obj)
        final_faces = final_quality.get('faces', 0)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Final faces: {final_faces}\n")
            logf.write(f"Reduction ratio: {final_faces/original_faces:.2%}\n")
            logf.write(f"Final quality: {final_quality}\n")

        # 7. 确保所有贴图都在final_obj的同一目录中，以便GLB转换时正确嵌入
        # 首先进行全面的贴图收集和复制
        final_obj_dir = os.path.dirname(final_obj)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Starting comprehensive texture collection and copying...\n")
        
        # 收集所有可能的贴图文件
        all_collected_textures = []
        
        # 1. 从原始输入收集贴图
        if input_model and not is_url(input_model):
            if os.path.isdir(input_model):
                original_textures = collect_texture_files_from_directory(input_model)
            elif os.path.isfile(input_model):
                original_dir = os.path.dirname(input_model)
                original_textures = collect_texture_files_from_directory(original_dir)
            else:
                original_textures = []
            all_collected_textures.extend(original_textures)
        
        # 2. 从temp目录收集贴图
        temp_textures = collect_texture_files_from_directory(TEMP_DIR)
        all_collected_textures.extend(temp_textures)
        
        # 3. 从additional_files收集贴图
        if additional_files:
            for file_path in additional_files:
                if os.path.isfile(file_path) and enhanced_is_texture_file(os.path.basename(file_path)):
                    all_collected_textures.append(file_path)
        
        # 4. 从final_obj所在目录收集现有贴图
        existing_textures = collect_texture_files_from_directory(final_obj_dir)
        all_collected_textures.extend(existing_textures)
        
        # 去重
        unique_textures = []
        texture_names_seen = set()
        for tex_path in all_collected_textures:
            tex_name = os.path.basename(tex_path)
            if tex_name not in texture_names_seen and os.path.exists(tex_path):
                unique_textures.append(tex_path)
                texture_names_seen.add(tex_name)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Found {len(unique_textures)} unique texture files:\n")
            for tex in unique_textures:
                logf.write(f"  - {tex}\n")
        
        # 将所有贴图复制到final_obj目录
        copied_textures = []
        for tex_path in unique_textures:
            tex_filename = os.path.basename(tex_path)
            dest_path = os.path.join(final_obj_dir, tex_filename)
            
            # 如果目标文件已存在且大小相同，跳过复制
            if os.path.exists(dest_path):
                try:
                    if os.path.getsize(tex_path) == os.path.getsize(dest_path):
                        copied_textures.append(dest_path)
                        continue
                except Exception:
                    pass
            
            # 复制贴图文件
            try:
                shutil.copy2(tex_path, dest_path)
                copied_textures.append(dest_path)
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Copied texture: {tex_filename}\n")
            except Exception as e:
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Failed to copy texture {tex_filename}: {e}\n")
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Successfully copied {len(copied_textures)} textures to final_obj directory.\n")
        
        # 现在调用原有的ensure_textures_in_obj_dir函数来处理MTL文件引用
        ensure_textures_in_obj_dir(final_obj)
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Ensured all textures are in OBJ directory for GLB conversion.\n")
        
        # 最终收集所有贴图文件（包括处理过程中生成的）
        final_texture_files = collect_texture_files_from_directory(final_obj_dir)
        temp_texture_files = collect_texture_files_from_directory(TEMP_DIR)
        
        # 合并所有贴图文件，去重
        all_final_textures = final_texture_files + temp_texture_files
        for tex_file in all_final_textures:
            if tex_file not in texture_files_for_archive:
                texture_files_for_archive.append(tex_file)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Collected {len(texture_files_for_archive)} texture files for archive.\n")

        # 8. 输出为GLB
        temp_glb = get_temp_file(".glb")
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Starting OBJ to GLB conversion...\n")
            logf.write("IMPORTANT: Temp directory will NOT be cleaned until GLB conversion is complete.\n")
        
        # 确保 obj_to_glb 真正等待 Blender 完成
        obj_to_glb(final_obj, temp_glb)
        temp_files.append(temp_glb)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("OBJ to GLB conversion completed.\n")
            logf.write(f"GLB file exists: {os.path.exists(temp_glb)}\n")
            if os.path.exists(temp_glb):
                logf.write(f"GLB file size: {os.path.getsize(temp_glb)} bytes\n")

        # 9. 移动到输出目录
        orig_name = get_original_name(input_model)
        output_name = os.path.splitext(orig_name)[0] + f"_{actual_operation}.glb"
        output_model = os.path.join(OUTPUT_DIR, output_name)
        move_and_cleanup(temp_glb, output_model)
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Model moved to output directory: {output_model}\n")
        
        # 10. 创建归档（可选）
        archive_path = None
        if create_archive:
            processing_info = {
                "operation": actual_operation,
                "mode": mode,
                "original_faces": original_faces,
                "target_faces": target_faces,
                "final_faces": final_faces,
                "reduction_ratio": f"{final_faces/original_faces:.2%}",
                "preserve_boundaries": preserve_boundaries,
                "preserve_uv": preserve_uv,
                "options": options,
                "quality_info": final_quality
            }
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write("Creating archive...\n")
            
            archive_path = create_model_archive(
                output_model, 
                input_model, 
                processing_info, 
                processing_log_file=log_file,
                temp_files=temp_files,
                texture_files=texture_files_for_archive
            )
            
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Archive created: {archive_path}\n")
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("All processing completed successfully. Now safe to clean temp directory.\n")
        
        # 11. 清理临时文件和temp目录（在所有处理完成后）
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Starting cleanup...\n")
        
        # 清理所有临时文件
        for f in temp_files:
            if os.path.exists(f):
                try:
                    os.remove(f)
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Removed temp file: {f}\n")
                except Exception as e:
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"Failed to remove temp file {f}: {e}\n")
        
        # 彻底清空temp目录
        clean_temp_directory()
        
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write("Cleanup completed.\n")
        
        # 返回结果
        if create_archive and archive_path:
            return archive_path
        else:
            return output_model
    except Exception as e:
        # 异常情况下也要清理
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Exception occurred: {e}\n")
            logf.write("Starting emergency cleanup...\n")
        
        # 清理所有临时文件
        for f in temp_files:
            if os.path.exists(f):
                try:
                    os.remove(f)
                except Exception:
                    pass
        
        # 彻底清空temp目录
        clean_temp_directory()
        
        raise

@mcp.tool()
async def analyze_model(
    input_path: str,
    analysis_type: str = "auto",
    include_folder_analysis: bool = False,
    include_validation: bool = False
) -> Dict[str, Any]:
    """
    统一的模型分析工具，支持网格质量分析、文件夹分析、包完整性验证等。
    
    Args:
        input_path (str): 输入路径（模型文件、文件夹或URL）
        analysis_type (str): 分析类型
            - "auto": 自动检测输入类型并选择合适的分析方式
            - "quality": 仅分析网格质量
            - "folder": 仅分析OBJ文件夹结构
            - "validation": 仅验证OBJ包完整性
            - "full": 执行所有可用的分析
        include_folder_analysis (bool): 如果输入是单个OBJ文件，是否同时分析其所在文件夹
        include_validation (bool): 如果输入是OBJ文件，是否同时验证包完整性
    Returns:
        dict: 包含分析结果的字典
    Raises:
        RuntimeError: 分析失败时抛出
    """
    try:
        result = {
            "input_path": input_path,
            "analysis_timestamp": datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
            "analyses_performed": []
        }
        
        # 自动检测输入类型
        is_folder = os.path.isdir(input_path)
        is_obj_file = input_path.lower().endswith('.obj') and os.path.isfile(input_path)
        is_model_file = (input_path.lower().endswith(('.obj', '.glb')) and os.path.isfile(input_path)) or is_url(input_path)
        
        # 决定要执行的分析
        if analysis_type == "auto":
            if is_folder:
                analyses_to_run = ["folder", "quality"]
            elif is_obj_file:
                analyses_to_run = ["quality", "validation"]
                if include_folder_analysis:
                    analyses_to_run.append("folder")
            elif is_model_file:
                analyses_to_run = ["quality"]
            else:
                raise RuntimeError(f"Unsupported input type: {input_path}")
        elif analysis_type == "full":
            if is_folder:
                analyses_to_run = ["folder", "quality"]
            elif is_obj_file:
                analyses_to_run = ["quality", "validation", "folder"]
            elif is_model_file:
                analyses_to_run = ["quality"]
            else:
                raise RuntimeError(f"Unsupported input type for full analysis: {input_path}")
        else:
            analyses_to_run = [analysis_type]
        
        # 执行分析
        for analysis in analyses_to_run:
            try:
                if analysis == "quality":
                    # 网格质量分析
                    if is_folder:
                        # 文件夹模式：分析第一个OBJ文件
                        folder_analysis = analyze_obj_folder(input_path)
                        if folder_analysis.get("obj_files"):
                            first_obj = os.path.join(input_path, folder_analysis["obj_files"][0])
                            quality_result = check_mesh_quality(first_obj)
                        else:
                            quality_result = {"error": "No OBJ files found in folder"}
                    else:
                        # 处理GLB文件
                        if input_path.lower().endswith('.glb') or (is_url(input_path) and 'glb' in input_path.lower()):
                            temp_obj = get_temp_file('.obj')
                            if is_url(input_path):
                                local_file = download_to_temp(input_path)
                                glb_to_obj(local_file, temp_obj)
                                os.remove(local_file)
                            else:
                                glb_to_obj(input_path, temp_obj)
                            
                            quality_result = check_mesh_quality(temp_obj)
                            os.remove(temp_obj)
                        else:
                            # 处理OBJ文件或URL
                            if is_url(input_path):
                                local_file = download_to_temp(input_path)
                                quality_result = check_mesh_quality(local_file)
                                os.remove(local_file)
                            else:
                                quality_result = check_mesh_quality(input_path)
                    
                    # 添加推荐建议
                    if "faces" in quality_result:
                        current_faces = quality_result["faces"]
                        quality_result.update({
                            "recommended_target_faces": current_faces // 5 if current_faces > 5000 else current_faces,
                            "complexity_level": "high" if current_faces > 20000 else "medium" if current_faces > 5000 else "low",
                            "recommended_operation": "simplify" if quality_result.get("watertight", False) else "remesh",
                            "reduction_suggestions": {
                                "aggressive": current_faces // 10,
                                "moderate": current_faces // 5,
                                "conservative": current_faces // 2
                            }
                        })
                    
                    result["mesh_quality"] = quality_result
                    result["analyses_performed"].append("quality")
                
                elif analysis == "folder":
                    # 文件夹结构分析
                    if is_folder:
                        folder_result = analyze_obj_folder(input_path)
                    elif is_obj_file:
                        # 分析OBJ文件所在的文件夹
                        folder_path = os.path.dirname(input_path)
                        folder_result = analyze_obj_folder(folder_path)
                        folder_result["note"] = f"Analysis of folder containing {os.path.basename(input_path)}"
                    else:
                        folder_result = {"error": "Folder analysis not applicable for this input type"}
                    
                    result["folder_analysis"] = folder_result
                    result["analyses_performed"].append("folder")
                
                elif analysis == "validation":
                    # OBJ包完整性验证
                    if is_obj_file:
                        validation_result = validate_obj_package_internal(input_path)
                    elif is_folder:
                        # 验证文件夹中的主OBJ文件
                        folder_analysis = analyze_obj_folder(input_path)
                        if folder_analysis.get("obj_files"):
                            main_obj = os.path.join(input_path, folder_analysis["obj_files"][0])
                            validation_result = validate_obj_package_internal(main_obj)
                            validation_result["note"] = f"Validation of main OBJ file: {folder_analysis['obj_files'][0]}"
                        else:
                            validation_result = {"error": "No OBJ files found for validation"}
                    else:
                        validation_result = {"error": "Package validation only applicable for OBJ files"}
                    
                    result["package_validation"] = validation_result
                    result["analyses_performed"].append("validation")
                    
            except Exception as e:
                result[f"{analysis}_error"] = str(e)

        # 清空temp目录
        clean_temp_directory()
        
        return result
        
    except Exception as e:
        clean_temp_directory()
        raise RuntimeError(f"Failed to analyze model: {e}")



@mcp.tool()
async def test_blender_detection_tool() -> Dict[str, Any]:
    """
    测试Blender 3.6自动检测功能，返回详细的检测结果。
    用于诊断Blender检测问题和验证移植环境。
    
    Returns:
        Dict[str, Any]: 包含系统信息、检测结果和最终路径的详细报告
    """
    return test_blender_detection()

@mcp.tool()
async def manage_archives(
    action: str,
    archive_name: str = None,
    limit: int = 20,
    days_to_keep: int = 30,
    dry_run: bool = True,
    copy_to: str = None
) -> Dict[str, Any]:
    """
    统一的归档管理工具，支持列出、清理、复制归档等操作。
    
    Args:
        action (str): 操作类型
            - "list": 列出归档文件夹
            - "clean": 清理旧的归档文件夹
            - "copy": 复制指定的归档文件夹
            - "info": 获取归档目录的详细信息
        archive_name (str): 归档名称（用于copy操作）
        limit (int): 列出归档的数量限制（用于list操作）
        days_to_keep (int): 清理时保留的天数（用于clean操作）
        dry_run (bool): 是否只是预览而不实际删除（用于clean操作）
        copy_to (str): 复制到的目录路径（用于copy操作，可选）
    Returns:
        dict: 操作结果
    Raises:
        RuntimeError: 操作失败时抛出
    """
    try:
        if action == "list":
            # 列出归档
            if not os.path.exists(ARCHIVE_DIR):
                return {"action": "list", "archives": [], "total_count": 0, "total_size": 0}
            
            archives = []
            total_size = 0
            
            # 获取所有归档文件夹
            archive_dirs = []
            for filename in os.listdir(ARCHIVE_DIR):
                file_path = os.path.join(ARCHIVE_DIR, filename)
                if os.path.isdir(file_path):
                    archive_dirs.append((filename, file_path))
            
            # 按修改时间排序
            archive_dirs.sort(key=lambda x: os.path.getmtime(x[1]), reverse=True)
            
            # 处理前limit个文件夹
            for filename, dir_path in archive_dirs[:limit]:
                try:
                    # 计算文件夹大小
                    dir_size = 0
                    for dirpath, dirnames, filenames in os.walk(dir_path):
                        for f in filenames:
                            fp = os.path.join(dirpath, f)
                            if os.path.exists(fp):
                                dir_size += os.path.getsize(fp)
                    
                    dir_time = datetime.datetime.fromtimestamp(os.path.getmtime(dir_path))
                    total_size += dir_size
                    
                    # 尝试读取info.json
                    info = {}
                    info_path = os.path.join(dir_path, 'info.json')
                    if os.path.exists(info_path):
                        try:
                            with open(info_path, 'r', encoding='utf-8') as f:
                                info = json.load(f)
                        except Exception:
                            pass
                    
                    archives.append({
                        "dirname": filename,
                        "size": dir_size,
                        "size_mb": round(dir_size / (1024 * 1024), 2),
                        "created": dir_time.strftime('%Y-%m-%d %H:%M:%S'),
                        "processing_info": info.get("processing_info", {}),
                        "original_input": info.get("original_input", "Unknown")
                    })
                    
                except Exception:
                    continue
            
            return {
                "action": "list",
                "archives": archives,
                "total_count": len(archive_dirs),
                "total_size": total_size,
                "total_size_mb": round(total_size / (1024 * 1024), 2),
                "archive_directory": ARCHIVE_DIR
            }

        elif action == "clean":
            # 清理归档
            if not os.path.exists(ARCHIVE_DIR):
                return {"action": "clean", "deleted_count": 0, "freed_space": 0, "message": "Archive directory not found"}
            
            cutoff_time = datetime.datetime.now() - datetime.timedelta(days=days_to_keep)
            dirs_to_delete = []
            total_size = 0
            
            # 找出要删除的文件夹
            for filename in os.listdir(ARCHIVE_DIR):
                dir_path = os.path.join(ARCHIVE_DIR, filename)
                if os.path.isdir(dir_path):
                    try:
                        dir_time = datetime.datetime.fromtimestamp(os.path.getmtime(dir_path))
                        if dir_time < cutoff_time:
                            # 计算文件夹大小
                            dir_size = 0
                            for dirpath, dirnames, filenames in os.walk(dir_path):
                                for f in filenames:
                                    fp = os.path.join(dirpath, f)
                                    if os.path.exists(fp):
                                        dir_size += os.path.getsize(fp)
                            
                            dirs_to_delete.append({
                                "dirname": filename,
                                "size": dir_size,
                                "created": dir_time.strftime('%Y-%m-%d %H:%M:%S')
                            })
                            total_size += dir_size
                    except Exception:
                        continue
            
            deleted_count = 0
            if not dry_run:
                # 实际删除文件夹
                for dir_info in dirs_to_delete:
                    try:
                        dir_path = os.path.join(ARCHIVE_DIR, dir_info["dirname"])
                        shutil.rmtree(dir_path)
                        deleted_count += 1
                    except Exception:
                        continue
            
            return {
                "action": "clean",
                "dirs_to_delete" if dry_run else "deleted_dirs": dirs_to_delete,
                "deleted_count": deleted_count if not dry_run else len(dirs_to_delete),
                "freed_space": total_size,
                "freed_space_mb": round(total_size / (1024 * 1024), 2),
                "dry_run": dry_run,
                "cutoff_date": cutoff_time.strftime('%Y-%m-%d %H:%M:%S')
            }

        elif action == "copy":
            # 复制归档
            if not archive_name:
                raise RuntimeError("Archive name is required for copy operation")
            
            archive_path = os.path.join(ARCHIVE_DIR, archive_name)
            if not os.path.exists(archive_path):
                raise RuntimeError(f"Archive not found: {archive_name}")
            
            if not os.path.isdir(archive_path):
                raise RuntimeError(f"Archive is not a directory: {archive_name}")
            
            if copy_to is None:
                copy_to = os.path.join(OUTPUT_DIR, "extracted", archive_name)
            
            if os.path.exists(copy_to):
                shutil.rmtree(copy_to)
            shutil.copytree(archive_path, copy_to)
            
            return {
                "action": "copy",
                "archive_name": archive_name,
                "copied_to": copy_to,
                "success": True
            }
        
        elif action == "info":
            # 获取归档目录信息
            if not os.path.exists(ARCHIVE_DIR):
                return {"action": "info", "exists": False}
            
            total_archives = 0
            total_size = 0
            oldest_date = None
            newest_date = None
            
            for filename in os.listdir(ARCHIVE_DIR):
                dir_path = os.path.join(ARCHIVE_DIR, filename)
                if os.path.isdir(dir_path):
                    total_archives += 1
                    
                    # 计算大小
                    for dirpath, dirnames, filenames in os.walk(dir_path):
                        for f in filenames:
                            fp = os.path.join(dirpath, f)
                            if os.path.exists(fp):
                                total_size += os.path.getsize(fp)
                    
                    # 检查日期
                    dir_time = datetime.datetime.fromtimestamp(os.path.getmtime(dir_path))
                    if oldest_date is None or dir_time < oldest_date:
                        oldest_date = dir_time
                    if newest_date is None or dir_time > newest_date:
                        newest_date = dir_time
            
            return {
                "action": "info",
                "exists": True,
                "archive_directory": ARCHIVE_DIR,
                "total_archives": total_archives,
                "total_size": total_size,
                "total_size_mb": round(total_size / (1024 * 1024), 2),
                "oldest_archive": oldest_date.strftime('%Y-%m-%d %H:%M:%S') if oldest_date else None,
                "newest_archive": newest_date.strftime('%Y-%m-%d %H:%M:%S') if newest_date else None
            }
        
        else:
            raise RuntimeError(f"Unsupported action: {action}. Use 'list', 'clean', 'copy', or 'info'")
            
    except Exception as e:
        raise RuntimeError(f"Archive management failed: {e}")

def main():
    mcp.run(transport='stdio')

if __name__ == "__main__":
    main()