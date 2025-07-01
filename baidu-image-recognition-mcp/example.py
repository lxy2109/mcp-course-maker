#!/usr/bin/env python3
"""
百度图像识别 MCP 服务使用示例
"""

import os
import asyncio
from dotenv import load_dotenv
import sys

# 加载环境变量
load_dotenv()

# 导入server模块
sys.path.append(os.path.dirname(__file__))
from server import baidu_image_recognition, get_recognition_types, BaiduImageRecognitionParams

async def show_recognition_types():
    """显示支持的识别类型"""
    print("🔍 支持的图像识别类型")
    print("=" * 60)
    
    try:
        result = await get_recognition_types()
        
        if result.get("success"):
            types_list = result.get("recognition_types", [])
            print(f"共支持 {len(types_list)} 种识别类型:\n")
            
            for i, type_info in enumerate(types_list, 1):
                type_code = type_info.get("type")
                name = type_info.get("name")
                description = type_info.get("description")
                print(f"{i:2d}. {name} ({type_code})")
                print(f"    {description}\n")
        else:
            print("❌ 获取识别类型失败")
    except Exception as e:
        print(f"❌ 错误: {e}")

async def example_general_recognition():
    """示例1: 通用物体识别"""
    print("📸 示例1: 通用物体识别")
    print("-" * 40)
    
    # 使用一个公开的测试图片
    test_url = "https://httpbin.org/image/jpeg"
    
    try:
        params = BaiduImageRecognitionParams(
            image_path=test_url,
            recognition_type="general_basic",
            top_num=5,
            baike_num=2
        )
        
        result = await baidu_image_recognition(params)
        
        print(f"识别类型: {result.recognition_type}")
        print(f"图片来源: {test_url}")
        print(f"识别状态: {'✅ 成功' if result.success else '❌ 失败'}")
        
        if result.success and result.data:
            results = result.data.get("result", [])
            print(f"识别结果数量: {len(results)}")
            
            for i, item in enumerate(results[:3], 1):
                keyword = item.get("keyword", "N/A")
                score = item.get("score", 0)
                root = item.get("root", "N/A")
                print(f"  {i}. {keyword} (分类: {root}, 置信度: {score:.3f})")
        else:
            print(f"错误信息: {result.error}")
    
    except Exception as e:
        print(f"❌ 示例执行失败: {e}")

async def example_animal_recognition():
    """示例2: 动物识别"""
    print("\n🐾 示例2: 动物识别")
    print("-" * 40)
    
    # 使用动物图片URL（如果有的话）
    test_url = "https://httpbin.org/image/jpeg"
    
    try:
        params = BaiduImageRecognitionParams(
            image_path=test_url,
            recognition_type="animal",
            top_num=3,
            baike_num=1
        )
        
        result = await baidu_image_recognition(params)
        
        print(f"识别类型: {result.recognition_type}")
        print(f"识别状态: {'✅ 成功' if result.success else '❌ 失败'}")
        
        if result.success and result.data:
            results = result.data.get("result", [])
            print(f"识别结果数量: {len(results)}")
            
            for i, item in enumerate(results, 1):
                name = item.get("name", item.get("keyword", "N/A"))
                score = item.get("score", 0)
                print(f"  {i}. {name} (置信度: {score:.3f})")
                
                # 如果有百科信息
                if "baike_info" in item:
                    baike = item["baike_info"]
                    print(f"     百科: {baike.get('description', 'N/A')[:50]}...")
        else:
            print(f"错误信息: {result.error}")
    
    except Exception as e:
        print(f"❌ 示例执行失败: {e}")

