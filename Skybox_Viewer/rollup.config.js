import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';

export default [
  {
    input: 'node_modules/parse-exr/index.js',
    output: {
      file: 'src/viewer/parse-exr.js',
      format: 'umd',
      name: 'parseExr', // window.parseExr
    },
    plugins: [resolve(), commonjs()],
  },
  {
    input: 'node_modules/ktx-parse/dist/ktx-parse.esm.js',
    output: {
      file: 'src/viewer/ktx-parse.js',
      format: 'umd',
      name: 'KTX2Container', // window.KTX2Container
    },
    plugins: [resolve(), commonjs()],
  },
  {
    input: 'node_modules/hdr.js/dist/hdr.js',
    output: {
      file: 'src/viewer/hdr.js',
      format: 'umd',
      name: 'HDRImage', // window.HDRImage
    },
    plugins: [resolve(), commonjs()],
  }
];
