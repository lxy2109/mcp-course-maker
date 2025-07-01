#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
复合Timeline生成功能测试示例

此文件展示如何使用新增的分离式和组合式timeline生成功能。
"""

def test_separate_timelines_generation():
    """
    测试分离式Timeline生成功能
    """
    print("=== 测试分离式Timeline生成 ===")
    
    # 模拟参数
    test_params = {
        "camera_timeline_name": "TestCameraTimeline",
        "camera_timeline_content": "镜头环视四周，俯视20度，持续10秒",
        "object_timeline_name": "TestObjectTimeline", 
        "object_timeline_content": "立方体向前移动5米并旋转90度",
        "target_object_name": "Cube",
        "camera_name": "Main Camera"
    }
    
    print("输入参数:")
    for key, value in test_params.items():
        print(f"  {key}: {value}")
    
    # 实际使用时，这里会调用MCP函数
    # result = mcp_unityMCP_generate_separate_timelines(**test_params)
    
    print("预期输出: 生成两个独立的timeline文件")
    print("  - TestCameraTimeline.playable (相机环视动画)")
    print("  - TestObjectTimeline.playable (立方体移动旋转)")
    

def test_combined_timeline_generation():
    """
    测试组合式Timeline生成功能
    """
    print("\n=== 测试组合式Timeline生成 ===")
    
    # 模拟参数
    test_params = {
        "timeline_name": "TestCombinedTimeline",
        "camera_timeline_content": "镜头移动到物体前进行特写，距离3米",
        "object_timeline_content": "球体向右移动3米",
        "target_object_name": "Sphere",
        "camera_name": "Main Camera",
        "clip_duration": 5.0
    }
    
    print("输入参数:")
    for key, value in test_params.items():
        print(f"  {key}: {value}")
    
    # 实际使用时，这里会调用MCP函数
    # result = mcp_unityMCP_generate_combined_timeline(**test_params)
    
    print("预期输出: 生成一个包含3个clip的复杂timeline")
    print("  - Clip 1 (0-5s): 相机移动到球体前")
    print("  - Clip 2 (5-10s): 球体向右移动3米")
    print("  - Clip 3 (10-15s): 相机返回初始位置")


def test_natural_language_parsing():
    """
    测试自然语言解析功能
    """
    print("\n=== 测试自然语言解析 ===")
    
    test_descriptions = [
        # 相机动画描述
        "镜头环视四周，俯视20度，持续10秒",
        "相机左右扫视，摆动45度",
        "镜头拉近特写，距离3米",
        "围绕立方体旋转，半径5米，高度2米",
        
        # 物体动画描述
        "立方体向前移动5米",
        "球体旋转90度",
        "圆柱体移动到左侧并旋转180度",
        "胶囊体上下移动，持续3秒"
    ]
    
    print("测试自然语言描述解析:")
    for desc in test_descriptions:
        print(f"  描述: '{desc}'")
        # 实际使用时，这里会调用解析函数
        # params = parse_timeline_description(None, desc, "TestObject", "TargetObject")
        print(f"    -> 预期解析为具体的动画参数")
    

def test_error_handling():
    """
    测试错误处理
    """
    print("\n=== 测试错误处理 ===")
    
    error_cases = [
        {
            "case": "物体不存在",
            "params": {
                "target_object_name": "NonExistentObject",
                "camera_timeline_content": "镜头特写",
                "object_timeline_content": "物体移动"
            }
        },
        {
            "case": "无法解析的描述",
            "params": {
                "target_object_name": "Cube",
                "camera_timeline_content": "随机的无意义文本",
                "object_timeline_content": "另一个无法解析的描述"
            }
        },
        {
            "case": "缺少必要参数",
            "params": {
                "camera_timeline_content": "镜头环视",
                # 缺少 target_object_name
            }
        }
    ]
    
    print("错误处理测试用例:")
    for case in error_cases:
        print(f"  案例: {case['case']}")
        print(f"    参数: {case['params']}")
        print(f"    预期: 返回详细的错误信息和建议")


def main():
    """
    主测试函数
    """
    print("复合Timeline生成功能测试")
    print("=" * 50)
    
    test_separate_timelines_generation()
    test_combined_timeline_generation()
    test_natural_language_parsing()
    test_error_handling()
    
    print("\n" + "=" * 50)
    print("测试完成")
    print("\n使用说明:")
    print("1. 确保Unity项目已加载并运行UnityMCP桥接")
    print("2. 场景中需要存在测试物体 (Cube, Sphere 等)")
    print("3. 通过MCP调用相应的函数进行实际测试")


if __name__ == "__main__":
    main() 