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
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { UserFilled, ArrowDown } from '@element-plus/icons-vue'
import GradientText from './bits-content/TextAnimations/GradientText/GradientText.vue'

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
    case 'logout':
      authStore.logout()
      router.push('/login')
      break
  }
}
</script>

<style>
#app {
  min-height: 100vh;
  background: var(--bg-primary);
}

.navbar {
  position: sticky;
  top: 0;
  z-index: 100;
  background: rgba(10, 10, 10, 0.95);
  backdrop-filter: blur(20px);
  border-bottom: 1px solid var(--border-color);
  box-shadow: 0 2px 20px rgba(0, 255, 136, 0.1);
}

.nav-container {
  max-width: 1920px;
  margin: 0 auto;
  padding: 1.2rem 4rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.nav-logo {
  text-decoration: none;
  font-size: 1.8rem;
  font-weight: bold;
}

.nav-menu {
  display: flex;
  gap: 2.5rem;
  align-items: center;
}

.nav-link {
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: 500;
  font-size: 1rem;
  transition: all 0.3s ease;
  position: relative;
  padding: 0.5rem 0;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.nav-link:hover {
  color: var(--accent-green);
}

.nav-link.router-link-active {
  color: var(--accent-green);
}

.nav-link.router-link-active::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(to right, var(--accent-green), var(--accent-yellow));
  border-radius: 2px;
  box-shadow: 0 0 10px var(--accent-green);
}

.main-content {
  min-height: calc(100vh - 80px);
  width: 100%;
}

.user-menu {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border: 1px solid var(--border-color);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.3s ease;
  color: var(--text-primary);
}

.user-menu:hover {
  border-color: var(--accent-green);
  background: rgba(0, 255, 136, 0.1);
}

.username {
  font-weight: 500;
}

:deep(.el-dropdown-menu) {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
}

:deep(.el-dropdown-menu__item) {
  color: var(--text-primary);
}

:deep(.el-dropdown-menu__item:hover) {
  background: rgba(0, 255, 136, 0.1);
  color: var(--accent-green);
}
</style>
