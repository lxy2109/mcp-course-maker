from typing import Optional, List, Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def clean_path(path: str) -> str:
    """去除前导斜杠，统一为正斜杠"""
    if not isinstance(path, str):
        return path
    path = path.lstrip("/\\")
    path = path.replace("\\", "/")
    return path

def register_nodegraph_tools(mcp):
    """注册所有NodeGraph相关工具"""
    mcp.tool()(create_empty_nodegraph)
    mcp.tool()(get_nodegraph_info)
    mcp.tool()(import_excel_to_nodegraph)
    mcp.tool()(get_flow_event_nodes)
    mcp.tool()(get_flow_event_node_names)
    mcp.tool()(get_flow_event_node_by_name)
    mcp.tool()(update_flow_event_node_timeline_assets)
    mcp.tool()(update_timeline_count_smart)
    mcp.tool()(save_nodegraph_changes)
    
    

def create_empty_nodegraph(
        ctx: Context,
        name: str,
        path: str = "Assets/NodeGraphTool/Test"
) -> str:
    """创建一个空的NodeGraph ScriptableObject文件。

    参数：
        ctx: MCP上下文
        name: 节点图文件名(不含扩展名)
        path: 资产保存路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        str: 创建结果信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("CREATE_EMPTY_NODEGRAPH", {
            "name": name,
            "path": path
        })

        # 直接检查response中的success字段
        if response.get("success") == True:
            return f"成功创建NodeGraph文件: {response.get('path', '未知路径')}"
        else:
            return f"创建NodeGraph失败: {response.get('error', '未知错误')}"
    except Exception as e:
        return f"执行操作时出错: {str(e)}"



def get_nodegraph_info(
        ctx: Context,
        name: str,
        path: str = ""
) -> str:
    """获取NodeGraph文件的详细信息。

    参数：
        ctx: MCP上下文
        node_graph_path: NodeGraph文件的完整路径，如"Assets/myGraph.asset"
        默认的path为"Assets/{课程名称}/{节点图名称}.asset"

    返回值：
        str: NodeGraph的详细信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("GET_NODEGRAPH_INFO", {
            "name": name,
            "path": path
        })

        if response.get("success") == True:
            name = response.get("name", "未知名称")
            path = response.get("path", "未知路径")

            # 提取节点计数信息
            total_node_count = response.get("totalNodeCount", 0)
            start_node_count = response.get("startNodeCount", 0)
            event_node_count = response.get("eventNodeCount", 0)
            flow_event_node_count = response.get("flowEventNodeCount", 0)

            # 格式化节点信息
            nodes_info = response.get("nodes", {})
            connections_count = len(response.get("connections", []))
            groups_count = len(response.get("groups", []))

            return (response)
        else:
            return f"获取NodeGraph信息失败: {response.get('error', '未知错误')}"
    except Exception as e:
        return f"执行操作时出错: {str(e)}"


def import_excel_to_nodegraph(
        ctx: Context,
        node_graph_path: str,
        excel_path: str,
        generate_voice: bool = True
) -> str:
    """将Excel文件数据导入到NodeGraph SO文件中。

    参数：
        ctx: MCP上下文
        node_graph_path: NodeGraph文件路径，如"Assets/NodeGraphTool/Test/myGraph.asset"
        excel_path: Excel文件路径，支持相对路径或绝对路径
        generate_voice: 是否生成语音文件，默认为True

    返回值：
        str: 导入结果信息
    """
    node_graph_path = clean_path(node_graph_path)
    excel_path = clean_path(excel_path)
    try:
        response = get_unity_connection().send_command("IMPORT_EXCEL_TO_NODEGRAPH", {
            "nodeGraphPath": node_graph_path,
            "excelPath": excel_path,
            "generateVoice": generate_voice
        })

        if response.get("success") == True:
            # 提取基本信息
            path = response.get("path", "未知路径")
            nodes_created = response.get("nodesCreated", 0)
            links_created = response.get("linksCreated", 0)
            
            # 提取节点详情
            node_details = response.get("nodeDetails", {})
            start_nodes = node_details.get("startNodes", 0)
            flow_event_nodes = node_details.get("flowEventNodes", 0)
            combine_nodes = node_details.get("combineNodes", 0)
            empty_nodes = node_details.get("emptyNodes", 0)
            
            # 提取其他详情
            data_count = response.get("dataCount", 0)
            generated_voice = "是" if response.get("generatedVoice", False) else "否"
            
            # 提取布局信息
            layout = response.get("layout", {})
            columns_count = layout.get("columnsCount", 0)
            max_row_count = layout.get("maxRowCount", 0)
            layout_width = layout.get("layoutWidth", 0)
            layout_height = layout.get("layoutHeight", 0)
            
            return f"""成功导入Excel数据到NodeGraph:
                - 路径: {path}
                - 创建节点数: {nodes_created}
                - 创建连接数: {links_created}
                - 数据条目数: {data_count}
                - 生成语音: {generated_voice}
                
                节点详情:
                - 开始节点: {start_nodes}
                - 流程事件节点: {flow_event_nodes}
                - 组合节点: {combine_nodes}
                - 空事件节点: {empty_nodes}
                
                布局信息:
                - 列数: {columns_count}
                - 最大行数: {max_row_count}
                - 布局尺寸: {layout_width}x{layout_height}
                """
        else:
            return f"导入Excel数据失败: {response.get('error', '未知错误')}"
    except Exception as e:
        return f"执行操作时出错: {str(e)}"
    
