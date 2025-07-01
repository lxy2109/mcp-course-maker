"""
Timeline动作分类系统 - 完整使用指南
=====================================

本指南展示如何在实际项目中使用Timeline动作分类系统
处理NodeGraph SO文件中的FlowEventNode数据
"""

from timeline_action_classifier import TimelineActionClassifier, ActionResult
import json
from typing import Dict, List, Tuple, Optional

class NodeGraphTimelineProcessor:
    """NodeGraph Timeline处理器"""
    
    def __init__(self):
        self.classifier = TimelineActionClassifier()
        self.processed_nodes = []
        self.timeline_results = {}
    
    def process_single_node(self, node_name: str, node_data: Dict) -> Dict:
        """
        处理单个FlowEventNode节点
        
        Args:
            node_name: 节点名称
            node_data: 节点完整数据
            
        Returns:
            处理结果，包含分析的动作信息和MCP调用参数
        """
        print(f"\n🔄 处理节点: {node_name}")
        print("-" * 40)
        
        # 提取timeline描述
        camera_timeline = node_data.get("camera_timeline_content", "")
        object_timeline = node_data.get("object_timeline_content", "")
        
        result = {
            "node_name": node_name,
            "camera_action": None,
            "object_action": None,
            "mcp_calls": []
        }
        
        # 处理镜头动作
        if camera_timeline and camera_timeline not in ["-", "无", ""]:
            print(f"📹 镜头描述: {camera_timeline}")
            camera_result = self.classifier.classify_timeline_action(camera_timeline, "camera")
            result["camera_action"] = camera_result
            
            print(f"   → 识别类型: {camera_result.action_type}")
            print(f"   → MCP函数: {camera_result.mcp_function}")
            print(f"   → 置信度: {camera_result.confidence:.2f}")
            
            # 生成MCP调用
            mcp_call = self._generate_mcp_call(camera_result, "camera")
            result["mcp_calls"].append(mcp_call)
            print(f"   → MCP调用: {mcp_call['function']}")
        
        # 处理物体动作  
        if object_timeline and object_timeline not in ["-", "无", ""]:
            print(f"📦 物体描述: {object_timeline}")
            object_result = self.classifier.classify_timeline_action(object_timeline, "object")
            result["object_action"] = object_result
            
            print(f"   → 识别类型: {object_result.action_type}")
            print(f"   → MCP函数: {object_result.mcp_function}")
            print(f"   → 置信度: {object_result.confidence:.2f}")
            
            # 生成MCP调用
            mcp_call = self._generate_mcp_call(object_result, "object")
            result["mcp_calls"].append(mcp_call)
            print(f"   → MCP调用: {mcp_call['function']}")
        
        return result
    
    def _generate_mcp_call(self, action_result: ActionResult, action_type: str) -> Dict:
        """
        根据动作识别结果生成MCP调用参数
        
        Args:
            action_result: 动作识别结果
            action_type: 动作类型 ("camera" 或 "object")
            
        Returns:
            MCP调用参数字典
        """
        mcp_call = {
            "function": action_result.mcp_function,
            "parameters": action_result.extracted_params.copy(),
            "confidence": action_result.confidence,
            "type": action_type
        }
        
        # 添加timeline名称
        timeline_name = f"{action_result.action_type}_{action_type}_timeline"
        mcp_call["parameters"]["timeline_asset_name"] = timeline_name
        
        return mcp_call
    
    def process_all_nodes_demo(self):
        """演示处理所有节点的流程"""
        print("🎬 Timeline动作分类系统使用演示")
        print("=" * 50)
        
        # 模拟NodeGraph中的节点数据
        demo_nodes = {
            "了解实验目的及意义": {
                "camera_timeline_content": "镜头环视四周",
                "object_timeline_content": "-"
            },
            "检查仪器外观": {
                "camera_timeline_content": "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周",
                "object_timeline_content": "-"
            },
            "连接仪器电源": {
                "camera_timeline_content": "镜头聚焦到电源插口进行特写",
                "object_timeline_content": "电源线平移至插排电源口处并插入"
            },
            "按下电源按钮": {
                "camera_timeline_content": "镜头特写电源按钮",
                "object_timeline_content": "按下电源按钮，按钮向下按压"
            },
            "放入参比溶液1": {
                "camera_timeline_content": "镜头跟随比色皿移动到样品室",
                "object_timeline_content": "比色皿1移动到样品室内部"
            },
            "擦拭样品室": {
                "camera_timeline_content": "镜头扫视整个清理过程",
                "object_timeline_content": "擦拭棉球在样品室内部环绕擦拭一圈用时6秒"
            }
        }
        
        # 处理每个节点
        all_results = []
        for node_name, node_data in demo_nodes.items():
            result = self.process_single_node(node_name, node_data)
            all_results.append(result)
        
        return all_results
    
    def generate_batch_mcp_calls(self, results: List[Dict]) -> List[Dict]:
        """
        批量生成MCP调用列表
        
        Args:
            results: 处理结果列表
            
        Returns:
            MCP调用列表
        """
        all_mcp_calls = []
        
        print("\n🚀 生成的MCP调用列表")
        print("=" * 30)
        
        for result in results:
            node_name = result["node_name"]
            mcp_calls = result["mcp_calls"]
            
            if mcp_calls:
                print(f"\n📝 节点: {node_name}")
                for i, call in enumerate(mcp_calls, 1):
                    print(f"   {i}. {call['function']}")
                    print(f"      参数: {json.dumps(call['parameters'], ensure_ascii=False, indent=8)}")
                    all_mcp_calls.append(call)
        
        return all_mcp_calls


