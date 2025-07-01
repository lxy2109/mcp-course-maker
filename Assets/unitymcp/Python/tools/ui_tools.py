from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection
from typing import Optional

def register_ui_tools(mcp: FastMCP):
    """Register all UI management tools with the MCP server."""

    # @mcp.tool()
    def create_ui_element(
        ctx: Context,
        type: str,
        name: str = None,
        parent_name: str = None,
        width: float = 160,
        height: float = 40
    ) -> str:
        """Create a UGUI element (Canvas, Panel, Button) in Unity."""
        try:
            unity = get_unity_connection()
            params = {
                "type": type,
                "name": name or type,
                "parent_name": parent_name or "",
                "width": width,
                "height": height
            }
            response = unity.send_command("CREATE_UI_ELEMENT", params)
            if not response.get("success", False):
                return f"Error creating UGUI element: {response.get('error', 'Unknown error')}"
            return f"UGUI element '{response.get('name', type)}' created successfully."
        except Exception as e:
            return f"Error creating UGUI element: {str(e)}"

    # @mcp.tool()
    def set_ui_color(
        ctx: Context,
        object_name: str,
        r: float,
        g: float,
        b: float,
        a: float = 1.0
    ) -> str:
        """Set the color of a UGUI Image component."""
        try:
            unity = get_unity_connection()
            params = {
                "object_name": object_name,
                "r": r,
                "g": g,
                "b": b,
                "a": a
            }
            response = unity.send_command("SET_UI_COLOR", params)
            if not response.get("success", False):
                return f"Error setting color: {response.get('error', 'Unknown error')}"
            return "Color set successfully."
        except Exception as e:
            return f"Error setting color: {str(e)}"

    # @mcp.tool()
    def set_canvas_properties(
        ctx: Context,
        canvas_name: str,
        render_mode: str = None,
        width: float = 800,
        height: float = 600
    ) -> str:
        """针对场景中你指定的Canvas进行大小以及rendermode的调整"""
        try:
            unity = get_unity_connection()
            params = {
                "canvas_name": canvas_name,
                "render_mode": render_mode,
                "width": width,
                "height": height
            }
            response = unity.send_command("SET_CANVAS_PROPERTIES", params)
            if not response.get("success", False):
                return f"Error setting canvas properties: {response.get('error', 'Unknown error')}"
            return "Canvas properties set successfully."
        except Exception as e:
            return f"Error setting canvas properties: {str(e)}"