def get_flow_event_nodes(
        ctx: Context,
        name: str,
        path: str
) -> Dict[str, Any]:
    """获取NodeGraph文件中的所有FlowEventNode节点信息。

    参数：
        ctx: MCP上下文
        name: 节点图文件名(不含扩展名)
        path: 资产路径，例如"Assets/紫外可见光光度计测量实验",后面不加{文件名}.asset

    返回值：
        Dict[str, Any]: FlowEventNode节点的详细信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("GET_FLOW_EVENT_NODES", {
            "name": name,
            "path": path
        })

        if response.get("success") == True:
            return response
        else:
            return {
                "success": False, 
                "error": response.get("message", "获取FlowEventNode节点信息失败")
            }
    except Exception as e:
        return {
            "success": False, 
            "error": f"执行操作时出错: {str(e)}"
        }
    
def get_flow_event_node_names(
        ctx: Context,
        name: str,
        path: str = "Assets/NodeGraphTool/Test"
) -> Dict[str, Any]:
    """获取NodeGraph文件中的所有FlowEventNode节点名称列表。

    参数：
        ctx: MCP上下文
        name: 节点图文件名(不含扩展名)
        path: 资产路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        Dict[str, Any]: FlowEventNode节点名称的列表信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("GET_FLOW_EVENT_NODE_NAMES", {
            "name": name,
            "path": path
        })

        if response.get("success") == True:
            return response
        else:
            return {
                "success": False,
                "error": response.get("message", "获取FlowEventNode节点名称失败")
            }
    except Exception as e:
        return {
            "success": False,
            "error": f"执行操作时出错: {str(e)}"
        }
    
    
