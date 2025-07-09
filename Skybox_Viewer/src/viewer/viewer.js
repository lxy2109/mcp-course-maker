(function() {
  const info = document.getElementById('info');
  info.textContent += ' [viewer.js加载]';

  // 仅网页环境显示历史预览和返回按钮
  const isWeb = window.location.protocol.startsWith('http');
  let historyBtn = null, backBtn = null;
  if (isWeb) {
    // 历史预览与返回按钮
    historyBtn = document.createElement('button');
    historyBtn.textContent = '历史预览';
    historyBtn.style.position = 'fixed';
    historyBtn.style.top = '16px';
    historyBtn.style.right = '120px';
    historyBtn.style.zIndex = 30;
    historyBtn.style.background = '#2a2d34';
    historyBtn.style.color = '#fff';
    historyBtn.style.border = 'none';
    historyBtn.style.borderRadius = '8px';
    historyBtn.style.padding = '8px 16px';
    historyBtn.style.fontSize = '1rem';
    historyBtn.style.boxShadow = '0 2px 8px 0 rgba(0,0,0,0.18)';
    historyBtn.style.cursor = 'pointer';
    historyBtn.style.opacity = '0.92';
    historyBtn.onmouseenter = () => historyBtn.style.opacity = '1';
    historyBtn.onmouseleave = () => historyBtn.style.opacity = '0.92';
    document.body.appendChild(historyBtn);

    backBtn = document.createElement('button');
    backBtn.textContent = '返回';
    backBtn.style.position = 'fixed';
    backBtn.style.top = '16px';
    backBtn.style.right = '24px';
    backBtn.style.zIndex = 30;
    backBtn.style.background = '#2a2d34';
    backBtn.style.color = '#fff';
    backBtn.style.border = 'none';
    backBtn.style.borderRadius = '8px';
    backBtn.style.padding = '8px 16px';
    backBtn.style.fontSize = '1rem';
    backBtn.style.boxShadow = '0 2px 8px 0 rgba(0,0,0,0.18)';
    backBtn.style.cursor = 'pointer';
    backBtn.style.opacity = '0.92';
    backBtn.onmouseenter = () => backBtn.style.opacity = '1';
    backBtn.onmouseleave = () => backBtn.style.opacity = '0.92';
    document.body.appendChild(backBtn);

    backBtn.onclick = () => { window.location.href = '/'; };

    // 历史弹窗
    let historyDialog = null;
    historyBtn.onclick = () => {
      if (historyDialog) { historyDialog.remove(); historyDialog = null; return; }
      historyDialog = document.createElement('div');
      historyDialog.style.position = 'fixed';
      historyDialog.style.top = '60px';
      historyDialog.style.right = '24px';
      historyDialog.style.background = '#23272e';
      historyDialog.style.color = '#fff';
      historyDialog.style.border = '1px solid #444';
      historyDialog.style.borderRadius = '8px';
      historyDialog.style.padding = '16px 20px 12px 20px';
      historyDialog.style.zIndex = 100;
      historyDialog.style.maxHeight = '60vh';
      historyDialog.style.overflowY = 'auto';
      historyDialog.style.minWidth = '260px';
      historyDialog.innerHTML = '<b>历史预览</b><br><br>';
      const list = document.createElement('ul');
      list.style.listStyle = 'none';
      list.style.padding = '0';
      list.style.margin = '0';
      const historyArr = JSON.parse(localStorage.getItem('previewHistory')||'[]');
      if (historyArr.length === 0) {
        const li = document.createElement('li');
        li.textContent = '暂无历史记录';
        list.appendChild(li);
      } else {
        historyArr.slice().reverse().forEach(item => {
          const li = document.createElement('li');
          li.style.marginBottom = '8px';
          const a = document.createElement('a');
          a.href = `/preview?type=${item.type}&key=${item.key}`;
          a.textContent = item.name + ' (' + item.type + ')';
          a.style.color = '#7ecfff';
          a.style.textDecoration = 'underline';
          a.style.cursor = 'pointer';
          li.appendChild(a);
          list.appendChild(li);
        });
      }
      historyDialog.appendChild(list);
      document.body.appendChild(historyDialog);
      // 点击弹窗外关闭
      setTimeout(()=>{
        window.addEventListener('mousedown', closeDialog, { once: true });
      }, 0);
      function closeDialog(e) {
        if (!historyDialog.contains(e.target)) {
          historyDialog.remove();
          historyDialog = null;
        }
      }
    };
  }

  // 新增两个canvas，2D和3D分离
  let canvas2d = document.getElementById('canvas2d');
  let canvas3d = document.getElementById('canvas3d');
  if (!canvas2d) {
    canvas2d = document.createElement('canvas');
    canvas2d.id = 'canvas2d';
    canvas2d.style.position = 'absolute';
    canvas2d.style.top = '0';
    canvas2d.style.left = '0';
    canvas2d.style.width = '100vw';
    canvas2d.style.height = '100vh';
    canvas2d.style.display = '';
    document.body.appendChild(canvas2d);
  }
  if (!canvas3d) {
    canvas3d = document.createElement('canvas');
    canvas3d.id = 'canvas3d';
    canvas3d.style.position = 'absolute';
    canvas3d.style.top = '0';
    canvas3d.style.left = '0';
    canvas3d.style.width = '100vw';
    canvas3d.style.height = '100vh';
    canvas3d.style.display = 'none';
    document.body.appendChild(canvas3d);
  }

  let ctx = canvas2d.getContext('2d');
  let rawImageData = null, imgWidth = 0, imgHeight = 0;
  let exposure = 1.0;
  let offsetX = 0, offsetY = 0, scale = 1.0;
  let isDragging = false, lastX = 0, lastY = 0;
  let skyboxMode = false;
  let threeRenderer = null, threeScene = null, threeCamera = null, threeMesh = null, threeTexture = null, lastImageData = null;

  // 曝光调节控件（美化卡片）
  const exposureDiv = document.createElement('div');
  exposureDiv.id = 'exposure-card';
  exposureDiv.className = 'exposure-card';
  exposureDiv.innerHTML = '曝光 <input id="exposure-slider" type="range" min="0.1" max="4" step="0.01" value="1"> <span id="exposure-value">1.00</span>';
  document.body.appendChild(exposureDiv);
  const exposureSlider = document.getElementById('exposure-slider');
  const exposureValue = document.getElementById('exposure-value');

  // 天空盒切换按钮
  const skyboxBtn = document.createElement('button');
  skyboxBtn.textContent = '天空盒预览';
  skyboxBtn.style.position = 'fixed';
  skyboxBtn.style.bottom = '24px';
  skyboxBtn.style.right = '24px';
  skyboxBtn.style.zIndex = 20;
  skyboxBtn.style.background = '#2a2d34';
  skyboxBtn.style.color = '#fff';
  skyboxBtn.style.border = 'none';
  skyboxBtn.style.borderRadius = '8px';
  skyboxBtn.style.padding = '10px 18px';
  skyboxBtn.style.fontSize = '1.08rem';
  skyboxBtn.style.boxShadow = '0 2px 8px 0 rgba(0,0,0,0.18)';
  skyboxBtn.style.cursor = 'pointer';
  skyboxBtn.style.opacity = '0.92';
  skyboxBtn.style.transition = 'background 0.2s, opacity 0.2s';
  skyboxBtn.onmouseenter = () => skyboxBtn.style.opacity = '1';
  skyboxBtn.onmouseleave = () => skyboxBtn.style.opacity = '0.92';
  document.body.appendChild(skyboxBtn);

  skyboxBtn.addEventListener('click', function() {
    skyboxMode = !skyboxMode;
    skyboxBtn.textContent = skyboxMode ? '2D预览' : '天空盒预览';
    if (skyboxMode) {
      startSkybox();
    } else {
      stopSkybox();
      render();
    }
  });

  /**
   * 垂直翻转像素数据
   * @param {Uint8ClampedArray} data - 原始像素数据
   * @param {number} w - 宽
   * @param {number} h - 高
   * @param {number} channels - 通道数（通常4）
   * @returns {Uint8ClampedArray} - 翻转后的新数据
   */
  function flipImageDataVertically(data, w, h, channels) {
    const out = new Uint8ClampedArray(data.length);
    for (let y = 0; y < h; y++) {
      const srcRow = y * w * channels;
      const dstRow = (h - 1 - y) * w * channels;
      out.set(data.subarray(srcRow, srcRow + w * channels), dstRow);
    }
    return out;
  }

  function base64ToArrayBuffer(base64) {
    const binary = atob(base64);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
    return bytes.buffer;
  }

  // 显示模式切换辅助函数
  function showImagePreviewMode() {
    const imgbox = document.getElementById('imgbox');
    if (imgbox) imgbox.style.display = 'flex';
    if (canvas2d) canvas2d.style.display = 'none';
    if (canvas3d) canvas3d.style.display = 'none';
    if (exposureDiv) exposureDiv.style.display = 'none';
    if (skyboxBtn) skyboxBtn.style.display = 'none';
  }
  function showCanvasMode() {
    const imgbox = document.getElementById('imgbox');
    if (imgbox) imgbox.style.display = 'none';
    if (canvas2d) canvas2d.style.display = '';
    if (exposureDiv) exposureDiv.style.display = '';
    if (skyboxBtn) skyboxBtn.style.display = '';
  }

  async function decodeAndShow() {
    info.textContent += ' [decodeAndShow调用]';
    try {
      info.textContent = '解码中...';
      const fileType = window.FILE_TYPE;
      const base64 = window.FILE_BASE64;
      const arrayBuffer = base64ToArrayBuffer(base64);

      if (fileType === 'exr') {
        try {
          if (!window.parseExr) throw new Error('parse-exr 未加载');
          info.textContent = '正在解析EXR...';
          // 调试：输出 parseExr 类型
          info.textContent += ' [parseExr:' + typeof window.parseExr + ']';
          const result = window.parseExr(arrayBuffer);
          info.textContent += ' [parseExr返回:' + (result && typeof result) + ']';
          const { width: w, height: h, data } = result;
          info.textContent += ` [w:${w},h:${h},data:${!!data}]`;
          let floatData;
          if (data instanceof Uint16Array) {
            floatData = new Float32Array(w * h * 4);
            for (let i = 0; i < w * h * 4; i++) {
              floatData[i] = decodeHalfFloat(data[i]);
            }
          } else if (data instanceof Float32Array) {
            floatData = data;
          } else {
            throw new Error('未知的EXR像素数据类型');
          }
          const imgData = new Uint8ClampedArray(w * h * 4);
          for (let i = 0; i < w * h; i++) {
            imgData[i * 4 + 0] = Math.min(255, Math.max(0, floatData[i * 4 + 0] * 255));
            imgData[i * 4 + 1] = Math.min(255, Math.max(0, floatData[i * 4 + 1] * 255));
            imgData[i * 4 + 2] = Math.min(255, Math.max(0, floatData[i * 4 + 2] * 255));
            imgData[i * 4 + 3] = 255;
          }
          const flipped = flipImageDataVertically(imgData, w, h, 4);
          setImageData(new ImageData(flipped, w, h), w, h);
        } catch (e) {
          info.textContent += ' [EXR异常]';
          info.textContent = 'EXR解码失败: ' + (e.message || e);
        }
      } else if (fileType === 'ktx2') {
        if (!window.KTX2Container) throw new Error('ktx-parse 未加载');
        info.textContent = '正在解析KTX2...';
        try {
          const ktx = window.KTX2Container.read(new Uint8Array(arrayBuffer));
          const level = ktx.levels[0];
          const w = ktx.pixelWidth, h = ktx.pixelHeight;
          const data = new Uint8ClampedArray(level.levelData);
          const flipped = flipImageDataVertically(data, w, h, 4);
          setImageData(new ImageData(flipped, w, h), w, h);
        } catch (e) {
          info.textContent = 'KTX2解码失败: ' + (e.message || e);
          throw e;
        }
      } else if (fileType === 'hdr') {
        if (!window.HDRImage) throw new Error('hdr.js 未加载');
        info.textContent = '正在解析HDR...';
        try {
          // 调试：打印头部ASCII
          const bytes = new Uint8Array(arrayBuffer);
          const asciiHead = Array.from(bytes.slice(0, 32)).map(c => (c >= 32 && c < 127) ? String.fromCharCode(c) : '.').join('');
          console.log('HDR头部ASCII:', asciiHead);
          // 兼容头部换行符（\r\n/\n/无换行）
          let patchedBuffer = arrayBuffer;
          if (!(bytes[0] === 0x23 && bytes[1] === 0x3F)) { // #?
            throw new Error('HDR文件头部不是#?RADIANCE');
          }
          // hdr.js 只接受严格的 #?RADIANCE\n，若是\r\n或无换行，自动修正
          let needsPatch = false;
          for (let i = 0; i < 16; i++) {
            if (bytes[i] === 0x0D && bytes[i + 1] === 0x0A) { // \r\n
              needsPatch = true;
              break;
            }
          }
          if (needsPatch) {
            // 替换所有\r\n为\n
            const arr = Array.from(bytes);
            for (let i = 0; i < arr.length - 1; i++) {
              if (arr[i] === 0x0D && arr[i + 1] === 0x0A) {
                arr.splice(i, 1); // 删除\r
              }
            }
            patchedBuffer = (new Uint8Array(arr)).buffer;
          }
          // 解析
          const result = window.HDRImage.read(new Uint8Array(patchedBuffer));
          if (typeof result === 'string') throw new Error('HDR解码失败: ' + result + ' [头部:' + asciiHead + "]");
          const w = result.width, h = result.height;
          const floatData = result.rgbFloat;
          if (!floatData) throw new Error('HDR像素数据为空');
          if (floatData.length !== w * h * 3) throw new Error('HDR像素数据长度异常');
          const data = new Uint8ClampedArray(w * h * 4);
          for (let i = 0; i < w * h; i++) {
            data[i * 4 + 0] = Math.min(255, Math.max(0, floatData[i * 3 + 0] * 255));
            data[i * 4 + 1] = Math.min(255, Math.max(0, floatData[i * 3 + 1] * 255));
            data[i * 4 + 2] = Math.min(255, Math.max(0, floatData[i * 3 + 2] * 255));
            data[i * 4 + 3] = 255;
          }
          setImageData(new ImageData(data, w, h), w, h);
        } catch (e) {
          info.textContent = 'HDR解码失败: ' + (e.message || e);
          throw e;
        }
      } else if (fileType === 'jpg' || fileType === 'png') {
        // 新增：JPG/PNG 用canvas解码base64为像素数据
        info.textContent = '正在解析JPG/PNG...';
        const img = new window.Image();
        img.onload = function() {
          const w = img.width, h = img.height;
          const canvas = document.createElement('canvas');
          canvas.width = w;
          canvas.height = h;
          const ctx2 = canvas.getContext('2d');
          ctx2.drawImage(img, 0, 0);
          const imageData = ctx2.getImageData(0, 0, w, h);
          // 赋值rawImageData并显示
          setImageData(imageData, w, h);
          // showImagePreviewMode(); // 移除，保持canvas和控件显示，支持天空盒
        };
        img.onerror = function() {
          info.textContent = 'JPG/PNG解码失败';
        };
        img.src = 'data:image/' + fileType + ';base64,' + base64;
        return; // 异步，后续流程在onload中执行
      } else {
        throw new Error('不支持的文件类型');
      }
    } catch (e) {
      info.textContent += ' [异常]';
      info.textContent = '解码失败: ' + (e.message || e);
    }
  }

  function render() {
    if (!rawImageData) return;
    canvas2d.width = imgWidth * scale;
    canvas2d.height = imgHeight * scale;
    // 色彩映射（曝光调整）
    const mapped = ctx.createImageData(imgWidth, imgHeight);
    for (let i = 0; i < imgWidth * imgHeight; i++) {
      mapped.data[i * 4 + 0] = Math.min(255, Math.max(0, rawImageData[i * 4 + 0] * exposure));
      mapped.data[i * 4 + 1] = Math.min(255, Math.max(0, rawImageData[i * 4 + 1] * exposure));
      mapped.data[i * 4 + 2] = Math.min(255, Math.max(0, rawImageData[i * 4 + 2] * exposure));
      mapped.data[i * 4 + 3] = rawImageData[i * 4 + 3];
    }
    const temp = document.createElement('canvas');
    temp.width = imgWidth;
    temp.height = imgHeight;
    temp.getContext('2d').putImageData(mapped, 0, 0);
    ctx.setTransform(scale, 0, 0, scale, offsetX, offsetY);
    ctx.clearRect(0, 0, canvas2d.width, canvas2d.height);
    ctx.drawImage(temp, 0, 0);
    // 显示2D，隐藏3D
    canvas2d.style.display = '';
    canvas3d.style.display = 'none';
    bindWheel(); // 2D模式下绑定滚轮
  }

  function setImageData(imageData, w, h) {
    rawImageData = new Uint8ClampedArray(imageData.data);
    imgWidth = w;
    imgHeight = h;
    offsetX = 0;
    offsetY = 0;
    scale = 1.0;
    render();
    info.style.display = 'none';
    showCanvasMode(); // 非JPG/PNG时显示canvas和控件
  }

  // 鼠标滚轮缩放交互（仅2D模式下绑定）
  function onWheel(e) {
    e.preventDefault();
    const prevScale = scale;
    scale *= e.deltaY < 0 ? 1.1 : 0.9;
    scale = Math.max(0.1, Math.min(10, scale));
    // 缩放中心跟随鼠标
    const rect = canvas2d.getBoundingClientRect();
    const mx = e.clientX - rect.left - offsetX;
    const my = e.clientY - rect.top - offsetY;
    offsetX -= (scale - prevScale) * mx;
    offsetY -= (scale - prevScale) * my;
    render();
  }
  function bindWheel() {
    canvas2d.addEventListener('wheel', onWheel);
  }
  function unbindWheel() {
    canvas2d.removeEventListener('wheel', onWheel);
  }
  bindWheel();

  // 交互：拖拽
  canvas2d.addEventListener('mousedown', function(e) {
    isDragging = true;
    lastX = e.clientX;
    lastY = e.clientY;
  });
  window.addEventListener('mousemove', function(e) {
    if (!isDragging) return;
    offsetX += e.clientX - lastX;
    offsetY += e.clientY - lastY;
    lastX = e.clientX;
    lastY = e.clientY;
    render();
  });
  window.addEventListener('mouseup', function() {
    isDragging = false;
  });
  // 曝光调节
  exposureSlider.addEventListener('input', function() {
    exposure = parseFloat(exposureSlider.value);
    exposureValue.textContent = exposure.toFixed(2);
    render();
  });

  function decodeHalfFloat(binary) {
    var exponent = (binary & 0x7C00) >> 10,
        fraction = binary & 0x03FF;
    return (binary >> 15 ? -1 : 1) *
      (exponent ?
        (exponent === 0x1F ?
          fraction ? NaN : Infinity :
          Math.pow(2, exponent - 15) * (1 + fraction / 0x400)) :
        6.103515625e-5 * (fraction / 0x400));
  }

  /**
   * 解析 HDR 文件
   * @param {ArrayBuffer} arrayBuffer - HDR 文件的二进制数据
   * @throws {Error} 解析失败时抛出
   * @returns {void}
   */

  function getExposedImageData() {
    const mapped = new Uint8Array(rawImageData.length);
    for (let i = 0; i < rawImageData.length; i += 4) {
      mapped[i] = Math.min(255, Math.max(0, rawImageData[i] * exposure));
      mapped[i+1] = Math.min(255, Math.max(0, rawImageData[i+1] * exposure));
      mapped[i+2] = Math.min(255, Math.max(0, rawImageData[i+2] * exposure));
      mapped[i+3] = rawImageData[i+3];
    }
    return mapped;
  }

  // 更严格的WebGL可用性检测函数
  function isWebGLReallyAvailable() {
    try {
      const canvas = document.createElement('canvas');
      const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
      if (!gl) return false;
      // 检查常用扩展
      const ok = !!gl.getExtension('OES_element_index_uint');
      gl.getExtension = null;
      return ok;
    } catch (e) {
      return false;
    }
  }

  function startSkybox() {
    if (!isWebGLReallyAvailable()) {
      info.style.display = '';
      info.textContent = '[skybox] 当前环境不支持WebGL，无法天空盒预览';
      // 状态同步
      skyboxMode = false;
      if (skyboxBtn) skyboxBtn.textContent = '天空盒预览';
      return;
    }
    try {
      if (!rawImageData) {
        info.style.display = '';
        info.textContent = '[skybox] rawImageData为空';
        console.warn('[skybox] rawImageData为空');
        throw new Error('rawImageData为空');
      }
      if (!window.THREE) {
        info.style.display = '';
        info.textContent = 'three.js 未加载，无法天空盒预览';
        console.warn('[skybox] three.js 未加载');
        throw new Error('three.js 未加载');
      }
      // 切换canvas显示
      showCanvasMode(); // 天空盒模式下也只显示canvas和控件
      canvas2d.style.display = 'none';
      canvas3d.style.display = '';
      unbindWheel(); // 天空盒模式下禁用滚轮
      // 重新创建WebGLRenderer，确保干净
      if (threeRenderer) {
        threeRenderer.dispose && threeRenderer.dispose();
        threeRenderer = null;
      }
      threeRenderer = new window.THREE.WebGLRenderer({canvas: canvas3d, antialias: true});
      threeRenderer.setClearColor(0x23272e);
      canvas3d.width = window.innerWidth;
      canvas3d.height = window.innerHeight;
      threeRenderer.setSize(canvas3d.width, canvas3d.height, false);
      threeScene = new window.THREE.Scene();
      const fov = 75;
      const aspect = canvas3d.width / canvas3d.height;
      threeCamera = new window.THREE.PerspectiveCamera(fov, aspect, 0.1, 100);
      threeCamera.position.set(0, 0, 0);
      if (threeTexture) threeTexture.dispose();
      const texData = getExposedImageData();
      let tex;
      try {
        tex = new window.THREE.DataTexture(texData, imgWidth, imgHeight, window.THREE.RGBAFormat);
        tex.needsUpdate = true;
        tex.flipY = true;
      } catch (err) {
        info.style.display = '';
        info.textContent = '[skybox] DataTexture 创建失败: ' + (err.message || err);
        stopSkybox();
        render();
        return;
      }
      threeTexture = tex;
      let geometry, material;
      try {
        geometry = new window.THREE.SphereGeometry(50, 64, 32);
        geometry.scale(-1, 1, 1);
        material = new window.THREE.MeshBasicMaterial({map: threeTexture});
        threeMesh = new window.THREE.Mesh(geometry, material);
        threeScene.add(threeMesh);
      } catch (err) {
        info.style.display = '';
        info.textContent = '[skybox] three.js 场景/材质创建失败: ' + (err.message || err);
        stopSkybox();
        render();
        return;
      }
      // 交互
      let lon = 0, lat = 0, isDrag = false, lastX = 0, lastY = 0;
      function onPointerDown(e) {
        isDrag = true;
        lastX = e.clientX;
        lastY = e.clientY;
      }
      function onPointerMove(e) {
        if (!isDrag) return;
        lon -= (e.clientX - lastX) * 0.15;
        lat += (e.clientY - lastY) * 0.15;
        lat = Math.max(-85, Math.min(85, lat));
        lastX = e.clientX;
        lastY = e.clientY;
      }
      function onPointerUp() { isDrag = false; }
      canvas3d.removeEventListener('mousedown', onPointerDown);
      window.removeEventListener('mousemove', onPointerMove);
      window.removeEventListener('mouseup', onPointerUp);
      canvas3d.addEventListener('mousedown', onPointerDown);
      window.addEventListener('mousemove', onPointerMove);
      window.addEventListener('mouseup', onPointerUp);
      // 曝光同步
      function updateExposure() {
        if (threeTexture && rawImageData) {
          const mapped = new Uint8ClampedArray(rawImageData.length);
          for (let i = 0; i < rawImageData.length; i += 4) {
            mapped[i] = Math.min(255, Math.max(0, rawImageData[i] * exposure));
            mapped[i+1] = Math.min(255, Math.max(0, rawImageData[i+1] * exposure));
            mapped[i+2] = Math.min(255, Math.max(0, rawImageData[i+2] * exposure));
            mapped[i+3] = rawImageData[i+3];
          }
          threeTexture.image.data.set(mapped);
          threeTexture.needsUpdate = true;
        }
      }
      exposureSlider.removeEventListener('input', updateExposure);
      exposureSlider.addEventListener('input', updateExposure);
      // 渲染循环
      function animate() {
        if (!skyboxMode) {
          return;
        }
        try {
          // 相机朝向
          const phi = window.THREE.MathUtils.degToRad(90 - lat);
          const theta = window.THREE.MathUtils.degToRad(lon);
          threeCamera.lookAt(
            50 * Math.sin(phi) * Math.cos(theta),
            50 * Math.cos(phi),
            50 * Math.sin(phi) * Math.sin(theta)
          );
          threeRenderer.render(threeScene, threeCamera);
        } catch (err) {
          info.style.display = '';
          info.textContent = '[skybox] 渲染失败: ' + (err.message || err);
          stopSkybox();
          render();
          return;
        }
        requestAnimationFrame(animate);
      }
      animate();
      updateExposure();
      info.style.display = 'none';
    } catch (e) {
      info.textContent = '[skybox] 启动失败: ' + (e.message || e);
      stopSkybox();
      render();
    }
  }
  function stopSkybox() {
    if (threeRenderer) {
      threeRenderer.clear();
    }
    // 解绑事件（防止残留）
    canvas3d.onmousedown = null;
    window.onmousemove = null;
    window.onmouseup = null;
    // 释放three对象
    if (threeTexture) { threeTexture.dispose(); threeTexture = null; }
    if (threeMesh && threeScene) { threeScene.remove(threeMesh); threeMesh = null; }
    threeScene = null;
    threeCamera = null;
    // 状态同步
    skyboxMode = false;
    if (skyboxBtn) skyboxBtn.textContent = '天空盒预览';
    // 显示2D，隐藏3D
    canvas2d.style.display = '';
    canvas3d.style.display = 'none';
    bindWheel(); // 2D模式下绑定滚轮
  }

  decodeAndShow();
})(); 