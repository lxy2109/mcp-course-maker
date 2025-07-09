// self.postMessage({ type: 'progress', text: 'worker started' });
// self.importScripts('https://unpkg.com/exr@0.6.0/dist/exr.js');
// self.importScripts('https://unpkg.com/ktx-parse@1.1.0/dist/ktx-parse.umd.js');
// self.importScripts('https://unpkg.com/three@0.152.2/build/three.min.js');
// self.importScripts('https://unpkg.com/three@0.152.2/examples/js/loaders/RGBELoader.js');

// function base64ToArrayBuffer(base64) {
//   const binary = atob(base64);
//   const len = binary.length;
//   const bytes = new Uint8Array(len);
//   for (let i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
//   return bytes.buffer;
// }

// self.onmessage = async function(e) {
//   const { fileType, base64 } = e.data;
//   try {
//     self.postMessage({ type: 'progress', text: '解码中...', percent: 5 });
//     const arrayBuffer = base64ToArrayBuffer(base64);
//     self.postMessage({ type: 'progress', text: '数据已加载，准备解码...', percent: 15 });
//     if (fileType === 'exr') {
//       const loader = self.EXRLoader || self.EXR;
//       if (!loader) throw new Error('EXRLoader (self.EXR) 未加载');
//       self.postMessage({ type: 'progress', text: '正在解析EXR...', percent: 30 });
//       const exr = await loader.fromArrayBuffer(arrayBuffer);
//       self.postMessage({ type: 'progress', text: '转换像素数据...', percent: 60 });
//       const w = exr.width, h = exr.height;
//       const data = new Uint8ClampedArray(w * h * 4);
//       for (let i = 0; i < w * h; i++) {
//         data[i * 4 + 0] = Math.min(255, Math.max(0, exr.channels.R[i] * 255));
//         data[i * 4 + 1] = Math.min(255, Math.max(0, exr.channels.G[i] * 255));
//         data[i * 4 + 2] = Math.min(255, Math.max(0, exr.channels.B[i] * 255));
//         data[i * 4 + 3] = 255;
//         if (i % Math.floor((w * h) / 10) === 0) {
//           self.postMessage({ type: 'progress', text: `转换像素数据...(${Math.floor(i/(w*h)*100)}%)`, percent: 60 + Math.floor(i/(w*h)*30) });
//         }
//       }
//       self.postMessage({ type: 'progress', text: '解码完成', percent: 100 });
//       self.postMessage({ type: 'image', data, width: w, height: h });
//     } else if (fileType === 'ktx2') {
//       if (!self.KTX2Container) throw new Error('KTX2Container 未加载');
//       self.postMessage({ type: 'progress', text: '正在解析KTX2...', percent: 40 });
//       const ktx = new self.KTX2Container(new Uint8Array(arrayBuffer));
//       self.postMessage({ type: 'progress', text: '转换像素数据...', percent: 70 });
//       const level = ktx.levels[0];
//       const w = ktx.pixelWidth, h = ktx.pixelHeight;
//       const data = new Uint8ClampedArray(level.levelData);
//       self.postMessage({ type: 'progress', text: '解码完成', percent: 100 });
//       self.postMessage({ type: 'image', data, width: w, height: h });
//     } else if (fileType === 'hdr') {
//       if (!self.THREE || !self.THREE.RGBELoader) throw new Error('THREE.RGBELoader 未加载');
//       self.postMessage({ type: 'progress', text: '正在解析HDR...', percent: 40 });
//       const loader = new self.THREE.RGBELoader();
//       loader.parse(arrayBuffer, function(texData) {
//         self.postMessage({ type: 'progress', text: '转换像素数据...', percent: 70 });
//         const w = texData.width, h = texData.height;
//         const floatData = texData.data;
//         const data = new Uint8ClampedArray(w * h * 4);
//         for (let i = 0; i < w * h; i++) {
//           data[i * 4 + 0] = Math.min(255, Math.max(0, floatData[i * 3 + 0] * 255));
//           data[i * 4 + 1] = Math.min(255, Math.max(0, floatData[i * 3 + 1] * 255));
//           data[i * 4 + 2] = Math.min(255, Math.max(0, floatData[i * 3 + 2] * 255));
//           data[i * 4 + 3] = 255;
//           if (i % Math.floor((w * h) / 10) === 0) {
//             self.postMessage({ type: 'progress', text: `转换像素数据...(${Math.floor(i/(w*h)*100)}%)`, percent: 70 + Math.floor(i/(w*h)*25) });
//           }
//         }
//         self.postMessage({ type: 'progress', text: '解码完成', percent: 100 });
//         self.postMessage({ type: 'image', data, width: w, height: h });
//       });
//     } else {
//       throw new Error('不支持的文件类型');
//     }
//   } catch (e) {
//     self.postMessage({ type: 'error', text: e.message || e });
//   }
// }; 