def demo_basic_usage():
    """基础使用示例"""
    print("\n📚 基础使用示例")
    print("=" * 20)
    
    # 1. 创建分类器实例
    classifier = TimelineActionClassifier()
    
    # 2. 分析单个动作描述
    descriptions = [
        ("镜头环视四周", "camera"),
        ("电源线平移至插排电源口处并插入", "object"),
        ("镜头聚焦到电源按钮进行特写观察", "camera"),
        ("比色皿1移动到样品室内部", "object")
    ]
    
    for desc, action_type in descriptions:
        result = classifier.classify_timeline_action(desc, action_type)
        print(f"\n输入: {desc}")
        print(f"类型: {action_type}")
        print(f"识别结果: {result.action_type}")
        print(f"MCP函数: {result.mcp_function}")
        print(f"置信度: {result.confidence:.2f}")
        print(f"参数: {json.dumps(result.extracted_params, ensure_ascii=False)}")


def demo_nodegraph_integration():
    """NodeGraph集成示例"""
    print("\n🔗 NodeGraph集成示例")
    print("=" * 25)
    
    # 这里展示如何与实际的MCP系统集成
    integration_code = '''
# 与Unity MCP系统集成的伪代码示例

def process_nodegraph_timelines(node_graph_name: str):
    """处理NodeGraph中所有节点的Timeline"""
    
    # 1. 获取所有FlowEventNode节点名称
    node_names = mcp_get_flow_event_node_names(node_graph_name)
    
    # 2. 创建分类器
    classifier = TimelineActionClassifier()
    
    # 3. 处理每个节点
    for node_name in node_names:
        # 获取节点详细信息
        node_data = mcp_get_flow_event_node_by_name(node_graph_name, node_name)
        
        # 提取timeline描述
        camera_desc = node_data.get("camera_timeline_content", "")
        object_desc = node_data.get("object_timeline_content", "")
        
        # 分析镜头动作
        if camera_desc and camera_desc != "-":
            camera_result = classifier.classify_timeline_action(camera_desc, "camera")
            
            # 调用对应的MCP函数
            if camera_result.mcp_function == "camera_panorama_animation":
                mcp_camera_panorama_animation(**camera_result.extracted_params)
            elif camera_result.mcp_function == "camera_closeup_animation":
                mcp_camera_closeup_animation(**camera_result.extracted_params)
            # ... 其他动作类型
        
        # 分析物体动作
        if object_desc and object_desc != "-":
            object_result = classifier.classify_timeline_action(object_desc, "object")
            
            # 调用对应的MCP函数
            if object_result.mcp_function == "create_multipoint_animation":
                mcp_create_multipoint_animation(**object_result.extracted_params)
            elif object_result.mcp_function == "rotate_around_target_animation":
                mcp_rotate_around_target_animation(**object_result.extracted_params)
    '''
    
    print(integration_code)


