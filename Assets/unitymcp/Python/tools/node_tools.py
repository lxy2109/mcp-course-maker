from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection
from typing import Optional

def register_node_tools(mcp: FastMCP):
    """Register all Node Graph management tools with the MCP server."""

    # @mcp.tool()
    def create_node_graph(
        ctx: Context,
        file_name: str = None
    ) -> str:
        """在unity的Assets/Resources/Course/NodeGraph目录下创建一个NodeGraph文件，并打开他"""
        try:
            unity = get_unity_connection()
            params = {
                "fileName": file_name or "NewNodeGraph"
            }
            response = unity.send_command("CREATE_NODE_GRAPH", params)
            if not response.get("success", False):
                return f"Error creating Node Graph: {response.get('error', 'Unknown error')}"
            return f"Node Graph created successfully at path: {response.get('path', '')}"
        except Exception as e:
            return f"Error creating Node Graph: {str(e)}"

    # @mcp.tool()
    def create_start_node(
        ctx: Context
    ) -> str:
        """Create a Start Node in the currently open Node Graph and auto-connect its output to the first FlowEventNode.
        """
        try:
            unity = get_unity_connection()
            response = unity.send_command("CREATE_START_NODE", {})
            if not response.get("success", False):
                return f"Error creating Start Node: {response.get('error', 'Unknown error')}"

            # 获取所有节点信息
            nodes_info = unity.send_command("GET_ALL_NODES_INFO", {})
            if not nodes_info.get("success", False):
                return f"Start Node created, but failed to get nodes info: {nodes_info.get('error', 'Unknown error')}"

            nodes = nodes_info.get("nodes", [])
            # 找到唯一的StartNode
            start_node = next((n for n in nodes if n.get("type") == "StartNode"), None)
            # 找到第一个有eventName的FlowEventNode
            exec_node = next((n for n in nodes if n.get("type") == "FlowEventNode" and n.get("eventName")), None)
            if start_node and exec_node:
                connect_params = {
                    "outputNodeEventname": "StartNode",  # 用nodename查找
                    "outputPortName": "Output",
                    "inputNodeEventname": exec_node.get("eventName"),
                    "inputPortName": "Input"
                }
                connect_response = unity.send_command("CONNECT_NODES", connect_params)
                if not connect_response.get("success", False):
                    return f"Start Node created, but failed to auto connect: {connect_response.get('error', 'Unknown error')}"
                return f"Start Node created and auto connected successfully"
            return f"Start Node created, but no FlowEventNode found to connect"
        except Exception as e:
            return f"Error creating Start Node: {str(e)}"

    # @mcp.tool()
    def create_combine_node(
        ctx: Context,
        description: str = None,
        auto_connect: bool = True
    ) -> str:
        """可以根据自行的信息判断，判断场上的节点中是否有需求combinenode节点，如果有就自行根据description格式创建
        一个相符合的combinenode，在创建完成之后，如果auto_connect为true，
        那么他就会根据传入的description自行连接其余的floweventnode
        参数：
            ctx: MCP 上下文
            description: 描述
            auto_connect: 是否自动连接
        返回值：
            str: 成功消息或错误详情
        
        
        
        """
        try:
            unity = get_unity_connection()
            params = {}
            if description:
                params["Description"] = description
            response = unity.send_command("CREATE_COMBINE_NODE", params)
            if not response.get("success", False):
                return f"Error creating Combine Node: {response.get('error', 'Unknown error')}"
            
            if auto_connect:
                connect_response = unity.send_command("AUTO_CONNECT_COMBINE_NODE", {})
                if not connect_response.get("success", False):
                    return f"Combine Node created but failed to auto connect: {connect_response.get('error', 'Unknown error')}"
                return f"Combine Node created and auto connected successfully"
            
            return f"Combine Node created successfully"
        except Exception as e:
            return f"Error creating Combine Node: {str(e)}"

    # @mcp.tool()
    def create_flow_event_node(
        ctx: Context,
        eventname: str = None,
        heigh_light: Optional[list] = None,
        time_line: Optional[list] = None,
        partcontext: str = None,
        clip: str = None,
        startevent: str = None,
        endevent: str = None,
        action: int = 0
    ) -> str:
        """在一个打开的nodegraph中，如果没有通过再整张nodegraph中搜索到你想要的结点之后，
        创建一个floweventnode，并为其赋值，比如说他的eventname
        ，他包含的文本内容，它包含的需要添加的timeline
        参数：
            ctx: MCP 上下文
            eventname: 事件名称
            heigh_light: 高亮物体列表
            time_line: 时间线列表
            partcontext: 部分上下文
            clip: 音频
            startevent: 开始事件
            endevent: 结束事件
            action: 动作
        返回值：
            str: 成功消息或错误详情
        """

        try:
            unity = get_unity_connection()
            params = {
                "eventname": eventname,
                "heighLight": heigh_light or [],
                "timeLine": time_line or [],
                "partcontext": partcontext,
                "clip": clip,
                "startevent": startevent,
                "endevent": endevent,
                "action": action
            }
            response = unity.send_command("CREATE_FLOW_EVENT_NODE", params)
            if not response.get("success", False):
                return f"Error creating Flow Event Node: {response.get('error', 'Unknown error')}"
            return f"Flow Event Node created successfully"
        except Exception as e:
            return f"Error creating Flow Event Node: {str(e)}"

    # @mcp.tool()
    def connect_nodes(
        ctx: Context,
        output_node_eventname: str,
        output_port_name: str,
        input_node_eventname: str,
        input_port_name: str
    ) -> str:
        """能够通过floweventnode的eventname来自行连接两个floweventnode
        参数：
            ctx: MCP 上下文
            output_node_eventname: 输出节点事件名称
            output_port_name: 输出端口名称
            input_node_eventname: 输入节点事件名称
            input_port_name: 输入端口名称
        """
        try:

            unity = get_unity_connection()
            params = {
                "outputNodeEventname": output_node_eventname,
                "outputPortName": output_port_name,
                "inputNodeEventname": input_node_eventname,
                "inputPortName": input_port_name
            }
            response = unity.send_command("CONNECT_NODES", params)
            if not response.get("success", False):
                return f"Error connecting nodes: {response.get('error', 'Unknown error')}"
            return f"Nodes connected successfully"
        except Exception as e:
            return f"Error connecting nodes: {str(e)}"

    # @mcp.tool()
    def get_all_nodes_info(ctx: Context) -> dict:
        """在当前打开的nodegraph中获取所有节点的信息，包括他的阶段文案"""
        try:
            unity = get_unity_connection()
            response = unity.send_command("GET_ALL_NODES_INFO", {})
            if not response.get("success", False):
                return {"error": response.get("error", "Unknown error")}
            # 确保每个节点都包含description字段
            nodes = response.get("nodes", [])
            for node in nodes:
                if "description" not in node:
                    node["description"] = None
            return nodes
        except Exception as e:
            return {"error": str(e)}
        
    # @mcp.tool()
    def auto_connect_combine_node(ctx: Context) -> str:
        """Auto connect CombineNode and FlowEventNode based on CombineNode's description."""
        try:
            unity = get_unity_connection()
            response = unity.send_command("AUTO_CONNECT_COMBINE_NODE", {})
            if not response.get("success", False):
                return f"Error auto connecting CombineNode: {response.get('error', 'Unknown error')}"
            return "Auto connected CombineNode successfully"
        except Exception as e:
            return f"Error auto connecting CombineNode: {str(e)}"

    # @mcp.tool()
    def add_highlight_objects_to_flow_event_node(
        ctx: Context,
        event_name: str,
        object_names: list
    ) -> str:
        """在一张打开的nodegraph里面，通过eventname搜索向指定的 FlowEventNode 添加高亮物体。
        
        参数：
            ctx: MCP 上下文
            event_name: 目标 FlowEventNode 的事件名称
            object_names: 要添加的高亮物体名称列表
        
        返回值：
            str: 成功消息或错误详情
        """
        try:
            unity = get_unity_connection()
            params = {
                "eventName": event_name,
                "objectNames": object_names
            }
            response = unity.send_command("ADD_HIGHLIGHT_OBJECTS_TO_FLOW_EVENT_NODE", params)
            if not response.get("success", False):
                return f"Error adding highlight objects: {response.get('error', 'Unknown error')}"
            return response.get("message", "Successfully added highlight objects")
        except Exception as e:
            return f"Error adding highlight objects: {str(e)}"

    # @mcp.tool()
    def add_audio_to_flow_event_node(
        ctx: Context,
        event_name: str,
        audio_name: str
    ) -> str:
        """在一张打开的nodegraph里面，通过eventname搜索向指定的 FlowEventNode 添加音频，音频可以模糊搜索。
        
        参数：
            ctx: MCP 上下文
            event_name: 目标 FlowEventNode 的事件名称
            audio_name: 音频文件名（支持部分名称模糊匹配，在 Assets/Resources/Course/Audio 目录下）
        
        返回值：
            str: 成功消息或错误详情
        """
        try:
            unity = get_unity_connection()
            params = {
                "eventName": event_name,
                "audioName": audio_name
            }
            response = unity.send_command("ADD_AUDIO_TO_FLOW_EVENT_NODE", params)
            if not response.get("success", False):
                return f"Error adding audio: {response.get('error', 'Unknown error')}"
            return response.get("message", "Successfully added audio")
        except Exception as e:
            return f"Error adding audio: {str(e)}"

    # @mcp.tool()
    def add_timelines_to_flow_event_node(
        ctx: Context,
        event_name: str,
        timeline_names: list
    ) -> str:
        """在一张打开的nodegraph里面，通过eventname搜索向指定的 FlowEventNode 添加timeline（支持批量、模糊匹配）。
        
        参数：
            ctx: MCP 上下文
            event_name: 目标 FlowEventNode 的事件名称
            timeline_names: 要添加的timeline名称列表（支持部分名称模糊匹配，在 Assets/Resources/Course/Timeline 目录下）
        
        返回值：
            str: 成功消息或错误详情
        """
        try:
            unity = get_unity_connection()
            params = {
                "eventName": event_name,
                "timelineNames": timeline_names
            }
            response = unity.send_command("ADD_TIMELINES_TO_FLOW_EVENT_NODE", params)
            if not response.get("success", False):
                return f"Error adding timelines: {response.get('error', 'Unknown error')}"
            return response.get("message", "Successfully added timelines")
        except Exception as e:
            return f"Error adding timelines: {str(e)}"

    # @mcp.tool()
    def set_flow_event_node_end_action(
        ctx: Context,
        event_name: str,
        end_action
    ) -> str:
        """
        自动给指定的 FlowEventNode 修改其 endAction。
        参数：
            ctx: MCP 上下文
            event_name: 目标 FlowEventNode 的事件名称
            end_action: 目标 endAction（可为 int 或字符串）
        返回值：
            str: 成功消息或错误详情
        """
        try:
            unity = get_unity_connection()
            params = {
                "eventName": event_name,
                "endAction": end_action
            }
            response = unity.send_command("SET_FLOW_EVENT_NODE_END_ACTION", params)
            if not response.get("success", False):
                return f"Error setting endAction: {response.get('error', 'Unknown error')}"
            return response.get("message", "Successfully set endAction")
        except Exception as e:
            return f"Error setting endAction: {str(e)}"
