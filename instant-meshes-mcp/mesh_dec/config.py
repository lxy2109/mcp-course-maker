import os

INSTANT_MESHES_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Instant Meshes.exe")

ARCHIVE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "archives")
os.makedirs(ARCHIVE_DIR, exist_ok=True)

TEMP_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "temp")
os.makedirs(TEMP_DIR, exist_ok=True)

OUTPUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "output_remesh")
os.makedirs(OUTPUT_DIR, exist_ok=True)

LOG_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "logs")
os.makedirs(LOG_DIR, exist_ok=True)

# Instant Meshes配置 - 性能优化版本
INSTANT_MESHES_CONFIG = {
    # 核心配置
    "default_target_faces": 3000,  # 降低默认目标面数，提升速度
    "max_target_faces": 20000,     # 限制最大面数，避免超长处理时间
    "min_target_faces": 500,       # 最小面数限制
    
    # 超时配置 - 大幅缩短
    "timeout_seconds": 120,        # 全局超时：2分钟
    "progressive_timeout": 90,     # 渐进式简化超时：1.5分钟
    "instant_meshes_timeout": 60,  # Instant Meshes超时：1分钟
    
    # 性能优化选项
    "enable_fast_mode": True,      # 启用快速模式
    "skip_repair_for_small": True, # 小模型跳过修复
    "early_termination": True,     # 启用早期终止
    "max_file_size_mb": 50,        # 最大处理文件大小（MB）
    
    # 减面策略优化
    "progressive_steps": {
        "max_steps": 3,            # 最大步数：从10减少到3
        "reduction_per_step": 0.65, # 每步减面比例：从0.6提升到0.65
        "min_reduction_threshold": 0.1  # 最小减面阈值：10%
    },
    
    # 质量vs速度权衡
    "quality_vs_speed": "speed",   # 优先速度而非质量
    "preserve_topology": False,    # 放宽拓扑保持，提升速度
    "auto_clean": False,           # 关闭自动清理，减少处理时间
    
    # 并行处理
    "enable_parallel": True,       # 启用并行处理（如果支持）
    "max_threads": 4,              # 最大线程数
    
    # 缓存优化
    "enable_cache": True,          # 启用结果缓存
    "cache_size_mb": 500,          # 缓存大小：500MB
    "cache_expiry_hours": 24,      # 缓存过期时间：24小时
}
