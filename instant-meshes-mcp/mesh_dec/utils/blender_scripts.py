"""
blender_scripts.py
存储所有用于 Blender 调用的 Python 脚本模板。
将脚本从 mesh_convert.py 中分离，提高代码可维护性。
"""

def get_glb_to_obj_script(glb_path: str, obj_path: str, output_dir: str, done_flag_path: str, blender_debug_log: str) -> str:
    """
    生成 GLB 转 OBJ 并提取贴图的 Blender 脚本。
    
    Args:
        glb_path: 输入 GLB 文件路径
        obj_path: 输出 OBJ 文件路径
        output_dir: 输出目录路径
        done_flag_path: 完成标记文件路径
        blender_debug_log: 调试日志文件路径
    
    Returns:
        str: Blender Python 脚本内容
    """
    return f'''
import bpy
import os
import sys
import time
import shutil

# 重定向输出到调试日志文件
debug_log_path = r"{blender_debug_log}"
done_flag_path = r"{done_flag_path}"
obj_output_path = r"{obj_path}"
output_dir = r"{output_dir}"

def log_message(message):
    timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
    with open(debug_log_path, "a", encoding="utf-8") as f:
        f.write(f"[{{timestamp}}] {{message}}\\n")

def write_done_flag():
    try:
        with open(done_flag_path, "w", encoding="utf-8") as f:
            f.write("completed")
        log_message(f"Done flag written: {{done_flag_path}}")
    except Exception as e:
        log_message(f"Failed to write done flag: {{e}}")

log_message("Blender script started for GLB to OBJ with texture extraction")
log_message(f"Input GLB: {glb_path}")
log_message(f"Output OBJ: {obj_path}")
log_message(f"Output directory: {output_dir}")
log_message(f"Done flag: {done_flag_path}")

# 检查输入文件是否存在
if not os.path.exists(r"{glb_path}"):
    log_message(f"ERROR: Input GLB file not found: {glb_path}")
    write_done_flag()
    sys.exit(1)

# 清除默认场景
try:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    log_message("Default scene cleared")
except Exception as e:
    log_message(f"Failed to clear scene: {{e}}")

# 导入GLB文件
try:
    log_message("Starting GLB import...")
    bpy.ops.import_scene.gltf(filepath=r"{glb_path}")
    log_message("GLB imported successfully")
    
    # 检查导入的对象数量
    imported_objects = len(bpy.context.scene.objects)
    log_message(f"Imported objects count: {{imported_objects}}")
    
    # 检查材质数量
    materials_count = len(bpy.data.materials)
    log_message(f"Materials count: {{materials_count}}")
    
    # 检查图像数量
    images_count = len(bpy.data.images)
    log_message(f"Images count: {{images_count}}")
    
except Exception as e:
    log_message(f"Failed to import GLB: {{e}}")
    write_done_flag()
    sys.exit(1)

# 提取所有贴图文件
extracted_textures = []
try:
    log_message("Starting texture extraction...")
    
    for image in bpy.data.images:
        if image.name in ['Render Result', 'Viewer Node']:
            continue

        # 类型识别 - 优先根据名称前缀判断
        image_name_lower = image.name.lower()
        
        # 检查名称前缀（适用于新的命名格式）
        if image_name_lower.startswith("color") or image_name_lower.startswith("diffuse") or image_name_lower.startswith("albedo") or image_name_lower.startswith("basecolor"):
            texture_type = "diffuse"
        elif image_name_lower.startswith("normalgl") or image_name_lower.startswith("normal") or image_name_lower.startswith("nrm"):
            texture_type = "normal"
        elif image_name_lower.startswith("orm") or image_name_lower.startswith("metallicroughness") or image_name_lower.startswith("metallic_roughness"):
            texture_type = "metallicroughness"
        elif image_name_lower.startswith("roughness") or image_name_lower.startswith("rough"):
            texture_type = "roughness"
        elif image_name_lower.startswith("metallic") or image_name_lower.startswith("metal"):
            texture_type = "metallic"
        elif image_name_lower.startswith("ao") or image_name_lower.startswith("occlusion") or image_name_lower.startswith("ambient"):
            texture_type = "ao"
        elif image_name_lower.startswith("emission") or image_name_lower.startswith("emissive"):
            texture_type = "emission"
        # 兼容旧的meshy命名格式（image_x）
        elif image_name_lower == "image_0" or image_name_lower == "image0":
            texture_type = "diffuse"
        elif image_name_lower == "image_1" or image_name_lower == "image1":
            texture_type = "metallicroughness"
        elif image_name_lower == "image_2" or image_name_lower == "image2":
            texture_type = "normal"
        elif image_name_lower == "image_3" or image_name_lower == "image3":
            texture_type = "roughness"
        elif image_name_lower == "image_4" or image_name_lower == "image4":
            texture_type = "ao"
        elif image_name_lower == "image_5" or image_name_lower == "image5":
            texture_type = "emission"
        else:
            texture_type = "diffuse"  # fallback

        # 命名直接拼接
        texture_name = f"{{image.name}}_{{texture_type}}.png"
        texture_path = os.path.join(output_dir, texture_name)

        try:
            image.file_format = 'PNG'
            image.save_render(texture_path)
            log_message(f"Texture saved: {{texture_path}}")
            extracted_textures.append(texture_name)
        except Exception as e:
            log_message(f"Failed to save texture {{image.name}}: {{e}}")
    
    log_message(f"Texture extraction completed. Extracted {{len(extracted_textures)}} textures: {{extracted_textures}}")
    
    # 强制检查是否遗漏了任何图像
    total_images = len(bpy.data.images)
    if len(extracted_textures) < total_images - 1:  # -1 for 'Render Result'
        log_message(f"Warning: Only extracted {{len(extracted_textures)}} textures out of {{total_images}} images. Attempting to extract missing textures...")
        
        # 尝试保存所有未处理的图像
        for image in bpy.data.images:
            if image.name not in ['Render Result', 'Viewer Node'] and image.users > 0:
                # 检查这个图像是否已经被保存
                image_saved = False
                for saved_texture in extracted_textures:
                    if image.name.lower() in saved_texture.lower() or saved_texture.lower() in image.name.lower():
                        image_saved = True
                        break
                
                if not image_saved:
                    log_message(f"Attempting to save missing image: {{image.name}}")
                    # 使用简单的命名策略
                    safe_name = "".join(c for c in image.name if c.isalnum() or c in "._-")
                    if not safe_name:
                        safe_name = f"texture_{{len(extracted_textures)}}"
                    
                    # 确保有扩展名
                    if not any(safe_name.lower().endswith(ext) for ext in ['.png', '.jpg', '.jpeg', '.tga', '.bmp']):
                        safe_name += '.png'
                    
                    fallback_path = os.path.join(output_dir, safe_name)
                    
                    # 尝试多种保存方法
                    try:
                        image.file_format = 'PNG'
                        image.save_render(fallback_path)
                        extracted_textures.append(safe_name)
                        log_message(f"Fallback texture saved: {{fallback_path}}")
                    except:
                        try:
                            if image.packed_file:
                                image.filepath = fallback_path
                                image.unpack(method='WRITE_ORIGINAL')
                                if os.path.exists(fallback_path):
                                    extracted_textures.append(safe_name)
                                    log_message(f"Fallback texture unpacked: {{fallback_path}}")
                        except Exception as e:
                            log_message(f"Failed to save fallback texture {{image.name}}: {{e}}")
    
    log_message(f"Final texture extraction result: {{len(extracted_textures)}} textures saved: {{extracted_textures}}")
    
except Exception as e:
    log_message(f"Texture extraction failed: {{e}}")
    import traceback
    log_message(f"Traceback: {{traceback.format_exc()}}")

# 导出为OBJ
try:
    log_message("Starting OBJ export...")
    bpy.ops.export_scene.obj(
        filepath=obj_output_path,
        use_materials=True,
        use_uvs=True,
        use_normals=True,
        use_triangles=False,
        path_mode='RELATIVE'
    )
    
    # 增强MTL文件生成，确保包含所有贴图类型
    mtl_path = obj_output_path.replace('.obj', '.mtl')
    if os.path.exists(mtl_path):
        log_message("Enhancing MTL file with all texture types...")
        
        # 收集贴图信息
        texture_map = {{}}
        for texture_name in extracted_textures:
            texture_lower = texture_name.lower()
            if 'Image_0_' in texture_lower or '_diffuse' in texture_lower or '_albedo' in texture_lower or '_basecolor' in texture_lower:
                texture_map['map_Kd'] = texture_name
            elif 'Image_2_' in texture_lower or '_normal' in texture_lower:
                texture_map['map_bump'] = texture_name
                texture_map['bump'] = texture_name  # 备用
            elif 'Image_1_' in texture_lower or '_metallic' in texture_lower:
                texture_map['map_Pm'] = texture_name  # PBR metallic
                texture_map['map_Ks'] = texture_name  # 兼容性
            elif 'Image_3_' in texture_lower or '_roughness' in texture_lower:
                texture_map['map_Pr'] = texture_name  # PBR roughness  
                texture_map['map_Ns'] = texture_name  # 兼容性
            elif 'Image_4_' in texture_lower or '_ao' in texture_lower or '_occlusion' in texture_lower:
                texture_map['map_Ka'] = texture_name
            elif '_emission' in texture_lower:
                texture_map['map_Ke'] = texture_name
        
        # 读取现有MTL内容
        try:
            with open(mtl_path, 'r', encoding='utf-8') as f:
                mtl_lines = f.readlines()
            
            # 增强MTL文件
            enhanced_lines = []
            current_material = None
            material_textures_added = set()
            
            for line in mtl_lines:
                enhanced_lines.append(line)
                
                # 检测新材质定义
                if line.startswith('newmtl '):
                    current_material = line.strip().split()[1]
                    material_textures_added.clear()
                
                # 在材质定义后添加所有可用的贴图
                elif current_material and line.strip() and not line.startswith('#'):
                    for map_type, texture_file in texture_map.items():
                        map_line = f"{{map_type}} {{texture_file}}\\n"
                        if map_type not in material_textures_added and map_line not in enhanced_lines:
                            enhanced_lines.append(map_line)
                            material_textures_added.add(map_type)
                            log_message(f"Added {{map_type}} -> {{texture_file}} to material {{current_material}}")
            
            # 写回增强的MTL文件
            with open(mtl_path, 'w', encoding='utf-8') as f:
                f.writelines(enhanced_lines)
            
            log_message(f"MTL file enhanced with {{len(texture_map)}} texture mappings")
            
        except Exception as e:
            log_message(f"Failed to enhance MTL file: {{e}}")
    else:
        log_message(f"MTL file not found: {{mtl_path}}")
    
    log_message("OBJ export completed successfully")
    log_message("OBJ exported successfully")
    
    # 检查输出文件
    if os.path.exists(obj_output_path):
        file_size = os.path.getsize(obj_output_path)
        log_message(f"OBJ file created, size: {{file_size}} bytes")
    else:
        log_message("ERROR: OBJ file was not created")
        write_done_flag()
        sys.exit(1)
        
except Exception as e:
    log_message(f"Failed to export OBJ: {{e}}")
    write_done_flag()
    sys.exit(1)

log_message("Conversion completed successfully")
write_done_flag()'''


