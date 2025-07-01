import json
import os

def fix_scale_fields(json_path):
    if not os.path.exists(json_path):
        print(f"文件不存在: {json_path}")
        return

    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    changed = False
    for category, items in data.items():
        for name, item in items.items():
            size = item.get("真实尺寸", {})
            l = size.get("length", 0)
            w = size.get("width", 0)
            h = size.get("height", 0)
            d = size.get("diameter", 0)
            max_edge = max(l, w, h, d)
            scale = round(max_edge / 100, 4) if max_edge > 0.01 else 0.0001
            if item.get("scale") != scale:
                item["scale"] = scale
                changed = True

    if changed:
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"已修正并保存: {json_path}")
    else:
        print("所有scale字段已符合新规则，无需修改。")

if __name__ == "__main__":
    # 修改为你的比例库json路径
    json_file = "Assets/ModelParameterLib/ModelScaleDatabase.json"
    fix_scale_fields(json_file)