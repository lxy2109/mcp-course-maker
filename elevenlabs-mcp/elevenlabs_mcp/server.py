"""
ElevenLabs MCP Server

⚠️ IMPORTANT: This server provides access to ElevenLabs API endpoints which may incur costs.
Each tool that makes an API call is marked with a cost warning. Please follow these guidelines:

1. Only use tools when explicitly requested by the user
2. For tools that generate audio, consider the length of the text as it affects costs
3. Some operations like voice cloning or text-to-voice may have higher costs

Tools without cost warnings in their description are free to use as they only read existing data.
"""

import httpx
import os
import base64
from datetime import datetime
from io import BytesIO
from typing import Literal
from dotenv import load_dotenv
from mcp.server.fastmcp import FastMCP
from mcp.types import TextContent
from elevenlabs.client import ElevenLabs
from elevenlabs_mcp.model import McpVoice
from elevenlabs_mcp.utils import (
    make_error,
    make_output_path,
    make_output_file,
    handle_input_file,
)
from elevenlabs_mcp.convai import create_conversation_config, create_platform_settings
from elevenlabs.types.knowledge_base_locator import KnowledgeBaseLocator

from elevenlabs import play
from elevenlabs_mcp import __version__

load_dotenv()
api_key = os.getenv("ELEVENLABS_API_KEY")
base_path = os.getenv("ELEVENLABS_MCP_BASE_PATH")
DEFAULT_VOICE_ID = "cgSgspJ2msm6clMCkdW9"

if not api_key:
    raise ValueError("ELEVENLABS_API_KEY environment variable is required")

# Add custom client to ElevenLabs to set User-Agent header
custom_client = httpx.Client(
    headers={
        "User-Agent": f"ElevenLabs-MCP/{__version__}",
    }
)

client = ElevenLabs(api_key=api_key, httpx_client=custom_client)
mcp = FastMCP("ElevenLabs")