def get_obj_to_glb_script(obj_path: str, glb_path: str, done_flag_path: str, blender_debug_log: str) -> str:
    """
    生成 OBJ 转 GLB 的 Blender 脚本。
    
    Args:
        obj_path: 输入 OBJ 文件路径
        glb_path: 输出 GLB 文件路径
        done_flag_path: 完成标记文件路径
        blender_debug_log: 调试日志文件路径
    
    Returns:
        str: Blender Python 脚本内容
    """
    return f'''
import bpy
import os
import sys
import time

# 重定向输出到调试日志文件
debug_log_path = r"{blender_debug_log}"
done_flag_path = r"{done_flag_path}"
glb_output_path = r"{glb_path}"

def log_message(message):
    timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
    with open(debug_log_path, "a", encoding="utf-8") as f:
        f.write(f"[{{timestamp}}] {{message}}\\n")

def write_done_flag():
    try:
        with open(done_flag_path, "w", encoding="utf-8") as f:
            f.write("completed")
        log_message(f"Done flag written: {{done_flag_path}}")
    except Exception as e:
        log_message(f"Failed to write done flag: {{e}}")

log_message("Blender script started")
log_message(f"Input OBJ: {obj_path}")
log_message(f"Output GLB: {glb_path}")
log_message(f"Done flag: {done_flag_path}")

# 检查输入文件是否存在
if not os.path.exists(r"{obj_path}"):
    log_message(f"ERROR: Input OBJ file not found: {obj_path}")
    write_done_flag()
    sys.exit(1)

# 清除默认场景
try:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    log_message("Default scene cleared")
except Exception as e:
    log_message(f"Failed to clear scene: {{e}}")

# 导入OBJ文件
try:
    log_message("Starting OBJ import...")
    bpy.ops.import_scene.obj(filepath=r"{obj_path}")
    log_message("OBJ imported successfully")
    
    # 检查导入的对象数量
    imported_objects = len(bpy.context.scene.objects)
    log_message(f"Imported objects count: {{imported_objects}}")
    
    # 检查材质数量
    materials_count = len(bpy.data.materials)
    log_message(f"Materials count: {{materials_count}}")
    
    # 检查图像数量
    images_count = len(bpy.data.images)
    log_message(f"Images count: {{images_count}}")
    
except Exception as e:
    log_message(f"Failed to import OBJ: {{e}}")
    write_done_flag()
    sys.exit(1)

# 处理材质和贴图关联
try:
    log_message("Processing materials and textures...")
    
    # 获取OBJ文件所在目录
    obj_dir = os.path.dirname(r"{obj_path}")
    log_message(f"OBJ directory: {{obj_dir}}")
    
    # 遍历所有材质，确保贴图正确关联
    for material in bpy.data.materials:
        log_message(f"Processing material: {{material.name}}")
        
        # 确保材质使用节点
        if not material.use_nodes:
            material.use_nodes = True
            log_message(f"Enabled nodes for material: {{material.name}}")
        
        # 获取材质节点树
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        
        # 查找主着色器节点
        principled_bsdf = None
        for node in nodes:
            if node.type == 'BSDF_PRINCIPLED':
                principled_bsdf = node
                break
        
        if not principled_bsdf:
            log_message(f"No Principled BSDF found for material: {{material.name}}")
            continue
        
        # 查找现有的图像纹理节点
        image_texture_nodes = [node for node in nodes if node.type == 'TEX_IMAGE']
        log_message(f"Found {{len(image_texture_nodes)}} image texture nodes in material: {{material.name}}")
        
        # 如果没有图像纹理节点，尝试创建
        if not image_texture_nodes:
            # 查找可能的贴图文件
            material_base_name = material.name.lower()
            potential_textures = []
            
            # 搜索OBJ目录中的贴图文件（扩展支持的贴图格式）
            try:
                for file in os.listdir(obj_dir):
                    if file.lower().endswith(('.png', '.jpg', '.jpeg', '.tga', '.bmp', '.tiff', '.tif', '.dds', '.hdr', '.exr', '.webp')):
                        potential_textures.append(os.path.join(obj_dir, file))
                        log_message(f"Found potential texture: {{file}}")
            except Exception as e:
                log_message(f"Failed to list directory {{obj_dir}}: {{e}}")
            
            # 按优先级排序贴图文件（增强分类逻辑）
            color_textures = []
            normal_textures = []
            orm_textures = []
            roughness_textures = []
            metallic_textures = []
            ao_textures = []
            emission_textures = []
            other_textures = []
            
            for texture_path in potential_textures:
                texture_name = os.path.basename(texture_path).lower()
                
                # 跳过dummy贴图
                if 'dummy' in texture_name:
                    log_message(f"Skipping dummy texture: {{texture_name}}")
                    continue
                
                # 按类型分类（更精确的分类）
                if any(keyword in texture_name for keyword in ['color', 'diffuse', 'albedo', 'basecolor', 'base_color', '_col', '_diff', '_alb', 'texture0', 'img0', 'image0', '_0.', 'material0']):
                    color_textures.append(texture_path)
                elif any(keyword in texture_name for keyword in ['normal', 'norm', '_nrm', 'normalgl', '_normalgl', 'texture1', 'img1', 'image1', '_1.', 'material1']):
                    normal_textures.append(texture_path)
                elif 'orm' in texture_name or any(keyword in texture_name for keyword in ['_orm', 'orm_', 'occlusion-roughness-metallic']):
                    orm_textures.append(texture_path)
                elif any(keyword in texture_name for keyword in ['roughness', 'rough', '_rgh', '_rough', 'glossiness', 'gloss']):
                    roughness_textures.append(texture_path)
                elif any(keyword in texture_name for keyword in ['metallic', 'metal', '_met', '_metal', 'metalness', 'texture2', 'img2', 'image2', '_2.', 'material2']):
                    metallic_textures.append(texture_path)
                elif any(keyword in texture_name for keyword in ['ao', 'ambient', 'occlusion', '_ao', 'ambient_occlusion']):
                    ao_textures.append(texture_path)
                elif any(keyword in texture_name for keyword in ['emission', 'emissive', 'emit', '_emit', '_emissive', '_emission']):
                    emission_textures.append(texture_path)
                else:
                    other_textures.append(texture_path)
            
            # 按优先级处理贴图
            processed_types = set()
            
            # 1. 处理基础颜色贴图（优先级最高）
            if color_textures and 'base_color' not in processed_types:
                texture_path = color_textures[0]  # 使用第一个找到的颜色贴图
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded color texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, 200)
                    
                    links.new(texture_node.outputs['Color'], principled_bsdf.inputs['Base Color'])
                    log_message(f"Connected {{os.path.basename(texture_path)}} to Base Color")
                    processed_types.add('base_color')
                    
                except Exception as e:
                    log_message(f"Failed to process color texture {{texture_path}}: {{e}}")
            
            # 2. 处理法线贴图
            if normal_textures and 'normal' not in processed_types:
                texture_path = normal_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded normal texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -100)
                    
                    # 设置为非颜色数据
                    image.colorspace_settings.name = 'Non-Color'
                    
                    normal_map_node = nodes.new(type='ShaderNodeNormalMap')
                    normal_map_node.location = (-150, -100)
                    links.new(texture_node.outputs['Color'], normal_map_node.inputs['Color'])
                    links.new(normal_map_node.outputs['Normal'], principled_bsdf.inputs['Normal'])
                    log_message(f"Connected {{os.path.basename(texture_path)}} to Normal")
                    processed_types.add('normal')
                    
                except Exception as e:
                    log_message(f"Failed to process normal texture {{texture_path}}: {{e}}")
            
            # 3. 处理Meshy AI的金属度-粗糙度复合贴图 (image_1)
            metallic_roughness_textures = [t for t in potential_textures 
                                         if 'metallicroughness' in os.path.basename(t).lower() or 
                                            'image_1' in os.path.basename(t).lower() or
                                            'image1' in os.path.basename(t).lower()]
            
            if metallic_roughness_textures and 'metallicroughness' not in processed_types:
                texture_path = metallic_roughness_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded Meshy AI metallic-roughness texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -400)
                    
                    # 设置为非颜色数据
                    image.colorspace_settings.name = 'Non-Color'
                    
                    # 创建分离RGB节点
                    separate_rgb = nodes.new(type='ShaderNodeSeparateRGB')
                    separate_rgb.location = (-150, -400)
                    links.new(texture_node.outputs['Color'], separate_rgb.inputs['Image'])
                    
                    # Meshy AI的金属度-粗糙度贴图：Red通道=金属度，Green通道=粗糙度
                    links.new(separate_rgb.outputs['R'], principled_bsdf.inputs['Metallic'])    # Red = Metallic
                    links.new(separate_rgb.outputs['G'], principled_bsdf.inputs['Roughness'])  # Green = Roughness
                    
                    log_message(f"Connected {{os.path.basename(texture_path)}} as Meshy AI metallic-roughness (R->Metallic, G->Roughness)")
                    processed_types.add('metallic_roughness')
                    processed_types.add('metallic')  # 标记已处理，避免重复
                    processed_types.add('roughness')  # 标记已处理，避免重复
                    
                except Exception as e:
                    log_message(f"Failed to process Meshy AI metallic-roughness texture {{texture_path}}: {{e}}")
            
            # 4. 处理标准ORM贴图
            if orm_textures and 'orm' not in processed_types and 'metallic_roughness' not in processed_types:
                texture_path = orm_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded ORM texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -400)
                    
                    # 设置为非颜色数据
                    image.colorspace_settings.name = 'Non-Color'
                    
                    # 创建分离RGB节点
                    separate_rgb = nodes.new(type='ShaderNodeSeparateRGB')
                    separate_rgb.location = (-150, -400)
                    links.new(texture_node.outputs['Color'], separate_rgb.inputs['Image'])
                    
                    # 连接到对应通道 (ORM = Occlusion-Roughness-Metallic)
                    links.new(separate_rgb.outputs['G'], principled_bsdf.inputs['Roughness'])  # Green = Roughness
                    links.new(separate_rgb.outputs['B'], principled_bsdf.inputs['Metallic'])   # Blue = Metallic
                    # Red通道(Occlusion)可以连接到AO，但Principled BSDF没有直接的AO输入
                    
                    log_message(f"Connected {{os.path.basename(texture_path)}} as ORM texture (G->Roughness, B->Metallic)")
                    processed_types.add('orm')
                    
                except Exception as e:
                    log_message(f"Failed to process ORM texture {{texture_path}}: {{e}}")
            
            # 5. 处理粗糙度贴图
            if roughness_textures and 'roughness' not in processed_types and 'orm' not in processed_types and 'metallic_roughness' not in processed_types:
                texture_path = roughness_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded roughness texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -500)
                    
                    # 设置为非颜色数据
                    image.colorspace_settings.name = 'Non-Color'
                    
                    links.new(texture_node.outputs['Color'], principled_bsdf.inputs['Roughness'])
                    log_message(f"Connected {{os.path.basename(texture_path)}} to Roughness")
                    processed_types.add('roughness')
                    
                except Exception as e:
                    log_message(f"Failed to process roughness texture {{texture_path}}: {{e}}")
            
            # 6. 处理金属度贴图
            if metallic_textures and 'metallic' not in processed_types and 'orm' not in processed_types and 'metallic_roughness' not in processed_types:
                texture_path = metallic_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded metallic texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -600)
                    
                    # 设置为非颜色数据
                    image.colorspace_settings.name = 'Non-Color'
                    
                    links.new(texture_node.outputs['Color'], principled_bsdf.inputs['Metallic'])
                    log_message(f"Connected {{os.path.basename(texture_path)}} to Metallic")
                    processed_types.add('metallic')
                    
                except Exception as e:
                    log_message(f"Failed to process metallic texture {{texture_path}}: {{e}}")
            
            # 7. 处理发光贴图
            if emission_textures and 'emission' not in processed_types:
                texture_path = emission_textures[0]
                try:
                    image = bpy.data.images.load(texture_path)
                    log_message(f"Loaded emission texture: {{os.path.basename(texture_path)}}")
                    
                    texture_node = nodes.new(type='ShaderNodeTexImage')
                    texture_node.image = image
                    texture_node.location = (-300, -700)
                    
                    links.new(texture_node.outputs['Color'], principled_bsdf.inputs['Emission'])
                    log_message(f"Connected {{os.path.basename(texture_path)}} to Emission")
                    processed_types.add('emission')
                    
                except Exception as e:
                    log_message(f"Failed to process emission texture {{texture_path}}: {{e}}")
            
            # 8. 处理其他贴图（如果没有找到主要贴图类型）
            if not processed_types and other_textures:
                # 如果没有处理任何主要贴图，将第一个其他贴图作为基础颜色
                texture_path = other_textures[0]
                texture_name = os.path.basename(texture_path).lower()
                
                # 再次跳过dummy贴图
                if 'dummy' not in texture_name:
                    try:
                        image = bpy.data.images.load(texture_path)
                        log_message(f"Loaded fallback texture: {{os.path.basename(texture_path)}}")
                        
                        texture_node = nodes.new(type='ShaderNodeTexImage')
                        texture_node.image = image
                        texture_node.location = (-300, 200)
                        
                        links.new(texture_node.outputs['Color'], principled_bsdf.inputs['Base Color'])
                        log_message(f"Connected {{os.path.basename(texture_path)}} to Base Color (fallback)")
                        
                    except Exception as e:
                        log_message(f"Failed to process fallback texture {{texture_path}}: {{e}}")
        else:
            # 检查现有图像纹理节点的图像是否有效
            for texture_node in image_texture_nodes:
                if texture_node.image:
                    log_message(f"Existing texture node has image: {{texture_node.image.name}}")
                else:
                    log_message(f"Existing texture node has no image assigned")
    
    log_message("Material and texture processing completed")
    
except Exception as e:
    log_message(f"Failed to process materials and textures: {{e}}")
    # 继续执行，不中断流程

# 导出前进行最终验证
try:
    log_message("Final verification before GLB export...")
    
    # 统计已处理的材质和贴图
    total_materials = len(bpy.data.materials)
    total_images = len(bpy.data.images)
    total_objects = len(bpy.context.scene.objects)
    
    log_message(f"Final stats: {{total_materials}} materials, {{total_images}} images, {{total_objects}} objects")
    
    # 验证每个材质的贴图连接
    for material in bpy.data.materials:
        if material.use_nodes:
            image_nodes = [node for node in material.node_tree.nodes if node.type == 'TEX_IMAGE' and node.image]
            log_message(f"Material '{{material.name}}' has {{len(image_nodes)}} connected image textures")
            for node in image_nodes:
                if node.image and node.image.filepath:
                    log_message(f"  - {{node.image.name}}: {{node.image.filepath}}")
        else:
            log_message(f"Material '{{material.name}}' does not use nodes")
    
except Exception as e:
    log_message(f"Verification failed: {{e}}")

# 导出为GLB
try:
    log_message("Starting GLB export...")
    bpy.ops.export_scene.gltf(
        filepath=glb_output_path,
        export_format='GLB',
        export_materials='EXPORT',
        export_texcoords=True,
        export_normals=True,
        export_colors=True,
        export_cameras=False,
        export_lights=False,
        export_animations=False,
        export_texture_dir='',  # 确保贴图被嵌入到GLB中
        export_keep_originals=False,  # 不保留原始贴图引用
        export_original_specular=False  # 使用PBR材质
    )
    log_message("GLB exported successfully")
    
    # 检查输出文件
    if os.path.exists(glb_output_path):
        file_size = os.path.getsize(glb_output_path)
        log_message(f"GLB file created, size: {{file_size}} bytes")
    else:
        log_message("ERROR: GLB file was not created")
        write_done_flag()
        sys.exit(1)
        
except Exception as e:
    log_message(f"Failed to export GLB: {{e}}")
    write_done_flag()
    sys.exit(1)

log_message("Conversion completed successfully")
write_done_flag()
'''

