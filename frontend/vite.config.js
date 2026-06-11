import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'node:path'

export default defineConfig({
  base: '/dist/',
  plugins: [vue()],
  build: {
    outDir: path.resolve(__dirname, 'dist'),
    emptyOutDir: true,
    manifest: true,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/index.js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'assets/index.css'
          }
          return 'assets/[name][extname]'
        }
      }
    }
  }
})