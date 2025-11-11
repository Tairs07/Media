<template>
  <div class="login-container">
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
import Orb from '../bits-content/Backgrounds/Orb/Orb.vue'

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

:deep(.el-button.tech-submit),
:deep(.el-button--primary.tech-submit) {
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue)) !important;
  background-size: 200% 200%;
  border: none !important;
  border-color: var(--accent-green) !important;
  color: #ffffff !important;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  box-shadow: var(--shadow-glow), 0 0 40px rgba(58, 237, 112, 0.15) !important;
  animation: glow-pulse 3s ease-in-out infinite alternate;
  position: relative;
  overflow: hidden;
}

:deep(.el-button.tech-submit:hover),
:deep(.el-button--primary.tech-submit:hover) {
  background: linear-gradient(135deg, var(--accent-green), var(--accent-blue)) !important;
  box-shadow: 0 0 60px rgba(58, 237, 109, 0.3), 0 0 120px rgba(92, 246, 138, 0.2) !important;
  transform: translateY(-2px);
}

:deep(.el-button.tech-submit::before),
:deep(.el-button--primary.tech-submit::before) {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.3), transparent);
  transition: left 0.6s ease;
}

:deep(.el-button.tech-submit:hover::before),
:deep(.el-button--primary.tech-submit:hover::before) {
  left: 100%;
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

/* 移动端适配 */
@media (max-width: 768px) {
  .login-container {
    padding: 2rem 1.5rem;
    min-height: 100vh;
  }

  .login-card {
    max-width: 100%;
    margin: 0 auto;
  }

  :deep(.el-card__header) {
    padding: 24px 20px 20px;
  }

  :deep(.el-card__body) {
    padding: 24px 20px;
  }

  :deep(.el-form-item) {
    margin-bottom: 20px;
  }

  :deep(.el-button) {
    height: 44px;
    font-size: 0.95rem;
  }
}

@media (max-width: 390px) {
  .login-container {
    padding: 1.5rem 1rem;
  }

  .login-card {
    box-shadow: 0 4px 24px rgba(0, 255, 136, 0.08);
  }

  :deep(.el-card__header) {
    padding: 20px 16px 16px;
  }

  :deep(.el-card__body) {
    padding: 20px 16px;
  }

  :deep(.el-form-item__label) {
    font-size: 0.9rem;
    margin-bottom: 8px;
  }

  :deep(.el-input__wrapper) {
    padding: 10px 12px;
  }

  :deep(.el-input__inner) {
    font-size: 0.95rem;
  }

  :deep(.el-button) {
    height: 46px;
    font-size: 0.9rem;
    min-height: 44px;
  }

  :deep(.el-form-item) {
    margin-bottom: 18px;
  }
}
</style>

