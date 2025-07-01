from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any

class TextToModelRequest(BaseModel):
    prompt: str = Field(..., description="文本描述，指引3D模型生成，最大1024字符。支持多语言，不支持emoji和部分特殊字符。")
    model_version: Optional[str] = Field("v2.5-20250123", description="模型版本，可选：v2.5-20250123, v2.0-20240919, v1.4-20240625。默认v2.5-20250123。")
    negative_prompt: Optional[str] = Field(None, description="反向描述，辅助生成与prompt相反的内容，最大255字符。")
    face_limit: Optional[int] = Field(None, description="输出模型面数上限。未设置时自适应。仅v2.0及以上版本有效。")
    texture: Optional[bool] = Field(True, description="是否生成贴图。默认True。仅v2.0及以上版本有效。")
    pbr: Optional[bool] = Field(True, description="是否生成PBR贴图。默认True。仅v2.0及以上版本有效。若为True，texture自动为True。")
    model_seed: Optional[int] = Field(None, description="模型生成随机种子。相同种子可复现同一几何。仅v2.0及以上版本有效。")
    texture_seed: Optional[int] = Field(None, description="贴图生成随机种子。相同种子可复现同一贴图。仅v2.0及以上版本有效。")
    texture_quality: Optional[str] = Field("standard", description="贴图质量，可选：standard, detailed。默认standard。仅v2.0及以上版本有效。")
    style: Optional[str] = Field(None, description="风格化类型，如person:person2cartoon, animal:venom, object:clay等。详见官方文档。")
    auto_size: Optional[bool] = Field(False, description="是否自动缩放到真实世界尺寸（米）。默认False。仅v2.0及以上版本有效。")
    quad: Optional[bool] = Field(False, description="是否输出四边面网格。默认False。若为True且未设置face_limit，默认10000。仅v2.0及以上版本有效。")

class ImageToModelRequest(BaseModel):
    file: Dict[str, Any] = Field(..., description="图片文件描述对象，包含：type（图片类型），file_token（上传token，可选），url（公网直链，可选），object（STS信息，可选）。三者互斥，详见官方文档。")
    model_version: Optional[str] = Field("v2.5-20250123", description="模型版本，同TextToModelRequest。")
    face_limit: Optional[int] = Field(None, description="输出模型面数上限。未设置时自适应。仅v2.0及以上版本有效。")
    texture: Optional[bool] = Field(True, description="是否生成贴图。默认True。仅v2.0及以上版本有效。")
    pbr: Optional[bool] = Field(True, description="是否生成PBR贴图。默认True。仅v2.0及以上版本有效。若为True，texture自动为True。")
    model_seed: Optional[int] = Field(None, description="模型生成随机种子。仅v2.0及以上版本有效。")
    texture_seed: Optional[int] = Field(None, description="贴图生成随机种子。仅v2.0及以上版本有效。")
    texture_quality: Optional[str] = Field("standard", description="贴图质量，可选：standard, detailed。默认standard。仅v2.0及以上版本有效。")
    style: Optional[str] = Field(None, description="风格化类型，详见官方文档。")
    auto_size: Optional[bool] = Field(False, description="是否自动缩放到真实世界尺寸。默认False。仅v2.0及以上版本有效。")
    quad: Optional[bool] = Field(False, description="是否输出四边面网格。默认False。仅v2.0及以上版本有效。")

class MultiviewToModelRequest(BaseModel):
    files: List[Dict[str, Any]] = Field(..., description="多视图图片输入，列表顺序为[front, left, back, right]，每项为dict，需包含type和file_token。front必填。")
    model_version: Optional[str] = Field("v2.5-20250123", description="模型版本，同TextToModelRequest。")
    face_limit: Optional[int] = Field(None, description="输出模型面数上限。未设置时自适应。仅v2.0及以上版本有效。")
    texture: Optional[bool] = Field(True, description="是否生成贴图。默认True。仅v2.0及以上版本有效。")
    pbr: Optional[bool] = Field(True, description="是否生成PBR贴图。默认True。仅v2.0及以上版本有效。若为True，texture自动为True。")
    model_seed: Optional[int] = Field(None, description="模型生成随机种子。仅v2.0及以上版本有效。")
    texture_seed: Optional[int] = Field(None, description="贴图生成随机种子。仅v2.0及以上版本有效。")
    texture_quality: Optional[str] = Field("standard", description="贴图质量，可选：standard, detailed。默认standard。仅v2.0及以上版本有效。")
    style: Optional[str] = Field(None, description="风格化类型，详见官方文档。")
    auto_size: Optional[bool] = Field(False, description="是否自动缩放到真实世界尺寸。默认False。仅v2.0及以上版本有效。")
    quad: Optional[bool] = Field(False, description="是否输出四边面网格。默认False。仅v2.0及以上版本有效。")

