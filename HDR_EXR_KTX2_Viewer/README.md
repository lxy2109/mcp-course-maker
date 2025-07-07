# HDR/EXR/KTX2 Viewer

适用于 VSCode/Cursor 的高动态范围图片查看插件，支持 EXR、KTX2、HDR 格式。

## 功能

- 直接在编辑器中预览 .exr、.ktx2、.hdr 文件
- 支持缩放、拖拽、色彩映射（可扩展）
- 兼容 VSCode 与 Cursor
- **支持本地 Web 端文件上传与预览（见下方"Web 预览服务"）**

## 安装

1. 克隆本项目
2. 运行 `npm install` 安装依赖
3. 运行 `npm run compile` 编译 TypeScript
4. 在 VSCode/Cursor 中选择"扩展开发主机"调试或打包发布

## 本地安装 VSIX 插件包

如果你已通过 `vsce package` 生成了 `.vsix` 文件，可以通过以下方式在本地安装插件：

### 命令行安装

1. 打开命令行，切换到 `.vsix` 文件所在目录。
2. 执行以下命令（将文件名替换为实际生成的 vsix 文件名）：

   ```sh
   code --install-extension hdr-exr-ktx2-viewer-0.0.1.vsix
   ```

   > 如果你使用的是 Cursor，可以同样使用 `code` 命令，或 `cursor --install-extension xxx.vsix`。

3. 安装成功后，重启 VSCode/Cursor 即可使用插件。

### 图形界面安装

1. 打开 VSCode/Cursor。
2. 进入"扩展"面板，点击右上角"..."，选择"从 VSIX 安装..."或"Install from VSIX..."
3. 选择你生成的 `.vsix` 文件，确认安装。
4. 安装完成后重启编辑器。

## 使用

- 打开任意 `.exr`