@mcp.tool(
    description="""
Agent管理工具，mode参数决定功能：

- create：创建Agent
    参数：
        name (str): Agent名称。
        first_message (str): 首句。
        system_prompt (str): 系统提示词。
        voice_id (str, optional): 语音ID。
        language (str, optional): 语言。
        llm (str, optional): LLM模型。
        temperature (float, optional): 温度。
        max_tokens (int, optional): 最大token数。
        asr_quality (str, optional): 识别质量。
        model_id (str, optional): 语音模型ID。
        optimize_streaming_latency (int, optional): 延迟优化。
        stability (float, optional): 稳定性。
        similarity_boost (float, optional): 相似度。
        turn_timeout (int, optional): 轮次超时。
        max_duration_seconds (int, optional): 最大对话时长。
        record_voice (bool, optional): 是否录音。
        retention_days (int, optional): 数据保留天数。
    返回：TextContent，包含创建结果

- list：列出所有Agent
    参数：无
    返回：TextContent，包含Agent列表

- get：获取Agent详情
    参数：
        agent_id (str): Agent ID。
    返回：TextContent，包含Agent详细信息

- add_knowledge_base：为Agent添加知识库
    参数：
        agent_id (str): Agent ID。
        knowledge_base_name (str): 知识库名称。
        url (str, optional): 知识库URL。
        input_file_path (str, optional): 知识库文件路径。
        text (str, optional): 知识库文本。
    返回：TextContent，包含添加结果
"""
)
def agent_manage(
    mode: Literal["create", "list", "get", "add_knowledge_base"],
    name: str | None = None,
    first_message: str | None = None,
    system_prompt: str | None = None,
    voice_id: str | None = DEFAULT_VOICE_ID,
    language: str = "en",
    llm: str = "gemini-2.0-flash-001",
    temperature: float = 0.5,
    max_tokens: int | None = None,
    asr_quality: str = "high",
    model_id: str = "eleven_turbo_v2",
    optimize_streaming_latency: int = 3,
    stability: float = 0.5,
    similarity_boost: float = 0.8,
    turn_timeout: int = 7,
    max_duration_seconds: int = 300,
    record_voice: bool = True,
    retention_days: int = 730,
    agent_id: str | None = None,
    knowledge_base_name: str | None = None,
    url: str | None = None,
    input_file_path: str | None = None,
    text: str | None = None,
):
    if mode == "create":
        conversation_config = create_conversation_config(
            language=language,
            system_prompt=system_prompt,
            llm=llm,
            first_message=first_message,
            temperature=temperature,
            max_tokens=max_tokens,
            asr_quality=asr_quality,
            voice_id=voice_id,
            model_id=model_id,
            optimize_streaming_latency=optimize_streaming_latency,
            stability=stability,
            similarity_boost=similarity_boost,
            turn_timeout=turn_timeout,
            max_duration_seconds=max_duration_seconds,
        )
        platform_settings = create_platform_settings(
            record_voice=record_voice,
            retention_days=retention_days,
        )
        response = client.conversational_ai.create_agent(
            name=name,
            conversation_config=conversation_config,
            platform_settings=platform_settings,
        )
        return TextContent(
            type="text",
            text=f"Agent created successfully: Name: {name}, Agent ID: {response.agent_id}, System Prompt: {system_prompt}, Voice ID: {voice_id or 'Default'}, Language: {language}, LLM: {llm}, You can use this agent ID for future interactions with the agent.",
        )
    elif mode == "list":
        response = client.conversational_ai.get_agents()
        if not response.agents:
            return TextContent(type="text", text="No agents found.")
        agent_list = ",".join(f"{agent.name} (ID: {agent.agent_id})" for agent in response.agents)
        return TextContent(type="text", text=f"Available agents: {agent_list}")
    elif mode == "get":
        response = client.conversational_ai.get_agent(agent_id=agent_id)
        voice_info = "None"
        if response.conversation_config.tts:
            voice_info = f"Voice ID: {response.conversation_config.tts.voice_id}"
        return TextContent(
            type="text",
            text=f"Agent Details: Name: {response.name}, Agent ID: {response.agent_id}, Voice Configuration: {voice_info}, Created At: {datetime.fromtimestamp(response.metadata.created_at_unix_secs).strftime('%Y-%m-%d %H:%M:%S')}",
        )
    elif mode == "add_knowledge_base":
        provided_params = [param for param in [url, input_file_path, text] if param is not None]
        if len(provided_params) == 0:
            make_error("Must provide either a URL, a file, or text")
        if len(provided_params) > 1:
            make_error("Must provide exactly one of: URL, file, or text")
        if text is not None:
            text_bytes = text.encode("utf-8")
            text_io = BytesIO(text_bytes)
            text_io.name = "text.txt"
            text_io.content_type = "text/plain"
            file = text_io
        elif input_file_path is not None:
            path = handle_input_file(file_path=input_file_path, audio_content_check=False)
            file = open(path, "rb")
        response = client.conversational_ai.add_to_knowledge_base(
            name=knowledge_base_name,
            url=url,
            file=file,
        )
        agent = client.conversational_ai.get_agent(agent_id=agent_id)
        agent.conversation_config.agent.prompt.knowledge_base.append(
            KnowledgeBaseLocator(
                type="file" if file else "url",
                name=knowledge_base_name,
                id=response.id,
            )
        )
        client.conversational_ai.update_agent(
            agent_id=agent_id, conversation_config=agent.conversation_config
        )
        return TextContent(
            type="text",
            text=f"Knowledge base created with ID: {response.id} and added to agent {agent_id} successfully.",
        )
    else:
        make_error("mode参数必须为 'create'、'list'、'get'、'add_knowledge_base'")

@mcp.tool(
    description="""
音频处理工具，mode参数决定功能：

- speech_to_speech：语音转换
    参数：
        input_file_path (str): 输入音频文件路径。
        voice_name (str, optional): 目标声音名称，默认'Adam'。
        output_directory (str, optional): 输出目录。
    返回：TextContent，包含转换后音频文件路径

- isolate：声音隔离
    参数：
        input_file_path (str): 输入音频文件路径。
        output_directory (str, optional): 输出目录。
    返回：TextContent，包含隔离后音频文件路径
"""
)
def audio_process(
    mode: Literal["speech_to_speech", "isolate"],
    input_file_path: str,
    voice_name: str = "Adam",
    output_directory: str | None = None,
):
    if mode == "speech_to_speech":
        voices = client.voices.search(search=voice_name)
        if len(voices.voices) == 0:
            make_error("No voice found with that name.")
        voice = next((v for v in voices.voices if v.name == voice_name), None)
        if voice is None:
            make_error(f"Voice with name: {voice_name} does not exist.")
        file_path = handle_input_file(input_file_path)
        output_path = make_output_path(output_directory, base_path)
        output_file_name = make_output_file("sts", file_path.name, output_path, "mp3")
        with file_path.open("rb") as f:
            audio_bytes = f.read()
        audio_data = client.speech_to_speech.convert(
            model_id="eleven_multilingual_sts_v2",
            voice_id=voice.voice_id,
            audio=audio_bytes,
        )
        audio_bytes = b"".join(audio_data)
        with open(output_path / output_file_name, "wb") as f:
            f.write(audio_bytes)
        return TextContent(
            type="text", text=f"Success. File saved as: {output_path / output_file_name}"
        )
    elif mode == "isolate":
        file_path = handle_input_file(input_file_path)
        output_path = make_output_path(output_directory, base_path)
        output_file_name = make_output_file("iso", file_path.name, output_path, "mp3")
        with file_path.open("rb") as f:
            audio_bytes = f.read()
        audio_data = client.audio_isolation.audio_isolation(
            audio=audio_bytes,
        )
        audio_bytes = b"".join(audio_data)
        with open(output_path / output_file_name, "wb") as f:
            f.write(audio_bytes)
        return TextContent(
            type="text",
            text=f"Success. File saved as: {output_path / output_file_name}",
        )
    else:
        make_error("mode参数必须为 'speech_to_speech' 或 'isolate'")