def demo_real_world_workflow():
    """真实世界工作流程示例"""
    print("\n🌍 真实工作流程示例")
    print("=" * 25)
    
    workflow_code = '''
# 完整的自动化Timeline生成工作流程

class AutoTimelineGenerator:
    def __init__(self):
        self.classifier = TimelineActionClassifier()
    
    def process_experiment_nodegraph(self, node_graph_path: str):
        """处理实验NodeGraph，自动生成Timeline"""
        
        # 步骤1: 读取NodeGraph信息
        node_names = self.get_all_flow_event_nodes(node_graph_path)
        
        # 步骤2: 逐个处理节点
        for node_name in node_names:
            print(f"处理节点: {node_name}")
            
            # 获取节点数据
            node_data = self.get_node_data(node_graph_path, node_name)
            
            # 分析Timeline描述
            camera_result, object_result = self.analyze_timelines(node_data)
            
            # 生成Timeline资产
            if camera_result:
                self.create_camera_timeline(camera_result, node_name)
            
            if object_result:
                self.create_object_timeline(object_result, node_name)
            
            # 绑定Timeline到节点
            self.bind_timelines_to_node(node_graph_path, node_name)
    
    def analyze_timelines(self, node_data):
        """分析节点的Timeline数据"""
        camera_desc = node_data.get("camera_timeline_content", "")
        object_desc = node_data.get("object_timeline_content", "")
        
        camera_result = None
        object_result = None
        
        if camera_desc and camera_desc not in ["-", "无"]:
            camera_result = self.classifier.classify_timeline_action(camera_desc, "camera")
        
        if object_desc and object_desc not in ["-", "无"]:
            object_result = self.classifier.classify_timeline_action(object_desc, "object")
        
        return camera_result, object_result
    
    def create_camera_timeline(self, camera_result, node_name):
        """创建镜头Timeline"""
        params = camera_result.extracted_params
        timeline_name = f"{node_name}_camera_timeline"
        
        if camera_result.mcp_function == "camera_panorama_animation":
            # 调用MCP创建全景动画
            self.mcp_camera_panorama_animation(
                camera_name=params.get("camera_name", "Main Camera"),
                pitch_angle=params.get("pitch_angle", -20),
                duration=params.get("duration", 10),
                timeline_asset_name=timeline_name
            )
        # ... 其他镜头动作类型
    
    def create_object_timeline(self, object_result, node_name):
        """创建物体Timeline"""
        params = object_result.extracted_params
        timeline_name = f"{node_name}_object_timeline"
        
        if object_result.mcp_function == "create_multipoint_animation":
            # 根据描述生成路径点
            points = self.generate_movement_points(object_result.description)
            
            # 调用MCP创建多点动画
            self.mcp_create_multipoint_animation(
                name=params.get("name"),
                points=points,
                duration=params.get("duration", 5),
                timeline_asset_name=timeline_name
            )
        # ... 其他物体动作类型

# 使用示例
generator = AutoTimelineGenerator()
generator.process_experiment_nodegraph("Assets/NodeGraphTool/Test/Example.asset")
    '''
    
    print(workflow_code)


def demo_error_handling():
    """错误处理和置信度检查示例"""
    print("\n⚠️ 错误处理示例")
    print("=" * 20)
    
    classifier = TimelineActionClassifier()
    
    # 测试各种边界情况
    edge_cases = [
        ("", "camera"),  # 空描述
        ("一些无法识别的描述文本", "camera"),  # 无法识别的描述
        ("镜头可能需要移动到某个地方", "camera"),  # 模糊描述
        ("旋转90度用时3秒", "object")  # 缺少主体的描述
    ]
    
    for desc, action_type in edge_cases:
        result = classifier.classify_timeline_action(desc, action_type)
        
        print(f"\n输入: '{desc}'")
        print(f"识别结果: {result.action_type}")
        print(f"置信度: {result.confidence:.2f}")
        
        # 置信度检查
        if result.confidence < 0.5:
            print("⚠️ 低置信度警告: 建议人工检查")
        elif result.confidence < 0.7:
            print("⚡ 中等置信度: 可能需要参数调整")
        else:
            print("✅ 高置信度: 可以自动处理")


if __name__ == "__main__":
    # 运行所有演示
    demo_basic_usage()
    
    # 完整流程演示
    processor = NodeGraphTimelineProcessor()
    results = processor.process_all_nodes_demo()
    mcp_calls = processor.generate_batch_mcp_calls(results)
    
    demo_nodegraph_integration()
    demo_real_world_workflow()
    demo_error_handling()
    
    print("\n🎉 演示完成！")
    print("现在你可以开始在你的项目中使用Timeline动作分类系统了。") 