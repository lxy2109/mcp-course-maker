const express = require('express');
const multer = require('multer');
const fs = require('fs');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

const app = express();
const upload = multer({ dest: 'uploads/' });

// 内存存储上传的base64数据
const fileStore = {};

// 静态资源映射
app.use('/viewer', express.static(path.join(__dirname, 'viewer')));
app.use('/three.js', express.static(path.join(__dirname, 'viewer/three.js'))); // 如有three.js
app.use('/parse-exr.js', express.static(path.join(__dirname, 'viewer/parse-exr.js')));
app.use('/ktx-parse.js', express.static(path.join(__dirname, 'viewer/ktx-parse.js')));
app.use('/hdr.js', express.static(path.join(__dirname, 'viewer/hdr.js')));

// 首页：上传表单
app.get('/', (req, res) => {
  res.send(`
    <h2>HDR/EXR/KTX2 预览</h2>
    <form action="/upload" method="post" enctype="multipart/form-data">
      <input type="file" name="file" required>
      <button type="submit">上传并预览</button>
    </form>
  `);
});

// 文件上传处理（用key存储base64，避免URL过长）
app.post('/upload', upload.single('file'), (req, res) => {
  const file = req.file;
  if (!file) return res.status(400).send('未上传文件');
  const ext = path.extname(file.originalname).toLowerCase().replace('.', '');
  if (!['hdr', 'exr', 'ktx2'].includes(ext)) {
    fs.unlinkSync(file.path);
    return res.status(400).send('仅支持HDR/EXR/KTX2文件');
  }
  const base64 = fs.readFileSync(file.path).toString('base64');
  fs.unlinkSync(file.path);
  const key = uuidv4();
  fileStore[key] = { base64, type: ext };
  res.redirect(`/preview?type=${ext}&key=${key}&name=${encodeURIComponent(file.originalname)}`);
});

// 提供base64数据的接口
app.get('/file/:key', (req, res) => {
  const { key } = req.params;
  if (!fileStore[key]) return res.status(404).send('文件不存在');
  res.json({ base64: fileStore[key].base64, type: fileStore[key].type });
});

// 预览页面（通过key异步获取base64）
app.get('/preview', (req, res) => {
  const { type, key, name } = req.query;
  if (!type || !key) return res.status(400).send('参数缺失');
  res.send(`
    <!DOCTYPE html>
    <html>
    <head>
      <meta charset="utf-8">
      <title>Web 预览</title>
      <style>
        html, body { margin:0; padding:0; background:#23272e; color:#fff; height:100%; }
        #canvas { width:100vw; height:100vh; display:block; }
        #info { position:fixed; top:10px; left:10px; background:rgba(0,0,0,0.6); color:#fff; padding:6px 12px; border-radius:6px; z-index:99; }
      </style>
    </head>
    <body>
      <div id="info">Web 预览</div>
      <canvas id="canvas"></canvas>
      <script>
        const key = "${key}";
        const type = "${type}";
        const name = decodeURIComponent("${name ? name : ''}");
        fetch("/file/" + key)
          .then(res => res.json())
          .then(data => {
            window.FILE_TYPE = data.type;
            window.FILE_BASE64 = data.base64;
            // 记录历史
            try {
              let historyArr = JSON.parse(localStorage.getItem('previewHistory')||'[]');
              // 避免重复
              historyArr = historyArr.filter(item => item.key !== key);
              historyArr.push({ name: name || '未命名', key, type });
              if (historyArr.length > 20) historyArr = historyArr.slice(-20);
              localStorage.setItem('previewHistory', JSON.stringify(historyArr));
            } catch(e){}
            // 动态加载viewer.js等
            const scripts = [
              '/hdr.js',
              '/ktx-parse.js',
              '/parse-exr.js',
              'https://cdn.jsdelivr.net/npm/three@0.152.2/build/three.min.js',
              '/viewer/viewer.js'
            ];
            scripts.forEach(src => {
              const s = document.createElement('script');
              s.src = src;
              document.body.appendChild(s);
            });
          });
      </script>
    </body>
    </html>
  `);
});

const port = 3000;
app.listen(port, () => {
  console.log(`Web 预览服务已启动: http://localhost:${port}/`);
}); 