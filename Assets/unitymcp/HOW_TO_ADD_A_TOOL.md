1. # Unity MCP Server

   此目录包含 Unity MCP Server 的实现，它提供了 Python 和 Unity 编辑器功能之间的桥梁。

   ## 添加新工具

   要向 MCP Server 添加新工具，请按照以下步骤操作：

   ### 1. 创建 C# 命令处理器

   首先，在 `Editor/Commands` 目录中创建或修改命令处理器：

   ```csharp
   // 示例：NewCommandHandler.cs
   public static class NewCommandHandler
   {
       public static object HandleNewCommand(JObject @params)
       {
           // 提取参数
           string param1 = (string)@params["param1"];
           int param2 = (int)@params["param2"];
   
           // 实现在 Unity 端的功能
           // ...
   
           // 返回结果
           return new {
               message = "操作成功",
               result = someResult
           };
       }
   }
   ```

   ### 2. 注册命令处理器

   将命令处理器添加到 `Editor/Commands` 目录中的 `CommandRegistry.cs`：

   ```csharp
   public static class CommandRegistry
   {
       private static readonly Dictionary<string, Func<JObject, object>> _handlers = new()
       {
           // ... 现有处理器 ...
           { "NEW_COMMAND", NewCommandHandler.HandleNewCommand }
       };
   }
   ```

   ### 3. 创建 Python 工具

   将工具添加到 `Python/tools` 目录中的适当 Python 模块：

   ```python
   @mcp.tool()
   def new_tool(
       ctx: Context,
       param1: str,
       param2: int
   ) -> str:
       """工具的功能描述。
   
       参数：
           ctx: MCP 上下文
           param1: param1 的描述
           param2: param2 的描述
   
       返回值：
           str: 成功消息或错误详情
       """
       try:
           response = get_unity_connection().send_command("NEW_COMMAND", {
               "param1": param1,
               "param2": param2
           })
           return response.get("message", "操作成功")
       except Exception as e:
           return f"执行操作时出错：{str(e)}"
   ```

   ### 4. 注册工具

   确保在适当的注册函数中注册工具：

   ```python
   # 在 Python/tools/__init__.py 中
   def register_all_tools(mcp):
       register_scene_tools(mcp)
       register_script_tools(mcp)
       register_material_tools(mcp)
       # 如有需要，添加新的工具注册
   ```

   ### 5. 更新提示信息

   如果工具应向用户开放，请在 `Python/server.py` 中更新提示信息：

   ```python
   @mcp.prompt()
   def asset_creation_strategy() -> str:
       return (
           "遵循以下 Unity 最佳实践：\n\n"
           "1. **你的类别**：\n"
           "   - 使用 `new_tool(param1, param2)` 来执行操作\n"
           # ... 其余提示内容 ...
       )
   ```

   ## 最佳实践

   1. **存在性检查**：

      - 在创建或更新对象、脚本、资源或材质之前，务必检查其是否存在
      - 使用适当的搜索工具（如 `find_objects_by_name`、`list_scripts`、`get_asset_list`）进行存在性验证
      - 要同时处理两种情况：不存在时创建以及存在时更新
      - 在找不到预期资源时，实现适当的错误处理

   2. **错误处理**：

      - 在 Python 工具中始终包含 try-catch 块
      - 在 C# 处理器中验证参数
      - 返回有意义的错误消息

   3. **文档编写**：

      - 为 C# 处理器添加 XML 文档
      - 为 Python 工具编写详细的文档字符串
      - 更新提示信息，提供清晰的使用说明

   4. **参数验证**：

      - 在 Python 和 C# 两侧验证参数
      - 使用适当的类型（如 str、int、float、List 等）
      - 在适当的情况下提供默认值

   5. **测试**：

      - 在 Unity 编辑器和 Python 环境中测试工具
      - 验证错误处理是否符合预期
      - 检查工具是否与现有功能良好集成

   6. **代码组织**：
      - 将相关工具分组到适当的处理器类中
      - 使工具保持专注且单一用途
      - 遵循现有的命名约定

   ## 示例实现

   以下是添加新工具的完整示例：

   1. **C# 处理器**（`Editor/Commands/ExampleHandler.cs`）：

   ```csharp
   public static class ExampleHandler
   {
       public static object CreatePrefab(JObject @params)
       {
           string prefabName = (string)@params["prefab_name"];
           string template = (string)@params["template"];
           bool overwrite = @params["overwrite"] != null ? (bool)@params["overwrite"] : false;
   
           // 检查预制件是否已存在
           string prefabPath = $"Assets/Prefabs/{prefabName}.prefab";
           bool prefabExists = System.IO.File.Exists(prefabPath);
   
           if (prefabExists && !overwrite)
           {
               return new {
                   message = $"预制件已存在：{prefabName}。使用 overwrite=true 可替换它。",
                   exists = true,
                   path = prefabPath
               };
           }
   
           // 实现功能
           GameObject prefab = new GameObject(prefabName);
           // ... 设置预制件 ...
   
           return new {
               message = prefabExists ? $"已更新预制件：{prefabName}" : $"已创建预制件：{prefabName}",
               exists = prefabExists,
               path = prefabPath
           };
       }
   }
   ```

   2. **Python 工具**（`Python/tools/example_tools.py`）：

   ```python
   @mcp.tool()
   def create_prefab(
       ctx: Context,
       prefab_name: str,
       template: str = "default",
       overwrite: bool = False
   ) -> str:
       """在项目中创建新预制件或更新已有预制件。
   
       参数：
           ctx: MCP 上下文
           prefab_name: 新预制件的名称
           template: 要使用的模板（默认："default"）
           overwrite: 是否覆盖已有预制件（默认：False）
   
       返回值：
           str: 成功消息或错误详情
       """
       try:
           # 首先检查预制件是否存在
           assets = get_unity_connection().send_command("GET_ASSET_LIST", {
               "type": "Prefab",
               "search_pattern": prefab_name,
               "folder": "Assets/Prefabs"
           }).get("assets", [])
           
           prefab_exists = any(asset.get("name") == prefab_name for asset in assets)
           
           if prefab_exists and not overwrite:
               return f"预制件 '{prefab_name}' 已存在。使用 overwrite=True 可替换它。"
               
           # 创建或更新预制件
           response = get_unity_connection().send_command("CREATE_PREFAB", {
               "prefab_name": prefab_name,
               "template": template,
               "overwrite": overwrite
           })
           
           return response.get("message", "预制件操作成功完成")
       except Exception as e:
           return f"预制件操作出错：{str(e)}"
   ```

   3. **更新提示信息**：

   ```python
   "1. **预制件管理**：\n"
   "   - 创建预制件前务必检查其是否存在\n"
   "   - 使用 `create_prefab(prefab_name, template, overwrite=False)` 创建或更新预制件\n"
   ```

   ## 排查问题

   如果遇到问题：

   1. 检查 Unity 控制台中的 C# 错误
   2. 确认 Python 和 C# 之间的命令名称是否匹配
   3. 确保所有参数均正确序列化
   4. 检查 Python 日志中的连接问题
   5. 确认工具在两种环境中均正确注册
