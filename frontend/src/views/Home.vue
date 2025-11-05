<template>
  <div class="home-container">
    <!-- 背景动画层 - Squares网格 -->
    <div class="background-layer">
      <Squares
        direction="diagonal"
        :speed="0.5"
        borderColor="rgba(0, 255, 136, 0.75)"
        :squareSize="50"
        hoverFillColor="rgba(0, 255, 136, 0.75)"
        style="width: 100%; height: 100%;"
      />
    </div>

    <!-- 内容层 -->
    <div class="content-wrapper">
      <FadeContent :duration="1500" :delay="200">
        <h1 class="main-title">
          <GradientText :text="'图片视频分享网站'" :colors="['#00ff88', '#ffd700', '#00ff88']" />
        </h1>
      </FadeContent>

      <FadeContent :duration="1500" :delay="400" :blur="true">
        <p class="subtitle">欢迎来到我们的分享平台</p>
      </FadeContent>

      <FadeContent :duration="1500" :delay="600">
        <div class="action-buttons">
          <el-button 
            v-if="authStore.token"
            type="primary" 
            size="large" 
            class="tech-button primary"
            @click="$router.push('/upload')"
          >
            <span class="button-glow"></span>
            开始上传
          </el-button>
          <el-button 
            v-if="!authStore.token"
            size="large" 
            class="tech-button secondary"
            @click="$router.push('/register')"
          >
            注册账号
          </el-button>
        </div>
      </FadeContent>

      <!-- 媒体文件列表 -->
      <FadeContent :duration="1500" :delay="800">
        <div class="media-section">
          <h2 class="section-title">最新上传</h2>
          
          <div v-if="loading" class="loading-container">
            <el-icon class="is-loading" :size="40">
              <Loading />
            </el-icon>
          </div>

          <div v-else-if="mediaFiles.length === 0" class="empty-state">
            <p>暂无内容</p>
          </div>

          <div v-else class="media-grid">
            <div
              v-for="file in mediaFiles"
              :key="file.id"
              class="media-card"
              @click="$router.push(`/media/${file.id}`)"
            >
              <div class="media-thumbnail">
                <!-- 有缩略图：显示缩略图 -->
                <img
                  v-if="file.thumbnailPath"
                  :src="getFileUrl(file.thumbnailPath)"
                  :alt="file.fileName"
                  class="thumbnail-image"
                  @error="handleImageError"
                />
                <!-- 图片类型但无缩略图：显示原图 -->
                <img
                  v-else-if="file.fileType === 'image' && file.filePath"
                  :src="getFileUrl(file.filePath)"
                  :alt="file.fileName"
                  class="thumbnail-image"
                  @error="handleImageError"
                />
                <!-- 视频类型：显示视频元素的第一帧 -->
                <video
                  v-else-if="file.fileType === 'video' && file.filePath"
                  :src="getFileUrl(file.filePath)"
                  class="thumbnail-image"
                  preload="metadata"
                  @error="handleVideoError"
                />
                <!-- 默认占位符 -->
                <div v-else class="thumbnail-placeholder">
                  <el-icon :size="48"><Picture /></el-icon>
                </div>
                <div class="media-overlay">
                  <span class="file-type-badge">{{ file.fileType === 'image' ? '图片' : '视频' }}</span>
                </div>
              </div>
              <div class="media-info">
                <p class="media-title">{{ file.title }}</p>
                <div class="media-stats">
                  <span><el-icon><View /></el-icon> {{ file.viewCount }}</span>
                  <span><el-icon><Star /></el-icon> {{ file.likeCount }}</span>
                </div>
              </div>
            </div>
          </div>

          <div v-if="hasMore" class="load-more">
            <el-button @click="loadMore" :loading="loadingMore">
              加载更多
            </el-button>
          </div>
        </div>
      </FadeContent>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { Loading, Picture, View, Star } from '@element-plus/icons-vue'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GradientText from '../bits-content/TextAnimations/GradientText/GradientText.vue'
import Squares from '../bits-content/Backgrounds/Squares/Squares.vue'
import api from '../utils/api'

const router = useRouter()
const authStore = useAuthStore()

const mediaFiles = ref<any[]>([])
const loading = ref(true)
const loadingMore = ref(false)
const currentPage = ref(1)
const hasMore = ref(false)

interface MediaFile {
  id: number
  title: string
  fileName: string
  fileType: string
  filePath: string
  thumbnailPath?: string
  viewCount: number
  likeCount: number
}

