"""Tools for inspecting and manipulating Unity objects."""

from typing import Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_event_tools(mcp: FastMCP):
    """Register all object inspection and manipulation tools with the MCP server."""

    @mcp.tool()
    def add_graph_pool(
        ctx: Context,
        node_graph_name: str
    ) -> Dict[str, Any]:
        """
        在EventManager的graphs字典中，替换默认的NodeGraph，替换完成游戏开始的时候会运行你调入的相关nodegraph的事件
        查找路径为Assets/Resources/Course/NodeGraph/{node_graph_name}.asset

        参数:
            node_graph_name: 要添加的NodeGraph的名称

        返回:
            success: 是否成功添加
            message: 成功或失败的消息
            unity_result: Unity返回的结果
        """
        if not node_graph_name:
            return {"success": False, "message": "nodeGraph name is required!"}
        try:
            unity = get_unity_connection()
            # 查找NodeGraph资源
            assets = unity.send_command("GET_ASSET_LIST", {
                "type": "NodeGraph",
                "search_pattern": node_graph_name,
                "folder": "Assets/Resources/Course/NodeGraph"
            }).get("assets", [])
            asset = next((a for a in assets if a.get("name") == node_graph_name), None)
            if not asset:
                return {"success": False, "message": f"Cannot find NodeGraph with name: {node_graph_name} in Assets/Resources/Course/NodeGraph"}
            # 通知Unity添加到EventManager
            result = unity.send_command("ADD_GRAPH_POOL", {
                "nodeGraphName": node_graph_name
            })
            return {"success": True, "message": f"Successfully added NodeGraph: {node_graph_name}", "unity_result": result}
        except Exception as ex:
            return {"success": False, "message": str(ex)}

    # @mcp.tool()
    def add_event(
        ctx: Context,
    ) -> Dict[str, Any]:
        """
        在EventInvokerManager的字典中添加UnityEventListeners里的UnityEvent。
        批量注册所有 UnityEvent。
        注意：事件将以其中文注释名称（Tooltip）作为字典的key。
    
        
        返回:
            success: 是否成功添加
            message: 成功或失败的消息
            unity_result: Unity返回的结果
        """
        try:
            unity = get_unity_connection()
            # 批量注册所有事件
            result = unity.send_command("ADD_EVENT",{})
            if not result.get("success", False):
                return {"success": False, "message": result.get("message", "Unknown error"), "unity_result": result}
            return {"success": True, "message": "成功批量注册所有 UnityEvent", "unity_result": result}
        except Exception as ex:
            return {"success": False, "message": str(ex)}

    @mcp.tool()
    def create_unity_event(
        ctx: Context
    ) -> Dict[str, Any]:
        """
        根据 AllUnityEvent 中的 enterEvent 和 exitEvent 字典自动生成unityevent和其对应的事件监听器脚本。
        生成的脚本将包含所有事件的监听器，并自动处理事件的注册和清理。
        
        返回:
            success: 是否成功生成
            message: 成功或失败的消息
            unity_result: Unity返回的结果
        """
        try:
            unity = get_unity_connection()
            # 通知Unity生成事件监听器脚本
            result = unity.send_command("CREATE_UNITY_EVENT", {})
            if not result.get("success", False):
                return {
                    "success": False, 
                    "message": result.get("message", "Unknown error"), 
                    "unity_result": result
                }
            return {
                "success": True, 
                "message": "Successfully generated event listeners script", 
                "unity_result": result
            }
        except Exception as ex:
            return {"success": False, "message": str(ex)}

    @mcp.tool()
    def flow_event_forth(
        ctx: Context,
        coursename: str
    ) -> Dict[str, Any]:
        """
        根据 EventCommandHandler 中的 FlowEventForth 方法，执行第四步流程。
        该流程包括：
        1. AnalyzeAndSaveNodeEventLinks() - 生成流程相关数据
        2. AutoMatch(coursename) - 自动匹配音频、高亮物体、timeline
        3. CreateUnityEvent() - 自动创建相关的UnityEvent

        参数:
            coursename: 课程名称，用于查找资源路径

        返回:
            success: 是否成功执行
            message: 成功或失败的消息
            unity_result: Unity返回的结果
        """
        if not coursename:
            return {"success": False, "message": "coursename is required!"}
        try:
            unity = get_unity_connection()
            # 调用Unity中的一个高级命令来执行整个FlowEventForth流程
            result = unity.send_command("FLOW_EVENT_FORTH", {
                "coursename": coursename
            })
            if not result.get("success", False):
                return {
                    "success": False,
                    "message": result.get("message", "Unknown error occurred in FlowEventForth."),
                    "unity_result": result
                }
            return {
                "success": True,
                "message": f"Successfully executed FlowEventForth for course: {coursename}",
                "unity_result": result
            }
        except Exception as ex:
            return {"success": False, "message": str(ex)}

    @mcp.tool()
    def add_event_object(
        ctx: Context,
        coursename: str
    ) -> Dict[str, Any]:
        """
        根据课程名称，在场景中批量实例化对应的Prefab，并自动添加必要的组件。
        此函数会查找 Assets/{coursename}/Prefabs 目录下的所有Prefab进行操作。

        参数:
            coursename: 课程名称，用于查找Prefab的路径。

        返回:
            success: 是否成功执行。
            message: 成功或失败的消息。
            created: 成功创建的GameObject列表。
            error: 如果失败，返回错误信息。
        """
        if not coursename:
            return {"success": False, "message": "coursename is required!"}
        try:
            unity = get_unity_connection()
            # I will assume the command is registered as "ADD_EVENT_OBJECT"
            result = unity.send_command("ADD_EVENT_OBJECT", {
                "coursename": coursename
            })

            if not result.get("success", False):
                return {
                    "success": False,
                    "message": result.get("error", "An unknown error occurred in AddEventObject."),
                    "unity_result": result
                }

            return {
                "success": True,
                "message": f"Successfully added event objects for course: {coursename}",
                "created": result.get("created", []),
                "unity_result": result
            }
        except Exception as ex:
            return {"success": False, "message": str(ex)}