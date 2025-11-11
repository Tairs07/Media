<template>
  <div class="upload-container">
    <!-- 背景 - Orb -->
    <div class="background-layer">
      <Orb
        :hue="100"
        :hoverIntensity="0.5"
        :rotateOnHover="true"
        :forceHoverState="false"
        style="width: 100%; height: 100%;"
      />
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

              <div v-else class="preview-container">
                <!-- 文件预览 -->
                <div class="preview-area">
                  <div
                    v-for="(file, index) in selectedFiles"
                    :key="index"
                    class="preview-item"
                  >
                    <!-- 图片预览 -->
                    <img
                      v-if="file.type.startsWith('image/')"
                      :src="previewUrls[index]"
                      :alt="file.name"
                      class="preview-media"
                    />
                    <!-- 视频预览 -->
                    <video
                      v-else-if="file.type.startsWith('video/')"
                      :src="previewUrls[index]"
                      class="preview-media"
                      controls
                    />
                    <!-- 删除按钮 -->
                    <el-button
                      class="remove-btn"
                      type="danger"
                      :icon="Delete"
                      circle
                      size="small"
                      @click.stop="removeFile(index)"
                    />
                    <div class="file-info-overlay">
                      <p class="file-name-overlay">{{ file.name }}</p>
                      <p class="file-size-overlay">{{ formatFileSize(file.size) }}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          <div v-if="selectedFiles.length" class="upload-options">
            <el-form :model="uploadForm" label-width="100px">
              <el-form-item label="标题" required>
                <el-input
                  v-model="uploadForm.title"
                  placeholder="请输入标题（必填）"
                  maxlength="200"
                  show-word-limit
                />
              </el-form-item>
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
import Orb from '../bits-content/Backgrounds/Orb/Orb.vue'
import api from '../utils/api'

const router = useRouter()
const fileInputRef = ref<HTMLInputElement>()
const selectedFiles = ref<File[]>([])
const uploading = ref(false)

const uploadForm = ref({
  title: '',
  description: '',
  tags: '',
  isPublic: true
})

// 文件预览URL
const previewUrls = ref<string[]>([])

const triggerFileInput = () => {
  fileInputRef.value?.click()
}

const handleFileSelect = (event: Event) => {
  const target = event.target as HTMLInputElement
  if (target.files) {
    const newFiles = Array.from(target.files)
    selectedFiles.value.push(...newFiles)
    // 生成预览URL
    newFiles.forEach(file => {
      const url = URL.createObjectURL(file)
      previewUrls.value.push(url)
    })
  }
}

const handleDrop = (event: DragEvent) => {
  if (event.dataTransfer?.files) {
    const newFiles = Array.from(event.dataTransfer.files)
    selectedFiles.value.push(...newFiles)
    // 生成预览URL
    newFiles.forEach(file => {
      const url = URL.createObjectURL(file)
      previewUrls.value.push(url)
    })
  }
}

const removeFile = (index: number) => {
  // 清理预览URL
  if (previewUrls.value[index]) {
    URL.revokeObjectURL(previewUrls.value[index])
  }
  selectedFiles.value.splice(index, 1)
  previewUrls.value.splice(index, 1)
}

