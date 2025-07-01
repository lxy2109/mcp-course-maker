"""
Timeline动作分类系统 - 实际应用示例
================================

展示如何在实际项目中集成Timeline动作分类系统
处理Example NodeGraph的Timeline数据
"""

from timeline_action_classifier import TimelineActionClassifier
import json

class NodeGraphTimelineAutoProcessor:
    """NodeGraph Timeline自动处理器 - 实际应用版本"""
    
    def __init__(self):
        self.classifier = TimelineActionClassifier()
        self.mcp_commands = []
    
    def process_example_nodegraph(self):
        """处理Example NodeGraph中的Timeline数据"""
        print("🎬 开始处理Example NodeGraph的Timeline数据")
        print("=" * 50)
        
        # 这里模拟从实际MCP获取的节点数据
        # 在真实应用中，这些数据来自：
        # mcp_get_flow_event_node_names() 和 mcp_get_flow_event_node_by_name()
        
        example_nodes = [
            {
                "name": "了解实验目的及意义",
                "camera_timeline_content": "镜头环视四周",
                "object_timeline_content": "-"
            },
            {
                "name": "检查仪器外观", 
                "camera_timeline_content": "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周",
                "object_timeline_content": "-"
            },
            {
                "name": "连接仪器电源",
                "camera_timeline_content": "镜头聚焦到电源插口进行特写",
                "object_timeline_content": "电源线平移至插排电源口处并插入"
            },
            {
                "name": "按下电源按钮",
                "camera_timeline_content": "镜头特写电源按钮",
                "object_timeline_content": "按下电源按钮，按钮向下按压"
            },
            {
                "name": "检查仪器样品室",
                "camera_timeline_content": "镜头聚焦样品室进行检查",
                "object_timeline_content": "紫外可见光分光仪样品室盖子向上旋转90度然后恢复"
            }
        ]
        
        # 处理每个节点
        for node in example_nodes:
            self.process_node_timelines(node)
        
        # 生成最终的MCP命令列表
        return self.generate_final_mcp_commands()
    
    def process_node_timelines(self, node_data):
        """处理单个节点的Timeline数据"""
        node_name = node_data["name"]
        print(f"\n🔄 处理节点: {node_name}")
        print("-" * 30)
        
        # 处理镜头Timeline
        camera_timeline = node_data.get("camera_timeline_content", "")
        if camera_timeline and camera_timeline not in ["-", "无", ""]:
            camera_command = self.process_camera_timeline(node_name, camera_timeline)
            if camera_command:
                self.mcp_commands.append(camera_command)
        
        # 处理物体Timeline
        object_timeline = node_data.get("object_timeline_content", "")
        if object_timeline and object_timeline not in ["-", "无", ""]:
            object_command = self.process_object_timeline(node_name, object_timeline)
            if object_command:
                self.mcp_commands.append(object_command)
    
    def process_camera_timeline(self, node_name, description):
        """处理镜头Timeline"""
        print(f"📹 分析镜头动作: {description}")
        
        # 使用分类器分析
        result = self.classifier.classify_timeline_action(description, "camera")
        
        print(f"   → 识别为: {result.action_type}")
        print(f"   → 置信度: {result.confidence:.2f}")
        print(f"   → MCP函数: {result.mcp_function}")
        
        # 根据识别结果生成MCP命令
        timeline_name = f"{node_name}_Camera"
        mcp_command = {
            "node_name": node_name,
            "type": "camera",
            "function": result.mcp_function,
            "timeline_name": timeline_name,
            "params": result.extracted_params.copy(),
            "confidence": result.confidence,
            "description": description
        }
        
        # 添加timeline名称到参数中
        mcp_command["params"]["timeline_asset_name"] = timeline_name
        
        # 根据不同的MCP函数类型，调整参数
        if result.mcp_function == "camera_panorama_animation":
            mcp_command["params"].setdefault("camera_name", "Main Camera")
            mcp_command["params"].setdefault("pitch_angle", -20)
            mcp_command["params"].setdefault("duration", 10)
            
        elif result.mcp_function == "camera_closeup_animation":
            mcp_command["params"].setdefault("camera_name", "Main Camera")
            mcp_command["params"].setdefault("closeup_distance", 5)
            mcp_command["params"].setdefault("pitch_angle", 10)
            
        elif result.mcp_function == "camera_sweep_animation":
            mcp_command["params"].setdefault("camera_name", "Main Camera")
            mcp_command["params"].setdefault("sweep_angle", 45)
            mcp_command["params"].setdefault("duration", 8)
        
        return mcp_command
    
    def process_object_timeline(self, node_name, description):
        """处理物体Timeline"""
        print(f"📦 分析物体动作: {description}")
        
        # 使用分类器分析
        result = self.classifier.classify_timeline_action(description, "object")
        
        print(f"   → 识别为: {result.action_type}")
        print(f"   → 置信度: {result.confidence:.2f}")
        print(f"   → MCP函数: {result.mcp_function}")
        
        # 根据识别结果生成MCP命令
        timeline_name = f"{node_name}_Object"
        mcp_command = {
            "node_name": node_name,
            "type": "object",
            "function": result.mcp_function,
            "timeline_name": timeline_name,
            "params": result.extracted_params.copy(),
            "confidence": result.confidence,
            "description": description
        }
        
        # 添加timeline名称到参数中
        mcp_command["params"]["timeline_asset_name"] = timeline_name
        
        # 根据不同的MCP函数类型，调整参数
        if result.mcp_function == "create_multipoint_animation":
            mcp_command["params"].setdefault("duration", 5)
            mcp_command["params"].setdefault("path_type", "linear")
            
            # 根据描述生成基本的路径点
            if "移动到" in description or "平移至" in description:
                mcp_command["params"]["points"] = self.generate_basic_movement_points(description)
            elif "旋转" in description:
                mcp_command["params"]["include_rotation"] = True
                mcp_command["params"]["points"] = self.generate_rotation_points(description)
        
        return mcp_command
    
    def generate_basic_movement_points(self, description):
        """根据描述生成基本的移动路径点"""
        # 这里是简化的路径点生成逻辑
        # 在实际应用中，可能需要更复杂的场景分析
        
        if "电源线" in description:
            return [
                {"position": {"x": 0, "y": 0, "z": 0}},  # 起始位置
                {"position": {"x": 2, "y": 0, "z": 1}}   # 插座位置
            ]
        elif "比色皿" in description:
            return [
                {"position": {"x": -1, "y": 0, "z": 0}}, # 起始位置
                {"position": {"x": 0, "y": 0.5, "z": 2}} # 样品室位置
            ]
        else:
            return [
                {"position": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 1, "y": 0, "z": 1}}
            ]
    
    def generate_rotation_points(self, description):
        """根据描述生成旋转路径点"""
        if "90度" in description:
            return [
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 90, "y": 0, "z": 0}},
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}}
            ]
        else:
            return [
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}},
                {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 45, "z": 0}}
            ]
    
    def generate_final_mcp_commands(self):
        """生成最终的MCP命令列表"""
        print("\n🚀 生成的MCP命令列表")
        print("=" * 40)
        
        for i, command in enumerate(self.mcp_commands, 1):
            print(f"\n{i}. 节点: {command['node_name']} ({command['type']})")
            print(f"   函数: {command['function']}")
            print(f"   Timeline: {command['timeline_name']}")
            print(f"   置信度: {command['confidence']:.2f}")
            print(f"   参数:")
            
            # 格式化显示参数
            for key, value in command['params'].items():
                if isinstance(value, dict) or isinstance(value, list):
                    print(f"     {key}: {json.dumps(value, ensure_ascii=False)}")
                else:
                    print(f"     {key}: {value}")
        
        return self.mcp_commands