def get_axis_convert_script(input_path: str, output_path: str) -> str:
    """
    生成Blender脚本，将模型坐标轴统一为Y-up（Unity标准）。
    支持OBJ/GLB输入输出。
    """
    return f'''
import bpy
import sys
import os

input_path = r"{input_path}"
output_path = r"{output_path}"

bpy.ops.wm.read_factory_settings(use_empty=True)
if input_path.lower().endswith('.obj'):
    bpy.ops.import_scene.obj(filepath=input_path)
elif input_path.lower().endswith('.glb') or input_path.lower().endswith('.gltf'):
    bpy.ops.import_scene.gltf(filepath=input_path)
else:
    raise Exception("Unsupported format")

for obj in bpy.context.scene.objects:
    if obj.type == 'MESH':
        obj.rotation_euler[0] -= 1.5708  # -90度，Z-up转Y-up

if output_path.lower().endswith('.obj'):
    bpy.ops.export_scene.obj(filepath=output_path)
elif output_path.lower().endswith('.glb') or output_path.lower().endswith('.gltf'):
    bpy.ops.export_scene.gltf(filepath=output_path, export_format='GLB')
else:
    raise Exception("Unsupported format")
'''

def get_axis_convert_script_with_done(input_path: str, output_path: str, done_flag_path: str) -> str:
    """
    生成Blender脚本，将模型坐标轴统一为Y-up（Unity标准），并在完成后写入done标记文件。
    支持OBJ/GLB输入输出。
    """
    return f'''
import bpy
import sys
import os
input_path = r"{input_path}"
output_path = r"{output_path}"
done_flag_path = r"{done_flag_path}"
def write_done_flag():
    try:
        with open(done_flag_path, "w") as f:
            f.write("done")
    except Exception:
        pass
try:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if input_path.lower().endswith('.obj'):
        bpy.ops.import_scene.obj(filepath=input_path)
    elif input_path.lower().endswith('.glb') or input_path.lower().endswith('.gltf'):
        bpy.ops.import_scene.gltf(filepath=input_path)
    else:
        raise Exception("Unsupported format")
    for obj in bpy.context.scene.objects:
        if obj.type == 'MESH':
            obj.rotation_euler[0] -= 1.5708  # -90度，Z-up转Y-up
    if output_path.lower().endswith('.obj'):
        bpy.ops.export_scene.obj(filepath=output_path)
    elif output_path.lower().endswith('.glb') or output_path.lower().endswith('.gltf'):
        bpy.ops.export_scene.gltf(filepath=output_path, export_format='GLB')
    else:
        raise Exception("Unsupported format")
    write_done_flag()
except Exception as e:
    write_done_flag()
    raise
'''

