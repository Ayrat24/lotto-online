import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  base: '/dist/',
  plugins: [vue()],
  build: {
    outDir: '../wwwroot/dist',
    emptyOutDir: true,
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
