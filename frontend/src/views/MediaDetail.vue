<template>
  <div class="media-detail-container">
    <!-- 背景 -->
    <div class="background-layer">
      <GridMotion class="absolute inset-0" />
    </div>

    <!-- 内容 -->
    <div class="content-wrapper">
      <!-- 返回按钮 -->
      <FadeContent :duration="500">
        <el-button 
          class="back-button" 
          :icon="ArrowLeft" 
          @click="$router.back()"
        >
          返回
        </el-button>
      </FadeContent>

      <!-- 加载状态 -->
      <div v-if="loading" class="loading-container">
        <el-icon class="is-loading" :size="60" color="#00ff88">
          <Loading />
        </el-icon>
      </div>

      <!-- 错误状态 -->
      <div v-else-if="error" class="error-container">
        <el-icon :size="60" color="#ff4444"><Warning /></el-icon>
        <p>{{ error }}</p>
        <el-button @click="loadMediaDetail">重试</el-button>
      </div>

      <!-- 媒体详情 -->
      <div v-else-if="mediaFile" class="media-detail-content">
        <FadeContent :duration="1000">
          <div class="media-display">
            <!-- 图片显示 -->
            <div v-if="mediaFile.fileType === 'image'" class="image-container">
              <img
                :src="getFileUrl(mediaFile.filePath)"
                :alt="mediaFile.fileName"
                class="media-image"
                @click="showImageViewer = true"
              />
            </div>

            <!-- 视频显示 -->
            <div v-else-if="mediaFile.fileType === 'video'" class="video-container">
              <video
                :src="getFileUrl(mediaFile.filePath)"
                controls
                class="media-video"
              >
                您的浏览器不支持视频播放
              </video>
            </div>
          </div>
        </FadeContent>

        <FadeContent :duration="1000" :delay="200">
          <el-card class="info-card">
            <div class="media-header">
              <div class="title-section">
                <h2 class="media-title">{{ mediaFile.fileName }}</h2>
                <div class="media-meta">
                  <span class="meta-item">
                    <el-icon><Calendar /></el-icon>
                    {{ formatDate(mediaFile.uploadedAt) }}
                  </span>
                  <span class="meta-item">
                    <el-icon><View /></el-icon>
                    {{ mediaFile.viewCount }} 次浏览
                  </span>
                  <span class="meta-item">
                    <el-icon><Star /></el-icon>
                    {{ mediaFile.likeCount }} 个赞
                  </span>
                </div>
              </div>
              <div v-if="isOwner" class="owner-actions">
                <el-button type="danger" @click="handleDelete">删除</el-button>
                <el-button @click="showEditDialog = true">编辑</el-button>
              </div>
            </div>

            <div v-if="mediaFile.description" class="description-section">
              <h3>描述</h3>
              <p>{{ mediaFile.description }}</p>
            </div>

            <div v-if="mediaFile.tags" class="tags-section">
              <h3>标签</h3>
              <div class="tags-list">
                <el-tag
                  v-for="(tag, index) in mediaFile.tags.split(',')"
                  :key="index"
                  class="tag-item"
                >
                  {{ tag.trim() }}
                </el-tag>
              </div>
            </div>

            <div v-if="mediaFile.user" class="user-section">
              <div class="user-info" @click="goToUserProfile">
                <div class="user-avatar">
                  <el-icon :size="40"><UserFilled /></el-icon>
                </div>
                <div class="user-details">
                  <p class="username">{{ mediaFile.user.username }}</p>
                  <p class="upload-info">上传于 {{ formatDate(mediaFile.uploadedAt) }}</p>
                </div>
              </div>
            </div>

            <div class="file-info-section">
              <h3>文件信息</h3>
              <div class="file-info-grid">
                <div class="info-item">
                  <span class="info-label">文件大小：</span>
                  <span>{{ formatFileSize(mediaFile.fileSize) }}</span>
                </div>
                <div v-if="mediaFile.width && mediaFile.height" class="info-item">
                  <span class="info-label">尺寸：</span>
                  <span>{{ mediaFile.width }} × {{ mediaFile.height }} 像素</span>
                </div>
                <div v-if="mediaFile.duration" class="info-item">
                  <span class="info-label">时长：</span>
                  <span>{{ formatDuration(mediaFile.duration) }}</span>
                </div>
                <div class="info-item">
                  <span class="info-label">类型：</span>
                  <span>{{ mediaFile.mimeType }}</span>
                </div>
              </div>
            </div>
          </el-card>
        </FadeContent>
      </div>
    </div>

    <!-- 图片查看器 -->
    <el-dialog
      v-model="showImageViewer"
      fullscreen
      class="image-viewer-dialog"
    >
      <img
        :src="mediaFile ? getFileUrl(mediaFile.filePath) : ''"
        class="fullscreen-image"
        alt="图片预览"
      />
    </el-dialog>

    <!-- 编辑对话框 -->
    <el-dialog
      v-model="showEditDialog"
      title="编辑文件信息"
      width="500px"
    >
      <el-form :model="editForm" label-width="80px">
        <el-form-item label="描述">
          <el-input
            v-model="editForm.description"
            type="textarea"
            :rows="4"
            placeholder="输入文件描述"
          />
        </el-form-item>
        <el-form-item label="标签">
          <el-input
            v-model="editForm.tags"
            placeholder="用逗号分隔多个标签"
          />
        </el-form-item>
        <el-form-item label="公开设置">
          <el-switch
            v-model="editForm.isPublic"
            active-text="公开"
            inactive-text="私有"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditDialog = false">取消</el-button>
        <el-button type="primary" :loading="updating" @click="handleUpdate">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ArrowLeft,
  Loading,
  Warning,
  Calendar,
  View,
  Star,
  UserFilled
} from '@element-plus/icons-vue'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GridMotion from '../bits-content/Backgrounds/GridMotion/GridMotion.vue'
import api from '../utils/api'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const mediaFile = ref<any>(null)
const loading = ref(true)
const error = ref('')
const showImageViewer = ref(false)
const showEditDialog = ref(false)
const updating = ref(false)