const clearFiles = () => {
  // 清理所有预览URL
  previewUrls.value.forEach(url => URL.revokeObjectURL(url))

  selectedFiles.value = []
  previewUrls.value = []
  uploadForm.value = {
    title: '',
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

  if (!uploadForm.value.title.trim()) {
    ElMessage.warning('请输入标题')
    return
  }

  uploading.value = true

  try {
    const formData = new FormData()

    // 添加文件
    selectedFiles.value.forEach(file => {
      formData.append('files', file)
    })

    // 添加标题（必填）
    formData.append('title', uploadForm.value.title.trim())

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
  height: calc(100vh - 90px);
  padding: 1.5rem 1.5rem 2rem;
  width: 100%;
  overflow: hidden;
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
  overflow: hidden;
}

.background-layer :deep(div) {
  position: relative !important;
  width: 100% !important;
  height: 100% !important;
}

.background-layer :deep(canvas) {
  width: 100% !important;
  height: 100% !important;
  display: block;
  position: absolute !important;
  top: 50% !important;
  left: 50% !important;
  transform: translate(-50%, -50%) !important;
}

.content-wrapper {
  position: relative;
  z-index: 1;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

.page-title {
  text-align: center;
  margin-bottom: 1.5rem;
  font-size: clamp(2rem, 4vw, 2.5rem);
  font-weight: 400;
  letter-spacing: -1px;
  text-shadow:
    0 0 2px rgba(255, 255, 255, 0.1),
    0 0 4px rgba(255, 255, 255, 0.3),
    0 0 80px rgba(58, 237, 112, 0.3);
  animation: fadeInUp 1s ease-out;
}

.upload-card {
  backdrop-filter: blur(25px);
  -webkit-backdrop-filter: blur(25px);
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 16px;
  box-shadow: var(--shadow-md);
  margin-bottom: 1rem;
  overflow: hidden;
  animation: fadeInUp 1s ease-out 0.2s both;
}

:deep(.el-card__body) {
  padding: 1.5rem;
}

.upload-area {
  min-height: 300px;
  border: 2px dashed var(--border-color);
  border-radius: 12px;
  padding: 2rem;
  cursor: pointer;
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  background: rgba(30, 160, 63, 0.03);
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

.upload-area::before {
  content: '';
  position: absolute;
  inset: 0;
  background: radial-gradient(circle at center, rgba(30, 160, 63, 0.1) 0%, transparent 70%);
  opacity: 0;
  transition: opacity 0.4s ease;
}

.upload-area:hover {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.08);
  box-shadow:
    0 0 40px rgba(58, 237, 112, 0.15),
    var(--shadow-md);
  transform: scale(1.01);
}

.upload-area:hover::before {
  opacity: 1;
}

.upload-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 1rem;
  position: relative;
  z-index: 1;
}

.upload-text {
  font-size: 1.1rem;
  font-weight: 500;
  color: var(--text-primary);
  text-align: center;
}

.upload-hint {
  color: var(--text-secondary);
  font-size: 0.9rem;
  opacity: 0.8;
}

.upload-options {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--border-color);
}

:deep(.el-form-item__label) {
  color: var(--text-primary);
  font-weight: 500;
}

:deep(.el-input__wrapper) {
  background: rgba(21, 21, 21, 0.6);
  border-color: var(--border-color);
  border-radius: 12px;
  transition: all 0.3s ease;
  backdrop-filter: blur(10px);
}

:deep(.el-input__wrapper:hover),
:deep(.el-input__wrapper.is-focus) {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.05);
  box-shadow: 0 0 0 1px var(--border-hover);
}

:deep(.el-textarea__inner) {
  background: rgba(21, 21, 21, 0.6);
  border-color: var(--border-color);
  border-radius: 12px;
  color: var(--text-primary);
  transition: all 0.3s ease;
  backdrop-filter: blur(10px);
}

:deep(.el-textarea__inner:hover),
:deep(.el-textarea__inner:focus) {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.05);
  box-shadow: 0 0 0 1px var(--border-hover);
}

:deep(.el-switch.is-checked .el-switch__core) {
  background-color: var(--accent-green);
  border-color: var(--accent-green);
}

:deep(.el-input__inner) {
  color: var(--text-primary);
}

.preview-container {
  width: 100%;
  flex: 1;
  display: flex;
}

.preview-area {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
  gap: 1rem;
  width: 100%;
  align-content: start;
}

/* 单个文件时填满整个区域 */
.preview-area:has(.preview-item:only-child) {
  grid-template-columns: 1fr;
  height: 100%;
}

.preview-area:has(.preview-item:only-child) .preview-item {
  height: 100%;
  min-height: 280px;
  aspect-ratio: unset;
}

.preview-item {
  position: relative;
  aspect-ratio: 16 / 9;
  border-radius: 12px;
  overflow: hidden;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  min-height: 180px;
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
}