@mcp.tool(
    description="""
音频实用工具，mode参数决定功能：

- speech_to_text：音频转文本
    参数：
        input_file_path (str): 输入音频文件路径。
        language_code (str, optional): 语言代码，默认"eng"。
        diarize (bool, optional): 是否区分说话人，默认False。
        save_transcript_to_file (bool, optional): 是否保存转录文件，默认True。
        return_transcript_to_client_directly (bool, optional): 是否直接返回文本，默认False。
        output_directory (str, optional): 输出目录。
    返回：TextContent，包含转录文本或文件路径

- play：播放音频
    参数：
        input_file_path (str): 输入音频文件路径。
    返回：TextContent，播放结果
"""
)
def audio_util(
    mode: Literal["speech_to_text", "play"],
    input_file_path: str,
    language_code: str = "eng",
    diarize: bool = False,
    save_transcript_to_file: bool = True,
    return_transcript_to_client_directly: bool = False,
    output_directory: str | None = None,
):
    if mode == "speech_to_text":
        if not save_transcript_to_file and not return_transcript_to_client_directly:
            make_error("Must save transcript to file or return it to the client directly.")
        file_path = handle_input_file(input_file_path)
        if save_transcript_to_file:
            output_path = make_output_path(output_directory, base_path)
            output_file_name = make_output_file("stt", file_path.name, output_path, "txt")
        with file_path.open("rb") as f:
            audio_bytes = f.read()
        transcription = client.speech_to_text.convert(
            model_id="scribe_v1",
            file=audio_bytes,
            language_code=language_code,
            enable_logging=True,
            diarize=diarize,
            tag_audio_events=True,
        )
        if save_transcript_to_file:
            with open(output_path / output_file_name, "w") as f:
                f.write(transcription.text)
        if return_transcript_to_client_directly:
            return TextContent(type="text", text=transcription.text)
        else:
            return TextContent(
                type="text", text=f"Transcription saved to {output_path / output_file_name}"
            )
    elif mode == "play":
        file_path = handle_input_file(input_file_path)
        play(open(file_path, "rb").read(), use_ffmpeg=False)
        return TextContent(type="text", text=f"Successfully played audio file: {file_path}")
    else:
        make_error("mode参数必须为 'speech_to_text' 或 'play'")

@mcp.tool(
    description="Check the current subscription status. Could be used to measure the usage of the API."
)
def check_subscription() -> TextContent:
    subscription = client.user.get_subscription()
    return TextContent(type="text", text=f"{subscription.model_dump_json(indent=2)}")

