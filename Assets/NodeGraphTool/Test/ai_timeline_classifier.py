"""
AI驱动的Timeline动作分类系统
===========================

使用大语言模型进行语义理解，大幅提升动作分类的准确率
支持OpenAI GPT、Claude、以及本地模型
"""

import json
import re
from typing import Dict, List, Tuple, Optional, Any
from dataclasses import dataclass, asdict
from enum import Enum

# 根据可用的AI库进行导入
try:
    import openai
    HAS_OPENAI = True
except ImportError:
    HAS_OPENAI = False

try:
    import anthropic
    HAS_ANTHROPIC = True
except ImportError:
    HAS_ANTHROPIC = False

class AIProvider(Enum):
    """AI提供商枚举"""
    OPENAI = "openai"
    ANTHROPIC = "anthropic"
    LOCAL = "local"
    MOCK = "mock"  # 用于演示

@dataclass
class AIActionResult:
    """AI动作识别结果"""
    action_type: str
    mcp_function: str
    confidence: float
    parameters: Dict[str, Any]
    reasoning: str
    timeline_type: str  # "camera" 或 "object"
    description: str

class AITimelineClassifier:
    """基于AI的Timeline动作分类器"""
    
    def __init__(self, provider: AIProvider = AIProvider.MOCK, api_key: str = None):
        self.provider = provider
        self.api_key = api_key
        self.client = self._initialize_client()
        
        # 动作类型定义（用于AI参考）
        self.action_definitions = {
            "camera_actions": {
                "camera_panorama": {
                    "description": "相机全景旋转动画，原地360度环视",
                    "mcp_function": "camera_panorama_animation",
                    "typical_keywords": ["环视", "全景", "360度", "旋转一周", "环绕"],
                    "parameters": ["camera_name", "pitch_angle", "duration", "steps"]
                },
                "camera_closeup": {
                    "description": "相机特写动画，聚焦到特定物体进行近距离观察",
                    "mcp_function": "camera_closeup_animation", 
                    "typical_keywords": ["特写", "聚焦", "近距离", "细节", "观察"],
                    "parameters": ["camera_name", "target_object_name", "closeup_distance", "pitch_angle"]
                },
                "camera_sweep": {
                    "description": "相机扫视动画，左右水平扫描观察",
                    "mcp_function": "camera_sweep_animation",
                    "typical_keywords": ["扫视", "左右", "水平", "扫描", "摆动"],
                    "parameters": ["camera_name", "sweep_angle", "duration", "pitch_angle"]
                },
                "camera_orbit": {
                    "description": "相机环绕目标动画，围绕特定物体移动拍摄",
                    "mcp_function": "rotate_around_target_animation",
                    "typical_keywords": ["围绕", "环绕", "轨道", "绕着", "圆周"],
                    "parameters": ["moving_object_name", "target_object_name", "radius", "height", "duration"]
                },
                "camera_move": {
                    "description": "相机移动动画，从一个位置移动到另一个位置",
                    "mcp_function": "create_multipoint_animation",
                    "typical_keywords": ["移动", "平移", "飞向", "推进", "拉远"],
                    "parameters": ["name", "points", "duration", "path_type"]
                }
            },
            "object_actions": {
                "object_move": {
                    "description": "物体移动动画，物体从一个位置移动到另一个位置",
                    "mcp_function": "create_multipoint_animation",
                    "typical_keywords": ["移动", "平移", "转移", "拿取", "放置"],
                    "parameters": ["name", "points", "duration", "path_type"]
                },
                "object_rotate": {
                    "description": "物体旋转动画，物体绕自身轴心旋转",
                    "mcp_function": "create_multipoint_animation",
                    "typical_keywords": ["旋转", "转动", "打开", "关闭", "按下"],
                    "parameters": ["name", "points", "duration", "include_rotation"]
                },
                "object_orbit": {
                    "description": "物体环绕动画，物体围绕另一个物体运动",
                    "mcp_function": "rotate_around_target_animation", 
                    "typical_keywords": ["围绕", "环绕", "绕着", "圆周", "轨道"],
                    "parameters": ["moving_object_name", "target_object_name", "radius", "duration"]
                }
            }
        }
    
    def _initialize_client(self):
        """初始化AI客户端"""
        if self.provider == AIProvider.OPENAI and HAS_OPENAI:
            return openai.OpenAI(api_key=self.api_key)
        elif self.provider == AIProvider.ANTHROPIC and HAS_ANTHROPIC:
            return anthropic.Anthropic(api_key=self.api_key)
        elif self.provider == AIProvider.MOCK:
            return None  # 模拟客户端
        else:
            return None
    
    def classify_timeline_action(self, description: str, timeline_type: str) -> AIActionResult:
        """
        使用AI分类timeline动作
        
        Args:
            description: 自然语言描述
            timeline_type: "camera" 或 "object"
            
        Returns:
            AIActionResult: AI分析结果
        """
        if not description or description.strip() in ["-", "无", ""]:
            return self._create_default_result(timeline_type, description)
        
        # 构建AI提示词
        prompt = self._build_classification_prompt(description, timeline_type)
        
        # 调用AI进行分类
        if self.provider == AIProvider.MOCK:
            ai_response = self._mock_ai_response(description, timeline_type)
        else:
            ai_response = self._call_ai_api(prompt)
        
        # 解析AI响应
        result = self._parse_ai_response(ai_response, description, timeline_type)
        
        return result
    
    def _build_classification_prompt(self, description: str, timeline_type: str) -> str:
        """构建AI分类提示词"""
        
        action_types = self.action_definitions[f"{timeline_type}_actions"]
        
        prompt = f"""
你是一个专业的Unity Timeline动作分析专家。请分析以下描述并确定最合适的动作类型和参数。

# 任务描述
分析这个{timeline_type}动作描述："{description}"

# 可选的{timeline_type}动作类型：
"""
        
        for action_name, action_info in action_types.items():
            prompt += f"""
## {action_name}
- 描述: {action_info['description']}
- MCP函数: {action_info['mcp_function']}
- 关键词: {', '.join(action_info['typical_keywords'])}
- 参数: {', '.join(action_info['parameters'])}
"""
        
        prompt += f"""

# 物体列表（紫外可见光光度计实验）
- 紫外可见光分光仪, 比色皿, 比色皿1, 比色皿2, 比色皿3
- 电源线, 废液烧杯, 擦拭棉球, 塑料洗瓶, 样品室
- 电源按钮, 吸光度按钮, 调零按钮, 样品杆, Main Camera

# 输出要求
请以JSON格式返回分析结果，包含以下字段：
{{
    "action_type": "选择的动作类型",
    "mcp_function": "对应的MCP函数名",
    "confidence": 0.95,  // 置信度 0.0-1.0
    "parameters": {{
        // 根据动作类型提取的具体参数
        "timeline_asset_name": "生成的timeline名称"
    }},
    "reasoning": "选择这个动作类型的原因分析",
    "extracted_objects": ["识别出的物体名称"],
    "extracted_numbers": {{
        // 提取的数值信息，如时间、角度、距离等
    }}
}}

请仔细分析描述的语义，选择最合适的动作类型，并提取相关参数。
"""
        
        return prompt
    
    def _call_ai_api(self, prompt: str) -> str:
        """调用AI API"""
        try:
            if self.provider == AIProvider.OPENAI and self.client:
                response = self.client.chat.completions.create(
                    model="gpt-4",
                    messages=[
                        {"role": "system", "content": "你是一个专业的Unity Timeline动作分析专家。"},
                        {"role": "user", "content": prompt}
                    ],
                    temperature=0.1
                )
                return response.choices[0].message.content
                
            elif self.provider == AIProvider.ANTHROPIC and self.client:
                response = self.client.messages.create(
                    model="claude-3-sonnet-20240229",
                    max_tokens=1000,
                    messages=[
                        {"role": "user", "content": prompt}
                    ]
                )
                return response.content[0].text
                
        except Exception as e:
            print(f"AI API调用失败: {e}")
            return self._fallback_response()
        
        return self._fallback_response()
    
    def _mock_ai_response(self, description: str, timeline_type: str) -> str:
        """模拟AI响应（用于演示）"""
        
        # 基于关键词的智能模拟
        mock_responses = {
            "镜头环视四周": {
                "action_type": "camera_panorama",
                "mcp_function": "camera_panorama_animation",
                "confidence": 0.95,
                "parameters": {
                    "camera_name": "Main Camera",
                    "pitch_angle": -20,
                    "duration": 10,
                    "steps": 24,
                    "timeline_asset_name": "panorama_timeline"
                },
                "reasoning": "描述明确指出'环视四周'，这是典型的全景旋转动画特征",
                "extracted_objects": ["Main Camera"],
                "extracted_numbers": {}
            },
            "镜头聚焦到电源按钮进行特写": {
                "action_type": "camera_closeup", 
                "mcp_function": "camera_closeup_animation",
                "confidence": 0.92,
                "parameters": {
                    "camera_name": "Main Camera",
                    "target_object_name": "电源按钮",
                    "closeup_distance": 5,
                    "pitch_angle": 10,
                    "duration": 8,
                    "timeline_asset_name": "closeup_timeline"
                },
                "reasoning": "描述包含'聚焦'和'特写'关键词，明确指定了目标物体'电源按钮'",
                "extracted_objects": ["Main Camera", "电源按钮"],
                "extracted_numbers": {}
            },
            "电源线平移至插排电源口处并插入": {
                "action_type": "object_move",
                "mcp_function": "create_multipoint_animation",
                "confidence": 0.88,
                "parameters": {
                    "name": "电源线",
                    "duration": 5,
                    "path_type": "linear",
                    "include_rotation": False,
                    "points": [
                        {"position": {"x": 0, "y": 0, "z": 0}},
                        {"position": {"x": 2, "y": 0, "z": 1}}
                    ],
                    "timeline_asset_name": "move_timeline"
                },
                "reasoning": "描述指出'电源线平移至插排'，这是典型的物体移动动画",
                "extracted_objects": ["电源线"],
                "extracted_numbers": {}
            },
            "按下电源按钮，按钮向下按压": {
                "action_type": "object_rotate",
                "mcp_function": "create_multipoint_animation", 
                "confidence": 0.90,
                "parameters": {
                    "name": "电源按钮",
                    "duration": 2,
                    "path_type": "linear",
                    "include_rotation": True,
                    "points": [
                        {"position": {"x": 0, "y": 0, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}},
                        {"position": {"x": 0, "y": -0.1, "z": 0}, "rotation": {"x": 0, "y": 0, "z": 0}}
                    ],
                    "timeline_asset_name": "press_timeline"
                },
                "reasoning": "描述包含'按下'和'向下按压'，说明是按钮的按压动作",
                "extracted_objects": ["电源按钮"],
                "extracted_numbers": {}
            }
        }
        
        # 查找最佳匹配
        best_match = None
        best_score = 0
        
        for key, response in mock_responses.items():
            score = self._calculate_similarity(description, key)
            if score > best_score:
                best_score = score
                best_match = response
        
        # 如果没有好的匹配，使用智能分析
        if best_match is None or best_score < 0.3:
            best_match = self._intelligent_mock_analysis(description, timeline_type)
        
        return json.dumps(best_match, ensure_ascii=False, indent=2)
    
    def _intelligent_mock_analysis(self, description: str, timeline_type: str) -> Dict:
        """智能模拟分析"""
        if timeline_type == "camera":
            if any(keyword in description for keyword in ["环视", "环绕", "旋转", "全景"]):
                return {
                    "action_type": "camera_panorama",
                    "mcp_function": "camera_panorama_animation",
                    "confidence": 0.85,
                    "parameters": {
                        "camera_name": "Main Camera",
                        "pitch_angle": -15,
                        "duration": 8,
                        "timeline_asset_name": "auto_panorama_timeline"
                    },
                    "reasoning": "基于关键词分析，判断为全景动画",
                    "extracted_objects": ["Main Camera"],
                    "extracted_numbers": {}
                }
            elif any(keyword in description for keyword in ["特写", "聚焦", "近距离", "细节"]):
                return {
                    "action_type": "camera_closeup",
                    "mcp_function": "camera_closeup_animation", 
                    "confidence": 0.82,
                    "parameters": {
                        "camera_name": "Main Camera",
                        "closeup_distance": 5,
                        "pitch_angle": 10,
                        "duration": 6,
                        "timeline_asset_name": "auto_closeup_timeline"
                    },
                    "reasoning": "基于关键词分析，判断为特写动画",
                    "extracted_objects": ["Main Camera"],
                    "extracted_numbers": {}
                }
            else:
                return {
                    "action_type": "camera_move",
                    "mcp_function": "create_multipoint_animation",
                    "confidence": 0.70,
                    "parameters": {
                        "name": "Main Camera",
                        "duration": 5,
                        "path_type": "linear",
                        "timeline_asset_name": "auto_move_timeline"
                    },
                    "reasoning": "无明确特征，默认为移动动画",
                    "extracted_objects": ["Main Camera"],
                    "extracted_numbers": {}
                }
        else:  # object
            if any(keyword in description for keyword in ["移动", "平移", "转移"]):
                return {
                    "action_type": "object_move",
                    "mcp_function": "create_multipoint_animation",
                    "confidence": 0.80,
                    "parameters": {
                        "name": "unknown_object",
                        "duration": 4,
                        "path_type": "linear",
                        "timeline_asset_name": "auto_object_move_timeline"
                    },
                    "reasoning": "基于关键词分析，判断为物体移动",
                    "extracted_objects": [],
                    "extracted_numbers": {}
                }
            elif any(keyword in description for keyword in ["旋转", "转动", "按下", "打开"]):
                return {
                    "action_type": "object_rotate",
                    "mcp_function": "create_multipoint_animation",
                    "confidence": 0.85,
                    "parameters": {
                        "name": "unknown_object",
                        "duration": 3,
                        "include_rotation": True,
                        "timeline_asset_name": "auto_object_rotate_timeline"
                    },
                    "reasoning": "基于关键词分析，判断为旋转动画",
                    "extracted_objects": [],
                    "extracted_numbers": {}
                }
            else:
                return {
                    "action_type": "object_move",
                    "mcp_function": "create_multipoint_animation",
                    "confidence": 0.60,
                    "parameters": {
                        "name": "unknown_object",
                        "duration": 5,
                        "timeline_asset_name": "auto_default_timeline"
                    },
                    "reasoning": "无明确特征，默认为移动动画",
                    "extracted_objects": [],
                    "extracted_numbers": {}
                }
    
    def _calculate_similarity(self, text1: str, text2: str) -> float:
        """计算文本相似度"""
        # 简单的字符匹配相似度
        words1 = set(text1)
        words2 = set(text2)
        
        if not words1 and not words2:
            return 1.0
        if not words1 or not words2:
            return 0.0
            
        intersection = len(words1.intersection(words2))
        union = len(words1.union(words2))
        
        return intersection / union if union > 0 else 0.0
    
    def _parse_ai_response(self, ai_response: str, description: str, timeline_type: str) -> AIActionResult:
        """解析AI响应"""
        try:
            # 提取JSON部分
            json_match = re.search(r'\{.*\}', ai_response, re.DOTALL)
            if json_match:
                response_data = json.loads(json_match.group())
            else:
                response_data = json.loads(ai_response)
            
            return AIActionResult(
                action_type=response_data.get("action_type", "unknown"),
                mcp_function=response_data.get("mcp_function", "unknown"),
                confidence=float(response_data.get("confidence", 0.5)),
                parameters=response_data.get("parameters", {}),
                reasoning=response_data.get("reasoning", "AI分析结果"),
                timeline_type=timeline_type,
                description=description
            )
            
        except Exception as e:
            print(f"解析AI响应失败: {e}")
            return self._create_default_result(timeline_type, description)
    
    def _create_default_result(self, timeline_type: str, description: str) -> AIActionResult:
        """创建默认结果"""
        if timeline_type == "camera":
            return AIActionResult(
                action_type="camera_move",
                mcp_function="create_multipoint_animation",
                confidence=0.3,
                parameters={"name": "Main Camera", "duration": 5},
                reasoning="无法分析，使用默认动作",
                timeline_type=timeline_type,
                description=description
            )
        else:
            return AIActionResult(
                action_type="object_move", 
                mcp_function="create_multipoint_animation",
                confidence=0.3,
                parameters={"name": "unknown_object", "duration": 5},
                reasoning="无法分析，使用默认动作",
                timeline_type=timeline_type,
                description=description
            )
    
    def _fallback_response(self) -> str:
        """API失败时的备用响应"""
        return json.dumps({
            "action_type": "unknown",
            "mcp_function": "create_multipoint_animation",
            "confidence": 0.5,
            "parameters": {"duration": 5},
            "reasoning": "AI API调用失败，使用备用分析",
            "extracted_objects": [],
            "extracted_numbers": {}
        }, ensure_ascii=False)

