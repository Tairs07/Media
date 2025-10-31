<template>
  <div class="profile-container">
    <!-- 背景 -->
    <div class="background-layer">
      <GridMotion class="absolute inset-0" />
    </div>

    <!-- 内容 -->
    <div class="content-wrapper">
      <FadeContent :duration="1000">
        <h1 class="page-title">
          <GradientText :text="'个人中心'" :colors="['#00ff88', '#ffd700']" />
        </h1>
      </FadeContent>

      <div class="profile-grid">
        <!-- 用户信息卡片 -->
        <FadeContent :duration="1000" :delay="200">
          <el-card class="profile-card">
            <template #header>
              <div class="card-title">个人信息</div>
            </template>
            <Magnet>
              <div class="user-info">
                <div class="avatar-placeholder">
                  <el-icon :size="64"><UserFilled /></el-icon>
                </div>
                <div class="info-details">
                  <p><strong>用户名：</strong>{{ userInfo.username || '加载中...' }}</p>
                  <p><strong>邮箱：</strong>{{ userInfo.email || '加载中...' }}</p>
                  <p><strong>注册时间：</strong>{{ formatDate(userInfo.createdAt) || '未知' }}</p>
                </div>
              </div>
            </Magnet>
            <div class="edit-section">
              <el-button type="primary" @click="showEditDialog = true">
                编辑资料
              </el-button>
            </div>
          </el-card>
        </FadeContent>

        <!-- 我的上传 -->
        <FadeContent :duration="1000" :delay="400">
          <el-card class="profile-card">
            <template #header>
              <div class="card-title">我的上传</div>
            </template>
            <div class="upload-stats">
              <div class="stat-item">
                <div class="stat-value">{{ userStats.totalUploads || 0 }}</div>
                <div class="stat-label">总上传数</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ userStats.imageCount || 0 }}</div>
                <div class="stat-label">图片</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ userStats.videoCount || 0 }}</div>
                <div class="stat-label">视频</div>
              </div>
            </div>
          </el-card>
        </FadeContent>
      </div>

      <!-- 操作按钮 -->
      <FadeContent :duration="1000" :delay="600">
        <div class="action-section">
          <el-button type="primary" size="large" class="tech-button primary" @click="$router.push('/upload')">
            上传新文件
          </el-button>
          <el-button type="danger" size="large" class="tech-button danger" @click="handleLogout">
            退出登录
          </el-button>
        </div>
      </FadeContent>
    </div>

    <!-- 编辑资料对话框 -->
    <el-dialog
      v-model="showEditDialog"
      title="编辑资料"
      width="500px"
      class="edit-dialog"
    >
      <el-form :model="editForm" label-width="80px">
        <el-form-item label="头像URL">
          <el-input v-model="editForm.avatarUrl" placeholder="输入头像URL" />
        </el-form-item>
        <el-form-item label="个人简介">
          <el-input
            v-model="editForm.bio"
            type="textarea"
            :rows="4"
            placeholder="介绍一下自己..."
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showEditDialog = false">取消</el-button>
        <el-button type="primary" :loading="updating" @click="handleUpdateProfile">
          保存
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ElMessage } from 'element-plus'
import { UserFilled } from '@element-plus/icons-vue'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GradientText from '../bits-content/TextAnimations/GradientText/GradientText.vue'
import GridMotion from '../bits-content/Backgrounds/GridMotion/GridMotion.vue'
import Magnet from '../bits-content/Animations/Magnet/Magnet.vue'
import api from '../utils/api'

const router = useRouter()
const authStore = useAuthStore()

const userInfo = ref({
  id: 0,
  username: '',
  email: '',
  avatarUrl: '',
  bio: '',
  createdAt: ''
})

const userStats = ref({
  totalUploads: 0,
  imageCount: 0,
  videoCount: 0
})

const showEditDialog = ref(false)
const updating = ref(false)

const editForm = ref({
  avatarUrl: '',
  bio: ''
})

const fetchUserProfile = async () => {
  if (!authStore.user?.id) {
    router.push('/login')
    return
  }

  try {
    const response = await api.get(`/users/${authStore.user.id}`)
    if (response.data.success && response.data.data) {
      const data = response.data.data
      userInfo.value = {
        id: data.id,
        username: data.username,
        email: data.email,
        avatarUrl: data.avatarUrl || '',
        bio: data.bio || '',
        createdAt: data.createdAt
      }
      userStats.value = {
        totalUploads: data.totalUploads || 0,
        imageCount: data.imageCount || 0,
        videoCount: data.videoCount || 0
      }
      
      // 同步编辑表单
      editForm.value = {
        avatarUrl: data.avatarUrl || '',
        bio: data.bio || ''
      }
    }
  } catch (error: any) {
    ElMessage.error(error.message || '获取用户信息失败')
  }
}