const editForm = ref({
  description: '',
  tags: '',
  isPublic: true
})

const isOwner = computed(() => {
  return authStore.user?.id === mediaFile.value?.user?.id
})

const loadMediaDetail = async () => {
  const mediaId = route.params.id
  if (!mediaId) {
    error.value = '无效的媒体ID'
    loading.value = false
    return
  }

  loading.value = true
  error.value = ''

  try {
    const response = await api.get(`/media/${mediaId}`)
    if (response.data.success && response.data.data) {
      mediaFile.value = response.data.data
      editForm.value = {
        description: mediaFile.value.description || '',
        tags: mediaFile.value.tags || '',
        isPublic: mediaFile.value.isPublic
      }
    } else {
      error.value = response.data.error?.message || '加载失败'
    }
  } catch (err: any) {
    error.value = err.message || '加载媒体详情失败'
  } finally {
    loading.value = false
  }
}

const getFileUrl = (filePath: string): string => {
  if (filePath.startsWith('http')) {
    return filePath
  }
  const baseURL = api.defaults.baseURL || 'http://localhost:5000/api'
  return baseURL.replace('/api', '') + `/api/files/${encodeURIComponent(filePath)}`
}

const formatDate = (dateString: string): string => {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    return date.toLocaleString('zh-CN')
  } catch {
    return dateString
  }
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

const formatDuration = (seconds: number): string => {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
  }
  return `${minutes}:${secs.toString().padStart(2, '0')}`
}

