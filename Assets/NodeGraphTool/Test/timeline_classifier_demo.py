"""
Timeline动作分类器演示
===================

基于紫外可见光光度计测量实验的完整动作分类系统
"""

from timeline_action_classifier import TimelineActionClassifier, ActionResult
import json

def demonstrate_classifier():
    """演示分类器功能"""
    classifier = TimelineActionClassifier()
    
    print("🔬 紫外可见光光度计实验 - Timeline动作分类器演示")
    print("=" * 60)
    
    # 实验中的典型镜头动作
    camera_actions = {
        "全景观察": [
            "镜头环视四周",
            "镜头原地旋转360度观察实验台",
            "全景拍摄实验室环境"
        ],
        
        "设备检查": [
            "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周",
            "镜头聚焦到电源按钮进行特写观察",
            "镜头仔细观察样品室内部结构",
            "镜头近距离检查仪器外观是否完好"
        ],
        
        "操作跟随": [
            "镜头左右扫视检查所有实验器材",
            "镜头从上方俯视所有器具的摆放",
            "镜头跟随手部动作移动到操作位置"
        ],
        
        "数据记录": [
            "镜头聚焦到显示屏读取数值",
            "镜头特写仪器面板上的指示灯",
            "镜头近距离观察波长设定值"
        ]
    }
    
    # 实验中的典型物体动作  
    object_actions = {
        "电源连接": [
            "电源线平移至插排电源口处并插入",
            "电源线从桌面移动到墙面插座"
        ],
        
        "按钮操作": [
            "按下电源按钮，按钮向下按压",
            "吸光度按钮旋转到指定位置",
            "调零按钮按下后弹起"
        ],
        
        "样品操作": [
            "比色皿1移动到样品室内部",
            "比色皿2从实验台转移到样品架",
            "比色皿3取出并放置到清洗区域"
        ],
        
        "仪器部件": [
            "紫外可见光分光仪样品室盖子向上旋转90度",
            "样品杆向左拉动切换样品位置",
            "样品杆向右推动回到初始位置"
        ],
        
        "清洁维护": [
            "擦拭棉球在样品室内部环绕擦拭一圈",
            "擦拭棉球移动到废液烧杯中丢弃",
            "塑料洗瓶移动到比色皿上方进行冲洗"
        ]
    }
    
    print("\n📹 镜头动作分类演示")
    print("-" * 40)
    
    for category, descriptions in camera_actions.items():
        print(f"\n🎬 {category}:")
        for desc in descriptions:
            result = classifier.classify_timeline_action(desc, "camera")
            print(f"  描述: {desc}")
            print(f"  → 动作类型: {result.action_type}")
            print(f"  → MCP函数: {result.mcp_function}")
            print(f"  → 置信度: {result.confidence:.2f}")
            if result.extracted_params:
                print(f"  → 参数: {json.dumps(result.extracted_params, ensure_ascii=False, indent=6)}")
            print()
    
    print("\n📦 物体动作分类演示") 
    print("-" * 40)
    
    for category, descriptions in object_actions.items():
        print(f"\n🔧 {category}:")
        for desc in descriptions:
            result = classifier.classify_timeline_action(desc, "object")
            print(f"  描述: {desc}")
            print(f"  → 动作类型: {result.action_type}")
            print(f"  → MCP函数: {result.mcp_function}")
            print(f"  → 置信度: {result.confidence:.2f}")
            if result.extracted_params:
                print(f"  → 参数: {json.dumps(result.extracted_params, ensure_ascii=False, indent=6)}")
            print()

def show_experiment_workflow():
    """展示完整的实验工作流程"""
    print("\n🧪 紫外可见光光度计实验完整工作流程")
    print("=" * 50)
    
    # 完整实验流程的Timeline动作
    workflow_steps = [
        {
            "step": "实验准备阶段",
            "camera": "镜头环视四周检查实验环境", 
            "object": "无"
        },
        {
            "step": "设备检查",
            "camera": "镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周",
            "object": "无"
        },
        {
            "step": "电源连接", 
            "camera": "镜头聚焦到电源插口进行特写",
            "object": "电源线平移至插排电源口处并插入"
        },
        {
            "step": "开机操作",
            "camera": "镜头特写电源按钮",
            "object": "按下电源按钮，按钮向下按压"
        },
        {
            "step": "样品放入",
            "camera": "镜头跟随样品移动到样品室",
            "object": "比色皿1移动到样品室内部"
        },
        {
            "step": "测量操作",
            "camera": "镜头聚焦到显示屏读取数值", 
            "object": "样品杆向左拉动切换样品位置"
        },
        {
            "step": "清理维护",
            "camera": "镜头扫视整个清理过程",
            "object": "擦拭棉球在样品室内部环绕擦拭一圈"
        }
    ]
    
    classifier = TimelineActionClassifier()
    
    for i, step_data in enumerate(workflow_steps, 1):
        print(f"\n步骤 {i}: {step_data['step']}")
        print("=" * 25)
        
        # 分析镜头动作
        if step_data['camera'] != "无":
            camera_result = classifier.classify_timeline_action(step_data['camera'], "camera")
            print(f"📹 镜头动作: {step_data['camera']}")
            print(f"   类型: {camera_result.action_type}")
            print(f"   MCP: {camera_result.mcp_function}")
            print(f"   置信度: {camera_result.confidence:.2f}")
            
        # 分析物体动作
        if step_data['object'] != "无":
            object_result = classifier.classify_timeline_action(step_data['object'], "object")
            print(f"📦 物体动作: {step_data['object']}")
            print(f"   类型: {object_result.action_type}")
            print(f"   MCP: {object_result.mcp_function}")
            print(f"   置信度: {object_result.confidence:.2f}")

def show_mcp_mapping():
    """展示MCP函数映射表"""
    print("\n🔗 MCP函数映射表")
    print("=" * 30)
    
    mcp_mappings = {
        "镜头动作MCP函数": {
            "camera_panorama_animation": "全景旋转动画",
            "camera_sweep_animation": "左右扫视动画", 
            "camera_closeup_animation": "特写聚焦动画",
            "rotate_around_target_animation": "环绕目标动画",
            "create_multipoint_animation": "多点路径动画"
        },
        
        "物体动作MCP函数": {
            "create_multipoint_animation": "多点路径移动",
            "rotate_around_target_animation": "环绕运动"
        }
    }
    
    for category, mappings in mcp_mappings.items():
        print(f"\n📋 {category}:")
        for mcp_func, description in mappings.items():
            print(f"  • {mcp_func}")
            print(f"    → {description}")

if __name__ == "__main__":
    demonstrate_classifier()
    show_experiment_workflow() 
    show_mcp_mapping() 