.preview-item:hover {
  border-color: var(--border-hover);
  box-shadow:
    0 0 30px rgba(58, 237, 112, 0.2),
    0 8px 32px rgba(0, 0, 0, 0.3);
  transform: translateY(-4px) scale(1.02);
}

.preview-media {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.remove-btn {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 10;
  opacity: 0;
  transition: opacity 0.3s ease;
}

.preview-item:hover .remove-btn {
  opacity: 1;
}

.file-info-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 12px;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.9), transparent);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.preview-item:hover .file-info-overlay {
  opacity: 1;
}

.file-name-overlay {
  color: white;
  font-size: 0.9rem;
  font-weight: 600;
  margin-bottom: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-size-overlay {
  color: var(--accent-green);
  font-size: 0.8rem;
}

.action-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  position: relative;
  z-index: 10;
  margin-top: 1rem;
  animation: fadeInUp 1s ease-out 0.4s both;
}

.tech-button {
  padding: 0 2rem;
  height: 45px;
  font-size: 0.9rem;
  font-weight: 500;
  border: none;
  border-radius: 50px;
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  overflow: hidden;
  isolation: isolate;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
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

.tech-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none !important;
}

@media (max-width: 768px) {
  .upload-container {
    padding: 2rem 1.5rem 3rem;
  }

  .page-title {
    margin-bottom: 2rem;
  }

  .upload-card {
    border-radius: 16px;
  }

  :deep(.el-card__body) {
    padding: 1.5rem;
  }

  .upload-area {
    padding: 1.5rem;
    min-height: 300px;
  }

  .upload-text {
    font-size: 1.1rem;
  }

  .upload-hint {
    font-size: 0.9rem;
  }

  .preview-area {
    grid-template-columns: 1fr;
    gap: 1rem;
  }

  .preview-item {
    min-height: 200px;
  }

  .upload-options {
    margin-top: 1.5rem;
    padding-top: 1.5rem;
  }

  /* 移动端表单标签顶部对齐 */
  :deep(.el-form-item) {
    flex-direction: column;
    align-items: flex-start;
  }

  :deep(.el-form-item__label) {
    width: 100% !important;
    text-align: left;
    margin-bottom: 8px;
  }

  :deep(.el-form-item__content) {
    width: 100%;
    margin-left: 0 !important;
  }

  .action-buttons {
    flex-direction: column;
    gap: 1rem;
    margin-top: 1.5rem;
  }

  .tech-button {
    width: 100%;
    height: 48px;
    min-height: 44px;
  }
}

@media (max-width: 390px) {
  .upload-container {
    padding: 1.5rem 1rem 2rem;
  }

  .page-title {
    margin-bottom: 1.5rem;
  }

  .upload-card {
    border-radius: 12px;
  }

  :deep(.el-card__body) {
    padding: 1.25rem;
  }

  .upload-area {
    padding: 1.25rem;
    min-height: 280px;
    border-radius: 12px;
  }

  .upload-text {
    font-size: 1rem;
  }

  .upload-hint {
    font-size: 0.85rem;
  }

  :deep(.el-icon) {
    font-size: 48px;
  }

  .upload-options {
    margin-top: 1.25rem;
    padding-top: 1.25rem;
  }

  :deep(.el-form-item__label) {
    font-size: 0.9rem;
    margin-bottom: 6px;
  }

  :deep(.el-input__wrapper),
  :deep(.el-textarea__inner) {
    border-radius: 8px;
    padding: 10px 12px;
  }

  :deep(.el-input__inner) {
    font-size: 0.95rem;
  }

  :deep(.el-textarea__inner) {
    font-size: 0.95rem;
  }

  .preview-item {
    min-height: 180px;
    border-radius: 12px;
  }

  .file-info-overlay {
    padding: 10px;
  }

  .file-name-overlay {
    font-size: 0.85rem;
  }

  .file-size-overlay {
    font-size: 0.75rem;
  }

  .tech-button {
    height: 46px;
    font-size: 0.9rem;
    padding: 0 1.5rem;
  }

  .action-buttons {
    margin-top: 1.25rem;
  }
}
</style>