class TextureModelRequest(BaseModel):
    original_model_task_id: str = Field(..., description="原始模型任务ID，需为text_to_model/image_to_model/multiview_to_model类型且成功。")
    texture: Optional[bool] = Field(True, description="是否生成贴图。默认True。")
    pbr: Optional[bool] = Field(True, description="是否生成PBR贴图。默认True。若为True，texture自动为True。")
    model_seed: Optional[int] = Field(None, description="模型生成随机种子。")
    texture_seed: Optional[int] = Field(None, description="贴图生成随机种子。")
    texture_quality: Optional[str] = Field("standard", description="贴图质量，可选：standard, detailed。默认standard。")
    texture_alignment: Optional[str] = Field("original_image", description="贴图对齐方式，可选original_image/geometry。默认original_image。")

class RefineModelRequest(BaseModel):
    draft_model_task_id: str = Field(..., description="草稿模型任务ID，仅支持text_to_model/image_to_model/multiview_to_model类型且成功。v2.0及以上版本不支持。")

class AnimatePrerigcheckRequest(BaseModel):
    original_model_task_id: str = Field(..., description="原始模型任务ID，支持text_to_model/image_to_model/multiview_to_model/texture_model/refine_model类型且成功。")

class AnimateRigRequest(BaseModel):
    original_model_task_id: str = Field(..., description="原始模型任务ID，支持text_to_model/image_to_model/multiview_to_model/texture_model/refine_model类型且成功。")
    out_format: Optional[str] = Field("glb", description="输出文件格式，可选glb/fbx，默认glb。")
    topology: Optional[str] = Field(None, description="骨骼拓扑结构，可选bip/quad。")
    spec: Optional[str] = Field("tripo", description="骨骼规格，可选mixamo/tripo，默认tripo。")

class AnimateRetargetRequest(BaseModel):
    original_model_task_id: str = Field(..., description="骨骼绑定任务ID，仅支持rig类型且成功。")
    out_format: Optional[str] = Field("glb", description="输出文件格式，可选glb/fbx，默认glb。")
    animation: str = Field(..., description="动画类型，可选preset:idle/preset:walk/preset:climb/preset:jump/preset:run/preset:slash/preset:shoot/preset:hurt/preset:fall/preset:turn。")
    bake_animation: Optional[bool] = Field(True, description="是否烘焙动画，仅fbx支持。默认True。")

class StylizeModelRequest(BaseModel):
    original_model_task_id: str = Field(..., description="原始模型任务ID，支持text_to_model/image_to_model/multiview_to_model/texture_model/refine_model/animate_rig/animate_retarget类型且成功。")
    style: str = Field(..., description="风格类型，可选lego/voxel/voronoi/minecraft。")
    block_size: Optional[int] = Field(80, description="网格大小，仅minecraft风格有效，范围32-128，默认80。")

class ConvertModelRequest(BaseModel):
    original_model_task_id: str = Field(..., description="原始模型任务ID，支持text_to_model/image_to_model/multiview_to_model/texture_model/refine_model/animate_rig/animate_retarget/convert类型且成功。")
    format: str = Field(..., description="目标格式，可选GLTF/USDZ/FBX/OBJ/STL/3MF。")
    quad: Optional[bool] = Field(False, description="是否四边面重拓扑。默认False。")
    force_symmetry: Optional[bool] = Field(False, description="四边面重拓扑时是否强制对称。默认False。")
    face_limit: Optional[int] = Field(10000, description="输出模型面数上限。默认10000。")
    flatten_bottom: Optional[bool] = Field(False, description="是否压平底部。默认False。")
    flatten_bottom_threshold: Optional[float] = Field(0.01, description="压平底部深度，仅flatten_bottom为True时有效。默认0.01。")
    texture_size: Optional[int] = Field(4096, description="贴图分辨率，默认4096。")
    texture_format: Optional[str] = Field("JPEG", description="贴图格式，可选BMP/DPX/HDR/JPEG/OPEN_EXR/PNG/TARGA/TIFF/WEBP。默认JPEG。FBX默认PNG。")
    pivot_to_center_bottom: Optional[bool] = Field(False, description="是否将枢轴点设置为底部中心。默认False。")
    scale_factor: Optional[float] = Field(1.0, description="缩放因子，默认1。")

class TaskIdRequest(BaseModel):
    task_id: str = Field(..., description="任务ID。用于查询任务状态和结果。")

class UploadImageRequest(BaseModel):
    file_path: str = Field(..., description="本地图片文件路径，支持webp、jpeg、png。最大20MB。")

class BalanceResponse(BaseModel):
    balance: float = Field(..., description="API钱包余额。")
    frozen: float = Field(..., description="冻结余额，任务运行或交易中时非零。") 