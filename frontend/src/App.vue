<template>
  <div id="app">
    <!-- 导航栏 -->
    <nav class="navbar">
      <div class="nav-container">
        <router-link to="/" class="nav-logo">
          <GradientText :text="'MediaShare'" :colors="['#00ff88', '#ffd700']" />
        </router-link>
        
        <div class="nav-menu">
          <router-link to="/" class="nav-link">首页</router-link>
          <router-link v-if="authStore.token" to="/upload" class="nav-link">上传</router-link>
          <router-link v-if="authStore.token" to="/chat" class="nav-link">AI对话</router-link>
          <router-link v-if="authStore.token" to="/profile" class="nav-link">个人中心</router-link>
          <template v-if="!authStore.token">
            <router-link to="/login" class="nav-link">登录</router-link>
            <router-link to="/register" class="nav-link">注册</router-link>
          </template>
          <template v-else>
            <el-dropdown @command="handleCommand">
              <span class="user-menu">
                <el-icon><UserFilled /></el-icon>
                <span class="username">{{ authStore.user?.username }}</span>
                <el-icon><ArrowDown /></el-icon>
              </span>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="profile">个人中心</el-dropdown-item>
                  <el-dropdown-item command="upload">上传文件</el-dropdown-item>
                  <el-dropdown-item command="chat">AI对话</el-dropdown-item>
                  <el-dropdown-item divided command="logout">退出登录</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </div>
      </div>
    </nav>

    <!-- 主内容 -->
    <main class="main-content">
      <router-view />
    </main>

    <!-- 底部导航栏（移动端） -->
    <BottomNavigation />
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { UserFilled, ArrowDown } from '@element-plus/icons-vue'
import GradientText from './bits-content/TextAnimations/GradientText/GradientText.vue'
import BottomNavigation from './components/BottomNavigation.vue'

const router = useRouter()
const authStore = useAuthStore()

onMounted(() => {
  if (authStore.token) {
    authStore.fetchUser()
  }
})

const handleCommand = (command: string) => {
  switch (command) {
    case 'profile':
      router.push('/profile')
      break
    case 'upload':
      router.push('/upload')
      break
    case 'chat':
      router.push('/chat')
      break
    case 'logout':
      authStore.logout()
      router.push('/login')
      break
  }
}
</script>

<style>
html, body {
  overflow-x: hidden;
}

#app {
  min-height: 100vh;
  background: var(--bg-primary);
}

/* 聊天页面时防止body滚动 */
body:has(.ai-chat-container) {
  overflow: hidden;
}

.navbar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background: linear-gradient(to bottom, rgba(11, 11, 11, 0.95), transparent);
  backdrop-filter: blur(25px);
  -webkit-backdrop-filter: blur(25px);
  border-bottom: 1px solid var(--border-color);
  box-shadow: var(--shadow-md);
  height: 90px;
  display: flex;
  align-items: center;
}

.nav-container {
  margin: 0 auto;
  padding: 0 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.nav-logo {
  text-decoration: none;
  font-size: 1.6rem;
  font-weight: 700;
  position: relative;
  z-index: 2;
}

.nav-logo::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 180px;
  height: 80px;
  background: transparent;
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  mask: radial-gradient(ellipse at center, black 0%, black 20%, transparent 80%);
  -webkit-mask: radial-gradient(ellipse at center, black 0%, black 20%, transparent 80%);
  z-index: -1;
  pointer-events: none;
}

.nav-menu {
  display: flex;
  gap: 2rem;
  align-items: center;
}

.nav-link {
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: 400;
  font-size: 0.95rem;
  transition: all 0.3s ease;
  position: relative;
  padding: 0.5rem 0;
  opacity: 0.7;
}

.nav-link:hover {
  color: var(--text-primary);
  opacity: 1;
  transform: translateY(-1px);
}

.nav-link.router-link-active {
  color: var(--text-primary);
  opacity: 1;
}

.nav-link.router-link-active::before {
  content: '';
  position: absolute;
  width: 6px;
  height: 6px;
  background-color: var(--accent-green-light);
  border-radius: 50%;
  left: -12px;
  top: 50%;
  transform: translateY(-50%);
  box-shadow: 0 0 10px var(--accent-green-light);
}

.main-content {
  width: 100%;
  padding-top: 90px;
}

/* AI对话页面特殊处理 - 不需要最小高度 */
.main-content:has(.ai-chat-container) {
  min-height: 0;
  padding-top: 90px;
}

/* 移动端为底部导航预留空间 */
@media (max-width: 768px) {
  .main-content {
    padding-bottom: 60px;
  }
}

.user-menu {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.2rem;
  height: 45px;
  border: 1px solid var(--border-color);
  background: var(--glass-bg);
  border-radius: 50px;
  cursor: pointer;
  transition: all 0.3s ease;
  color: var(--text-primary);
  backdrop-filter: blur(15px);
  -webkit-backdrop-filter: blur(15px);
}

.user-menu:hover {
  border-color: var(--border-hover);
  background: rgba(30, 160, 63, 0.05);
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.username {
  font-weight: 500;
  font-size: 0.9rem;
}

/* 全局下拉菜单样式 - 适用于所有页面 */
.el-dropdown-menu {
  background: rgba(26, 26, 26, 0.98) !important;
  border: 1px solid rgba(0, 255, 136, 0.3) !important;
  border-radius: 8px !important;
  backdrop-filter: blur(20px) !important;
  -webkit-backdrop-filter: blur(20px) !important;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5), 0 0 24px rgba(0, 255, 136, 0.25) !important;
  padding: 8px !important;
}

.el-dropdown-menu__item {
  background: transparent !important;
  color: #e0e0e0 !important;
  font-weight: 500 !important;
  padding: 10px 16px !important;
  border-radius: 8px !important;
  margin-bottom: 4px !important;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
}

.el-dropdown-menu__item:last-child {
  margin-bottom: 0 !important;
}

.el-dropdown-menu__item:hover {
  background: rgba(0, 255, 136, 0.15) !important;
  color: var(--accent-green-light) !important;
  transform: translateX(4px);
}

.el-dropdown-menu__item.is-divided {
  border-top: 1px solid rgba(0, 255, 136, 0.2) !important;
  margin-top: 4px !important;
  padding-top: 10px !important;
}

@media (max-width: 900px) {
  .navbar {
    height: 70px;
  }

  .nav-container {
    padding: 0 1.5rem;
  }

  .nav-logo {
    font-size: 1.4rem;
  }

  .main-content {
    padding-top: 70px;
  }
}

@media (max-width: 768px) {
  /* 移动端隐藏顶部导航菜单 */
  .navbar {
    display: none;
  }

  .main-content {
    padding-top: 0;
    padding-bottom: 60px; /* 为底部导航栏留出空间 */
  }

  /* AI对话页面特殊处理 */
  .main-content:has(.ai-chat-container) {
    padding-bottom: 0;
  }
}

@media (max-width: 640px) {
  .navbar {
    display: none;
  }

  .nav-container {
    padding: 0 1rem;
  }

  .nav-logo {
    font-size: 1.2rem;
  }

  .nav-menu {
    gap: 1rem;
  }

  .nav-link {
    font-size: 0.85rem;
  }

  .main-content {
    padding-top: 0;
  }
}
</style>