async def example_plant_recognition():
    """示例3: 植物识别"""
    print("\n🌿 示例3: 植物识别")
    print("-" * 40)
    
    test_url = "https://httpbin.org/image/jpeg"
    
    try:
        params = BaiduImageRecognitionParams(
            image_path=test_url,
            recognition_type="plant",
            top_num=3,
            baike_num=1
        )
        
        result = await baidu_image_recognition(params)
        
        print(f"识别类型: {result.recognition_type}")
        print(f"识别状态: {'✅ 成功' if result.success else '❌ 失败'}")
        
        if result.success and result.data:
            results = result.data.get("result", [])
            print(f"识别结果数量: {len(results)}")
            
            for i, item in enumerate(results, 1):
                name = item.get("name", item.get("keyword", "N/A"))
                score = item.get("score", 0)
                print(f"  {i}. {name} (置信度: {score:.3f})")
        else:
            print(f"错误信息: {result.error}")
    
    except Exception as e:
        print(f"❌ 示例执行失败: {e}")

async def example_logo_recognition():
    """示例4: Logo识别"""
    print("\n🏷️ 示例4: Logo识别")
    print("-" * 40)
    
    test_url = "https://httpbin.org/image/jpeg"
    
    try:
        params = BaiduImageRecognitionParams(
            image_path=test_url,
            recognition_type="logo",
            top_num=5
        )
        
        result = await baidu_image_recognition(params)
        
        print(f"识别类型: {result.recognition_type}")
        print(f"识别状态: {'✅ 成功' if result.success else '❌ 失败'}")
        
        if result.success and result.data:
            results = result.data.get("result", [])
            print(f"识别结果数量: {len(results)}")
            
            for i, item in enumerate(results, 1):
                name = item.get("name", item.get("keyword", "N/A"))
                score = item.get("probability", item.get("score", 0))
                print(f"  {i}. {name} (置信度: {score:.3f})")
        else:
            print(f"错误信息: {result.error}")
    
    except Exception as e:
        print(f"❌ 示例执行失败: {e}")

async def example_dish_recognition():
    """示例5: 菜品识别"""
    print("\n🍽️ 示例5: 菜品识别")
    print("-" * 40)
    
    test_url = "https://httpbin.org/image/jpeg"
    
    try:
        params = BaiduImageRecognitionParams(
            image_path=test_url,
            recognition_type="dish",
            top_num=3
        )
        
        result = await baidu_image_recognition(params)
        
        print(f"识别类型: {result.recognition_type}")
        print(f"识别状态: {'✅ 成功' if result.success else '❌ 失败'}")
        
        if result.success and result.data:
            results = result.data.get("result", [])
            print(f"识别结果数量: {len(results)}")
            
            for i, item in enumerate(results, 1):
                name = item.get("name", item.get("keyword", "N/A"))
                score = item.get("probability", item.get("score", 0))
                calorie = item.get("calorie", "N/A")
                print(f"  {i}. {name} (置信度: {score:.3f}, 卡路里: {calorie})")
        else:
            print(f"错误信息: {result.error}")
    
    except Exception as e:
        print(f"❌ 示例执行失败: {e}")

async def main():
    """主函数"""
    print("🚀 百度图像识别 MCP 服务使用示例")
    print("=" * 60)
    
    # 检查API密钥配置
    if not os.getenv("BAIDU_API_KEY") or not os.getenv("BAIDU_SECRET_KEY"):
        print("❌ 错误: 请先配置百度API密钥")
        print("1. 复制 env.example 为 .env")
        print("2. 在 .env 文件中填入你的API密钥")
        return
    
    try:
        # 显示识别类型
        await show_recognition_types()
        
        print("\n📋 运行示例")
        print("=" * 60)
        
        # 运行各种识别示例
        examples = [
            example_general_recognition,
            example_animal_recognition,
            example_plant_recognition,
            example_logo_recognition,
            example_dish_recognition,
        ]
        
        for example_func in examples:
            try:
                await example_func()
            except Exception as e:
                print(f"❌ 示例执行失败: {e}")
        
        print("\n" + "=" * 60)
        print("✅ 所有示例运行完成！")
        
        print("\n💡 提示:")
        print("1. 以上示例使用的是测试URL，可能无法获得真实识别结果")
        print("2. 要获得真实结果，请使用实际的图片URL或本地文件路径")
        print("4. 可以查看 test_server.py 了解如何编写自己的测试")
        
    except Exception as e:
        print(f"❌ 程序执行失败: {e}")

if __name__ == "__main__":
    asyncio.run(main()) 