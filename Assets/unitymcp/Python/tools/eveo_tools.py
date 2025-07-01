"""Tools for inspecting and manipulating Unity objects."""

from typing import Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_eveo_tools(mcp: FastMCP):
    """Register all object inspection and manipulation tools with the MCP server."""

    @mcp.tool()
    def add_event_object():
        """
        批量实例化 Assets/Resources/Course/Perfabs 下所有 prefab，
        并为每个物体添加 ObjectRegister 脚本，统一设置为 GameObjectRoot 的子物体。


        """
        try:
            unity = get_unity_connection()
            response = unity.send_command("ADD_EVENT_OBJECT", {})
            if not response.get("success", False):
                return f"Error: {response.get('error', 'Unknown error')}"
            created = response.get("created", [])
            return f"成功批量实例化 {len(created)} 个物体: {created}"
        except Exception as e:
            return f"Error calling AddEventObject: {str(e)}"
