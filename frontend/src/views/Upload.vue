<template>
  <div class="upload-container">
    <!-- 背景 -->
    <div class="background-layer">
      <GridMotion class="absolute inset-0" />
    </div>

    <!-- 内容 -->
    <div class="content-wrapper">
      <FadeContent :duration="1000">
        <h1 class="page-title">
          <GradientText :text="'上传文件'" :colors="['#00ff88', '#ffd700']" />
        </h1>
      </FadeContent>

      <FadeContent :duration="1000" :delay="200">
        <el-card class="upload-card">
          <Magnet :magnetStrength="3" :padding="50">
            <div 
              class="upload-area" 
              @click="triggerFileInput"
              @dragover.prevent
              @drop.prevent="handleDrop"
            >
              <input
                ref="fileInputRef"
                type="file"
                multiple
                accept="image/*,video/*"
                style="display: none"
                @change="handleFileSelect"
              />
              
              <div v-if="!selectedFiles.length" class="upload-placeholder">
                <el-icon :size="64" color="#00ff88">
                  <UploadFilled />
                </el-icon>
                <p class="upload-text">点击或拖拽文件到此处上传</p>
                <p class="upload-hint">支持图片和视频格式</p>
              </div>

              <div v-else class="file-list">
                <div
                  v-for="(file, index) in selectedFiles"
                  :key="index"
                  class="file-item"
                >
                  <el-icon><Document /></el-icon>
                  <span class="file-name">{{ file.name }}</span>
                  <span class="file-size">{{ formatFileSize(file.size) }}</span>
                  <el-button
                    type="danger"
                    :icon="Delete"
                    circle
                    size="small"
                    @click="removeFile(index)"
                  />
                </div>
              </div>
            </div>
          </Magnet>

          <div v-if="selectedFiles.length" class="upload-options">
            <el-form :model="uploadForm" label-width="100px">
              <el-form-item label="描述">
                <el-input
                  v-model="uploadForm.description"
                  type="textarea"
                  :rows="3"
                  placeholder="可选：添加文件描述"
                />
              </el-form-item>
              <el-form-item label="标签">
                <el-input
                  v-model="uploadForm.tags"
                  placeholder="可选：用逗号分隔多个标签"
                />
              </el-form-item>
              <el-form-item label="公开设置">
                <el-switch
                  v-model="uploadForm.isPublic"
                  active-text="公开"
                  inactive-text="私有"
                />
              </el-form-item>
            </el-form>
          </div>
        </el-card>
      </FadeContent>

      <FadeContent :duration="1000" :delay="400" v-if="selectedFiles.length">
        <div class="action-buttons">
          <el-button 
            type="primary" 
            size="large" 
            class="tech-button primary" 
            :loading="uploading"
            @click="handleUpload"
          >
            {{ uploading ? '上传中...' : '开始上传' }}
          </el-button>
          <el-button 
            size="large" 
            class="tech-button secondary" 
            :disabled="uploading"
            @click="clearFiles"
          >
            清空列表
          </el-button>
        </div>
      </FadeContent>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { UploadFilled, Delete, Document } from '@element-plus/icons-vue'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GradientText from '../bits-content/TextAnimations/GradientText/GradientText.vue'
import GridMotion from '../bits-content/Backgrounds/GridMotion/GridMotion.vue'
import Magnet from '../bits-content/Animations/Magnet/Magnet.vue'
import api from '../utils/api'

const router = useRouter()
const fileInputRef = ref<HTMLInputElement>()
const selectedFiles = ref<File[]>([])
const uploading = ref(false)

const uploadForm = ref({
  description: '',
  tags: '',
  isPublic: true
})

const triggerFileInput = () => {
  fileInputRef.value?.click()
}

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    const newFiles = Array.from(target.files)
    selectedFiles.value.push(...newFiles)
  }
}

const handleDrop = (event: DragEvent) => {
  if (event.dataTransfer?.files) {
    const newFiles = Array.from(event.dataTransfer.files)
    selectedFiles.value.push(...newFiles)
  }
}

