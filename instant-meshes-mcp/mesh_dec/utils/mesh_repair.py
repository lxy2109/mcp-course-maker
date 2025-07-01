"""
mesh_repair.py
网格修复、pymeshlab修补、边界处理相关工具函数。
"""
# ... 迁移自 mesh_processing.py ...
# repair_mesh_with_pymeshlab

import pymeshlab

def repair_mesh_with_pymeshlab(obj_path: str) -> str:
    """
    使用pymeshlab修复网格破洞和其他问题。
    Args:
        obj_path (str): 输入OBJ文件路径
    Returns:
        str: 修复后的OBJ文件路径
    """
    ms = pymeshlab.MeshSet()
    ms.load_new_mesh(obj_path)
    
    # 移除重复顶点
    ms.meshing_remove_duplicate_vertices()
    
    # 移除重复面
    ms.meshing_remove_duplicate_faces()
    
    # 移除零面积面
    ms.meshing_remove_null_faces()
    
    # 移除非流形边
    ms.meshing_remove_non_manifold_edges()
    
    # 修复小破洞（自适应大小）
    try:
        ms.meshing_close_holes(maxholesize=30)
    except Exception:
        # 如果自动修复失败，尝试更小的破洞
        try:
            ms.meshing_close_holes(maxholesize=10)
        except Exception:
            pass  # 如果仍然失败，继续处理
    
    # 平滑网格，减少噪声
    try:
        ms.apply_coord_laplacian_smoothing(stepsmoothnum=3, cotangentweight=False)
    except Exception:
        pass
    
    # 保存修复后的网格
    repaired_path = obj_path.replace('.obj', '_repaired.obj')
    ms.save_current_mesh(repaired_path)
    return repaired_path