const handleUpdateProfile = async () => {
  if (!authStore.user?.id) return

  updating.value = true
  try {
    const response = await api.put(`/users/${authStore.user.id}`, {
      avatarUrl: editForm.value.avatarUrl,
      bio: editForm.value.bio
    })

    if (response.data.success && response.data.data) {
      ElMessage.success('更新成功')
      showEditDialog.value = false
      // 刷新用户信息
      await fetchUserProfile()
      // 更新store中的用户信息
      await authStore.fetchUser()
    } else {
      ElMessage.error(response.data.error?.message || '更新失败')
    }
  } catch (error: any) {
    ElMessage.error(error.message || '更新失败')
  } finally {
    updating.value = false
  }
}

const handleLogout = () => {
  authStore.logout()
  router.push('/login')
}

const formatDate = (dateString: string): string => {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString('zh-CN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  } catch {
    return dateString
  }
}

onMounted(() => {
  fetchUserProfile()
})
</script>

<style scoped>
.profile-container {
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
  max-width: 1600px;
  margin: 0 auto;
  width: 100%;
}

.page-title {
  text-align: center;
  margin-bottom: 4rem;
  font-size: clamp(2.5rem, 4vw, 4rem);
}

.profile-grid {
  display: grid;
  gap: 2.5rem;
  grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
  margin-bottom: 3rem;
}

.profile-card {
  backdrop-filter: blur(20px);
  background: rgba(26, 26, 26, 0.9);
  border: 1px solid var(--border-color);
  box-shadow: 0 8px 32px rgba(0, 255, 136, 0.1);
  transition: all 0.3s ease;
}

.profile-card:hover {
  box-shadow: 0 12px 48px rgba(0, 255, 136, 0.2);
  border-color: var(--accent-green);
}

:deep(.el-card__header) {
  padding: 24px;
  border-bottom: 1px solid var(--border-color);
  background: transparent;
}

:deep(.el-card__body) {
  padding: 24px;
  background: transparent;
}

.card-title {
  font-size: 1.3rem;
  font-weight: 600;
  color: var(--accent-green);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.user-info {
  display: flex;
  gap: 2rem;
  align-items: center;
  margin-bottom: 2rem;
}

.avatar-placeholder {
  width: 120px;
  height: 120px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--accent-green) 0%, var(--accent-yellow) 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--bg-primary);
  box-shadow: 0 0 30px rgba(0, 255, 136, 0.3);
  flex-shrink: 0;
}

.info-details {
  flex: 1;
}

.info-details p {
  margin: 1rem 0;
  color: var(--text-secondary);
  font-size: 1.1rem;
}

.info-details strong {
  color: var(--text-primary);
  font-weight: 600;
}

.edit-section {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--border-color);
}

.upload-stats {
  display: flex;
  justify-content: space-around;
  gap: 2rem;
  padding: 1rem 0;
}

.stat-item {
  text-align: center;
  flex: 1;
}

.stat-value {
  font-size: 3rem;
  font-weight: 900;
  background: linear-gradient(135deg, var(--accent-green), var(--accent-yellow));
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
  line-height: 1;
}

.stat-label {
  color: var(--text-secondary);
  font-size: 1rem;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.action-section {
  display: flex;
  gap: 1.5rem;
  justify-content: center;
  flex-wrap: wrap;
}

.tech-button {
  padding: 1rem 2rem;
  font-size: 1rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  border: 2px solid;
  transition: all 0.3s ease;
  min-width: 150px;
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

.tech-button.danger {
  background: transparent;
  border-color: #ff4444;
  color: #ff4444;
}

.tech-button.danger:hover {
  background: rgba(255, 68, 68, 0.1);
  box-shadow: 0 0 20px rgba(255, 68, 68, 0.4);
  transform: translateY(-2px);
}

:deep(.edit-dialog) {
  background: var(--bg-secondary);
}

:deep(.edit-dialog .el-dialog) {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
}

:deep(.edit-dialog .el-dialog__header) {
  border-bottom: 1px solid var(--border-color);
  color: var(--text-primary);
}

:deep(.edit-dialog .el-input__wrapper) {
  background: var(--bg-secondary);
  border-color: var(--border-color);
}

:deep(.edit-dialog .el-textarea__inner) {
  background: var(--bg-secondary);
  border-color: var(--border-color);
  color: var(--text-primary);
}
</style>
