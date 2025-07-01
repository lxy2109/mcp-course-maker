"""
axis_change.py
坐标轴归一化相关工具函数。
"""

import tempfile, time, os, subprocess, psutil, datetime
from .blender_scripts import get_axis_convert_script_with_done
from .blender_utils import get_blender_executable_with_fallback

def normalize_model_axis_with_blender(input_model_path: str, output_model_path: str, log_dir: str = None, timeout: int = 180) -> bool:
    """
    使用Blender将模型坐标轴从Z-up转换为Y-up（Unity标准）。
    Args:
        input_model_path (str): 输入模型路径（.glb/.obj）
        output_model_path (str): 输出归一化后的模型路径
        log_dir (str): 日志保存目录（可选）
        timeout (int): 最大等待时间（秒）
    Returns:
        bool: 是否归一化成功
    """
    import tempfile, time, os, subprocess, psutil, datetime

    axis_norm_script = get_axis_convert_script_with_done(input_model_path, output_model_path, output_model_path + ".done")
    script_path = tempfile.mktemp(suffix=".py")
    done_flag_path = output_model_path + ".done"
    with open(script_path, 'w', encoding='utf-8') as f:
        f.write(axis_norm_script)
    blender_exe = get_blender_executable_with_fallback()
    if not blender_exe:
        raise RuntimeError("Blender executable not found for axis normalization!")
    timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    log_file = os.path.join(log_dir or os.path.dirname(output_model_path), f"axis_norm_{timestamp}.log")
    cmd = f'start "" "{blender_exe}" --background --python "{script_path}"'
    with open(log_file, "w", encoding="utf-8") as logf:
        logf.write(f"Axis normalization: {cmd}\n")
        logf.write(f"Done flag: {done_flag_path}\n")
        logf.write(f"Output: {output_model_path}\n")
    subprocess.Popen(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    waited_time = 0
    completed = False
    while waited_time < timeout:
        done_flag_exists = os.path.exists(done_flag_path)
        norm_exists = os.path.exists(output_model_path)
        norm_size = os.path.getsize(output_model_path) if norm_exists else 0
        if waited_time % 10 == 0:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Wait {waited_time}s: done={done_flag_exists}, norm={norm_exists}, size={norm_size}\n")
        if done_flag_exists and norm_exists and norm_size > 0:
            completed = True
            break
        elif norm_exists and norm_size > 0 and waited_time > 30:
            time.sleep(2)
            new_size = os.path.getsize(output_model_path) if os.path.exists(output_model_path) else 0
            if new_size == norm_size and norm_size > 0:
                completed = True
                break
        time.sleep(1)
        waited_time += 1
    if not completed:
        with open(log_file, "a", encoding="utf-8") as logf:
            logf.write(f"Timeout after {timeout}s\n")
            logf.write("Attempting to terminate Blender...\n")
        try:
            for p in psutil.process_iter(['name', 'exe', 'cmdline']):
                try:
                    if p.info['name'] and 'blender' in p.info['name'].lower():
                        if p.info['cmdline'] and script_path in ' '.join(p.info['cmdline']):
                            p.terminate()
                            with open(log_file, "a", encoding="utf-8") as logf:
                                logf.write(f"Terminated Blender: {p.pid}\n")
                except Exception:
                    continue
        except Exception as e:
            with open(log_file, "a", encoding="utf-8") as logf:
                logf.write(f"Failed to terminate Blender: {e}\n")
    # 清理临时文件
    try:
        if os.path.exists(script_path):
            os.remove(script_path)
    except Exception:
        pass
    try:
        if os.path.exists(done_flag_path):
            os.remove(done_flag_path)
    except Exception:
        pass
    return completed and os.path.exists(output_model_path) and os.path.getsize(output_model_path) > 0
