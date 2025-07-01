"""
blender_utils.py
Blender相关检测、调用、脚本生成、版本校验等工具函数。
"""
# ... 迁移自 server.py ...
# find_blender_executable, verify_blender_version, get_blender_executable_with_fallback, run_blender_with_start, test_blender_detection

import os
import platform
import subprocess
import time
from typing import Any, Dict, Optional
import signal

def find_blender_executable() -> Optional[str]:
    """
    自动检索设备中的Blender 3.6可执行文件。
    支持Windows、macOS和Linux系统，检查多个常见安装位置。
    Returns:
        str: Blender 3.6可执行文件路径，如果未找到则返回None
    """
    system = platform.system().lower()
    
    # 定义不同系统的可能路径
    if system == "windows":
        # Windows系统的可能路径
        possible_paths = [
            # 用户自定义路径（从环境变量或注册表）
            os.getenv("BLENDER_PATH"),
            
            # 标准安装路径
            r"C:\Program Files\Blender Foundation\Blender 3.6\blender.exe",
            r"C:\Program Files (x86)\Blender Foundation\Blender 3.6\blender.exe",
            
            # 其他常见驱动器
            r"D:\Program Files\Blender Foundation\Blender 3.6\blender.exe",
            r"E:\Program Files\Blender Foundation\Blender 3.6\blender.exe",
            r"F:\Program Files\Blender Foundation\Blender 3.6\blender.exe",
            
            # 便携版路径
            r"C:\Blender\3.6\blender.exe",
            r"D:\Blender\3.6\blender.exe",
            r"E:\Blender\3.6\blender.exe",
            r"H:\Blender\3.6\blender.exe",  # 用户原有路径
            
            # Steam安装路径
            r"C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe",
            r"C:\Program Files\Steam\steamapps\common\Blender\blender.exe",
            
            # 用户目录安装
            os.path.expanduser(r"~\AppData\Local\Programs\Blender Foundation\Blender 3.6\blender.exe"),
            os.path.expanduser(r"~\Documents\Blender\3.6\blender.exe"),
            
            # 当前目录相对路径
            r".\blender\blender.exe",
            r".\Blender 3.6\blender.exe",
        ]
        
        # 尝试从PATH环境变量中查找
        path_env = os.environ.get("PATH", "")
        for path_dir in path_env.split(os.pathsep):
            if "blender" in path_dir.lower():
                blender_exe = os.path.join(path_dir, "blender.exe")
                if os.path.exists(blender_exe):
                    possible_paths.append(blender_exe)
        
        # 尝试从注册表查找（Windows特有）
        try:
            import winreg
            # 查找Blender在注册表中的安装路径
            registry_paths = [
                (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Blender Foundation\Blender\3.6"),
                (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Blender Foundation\Blender\3.6"),
                (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Blender Foundation\Blender\3.6"),
            ]
            
            for hkey, subkey in registry_paths:
                try:
                    with winreg.OpenKey(hkey, subkey) as key:
                        install_path = winreg.QueryValueEx(key, "InstallDir")[0]
                        blender_exe = os.path.join(install_path, "blender.exe")
                        if os.path.exists(blender_exe):
                            possible_paths.append(blender_exe)
                except (FileNotFoundError, OSError):
                    continue
        except ImportError:
            pass  # winreg不可用（非Windows系统）
            
    elif system == "darwin":  # macOS
        possible_paths = [
            # 标准应用程序路径
            "/Applications/Blender.app/Contents/MacOS/Blender",
            "/Applications/Blender 3.6/Blender.app/Contents/MacOS/Blender",
            
            # 用户应用程序路径
            os.path.expanduser("~/Applications/Blender.app/Contents/MacOS/Blender"),
            os.path.expanduser("~/Applications/Blender 3.6/Blender.app/Contents/MacOS/Blender"),
            
            # Homebrew路径
            "/usr/local/bin/blender",
            "/opt/homebrew/bin/blender",
            
            # MacPorts路径
            "/opt/local/bin/blender",
            
            # 其他可能路径
            "/usr/bin/blender",
            "/usr/local/Cellar/blender/*/bin/blender",
        ]
        
    else:  # Linux和其他Unix系统
        possible_paths = [
            # 标准系统路径
            "/usr/bin/blender",
            "/usr/local/bin/blender",
            "/opt/blender/blender",
            "/opt/blender-3.6/blender",
            
            # Snap包路径
            "/snap/blender/current/blender",
            
            # Flatpak路径
            "/var/lib/flatpak/app/org.blender.Blender/current/active/files/blender",
            os.path.expanduser("~/.local/share/flatpak/app/org.blender.Blender/current/active/files/blender"),
            
            # AppImage路径
            os.path.expanduser("~/Applications/Blender-3.6-linux-x64.AppImage"),
            os.path.expanduser("~/Downloads/Blender-3.6-linux-x64.AppImage"),
            
            # 用户本地安装
            os.path.expanduser("~/blender/blender"),
            os.path.expanduser("~/blender-3.6/blender"),
            os.path.expanduser("~/.local/bin/blender"),
            
            # 其他常见路径
            "/home/blender/blender",
            "/opt/blender-foundation/blender-3.6/blender",
        ]
        
        # 从PATH环境变量查找
        import shutil
        path_blender = shutil.which("blender")
        if path_blender:
            possible_paths.insert(0, path_blender)
    
    # 移除None值和重复路径
    possible_paths = list(dict.fromkeys([p for p in possible_paths if p is not None]))
    
    # 逐一检查路径
    for path in possible_paths:
        try:
            # 展开通配符路径（如/usr/local/Cellar/blender/*/bin/blender）
            if '*' in path:
                import glob
                expanded_paths = glob.glob(path)
                for expanded_path in expanded_paths:
                    if os.path.exists(expanded_path) and os.access(expanded_path, os.X_OK):
                        # 验证版本
                        if verify_blender_version(expanded_path):
                            return expanded_path
            else:
                if os.path.exists(path) and os.access(path, os.X_OK):
                    # 验证版本
                    if verify_blender_version(path):
                        return path
        except Exception:
            continue
    
    return None

def verify_blender_version(blender_path: str, required_version: str = "3.6") -> bool:
    """
    验证Blender可执行文件的版本。
    Args:
        blender_path (str): Blender可执行文件路径
        required_version (str): 要求的版本号
    Returns:
        bool: 是否为要求的版本
    """
    try:
        result = subprocess.run(
            [blender_path, "--version"], 
            capture_output=True, 
            text=True, 
            timeout=15,
            creationflags=subprocess.CREATE_NO_WINDOW if platform.system() == "Windows" else 0
        )
        
        if result.returncode == 0:
            version_output = result.stdout.lower()
            # 检查版本号（支持3.6.x格式）
            if required_version in version_output or f"blender {required_version}" in version_output:
                return True
            
            # 更精确的版本检查
            import re
            version_pattern = r"blender\s+(\d+\.\d+)"
            match = re.search(version_pattern, version_output)
            if match:
                found_version = match.group(1)
                return found_version.startswith(required_version)
                
        return False
    except Exception:
        return False

def get_blender_executable_with_fallback() -> Optional[str]:
    """
    获取Blender可执行文件路径，包含回退机制。
    支持以下检测方式（按优先级）：
    1. BLENDER_EXECUTABLE 环境变量
    2. BLENDER_PATH 环境变量
    3. 自动检测系统中的Blender 3.6
    4. 通用命令检测
    
    Returns:
        str: Blender可执行文件路径，如果未找到则返回None
    """
    # 1. 优先检查 BLENDER_EXECUTABLE 环境变量
    blender_env = os.environ.get("BLENDER_EXECUTABLE")
    if blender_env and os.path.exists(blender_env) and verify_blender_version(blender_env):
        return blender_env
    
    # 2. 检查 BLENDER_PATH 环境变量（兼容性）
    blender_path_env = os.environ.get("BLENDER_PATH")
    if blender_path_env and os.path.exists(blender_path_env) and verify_blender_version(blender_path_env):
        return blender_path_env
    
    # 3. 尝试自动检测
    blender_exe = find_blender_executable()
    
    if blender_exe:
        return blender_exe
    
    # 4. 如果自动检测失败，尝试一些通用命令
    fallback_commands = ["blender", "blender3.6", "blender-3.6"]
    
    for cmd in fallback_commands:
        try:
            import shutil
            path = shutil.which(cmd)
            if path and verify_blender_version(path):
                return path
        except Exception:
            continue
    
    return None

def test_blender_detection() -> Dict[str, Any]:
    """
    测试Blender检测功能，返回详细的检测结果。
    Returns:
        Dict[str, Any]: 包含检测结果的字典
    """
    result = {
        "system": platform.system(),
        "architecture": platform.architecture(),
        "environment_variables": {
            "BLENDER_EXECUTABLE": os.environ.get("BLENDER_EXECUTABLE"),
            "BLENDER_PATH": os.environ.get("BLENDER_PATH"),
            "PATH": os.environ.get("PATH", "").split(os.pathsep)[:5]  # 只显示前5个PATH条目
        },
        "detection_results": {},
        "final_result": None,
        "error": None
    }
    
    try:
        # 测试环境变量检测
        blender_env = os.environ.get("BLENDER_EXECUTABLE")
        if blender_env:
            result["detection_results"]["env_BLENDER_EXECUTABLE"] = {
                "path": blender_env,
                "exists": os.path.exists(blender_env),
                "version_valid": verify_blender_version(blender_env) if os.path.exists(blender_env) else False
            }
        
        blender_path_env = os.environ.get("BLENDER_PATH")
        if blender_path_env:
            result["detection_results"]["env_BLENDER_PATH"] = {
                "path": blender_path_env,
                "exists": os.path.exists(blender_path_env),
                "version_valid": verify_blender_version(blender_path_env) if os.path.exists(blender_path_env) else False
            }
        
        # 测试自动检测
        auto_detected = find_blender_executable()
        if auto_detected:
            result["detection_results"]["auto_detection"] = {
                "path": auto_detected,
                "exists": os.path.exists(auto_detected),
                "version_valid": verify_blender_version(auto_detected)
            }
        
        # 测试通用命令
        fallback_commands = ["blender", "blender3.6", "blender-3.6"]
        for cmd in fallback_commands:
            try:
                import shutil
                path = shutil.which(cmd)
                if path:
                    result["detection_results"][f"command_{cmd}"] = {
                        "path": path,
                        "exists": os.path.exists(path),
                        "version_valid": verify_blender_version(path)
                    }
            except Exception as e:
                result["detection_results"][f"command_{cmd}"] = {
                    "error": str(e)
                }
        
        # 获取最终结果
        final_blender = get_blender_executable_with_fallback()
        result["final_result"] = {
            "path": final_blender,
            "success": final_blender is not None
        }
        
        if final_blender:
            # 获取版本信息
            try:
                version_result = subprocess.run(
                    [final_blender, "--version"], 
                    capture_output=True, 
                    text=True, 
                    timeout=10,
                    creationflags=subprocess.CREATE_NO_WINDOW if platform.system() == "Windows" else 0
                )
                if version_result.returncode == 0:
                    result["final_result"]["version_output"] = version_result.stdout.strip()
            except Exception as e:
                result["final_result"]["version_error"] = str(e)
        
    except Exception as e:
        result["error"] = str(e)
    
    return result

def run_blender_with_start(blender_exe: str, script_path: str, done_flag_path: str, timeout: int = 120) -> bool:
    """
    使用Windows start命令启动Blender，确保独立运行。
    Args:
        blender_exe (str): Blender可执行文件路径
        script_path (str): Blender脚本文件路径
        done_flag_path (str): 完成标记文件路径
        timeout (int): 超时时间（秒）
    Returns:
        bool: 是否成功完成
    """
    try:
        # 构建命令
        if platform.system() == "Windows":
            # Windows使用start命令独立启动
            cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
        else:
            # Unix系统使用nohup或直接启动
            cmd = f'nohup "{blender_exe}" --background --python "{script_path}" > /dev/null 2>&1 &'
        
        # 启动Blender进程
        subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        
        # 等待完成标记文件
        start_time = time.time()
        while time.time() - start_time < timeout:
            if os.path.exists(done_flag_path):
                return True
            time.sleep(1)
        
        return False
        
    except Exception as e:
        print(f"Failed to run Blender: {e}")
        return False

def run_blender_with_done(script_content: str, output_path: str, done_flag_path: str, max_wait_time: int = 180) -> bool:
    """
    通用Blender独立进程+done标记+超时+进程清理执行函数。
    Args:
        script_content (str): Blender脚本内容
        output_path (str): 期望输出文件路径
        done_flag_path (str): 完成标记文件路径
        max_wait_time (int): 最大等待时间（秒）
    Returns:
        bool: 是否成功完成
    """
    import platform
    from .file_utils import get_temp_file
    script_path = get_temp_file('.py')
    with open(script_path, 'w', encoding='utf-8') as f:
        f.write(script_content)
    blender_exe = get_blender_executable_with_fallback()
    if not blender_exe:
        raise RuntimeError("Blender executable not found!")
    
    # 跨平台命令构建
    if platform.system() == "Windows":
        cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
    else:
        cmd = f'nohup "{blender_exe}" --background --python "{script_path}" >/dev/null 2>&1 &'
    
    # 启动独立进程
    import subprocess
    try:
        if platform.system() == "Windows":
            subprocess.run(cmd, shell=True, check=False)
        else:
            subprocess.Popen(cmd, shell=True)
    except Exception:
        return False
    
    # 等待done标记或超时
    import time
    start_time = time.time()
    while time.time() - start_time < max_wait_time:
        if os.path.exists(done_flag_path):
            # 检查输出文件是否存在且有效
            if os.path.exists(output_path) and os.path.getsize(output_path) > 0:
                # 清理临时文件
                for temp_file in [script_path, done_flag_path]:
                    if os.path.exists(temp_file):
                        try:
                            os.remove(temp_file)
                        except Exception:
                            pass
                return True
            else:
                break
        time.sleep(1)
    
    # 超时或失败，强制清理进程
    try:
        import psutil
        for proc in psutil.process_iter(['pid', 'name', 'cmdline']):
            try:
                if proc.info['name'] and 'blender' in proc.info['name'].lower():
                    if proc.info['cmdline'] and script_path in ' '.join(proc.info['cmdline']):
                        proc.terminate()
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
    except ImportError:
        pass  # psutil不可用时跳过
    
    # 清理临时文件
    for temp_file in [script_path, done_flag_path]:
        if os.path.exists(temp_file):
            try:
                os.remove(temp_file)
            except Exception:
                pass
    
    return False