const fetchMediaFiles = async (page = 1, append = false) => {
  try {
    if (page === 1) {
      loading.value = true
    } else {
      loadingMore.value = true
    }

    const response = await api.get('/media', {
      params: {
        page,
        pageSize: 20,
        orderBy: 'uploadedAt',
        order: 'desc'
      }
    })

    if (response.data.success && response.data.data) {
      const data = response.data.data
      if (append) {
        mediaFiles.value.push(...data.items)
      } else {
        mediaFiles.value = data.items
      }
      hasMore.value = data.page < data.totalPages
      currentPage.value = page
    }
  } catch (error) {
    console.error('获取媒体文件失败:', error)
  } finally {
    loading.value = false
    loadingMore.value = false
  }
}

const loadMore = () => {
  if (!loadingMore.value && hasMore.value) {
    fetchMediaFiles(currentPage.value + 1, true)
  }
}

const getFileUrl = (filePath: string): string => {
  // 如果filePath是完整URL，直接返回
  if (filePath.startsWith('http')) {
    return filePath
  }
  // 否则构建API路径
  const baseURL = api.defaults.baseURL || 'http://localhost:5000/api'
  return baseURL.replace('/api', '') + `/api/files/${encodeURIComponent(filePath)}`
}

const handleImageError = (event: Event) => {
  const target = event.target as HTMLImageElement
  console.error('图片加载失败:', target.src)
  // 可以设置一个默认占位图
  target.style.display = 'none'
  const placeholder = target.nextElementSibling
  if (placeholder) {
    placeholder.classList.remove('hidden')
  }
}

const handleVideoError = (event: Event) => {
  const target = event.target as HTMLVideoElement
  console.error('视频加载失败:', target.src)
  target.style.display = 'none'
}

onMounted(() => {
  fetchMediaFiles()
})
</script>

<style scoped>
.home-container {
  position: relative;
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  width: 100%;
}

.background-layer {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  width: 100vw;
  height: 100vh;
  z-index: 0;
  background: var(--bg-primary);
}

.background-layer :deep(canvas) {
  width: 100% !important;
  height: 100% !important;
  display: block;
}

.content-wrapper {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 4rem 4rem;
  text-align: center;
}

.content-layer {
  text-align: center;
  width: 100%;
}

.main-title {
  font-size: clamp(1.8rem, 5vw, 4.5rem);
  font-weight: 400;
  margin-bottom: 2rem;
  line-height: 1.1;
  letter-spacing: -2px;
  text-shadow:
    0 0 2px rgba(255, 255, 255, 0.1),
    0 0 4px rgba(255, 255, 255, 0.3),
    0 0 8px rgba(255, 255, 255, 0.2),
    0 0 100px rgba(58, 237, 112, 0.3);
  animation: fadeInUp 1s ease-out;
}

.subtitle {
  font-size: clamp(0.9rem, 2vw, 1.4rem);
  color: var(--text-secondary);
  margin-bottom: 3rem;
  font-weight: 300;
  line-height: 1.6;
  max-width: 600px;
  margin-left: auto;
  margin-right: auto;
  animation: fadeInUp 1s ease-out 0.2s both;
}

.action-buttons {
  display: flex;
  gap: 1.5rem;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 4rem;
  animation: fadeInUp 1s ease-out 0.4s both;
}

.tech-button {
  position: relative;
  padding: 0 2rem;
  height: 55px;
  font-size: 1rem;
  font-weight: 500;
  border: none;
  border-radius: 50px;
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  overflow: hidden;
  isolation: isolate;
  display: flex;
  align-items: center;
  justify-content: center;
}

.tech-button.primary {
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
  background-size: 200% 200%;
  color: #ffffff;
  box-shadow: var(--shadow-glow), var(--shadow-lg);
  animation: glow-pulse 3s ease-in-out infinite alternate;
}

.tech-button.primary::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.4), transparent);
  transition: left 0.6s ease;
  z-index: 1;
}

.tech-button.primary:hover {
  box-shadow:
    0 0 60px rgba(58, 237, 109, 0.3),
    0 0 120px rgba(92, 246, 138, 0.2),
    0 12px 40px rgba(0, 0, 0, 0.4);
  transform: translateY(-4px) scale(1.02);
}

.tech-button.primary:hover::before {
  left: 100%;
}

.tech-button.secondary {
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
}

.tech-button.secondary:hover {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.1);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.button-glow {
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
  transition: left 0.5s;
  z-index: 1;
}

.tech-button:hover .button-glow {
  left: 100%;
}

.media-section {
  margin-top: 5rem;
  text-align: left;
  animation: fadeInUp 1s ease-out 0.6s both;
}

