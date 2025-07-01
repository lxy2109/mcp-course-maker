from .scene_tools import register_scene_tools
from .script_tools import register_script_tools
from .material_tools import register_material_tools
from .editor_tools import register_editor_tools
from .asset_tools import register_asset_tools
from .object_tools import register_object_tools
from .generate_tools import register_generate_tools
from .animation_tools import register_animation_tools
from .nodegraph_tool import register_nodegraph_tools
from .ui_tools import register_ui_tools
from .node_tools import register_node_tools
from .event_tools import register_event_tools
from .eveo_tools import register_eveo_tools
# from .generate_model_tools import generate_3d_model
#from .ui_tools import register_ui_tools

def register_all_tools(mcp):
    """Register all tools with the MCP server."""
    register_scene_tools(mcp)
    register_script_tools(mcp)
    register_material_tools(mcp)
    register_editor_tools(mcp)
    register_asset_tools(mcp)
    register_object_tools(mcp)
    register_generate_tools(mcp)
    register_animation_tools(mcp)
    register_nodegraph_tools(mcp)
    register_ui_tools(mcp)
    register_node_tools(mcp)
    register_event_tools(mcp)
    register_eveo_tools(mcp)
    # generate_3d_model(mcp)
    #register_ui_tools(mcp)  # Add this line