@mcp.tool(
    description="""
声音管理工具，mode参数决定功能：

- search_local：查找本地声音库
    参数：
        search (str, optional): 搜索关键字。
        sort (str, optional): 排序字段，'created_at_unix' 或 'name'，默认'name'。
        sort_direction (str, optional): 排序方向，'asc' 或 'desc'，默认'desc'。
    返回：List[McpVoice]

- get：获取单个声音详情
    参数：
        voice_id (str): 声音ID。
    返回：McpVoice

- clone：克隆声音
    参数：
        name (str): 新声音名称。
        files (list[str]): 音频文件路径列表。
        description (str, optional): 声音描述。
    返回：TextContent，包含克隆结果

- search_library：查找全局声音库
    参数：
        page (int, optional): 页码，默认0。
        page_size (int, optional): 每页数量，默认10。
        search (str, optional): 搜索关键字。
    返回：TextContent，包含查找结果

- create_from_preview：用预览ID创建声音
    参数：
        generated_voice_id (str): 预览生成的ID。
        voice_name (str): 新声音名称。
        voice_description (str): 声音描述。
    返回：TextContent，包含创建结果
"""
)
def voice_manage(
    mode: Literal["search_local", "search_library", "get", "clone", "create_from_preview"],
    search: str | None = None,
    sort: Literal["created_at_unix", "name"] = "name",
    sort_direction: Literal["asc", "desc"] = "desc",
    voice_id: str | None = None,
    name: str | None = None,
    files: list[str] | None = None,
    description: str | None = None,
    page: int = 0,
    page_size: int = 10,
    generated_voice_id: str | None = None,
    voice_name: str | None = None,
    voice_description: str | None = None,
):
    if mode == "search_local":
        response = client.voices.search(search=search, sort=sort, sort_direction=sort_direction)
        return [McpVoice(id=voice.voice_id, name=voice.name, category=voice.category) for voice in response.voices]
    elif mode == "get":
        response = client.voices.get(voice_id=voice_id)
        return McpVoice(
            id=response.voice_id,
            name=response.name,
            category=response.category,
            fine_tuning_status=response.fine_tuning.state,
        )
    elif mode == "clone":
        input_files = [str(handle_input_file(file).absolute()) for file in (files or [])]
        voice = client.clone(name=name, description=description, files=input_files)
        return TextContent(
            type="text",
            text=f"Voice cloned successfully: Name: {voice.name}\nID: {voice.voice_id}\nCategory: {voice.category}\nDescription: {voice.description or 'N/A'}",
        )
    elif mode == "search_library":
        response = client.voices.get_shared(page=page, page_size=page_size, search=search)
        if not response.voices:
            return TextContent(type="text", text="No shared voices found with the specified criteria.")
        voice_list = []
        for voice in response.voices:
            language_info = "N/A"
            if hasattr(voice, "verified_languages") and voice.verified_languages:
                languages = []
                for lang in voice.verified_languages:
                    accent_info = f" ({lang.accent})" if hasattr(lang, "accent") and lang.accent else ""
                    languages.append(f"{lang.language}{accent_info}")
                language_info = ", ".join(languages)
            details = [
                f"Name: {voice.name}",
                f"ID: {voice.voice_id}",
                f"Category: {getattr(voice, 'category', 'N/A')}",
            ]
            if hasattr(voice, "gender") and voice.gender:
                details.append(f"Gender: {voice.gender}")
            if hasattr(voice, "age") and voice.age:
                details.append(f"Age: {voice.age}")
            if hasattr(voice, "accent") and voice.accent:
                details.append(f"Accent: {voice.accent}")
            if hasattr(voice, "description") and voice.description:
                details.append(f"Description: {voice.description}")
            if hasattr(voice, "use_case") and voice.use_case:
                details.append(f"Use Case: {voice.use_case}")
            details.append(f"Languages: {language_info}")
            if hasattr(voice, "preview_url") and voice.preview_url:
                details.append(f"Preview URL: {voice.preview_url}")
            voice_info = "\n".join(details)
            voice_list.append(voice_info)
        formatted_info = "\n\n".join(voice_list)
        return TextContent(type="text", text=f"Shared Voices:\n\n{formatted_info}")
    elif mode == "create_from_preview":
        voice = client.text_to_voice.create_voice_from_preview(
            voice_name=voice_name,
            voice_description=voice_description,
            generated_voice_id=generated_voice_id,
        )
        return TextContent(
            type="text",
            text=f"Success. Voice created: {voice.name} with ID:{voice.voice_id}",
        )
    else:
        make_error("mode参数必须为 'search_local'、'search_library'、'get'、'clone'、'create_from_preview'")