.section-title {
  font-size: 1.8rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 2rem;
  letter-spacing: -0.5px;
  position: relative;
  display: inline-block;
}

.section-title::after {
  content: '';
  position: absolute;
  bottom: -8px;
  left: 0;
  width: 60px;
  height: 3px;
  background: linear-gradient(90deg, var(--accent-green), var(--accent-blue));
  border-radius: 2px;
  box-shadow: 0 0 10px var(--glow-green);
}

.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 4rem 0;
  color: var(--accent-green-light);
}

.empty-state {
  text-align: center;
  padding: 4rem 0;
  color: var(--text-secondary);
  font-size: 1.1rem;
  opacity: 0.7;
}

.media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
  margin-bottom: 3rem;
}

.media-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 16px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
}

.media-card:hover {
  transform: translateY(-8px) scale(1.02);
  box-shadow:
    0 0 40px rgba(58, 237, 112, 0.2),
    0 10px 40px rgba(0, 0, 0, 0.3);
  border-color: var(--border-hover);
}

.media-thumbnail {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  background: var(--bg-secondary);
  overflow: hidden;
}

.thumbnail-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  background: var(--bg-secondary);
}

.thumbnail-image:is(video) {
  /* 视频元素特定样式 */
  pointer-events: none;
}

.thumbnail-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-secondary);
}

.media-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(
    to bottom,
    transparent 0%,
    rgba(0, 0, 0, 0.3) 50%,
    rgba(0, 0, 0, 0.8) 100%
  );
  display: flex;
  align-items: flex-end;
  justify-content: flex-end;
  padding: 1rem;
  opacity: 0;
  transition: opacity 0.4s ease;
}

.media-card:hover .media-overlay {
  opacity: 1;
}

.file-type-badge {
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue));
  color: #ffffff;
  padding: 0.3rem 0.85rem;
  border-radius: 50px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
}

.media-info {
  padding: 1.25rem;
  background: linear-gradient(to bottom, transparent, rgba(11, 11, 11, 0.3));
}

.media-title {
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.95rem;
  margin-bottom: 0.75rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.media-stats {
  display: flex;
  gap: 1.2rem;
  color: var(--text-secondary);
  font-size: 0.85rem;
}

.media-stats span {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  transition: color 0.2s ease;
}

.media-stats span:hover {
  color: var(--accent-green-light);
}

.load-more {
  text-align: center;
  margin-top: 3rem;
}

.load-more .el-button {
  padding: 0.8rem 2rem;
  border-radius: 50px;
  border: 1px solid var(--border-color);
  background: var(--glass-bg);
  color: var(--text-primary);
  font-weight: 500;
  transition: all 0.3s ease;
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
}

.load-more .el-button:hover {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.1);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

@media (max-width: 768px) {
  .content-wrapper {
    padding: 2rem 1.5rem 3rem;
  }

  .main-title {
    margin-bottom: 1.5rem;
  }

  .subtitle {
    margin-bottom: 2.5rem;
  }

  .media-section {
    margin-top: 3rem;
  }

  .section-title {
    font-size: 1.5rem;
  }

  .media-grid {
    grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
    gap: 1rem;
  }

  .media-card {
    border-radius: 12px;
  }

  .media-info {
    padding: 1rem;
  }

  .media-title {
    font-size: 0.9rem;
  }

  .media-stats {
    font-size: 0.8rem;
    gap: 1rem;
  }
}

@media (max-width: 390px) {
  .content-wrapper {
    padding: 1.5rem 1rem 2rem;
  }

  .main-title {
    margin-bottom: 1rem;
    letter-spacing: -1px;
  }

  .subtitle {
    margin-bottom: 2rem;
  }

  .action-buttons {
    flex-direction: column;
    gap: 1rem;
    margin-bottom: 2.5rem;
    width: 100%;
  }

  .tech-button {
    height: 48px;
    width: 100%;
    padding: 0 1.5rem;
    font-size: 0.95rem;
    min-height: 44px;
  }

  .media-section {
    margin-top: 2rem;
  }

  .section-title {
    font-size: 1.3rem;
  }

  .media-grid {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .media-card {
    border-radius: 10px;
  }

  .media-thumbnail {
    aspect-ratio: 16 / 9;
  }

  .media-info {
    padding: 0.875rem;
  }

  .media-title {
    font-size: 0.85rem;
    margin-bottom: 0.5rem;
  }

  .media-stats {
    font-size: 0.75rem;
  }

  .load-more .el-button {
    width: 100%;
    padding: 0.75rem 2rem;
  }
}
</style>