# 测试和演示功能
def test_ai_classifier():
    """测试AI分类器"""
    print("🤖 AI Timeline动作分类器测试")
    print("=" * 40)
    
    # 创建AI分类器（使用模拟模式）
    classifier = AITimelineClassifier(provider=AIProvider.MOCK)
    
    # 测试用例
    test_cases = [
        ("镜头环视四周", "camera"),
        ("镜头聚焦到电源按钮进行特写", "camera"),
        ("镜头从当前点位平移至紫外可见光分光仪前，环绕旋转一周", "camera"),
        ("电源线平移至插排电源口处并插入", "object"),
        ("按下电源按钮，按钮向下按压", "object"),
        ("紫外可见光分光仪样品室盖子向上旋转90度", "object")
    ]
    
    for description, timeline_type in test_cases:
        print(f"\n📝 测试: {description}")
        print(f"类型: {timeline_type}")
        
        result = classifier.classify_timeline_action(description, timeline_type)
        
        print(f"🎯 AI分析结果:")
        print(f"   动作类型: {result.action_type}")
        print(f"   MCP函数: {result.mcp_function}")
        print(f"   置信度: {result.confidence:.2f}")
        print(f"   推理过程: {result.reasoning}")
        print(f"   参数: {json.dumps(result.parameters, ensure_ascii=False, indent=6)}")
        
        # 置信度评估
        if result.confidence >= 0.8:
            print("   ✅ 高置信度 - 可直接使用")
        elif result.confidence >= 0.6:
            print("   ⚡ 中等置信度 - 建议检查")
        else:
            print("   ⚠️ 低置信度 - 需要人工确认")

if __name__ == "__main__":
    test_ai_classifier()