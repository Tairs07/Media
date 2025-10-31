<template>
  <div class="login-container">
    <!-- 背景动画 -->
    <div class="background-layer">
      <GridMotion class="absolute inset-0" />
    </div>

    <!-- 登录卡片 -->
    <FadeContent :duration="1000" :blur="true">
      <el-card class="login-card">
        <template #header>
          <div class="card-header">
            <GradientText :text="'登录'" :colors="['#00ff88', '#ffd700']" />
          </div>
        </template>
        
        <el-form :model="form" @submit.prevent="handleLogin">
          <FadeContent :duration="800" :delay="200">
            <el-form-item label="用户名">
              <el-input 
                v-model="form.username" 
                placeholder="请输入用户名"
                :prefix-icon="'User'"
                class="tech-input"
              />
            </el-form-item>
          </FadeContent>

          <FadeContent :duration="800" :delay="300">
            <el-form-item label="密码">
              <el-input 
                v-model="form.password" 
                type="password" 
                placeholder="请输入密码"
                :prefix-icon="'Lock'"
                show-password
                class="tech-input"
              />
            </el-form-item>
          </FadeContent>

          <FadeContent :duration="800" :delay="400">
            <el-form-item>
              <el-button type="primary" style="width: 100%" class="tech-submit" @click="handleLogin">
                登录
              </el-button>
            </el-form-item>
          </FadeContent>

          <FadeContent :duration="800" :delay="500">
            <el-form-item>
              <el-button style="width: 100%" class="tech-secondary" @click="$router.push('/register')">
                还没有账号？立即注册
              </el-button>
            </el-form-item>
          </FadeContent>
        </el-form>
      </el-card>
    </FadeContent>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import FadeContent from '../bits-content/Animations/FadeContent/FadeContent.vue'
import GradientText from '../bits-content/TextAnimations/GradientText/GradientText.vue'
import GridDistortion from '../bits-content/Backgrounds/GridDistortion/GridDistortion.vue'

const router = useRouter()
const authStore = useAuthStore()

const form = ref({
  username: '',
  password: ''
})

const handleLogin = async () => {
  try {
    await authStore.login(form.value.username, form.value.password)
    router.push('/')
  } catch (error) {
    console.error('登录失败:', error)
  }
}
</script>

<style scoped>
.login-container {
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: calc(100vh - 80px);
  padding: 40px 4rem;
  width: 100%;
}

.background-layer {
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;
}

.login-card {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 500px;
  backdrop-filter: blur(20px);
  background: rgba(26, 26, 26, 0.9);
  border: 1px solid var(--border-color);
  box-shadow: 0 8px 32px rgba(0, 255, 136, 0.1);
}

.card-header {
  text-align: center;
}

:deep(.el-card__header) {
  padding: 32px 32px 24px;
  border-bottom: 1px solid var(--border-color);
  background: transparent;
}

:deep(.el-card__body) {
  padding: 32px;
  background: transparent;
}

:deep(.el-form-item__label) {
  color: var(--text-secondary);
  font-weight: 500;
}

:deep(.tech-input .el-input__wrapper) {
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  transition: all 0.3s ease;
}

:deep(.tech-input .el-input__wrapper:hover) {
  border-color: var(--accent-green);
}

:deep(.tech-input .el-input__wrapper.is-focus) {
  border-color: var(--accent-green);
  box-shadow: 0 0 10px rgba(0, 255, 136, 0.3);
}

:deep(.tech-submit) {
  background: var(--accent-green);
  border-color: var(--accent-green);
  color: var(--bg-primary);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
}

:deep(.tech-submit:hover) {
  background: var(--accent-green-dark);
  box-shadow: 0 0 20px rgba(0, 255, 136, 0.5);
}

:deep(.tech-secondary) {
  background: transparent;
  border-color: var(--accent-yellow);
  color: var(--accent-yellow);
  font-weight: 500;
}

:deep(.tech-secondary:hover) {
  background: rgba(255, 215, 0, 0.1);
  box-shadow: 0 0 15px rgba(255, 215, 0, 0.3);
}
</style>