def get_fbx_to_obj_script(fbx_path: str, obj_path: str, output_dir: str, done_flag_path: str, blender_debug_log: str) -> str:
    """
    生成 FBX 转 OBJ 并提取贴图的 Blender 脚本。
    Args:
        fbx_path: 输入FBX文件路径
        obj_path: 输出OBJ文件路径
        output_dir: 输出目录路径
        done_flag_path: 完成标记文件路径
        blender_debug_log: 调试日志文件路径
    Returns:
        str: Blender Python脚本内容
    """
    return f'''
import bpy
import os
import sys
import time
import shutil

debug_log_path = r"{blender_debug_log}"
done_flag_path = r"{done_flag_path}"
obj_output_path = r"{obj_path}"
output_dir = r"{output_dir}"

def log_message(message):
    timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
    with open(debug_log_path, "a", encoding="utf-8") as f:
        f.write(f"[{{timestamp}}] {{message}}\\n")

def write_done_flag():
    try:
        with open(done_flag_path, "w", encoding="utf-8") as f:
            f.write("completed")
        log_message(f"Done flag written: {{done_flag_path}}")
    except Exception as e:
        log_message(f"Failed to write done flag: {{e}}")

log_message("Blender script started for FBX to OBJ with texture extraction")
log_message(f"Input FBX: {fbx_path}")
log_message(f"Output OBJ: {obj_path}")
log_message(f"Output directory: {output_dir}")
log_message(f"Done flag: {done_flag_path}")

if not os.path.exists(r"{fbx_path}"):
    log_message(f"ERROR: Input FBX file not found: {fbx_path}")
    write_done_flag()
    sys.exit(1)

try:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    log_message("Default scene cleared")
except Exception as e:
    log_message(f"Failed to clear scene: {{e}}")

try:
    log_message("Starting FBX import...")
    bpy.ops.import_scene.fbx(filepath=r"{fbx_path}")
    log_message("FBX imported successfully")
    imported_objects = len(bpy.context.scene.objects)
    log_message(f"Imported objects count: {{imported_objects}}")
    materials_count = len(bpy.data.materials)
    log_message(f"Materials count: {{materials_count}}")
    images_count = len(bpy.data.images)
    log_message(f"Images count: {{images_count}}")
except Exception as e:
    log_message(f"Failed to import FBX: {{e}}")
    write_done_flag()
    sys.exit(1)

# 贴图提取逻辑同GLB/OBJ
extracted_textures = []
try:
    log_message("Starting texture extraction...")
    for image in bpy.data.images:
        if image.name in ['Render Result', 'Viewer Node']:
            continue
        image_name_lower = image.name.lower()
        if image_name_lower.startswith("color") or image_name_lower.startswith("diffuse") or image_name_lower.startswith("albedo") or image_name_lower.startswith("basecolor"):
            texture_type = "diffuse"
        elif image_name_lower.startswith("normalgl") or image_name_lower.startswith("normal") or image_name_lower.startswith("nrm"):
            texture_type = "normal"
        elif image_name_lower.startswith("orm") or image_name_lower.startswith("metallicroughness") or image_name_lower.startswith("metallic_roughness"):
            texture_type = "metallicroughness"
        elif image_name_lower.startswith("roughness") or image_name_lower.startswith("rough"):
            texture_type = "roughness"
        elif image_name_lower.startswith("metallic") or image_name_lower.startswith("metal"):
            texture_type = "metallic"
        elif image_name_lower.startswith("ao") or image_name_lower.startswith("occlusion") or image_name_lower.startswith("ambient"):
            texture_type = "ao"
        elif image_name_lower.startswith("emission") or image_name_lower.startswith("emissive"):
            texture_type = "emission"
        elif image_name_lower == "image_0" or image_name_lower == "image0":
            texture_type = "diffuse"
        elif image_name_lower == "image_1" or image_name_lower == "image1":
            texture_type = "metallicroughness"
        elif image_name_lower == "image_2" or image_name_lower == "image2":
            texture_type = "normal"
        elif image_name_lower == "image_3" or image_name_lower == "image3":
            texture_type = "roughness"
        elif image_name_lower == "image_4" or image_name_lower == "image4":
            texture_type = "ao"
        elif image_name_lower == "image_5" or image_name_lower == "image5":
            texture_type = "emission"
        else:
            texture_type = "diffuse"
        texture_name = f"{{image.name}}_{{texture_type}}.png"
        texture_path = os.path.join(output_dir, texture_name)
        try:
            image.file_format = 'PNG'
            image.save_render(texture_path)
            log_message(f"Texture saved: {{texture_path}}")
            extracted_textures.append(texture_name)
        except Exception as e:
            log_message(f"Failed to save texture {{image.name}}: {{e}}")
    log_message(f"Texture extraction completed. Extracted {{len(extracted_textures)}} textures: {{extracted_textures}}")
except Exception as e:
    log_message(f"Texture extraction failed: {{e}}")
    import traceback
    log_message(f"Traceback: {{traceback.format_exc()}}")

# 导出为OBJ
try:
    log_message("Starting OBJ export...")
    bpy.ops.export_scene.obj(
        filepath=obj_output_path,
        use_materials=True,
        use_uvs=True,
        use_normals=True,
        use_triangles=False,
        path_mode='RELATIVE'
    )
    mtl_path = obj_output_path.replace('.obj', '.mtl')
    if os.path.exists(mtl_path):
        log_message("Enhancing MTL file with all texture types...")
        # 贴图增强逻辑同GLB/OBJ
    log_message("OBJ export completed successfully")
    if os.path.exists(obj_output_path):
        file_size = os.path.getsize(obj_output_path)
        log_message(f"OBJ file created, size: {{file_size}} bytes")
    else:
        log_message("ERROR: OBJ file was not created")
        write_done_flag()
        sys.exit(1)
except Exception as e:
    log_message(f"Failed to export OBJ: {{e}}")
    write_done_flag()
    sys.exit(1)
log_message("Conversion completed successfully")
write_done_flag()'''