@mcp.tool(
    description="""
文本生成音频（支持三种模式）：
- tts: 文本转语音（Text to Speech）
- sfx: 文本描述生成音效（Text to Sound Effects）
- voice_design: 文本描述生成声音设计/预览（Text to Voice）

通过 mode 参数选择功能，其余参数与原有各工具一致，未用到的参数可留空。

⚠️ COST WARNING: 本工具会调用 ElevenLabs API，可能产生费用。仅在用户明确请求时使用。

Args:
    mode (str): 选择功能类型，可选 'tts'、'sfx'、'voice_design'。
    text (str): 要转换的文本内容（tts/sfx/voice_design均需）。
    voice_name (str, optional): 语音名称，仅tts模式可用。
    voice_id (str, optional): 语音ID，仅tts模式可用。
    stability (float, optional): 语音稳定性，仅tts模式可用。0-1。
    similarity_boost (float, optional): 相似度提升，仅tts模式可用。0-1。
    style (float, optional): 风格夸张度，仅tts模式可用。0-1。
    use_speaker_boost (bool, optional): 是否提升说话人相似度，仅tts模式可用。
    speed (float, optional): 语速，仅tts模式可用。0.7-1.2。
    language (str, optional): 语音语言，仅tts模式可用。ISO 639-1。
    output_format (str, optional): 输出音频格式，所有模式可用。默认"mp3_44100_128"。
    output_file_name (str, optional): 自定义输出文件名，所有模式可用。
    output_directory (str, optional): 输出目录，所有模式可用。
    duration_seconds (float, optional): 音效时长，仅sfx模式可用。0.5-5秒。
    voice_description (str, optional): 声音设计描述，仅voice_design模式可用。

Returns:
    TextContent，包含生成的音频文件路径和相关信息。
"""
)
def generate_audio(
    mode: Literal["tts", "sfx", "voice_design"],
    text: str,
    voice_name: str | None = None,
    voice_id: str | None = None,
    stability: float = 0.5,
    similarity_boost: float = 0.75,
    style: float = 0,
    use_speaker_boost: bool = True,
    speed: float = 1.0,
    language: str = "en",
    output_format: str = "mp3_44100_128",
    output_file_name: str | None = None,
    output_directory: str | None = None,
    duration_seconds: float = 2.0,
    voice_description: str | None = None,
):
    if mode == "tts":
        if text == "":
            make_error("Text is required.")
        if voice_id is not None and voice_name is not None:
            make_error("voice_id and voice_name cannot both be provided.")
        voice = None
        if voice_id is not None:
            voice = client.voices.get(voice_id=voice_id)
        elif voice_name is not None:
            voices = client.voices.search(search=voice_name)
            if len(voices.voices) == 0:
                make_error("No voices found with that name.")
            voice = next((v for v in voices.voices if v.name == voice_name), None)
            if voice is None:
                make_error(f"Voice with name: {voice_name} does not exist.")
        voice_id_final = voice.voice_id if voice else DEFAULT_VOICE_ID
        output_path = make_output_path(output_directory, base_path)
        if output_file_name:
            if not output_file_name.endswith(".mp3"):
                output_file_name = make_output_file(output_file_name, output_path, "mp3")
        else:
            output_file_name = make_output_file("tts", text, output_path, "mp3")
        model_id = "eleven_flash_v2_5" if language in ["hu", "no", "vi"] else "eleven_multilingual_v2"
        audio_data = client.text_to_speech.convert(
            text=text,
            voice_id=voice_id_final,
            model_id=model_id,
            output_format=output_format,
            voice_settings={
                "stability": stability,
                "similarity_boost": similarity_boost,
                "style": style,
                "use_speaker_boost": use_speaker_boost,
                "speed": speed,
            },
        )
        audio_bytes = b"".join(audio_data)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path / output_file_name, "wb") as f:
            f.write(audio_bytes)
        return TextContent(
            type="text",
            text=f"Success. File saved as: {output_path / output_file_name}. Voice used: {voice.name if voice else DEFAULT_VOICE_ID}",
        )
    elif mode == "sfx":
        if duration_seconds < 0.5 or duration_seconds > 5:
            make_error("Duration must be between 0.5 and 5 seconds")
        output_path = make_output_path(output_directory, base_path)
        output_file_name_final = make_output_file("sfx", text, output_path, "mp3")
        audio_data = client.text_to_sound_effects.convert(
            text=text,
            output_format=output_format,
            duration_seconds=duration_seconds,
        )
        audio_bytes = b"".join(audio_data)
        with open(output_path / output_file_name_final, "wb") as f:
            f.write(audio_bytes)
        return TextContent(
            type="text",
            text=f"Success. File saved as: {output_path / output_file_name_final}",
        )
    elif mode == "voice_design":
        if not voice_description:
            make_error("Voice description is required.")
        previews = client.text_to_voice.create_previews(
            voice_description=voice_description,
            text=text,
            auto_generate_text=True if not text else False,
        )
        output_path = make_output_path(output_directory, base_path)
        generated_voice_ids = []
        output_file_paths = []
        for preview in previews.previews:
            output_file_name_final = make_output_file(
                "voice_design", preview.generated_voice_id, output_path, "mp3", full_id=True
            )
            output_file_paths.append(str(output_file_name_final))
            generated_voice_ids.append(preview.generated_voice_id)
            audio_bytes = base64.b64decode(preview.audio_base_64)
            with open(output_path / output_file_name_final, "wb") as f:
                f.write(audio_bytes)
        return TextContent(
            type="text",
            text=f"Success. Files saved at: {', '.join(output_file_paths)}. Generated voice IDs are: {', '.join(generated_voice_ids)}",
        )
    else:
        make_error("mode参数必须为 'tts'、'sfx' 或 'voice_design'")

def main():
    print("Starting MCP server")
    """Run the MCP server"""
    mcp.run()


if __name__ == "__main__":
    main()
