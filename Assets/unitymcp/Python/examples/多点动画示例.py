from unitymcp import UnityMCP
import time

# 初始化MCP连接
mcp = UnityMCP()

# 创建一个简单的多点动画示例，让物体沿着弧线路径移动
def create_arc_animation():
    result = mcp.tools.create_multipoint_animation(
        name="MovingCube",  # 要移动的物体名称
        points=[
            {"position": {"x": 0, "y": 0, "z": 0}},  # 起点
            {"position": {"x": 2, "y": 3, "z": 2}},  # 中间高点
            {"position": {"x": 4, "y": 0, "z": 0}}   # 终点
        ],
        duration=3.0,
        timeline_asset_name="ArcMovement",
        path_type="bezier"  # 使用贝塞尔曲线插值
    )
    
    print(f"创建弧线动画结果: {result}")

# 创建Z字形多点路径动画
def create_zigzag_animation():
    result = mcp.tools.create_multipoint_animation(
        name="MovingCube",
        points=[
            {"position": {"x": -4, "y": 0, "z": 0}, "time": 0.0},
            {"position": {"x": -2, "y": 0, "z": 2}, "time": 1.0},
            {"position": {"x": 0, "y": 0, "z": 0}, "time": 2.0},
            {"position": {"x": 2, "y": 0, "z": 2}, "time": 3.0},
            {"position": {"x": 4, "y": 0, "z": 0}, "time": 4.0}
        ],
        duration=4.0,
        timeline_asset_name="ZigzagMovement",
        path_type="linear"  # 使用线性插值
    )
    
    print(f"创建Z字形动画结果: {result}")

# 创建圆形路径动画
def create_circle_animation():
    import math
    
    # 生成圆形路径的点
    circle_points = []
    radius = 3
    num_points = 8
    
    for i in range(num_points + 1):
        angle = 2 * math.pi * i / num_points
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        
        circle_points.append({
            "position": {"x": x, "y": 0, "z": z}
        })
    
    result = mcp.tools.create_multipoint_animation(
        name="MovingCube",
        points=circle_points,
        duration=5.0,
        timeline_asset_name="CircleMovement",
        path_type="curve"  # 使用曲线插值
    )
    
    print(f"创建圆形动画结果: {result}")

# 创建带旋转的动画
def create_animation_with_rotation():
    result = mcp.tools.create_multipoint_animation(
        name="MovingCube",
        points=[
            {
                "position": {"x": 0, "y": 0, "z": 0},
                "rotation": {"x": 0, "y": 0, "z": 0},
                "time": 0.0
            },
            {
                "position": {"x": 2, "y": 1, "z": 2},
                "rotation": {"x": 0, "y": 90, "z": 0},
                "time": 1.5
            },
            {
                "position": {"x": 4, "y": 0, "z": 0},
                "rotation": {"x": 0, "y": 180, "z": 0},
                "time": 3.0
            }
        ],
        duration=3.0,
        timeline_asset_name="MoveAndRotate",
        include_rotation=True,
        path_type="bezier"
    )
    
    print(f"创建带旋转的动画结果: {result}")

# 创建从桌子到地板的弧线动画
def create_desk_to_floor_animation():
    result = mcp.tools.create_multipoint_animation(
        name="hammer",  # 假设场景中有名为"hammer"的物体
        points=[
            {"position": {"x": 0.5, "y": 1.0, "z": 0.5}},  # 桌子上的位置
            {"position": {"x": 1.0, "y": 1.5, "z": 0.0}},  # 中间高点
            {"position": {"x": 2.0, "y": 0.0, "z": -1.0}}  # 地板上的位置
        ],
        duration=2.5,
        timeline_asset_name="HammerDrop",
        path_type="bezier"
    )
    
    print(f"创建桌子到地板的动画结果: {result}")

# 运行示例
if __name__ == "__main__":
    print("启动多点动画示例...")
    
    # 选择要运行的示例
    # create_arc_animation()
    # create_zigzag_animation()
    # create_circle_animation()
    # create_animation_with_rotation()
    create_desk_to_floor_animation()
    
    print("示例完成!") 