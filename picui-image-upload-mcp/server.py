import os
from dotenv import load_dotenv
from mcp.server.fastmcp import FastMCP
from pydantic import BaseModel, Field
from typing import Optional, Dict, Any, List, Union, Literal
import httpx

load_dotenv()

PICUI_TOKEN = os.getenv("PICUI_TOKEN")  # 你的 Bearer Token
BASE_URL = "https://picui.cn/api/v1"

mcp = FastMCP("PICUI 图床 MCP Server", log_level="INFO")

# -------------------- 类型定义 --------------------

class PicuiUserProfile(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

class PicuiStrategyList(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

class PicuiTokenItem(BaseModel):
    token: str
    expired_at: str

class PicuiTokenResponse(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, List[PicuiTokenItem]]]

class PicuiUploadResponse(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

class PicuiImageList(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

class PicuiDeleteResponse(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

class PicuiAlbumList(BaseModel):
    status: bool
    message: str
    data: Optional[Dict[str, Any]]

# 统一返回类型
PicuiApiResponse = Union[
    PicuiUserProfile,
    PicuiStrategyList,
    PicuiTokenResponse,
    PicuiUploadResponse,
    PicuiImageList,
    PicuiDeleteResponse,
    PicuiAlbumList
]

class PicuiApiParams(BaseModel):
    action: Literal[
        'get_profile', 'get_strategies', 'generate_token', 'upload_image',
        'list_images', 'delete_image', 'list_albums', 'delete_album'
    ] = Field(..., description="操作类型")
    # 通用参数
    q: Optional[str] = Field(None, description="筛选关键字")
    # generate_token
    num: Optional[int] = Field(None, description="生成Token数量")
    seconds: Optional[int] = Field(None, description="Token有效期(秒)")
    # upload_image（严格按官方文档参数）
    file_path: Optional[str] = Field(None, description="本地图片路径（必填，File类型）")
    token: Optional[str] = Field(None, description="临时上传Token")
    permission: Optional[int] = Field(None, description="权限，1=公开，0=私有")
    strategy_id: Optional[int] = Field(None, description="储存策略ID")
    album_id: Optional[int] = Field(None, description="相册ID")
    expired_at: Optional[str] = Field(None, description="图片过期时间(yyyy-MM-dd HH:mm:ss)")
    # list_images
    page: Optional[int] = Field(1, description="页码")
    order: Optional[str] = Field("newest", description="排序方式")
    image_permission: Optional[str] = Field(None, description="图片权限 public/private")
    image_album_id: Optional[int] = Field(None, description="图片相册ID")
    image_q: Optional[str] = Field(None, description="图片筛选关键字")
    # delete_image
    key: Optional[str] = Field(None, description="图片唯一密钥")
    # list_albums
    album_page: Optional[int] = Field(1, description="相册页码")
    album_order: Optional[str] = Field("newest", description="相册排序方式")
    album_q: Optional[str] = Field(None, description="相册筛选关键字")
    # delete_album
    album_id_to_delete: Optional[int] = Field(None, description="要删除的相册ID")

# -------------------- 工具实现 --------------------

def get_headers() -> Dict[str, str]:
    headers = {"Accept": "application/json"}
    if PICUI_TOKEN:
        headers["Authorization"] = f"Bearer {PICUI_TOKEN}"
    return headers

@mcp.tool(description="PICUI图床多功能API工具，action区分操作类型。支持：用户资料、策略列表、生成Token、上传图片、图片列表、删除图片、相册列表、删除相册。参数详见文档。")
async def picui_api(params: PicuiApiParams) -> PicuiApiResponse:
    """
    PICUI图床多功能API工具
    Args:
        params (PicuiApiParams): 操作类型及相关参数
    Returns:
        PicuiApiResponse: 兼容所有接口的返回类型
    Raises:
        RuntimeError: 操作失败或HTTP错误
    """
    action = params.action
    async with httpx.AsyncClient() as client:
        if action == 'get_profile':
            resp = await client.get(f"{BASE_URL}/profile", headers=get_headers())
            resp.raise_for_status()
            return PicuiUserProfile(**resp.json())
        elif action == 'get_strategies':
            q = params.q
            query = {"q": q} if q else {}
            resp = await client.get(f"{BASE_URL}/strategies", headers=get_headers(), params=query)
            resp.raise_for_status()
            return PicuiStrategyList(**resp.json())
        elif action == 'generate_token':
            if params.num is None or params.seconds is None:
                raise ValueError("num 和 seconds 必填")
            data = {"num": params.num, "seconds": params.seconds}
            resp = await client.post(f"{BASE_URL}/images/tokens", headers=get_headers(), json=data)
            resp.raise_for_status()
            return PicuiTokenResponse(**resp.json())
        elif action == 'upload_image':
            # 严格按官方文档参数，file_path为必填
            if not params.file_path:
                raise ValueError("file_path（本地图片路径）为必填参数")
            import os
            if not os.path.isfile(params.file_path):
                raise ValueError(f"file_path 不存在: {params.file_path}")
            with open(params.file_path, 'rb') as f:
                file_content = f.read()
            file_name = os.path.basename(params.file_path)
            files = {"file": (file_name, file_content)}
            data = {}
            if params.token is not None:
                data["token"] = params.token
            if params.permission is not None:
                data["permission"] = str(params.permission)
            if params.strategy_id is not None:
                data["strategy_id"] = str(params.strategy_id)
            if params.album_id is not None:
                data["album_id"] = str(params.album_id)
            if params.expired_at is not None:
                data["expired_at"] = params.expired_at
            resp = await client.post(f"{BASE_URL}/upload", headers=get_headers(), data=data, files=files, timeout=30)
            if resp.status_code != 200:
                raise RuntimeError(f"HTTP错误: {resp.status_code} - {resp.text}")
            result = resp.json()
            if not result.get("status"):
                raise RuntimeError(f"上传失败: {result.get('message')}")
            return PicuiUploadResponse(**result)
        elif action == 'list_images':
            params_dict = {"page": params.page, "order": params.order}
            if params.image_permission: params_dict["permission"] = params.image_permission
            if params.image_album_id: params_dict["album_id"] = params.image_album_id
            if params.image_q: params_dict["q"] = params.image_q
            resp = await client.get(f"{BASE_URL}/images", headers=get_headers(), params=params_dict)
            resp.raise_for_status()
            return PicuiImageList(**resp.json())
        elif action == 'delete_image':
            if not params.key:
                raise ValueError("key 必填")
            resp = await client.delete(f"{BASE_URL}/images/{params.key}", headers=get_headers())
            resp.raise_for_status()
            return PicuiDeleteResponse(**resp.json())
        elif action == 'list_albums':
            params_dict = {"page": params.album_page, "order": params.album_order}
            if params.album_q: params_dict["q"] = params.album_q
            resp = await client.get(f"{BASE_URL}/albums", headers=get_headers(), params=params_dict)
            resp.raise_for_status()
            return PicuiAlbumList(**resp.json())
        elif action == 'delete_album':
            if not params.album_id_to_delete:
                raise ValueError("album_id_to_delete 必填")
            resp = await client.delete(f"{BASE_URL}/albums/{params.album_id_to_delete}", headers=get_headers())
            resp.raise_for_status()
            return PicuiDeleteResponse(**resp.json())
        else:
            raise ValueError(f"不支持的 action: {action}")

if __name__ == "__main__":
    mcp.run() 