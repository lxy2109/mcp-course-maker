"""
mesh_quality.py
网格质量分析、统计相关工具函数。
"""
# ... 迁移自 mesh_processing.py ...
# check_mesh_quality

import trimesh
from typing import Any, Dict

def check_mesh_quality(obj_path: str) -> Dict[str, Any]:
    """
    检查网格质量，返回面数、边数、顶点数和拓扑信息。
    Args:
        obj_path (str): OBJ文件路径
    Returns:
        dict: 包含网格质量信息的字典
    """
    try:
        mesh = trimesh.load(obj_path, force='mesh')
        quality_info = {
            "vertices": len(mesh.vertices),
            "faces": len(mesh.faces),
            "edges": len(mesh.edges),
            "watertight": mesh.is_watertight,
            "volume": mesh.volume if mesh.is_watertight else "N/A",
            "surface_area": mesh.area,
            "bounds": mesh.bounds.tolist(),
            "bbox_diagonal": ((mesh.bounds[1] - mesh.bounds[0]) ** 2).sum() ** 0.5
        }
        
        # 检查网格质量问题
        issues = []
        warnings = []
        
        if not mesh.is_watertight:
            issues.append("Not watertight (has holes)")
        if len(mesh.faces) == 0:
            issues.append("No faces")
        if len(mesh.vertices) == 0:
            issues.append("No vertices")
            
        # 检查是否可能破碎
        try:
            components = mesh.split(only_watertight=False)
            if len(components) > 1:
                warnings.append(f"Model has {len(components)} separate components")
                quality_info["components"] = len(components)
        except Exception:
            pass
        
        # 检查面积是否异常小（可能表示破碎）
        if mesh.area < 0.001:
            warnings.append("Very small surface area - model might be damaged")
            
        # 检查边长比例
        try:
            edge_lengths = mesh.edges_unique_length
            if len(edge_lengths) > 0:
                min_edge = edge_lengths.min()
                max_edge = edge_lengths.max()
                if max_edge > 0 and min_edge / max_edge < 0.001:
                    warnings.append("Extreme edge length variation - model might have artifacts")
        except Exception:
            pass
            
        quality_info["issues"] = issues
        quality_info["warnings"] = warnings
        return quality_info
    except Exception as e:
        return {"error": str(e)}
