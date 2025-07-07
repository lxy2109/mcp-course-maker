"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const fs = __importStar(require("fs"));
function activate(context) {
    context.subscriptions.push(vscode.window.registerCustomEditorProvider('hdrExrKtx2Viewer.viewer', new HdrExrKtx2ViewerProvider(context), {
        webviewOptions: { retainContextWhenHidden: true },
        supportsMultipleEditorsPerDocument: false
    }));
}
exports.activate = activate;
class HdrExrKtx2ViewerProvider {
    constructor(context) {
        this.context = context;
    }
    async openCustomDocument(uri, openContext, token) {
        return { uri, dispose: () => { } };
    }
    async resolveCustomEditor(document, webviewPanel, token) {
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
        // 构建webview内容
        const htmlPath = vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'webview.html'));
        let html = fs.readFileSync(htmlPath.fsPath, 'utf8');
        html = html.replace('{{BASE64}}', base64).replace('{{TYPE}}', ext);
        const parseExrUri = webviewPanel.webview.asWebviewUri(vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'parse-exr.js')));
        html = html.replace('PARSE_EXR_PATH', parseExrUri.toString());
        const viewerJsUri = webviewPanel.webview.asWebviewUri(vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'viewer.js')));
        html = html.replace('VIEWER_JS_PATH', viewerJsUri.toString());
        const ktxParseUri = webviewPanel.webview.asWebviewUri(vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'ktx-parse.js')));
        html = html.replace('KTX_PARSE_PATH', ktxParseUri.toString());
        const hdrJsUri = webviewPanel.webview.asWebviewUri(vscode.Uri.file(path.join(this.context.extensionPath, 'src', 'viewer', 'hdr.js')));
        html = html.replace('HDR_JS_PATH', hdrJsUri.toString());
        console.log(html); // 临时加，调试用
        webviewPanel.webview.html = html;
    }
}
//# sourceMappingURL=extension.js.map