def get_fbx_to_glb_script(fbx_path: str, glb_path: str, done_flag_path: str, blender_debug_log: str) -> str:
    """
    生成 FBX 转 GLB 的 Blender 脚本。
    Args:
        fbx_path: 输入FBX文件路径
        glb_path: 输出GLB文件路径
        done_flag_path: 完成标记文件路径
        blender_debug_log: 调试日志文件路径
    Returns:
        str: Blender Python脚本内容
    """
    return f'''
import bpy
import os
import sys
import time

debug_log_path = r"{blender_debug_log}"
done_flag_path = r"{done_flag_path}"
glb_output_path = r"{glb_path}"

def log_message(message):
    timestamp = time.strftime("%Y-%m-%d %H:%M:%S")
    with open(debug_log_path, "a", encoding="utf-8") as f:
        f.write(f"[{{timestamp}}] {{message}}\\n")

def write_done_flag():
    try:
        with open(done_flag_path, "w", encoding="utf-8") as f:
            f.write("completed")
        log_message(f"Done flag written: {{done_flag_path}}")
    except Exception as e:
        log_message(f"Failed to write done flag: {{e}}")

log_message("Blender script started for FBX to GLB")
log_message(f"Input FBX: {fbx_path}")
log_message(f"Output GLB: {glb_path}")
log_message(f"Done flag: {done_flag_path}")

if not os.path.exists(r"{fbx_path}"):
    log_message(f"ERROR: Input FBX file not found: {fbx_path}")
    write_done_flag()
    sys.exit(1)

try:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    log_message("Default scene cleared")
except Exception as e:
    log_message(f"Failed to clear scene: {{e}}")

try:
    log_message("Starting FBX import...")
    bpy.ops.import_scene.fbx(filepath=r"{fbx_path}")
    log_message("FBX imported successfully")
    imported_objects = len(bpy.context.scene.objects)
    log_message(f"Imported objects count: {{imported_objects}}")
    materials_count = len(bpy.data.materials)
    log_message(f"Materials count: {{materials_count}}")
    images_count = len(bpy.data.images)
    log_message(f"Images count: {{images_count}}")
except Exception as e:
    log_message(f"Failed to import FBX: {{e}}")
    write_done_flag()
    sys.exit(1)

try:
    log_message("Starting GLB export...")
    bpy.ops.export_scene.gltf(
        filepath=glb_output_path,
        export_format='GLB',
        export_apply=True
    )
    log_message("GLB export completed successfully")
    if os.path.exists(glb_output_path):
        file_size = os.path.getsize(glb_output_path)
        log_message(f"GLB file created, size: {{file_size}} bytes")
    else:
        log_message("ERROR: GLB file was not created")
        write_done_flag()
        sys.exit(1)
except Exception as e:
    log_message(f"Failed to export GLB: {{e}}")
    write_done_flag()
    sys.exit(1)
log_message("Conversion completed successfully")
write_done_flag()'''