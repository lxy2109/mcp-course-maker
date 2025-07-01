from unitymcp import UnityMCP
import time

# 初始化MCP连接
mcp = UnityMCP()

def create_desk_to_table_animation():
    """创建一个物体从书桌到桌子的动画"""
    print("正在创建从书桌到桌子的动画...")
    
    # 为了更好的视觉效果，我们将创建一个弧线路径的动画
    # 物体将先上升，然后在空中移动，最后降落到目标位置
    
    result = mcp.tools.create_multipoint_animation(
        name="hammer",  # 假设场景中有名为"hammer"的物体
        points=[
            # 起点（书桌上）
            {"position": {"x": 0.5, "y": 1.0, "z": 0.5}},
            
            # 中间点1（上升）
            {"position": {"x": 0.5, "y": 2.0, "z": 0.5}},
            
            # 中间点2（空中）
            {"position": {"x": 2.0, "y": 2.5, "z": -1.0}},
            
            # 终点（桌子上）
            {"position": {"x": 3.5, "y": 1.0, "z": -2.5}}
        ],
        duration=4.0,
        timeline_asset_name="DeskToTableAnimation",
        path_type="bezier"  # 使用贝塞尔曲线获得平滑的弧线
    )
    
    print(f"创建动画结果: {result}")
    return result

def create_desk_to_table_with_rotation():
    """创建一个带旋转的从书桌到桌子的动画"""
    print("正在创建带旋转的从书桌到桌子的动画...")
    
    result = mcp.tools.create_multipoint_animation(
        name="hammer",
        points=[
            # 起点（书桌上）- 初始旋转
            {
                "position": {"x": 0.5, "y": 1.0, "z": 0.5},
                "rotation": {"x": 0, "y": 0, "z": 0},
                "time": 0.0
            },
            
            # 中间点1（上升）- 开始旋转
            {
                "position": {"x": 0.5, "y": 2.0, "z": 0.5},
                "rotation": {"x": 0, "y": 90, "z": 0},
                "time": 1.0
            },
            
            # 中间点2（空中）- 继续旋转
            {
                "position": {"x": 2.0, "y": 2.5, "z": -1.0},
                "rotation": {"x": 0, "y": 180, "z": 0},
                "time": 2.5
            },
            
            # 终点（桌子上）- 完成旋转
            {
                "position": {"x": 3.5, "y": 1.0, "z": -2.5},
                "rotation": {"x": 0, "y": 360, "z": 0},
                "time": 4.0
            }
        ],
        duration=4.0,
        timeline_asset_name="DeskToTableWithRotation",
        include_rotation=True,
        path_type="bezier"
    )
    
    print(f"创建带旋转的动画结果: {result}")
    return result

def get_object_positions():
    """获取场景中书桌和桌子的位置，以便更精确地设置动画路径"""
    
    # 获取desk的位置
    desk_info = mcp.send_command("GET_OBJECT_INFO", {
        "name": "desk"  # 书桌的对象名称
    })
    
    # 获取table的位置
    table_info = mcp.send_command("GET_OBJECT_INFO", {
        "name": "table"  # 桌子的对象名称
    })
    
    # 如果成功获取到位置信息，则打印出来
    if desk_info and "position" in desk_info:
        desk_pos = desk_info["position"]
        print(f"书桌位置: X={desk_pos[0]}, Y={desk_pos[1]}, Z={desk_pos[2]}")
    else:
        print("无法获取书桌位置")
        
    if table_info and "position" in table_info:
        table_pos = table_info["position"]
        print(f"桌子位置: X={table_pos[0]}, Y={table_pos[1]}, Z={table_pos[2]}")
    else:
        print("无法获取桌子位置")
        
    return desk_info, table_info

def create_dynamic_animation():
    """根据场景中实际物体位置创建动态动画"""
    
    # 获取书桌和桌子的位置
    desk_info, table_info = get_object_positions()
    
    # 如果无法获取位置信息，使用默认值
    if not desk_info or "position" not in desk_info:
        desk_pos = [0.5, 1.0, 0.5]  # 默认书桌位置
    else:
        desk_pos = desk_info["position"]
        
    if not table_info or "position" not in table_info:
        table_pos = [3.5, 1.0, -2.5]  # 默认桌子位置
    else:
        table_pos = table_info["position"]
    
    # 书桌上方的位置
    desk_top_pos = {
        "x": desk_pos[0], 
        "y": desk_pos[1] + 0.5,  # 稍微高于书桌表面
        "z": desk_pos[2]
    }
    
    # 桌子上方的位置
    table_top_pos = {
        "x": table_pos[0], 
        "y": table_pos[1] + 0.5,  # 稍微高于桌子表面
        "z": table_pos[2]
    }
    
    # 中间的高点，用于创建弧线
    mid_point = {
        "x": (desk_pos[0] + table_pos[0]) / 2,  # X位置的中点
        "y": max(desk_pos[1], table_pos[1]) + 2.0,  # 比两个表面都高
        "z": (desk_pos[2] + table_pos[2]) / 2   # Z位置的中点
    }
    
    # 创建动画
    result = mcp.tools.create_multipoint_animation(
        name="hammer",  # 假设场景中有名为"hammer"的物体
        points=[
            # 起点（书桌上）
            {"position": desk_top_pos},
            
            # 中间点（空中）
            {"position": mid_point},
            
            # 终点（桌子上）
            {"position": table_top_pos}
        ],
        duration=3.0,
        timeline_asset_name="DynamicDeskToTable",
        path_type="bezier"
    )
    
    print(f"创建动态动画结果: {result}")
    return result

# 运行示例
if __name__ == "__main__":
    print("启动从书桌到桌子的动画示例...")
    
    # 选择要运行的示例
    # create_desk_to_table_animation()
    # create_desk_to_table_with_rotation()
    create_dynamic_animation()
    
    print("示例完成!") 