const removeFile = (index: number) => {
  selectedFiles.value.splice(index, 1)
}

const clearFiles = () => {
  selectedFiles.value = []
  uploadForm.value = {
    description: '',
    tags: '',
    isPublic: true
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

const handleUpload = async () => {
  if (selectedFiles.value.length === 0) {
    ElMessage.warning('请选择要上传的文件')
    return
  }

  uploading.value = true

  try {
    const formData = new FormData()
    
    // 添加文件
    selectedFiles.value.forEach(file => {
      formData.append('files', file)
    })

    // 添加其他字段
    if (uploadForm.value.description) {
      formData.append('description', uploadForm.value.description)
    }
    if (uploadForm.value.tags) {
      formData.append('tags', uploadForm.value.tags)
    }
    formData.append('isPublic', uploadForm.value.isPublic.toString())

    const response = await api.post('/media/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })

    if (response.data.success) {
      ElMessage.success(`成功上传 ${response.data.data.files.length} 个文件`)
      clearFiles()
      // 可选：跳转到个人中心查看上传的文件
      // router.push('/profile')
    } else {
      ElMessage.error(response.data.error?.message || '上传失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '上传失败，请稍后重试')
  } finally {
    uploading.value = false
  }
}
</script>

<style scoped>
.upload-container {
  position: relative;
  min-height: calc(100vh - 80px);
  padding: 60px 4rem;
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

.page-title {
  text-align: center;
  margin-bottom: 3rem;
  font-size: clamp(2.5rem, 4vw, 4rem);
}

.upload-card {
  backdrop-filter: blur(20px);
  background: rgba(26, 26, 26, 0.9);
  border: 1px solid var(--border-color);
  box-shadow: 0 8px 32px rgba(0, 255, 136, 0.1);
  margin-bottom: 2rem;
}

.upload-area {
  min-height: 400px;
  border: 3px dashed var(--accent-green);
  border-radius: 12px;
  padding: 60px;
  cursor: pointer;
  transition: all 0.3s ease;
  background: rgba(0, 255, 136, 0.05);
}

.upload-area:hover {
  border-color: var(--accent-yellow);
  background: rgba(0, 255, 136, 0.1);
  box-shadow: 0 0 30px rgba(0, 255, 136, 0.3);
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 1.5rem;
}

.upload-text {
  font-size: 1.4rem;
  font-weight: 600;
  color: var(--accent-green);
}

.upload-hint {
  color: var(--text-secondary);
  font-size: 1rem;
}

.upload-options {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--border-color);
}

:deep(.el-form-item__label) {
  color: var(--text-secondary);
}

:deep(.el-input__wrapper) {
  background: var(--bg-secondary);
  border-color: var(--border-color);
}

:deep(.el-textarea__inner) {
  background: var(--bg-secondary);
  border-color: var(--border-color);
  color: var(--text-primary);
}

.file-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.file-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 16px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  transition: all 0.3s ease;
}

.file-item:hover {
  border-color: var(--accent-green);
  background: rgba(0, 255, 136, 0.05);
}

.file-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--text-primary);
}

.file-size {
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.action-buttons {
  display: flex;
  gap: 2rem;
  justify-content: center;
}

.tech-button {
  padding: 1rem 2.5rem;
  font-size: 1.1rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  border: 2px solid;
  transition: all 0.3s ease;
}

.tech-button.primary {
  background: var(--accent-green);
  border-color: var(--accent-green);
  color: var(--bg-primary);
}

.tech-button.primary:hover {
  background: var(--accent-green-dark);
  box-shadow: 0 0 30px rgba(0, 255, 136, 0.5);
  transform: translateY(-2px);
}

.tech-button.secondary {
  background: transparent;
  border-color: var(--accent-yellow);
  color: var(--accent-yellow);
}

.tech-button.secondary:hover {
  background: rgba(255, 215, 0, 0.1);
  box-shadow: 0 0 20px rgba(255, 215, 0, 0.4);
  transform: translateY(-2px);
}
</style>