def get_flow_event_node_by_name(
        ctx: Context,
        name: str,
        event_name: str,
        path: str = "Assets/NodeGraphTool/Test"
) -> Dict[str, Any]:
    """根据事件名称获取NodeGraph中特定FlowEventNode节点的完整信息。

    参数：
        ctx: MCP上下文
        name: 节点图文件名(不含扩展名)
        event_name: 要查找的FlowEventNode节点的名称
        path: 资产路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        Dict[str, Any]: 指定FlowEventNode节点的完整信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("GET_FLOW_EVENT_NODE_BY_NAME", {
            "name": name,
            "eventName": event_name,
            "path": path
        })

        if response.get("success") == True:
            return response
        else:
            return {
                "success": False,
                "error": response.get("message", f"获取名为'{event_name}'的FlowEventNode节点失败")
            }
    except Exception as e:
        return {
            "success": False,
            "error": f"执行操作时出错: {str(e)}"
        }

def update_flow_event_node_timeline_assets(
        ctx: Context,
        name: str,
        event_name: str,
        camera_timeline_asset: Optional[str] = None,
        object_timeline_asset: Optional[str] = None,
        path: str = "Assets/NodeGraphTool/Test"
) -> Dict[str, Any]:
    """更新指定FlowEventNode节点的timeline资产引用，并自动更新timelineCount计数。

    参数：
        ctx: MCP上下文
        name: NodeGraph文件名(不含扩展名)
        event_name: 要更新的FlowEventNode节点的事件名称
        camera_timeline_asset: 相机Timeline资产的完整路径
        object_timeline_asset: 物体Timeline资产的完整路径
        path: NodeGraph文件所在路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        Dict[str, Any]: 更新结果信息，包含timelineCount更新状态
    """
    path = clean_path(path)
    camera_timeline_asset = clean_path(camera_timeline_asset) if camera_timeline_asset else camera_timeline_asset
    object_timeline_asset = clean_path(object_timeline_asset) if object_timeline_asset else object_timeline_asset
    
    try:
        # 1. 获取添加前的状态
        before_response = get_unity_connection().send_command("GET_FLOW_EVENT_NODE_BY_NAME", {
            "name": name,
            "eventName": event_name,
            "path": path
        })
        
        before_count = 0
        if before_response.get("success", False):
            before_node_data = before_response.get("nodeData", {})
            before_timeline_assets = before_node_data.get("timelineAssets", [])
            before_count = sum(1 for t in before_timeline_assets if t and t.strip())
        
        # 2. 更新timeline资产
        response = get_unity_connection().send_command("UPDATE_FLOW_EVENT_NODE_TIMELINE_ASSETS", {
            "name": name,
            "eventName": event_name,
            "cameraTimelineAsset": camera_timeline_asset,
            "objectTimelineAsset": object_timeline_asset,
            "path": path
        })

        if not response.get("success", False):
            return {
                "success": False,
                "error": response.get("message", f"更新FlowEventNode '{event_name}' 的Timeline资产失败")
            }
        
        # 3. 获取更新后的状态并计算timeline数量
        after_response = get_unity_connection().send_command("GET_FLOW_EVENT_NODE_BY_NAME", {
            "name": name,
            "eventName": event_name,
            "path": path
        })
        
        if after_response.get("success", False):
            after_node_data = after_response.get("nodeData", {})
            after_timeline_assets = after_node_data.get("timelineAssets", [])
            
            # 计算有效的timeline数量（非空的timeline资产）
            valid_timeline_count = 0
            timeline_info = []
            
            for timeline in after_timeline_assets:
                if timeline and timeline.strip():  # 检查timeline不为空
                    valid_timeline_count += 1
                    timeline_info.append(timeline.strip())
            
            # 4. 通过重新更新来同步timelineCount（包含计算的数量）
            sync_response = get_unity_connection().send_command("UPDATE_FLOW_EVENT_NODE_TIMELINE_ASSETS", {
                "name": name,
                "eventName": event_name,
                "cameraTimelineAsset": camera_timeline_asset,
                "objectTimelineAsset": object_timeline_asset,
                "timelineCount": valid_timeline_count,  # 添加计算出的timelineCount
                "path": path
            })
            
            # 5. 保存修改
            save_response = get_unity_connection().send_command("SAVE_NODEGRAPH_CHANGES", {
                "name": name,
                "path": path
            })
            
            # 6. 构建详细的返回信息
            added_timelines = []
            if camera_timeline_asset:
                added_timelines.append(f"相机Timeline: {camera_timeline_asset}")
            if object_timeline_asset:
                added_timelines.append(f"物体Timeline: {object_timeline_asset}")
            
            result = {
                "success": True,
                "message": response.get("message", "Timeline资产更新成功"),
                "timeline_count_before": before_count,
                "timeline_count_after": valid_timeline_count,
                "timeline_count_changed": valid_timeline_count != before_count,
                "timeline_assets": timeline_info,
                "added_timelines": added_timelines,
                "sync_success": sync_response.get("success", False),
                "save_success": save_response.get("success", False)
            }
            
            # 添加详细状态信息
            if valid_timeline_count != before_count:
                count_change = valid_timeline_count - before_count
                change_text = f"增加了{count_change}个" if count_change > 0 else f"减少了{abs(count_change)}个"
                result["message"] += f" | timelineCount {change_text}timeline (从{before_count}变为{valid_timeline_count})"
            else:
                result["message"] += f" | timelineCount保持为{valid_timeline_count}"
            
            if not sync_response.get("success", False):
                result["warning"] = "Timeline资产更新成功，但timelineCount同步可能失败"
            
            if not save_response.get("success", False):
                result["warning"] = result.get("warning", "") + " | NodeGraph保存可能失败"
            
            return result
        else:
            return {
                "success": True,
                "message": response.get("message", "Timeline资产更新成功"),
                "warning": "无法重新获取节点状态以更新timelineCount"
            }
            
    except Exception as e:
        return {
            "success": False,
            "error": f"执行操作时出错: {str(e)}"
        }


def update_timeline_count_smart(
        ctx: Context,
        name: str,
        event_name: str,
        path: str = "Assets/NodeGraphTool/Test"
) -> Dict[str, Any]:
    """智能更新FlowEventNode的timelineCount计数，基于实际timelineAssets数量。

    参数：
        ctx: MCP上下文
        name: NodeGraph文件名(不含扩展名)
        event_name: FlowEventNode节点的事件名称
        path: NodeGraph文件所在路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        Dict[str, Any]: 更新结果信息
    """
    path = clean_path(path)
    try:
        # 1. 获取当前节点状态
        node_response = get_unity_connection().send_command("GET_FLOW_EVENT_NODE_BY_NAME", {
            "name": name,
            "eventName": event_name,
            "path": path
        })
        
        if not node_response.get("success", False):
            return {
                "success": False,
                "error": f"无法获取FlowEventNode '{event_name}' 的状态"
            }
        
        node_data = node_response.get("nodeInfo", {})
        timeline_assets = node_data.get("timelineAssets", [])
        current_count = node_data.get("timelineCount", 0)
        
        # 2. 计算有效的timeline数量
        valid_timeline_count = sum(1 for timeline in timeline_assets if timeline and timeline.strip())
        
        if current_count == valid_timeline_count:
            return {
                "success": True,
                "message": f"timelineCount已正确 (当前值: {current_count})",
                "timeline_count": current_count,
                "timeline_assets_count": len(timeline_assets),
                "valid_timeline_count": valid_timeline_count,
                "no_update_needed": True
            }
        
        # 3. 尝试通过Unity命令更新timelineCount
        # 使用一个特殊的UPDATE命令来直接更新计数
        update_response = get_unity_connection().send_command("UPDATE_FLOW_EVENT_NODE_FIELD", {
            "name": name,
            "eventName": event_name,
            "fieldName": "timelineCount",
            "fieldValue": valid_timeline_count,
            "path": path
        })
        
        # 4. 如果专用命令不存在，尝试通过重新设置timeline资产来触发计数更新
        if not update_response.get("success", False):
            # 获取当前的timeline资产
            camera_timeline = node_data.get("cameraTimelineName", "")
            object_timeline = node_data.get("objectTimelineName", "")
            
            # 通过重新设置来触发计数更新（带计数参数）
            fallback_response = get_unity_connection().send_command("UPDATE_FLOW_EVENT_NODE_TIMELINE_ASSETS_WITH_COUNT", {
                "name": name,
                "eventName": event_name,
                "cameraTimelineAsset": camera_timeline if camera_timeline and camera_timeline.strip() else None,
                "objectTimelineAsset": object_timeline if object_timeline and object_timeline.strip() else None,
                "timelineCount": valid_timeline_count,
                "path": path
            })
            
            if fallback_response.get("success", False):
                update_response = fallback_response
        
        # 5. 保存修改
        save_response = get_unity_connection().send_command("SAVE_NODEGRAPH_CHANGES", {
            "name": name,
            "path": path
        })
        
        return {
            "success": update_response.get("success", False),
            "message": f"timelineCount从{current_count}更新为{valid_timeline_count}" if update_response.get("success", False) else "timelineCount更新失败",
            "timeline_count_before": current_count,
            "timeline_count_after": valid_timeline_count,
            "timeline_assets": [t for t in timeline_assets if t and t.strip()],
            "valid_timeline_count": valid_timeline_count,
            "update_method": "direct_field" if update_response.get("success", False) else "fallback",
            "save_success": save_response.get("success", False)
        }
        
    except Exception as e:
        return {
            "success": False,
            "error": f"执行智能timelineCount更新时出错: {str(e)}"
        }

def save_nodegraph_changes(
        ctx: Context,
        name: str,
        path: str = "Assets/NodeGraphTool/Test"
) -> Dict[str, Any]:
    """保存NodeGraph文件的所有修改。

    参数：
        ctx: MCP上下文
        name: NodeGraph文件名(不含扩展名)
        path: NodeGraph文件所在路径，默认为"Assets/NodeGraphTool/Test"

    返回值：
        Dict[str, Any]: 保存结果信息
    """
    path = clean_path(path)
    try:
        response = get_unity_connection().send_command("SAVE_NODEGRAPH_CHANGES", {
            "name": name,
            "path": path
        })

        if response.get("success") == True:
            return response
        else:
            return {
                "success": False,
                "error": response.get("message", "保存NodeGraph修改失败")
            }
    except Exception as e:
        return {
            "success": False,
            "error": f"执行操作时出错: {str(e)}"
        }