def execute_mcp_commands_simulation(commands):
    """模拟执行MCP命令"""
    print("\n🎯 模拟执行MCP命令")
    print("=" * 25)
    
    for command in commands:
        print(f"\n执行: {command['function']}")
        print(f"Timeline: {command['timeline_name']}")
        
        # 这里在实际应用中会调用真正的MCP函数
        # 例如：
        # if command['function'] == 'camera_panorama_animation':
        #     mcp_camera_panorama_animation(**command['params'])
        # elif command['function'] == 'camera_closeup_animation':
        #     mcp_camera_closeup_animation(**command['params'])
        # elif command['function'] == 'create_multipoint_animation':
        #     mcp_create_multipoint_animation(**command['params'])
        
        print(f"✅ {command['function']} 执行完成")


def main():
    """主函数 - 演示完整的应用流程"""
    print("🎬 Timeline动作分类系统 - 实际应用演示")
    print("=" * 45)
    
    # 创建处理器
    processor = NodeGraphTimelineAutoProcessor()
    
    # 处理NodeGraph
    mcp_commands = processor.process_example_nodegraph()
    
    # 模拟执行MCP命令
    execute_mcp_commands_simulation(mcp_commands)
    
    print("\n📊 处理结果统计")
    print("=" * 20)
    
    camera_commands = [cmd for cmd in mcp_commands if cmd['type'] == 'camera']
    object_commands = [cmd for cmd in mcp_commands if cmd['type'] == 'object']
    
    print(f"总节点数: {len(set(cmd['node_name'] for cmd in mcp_commands))}")
    print(f"镜头动画: {len(camera_commands)} 个")
    print(f"物体动画: {len(object_commands)} 个")
    print(f"总Timeline: {len(mcp_commands)} 个")
    
    # 置信度统计
    high_confidence = [cmd for cmd in mcp_commands if cmd['confidence'] >= 0.7]
    medium_confidence = [cmd for cmd in mcp_commands if 0.5 <= cmd['confidence'] < 0.7]
    low_confidence = [cmd for cmd in mcp_commands if cmd['confidence'] < 0.5]
    
    print(f"\n置信度统计:")
    print(f"高置信度 (≥0.7): {len(high_confidence)} 个")
    print(f"中等置信度 (0.5-0.7): {len(medium_confidence)} 个")
    print(f"低置信度 (<0.5): {len(low_confidence)} 个")
    
    if low_confidence:
        print(f"\n⚠️ 需要人工检查的低置信度项目:")
        for cmd in low_confidence:
            print(f"   - {cmd['node_name']}: {cmd['description'][:30]}...")


if __name__ == "__main__":
    main() 