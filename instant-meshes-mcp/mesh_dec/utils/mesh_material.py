"""
mesh_material.py
材质、贴图增强、MTL修复、AI模型材质处理相关工具函数。
"""
# ... 迁移自 mesh_processing.py ...
# enhance_meshy_ai_materials, restore_obj_material

import os
from .common_utils import enhanced_is_texture_file
from .file_utils import safe_copy
from ..config import LOG_DIR, TEMP_DIR

def enhance_meshy_ai_materials(obj_path: str, log_file: str = None) -> None:
    """
    专门为Meshy AI等AI生成模型增强材质文件，确保法线贴图和金属贴图正确关联。
    Args:
        obj_path (str): OBJ文件路径
        log_file (str): 指定的日志文件路径
    """
    obj_dir = os.path.dirname(obj_path)
    obj_name = os.path.splitext(os.path.basename(obj_path))[0]
    mtl_path = os.path.join(obj_dir, f"{obj_name}.mtl")
    
    # 确定日志文件路径
    if log_file is None:
        log_file = f"{LOG_DIR}/instant_meshes.log"
    
    try:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"\n=== Enhance Meshy AI Materials ===\n")
            logf.write(f"Processing: {obj_path}\n")
            logf.write(f"MTL path: {mtl_path}\n")
        
        # 扫描目录中的所有贴图文件
        texture_files = {}
        texture_types = {
            'diffuse': ['_diffuse', '_albedo', '_basecolor', '_color', '_diff', '_alb'],
            'normal': ['_normal', '_norm', '_nrm', '_bump', '_normalgl'],
            'metallic': ['_metallic', '_metal', '_met', '_metalness'],
            'roughness': ['_roughness', '_rough', '_rgh'],
            'ao': ['_ao', '_ambient', '_occlusion'],
            'emission': ['_emission', '_emissive', '_emit']
        }
        
        # 额外的Meshy AI命名模式（更精确的模式匹配）
        meshy_patterns = {
            'diffuse': ['texture0', 'tex0', 'img0', 'image_0', 'image0', '_0.', 'texture_0', 'tex_0'],
            'normal': ['texture1', 'tex1', 'img1', 'image_1', 'image1', '_1.', 'texture_1', 'tex_1'],
            'metallic': ['texture2', 'tex2', 'img2', 'image_2', 'image2', '_2.', 'texture_2', 'tex_2'],
            'roughness': ['texture3', 'tex3', 'img3', 'image_3', 'image3', '_3.', 'texture_3', 'tex_3'],
            'ao': ['texture4', 'tex4', 'img4', 'image_4', 'image4', '_4.', 'texture_4', 'tex_4'],
            'emission': ['texture5', 'tex5', 'img5', 'image_5', 'image5', '_5.', 'texture_5', 'tex_5']
        }
        
        # 特殊的Meshy AI模式检测
        meshy_exact_patterns = {
            'image_0': 'diffuse',
            'image_2': 'normal', 
            'image_1': 'metallic_roughness',  # Meshy AI的复合贴图
            'image_3': 'roughness',
            'image_4': 'ao',
            'image_5': 'emission',
            'image0': 'diffuse',
            'image2': 'normal',
            'image1': 'metallic_roughness',  # Meshy AI的复合贴图 
            'image3': 'roughness',
            'image4': 'ao',
            'image5': 'emission'
        }
        
        # 记录详细的贴图分析过程
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Available files: {os.listdir(obj_dir)}\n")
        
        # 查找所有贴图文件
        for filename in os.listdir(obj_dir):
            if enhanced_is_texture_file(filename):
                lower_name = filename.lower()
                classified = False
                
                with open(log_file, "a", encoding="utf-8") as logf:
                    logf.write(f"Analyzing texture: {filename}\n")
                
                # 优先检查Meshy AI的精确命名模式
                base_name = os.path.splitext(filename.lower())[0]
                if base_name == 'image_0':
                    tex_type = 'diffuse'
                elif base_name == 'image_1':
                    tex_type = 'metallicroughness'
                elif base_name == 'image_2':
                    tex_type = 'normal'
                elif base_name == 'image_3':
                    tex_type = 'roughness'
                elif base_name == 'image_4':
                    tex_type = 'ao'
                elif base_name == 'image_5':
                    tex_type = 'emission'
                else:
                    # 旧逻辑保留
                    if base_name in meshy_exact_patterns:
                        tex_type = meshy_exact_patterns[base_name]
                    else:
                        tex_type = None
                if tex_type and tex_type not in texture_files:
                    texture_files[tex_type] = filename
                    classified = True
                    with open(log_file, "a", encoding="utf-8") as logf:
                        logf.write(f"  Classified as {tex_type} (Meshy AI exact pattern)\n")
                
                # 如果精确匹配失败，尝试标准命名模式
                if not classified:
                    for tex_type, suffixes in texture_types.items():
                        if any(suffix in lower_name for suffix in suffixes):
                            if tex_type not in texture_files:  # 避免重复
                                texture_files[tex_type] = filename
                                classified = True
                                with open(log_file, "a", encoding="utf-8") as logf:
                                    logf.write(f"  Classified as {tex_type} (standard naming)\n")
                                break
                
                # 如果标准模式失败，尝试Meshy AI模式
                if not classified:
                    for tex_type, patterns in meshy_patterns.items():
                        if any(pattern in lower_name for pattern in patterns):
                            if tex_type not in texture_files:  # 避免重复
                                texture_files[tex_type] = filename
                                classified = True
                                with open(log_file, "a", encoding="utf-8") as logf:
                                    logf.write(f"  Classified as {tex_type} (Meshy AI pattern)\n")
                                break
                
                # 如果都失败了，根据位置索引推断（针对类似texture0.jpg的情况）
                if not classified:
                    import re
                    # 查找文件名中的数字
                    numbers = re.findall(r'\d+', filename)
                    if numbers:
                        first_number = int(numbers[0])
                        if first_number == 0 and 'diffuse' not in texture_files:
                            texture_files['diffuse'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as diffuse (number index 0)\n")
                        elif first_number == 1 and 'normal' not in texture_files:
                            texture_files['normal'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as normal (number index 1)\n")
                        elif first_number == 2 and 'metallic_roughness' not in texture_files:
                            texture_files['metallic_roughness'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as metallic_roughness (number index 2)\n")
                        elif first_number == 3 and 'roughness' not in texture_files:
                            texture_files['roughness'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as roughness (number index 3)\n")
                        elif first_number == 4 and 'ao' not in texture_files:
                            texture_files['ao'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as ao (number index 4)\n")
                        elif first_number == 5 and 'emission' not in texture_files:
                            texture_files['emission'] = filename
                            classified = True
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"  Classified as emission (number index 5)\n")
                
                # 最后的fallback：如果没有漫反射贴图且这是唯一的贴图文件
                if not classified and 'diffuse' not in texture_files:
                    if not any(suffix in lower_name for type_list in texture_types.values() for suffix in type_list):
                        texture_files['diffuse'] = filename
                        with open(log_file, "a", encoding="utf-8") as logf:
                            logf.write(f"  Classified as diffuse (fallback)\n")
        
        with open(log_file if log_file else f"{LOG_DIR}/instant_meshes.log", "a", encoding="utf-8") as logf:
            logf.write(f"Final texture classification: {texture_files}\n")
        
        # 读取现有MTL文件
        with open(mtl_path, 'r', encoding='utf-8') as f:
            mtl_lines = f.readlines()
        
        # 重写MTL文件，确保包含所有贴图
        enhanced_lines = []
        current_material = None
        material_section_started = False
        
        for line in mtl_lines:
            if line.startswith('newmtl '):
                enhanced_lines.append(line)
                current_material = line.strip().split()[1]
                material_section_started = True
                
                # 添加所有可用的贴图映射
                for tex_type, filename in texture_files.items():
                    if tex_type == 'diffuse':
                        enhanced_lines.append(f"map_Kd {filename}\n")
                    elif tex_type == 'normal':
                        enhanced_lines.append(f"map_bump {filename}\n")
                        enhanced_lines.append(f"bump {filename}\n")  # 兼容性
                    elif tex_type == 'metallic':
                        enhanced_lines.append(f"map_Pm {filename}\n")  # PBR metallic
                        enhanced_lines.append(f"map_Ks {filename}\n")  # 兼容性
                    elif tex_type == 'metallic_roughness':
                        # Meshy AI的复合贴图，同时用于金属度和粗糙度
                        enhanced_lines.append(f"map_Pm {filename}\n")  # PBR metallic
                        enhanced_lines.append(f"map_Pr {filename}\n")  # PBR roughness
                        enhanced_lines.append(f"map_Ks {filename}\n")  # 兼容性 - 镜面反射
                        enhanced_lines.append(f"map_Ns {filename}\n")  # 兼容性 - 镜面指数
                    elif tex_type == 'roughness':
                        enhanced_lines.append(f"map_Pr {filename}\n")  # PBR roughness
                        enhanced_lines.append(f"map_Ns {filename}\n")  # 兼容性
                    elif tex_type == 'ao':
                        enhanced_lines.append(f"map_Ka {filename}\n")
                    elif tex_type == 'emission':
                        enhanced_lines.append(f"map_Ke {filename}\n")
                
            elif material_section_started and line.strip() and not line.startswith('#'):
                # 跳过原有的贴图映射行，使用我们生成的
                if not line.lower().startswith(('map_', 'bump ')):
                    enhanced_lines.append(line)
            else:
                enhanced_lines.append(line)
        
        # 如果没有材质定义，创建默认材质
        if not any(line.startswith('newmtl ') for line in mtl_lines):
            enhanced_lines.append(f"newmtl {obj_name}\n")
            enhanced_lines.append("Ka 1.000 1.000 1.000\n")
            enhanced_lines.append("Kd 0.800 0.800 0.800\n")
            enhanced_lines.append("Ks 0.500 0.500 0.500\n")
            enhanced_lines.append("Ns 50.000\n")
            
            # 添加所有贴图
            for tex_type, filename in texture_files.items():
                if tex_type == 'diffuse':
                    enhanced_lines.append(f"map_Kd {filename}\n")
                elif tex_type == 'normal':
                    enhanced_lines.append(f"map_bump {filename}\n")
                elif tex_type == 'metallic':
                    enhanced_lines.append(f"map_Pm {filename}\n")
                elif tex_type == 'metallic_roughness':
                    # Meshy AI的复合贴图
                    enhanced_lines.append(f"map_Pm {filename}\n")  # PBR metallic
                    enhanced_lines.append(f"map_Pr {filename}\n")  # PBR roughness
                elif tex_type == 'roughness':
                    enhanced_lines.append(f"map_Pr {filename}\n")
                elif tex_type == 'ao':
                    enhanced_lines.append(f"map_Ka {filename}\n")
                elif tex_type == 'emission':
                    enhanced_lines.append(f"map_Ke {filename}\n")
        
        # 写回增强的MTL文件
        with open(mtl_path, 'w', encoding='utf-8') as f:
            f.writelines(enhanced_lines)
        
        # 确保OBJ文件引用正确的MTL
        with open(obj_path, 'r', encoding='utf-8') as f:
            obj_lines = f.readlines()
        
        # 检查并修正mtllib行
        obj_updated = False
        for i, line in enumerate(obj_lines):
            if line.lower().startswith('mtllib'):
                expected_mtl = f"{obj_name}.mtl"
                if expected_mtl not in line:
                    obj_lines[i] = f"mtllib {expected_mtl}\n"
                    obj_updated = True
                break
        else:
            # 如果没有mtllib行，添加一个
            obj_lines.insert(0, f"mtllib {obj_name}.mtl\n")
            obj_updated = True
        
        if obj_updated:
            with open(obj_path, 'w', encoding='utf-8') as f:
                f.writelines(obj_lines)
        
    except Exception as e:
        # 记录到日志文件而不是控制台
        pass



def rename_tripo_textures_and_update_mtl(model_dir: str, obj_name: str = None, mtl_name: str = None) -> None:
    """
    自动识别Tripo导出的贴图文件，按序号重命名为标准PBR格式，并自动修正MTL文件中的贴图引用。
    支持tripo_image_xxx_N.png和tripo_image_xxx_N_diffuse.png两种情况。
    Args:
        model_dir (str): 模型目录，包含OBJ/MTL/贴图
        obj_name (str): OBJ文件名（可选，仅用于日志）
        mtl_name (str): MTL文件名（可选，默认自动查找）
    """
    import os
    import re
    # 贴图序号与PBR用途映射
    pbr_map = {
        0: 'basecolor',
        1: 'metallic',
        2: 'roughness',
        3: 'normal',
    }
    files = os.listdir(model_dir)
    # 支持无_diffuse和有_diffuse两种情况
    tripo_tex = [f for f in files if re.match(r'tripo_image_.*_(\d)(?:_diffuse)?\.png$', f)]
    rename_map = {}
    for f in tripo_tex:
        m = re.match(r'(tripo_image_.*)_(\d)(?:_diffuse)?\.png$', f)
        if not m:
            continue
        idx = int(m.group(2))
        pbr = pbr_map.get(idx)
        if not pbr:
            continue
        # 统一重命名为tripo_image_xxx_{pbr}.png
        new_name = f"{m.group(1)}_{pbr}.png"
        src = os.path.join(model_dir, f)
        dst = os.path.join(model_dir, new_name)
        if src != dst:
            # 若目标已存在，先删除
            if os.path.exists(dst):
                os.remove(dst)
            os.rename(src, dst)
        rename_map[f] = new_name
    # 自动查找MTL文件
    if not mtl_name:
        mtl_name = next((f for f in files if f.lower().endswith('.mtl')), None)
    if not mtl_name:
        return
    mtl_path = os.path.join(model_dir, mtl_name)
    # 读取并修正MTL
    with open(mtl_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    new_lines = []
    for line in lines:
        for old, new in rename_map.items():
            if old in line:
                line = line.replace(old, new)
        new_lines.append(line)
    with open(mtl_path, 'w', encoding='utf-8') as f:
        f.writelines(new_lines)


def restore_obj_material(obj_path: str, original_obj_path: str):
    """
    将原始OBJ的mtl和贴图引用复制到新OBJ，修正mtllib和usemtl，保证贴图不丢失。
    确保所有贴图都在temp目录中处理，最终GLB包含完整材质。
    """
    if not os.path.exists(original_obj_path):
        return
        
    orig_dir = os.path.dirname(original_obj_path)
    new_dir = os.path.dirname(obj_path)
    
    try:
        with open(original_obj_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except Exception:
        return
    
    mtl_files = [line.split()[1] for line in lines if line.lower().startswith('mtllib')]
    if not mtl_files:
        return
        
    mtl_file = mtl_files[0]
    # 处理可能的路径分隔符
    mtl_file = mtl_file.replace('\\', '/').split('/')[-1]
    
    # 优先在temp目录查找，再在原始目录查找
    temp_mtl_path = os.path.join(TEMP_DIR, mtl_file)
    orig_mtl_path = os.path.join(orig_dir, mtl_file)
    
    mtl_source_path = None
    if os.path.exists(temp_mtl_path):
        mtl_source_path = temp_mtl_path
    elif os.path.exists(orig_mtl_path):
        mtl_source_path = orig_mtl_path
    
    if not mtl_source_path:
        return
        
    # 复制MTL文件到新目录
    try:
        safe_copy(mtl_source_path, new_dir)
    except Exception:
        return
    
    # 复制贴图文件
    try:
        with open(mtl_source_path, 'r', encoding='utf-8') as f:
            mtl_lines = f.readlines()
            
        for line in mtl_lines:
            line_lower = line.lower().strip()
            if line_lower.startswith(('map_kd', 'map_ka', 'map_ks', 'map_ns', 'map_bump', 'map_d', 'map_normal', 'map_normalgl', 'map_orm', 'map_roughness', 'map_metallic', 'map_ao', 'map_emissive', 'map_opacity', 'map_displacement', 'map_height')):
                parts = line.split()
                if len(parts) > 1:
                    tex_file = parts[-1]  # 取最后一个部分作为文件名
                    # 处理可能的路径分隔符
                    tex_file = tex_file.replace('\\', '/').split('/')[-1]
                    
                    # 优先在temp目录查找贴图文件，再在原始目录查找
                    tex_source_path = None
                    temp_tex_path = os.path.join(TEMP_DIR, tex_file)
                    orig_tex_path = os.path.join(orig_dir, tex_file)
                    
                    if os.path.exists(temp_tex_path):
                        tex_source_path = temp_tex_path
                    elif os.path.exists(orig_tex_path):
                        tex_source_path = orig_tex_path
                    
                    if tex_source_path:
                        try:
                            safe_copy(tex_source_path, new_dir)
                        except Exception:
                            continue
    except Exception:
        pass
    
    # 修正新OBJ的mtllib引用
    try:
        with open(obj_path, 'r', encoding='utf-8') as f:
            obj_lines = f.readlines()
            
        new_obj_lines = []
        mtl_written = False
        for line in obj_lines:
            if line.lower().startswith('mtllib'):
                if not mtl_written:
                    new_obj_lines.append(f'mtllib {mtl_file}\n')
                    mtl_written = True
            else:
                new_obj_lines.append(line)
                
        if not mtl_written:
            new_obj_lines.insert(0, f'mtllib {mtl_file}\n')
            
        with open(obj_path, 'w', encoding='utf-8') as f:
            f.writelines(new_obj_lines)
    except Exception:
        pass

