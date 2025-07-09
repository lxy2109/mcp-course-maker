import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

export function activate(context: vscode.ExtensionContext) {
  context.subscriptions.push(
    vscode.window.registerCustomEditorProvider(
      'hdrExrKtx2Viewer.viewer',
      new HdrExrKtx2ViewerProvider(context),
      {
        webviewOptions: { retainContextWhenHidden: true },
        supportsMultipleEditorsPerDocument: false
      }
    )
  );
}

class HdrExrKtx2ViewerProvider implements vscode.CustomReadonlyEditorProvider {
  constructor(private readonly context: vscode.ExtensionContext) {}

  async openCustomDocument(
    uri: vscode.Uri,
    openContext: vscode.CustomDocumentOpenContext,
    token: vscode.CancellationToken
  ): Promise<vscode.CustomDocument> {
    return { uri, dispose: () => {} };
  }

  async resolveCustomEditor(
    document: vscode.CustomDocument,
    webviewPanel: vscode.WebviewPanel,
    token: vscode.CancellationToken
  ): Promise<void> {
    webviewPanel.webview.options = {
      enableScripts: true,
      localResourceRoots: [
        vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer'))
      ]
    };

    // 读取图片文件为base64
    const fileData = fs.readFileSync(document.uri.fsPath);
    const base64 = fileData.toString('base64');
    const ext = path.extname(document.uri.fsPath).toLowerCase().replace('.', '');
    // 兼容jpeg扩展名
    const type = ext === 'jpeg' ? 'jpg' : ext;

    // 构建webview内容
    const htmlPath = vscode.Uri.file(
      path.join(this.context.extensionPath, 'src', 'viewer', 'webview.html')
    );
    let html = fs.readFileSync(htmlPath.fsPath, 'utf8');
    html = html.replace('{{BASE64}}', base64).replace('{{TYPE}}', type);

    const parseExrUri = webviewPanel.webview.asWebviewUri(
      vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'parse-exr.js'))
    );
    html = html.replace('PARSE_EXR_PATH', parseExrUri.toString());

    const viewerJsUri = webviewPanel.webview.asWebviewUri(
      vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'viewer.js'))
    );
    html = html.replace('VIEWER_JS_PATH', viewerJsUri.toString());

    const ktxParseUri = webviewPanel.webview.asWebviewUri(
      vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'ktx-parse.js'))
    );
    html = html.replace('KTX_PARSE_PATH', ktxParseUri.toString());

    const hdrJsUri = webviewPanel.webview.asWebviewUri(
      vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'hdr.js'))
    );
    html = html.replace('HDR_JS_PATH', hdrJsUri.toString());

    // 传递type和base64到webview，webview.html需支持jpg/png分支
    webviewPanel.webview.html = html;
  }
} 