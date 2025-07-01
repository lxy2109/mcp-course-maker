"""
真实MCP集成的AI Timeline处理器
==============================

这个版本可以调用真实的Unity MCP函数，实现完全自动化的Timeline生成
支持连接到实际的Unity MCP服务器并执行动画命令
"""

import asyncio
import json
import time
from typing import Dict, List, Optional, Any, Callable
from dataclasses import dataclass, asdict
from ai_timeline_classifier import AITimelineClassifier, AIProvider, AIActionResult

@dataclass
class MCPCommand:
    """MCP命令结构"""
    function_name: str
    parameters: Dict[str, Any]
    confidence: float
    node_name: str
    reasoning: str

@dataclass
class ProcessingResult:
    """处理结果"""
    success: bool
    processed_nodes: int
    generated_commands: List[MCPCommand]
    errors: List[str]
    processing_time: float

class RealMCPAIProcessor:
    """真实MCP集成的AI Timeline处理器"""
    
    def __init__(self, mcp_client=None, ai_provider: AIProvider = AIProvider.MOCK, api_key: str = None):
        """
        初始化真实MCP AI处理器
        
        Args:
            mcp_client: MCP客户端实例（在Cursor环境中自动提供）
            ai_provider: AI提供商
            api_key: API密钥
        """
        self.mcp_client = mcp_client
        self.ai_classifier = AITimelineClassifier(provider=ai_provider, api_key=api_key)
        self.min_confidence_threshold = 0.6
        
        # MCP函数映射（真实的MCP函数名）
        self.mcp_functions = {
            "camera_panorama_animation": "mcp_unityMCP_camera_panorama_animation",
            "camera_closeup_animation": "mcp_unityMCP_camera_closeup_animation", 
            "camera_sweep_animation": "mcp_unityMCP_camera_sweep_animation",
            "rotate_around_target_animation": "mcp_unityMCP_rotate_around_target_animation",
            "create_multipoint_animation": "mcp_unityMCP_create_multipoint_animation"
        }
    
    async def process_nodegraph_with_real_mcp(self, nodegraph_name: str, 
                                              nodegraph_path: str = "Assets/NodeGraphTool/Test",
                                              execute_immediately: bool = False) -> ProcessingResult:
        """
        使用真实MCP处理NodeGraph Timeline
        
        Args:
            nodegraph_name: NodeGraph名称
            nodegraph_path: NodeGraph路径
            execute_immediately: 是否立即执行生成的命令
            
        Returns:
            ProcessingResult: 处理结果
        """
        start_time = time.time()
        
        print(f"🤖 开始真实MCP AI Timeline处理")
        print(f"目标NodeGraph: {nodegraph_name}")
        print(f"路径: {nodegraph_path}")
        print("=" * 50)
        
        try:
            # 1. 通过真实MCP获取FlowEventNode信息
            print("📚 正在获取FlowEventNode信息...")
            
            if hasattr(self, 'mcp_client') and self.mcp_client:
                # 调用真实MCP函数获取节点信息
                flow_nodes_result = await self.mcp_client.call_tool(
                    "mcp_unityMCP_get_flow_event_nodes",
                    {"name": nodegraph_name, "path": nodegraph_path}
                )
                flow_nodes = self._parse_flow_nodes_result(flow_nodes_result)
            else:
                # 备用：使用模拟数据
                print("⚠️ MCP客户端不可用，使用模拟数据")
                flow_nodes = self._get_mock_flow_nodes()
            
            if not flow_nodes:
                return ProcessingResult(
                    success=False,
                    processed_nodes=0, 
                    generated_commands=[],
                    errors=["无法获取FlowEventNode信息"],
                    processing_time=time.time() - start_time
                )
            
            print(f"✅ 成功获取 {len(flow_nodes)} 个FlowEventNode")
            
            # 2. 使用AI分析每个节点
            generated_commands = []
            errors = []
            processed_count = 0
            
            for node_name, node_data in flow_nodes.items():
                print(f"\n🔍 处理节点: {node_name}")
                
                try:
                    commands = await self._process_single_node_real(node_name, node_data)
                    generated_commands.extend(commands)
                    processed_count += 1
                    
                    print(f"   ✅ 生成 {len(commands)} 个动画命令")
                    
                except Exception as e:
                    error_msg = f"处理节点 {node_name} 失败: {str(e)}"
                    errors.append(error_msg)
                    print(f"   ❌ {error_msg}")
            
            # 3. 可选：立即执行命令
            if execute_immediately and generated_commands:
                print(f"\n🚀 立即执行 {len(generated_commands)} 个命令...")
                execution_result = await self._execute_real_mcp_commands(generated_commands)
                print(f"执行结果: 成功 {execution_result['successful']}, 失败 {execution_result['failed']}")
            
            # 4. 生成处理报告
            processing_time = time.time() - start_time
            
            print(f"\n📊 处理完成统计:")
            print(f"   处理节点: {processed_count}/{len(flow_nodes)}")
            print(f"   生成命令: {len(generated_commands)}")
            print(f"   错误数量: {len(errors)}")
            print(f"   处理耗时: {processing_time:.2f}秒")
            
            return ProcessingResult(
                success=len(errors) == 0,
                processed_nodes=processed_count,
                generated_commands=generated_commands,
                errors=errors,
                processing_time=processing_time
            )
            
        except Exception as e:
            return ProcessingResult(
                success=False,
                processed_nodes=0,
                generated_commands=[],
                errors=[f"处理失败: {str(e)}"],
                processing_time=time.time() - start_time
            )
    
    def _parse_flow_nodes_result(self, mcp_result: Any) -> Dict[str, Dict]:
        """解析MCP返回的FlowEventNode结果"""
        try:
            if isinstance(mcp_result, dict):
                # 提取节点信息
                nodes = {}
                for node_name, node_info in mcp_result.items():
                    if isinstance(node_info, dict):
                        nodes[node_name] = {
                            "镜头timeline名称": node_info.get("镜头timeline名称", "-"),
                            "镜头timeline内容": node_info.get("镜头timeline内容", "-"),
                            "物体timeline名称": node_info.get("物体timeline名称", "-"),
                            "物体timeline内容": node_info.get("物体timeline内容", "-")
                        }
                return nodes
            return {}
        except Exception as e:
            print(f"解析MCP结果失败: {e}")
            return {}
    
    def _get_mock_flow_nodes(self) -> Dict[str, Dict]:
        """获取模拟FlowEventNode数据"""
        return {
            "了解实验目的及意义": {
                "镜头timeline名称": "课程介绍_镜头",
                "镜头timeline内容": "镜头环视整个实验台，展示所有实验设备",
                "物体timeline名称": "-",
                "物体timeline内容": "-"
            },
            "检查仪器外观": {
                "镜头timeline名称": "检查外观_镜头",
                "镜头timeline内容": "镜头聚焦到紫外可见光分光仪进行特写观察",
                "物体timeline名称": "-", 
                "物体timeline内容": "-"
            },
            "连接仪器电源": {
                "镜头timeline名称": "连接电源_镜头",
                "镜头timeline内容": "镜头跟随电源线的移动过程",
                "物体timeline名称": "连接电源_电源线",
                "物体timeline内容": "电源线平移至插排电源口处并插入"
            }
        }
    
    async def _process_single_node_real(self, node_name: str, node_data: Dict) -> List[MCPCommand]:
        """处理单个节点（真实版本）"""
        commands = []
        
        # 处理镜头Timeline
        camera_content = node_data.get("镜头timeline内容", "")
        if camera_content and camera_content.strip() not in ["-", "无", ""]:
            print(f"   🎥 AI分析镜头动作: {camera_content}")
            
            ai_result = self.ai_classifier.classify_timeline_action(camera_content, "camera")
            
            if ai_result.confidence >= self.min_confidence_threshold:
                command = self._generate_real_mcp_command(ai_result, node_name, "camera")
                if command:
                    commands.append(command)
                    print(f"      ✅ 生成镜头命令 (置信度: {ai_result.confidence:.2f})")
            else:
                print(f"      ⚠️ 镜头动作置信度过低: {ai_result.confidence:.2f}")
        
        # 处理物体Timeline
        object_content = node_data.get("物体timeline内容", "")
        if object_content and object_content.strip() not in ["-", "无", ""]:
            print(f"   🎯 AI分析物体动作: {object_content}")
            
            ai_result = self.ai_classifier.classify_timeline_action(object_content, "object")
            
            if ai_result.confidence >= self.min_confidence_threshold:
                command = self._generate_real_mcp_command(ai_result, node_name, "object")
                if command:
                    commands.append(command)
                    print(f"      ✅ 生成物体命令 (置信度: {ai_result.confidence:.2f})")
            else:
                print(f"      ⚠️ 物体动作置信度过低: {ai_result.confidence:.2f}")
        
        return commands
    
    def _generate_real_mcp_command(self, ai_result: AIActionResult, node_name: str, timeline_type: str) -> Optional[MCPCommand]:
        """生成真实MCP命令"""
        function_name = ai_result.mcp_function
        
        if function_name not in self.mcp_functions:
            print(f"      ❌ 不支持的MCP函数: {function_name}")
            return None
        
        # 生成参数
        parameters = self._generate_command_parameters(ai_result, node_name, timeline_type)
        
        return MCPCommand(
            function_name=self.mcp_functions[function_name],
            parameters=parameters,
            confidence=ai_result.confidence,
            node_name=node_name,
            reasoning=ai_result.reasoning
        )
    
    def _generate_command_parameters(self, ai_result: AIActionResult, node_name: str, timeline_type: str) -> Dict[str, Any]:
        """生成命令参数"""
        base_params = ai_result.parameters
        function_name = ai_result.mcp_function
        
        # 根据不同的MCP函数生成对应参数
        if function_name == "camera_panorama_animation":
            return {
                "camera_name": base_params.get("camera_name", "Main Camera"),
                "pitch_angle": base_params.get("pitch_angle", -20),
                "duration": base_params.get("duration", 10),
                "steps": base_params.get("steps", 24),
                "timeline_asset_name": f"{node_name}_panorama"
            }
        
        elif function_name == "camera_closeup_animation":
            # 提取目标物体
            target_object = self._extract_target_object(ai_result, base_params)
            
            return {
                "camera_name": base_params.get("camera_name", "Main Camera"),
                "target_object_name": target_object,
                "closeup_distance": base_params.get("closeup_distance", 5),
                "pitch_angle": base_params.get("pitch_angle", 10),
                "duration": base_params.get("duration", 8),
                "timeline_asset_name": f"{node_name}_closeup"
            }
        
        elif function_name == "camera_sweep_animation":
            return {
                "camera_name": base_params.get("camera_name", "Main Camera"),
                "pitch_angle": base_params.get("pitch_angle", 0),
                "sweep_angle": base_params.get("sweep_angle", 45),
                "duration": base_params.get("duration", 8),
                "timeline_asset_name": f"{node_name}_sweep"
            }
        
        elif function_name == "rotate_around_target_animation":
            if timeline_type == "camera":
                moving_object = "Main Camera"
                target_object = self._extract_target_object(ai_result, base_params)
            else:
                moving_object = self._extract_object_name(ai_result, base_params)
                target_object = "紫外可见光分光仪"
            
            return {
                "moving_object_name": moving_object,
                "target_object_name": target_object,
                "radius": base_params.get("radius", 8),
                "height": base_params.get("height", 2),
                "duration": base_params.get("duration", 12),
                "timeline_asset_name": f"{node_name}_orbit"
            }
        
        elif function_name == "create_multipoint_animation":
            if timeline_type == "camera":
                object_name = "Main Camera"
            else:
                object_name = self._extract_object_name(ai_result, base_params)
            
            # 生成路径点
            points = self._generate_animation_points(ai_result, base_params)
            
            return {
                "name": object_name,
                "points": points,
                "duration": base_params.get("duration", 5),
                "path_type": base_params.get("path_type", "linear"),
                "include_rotation": base_params.get("include_rotation", False),
                "timeline_asset_name": f"{node_name}_multipoint"
            }
        
        else:
            # 默认参数
            return {
                "duration": 5,
                "timeline_asset_name": f"{node_name}_default"
            }
    
    def _extract_target_object(self, ai_result: AIActionResult, base_params: Dict) -> str:
        """提取目标物体名称"""
        # 优先级：显式指定 > AI提取 > 默认值
        target = base_params.get("target_object_name")
        if target:
            return target
        
        extracted_objects = base_params.get("extracted_objects", [])
        if extracted_objects:
            return extracted_objects[0]
        
        # 根据描述智能推断
        description = ai_result.description.lower()
        if "电源按钮" in description:
            return "电源按钮"
        elif "分光仪" in description:
            return "紫外可见光分光仪"
        elif "比色皿" in description:
            return "比色皿"
        
        return "紫外可见光分光仪"  # 默认目标
    
    def _extract_object_name(self, ai_result: AIActionResult, base_params: Dict) -> str:
        """提取物体名称"""
        object_name = base_params.get("name")
        if object_name and object_name != "unknown_object":
            return object_name
        
        # 从描述中提取
        description = ai_result.description
        if "电源线" in description:
            return "电源线"
        elif "电源按钮" in description:
            return "电源按钮"
        elif "比色皿" in description:
            return "比色皿"
        elif "样品室" in description:
            return "样品室"
        
        return "unknown_object"
    
    def _generate_animation_points(self, ai_result: AIActionResult, base_params: Dict) -> List[Dict]:
        """生成动画路径点"""
        existing_points = base_params.get("points", [])
        if existing_points:
            return existing_points
        
        # 根据动作类型生成默认路径点
        if base_params.get("include_rotation", False):
            # 按钮按压类动作
            return [
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 0, "y": -0.1, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}}
            ]
        else:
            # 移动类动作
            return [
                {"position": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 3, "y": 1, "z": 2}},
                {"position": {"x": 0, "y": 0, "z": 0}}
            ]
    
    async def _execute_real_mcp_commands(self, commands: List[MCPCommand]) -> Dict[str, Any]:
        """执行真实MCP命令"""
        print(f"\n🚀 开始执行 {len(commands)} 个MCP命令")
        
        results = {
            "total_commands": len(commands),
            "successful": 0,
            "failed": 0,
            "results": []
        }
        
        for i, command in enumerate(commands, 1):
            print(f"\n执行命令 {i}/{len(commands)}: {command.function_name}")
            print(f"节点: {command.node_name} (置信度: {command.confidence:.2f})")
            
            try:
                if hasattr(self, 'mcp_client') and self.mcp_client:
                    # 调用真实MCP函数
                    result = await self.mcp_client.call_tool(
                        command.function_name,
                        command.parameters
                    )
                    
                    print(f"✅ 命令执行成功")
                    results["successful"] += 1
                    results["results"].append({
                        "command": command.function_name,
                        "node": command.node_name,
                        "status": "success",
                        "result": result
                    })
                else:
                    # 模拟执行
                    print("📝 模拟执行 (MCP客户端不可用)")
                    results["successful"] += 1
                    results["results"].append({
                        "command": command.function_name,
                        "node": command.node_name,
                        "status": "simulated"
                    })
                
            except Exception as e:
                print(f"❌ 命令执行失败: {e}")
                results["failed"] += 1
                results["results"].append({
                    "command": command.function_name,
                    "node": command.node_name,
                    "status": "failed",
                    "error": str(e)
                })
        
        print(f"\n📊 执行完成统计:")
        print(f"   成功: {results['successful']}")
        print(f"   失败: {results['failed']}")
        
        return results
    
    def get_processing_summary(self, result: ProcessingResult) -> str:
        """生成处理摘要报告"""
        summary = f"""
🤖 AI Timeline处理摘要报告
===========================

📊 基本统计:
   • 处理状态: {'✅ 成功' if result.success else '❌ 失败'}
   • 处理节点: {result.processed_nodes}
   • 生成命令: {len(result.generated_commands)}
   • 错误数量: {len(result.errors)}
   • 处理耗时: {result.processing_time:.2f}秒

🎯 生成的命令:
"""
        
        for cmd in result.generated_commands:
            summary += f"   • {cmd.function_name} (节点: {cmd.node_name}, 置信度: {cmd.confidence:.2f})\n"
        
        if result.errors:
            summary += f"\n❌ 错误信息:\n"
            for error in result.errors:
                summary += f"   • {error}\n"
        
        avg_confidence = sum(cmd.confidence for cmd in result.generated_commands) / len(result.generated_commands) if result.generated_commands else 0
        
        summary += f"""
📈 质量评估:
   • 平均置信度: {avg_confidence:.2f}
   • 质量等级: {'优秀' if avg_confidence >= 0.8 else '良好' if avg_confidence >= 0.6 else '需改进'}
   • 建议: {'可直接使用' if avg_confidence >= 0.8 else '建议人工检查后使用' if avg_confidence >= 0.6 else '建议重新调整AI参数'}
"""
        
        return summary

# 使用示例和测试
async def demo_real_mcp_integration():
    """演示真实MCP集成"""
    print("🤖 真实MCP AI Timeline处理演示")
    print("=" * 50)
    
    # 创建处理器（在实际Cursor环境中，mcp_client会自动提供）
    processor = RealMCPAIProcessor(
        mcp_client=None,  # 在实际环境中会自动注入
        ai_provider=AIProvider.MOCK
    )
    
    # 处理NodeGraph
    result = await processor.process_nodegraph_with_real_mcp(
        nodegraph_name="Example",
        execute_immediately=False  # 设置为True可立即执行
    )
    
    # 打印摘要报告
    summary = processor.get_processing_summary(result)
    print(summary)
    
    return result

# 同步版本的接口
def process_nodegraph_sync(nodegraph_name: str, execute_immediately: bool = False):
    """同步版本的NodeGraph处理接口"""
    return asyncio.run(demo_real_mcp_integration())

if __name__ == "__main__":
    # 运行演示
    asyncio.run(demo_real_mcp_integration())