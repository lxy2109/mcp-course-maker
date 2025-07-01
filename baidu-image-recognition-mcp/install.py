#!/usr/bin/env python3
"""
百度图像识别 MCP 服务自动安装脚本
"""

import os
import sys
import subprocess
from pathlib import Path

def check_python_version():
    """检查Python版本"""
    if sys.version_info < (3, 8):
        print("❌ 错误: 需要Python 3.8或更高版本")
        print(f"当前版本: Python {sys.version}")
        return False
    print(f"✅ Python版本检查通过: {sys.version.split()[0]}")
    return True

def check_pip():
    """检查pip是否可用"""
    try:
        import pip
        print("✅ pip已安装")
        return True
    except ImportError:
        print("❌ 错误: pip未安装")
        return False

def install_requirements():
    """安装依赖包"""
    print("\n📦 安装依赖包...")
    requirements_file = Path(__file__).parent / "requirements.txt"
    
    if not requirements_file.exists():
        print("❌ 错误: requirements.txt文件不存在")
        return False
    
    try:
        # 使用pip安装依赖
        result = subprocess.run([
            sys.executable, "-m", "pip", "install", "-r", str(requirements_file)
        ], capture_output=True, text=True, check=True)
        
        print("✅ 依赖包安装成功")
        return True
        
    except subprocess.CalledProcessError as e:
        print(f"❌ 安装依赖包失败: {e}")
        print(f"错误输出: {e.stderr}")
        return False

def test_imports():
    """测试关键模块导入"""
    print("\n🧪 测试模块导入...")
    
    modules = [
        "httpx",
        "dotenv", 
        "pydantic",
        "requests",
        "fastmcp",
        "mcp"
    ]
    
    failed_modules = []
    for module in modules:
        try:
            if module == "dotenv":
                from dotenv import load_dotenv
            else:
                __import__(module)
            print(f"✅ {module}")
        except ImportError:
            print(f"❌ {module}")
            failed_modules.append(module)
    
    if failed_modules:
        print(f"\n❌ 模块导入失败: {', '.join(failed_modules)}")
        return False
    
    print("✅ 所有模块导入成功")
    return True

def main():
    """主安装函数"""
    print("🚀 百度图像识别 MCP 服务安装程序")
    print("=" * 50)
    
    # 检查Python版本
    if not check_python_version():
        sys.exit(1)
    
    # 检查pip
    if not check_pip():
        sys.exit(1)
    
    # 安装依赖
    if not install_requirements():
        sys.exit(1)
    
    # 测试模块导入
    if not test_imports():
        print("\n❌ 安装验证失败，请检查依赖安装")
        sys.exit(1)
    
    print("\n" + "=" * 50)
    print("🎉 安装完成!")
    print("\n下一步:")
    print("1. 运行服务: python server.py")
    print("\n如需获取百度API密钥，请访问:")
    print("https://console.bce.baidu.com/ai/#/ai/imagerecognition/overview/index")

if __name__ == "__main__":
    main() 