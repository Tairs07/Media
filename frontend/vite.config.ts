import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'

export default defineConfig({
  plugins: [
    vue(),
    // Element Plus 自动导入
    AutoImport({
      resolvers: [ElementPlusResolver()],
      dts: 'src/auto-imports.d.ts',
    }),
    // Element Plus 组件自动导入
    Components({
      resolvers: [ElementPlusResolver()],
      dts: 'src/components.d.ts',
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src')
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  },
  build: {
    rollupOptions: {
      output: {
        // 将 JS 文件输出到 js 目录
        entryFileNames: 'js/[name]-[hash].js',
        chunkFileNames: 'js/[name]-[hash].js',
        assetFileNames: (assetInfo) => {
          // CSS 文件放到 css 目录
          if (assetInfo.name?.endsWith('.css')) {
            return 'css/[name]-[hash][extname]'
          }
          // 图片等其他资源放到 assets 目录
          return 'assets/[name]-[hash][extname]'
        },
        manualChunks: (id) => {
          // 将 node_modules 中的包分割成单独的 chunk
          if (id.includes('node_modules')) {
            // Element Plus 单独打包
            if (id.includes('element-plus')) {
              return 'element-plus'
            }
            // Three.js 及相关库单独打包
            if (id.includes('three') || id.includes('ogl')) {
              return 'three'
            }
            // GSAP 动画库单独打包
            if (id.includes('gsap')) {
              return 'gsap'
            }
            // Markdown 相关库单独打包（主要用于 AI 聊天）
            if (id.includes('markdown-it') || id.includes('highlight.js')) {
              return 'markdown'
            }
            // Vue 核心库打��在一起
            if (id.includes('vue') || id.includes('pinia') || id.includes('vue-router')) {
              return 'vue-vendor'
            }
            // Axios 单独打包
            if (id.includes('axios')) {
              return 'axios'
            }
            // 其他第三方库打包为 vendor
            return 'vendor'
          }
          // bits-content 展示组件单独打包
          if (id.includes('bits-content')) {
            // 可以进一步细分，比如按类别分割
            if (id.includes('bits-content/Animations')) {
              return 'bits-animations'
            }
            if (id.includes('bits-content/Backgrounds')) {
              return 'bits-backgrounds'
            }
            if (id.includes('bits-content/Components')) {
              return 'bits-components'
            }
            if (id.includes('bits-content/TextAnimations')) {
              return 'bits-text'
            }
            return 'bits-content'
          }
        }
      }
    },
    // 适当增加 chunk 大小警告阈值（可选）
    chunkSizeWarningLimit: 600
  }
})



