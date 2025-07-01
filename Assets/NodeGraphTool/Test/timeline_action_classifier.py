import re
import json
from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass

@dataclass
class ActionResult:
    """动作识别结果"""
    action_type: str
    confidence: float
    extracted_params: Dict
    mcp_function: str
    description: str

class TimelineActionClassifier:
    """Timeline动作类型识别器"""
    
    def __init__(self):
        self.camera_action_patterns = {
            # 镜头全景动画 - camera_panorama_animation
            "camera_panorama": {
                "keywords": [
                    "环视", "环绕", "360度", "全景", "旋转一周", "环视四周", 
                    "原地旋转", "绕圈", "全方位观察", "环顾", "俯瞰全景"
                ],
                "mcp_function": "camera_panorama_animation",
                "default_params": {
                    "camera_name": "Main Camera",
                    "pitch_angle": -20,
                    "duration": 10,
                    "steps": 24
                }
            },
            
            # 镜头扫视动画 - camera_sweep_animation  
            "camera_sweep": {
                "keywords": [
                    "扫视", "左右扫描", "水平扫描", "来回扫视", "左右移动",
                    "水平摆动", "扫描", "左右观察", "横向移动", "摆头"
                ],
                "mcp_function": "camera_sweep_animation", 
                "default_params": {
                    "camera_name": "Main Camera",
                    "pitch_angle": 0,
                    "sweep_angle": 45,
                    "duration": 8,
                    "steps": 18
                }
            },
            
            # 镜头特写动画 - camera_closeup_animation
            "camera_closeup": {
                "keywords": [
                    "特写", "聚焦", "近距离", "接近", "细节观察", "放大观察",
                    "靠近", "仔细观察", "详细查看", "近景", "局部观察"
                ],
                "mcp_function": "camera_closeup_animation",
                "default_params": {
                    "camera_name": "Main Camera", 
                    "closeup_distance": 5,
                    "pitch_angle": 10,
                    "horizontal_angle": 60,
                    "duration": 10,
                    "move_speed": 5
                }
            },
            
            # 镜头环绕目标动画 - rotate_around_target_animation
            "camera_orbit": {
                "keywords": [
                    "围绕.*旋转", "绕.*移动", "环绕.*拍摄", "围绕.*环视", 
                    "绕.*一周", "环形移动", "轨道运动", "绕轴旋转", "圆周运动"
                ],
                "mcp_function": "rotate_around_target_animation",
                "default_params": {
                    "moving_object_name": "Main Camera",
                    "radius": 8,
                    "height": 3,
                    "duration": 12,
                    "look_at_target": True
                }
            },
            
            # 镜头平移动画
            "camera_move": {
                "keywords": [
                    "平移", "移动到", "移至", "飞向", "推进", "拉远", 
                    "前进", "后退", "上升", "下降", "位移", "传送"
                ],
                "mcp_function": "create_multipoint_animation",
                "default_params": {
                    "duration": 5,
                    "path_type": "linear",
                    "include_rotation": True
                }
            }
        }
        
        self.object_action_patterns = {
            # 多点路径动画 - create_multipoint_animation
            "object_multipoint": {
                "keywords": [
                    "移动到", "平移至", "移至", "位移", "传送到", "拿取", 
                    "放置", "移动", "转移", "搬运", "取出", "放入"
                ],
                "mcp_function": "create_multipoint_animation",
                "default_params": {
                    "duration": 5,
                    "path_type": "linear", 
                    "include_rotation": False
                }
            },
            
            # 旋转动画
            "object_rotation": {
                "keywords": [
                    "旋转", "转动", "自转", "翻转", "转圈", "打开", "关闭",
                    "拧开", "拧紧", "扭转", "掰开", "合上"
                ],
                "mcp_function": "create_multipoint_animation", 
                "default_params": {
                    "duration": 3,
                    "path_type": "linear",
                    "include_rotation": True
                }
            },
            
            # 环绕运动 - rotate_around_target_animation
            "object_orbit": {
                "keywords": [
                    "围绕.*移动", "绕.*旋转", "环绕.*运动", "轨道运动",
                    "圆周运动", "环形移动", "绕圈移动"
                ],
                "mcp_function": "rotate_around_target_animation",
                "default_params": {
                    "radius": 5,
                    "height": 0,
                    "duration": 8,
                    "look_at_target": False
                }
            },
            
            # 复杂动作序列
            "object_sequence": {
                "keywords": [
                    "依次", "逐个", "顺序", "分别", "一个接一个", 
                    "先.*后", "接着", "然后", "最后"
                ],
                "mcp_function": "create_multipoint_animation",
                "default_params": {
                    "duration": 8,
                    "path_type": "curve",
                    "include_rotation": True
                }
            }
        }
        
        # 紫外可见光光度计实验专用动作模式
        self.experiment_specific_patterns = {
            # 仪器检查类
            "instrument_inspection": {
                "keywords": [
                    "检查.*外观", "检查.*电源", "检查.*样品室", "观察.*状态",
                    "确认.*连接", "验证.*功能"
                ],
                "camera_action": "camera_closeup",
                "object_action": None
            },
            
            # 按钮操作类
            "button_operation": {
                "keywords": [
                    "按下.*按钮", "按.*键", "点击.*按钮", "按压.*开关", 
                    "操作.*控制", "启动.*按钮"
                ],
                "camera_action": "camera_closeup",
                "object_action": "object_rotation"
            },
            
            # 样品操作类  
            "sample_handling": {
                "keywords": [
                    "放入.*比色皿", "取出.*样品", "更换.*溶液", "清洗.*器皿",
                    "倒入.*溶液", "倒掉.*液体"
                ],
                "camera_action": "camera_closeup", 
                "object_action": "object_multipoint"
            },
            
            # 设备连接类
            "equipment_connection": {
                "keywords": [
                    "连接.*电源", "插入.*插座", "连接.*线缆", "接通.*电源"
                ],
                "camera_action": "camera_closeup",
                "object_action": "object_multipoint" 
            },
            
            # 数据记录类
            "data_recording": {
                "keywords": [
                    "记录.*数值", "观察.*读数", "读取.*数据", "记录.*结果"
                ],
                "camera_action": "camera_closeup",
                "object_action": None
            },
            
            # 清理整理类
            "cleanup_organization": {
                "keywords": [
                    "擦拭.*表面", "清洁.*部件", "整理.*设备", "收拾.*器具",
                    "扔掉.*废料", "清理.*残留"
                ],
                "camera_action": "camera_sweep",
                "object_action": "object_multipoint"
            }
        }

    def extract_object_names(self, description: str) -> List[str]:
        """从描述中提取物体名称"""
        # 紫外可见光光度计实验常见物体
        objects = [
            "紫外可见光分光仪", "比色皿", "电源线", "废液烧杯", "擦拭棉球",
            "塑料洗瓶", "样品室", "电源按钮", "吸光度按钮", "调零按钮",
            "样品杆", "Main Camera", "笔记本", "手套"
        ]
        
        found_objects = []
        for obj in objects:
            if obj in description:
                found_objects.append(obj)
        
        # 使用正则表达式提取编号物体 (如比色皿1, 比色皿2)
        numbered_pattern = r'(比色皿|溶液|样品)(\d+)'
        matches = re.findall(numbered_pattern, description)
        for base_name, number in matches:
            found_objects.append(f"{base_name}{number}")
            
        return found_objects

    def extract_numeric_params(self, description: str) -> Dict:
        """从描述中提取数值参数"""
        params = {}
        
        # 提取时间相关
        time_patterns = [
            (r'(\d+\.?\d*)秒', 'duration'),
            (r'用时(\d+\.?\d*)秒', 'duration'), 
            (r'持续(\d+\.?\d*)秒', 'duration')
        ]
        
        # 提取角度相关
        angle_patterns = [
            (r'(\d+)度', 'angle'),
            (r'旋转(\d+)度', 'rotation_angle'),
            (r'俯仰(\d+)度', 'pitch_angle')
        ]
        
        # 提取距离相关
        distance_patterns = [
            (r'距离(\d+\.?\d*)米', 'distance'),
            (r'半径(\d+\.?\d*)米', 'radius'),
            (r'高度(\d+\.?\d*)米', 'height')
        ]
        
        all_patterns = time_patterns + angle_patterns + distance_patterns
        
        for pattern, param_name in all_patterns:
            match = re.search(pattern, description)
            if match:
                params[param_name] = float(match.group(1))
                
        return params

    def classify_timeline_action(self, description: str, timeline_type: str = "camera") -> ActionResult:
        """
        根据自然语言描述分类动作类型
        
        Args:
            description: 自然语言描述
            timeline_type: "camera" 或 "object"
            
        Returns:
            ActionResult: 包含动作类型、置信度、参数等信息
        """
        description = description.strip()
        best_match = None
        best_confidence = 0.0
        
        # 选择对应的模式库
        if timeline_type == "camera":
            patterns = self.camera_action_patterns
        else:
            patterns = self.object_action_patterns
            
        # 首先检查实验特定模式
        for exp_type, exp_data in self.experiment_specific_patterns.items():
            for keyword in exp_data["keywords"]:
                if re.search(keyword, description):
                    if timeline_type == "camera" and exp_data["camera_action"]:
                        action_type = exp_data["camera_action"]
                        confidence = 0.9
                        if confidence > best_confidence:
                            best_confidence = confidence
                            best_match = action_type
                    elif timeline_type == "object" and exp_data["object_action"]:
                        action_type = exp_data["object_action"] 
                        confidence = 0.9
                        if confidence > best_confidence:
                            best_confidence = confidence
                            best_match = action_type
        
        # 检查通用模式
        for action_type, action_data in patterns.items():
            keywords = action_data["keywords"]
            matches = 0
            total_keywords = len(keywords)
            
            for keyword in keywords:
                if re.search(keyword, description):
                    matches += 1
                    
            confidence = matches / total_keywords
            if confidence > best_confidence:
                best_confidence = confidence
                best_match = action_type
                
        if best_match is None:
            # 默认动作
            if timeline_type == "camera":
                best_match = "camera_move"
                best_confidence = 0.3
            else:
                best_match = "object_multipoint" 
                best_confidence = 0.3
        
        # 提取参数
        extracted_objects = self.extract_object_names(description)
        extracted_params = self.extract_numeric_params(description)
        
        # 合并默认参数
        action_data = patterns[best_match]
        default_params = action_data["default_params"].copy()
        default_params.update(extracted_params)
        
        # 添加物体信息
        if extracted_objects:
            if timeline_type == "camera" and "target_object_name" not in default_params:
                default_params["target_object_name"] = extracted_objects[0]
            elif timeline_type == "object" and "name" not in default_params:
                default_params["name"] = extracted_objects[0]
                
        return ActionResult(
            action_type=best_match,
            confidence=best_confidence,
            extracted_params=default_params,
            mcp_function=action_data["mcp_function"],
            description=description
        )

    def analyze_experiment_timelines(self, node_data: Dict) -> Tuple[ActionResult, ActionResult]:
        """
        分析实验节点的timeline数据，返回镜头和物体动作
        
        Args:
            node_data: FlowEventNode的完整数据
            
        Returns:
            Tuple[ActionResult, ActionResult]: (镜头动作结果, 物体动作结果)
        """
        camera_timeline = node_data.get("camera_timeline_content", "")
        object_timeline = node_data.get("object_timeline_content", "")
        
        camera_result = None
        object_result = None
        
        if camera_timeline and camera_timeline != "-" and camera_timeline != "无":
            camera_result = self.classify_timeline_action(camera_timeline, "camera")
            
        if object_timeline and object_timeline != "-" and object_timeline != "无":
            object_result = self.classify_timeline_action(object_timeline, "object")
            
        return camera_result, object_result

