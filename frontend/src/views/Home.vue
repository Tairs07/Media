<template>
  <div class="home-container">
    <!-- 背景动画层 -->
    <div class="background-layer">
      <GridMotion class="absolute inset-0" />
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
                <img
                  v-if="file.thumbnailPath"
                  :src="getFileUrl(file.thumbnailPath)"
                  :alt="file.fileName"
                  class="thumbnail-image"
                />
                <div v-else class="thumbnail-placeholder">
                  <el-icon :size="48"><Picture /></el-icon>
                </div>
                <div class="media-overlay">
                  <span class="file-type-badge">{{ file.fileType === 'image' ? '图片' : '视频' }}</span>
                </div>
              </div>
              <div class="media-info">
                <p class="media-name">{{ file.fileName }}</p>
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
import GridMotion from '../bits-content/Backgrounds/GridMotion/GridMotion.vue'
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
  fileName: string
  fileType: string
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

onMounted(() => {
  fetchMediaFiles()
})
</script>

<style scoped>
.home-container {
  position: relative;
  min-height: calc(100vh - 80px);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  width: 100%;
}

.background-layer {
  position: absolute;
  inset: 0;
  z-index: 0;
  background: var(--bg-primary);
}

.content-wrapper {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 1600px;
  margin: 0 auto;
  padding: 80px 4rem;
}

.content-layer {
  text-align: center;
  width: 100%;
}

.main-title {
  font-size: clamp(3rem, 5vw, 5rem);
  font-weight: 900;
  margin-bottom: 2rem;
  line-height: 1.2;
  letter-spacing: -0.02em;
}

.subtitle {
  font-size: clamp(1.4rem, 2vw, 2rem);
  color: var(--text-secondary);
  margin-bottom: 4rem;
  font-weight: 300;
  letter-spacing: 0.05em;
}

.action-buttons {
  display: flex;
  gap: 2rem;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 4rem;
}

.tech-button {
  position: relative;
  padding: 1rem 2.5rem;
  font-size: 1.1rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  border: 2px solid;
  transition: all 0.3s ease;
  overflow: hidden;
}

.tech-button.primary {
  background: transparent;
  border-color: var(--accent-green);
  color: var(--accent-green);
}

.tech-button.primary:hover {
  background: var(--accent-green);
  color: var(--bg-primary);
  box-shadow: 0 0 30px rgba(0, 255, 136, 0.5);
  transform: translateY(-2px);
}

.tech-button.secondary {
  background: transparent;
  border-color: var(--accent-yellow);
  color: var(--accent-yellow);
}

.tech-button.secondary:hover {
  background: var(--accent-yellow);
  color: var(--bg-primary);
  box-shadow: 0 0 30px rgba(255, 215, 0, 0.5);
  transform: translateY(-2px);
}

.button-glow {
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(0, 255, 136, 0.3), transparent);
  transition: left 0.5s;
}

.tech-button:hover .button-glow {
  left: 100%;
}

.media-section {
  margin-top: 4rem;
  text-align: left;
}

.section-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--accent-green);
  margin-bottom: 2rem;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 4rem 0;
  color: var(--accent-green);
}

.empty-state {
  text-align: center;
  padding: 4rem 0;
  color: var(--text-secondary);
  font-size: 1.2rem;
}

.media-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 2rem;
  margin-bottom: 3rem;
}

.media-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  overflow: hidden;
  cursor: pointer;
  transition: all 0.3s ease;
}

.media-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 10px 30px rgba(0, 255, 136, 0.3);
  border-color: var(--accent-green);
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
  background: linear-gradient(to bottom, transparent, rgba(0, 0, 0, 0.7));
  display: flex;
  align-items: flex-end;
  justify-content: flex-end;
  padding: 1rem;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.media-card:hover .media-overlay {
  opacity: 1;
}

.file-type-badge {
  background: var(--accent-green);
  color: var(--bg-primary);
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
}

.media-info {
  padding: 1rem;
}

.media-name {
  color: var(--text-primary);
  font-weight: 600;
  margin-bottom: 0.5rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.media-stats {
  display: flex;
  gap: 1rem;
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.media-stats span {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.load-more {
  text-align: center;
  margin-top: 2rem;
}
</style>
