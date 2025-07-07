"""Tools for inspecting and manipulating Unity objects."""

from typing import Optional, List, Dict, Any
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_object_tools(mcp: FastMCP):
    """Register all object inspection and manipulation tools with the MCP server."""
    
    @mcp.tool()
    def get_object_properties(
        ctx: Context,
        name: str
    ) -> Dict[str, Any]:
        """Get all properties of a specified game object.

        Args:
            ctx: The MCP context
            name: Name of the game object to inspect

        Returns:
            Dict containing the object's properties, components, and their values
        """
        try:
            response = get_unity_connection().send_command("GET_OBJECT_PROPERTIES", {
                "name": name
            })
            return response
        except Exception as e:
            return {"error": f"Failed to get object properties: {str(e)}"}

    @mcp.tool()
    def get_component_properties(
        ctx: Context,
        object_name: str,
        component_type: str
    ) -> Dict[str, Any]:
        """Get properties of a specific component on a game object.

        Args:
            ctx: The MCP context
            object_name: Name of the game object
            component_type: Type of the component to inspect

        Returns:
            Dict containing the component's properties and their values
        """
        try:
            response = get_unity_connection().send_command("GET_COMPONENT_PROPERTIES", {
                "object_name": object_name,
                "component_type": component_type
            })
            return response
        except Exception as e:
            return {"error": f"Failed to get component properties: {str(e)}"}

    @mcp.tool()
    def find_objects_by_name(
        ctx: Context,
        name: str
    ) -> List[Dict[str, str]]:
        """Find game objects in the scene by name.

        Args:
            ctx: The MCP context
            name: Name to search for (partial matches are supported)

        Returns:
            List of dicts containing object names and their paths
        """
        try:
            response = get_unity_connection().send_command("FIND_OBJECTS_BY_NAME", {
                "name": name
            })
            return response.get("objects", [])
        except Exception as e:
            return [{"error": f"Failed to find objects: {str(e)}"}]

    @mcp.tool()
    def find_objects_by_tag(
        ctx: Context,
        tag: str
    ) -> List[Dict[str, str]]:
        """Find game objects in the scene by tag.

        Args:
            ctx: The MCP context
            tag: Tag to search for

        Returns:
            List of dicts containing object names and their paths
        """
        try:
            response = get_unity_connection().send_command("FIND_OBJECTS_BY_TAG", {
                "tag": tag
            })
            return response.get("objects", [])
        except Exception as e:
            return [{"error": f"Failed to find objects: {str(e)}"}]

    @mcp.tool()
    def get_scene_info(ctx: Context) -> Dict[str, Any]:
        """Get information about the current scene.

        Args:
            ctx: The MCP context

        Returns:
            Dict containing scene information including name and root objects
        """
        try:
            response = get_unity_connection().send_command("GET_SCENE_INFO")
            return response
        except Exception as e:
            return {"error": f"Failed to get scene info: {str(e)}"}

    @mcp.tool()
    def get_hierarchy(ctx: Context) -> Dict[str, Any]:
        """Get the current hierarchy of game objects in the scene.

        Args:
            ctx: The MCP context

        Returns:
            Dict containing the scene hierarchy as a tree structure
        """
        try:
            response = get_unity_connection().send_command("GET_HIERARCHY")
            return response
        except Exception as e:
            return {"error": f"Failed to get hierarchy: {str(e)}"}

    @mcp.tool()
    def select_object(
        ctx: Context,
        name: str
    ) -> Dict[str, str]:
        """Select a game object in the Unity Editor.

        Args:
            ctx: The MCP context
            name: Name of the object to select

        Returns:
            Dict containing the name of the selected object
        """
        try:
            response = get_unity_connection().send_command("SELECT_OBJECT", {
                "name": name
            })
            return response
        except Exception as e:
            return {"error": f"Failed to select object: {str(e)}"}

    @mcp.tool()
    def get_selected_object(ctx: Context) -> Optional[Dict[str, str]]:
        """Get the currently selected game object in the Unity Editor.

        Args:
            ctx: The MCP context

        Returns:
            Dict containing the selected object's name and path, or None if no object is selected
        """
        try:
            response = get_unity_connection().send_command("GET_SELECTED_OBJECT")
            return response.get("selected")
        except Exception as e:
            return {"error": f"Failed to get selected object: {str(e)}"}

    @mcp.tool()
    def get_asset_list(
        ctx: Context,
        type: Optional[str] = None,
        search_pattern: str = "*",
        folder: str = "Assets"
    ) -> List[Dict[str, str]]:
        """Get a list of assets in the project.

        Args:
            ctx: The MCP context
            type: Optional asset type to filter by
            search_pattern: Pattern to search for in asset names
            folder: Folder to search in (default: "Assets")

        Returns:
            List of dicts containing asset information
        """
        try:
            response = get_unity_connection().send_command("GET_ASSET_LIST", {
                "type": type,
                "search_pattern": search_pattern,
                "folder": folder
            })
            return response.get("assets", [])
        except Exception as e:
            return [{"error": f"Failed to get asset list: {str(e)}"}]
            
    @mcp.tool()
    def execute_context_menu_item(
        ctx: Context,
        object_name: str,
        component: str,
        context_menu_item: str
    ) -> Dict[str, Any]:
        """Execute a specific [ContextMenu] method on a component of a given game object.

        Args:
            ctx: The MCP context
            object_name: Name of the game object to call
            component: Name of the component type
            context_menu_item: Name of the context menu item to execute

        Returns:
            Dict containing the result of the operation
        """
        try:
            unity = get_unity_connection()
            
            # Check if the object exists
            found_objects = unity.send_command("FIND_OBJECTS_BY_NAME", {
                "name": object_name
            }).get("objects", [])
            
            if not found_objects:
                return {"error": f"Object with name '{object_name}' not found in the scene."}
            
            # Check if the component exists on the object
            object_props = unity.send_command("GET_OBJECT_PROPERTIES", {
                "name": object_name
            })
            
            if "error" in object_props:
                return {"error": f"Failed to get object properties: {object_props['error']}"}
                
            components = object_props.get("components", [])
            component_exists = any(comp.get("type") == component for comp in components)
            
            if not component_exists:
                return {"error": f"Component '{component}' is not attached to object '{object_name}'."}
            
            # Now execute the context menu item
            response = unity.send_command("EXECUTE_CONTEXT_MENU_ITEM", {
                "object_name": object_name,
                "component": component,
                "context_menu_item": context_menu_item
            })
            return response
        except Exception as e:
            return {"error": f"Failed to execute context menu item: {str(e)}"} 

    @mcp.tool()
    def get_all_scene_objects(ctx: Context) -> Dict[str, Any]:
        """Get all game objects in the current scene.

        Args:
            ctx: The MCP context

        Returns:
            Dict containing list of all objects with their basic information
        """
        try:
            response = get_unity_connection().send_command("GET_ALL_SCENE_OBJECTS")
            return response
        except Exception as e:
            return {"error": f"Failed to get all scene objects: {str(e)}"}

    @mcp.tool()
    def get_object_transform_info(
        ctx: Context,
        name: str
    ) -> Dict[str, Any]:
        """Get detailed Transform information for a specific object.

        Args:
            ctx: The MCP context
            name: Name of the game object

        Returns:
            Dict containing detailed transform information including local and world coordinates
        """
        try:
            response = get_unity_connection().send_command("GET_OBJECT_TRANSFORM_INFO", {
                "name": name
            })
            return response
        except Exception as e:
            return {"error": f"Failed to get object transform info: {str(e)}"}

    @mcp.tool()
    def find_camera_objects(ctx: Context) -> Dict[str, Any]:
        """Find all camera objects in the scene.

        Args:
            ctx: The MCP context

        Returns:
            Dict containing list of all camera objects with their properties
        """
        try:
            response = get_unity_connection().send_command("FIND_CAMERA_OBJECTS")
            return response
        except Exception as e:
            return {"error": f"Failed to find camera objects: {str(e)}"}

    @mcp.tool()
    def find_objects_by_name_pattern(
        ctx: Context,
        pattern: str,
        case_sensitive: bool = False,
        exact_match: bool = False,
        include_inactive: bool = True
    ) -> Dict[str, Any]:
        """Find game objects by enhanced name pattern matching.

        Args:
            ctx: The MCP context
            pattern: Search pattern (supports wildcards * and ?)
            case_sensitive: Whether to perform case-sensitive search
            exact_match: Whether to require exact name match
            include_inactive: Whether to include inactive objects

        Returns:
            Dict containing list of matching objects with enhanced search info
        """
        try:
            response = get_unity_connection().send_command("FIND_OBJECTS_BY_NAME_PATTERN", {
                "pattern": pattern,
                "case_sensitive": case_sensitive,
                "exact_match": exact_match,
                "include_inactive": include_inactive
            })
            return response
        except Exception as e:
            return {"error": f"Failed to find objects by pattern: {str(e)}"} 

    @mcp.tool()
    def get_object_bounds(
        ctx: Context,
        name: str
    ) -> Dict[str, Any]:
        """Get the bounds information of a specific object from both Renderer and Collider components.

        Args:
            ctx: The MCP context
            name: Name of the game object

        Returns:
            Dict containing bounds information from both Renderer and Collider, plus transform info
        """
        try:
            response = get_unity_connection().send_command("GET_OBJECT_BOUNDS", {
                "name": name
            })
            return response
        except Exception as e:
            return {"error": f"Failed to get object bounds: {str(e)}"}

    @mcp.tool()
    def get_combined_bounds(
        ctx: Context,
        object_names: List[str]
    ) -> Dict[str, Any]:
        """Get the combined bounds of multiple objects.

        Args:
            ctx: The MCP context
            object_names: List of object names to calculate combined bounds for

        Returns:
            Dict containing the combined bounds information and details about found/not found objects
        """
        try:
            response = get_unity_connection().send_command("GET_COMBINED_BOUNDS", {
                "object_names": object_names
            })
            return response
        except Exception as e:
            return {"error": f"Failed to get combined bounds: {str(e)}"}

    @mcp.tool()
    def position_camera_to_frame_objects(
        ctx: Context,
        object_names: List[str],
        camera_name: str = "Main Camera",
        padding: float = 1.2,
        frame_mode: str = "fit",
        view_direction: Optional[List[float]] = None
    ) -> Dict[str, Any]:
        """Position camera to perfectly frame the specified objects using FOV and bounds calculations.

        Args:
            ctx: The MCP context
            object_names: List of object names to frame in the camera view
            camera_name: Name of the camera to position (default: "Main Camera")
            padding: Padding factor around objects (1.2 = 20% padding, default: 1.2)
            frame_mode: How to frame objects - "fit" (ensure all visible), "fill" (fill viewport), "custom"
            view_direction: Optional custom view direction as [x, y, z] (default: smart diagonal view)

        Returns:
            Dict containing camera positioning results and settings
        """
        try:
            params = {
                "object_names": object_names,
                "camera_name": camera_name,
                "padding": padding,
                "frame_mode": frame_mode
            }
            
            if view_direction is not None:
                params["view_direction"] = view_direction
                
            response = get_unity_connection().send_command("POSITION_CAMERA_TO_FRAME_OBJECTS", params)
            return response
        except Exception as e:
            return {"error": f"Failed to position camera: {str(e)}"}

    @mcp.tool()
    def auto_position_camera_to_objects(
        ctx: Context,
        object_names: List[str],
        camera_name: str = "Main Camera",
        fov: float = 45.0,
        pitch_angle: float = 30.0,
        padding: float = 1.2,
        force_reset_rotation_y: bool = True,
        apply_to_camera: bool = True
    ) -> Dict[str, Any]:
        """智能相机自动定位：一键式bounds分析和相机定位解决方案
        
        自动获取指定物体的边界信息，计算最佳相机位置，可选择是否应用到相机。
        默认设置：FOV=45°，俯视30°（自动限制在30-40度范围内），rotation.y强制为0。

        Args:
            ctx: The MCP context
            object_names: 要框住的物体名称列表
            camera_name: 相机名称 (默认: "Main Camera")
            fov: 相机视野角度 (默认: 45度)
            pitch_angle: 俯视角度 (默认: 30度，自动限制在30-40度范围内)
            padding: 边距系数 (默认: 1.2，即20%边距)
            force_reset_rotation_y: 是否强制重置rotation.y为0 (默认: True)
            apply_to_camera: 是否将计算结果应用到相机 (默认: True，设为False时仅返回计算值)

        Returns:
            Dict containing:
            - success: 操作是否成功
            - applied: 是否已应用到相机
            - cameraName: 相机名称
            - targetObjectsFound/targetObjectsNotFound: 找到和未找到的对象数量
            - notFoundObjectNames: 未找到的对象名称列表
            - originalCamera: 原始相机状态 (position, rotation, fieldOfView)
            - adjustedCamera: 调整后的相机状态
            - boundsAnalysis: 详细的bounds分析信息
              - individualObjects: 每个对象的bounds信息 (renderer/collider bounds)
              - combinedBounds: 合并后的原始bounds (center, size, min, max)
              - paddedBounds: 应用边距后的bounds
              - statistics: bounds统计信息 (volume, dimensions, aspect ratios)
            - cameraCalculation: 相机计算详情
              - inputParameters: 输入参数
              - calculatedDistance/theoreticalDistance: 计算和理论距离
              - maxDimensionUsed: 使用的最大维度
              - viewFrustumInfo: 视锥信息 (FOV弧度、tangent值、视口宽度等)
        """
        try:
            params = {
                "object_names": object_names,
                "camera_name": camera_name,
                "fov": fov,
                "pitch_angle": pitch_angle,
                "padding": padding,
                "force_reset_rotation_y": force_reset_rotation_y,
                "apply_to_camera": apply_to_camera
            }
                
            response = get_unity_connection().send_command("AUTO_POSITION_CAMERA_TO_OBJECTS", params)
            return response
        except Exception as e:
            return {"error": f"Failed to auto position camera: {str(e)}"} 