# 测试用例和使用示例
def test_classifier():
    """测试分类器功能"""
    classifier = TimelineActionClassifier()
    
    # 测试用例 - 紫外可见光光度计实验典型描述
    test_cases = [
        # 镜头动作测试
        ("镜头环视四周", "camera"),
        ("镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周", "camera"), 
        ("镜头平移至实验桌上方，俯视所有器具后返回", "camera"),
        ("镜头聚焦到电源按钮进行特写拍摄", "camera"),
        ("镜头左右扫视检查所有实验器材", "camera"),
        
        # 物体动作测试
        ("比色皿1移动到样品室内部", "object"),
        ("电源线平移至插排电源口处并插入", "object"), 
        ("擦拭棉球在样品室内部环绕擦拭一圈", "object"),
        ("按下电源按钮，按钮向下按压", "object"),
        ("紫外可见光分光仪样品室盖子向上旋转90度", "object")
    ]
    
    print("=== Timeline动作分类器测试结果 ===\n")
    
    for description, timeline_type in test_cases:
        result = classifier.classify_timeline_action(description, timeline_type)
        print(f"描述: {description}")
        print(f"类型: {timeline_type}")
        print(f"识别动作: {result.action_type}")
        print(f"置信度: {result.confidence:.2f}")
        print(f"MCP函数: {result.mcp_function}")
        print(f"提取参数: {result.extracted_params}")
        print("-" * 50)

if __name__ == "__main__":
    test_classifier() 