const handleDelete = async () => {
  try {
    await ElMessageBox.confirm('确定要删除这个文件吗？', '确认删除', {
      type: 'warning'
    })

    const response = await api.delete(`/media/${mediaFile.value.id}`)
    if (response.data.success) {
      ElMessage.success('删除成功')
      router.push('/')
    } else {
      ElMessage.error(response.data.error?.message || '删除失败')
    }
  } catch (err: any) {
    if (err !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleUpdate = async () => {
  updating.value = true
  try {
    const response = await api.put(`/media/${mediaFile.value.id}`, editForm.value)
    if (response.data.success) {
      ElMessage.success('更新成功')
      showEditDialog.value = false
      await loadMediaDetail()
    } else {
      ElMessage.error(response.data.error?.message || '更新失败')
    }
  } catch (err: any) {
    ElMessage.error(err.message || '更新失败')
  } finally {
    updating.value = false
  }
}

const goToUserProfile = () => {
  if (mediaFile.value?.user?.id) {
    router.push(`/users/${mediaFile.value.user.id}`)
  }
}

onMounted(() => {
  loadMediaDetail()
})
</script>

<style scoped>
.media-detail-container {
  position: relative;
  min-height: calc(100vh - 80px);
  padding: 40px 4rem;
  width: 100%;
}

.background-layer {
  position: absolute;
  inset: 0;
  z-index: 0;
}

.content-wrapper {
  position: relative;
  z-index: 1;
  max-width: 1400px;
  margin: 0 auto;
  width: 100%;
}

.back-button {
  margin-bottom: 2rem;
  background: transparent;
  border-color: var(--accent-green);
  color: var(--accent-green);
}

.back-button:hover {
  background: rgba(0, 255, 136, 0.1);
}

.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
}

.error-container {
  text-align: center;
  padding: 4rem 0;
  color: var(--text-secondary);
}

.error-container p {
  margin: 1rem 0 2rem;
  font-size: 1.2rem;
}

.media-detail-content {
  display: grid;
  grid-template-columns: 1fr;
  gap: 2rem;
}

.media-display {
  width: 100%;
}

.image-container {
  width: 100%;
  background: var(--bg-secondary);
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid var(--border-color);
}

.media-image {
  width: 100%;
  height: auto;
  display: block;
  cursor: zoom-in;
  max-height: 80vh;
  object-fit: contain;
}

.video-container {
  width: 100%;
  background: var(--bg-secondary);
  border-radius: 12px;
  overflow: hidden;
  border: 1px solid var(--border-color);
}

.media-video {
  width: 100%;
  max-height: 80vh;
  display: block;
}

.info-card {
  backdrop-filter: blur(20px);
  background: rgba(26, 26, 26, 0.9);
  border: 1px solid var(--border-color);
  box-shadow: 0 8px 32px rgba(0, 255, 136, 0.1);
}

:deep(.el-card__body) {
  padding: 2rem;
}

.media-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--border-color);
}

.media-title {
  font-size: 2rem;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 1rem;
}

.media-meta {
  display: flex;
  gap: 2rem;
  flex-wrap: wrap;
  color: var(--text-secondary);
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.owner-actions {
  display: flex;
  gap: 1rem;
}

.description-section,
.tags-section,
.user-section,
.file-info-section {
  margin-bottom: 2rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--border-color);
}

.description-section:last-child,
.tags-section:last-child,
.user-section:last-child,
.file-info-section:last-child {
  border-bottom: none;
}

.description-section h3,
.tags-section h3,
.file-info-section h3 {
  color: var(--accent-green);
  font-size: 1.2rem;
  margin-bottom: 1rem;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.description-section p {
  color: var(--text-secondary);
  line-height: 1.8;
  font-size: 1.1rem;
}

.tags-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.tag-item {
  background: rgba(0, 255, 136, 0.1);
  border-color: var(--accent-green);
  color: var(--accent-green);
}

.user-info {
  display: flex;
  gap: 1rem;
  align-items: center;
  cursor: pointer;
  padding: 1rem;
  border-radius: 8px;
  transition: all 0.3s ease;
}

.user-info:hover {
  background: var(--bg-secondary);
}

.user-avatar {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent-green), var(--accent-yellow));
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--bg-primary);
  flex-shrink: 0;
}

.username {
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.25rem;
}

.upload-info {
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.file-info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.info-label {
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.info-item span:last-child {
  color: var(--text-primary);
  font-weight: 500;
}

:deep(.image-viewer-dialog) {
  background: rgba(0, 0, 0, 0.95);
}

:deep(.image-viewer-dialog .el-dialog__header) {
  background: transparent;
}

.fullscreen-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
}
</style>
